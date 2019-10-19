#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeResolver.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Represents a class that is able to convert/parse every runtime type to/from string.
    /// </summary>
    internal sealed class TypeResolver
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
            /// A type name optionally with assembly name.
            /// </summary>
            FullNameOrAqn,

            /// <summary>
            /// A type name without assembly name.
            /// </summary>
            TypeName,

            /// <summary>
            /// Assembly name part.
            /// </summary>
            AssemblyName,

            /// <summary>
            /// [ in FullName or TypeName
            /// </summary>
            ArrayOrGeneric,

            /// <summary>
            /// Array, pointer, ByRef part
            /// </summary>
            Modifiers,

            /// <summary>
            /// After , in generic type
            /// </summary>
            BeforeArgument,

            /// <summary>
            /// After inner ] in generic type argument
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
                    Pop();
                    Push(value);
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

            internal void Pop()
            {
                if (stack.Count > 0)
                    stack.Pop();
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

        private const int pointer = -1;
        private const int byRef = -2;

        private const TypeNameKind removeAssemblyVersions = (TypeNameKind)(-1);

        #endregion

        #region Fields

        #region Static Fields

        private static LockingDictionary<string, Type> typeCacheByString;
        private static IThreadSafeCacheAccessor<Assembly, LockingDictionary<string, Type>> typeCacheByAssembly;
        private static IThreadSafeCacheAccessor<Type, LockingDictionary<TypeNameKind, string>> typeNameCache;

#if !NETFRAMEWORK
        private static Assembly mscorlibAssembly;
#endif

        #endregion

        #region Instance Fields

        private readonly ResolveTypeOptions options;
        private readonly CircularList<int> modifiers = new CircularList<int>();
        private readonly List<TypeResolver> genericArgs = new List<TypeResolver>();

        private string rootName;
        private string assemblyName;
        private TypeResolver declaringType;
        private string declaringMethod;

        private Type type;
        private Assembly assembly;

        #endregion

        #endregion

        #region Properties

        private static LockingDictionary<string, Type> TypeCacheByString
            => typeCacheByString ??= new Cache<string, Type>(256).AsThreadSafe();

        private static IThreadSafeCacheAccessor<Assembly, LockingDictionary<string, Type>> TypeCacheByAssembly
            => typeCacheByAssembly ??= new Cache<Assembly, LockingDictionary<string, Type>>(a => new Cache<string, Type>().AsThreadSafe()).GetThreadSafeAccessor(true); // true because the inner creation is fast

        private static IThreadSafeCacheAccessor<Type, LockingDictionary<TypeNameKind, string>> TypeNameCache
            => typeNameCache ??= new Cache<Type, LockingDictionary<TypeNameKind, string>>(t => new Dictionary<TypeNameKind, string>(1, ComparerHelper<TypeNameKind>.EqualityComparer).AsThreadSafe()).GetThreadSafeAccessor(true); // true because the inner creation is fast

#if !NETFRAMEWORK
        private static Assembly MscorlibAssembly => mscorlibAssembly ??= AssemblyResolver.ResolveAssembly("mscorlib", ResolveAssemblyOptions.TryToLoadAssembly);
#endif

        #endregion

        #region Constructors

        private TypeResolver(ResolveTypeOptions options) => this.options = options;

        private TypeResolver(string typeName, ResolveTypeOptions options) : this(options)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName), Res.ArgumentNull);

            Initialize(typeName);
        }

        private TypeResolver(Assembly assembly, string typeName, ResolveTypeOptions options) : this(options)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName), Res.ArgumentNull);

            this.assembly = assembly;
            assemblyName = assembly.FullName;
            Initialize(typeName);
        }

        private TypeResolver(Type type, TypeNameKind kind, Func<Type, AssemblyName> assemblyNameResolver, Func<Type, string> typeNameResolver)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(this.type), Res.ArgumentNull);

            // modifiers
            // ReSharper disable once PossibleNullReferenceException
            while (type.HasElementType)
            {
                if (type.IsArray)
                    modifiers.AddFirst(type.IsZeroBasedArray() ? 0 : type.GetArrayRank());
                else if (type.IsByRef)
                    modifiers.AddFirst(byRef);
                else if (type.IsPointer)
                    modifiers.AddFirst(pointer);
                type = type.GetElementType();
            }

            // generic arguments
            if (type.IsGenericType && !type.IsGenericTypeDefinition) // same as: type.IsConstructedGenericType from .NET4
            {
                TypeNameKind subKind = kind == TypeNameKind.FullName ? TypeNameKind.AssemblyQualifiedName
                    : kind == TypeNameKind.ForcedFullName ? TypeNameKind.ForcedAssemblyQualifiedName
                    : kind;
                foreach (Type genericArgument in type.GetGenericArguments())
                    genericArgs.Add(new TypeResolver(genericArgument, subKind, assemblyNameResolver, typeNameResolver));

                type = type.GetGenericTypeDefinition();
            }

            // root type
            bool isGenericParam = type.IsGenericParameter;
            rootName = isGenericParam ? type.Name : typeNameResolver?.Invoke(type) ?? type.FullName;
            if (!isGenericParam)
            {
                assembly = type.Assembly;
                if (kind.In(TypeNameKind.AssemblyQualifiedName, TypeNameKind.ForcedAssemblyQualifiedName))
                    assemblyName =  assemblyNameResolver?.Invoke(type)?.FullName ?? assembly.FullName;
                return;
            }

            // generic parameter
            declaringType = new TypeResolver(type.DeclaringType, kind, assemblyNameResolver, typeNameResolver);
            declaringMethod = type.DeclaringMethod?.ToString();
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Internal Methods

        internal static Type ResolveType(string typeName, Func<AssemblyName, string, Type> typeResolver, ResolveTypeOptions options)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName), Res.ArgumentNull);
            if (!options.AllFlagsDefined())
                throw new ArgumentOutOfRangeException(nameof(options), Res.FlagsEnumOutOfRange(options));

            string key = null;
            Type result;

            // Trying to use the cache but only if no resolver is specified
            if (typeResolver == null)
            {
                ResolveTypeOptions prefix = options & ~ResolveTypeOptions.ThrowError;
                if ((prefix & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None)
                    prefix &= ~ResolveTypeOptions.AllowPartialAssemblyMatch;
                key = ((int)prefix).ToString(CultureInfo.InvariantCulture) + typeName;
                if (TypeCacheByString.TryGetValue(key, out result))
                    return result;
            }

            try
            {
                result = new TypeResolver(typeName, options).Resolve(typeResolver);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                    throw new ReflectionException(Res.ReflectionNotAType(typeName), e);
                return null;
            }

            if (result == null && (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                throw new ReflectionException(Res.ReflectionNotAType(typeName));

            if (key != null && result != null)
                typeCacheByString[key] = result;

            return result;
        }

        internal static Type ResolveType(Assembly assembly, string typeName, ResolveTypeOptions options)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly), Res.ArgumentNull);
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName), Res.ArgumentNull);
            if (!options.AllFlagsDefined())
                throw new ArgumentOutOfRangeException(nameof(options), Res.FlagsEnumOutOfRange(options));

            ResolveTypeOptions prefix = options & ~ResolveTypeOptions.ThrowError;
            if ((prefix & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None)
                prefix &= ~ResolveTypeOptions.AllowPartialAssemblyMatch;
            string key = ((int)prefix).ToString(CultureInfo.InvariantCulture) + typeName;
            LockingDictionary<string, Type> cache = TypeCacheByAssembly[assembly];
            if (cache.TryGetValue(key, out Type result))
                return result;

            if (GetAssemblyNamePos(typeName) >= 0)
            {
                if ((options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                    throw new ArgumentException(Res.ReflectionTypeWithAssemblyName, nameof(typeName));
                return null;
            }

            try
            {
                result = assembly.GetType(typeName) ?? new TypeResolver(assembly, typeName, options).Resolve(null);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                    throw new ReflectionException(Res.ReflectionNotAType(typeName), e);
                return null;
            }

            if (result == null && (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None)
                throw new ReflectionException(Res.ReflectionNotAType(typeName));

            if (result != null)
                cache[key] = result;
            return result;
        }

        internal static string GetName(Type type, TypeNameKind kind, Func<Type, AssemblyName> assemblyNameResolver, Func<Type, string> typeNameResolver)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (!Enum<TypeNameKind>.IsDefined(kind))
                throw new ArgumentOutOfRangeException(nameof(kind), Res.EnumOutOfRange(kind));

            var resolver = new TypeResolver(type, kind, assemblyNameResolver, typeNameResolver);

            // not caching if the result can be provided by delegates
            if (assemblyNameResolver != null || typeNameResolver != null)
                return resolver.GetName(kind);

            LockingDictionary<TypeNameKind, string> cache = TypeNameCache[type];
            if (cache.TryGetValue(kind, out string result))
                return result;

            result = resolver.GetName(kind);

            cache[kind] = result;
            return result;
        }

        internal static string StripName(string typeName, bool stripVersionOnly)
            => new TypeResolver(typeName, ResolveTypeOptions.None).GetName(stripVersionOnly ? removeAssemblyVersions : TypeNameKind.LongName) ?? typeName;

        internal static void SplitName(string fullName, out string assemblyName, out string typeName)
        {
            int pos = GetAssemblyNamePos(fullName);
            if (pos < 0)
            {
                assemblyName = null;
                typeName = fullName;
                return;
            }

            assemblyName = fullName.Substring(pos + 1).Trim();
            typeName = fullName.Substring(0, pos).Trim();
        }

        #endregion

        #region Private Methods

        private static int GetAssemblyNamePos(string typeName)
        {
            int compoundNameEnd = typeName.LastIndexOf(']');
            return typeName.IndexOf(',', compoundNameEnd + 1);
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        public override string ToString() => GetName(TypeNameKind.ForcedAssemblyQualifiedName) ?? base.ToString();

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
                    throw new ReflectionException(Res.ReflectionNotAType(typeName));
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
            genericArgs.Clear();
            declaringType = null;
            declaringMethod = null;
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
                    rootName = ctx.GetBuf();
                    ctx.State = State.AfterArgument;
                    ctx.Push(State.Return);
                    return;
                }

                // common part with TypeName
                ParseFullNameOrAqnAndTypeNameCommon(ref ctx);
            }

            void ParseTypeName(ref ParseContext ctx)
            {
                if (ctx.Char == ',') // Type name separator in generic: returning from recursion
                {
                    rootName = ctx.GetBuf();
                    ctx.State = State.BeforeArgument;
                    ctx.Push(State.Return);
                    return;
                }

                if (ctx.Char == ']') // end of generics, returning from recursion
                {
                    rootName = ctx.GetBuf();
                    ctx.State = State.Modifiers;
                    ctx.Push(State.Return);
                    return;
                }

                // common part with FullNameOrAqn
                ParseFullNameOrAqnAndTypeNameCommon(ref ctx);
            }

            void ParseFullNameOrAqnAndTypeNameCommon(ref ParseContext ctx)
            {
                if (ctx.Char == '[') // array or generic type arguments
                {
                    Debug.Assert(rootName == null);
                    rootName = ctx.GetBuf();
                    ctx.Push(State.ArrayOrGeneric);
                    return;
                }

                if (ctx.Char.In('*', '&'))
                {
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
                if (ctx.Char == ',') // type or assembly separator
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

                    throw new InvalidOperationException(Res.InternalError($"Unexpected state: {ctx.State}"));
                }

                if (ctx.Char == '[') // array
                {
                    ctx.State = State.Array;
                    return;
                }

                if (ctx.Char == ']') // end of generic type: returning from recursion
                {
                    ctx.Pop();
                    ctx.State = ctx.State == State.FullNameOrAqn ? State.AfterArgument
                        : ctx.State == State.TypeName ? State.Modifiers
                        : State.Invalid;
                    ctx.Push(State.Return);
                    return;
                }

                if (ctx.Char == '*') // pointer
                {
                    modifiers.Add(pointer);
                    return;
                }

                if (ctx.Char == '*') // pointer
                {
                    modifiers.Add(pointer);
                    return;
                }

                if (ctx.Char == '&') // pointer
                {
                    modifiers.Add(byRef);
                    return;
                }

                if (ctx.Char == ':') // generic parameter identifier
                {
                    ctx.Pop();
                    Debug.Assert(ctx.State.In(State.GenericParameterName, State.GenericMethodParameterName));
                    switch (ctx.State)
                    {
                        case State.GenericParameterName:
                            ParseGenericParameterName(ref ctx);
                            return;
                        case State.GenericMethodParameterName:
                            ParseGenericMethodParameterName(ref ctx);
                            return;
                        default:
                            ctx.State = State.Invalid;
                            return;
                    }
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
                if (ctx.Char.In(']', ',', '*'))
                {
                    ctx.State = State.Invalid;
                    return;
                }

                if (ctx.IsWhiteSpace)
                    return;

                TypeResolver arg;
                if (ctx.Char == '[') // AQN in generic: recursion
                {
                    arg = new TypeResolver(options);
                    ctx.State = State.FullNameOrAqn;
                    arg.Parse(ref ctx);
                    if (ctx.State != State.AfterArgument)
                    {
                        ctx.State = State.Invalid;
                        return;
                    }

                    genericArgs.Add(arg);
                    return;
                }

                // type name in generics: recursion
                ctx.State = State.TypeName;
                arg = new TypeResolver(options);
                if (ctx.Char == '!')
                    ctx.Push(State.GenericParameterName);
                else
                    ctx.AppendChar();

                arg.Parse(ref ctx);
                if (!ctx.State.In(State.Modifiers, State.BeforeArgument))
                {
                    ctx.State = State.Invalid;
                    return;
                }

                genericArgs.Add(arg);
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
                    if (rootName == null)
                        rootName = ctx.GetBuf();
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
                    if (rootName == null)
                        rootName = ctx.GetBuf();
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

                if (ctx.Char.In('*', '&')) // pointer/ByRef
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
                Debug.Assert(ctx.State.In(State.FullNameOrAqn, State.TypeName));
                var def = new TypeResolver(options);
                def.Parse(ref ctx);
                if (!ctx.State.In(State.None, State.AfterArgument, State.BeforeArgument, State.Modifiers))
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

                    case State.Invalid:
                        return;

                    default:
                        throw new InvalidOperationException(Res.InternalError($"Unexpected state: {context.State}"));
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
                    rootName = context.GetBuf();
                    context.Pop();
                    break;

                case State.AssemblyName:
                    assemblyName = context.GetBuf();
                    context.Pop();
                    break;

                case State.Modifiers:
                    context.Pop(); // Modifiers
                    context.Pop(); // FullName/TypeName
                    break;

                default:
                    return;
            }
        }

        private string GetName(TypeNameKind kind)
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
                => sb.Append(kind == TypeNameKind.ShortName ? rootName.Split('.', '+').LastOrDefault() ?? String.Empty : rootName);

            void DumpGenericArguments(StringBuilder sb, TypeNameKind kind)
            {
                if (genericArgs.Count <= 0)
                    return;

                sb.Append('[');
                for (int i = 0; i < genericArgs.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    TypeResolver arg = genericArgs[i];
                    bool aqn = kind == TypeNameKind.ForcedAssemblyQualifiedName
                        || kind.In(TypeNameKind.AssemblyQualifiedName, removeAssemblyVersions)
                            && !(arg.assemblyName ?? arg.declaringType?.assemblyName).In(null, Reflector.SystemCoreLibrariesAssemblyName);
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
                        || kind.In(TypeNameKind.AssemblyQualifiedName, removeAssemblyVersions) && assemblyName != Reflector.SystemCoreLibrariesAssemblyName))
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

            #endregion

            // Generic parameter indicator
            DumpGenericParameterIndicator(result, typeNameKind);

            // Root name
            DumpRootName(result, typeNameKind);

            // Generic arguments
            DumpGenericArguments(result, typeNameKind);

            // Modifiers (array ranks, pointers, ByRef)
            DumpModifiers(result);

            // Generic parameter identification
            DumpGenericParameter(result, typeNameKind);

            // Assembly name
            DumpAssemblyName(result, typeNameKind);
        }

        private Type Resolve(Func<AssemblyName, string, Type> typeResolver)
        {
            if (type != null)
                return type;

            // RootName is null if parsing was unsuccessful.
            if (rootName == null)
                return null;

            // 1. Resolving root type
            Type result = ResolveRootType(typeResolver);
            if (result == null)
                return null;

            // 2. Applying generic arguments
            if (genericArgs.Count > 0)
            {
                Debug.Assert(result.IsGenericTypeDefinition, "Root type is expected to be a generic type definition");
                Type[] args = new Type[genericArgs.Count];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = genericArgs[i].Resolve(typeResolver);
                    if (args[i] == null)
                        return null;
                }

                result = result.GetGenericType(args);
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

        private Type ResolveRootType(Func<AssemblyName, string, Type> typeResolver)
        {
            if (declaringType != null)
                return ResolveGenericParameter(typeResolver);

            bool throwError = (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None;
            bool allowIgnoreAssembly = (options & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None;
            bool ignoreCase = (options & ResolveTypeOptions.IgnoreCase) != ResolveTypeOptions.None;
            Type result;

            // 1.) By resolver
            if (typeResolver != null)
            {
                AssemblyName asmName = null;
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
                        throw new ArgumentException(Res.ReflectionInvalidAssemblyName(assemblyName), e);

                    // In this case we don't use fallback logic because we couldn't call the delegate.
                    return null;
                }

                result = typeResolver.Invoke(asmName, rootName);
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
                result = assembly.GetType(rootName, throwError && !allowIgnoreAssembly, ignoreCase);
                if (result != null || !allowIgnoreAssembly)
                    return result;
            }
#if !NETFRAMEWORK // 3/b. If there is no assembly defined we try to use the mscorlib.dll in the first place, which contains forwarded types on non-framework platforms.
            else if (assemblyName == null)
            {
                // We are not throwing an exception from here because on failure we try all assemblies
                result = typeResolver?.Invoke(null, rootName) ?? MscorlibAssembly?.GetType(rootName, false, ignoreCase);
                if (result != null)
                    return result;
            }
#endif

            // 3/c. Resolving the type from any assembly
            // Type.GetType is not redundant even if we tried mscorlib.dll above because it still can load core library types.
            result = Type.GetType(rootName, false, ignoreCase);
            if (result != null)
                return result;

            // Looking for the type in the loaded assemblies
            foreach (Assembly asm in Reflector.GetLoadedAssemblies())
            {
                result = asm.GetType(rootName, false, ignoreCase);
                if (result != null)
                    return result;
            }

            return throwError ? throw new ReflectionException(Res.ReflectionNotAType(rootName)) : default(Type);
        }

        private Type ResolveGenericParameter(Func<AssemblyName, string, Type> typeResolver)
        {
            // Declaring Type
            Type t = declaringType.Resolve(typeResolver);
            if (t == null)
                return null;

            // Generic type argument
            if (declaringMethod == null)
                return t.GetGenericArguments().FirstOrDefault(a => a.Name == rootName);

            // Generic method argument
            return t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.ToString() == declaringMethod)?.GetGenericArguments().FirstOrDefault(a => a.Name == rootName);
        }

        #endregion

        #endregion

        #endregion
    }
}
