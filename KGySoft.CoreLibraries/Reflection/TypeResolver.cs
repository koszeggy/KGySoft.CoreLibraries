#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeResolver.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2025 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
#if NET9_0_OR_GREATER
using System.Reflection.Metadata;
#endif
using System.Text;
using System.Threading;

using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Represents a class that is able to convert/parse every runtime type to/from string.
    /// </summary>
#if !NET9_0_OR_GREATER
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart", Justification = "The other part is not visible in all targeted platforms")]
#endif
    internal sealed partial class TypeResolver
    {
        #region Nested types

        #region Enumerations

        private enum State
        {
            /// <summary>
            /// Empty stack.
            /// </summary>
            None,

            /// <summary>
            /// A type name optionally with assembly name. When not at root level, it's always embedded in [].
            /// </summary>
            FullNameOrAqn,

            /// <summary>
            /// A type name without assembly name (a name without embedding square brackets).
            /// </summary>
            TypeName,

            /// <summary>
            /// Assembly name part.
            /// </summary>
            AssemblyName,

            /// <summary>
            /// '[' in FullNameOrAqn or TypeName after a name.
            /// </summary>
            ArrayOrGeneric,

            /// <summary>
            /// Array, pointer, ByRef part
            /// </summary>
            Modifiers,

            /// <summary>
            /// Similar to FullNameOrAqn, but allows the expected type to be embedded in square brackets. Occurs in generics after ',', or function pointer after '(', ',' and ':' in a function pointer.
            /// </summary>
            BeforeArgument,

            /// <summary>
            /// After inner ']' when a type name is inside square brackets.
            /// </summary>
            AfterArgument,

            /// <summary>
            /// Inside an array declaration
            /// </summary>
            Array,

            /// <summary>
            /// ! at the beginning of FullNameOrAqn or TypeName
            /// </summary>
            GenericParameterName,

            /// <summary>
            /// !! at the beginning of FullNameOrAqn or TypeName
            /// </summary>
            GenericMethodParameterName,

            /// <summary>
            /// Signature of declaring method of generic parameter.
            /// </summary>
            MethodSignature,

            /// <summary>
            /// Function pointer name, or before calling conventions/parameters/return type
            /// </summary>
            FunctionPointer,

            /// <summary>
            /// In function pointer calling conventions. Unlike FunctionPointerParameters, it is parsed separately, because we don't expect
            /// complex types with potential generics, nesting and modifiers in calling conventions.
            /// </summary>
            FunctionPointerCallingConventions,

            /// <summary>
            /// In function pointer parameters. Acts as a marker on the stack only, it has no separate Parse method (parsed together with generic arguments).
            /// If it is on top of FunctionPointer, we are processing the parameters part, if it's missing, we are in the return type part.
            /// </summary>
            FunctionPointerParameters,

            /// <summary>
            /// Return from recursion.
            /// </summary>
            Return,

            /// <summary>
            /// Invalid state.
            /// </summary>
            Invalid
        }

        #endregion

        #region ParseContext struct

        [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
            Justification = "False alarm starting with FxCopAnalyzers 3.0 - it IS disposable and reader is also disposed")]
        private struct ParseContext : IDisposable
        {
            #region Fields

            private readonly TextReader reader;
            private readonly Stack<State> stack;
            private readonly StringBuilder buf;

            #endregion

            #region Properties

            internal bool Success => reader.Peek() == -1 && stack.Count == 0;
            internal char Char { get; private set; }

            internal State State
            {
                get => stack.Count == 0 ? State.None : stack.Peek();
                set
                {
                    if (stack.Count > 0)
                        Pop();
                    Push(value);
                }
            }

            internal State PrevState
            {
                get
                {
                    if (stack.Count <= 1)
                        return State.None;

                    var top = stack.Pop();
                    var result = stack.Peek();
                    Push(top);
                    return result;
                }
            }

            internal int Rank { get; set; }
            internal bool IsBufEmpty => buf.Length == 0;
            internal bool IsWhiteSpace => Char.IsWhiteSpace(Char);

            #endregion

            #region Constructor

            internal ParseContext(string name) : this()
            {
                reader = new StringReader(name);
                stack = new Stack<State>();
                buf = new StringBuilder(name.Length);
            }

            #endregion

            #region Methods

            #region Public Methods

            public void Dispose() => reader.Dispose();
            public override string ToString() => Enum<State>.ToString(State);

            #endregion

            #region Internal Methods

            internal void Push(State state)
            {
                stack.Push(state);
                Rank = 0;
            }

            internal State Pop()
            {
                if (stack.Count > 0)
                    return stack.Pop();
                Debug.Fail("Popping an empty stack");
                return State.Invalid;
            }

            internal bool Read()
            {
                int c = reader.Read();
                if (c == -1)
                {
                    Char = default;
                    return false;
                }

                Char = (char)c;
                return true;
            }

            internal void AppendChar() => buf.Append(Char);

            internal string GetBuf()
            {
                string result = buf.ToString().Trim();
                buf.Length = 0;
                return result;
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Constants

        #region Internal Constants

        internal const string StringTypeFullName = "System.String";
        internal const string FunctionPointerPrefix = "&fn";
        internal const string FunctionPointerUnmanagedPrefix = "*fn";

        #endregion

        #region Private Constants

        private const int pointer = -1;
        private const int byRef = -2;

        private const TypeNameKind removeAssemblyVersions = (TypeNameKind)(-1);
        private const TypeNameKind callingConventionName = (TypeNameKind)(-2);

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        private static ConditionallyStoringLockFreeCache<(string, int), Type?>? typeCacheByString; // Key: Type name + options (as int due to faster GetHashCode)
        private static ConditionallyStoringLockFreeCache<(Assembly, string, int), Type?>? typeCacheByAssembly; // Key: Assembly + type name + options
        private static LockFreeCache<(Type, int), string>? typeNameCache; // Key: Type + kind (as int due to faster GetHashCode)

        #endregion

        #region Instance Fields

        private readonly ResolveTypeOptions options;
        private readonly CircularList<int> modifiers = new();
        private readonly List<TypeResolver> typeArguments = new(); // generic or function pointer
        private readonly List<TypeResolver> functionPointerCallingConventions = new();

        private string? rootName;
        private string? assemblyName;
        private TypeResolver? declaringType;
        private string? declaringMethod;
        private TypeResolver? functionPointerReturnType;

        private Type? type;
        private Assembly? assembly;

        #endregion

        #endregion

        #region Properties

        private static ConditionallyStoringLockFreeCache<(string, int), Type?> TypeCacheByString
        {
            get
            {
                if (typeCacheByString == null)
                    Interlocked.CompareExchange(ref typeCacheByString, new ConditionallyStoringLockFreeCache<(string, int), Type?>(TryResolveType, LockFreeCacheOptions.Profile256), null);
                return typeCacheByString;
            }
        }

        private static ConditionallyStoringLockFreeCache<(Assembly, string, int), Type?> TypeCacheByAssembly
        {
            get
            {
                if (typeCacheByAssembly == null)
                    Interlocked.CompareExchange(ref typeCacheByAssembly, new ConditionallyStoringLockFreeCache<(Assembly, string, int), Type?>(TryResolveTypeByAssembly, LockFreeCacheOptions.Profile256), null);
                return typeCacheByAssembly;
            }
        }

        private static LockFreeCache<(Type, int), string> TypeNameCache
        {
            get
            {
                if (typeNameCache == null)
                    Interlocked.CompareExchange(ref typeNameCache, new LockFreeCache<(Type, int), string>(FormatType, null, LockFreeCacheOptions.Profile256), null);
                return typeNameCache;
            }
        }

        #endregion

        #region Constructors

        #region For Parsing

        private TypeResolver(ResolveTypeOptions options) => this.options = options;

        private TypeResolver(string typeName, ResolveTypeOptions options) : this(options)
        {
            if (typeName == null!)
                Throw.ArgumentNullException(Argument.typeName);
            Initialize(typeName);
        }

        private TypeResolver(Assembly assembly, string typeName, ResolveTypeOptions options) : this(options)
        {
            if (typeName == null!)
                Throw.ArgumentNullException(Argument.typeName);

            this.assembly = assembly;
            assemblyName = assembly.FullName;
            Initialize(typeName);
        }

        #endregion

        #region For Building Name

        private TypeResolver(Type type, TypeNameKind kind, Func<Type, AssemblyName?>? assemblyNameResolver, Func<Type, string?>? typeNameResolver)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            this.type = type;

            // modifiers
            while (type.HasElementType)
            {
                if (type.IsArray)
                    modifiers.AddFirst(type.IsZeroBasedArray() ? 0 : type.GetArrayRank());
                else if (type.IsByRef)
                    modifiers.AddFirst(byRef);
                else if (type.IsPointer)
                    modifiers.AddFirst(pointer);
                type = type.GetElementType()!;
            }

            // generic arguments
            if (type.IsConstructedGenericType())
            {
                TypeNameKind subKind = kind == TypeNameKind.FullName ? TypeNameKind.AssemblyQualifiedName
                    : kind == TypeNameKind.ForcedFullName ? TypeNameKind.ForcedAssemblyQualifiedName
                    : kind;
                foreach (Type genericArgument in type.GetGenericArguments())
                    typeArguments.Add(new TypeResolver(genericArgument, subKind, assemblyNameResolver, typeNameResolver));

                type = type.GetGenericTypeDefinition();
            }

            // generic parameter
            if (type.IsGenericParameter)
            {
                rootName = type.Name;
                declaringType = new TypeResolver(type.DeclaringType!, kind, assemblyNameResolver, typeNameResolver);
                declaringMethod = type.DeclaringMethod?.ToString();
                return;
            }

#if NET8_0_OR_GREATER
            // function pointer
            if (type.IsFunctionPointer)
            {
                rootName = type.IsUnmanagedFunctionPointer ? FunctionPointerUnmanagedPrefix : FunctionPointerPrefix;
                functionPointerReturnType = new TypeResolver(type.GetFunctionPointerReturnType(), kind, assemblyNameResolver, typeNameResolver);
                foreach (Type conv in type.GetFunctionPointerCallingConventions())
                    functionPointerCallingConventions.Add(new TypeResolver(conv, kind, assemblyNameResolver, typeNameResolver));
                foreach (Type paramType in type.GetFunctionPointerParameterTypes())
                    typeArguments.Add(new TypeResolver(paramType, kind, assemblyNameResolver, typeNameResolver));
                return;
            }
#endif

            // root type
            rootName = typeNameResolver?.Invoke(type) ?? type.FullName;
            assembly = type.Assembly;
            if (kind is TypeNameKind.AssemblyQualifiedName or TypeNameKind.ForcedAssemblyQualifiedName)
                assemblyName = assemblyNameResolver?.Invoke(type)?.FullName ?? assembly.FullName;

        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        #region Internal Methods

        internal static Type? ResolveType(string typeName, Func<AssemblyName?, string, Type?>? typeResolver, ResolveTypeOptions options)
        {
            if (typeName == null!)
                Throw.ArgumentNullException(Argument.typeName);
            if (!options.AllFlagsDefined())
                Throw.FlagsEnumArgumentOutOfRange(Argument.options, options);

            Type? result;

            // Trying to use the cache but only if no resolver is specified
            if (typeResolver == null)
            {
                if ((options & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None)
                    options &= ~ResolveTypeOptions.AllowPartialAssemblyMatch;

                // The item loader can throw an exception and only non-null values will be stored in the cache
                return TypeCacheByString[(typeName, (int)options)];
            }

            // going on with resolver, whose result is not cached
            try
            {
                result = new TypeResolver(typeName, options).Resolve(typeResolver);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                    Throw.ReflectionException(Res.ReflectionNotAType(typeName), e);
                return null;
            }

            if (result == null && (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                Throw.ReflectionException(Res.ReflectionNotAType(typeName));

            return result;
        }

        internal static Type? ResolveType(Assembly assembly, string typeName, ResolveTypeOptions options)
        {
            if (assembly == null!)
                Throw.ArgumentNullException(Argument.assembly);
            if (typeName == null!)
                Throw.ArgumentNullException(Argument.typeName);
            if (!options.AllFlagsDefined())
                Throw.FlagsEnumArgumentOutOfRange(Argument.options, options);

            if ((options & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None)
                options &= ~ResolveTypeOptions.AllowPartialAssemblyMatch;

            // the item loader can throw an exception and only non-null values will be stored in the cache
            return TypeCacheByAssembly[(assembly, typeName, (int)options)];
        }

#if NET9_0_OR_GREATER
        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "Forwarding the validation to the other overload.")]
        internal static Type? ResolveType(TypeName typeName, Func<TypeName, Type?>? typeResolver, ResolveTypeOptions options)
        {
            // Using the caching version. We have to use our own parser because TypeName has no overridden Equals/GetHashCode, so we must use a string as key.
            // Unfortunately this may allocate again and again for different TypeName instances resulting the same AQN.
            if (typeResolver == null)
                return ResolveType(typeName?.AssemblyQualifiedName!, null, options);

            if (typeName == null!)
                Throw.ArgumentNullException(Argument.typeName);
            if (!options.AllFlagsDefined())
                Throw.FlagsEnumArgumentOutOfRange(Argument.options, options);

            Type? result;

            // going on with the resolver delegate, whose result is not cached
            try
            {
                result = new TypeNameResolver(typeName, options).Resolve(typeResolver);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                    Throw.ReflectionException(Res.ReflectionNotAType(typeName.AssemblyQualifiedName), e);
                return null;
            }

            if (result == null && (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                Throw.ReflectionException(Res.ReflectionNotAType(typeName.AssemblyQualifiedName));

            return result;
        }

        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "Forwarding the validation to the other overload.")]
        internal static Type? ResolveType(Assembly assembly, TypeName typeName, ResolveTypeOptions options)
        {
            // Using the caching version. We have to use our own parser because TypeName has no overridden Equals/GetHashCode, so we must use a string as key.
            // Unfortunately this may allocate again and again for different TypeName instances resulting the same AQN.
            // NOTE: Passing the AssemblyQualifiedName instead of FullName is intended, so it can throw if it also contains an assembly name.
            return ResolveType(assembly, typeName?.AssemblyQualifiedName!, options);
        }

#endif

        internal static string GetName(Type type, TypeNameKind kind, Func<Type, AssemblyName?>? assemblyNameResolver, Func<Type, string?>? typeNameResolver)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            if (!Enum<TypeNameKind>.IsDefined(kind))
                Throw.EnumArgumentOutOfRange(Argument.kind, kind);

            // not caching if the result can be provided by delegates
            if (assemblyNameResolver != null || typeNameResolver != null)
                return new TypeResolver(type, kind, assemblyNameResolver, typeNameResolver).GetName(kind)!;

            return TypeNameCache[(type, (int)kind)];
        }

        internal static string StripName(string typeName, bool stripVersionOnly)
            => new TypeResolver(typeName, ResolveTypeOptions.None).GetName(stripVersionOnly ? removeAssemblyVersions : TypeNameKind.LongName) ?? typeName;

        internal static string? GetName(string typeName, TypeNameKind kind)
        {
            if (!Enum<TypeNameKind>.IsDefined(kind))
                Throw.EnumArgumentOutOfRange(Argument.kind, kind);
            return new TypeResolver(typeName, ResolveTypeOptions.None).GetName(kind);
        }

        #endregion

        #region Private Methods

        private static Type? TryResolveType((string TypeName, int Options) key, out bool storeValue)
        {
            Type? result;
            try
            {
                result = new TypeResolver(key.TypeName, (ResolveTypeOptions)key.Options).Resolve(null);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((key.Options & (int)ResolveTypeOptions.ThrowError) != 0)
                    Throw.ReflectionException(Res.ReflectionNotAType(key.TypeName), e);
                storeValue = false;
                return null;
            }

            if (result == null && (key.Options & (int)ResolveTypeOptions.ThrowError) != 0)
                Throw.ReflectionException(Res.ReflectionNotAType(key.TypeName));

            storeValue = result != null;
            return result;
        }

        private static Type? TryResolveTypeByAssembly((Assembly Assembly, string TypeName, int Options) key, out bool storeValue)
        {
            #region Local Methods

            static int GetAssemblyNamePos(string typeName)
            {
                int compoundNameEnd = typeName.LastIndexOf(']');
                return typeName.IndexOf(',', compoundNameEnd + 1);
            }

            #endregion

            Type? result;
            if (GetAssemblyNamePos(key.TypeName) >= 0)
            {
                if ((key.Options & (int)ResolveTypeOptions.ThrowError) != 0)
                    Throw.ArgumentException(Argument.typeName, Res.ReflectionTypeWithAssemblyName);
                storeValue = false;
                return null;
            }

            try
            {
                result = key.Assembly.GetType(key.TypeName) ?? new TypeResolver(key.Assembly, key.TypeName, (ResolveTypeOptions)key.Options).Resolve(null);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((key.Options & (int)ResolveTypeOptions.ThrowError) != 0)
                    Throw.ReflectionException(Res.ReflectionNotAType(key.TypeName), e);
                storeValue = false;
                return null;
            }

            if (result == null && (key.Options & (int)ResolveTypeOptions.ThrowError) != 0)
                Throw.ReflectionException(Res.ReflectionNotAType(key.TypeName));

            storeValue = result != null;
            return result;
        }

        private static string FormatType((Type Type, int Kind) key)
            => new TypeResolver(key.Type, (TypeNameKind)key.Kind, null, null).GetName((TypeNameKind)key.Kind)!;

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        public override string ToString() => GetName(TypeNameKind.ForcedAssemblyQualifiedName) ?? base.ToString()!;

        #endregion

        #region Private Methods

        private void Initialize(string typeName)
        {
            // Cannot be put in using due to the ref parameter usage so using try-finally.
            var context = new ParseContext(typeName);
            try
            {
                context.Push(State.FullNameOrAqn);
                Parse(ref context);
                if (context.Success)
                    return;
                if ((options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                    Throw.ReflectionException(Res.ReflectionNotAType(typeName));
            }
            finally
            {
                context.Dispose();
            }

            // Initialization failed but throwError is false: clearing everything.
            rootName = null;
            assemblyName = null;
            assembly = null;
            modifiers.Clear();
            typeArguments.Clear();
            declaringType = null;
            declaringMethod = null;
            functionPointerCallingConventions.Clear();
            functionPointerReturnType = null;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        private void Parse(ref ParseContext context)
        {
            #region Local Methods to Reduce Complexity

            void ParseFullNameOrAqn(ref ParseContext ctx)
            {
                if (ctx.Char == ',') // assembly separator
                {
                    rootName = ctx.GetBuf();
                    ctx.State = State.AssemblyName;
                    return;
                }

                if (ctx.Char == ']') // end of current argument, returning from recursion
                {
                    Debug.Assert(rootName == null && !ctx.IsBufEmpty || rootName != null && ctx.IsBufEmpty && functionPointerReturnType != null, "rootName is already set if returning from function pointer");
                    rootName ??= ctx.GetBuf();
                    ctx.State = State.AfterArgument;
                    ctx.Push(State.Return);
                    return;
                }

                // common part with TypeName
                ParseFullNameOrAqnAndTypeNameCommon(ref ctx);
            }

            void ParseTypeName(ref ParseContext ctx)
            {
                if (ctx.Char == ',') // Type name separator in type arguments or function pointer return: returning from recursion
                {
                    rootName = ctx.GetBuf();
                    ctx.Pop();
                    if (ctx.State != State.FunctionPointer)
                        ctx.Push(State.BeforeArgument);
                    ctx.Push(State.Return);
                    return;
                }

                if (ctx.Char == ']') // end of generic or nested function pointer type, returning from recursion
                {
                    rootName = ctx.GetBuf();
                    ctx.Pop();
                    if (ctx.State != State.FunctionPointer)
                        ctx.Push(State.Modifiers);
                    ctx.Push(State.Return);
                    return;
                }

                if (ctx.Char == ')' && ctx.PrevState is State.FunctionPointerParameters or State.FunctionPointer)
                {
                    rootName = ctx.GetBuf();
                    ctx.State = State.Return;
                    return;
                }

                // common part with FullNameOrAqn
                ParseFullNameOrAqnAndTypeNameCommon(ref ctx);
            }

            void ParseFullNameOrAqnAndTypeNameCommon(ref ParseContext ctx)
            {
                if (ctx.Char == '[') // array or generic type arguments after a name
                {
                    if (rootName != null || ctx.IsBufEmpty)
                    {
                        ctx.State = State.Invalid;
                        return;
                    }

                    rootName = ctx.GetBuf();
                    ctx.Push(State.ArrayOrGeneric);
                    return;
                }

                if (ctx.Char is '*' or '&')
                {
                    if (ctx.IsBufEmpty)
                    {
                        ctx.Push(State.FunctionPointer);
                        ctx.AppendChar();
                        return;
                    }

                    rootName = ctx.GetBuf();
                    modifiers.Add(ctx.Char == '*' ? pointer : byRef);
                    ctx.Push(State.Modifiers);
                    return;
                }

                if (ctx.Char == '!' && ctx.IsBufEmpty) // generic parameter
                {
                    ctx.Push(State.GenericParameterName);
                    return;
                }

                if (ctx.IsBufEmpty && ctx.IsWhiteSpace)
                    return;

                ctx.AppendChar();
            }

            void ParseAssemblyName(ref ParseContext ctx)
            {
                if (ctx.Char == ']') // end of current argument, returning from recursion
                {
                    assemblyName = ctx.GetBuf();
                    ctx.State = State.AfterArgument;
                    ctx.Push(State.Return);
                    return;
                }

                ctx.AppendChar();
            }

            void ParseArrayOrGeneric(ref ParseContext ctx)
            {
                if (ctx.Char == ']') // Zero-based 1D array
                {
                    modifiers.Add(0);
                    ctx.State = State.Modifiers;
                    return;
                }

                if (ctx.Char == ',') // multidimensional array
                {
                    ctx.State = State.Array;
                    ctx.Rank = 2;
                    return;
                }

                if (ctx.Char == '*') // nonzero-based array
                {
                    ctx.State = State.Array;
                    ctx.Rank = 1;
                    return;
                }

                // Otherwise, in generic argument. Everything else is common with BeforeArgument
                ParseBeforeArgument(ref ctx);
            }

            void ParseModifiers(ref ParseContext ctx)
            {
                if (ctx.Char == ',') // parameter, type argument or assembly separator
                {
                    ctx.Pop();

                    // AQN: switching to assembly name part
                    if (ctx.State == State.FullNameOrAqn)
                    {
                        ctx.State = State.AssemblyName;
                        return;
                    }

                    // Type name separator in generic: returning from recursion
                    if (ctx.State == State.TypeName)
                    {
                        ctx.State = State.BeforeArgument;
                        ctx.Push(State.Return);
                        return;
                    }

                    ctx.State = State.Invalid;
                    return;
                }

                if (ctx.Char == '[') // array
                {
                    ctx.State = State.Array;
                    return;
                }

                if (ctx.Char == ']') // end of generic type or function pointer: returning from recursion
                {
                    ctx.Pop();
                    State state = ctx.Pop();
                    if (state == State.FullNameOrAqn)
                        ctx.Push(State.AfterArgument);
                    else if (ctx.State != State.FunctionPointer)
                        ctx.Push(state == State.TypeName ? State.Modifiers : State.Invalid);
                    ctx.Push(State.Return);
                    return;
                }

                if (ctx.Char == '*') // pointer
                {
                    modifiers.Add(pointer);
                    return;
                }

                if (ctx.Char == '&') // ByRef type
                {
                    modifiers.Add(byRef);
                    return;
                }

                if (ctx.Char == ':') // generic parameter identifier or function pointer return value
                {
                    ctx.Pop();
                    switch (ctx.State)
                    {
                        case State.GenericParameterName:
                            ParseGenericParameterName(ref ctx);
                            return;
                        case State.GenericMethodParameterName:
                            ParseGenericMethodParameterName(ref ctx);
                            return;
                        case State.FunctionPointer:
                            ctx.Push(State.BeforeArgument);
                            return;
                        default:
                            ctx.State = State.Invalid;
                            return;
                    }
                }

                if (ctx.Char == ')') // end of function pointer parameters
                {
                    ctx.Pop();
                    ctx.State = ctx is { State: State.TypeName, PrevState: State.FunctionPointerParameters } ? State.Return : State.Invalid;
                    return;
                }

                if (ctx.IsWhiteSpace)
                    return;

                ctx.State = State.Invalid;
            }

            void ParseArray(ref ParseContext ctx)
            {
                if (ctx.Char == ']') // End of array
                {
                    modifiers.Add(ctx.Rank);
                    ctx.State = State.Modifiers;
                    return;
                }

                if (ctx.Char == ',') // array rank
                {
                    ctx.Rank = ctx.Rank < 2 ? 2 : ctx.Rank + 1;
                    return;
                }

                if (ctx.Char == '*') // nonzero-based array
                {
                    if (ctx.Rank == 0)
                    {
                        ctx.Rank = 1;
                        return;
                    }

                    ctx.State = State.Invalid;
                    return;
                }

                if (ctx.IsWhiteSpace)
                    return;

                ctx.State = State.Invalid;
            }

            void ParseBeforeArgument(ref ParseContext ctx)
            {
                if (ctx.Char is ']' or ',')
                {
                    ctx.State = State.Invalid;
                    return;
                }

                if (ctx.IsWhiteSpace)
                    return;

                TypeResolver arg;
                if (ctx.Char == '[') // AQN in type arguments or in return type
                {
                    arg = new TypeResolver(options);
                    ctx.State = State.FullNameOrAqn;
                    arg.Parse(ref ctx);
                    if (ctx.State != State.AfterArgument)
                    {
                        ctx.State = State.Invalid;
                        return;
                    }

                    if (ctx.PrevState == State.FunctionPointer)
                    {
                        functionPointerReturnType = arg;
                        ctx.Pop();
                        ctx.Pop();
                        if (ctx.State != State.FullNameOrAqn) // finished function pointer was a nested parameter
                            ctx.Push(State.Return);
                        return;
                    }

                    // generic or function pointer parameters
                    typeArguments.Add(arg);
                    return;
                }

                // In function pointer parameters we accept empty params (), but not a closing parenthesis after ','
                if (ctx.Char == ')' && ctx.PrevState == State.FunctionPointerParameters)
                {
                    if (typeArguments.Count > 0) 
                    {
                        ctx.State = State.Invalid;
                        return;
                    }

                    ctx.Pop();
                    ctx.State = State.Modifiers;
                    return;
                }

                // type name in generics and function parameters: recursion
                ctx.State = ctx.Char is '&' or '*' ? State.FunctionPointer : State.TypeName;
                arg = new TypeResolver(options);
                if (ctx.Char == '!')
                    ctx.Push(State.GenericParameterName);
                else
                    ctx.AppendChar();

                arg.Parse(ref ctx);
                switch (ctx.State)
                {
                    case State.Modifiers or State.BeforeArgument:
                        // it can be even function pointer parameter if ctx.PrevState is FunctionPointerParameters or FunctionPointer
                        typeArguments.Add(arg);
                        return;

                    case State.FunctionPointerParameters:
                        typeArguments.Add(arg);
                        switch (ctx.Char)
                        {
                            case ']':
                                ctx.Push(State.AfterArgument);
                                return;
                            case ')':
                                ctx.State = State.Modifiers;
                                return;
                            default:
                                ctx.State = State.Invalid;
                                return;
                        }

                    case State.FunctionPointer:
                        functionPointerReturnType = arg;
                        ctx.Pop();
                        switch (ctx.State)
                        {
                            case State.FullNameOrAqn: // return type was at the end or in []
                                ctx.Pop();
                                if (ctx.State is State.FunctionPointerParameters or State.FunctionPointer)
                                {
                                    ctx.Push(State.AfterArgument);
                                    ctx.Push(State.Return);
                                }

                                return;
                            case State.FunctionPointerParameters: // exiting from nested parameters
                                if (ctx.Char == ')') // exiting from nested parameters
                                    ctx.State = State.Modifiers;
                                else // exiting from return part of nested parameters
                                    ctx.Push(ctx.Char switch { ',' => State.BeforeArgument, ']' => State.AfterArgument, _ => State.Invalid });
                                ctx.Push(State.Return);
                                return;
                            case State.FunctionPointer: // exiting from nested return type
                                if (ctx.Char == ']')
                                    ctx.Push(State.AfterArgument);
                                ctx.Push(State.Return);
                                return;
                            default:
                                ctx.State = State.Invalid;
                                return;
                        }

                    default:
                        ctx.State = State.Invalid;
                        return;
                }
            }

            static void ParseAfterArgument(ref ParseContext ctx)
            {
                if (ctx.Char == ',') // next argument
                {
                    ctx.State = State.BeforeArgument;
                    return;
                }

                if (ctx.Char == ']') // end of generic arguments
                {
                    ctx.State = State.Modifiers;
                    return;
                }

                if (ctx.Char == ')') // end of function pointer parameters
                {
                    ctx.Pop();
                    ctx.State = ctx.State == State.FunctionPointerParameters ? State.Modifiers : State.Invalid;
                    return;
                }

                if (ctx.IsWhiteSpace)
                    return;

                ctx.State = State.Invalid;
            }

            void ParseGenericParameterName(ref ParseContext ctx)
            {
                if (ctx.Char == '!' && ctx.IsBufEmpty)
                {
                    ctx.State = State.GenericMethodParameterName;
                    return;
                }

                if (ctx.Char == ':') // generic parameter declaring type
                {
                    rootName ??= ctx.GetBuf();
                    ParseDeclaringType(ref ctx);
                    return;
                }

                // Common part with GenericMethodParameterName
                ParseGenericParameterNameCommon(ref ctx);
            }

            void ParseGenericMethodParameterName(ref ParseContext ctx)
            {
                if (ctx.Char == '!' && ctx.IsBufEmpty)
                {
                    ctx.State = State.Invalid;
                    return;
                }

                if (ctx.Char == ':') // generic method signature
                {
                    rootName ??= ctx.GetBuf();
                    ctx.State = State.MethodSignature;
                    return;
                }

                // Common part with GenericParameterName
                ParseGenericParameterNameCommon(ref ctx);
            }

            void ParseGenericParameterNameCommon(ref ParseContext ctx)
            {
                if (ctx.Char == '[') // array
                {
                    rootName = ctx.GetBuf();
                    ctx.Push(State.Array);
                    return;
                }

                if (ctx.Char is '*' or '&') // pointer/ByRef
                {
                    rootName = ctx.GetBuf();
                    modifiers.Add(ctx.Char == '*' ? pointer : byRef);
                    ctx.Push(State.Modifiers);
                    return;
                }

                ctx.AppendChar();
            }

            void ParseDeclaringType(ref ParseContext ctx)
            {
                ctx.Pop();
                Debug.Assert(ctx.State is State.FullNameOrAqn or State.TypeName);
                var def = new TypeResolver(options);
                def.Parse(ref ctx);
                if (ctx.State is not (State.None or State.AfterArgument or State.BeforeArgument or State.Modifiers))
                {
                    ctx.State = State.Invalid;
                    return;
                }

                declaringType = def;
                ctx.Push(State.Return);
            }

            void ParseMethodSignature(ref ParseContext ctx)
            {
                if (ctx.Char == ':')
                {
                    declaringMethod = ctx.GetBuf();
                    ParseDeclaringType(ref ctx);
                    return;
                }

                ctx.AppendChar();
            }

            void ParseFunctionPointer(ref ParseContext ctx)
            {
                // calling conventions
                if (ctx.Char == '[')
                {
                    if (functionPointerCallingConventions.Count > 0 || typeArguments.Count > 0)
                    {
                        ctx.State = State.Invalid;
                        return;
                    }

                    rootName = ctx.GetBuf();
                    ctx.Push(State.FunctionPointerCallingConventions);
                    return;
                }

                // parameters
                if (ctx.Char == '(')
                {
                    if (typeArguments.Count > 0)
                    {
                        ctx.State = State.Invalid;
                        return;
                    }

                    rootName ??= ctx.GetBuf();
                    ctx.Push(State.FunctionPointerParameters);
                    ctx.Push(State.BeforeArgument);
                    return;
                }

                // whitespace after calling conventions or parameters
                if (ctx.IsWhiteSpace && rootName != null)
                    return;

                ctx.AppendChar();
            }

            void ParseFunctionPointerCallingConventions(ref ParseContext ctx)
            {
                if (ctx.Char is ',' or ']')
                {
                    if (ctx.IsBufEmpty)
                    {
                        ctx.State = State.Invalid;
                        return;
                    }

                    functionPointerCallingConventions.Add(new TypeResolver(options) { rootName = ctx.GetBuf().Trim() });
                    if (ctx.Char == ']')
                        ctx.Pop();
                    return;
                }

                if (ctx.IsWhiteSpace && ctx.IsBufEmpty)
                    return;

                ctx.AppendChar();
            }

            #endregion

            while (context.Read())
            {
                switch (context.State)
                {
                    case State.FullNameOrAqn:
                        ParseFullNameOrAqn(ref context);
                        break;

                    case State.TypeName:
                        ParseTypeName(ref context);
                        break;

                    case State.AssemblyName:
                        ParseAssemblyName(ref context);
                        break;

                    case State.ArrayOrGeneric:
                        ParseArrayOrGeneric(ref context);
                        break;

                    case State.Modifiers:
                        ParseModifiers(ref context);
                        break;

                    case State.Array:
                        ParseArray(ref context);
                        break;

                    case State.BeforeArgument:
                        ParseBeforeArgument(ref context);
                        break;

                    case State.AfterArgument:
                        ParseAfterArgument(ref context);
                        break;

                    case State.GenericParameterName:
                        ParseGenericParameterName(ref context);
                        break;

                    case State.GenericMethodParameterName:
                        ParseGenericMethodParameterName(ref context);
                        break;

                    case State.MethodSignature:
                        ParseMethodSignature(ref context);
                        break;

                    case State.FunctionPointer:
                        ParseFunctionPointer(ref context);
                        break;

                    case State.FunctionPointerCallingConventions:
                        ParseFunctionPointerCallingConventions(ref context);
                        break;

                    case State.Invalid:
                        return;

                    case State.None:
                        if (context.IsWhiteSpace)
                            break;

                        // the parsing has ended, but there is still something in the queue
                        context.Push(State.Invalid);
                        return;

                    case State.FunctionPointerParameters: // it's just a stack marker state, not expected to be parsed
                    default:
                        Throw.InternalError($"Unexpected state: {context.State}");
                        return;
                }

                if (context.State == State.Return)
                {
                    context.Pop();
                    return;
                }
            }

            // finishing initialization
            switch (context.State)
            {
                // simple type without assembly name
                case State.FullNameOrAqn:
                case State.TypeName:
                    Debug.Assert(rootName == null || functionPointerReturnType != null);
                    rootName ??= context.GetBuf();
                    context.Pop();
                    break;

                case State.AssemblyName:
                    assemblyName = context.GetBuf();
                    context.Pop();
                    break;

                case State.Modifiers:
                    context.Pop();
                    if (context.Pop() is not (State.FullNameOrAqn or State.TypeName))
                        context.Push(State.Invalid);
                    break;

                default:
                    return;
            }
        }

        private string? GetName(TypeNameKind kind)
        {
            if (rootName == null)
                return null;
            var result = new StringBuilder();
            DumpName(result, kind);
            return result.ToString();
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        private void DumpName(StringBuilder result, TypeNameKind typeNameKind)
        {
            #region Local Methods to Reduce Complexity

            void DumpGenericParameterIndicator(StringBuilder sb, TypeNameKind kind)
            {
                if (kind == TypeNameKind.ShortName || declaringType == null)
                    return;

                sb.Append('!');
                if (declaringMethod != null)
                    sb.Append('!');
            }

            void DumpRootName(StringBuilder sb, TypeNameKind kind)
            {
                switch (kind)
                {
                    case TypeNameKind.ShortName:
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                        sb.Append(rootName!.AsSegment().Split('.', '+').LastOrDefault().AsSpan);
#else
                        sb.Append(rootName!.AsSegment().Split('.', '+').LastOrDefault().ToString());
#endif
                        break;

                    case callingConventionName:
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                        sb.Append(rootName!.StartsWith("CallConv", StringComparison.Ordinal) ? rootName.AsSpan("CallConv".Length) : rootName);
#else
                        sb.Append(rootName!.StartsWith("CallConv", StringComparison.Ordinal) ? rootName.Substring("CallConv".Length) : rootName);
#endif
                        break;

                    default:
                        sb.Append(rootName);
                        break;
                }
            }

            void DumpGenericArguments(StringBuilder sb, TypeNameKind kind)
            {
                if (typeArguments.Count == 0 || functionPointerReturnType != null)
                    return;

                sb.Append('[');
                for (int i = 0; i < typeArguments.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    TypeResolver arg = typeArguments[i];
                    bool aqn = kind == TypeNameKind.ForcedAssemblyQualifiedName
                        || kind is TypeNameKind.AssemblyQualifiedName or removeAssemblyVersions
                            && !AssemblyResolver.IsCoreLibAssemblyName(arg.assemblyName ?? arg.declaringType?.assemblyName!);
                    if (aqn)
                        sb.Append('[');
                    arg.DumpName(sb, kind);
                    if (aqn)
                        sb.Append(']');
                }

                sb.Append(']');
            }

            void DumpModifiers(StringBuilder sb)
            {
                foreach (int rank in modifiers)
                {
                    switch (rank)
                    {
                        case byRef:
                            sb.Append('&');
                            break;
                        case pointer:
                            sb.Append('*');
                            break;
                        case 0:
                            sb.Append("[]");
                            break;
                        case 1:
                            sb.Append("[*]");
                            break;
                        default:
                            sb.Append('[');
                            sb.Append(',', rank - 1);
                            sb.Append(']');
                            break;
                    }
                }
            }

            void DumpGenericParameter(StringBuilder sb, TypeNameKind kind)
            {
                if (kind == TypeNameKind.ShortName || declaringType == null)
                    return;
                if (declaringMethod != null)
                {
                    sb.Append(':');
                    sb.Append(declaringMethod);
                }

                sb.Append(':');
                declaringType.DumpName(sb, kind);
            }

            void DumpAssemblyName(StringBuilder sb, TypeNameKind kind)
            {
                if (assemblyName == null
                    || !(kind == TypeNameKind.ForcedAssemblyQualifiedName
                        || kind is TypeNameKind.AssemblyQualifiedName or removeAssemblyVersions
                        && !AssemblyResolver.IsCoreLibAssemblyName(assemblyName)))
                {
                    return;
                }

                sb.Append(", ");
                string asmName = assemblyName;
                if (kind == removeAssemblyVersions)
                {
                    var an = new AssemblyName(asmName);
                    if (an.Version != null)
                    {
                        an.Version = null;
                        asmName = an.FullName;
                    }
                }

                sb.Append(asmName);
            }

            void DumpFunctionPointerCallingConventions(StringBuilder sb, TypeNameKind kind)
            {
                if (functionPointerCallingConventions.Count == 0)
                    return;

                // calling conventions
                if (kind != TypeNameKind.ShortName && functionPointerCallingConventions.Count > 0)
                {
                    sb.Append('[');
                    for (int i = 0; i < functionPointerCallingConventions.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(',');
                        functionPointerCallingConventions[i].DumpName(sb, callingConventionName);
                    }

                    sb.Append(']');
                }
            }

            void DumpFunctionPointerParameters(StringBuilder sb, TypeNameKind kind)
            {
                if (functionPointerReturnType == null)
                    return;

                sb.Append('(');
                for (int i = 0; i < typeArguments.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    TypeResolver param = typeArguments[i];
                    bool aqn = kind == TypeNameKind.ForcedAssemblyQualifiedName
                        || kind is TypeNameKind.AssemblyQualifiedName or removeAssemblyVersions
                        && !AssemblyResolver.IsCoreLibAssemblyName(param.assemblyName ?? param.declaringType?.assemblyName!);
                    if (aqn)
                        sb.Append('[');
                    param.DumpName(sb, kind);
                    if (aqn)
                        sb.Append(']');
                }

                sb.Append(')');
            }

            void DumpFunctionPointerReturnType(StringBuilder sb, TypeNameKind kind)
            {
                if (functionPointerReturnType == null)
                    return;

                sb.Append(':');
                bool aqn = kind == TypeNameKind.ForcedAssemblyQualifiedName
                    || kind is TypeNameKind.AssemblyQualifiedName or removeAssemblyVersions
                    && !AssemblyResolver.IsCoreLibAssemblyName(functionPointerReturnType.assemblyName ?? functionPointerReturnType.declaringType?.assemblyName!);
                if (aqn)
                    sb.Append('[');
                functionPointerReturnType.DumpName(sb, kind);
                if (aqn)
                    sb.Append(']');
            }

            #endregion

            // Generic type or method parameter indicator
            DumpGenericParameterIndicator(result, typeNameKind);

            // The root name without specifiers/parameters/etc.
            DumpRootName(result, typeNameKind);

            // Unmanaged function pointer: calling conventions
            DumpFunctionPointerCallingConventions(result, typeNameKind);

            // Function pointer: parameters (without the return value)
            DumpFunctionPointerParameters(result, typeNameKind);

            // Constructed generic type (possibly still an open type): generic arguments
            DumpGenericArguments(result, typeNameKind);

            // Modifiers (array ranks, pointers, ByRef)
            DumpModifiers(result);

            // Function pointer: return type (even if void)
            DumpFunctionPointerReturnType(result, typeNameKind);

            // Generic type of method parameter identification
            DumpGenericParameter(result, typeNameKind);

            // Assembly name
            DumpAssemblyName(result, typeNameKind);
        }

        private Type? Resolve(Func<AssemblyName?, string, Type?>? typeResolver)
        {
            if (type != null)
                return type;

            // RootName is null if parsing was unsuccessful.
            if (rootName == null)
                return null;

            // 1. Resolving root type
            Type? result = ResolveRootType(typeResolver);
            if (result == null)
                return null;

            // 2. Applying generic arguments
            if (typeArguments.Count > 0 && functionPointerReturnType == null)
            {
                Debug.Assert(result.IsGenericTypeDefinition, "Root type is expected to be a generic type definition");
                Type?[] args = new Type[typeArguments.Count];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = typeArguments[i].Resolve(typeResolver);
                    if (args[i] == null)
                        return null;
                }

                result = result.GetGenericType(args!);
            }

            // 3. Applying modifiers
            foreach (int modifier in modifiers)
            {
                switch (modifier)
                {
                    case 0: // zero based array
                        result = result.MakeArrayType();
                        break;
                    case byRef:
                        result = result.MakeByRefType();
                        break;
                    case pointer:
                        result = result.MakePointerType();
                        break;
                    default: // array rank
                        result = result.MakeArrayType(modifier);
                        break;
                }
            }

            return type = result;
        }

        private Type? ResolveRootType(Func<AssemblyName?, string, Type?>? typeResolver)
        {
            // It's important that typeResolver handles with standard types only, whereas extensions like generic parameters and function pointers
            // are handled always internally. The typeResolver is passed to them to handle the declaring type/parameters/return types.
            if (declaringType != null)
                return ResolveGenericParameter(typeResolver);
            if (functionPointerReturnType != null)
                return ResolveFunctionPointer(typeResolver);

            bool throwError = (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None;
            bool allowIgnoreAssembly = (options & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None;
            bool ignoreCase = (options & ResolveTypeOptions.IgnoreCase) != ResolveTypeOptions.None;
            Type? result;

            // 1.) By resolver
            if (typeResolver != null)
            {
                AssemblyName? asmName = null;
                try
                {
                    if (assemblyName != null)
                        asmName = new AssemblyName(assemblyName);
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    // If we cannot create even the AssemblyName, then we cannot query the custom resolver.
                    // This is OK, we don't want to support wrong names, which could break parsing.
                    // And this is the symmetric logic with GetName's assemblyNameResolver.
                    if (throwError)
                        Throw.ArgumentException(Res.ReflectionInvalidAssemblyName(assemblyName!), e);

                    // In this case we don't use fallback logic because we couldn't call the delegate.
                    return null;
                }

                result = typeResolver.Invoke(asmName, rootName!);
                if (result != null)
                    return result;
            }

            // 2. Resolving assembly if needed
            if (assembly == null && assemblyName != null)
            {
                var resolveAssemblyOptions = (ResolveAssemblyOptions)options & Enum<ResolveAssemblyOptions>.GetFlagsMask();
                if ((options & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None)
                    resolveAssemblyOptions &= ~ResolveAssemblyOptions.ThrowError;
                assembly = AssemblyResolver.ResolveAssembly(assemblyName, resolveAssemblyOptions);
                if (assembly == null && (options & ResolveTypeOptions.AllowIgnoreAssemblyName) == ResolveTypeOptions.None)
                    return null;
            }

            // 3/a. Resolving the type from a specific assembly
            if (assembly != null)
            {
                result = assembly.GetType(rootName!, throwError && !allowIgnoreAssembly, ignoreCase);
                if (result != null || !allowIgnoreAssembly)
                    return result;
            }
#if !NETFRAMEWORK // 3/b. If there is no assembly defined we try to use the mscorlib.dll in the first place, which contains forwarded types on non-framework platforms.
            else if (assemblyName == null)
            {
                // We are not throwing an exception from here because on failure we try all assemblies
                result = AssemblyResolver.MscorlibAssembly.GetType(rootName!, false, ignoreCase);
                if (result != null)
                    return result;
            }
#endif

            // 3/c. Resolving the type from any assembly
            // Type.GetType is not redundant even if we tried mscorlib.dll above because it still can load core library types.
            result = Type.GetType(rootName!, false, ignoreCase);
            if (result != null)
                return result;

            // Looking for the type in the loaded assemblies
            foreach (Assembly asm in Reflector.GetLoadedAssemblies())
            {
                result = asm.GetType(rootName!, false, ignoreCase);
                if (result != null)
                    return result;
            }

            if (throwError)
                Throw.ReflectionException(Res.ReflectionNotAType(rootName!));
            return null;
        }

        private Type? ResolveGenericParameter(Func<AssemblyName?, string, Type?>? typeResolver)
        {
            // Declaring Type
            Type? t = declaringType!.Resolve(typeResolver);
            if (t == null)
                return null; // it's OK, exception is thrown from inside when requested

            t = declaringMethod != null
                // Generic method argument
                ? t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(m => m.ToString() == declaringMethod)?.GetGenericArguments().FirstOrDefault(a => a.Name == rootName)
                // Generic type argument
                : t.GetGenericArguments().FirstOrDefault(a => a.Name == rootName);

            if (t is null && (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                Throw.ReflectionException(Res.ReflectionNotAType(ToString()));
            return t;
        }

#if !NET11_0_OR_GREATER
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Usage depends on targeted platform")]
#endif
        private Type? ResolveFunctionPointer(Func<AssemblyName?, string, Type?>? typeResolver)
        {
            Debug.Assert(functionPointerReturnType != null);
            bool throwError = (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None;
            if (rootName is not (FunctionPointerPrefix or FunctionPointerUnmanagedPrefix))
            {
                if (throwError)
                    Throw.ReflectionException(Res.ReflectionNotAType(rootName!));
                return null;
            }

#if NET11_0_OR_GREATER
#error Implement if API is already available - https://github.com/dotnet/runtime/issues/75348
#endif
            if (!throwError)
                return null;

            return Throw.PlatformNotSupportedException<Type?>(Res.ReflectionFunctionPointersNotSupported);
        }

        #endregion

        #endregion

        #endregion
    }
}
