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
            /// Invalid state.
            /// </summary>
            Invalid
        }

        #endregion

        #region StateStack class

        private sealed class StateStack : Stack<State>
        {
            #region Properties

            public State Top
            {
                get => Count == 0 ? State.None : Peek();
                set
                {
                    Pop();
                    Push(value);
                }
            }

            #endregion

            #region Methods

            public override string ToString() => Top.ToString();

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

        #endregion

        #region Instance Fields

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
            => typeCacheByAssembly ??= new Cache<Assembly, LockingDictionary<string, Type>>(a => new Cache<string, Type>(64).AsThreadSafe()).GetThreadSafeAccessor(true); // true because the inner creation is fast

        private static IThreadSafeCacheAccessor<Type, LockingDictionary<TypeNameKind, string>> TypeNameCache
            => typeNameCache ??= new Cache<Type, LockingDictionary<TypeNameKind, string>>(t => new Dictionary<TypeNameKind, string>(1, ComparerHelper<TypeNameKind>.EqualityComparer).AsThreadSafe()).GetThreadSafeAccessor(true); // true because the inner creation is fast

        #endregion

        #region Constructors

        private TypeResolver()
        {
        }

        private TypeResolver(string typeName, bool throwError)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName), Res.ArgumentNull);

            Initialize(typeName, throwError);
        }

        private TypeResolver(Type type) => Initialize(type ?? throw new ArgumentNullException(nameof(type), Res.ArgumentNull));

        private TypeResolver(Assembly assembly, string typeName, bool throwError)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName), Res.ArgumentNull);

            this.assembly = assembly;
            assemblyName = assembly.FullName;
            Initialize(typeName, throwError);
        }

        #endregion

        #region Methods

        #region Static Methods

        internal static Type ResolveType(string typeName, bool throwError, bool loadPartiallyDefinedAssemblies, bool matchAssemblyByWeakName)
        {
            if (String.IsNullOrEmpty(typeName))
                return null;

            if (TypeCacheByString.TryGetValue(typeName, out Type result))
                return result;

            try
            {
                result = new TypeResolver(typeName, throwError).Resolve(throwError, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if (throwError)
                    throw new ReflectionException(Res.ReflectionNotAType(typeName), e);
                return null;
            }

            if (result == null && throwError)
                throw new ReflectionException(Res.ReflectionNotAType(typeName));

            if (result != null)
                typeCacheByString[typeName] = result;

            return result;
        }

        internal static Type ResolveType(Assembly assembly, string typeName, bool throwError, bool loadPartiallyDefinedAssemblies, bool matchAssemblyByWeakName)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly), Res.ArgumentNull);
            if (String.IsNullOrEmpty(typeName))
                return null;

            LockingDictionary<string, Type> cache = TypeCacheByAssembly[assembly];
            if (cache.TryGetValue(typeName, out var result))
                return result;

            int compoundNameEnd = typeName.LastIndexOf(']');
            int asmNamePos = typeName.IndexOf(',', compoundNameEnd + 1);
            if (asmNamePos >= 0)
                throw new ArgumentException(Res.ReflectionTypeWithAssemblyName, nameof(typeName));

            try
            {
                result = assembly.GetType(typeName) ?? new TypeResolver(assembly, typeName, throwError).Resolve(throwError, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if (throwError)
                    throw new ReflectionException(Res.ReflectionNotAType(typeName), e);
                return null;
            }

            if (result == null && throwError)
                throw new ReflectionException(Res.ReflectionNotAType(typeName));

            if (result != null)
                cache[typeName] = result;
            return result;
        }

        internal static string GetName(Type type, TypeNameKind kind)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (!Enum<TypeNameKind>.IsDefined(kind))
                throw new ArgumentOutOfRangeException(nameof(kind), Res.EnumOutOfRange(kind));

            LockingDictionary<TypeNameKind, string> cache = TypeNameCache[type];
            if (cache.TryGetValue(kind, out string result))
                return result;

            result = new TypeResolver(type).GetName(kind);

            cache[kind] = result;
            return result;
        }

        internal static string StripName(string typeName, bool stripVersionOnly)
            => new TypeResolver(typeName, false).GetName(stripVersionOnly ? removeAssemblyVersions : TypeNameKind.FullName) ?? typeName;

        #endregion

        #region Instance Methods

        #region Public Methods

        public override string ToString() => GetName(TypeNameKind.AssemblyQualifiedNameForced) ?? base.ToString();

        #endregion

        #region Private Methods

        private void Initialize(string typeName, bool throwError)
        {
            using (var sr = new StringReader(typeName))
            {
                var stack = new StateStack();
                stack.Push(State.FullNameOrAqn);
                Parse(new StringBuilder(typeName.Length), sr, stack);
                if (sr.Peek() == -1 && stack.Count <= 0)
                    return;
                if (throwError)
                    throw new ArgumentException(Res.ArgumentInvalidString, nameof(typeName));
                rootName = null;
                assemblyName = null;
                assembly = null;
                modifiers.Clear();
                genericArgs.Clear();
                declaringType = null;
                declaringMethod = null;
            }
        }

        private void Initialize(Type t)
        {
            type = t;
            assembly = t.Assembly;
            assemblyName = assembly.FullName;

            // modifiers
            // ReSharper disable once PossibleNullReferenceException
            while (t.HasElementType)
            {
                if (t.IsArray)
                    modifiers.AddFirst(t.IsZeroBasedArray() ? 0 : t.GetArrayRank());
                else if (t.IsByRef)
                    modifiers.AddFirst(byRef);
                else if (t.IsPointer)
                    modifiers.AddFirst(pointer);
                t = t.GetElementType();
            }

            // generic arguments
            if (t.IsGenericType && !t.IsGenericTypeDefinition) // same as: type.IsConstructedGenericType from .NET4
            {
                foreach (Type genericArgument in t.GetGenericArguments())
                    genericArgs.Add(new TypeResolver(genericArgument));

                t = t.GetGenericTypeDefinition();
            }

            // root type
            bool isGenericParam = t.IsGenericParameter;
            rootName = isGenericParam ? t.Name : t.FullName;
            if (!isGenericParam)
                return;

            // generic parameter
            declaringType = new TypeResolver(t.DeclaringType);
            declaringMethod = t.DeclaringMethod?.ToString();
        }

        private void Parse(StringBuilder buf, StringReader sr, StateStack state)
        {
            int chr;
            int rank = 0;
            while ((chr = sr.Read()) != -1)
            {
                var c = (char)chr;
                switch (state.Top)
                {
                    case State.FullNameOrAqn:
                        if (c == ',') // assembly separator
                        {
                            rootName = buf.ToString().Trim();
                            buf.Clear();
                            state.Top = State.AssemblyName;
                            break;
                        }

                        if (c == ']') // end of current argument, returning from recursion
                        {
                            rootName = buf.ToString().Trim();
                            state.Top = State.AfterArgument;
                            return;
                        }

                        goto case State.TypeName;

                    case State.TypeName:
                        if (c == ',') // Type name separator in generic: returning from recursion
                        {
                            rootName = buf.ToString().Trim();
                            state.Top = State.BeforeArgument;
                            return;
                        }

                        if (c == '[') // array or generic type arguments
                        {
                            Debug.Assert(rootName == null);
                            rootName = buf.ToString().Trim();
                            state.Push(State.ArrayOrGeneric);
                            break;
                        }

                        if (c == ']') // end of generics, returning from recursion
                        {
                            rootName = buf.ToString().Trim();
                            state.Top = State.Modifiers;
                            return;
                        }

                        //// - common part with FullNameOrAqn

                        if (c.In('*', '&'))
                        {
                            rootName = buf.ToString().Trim();
                            modifiers.Add(c == '*' ? pointer : byRef);
                            state.Push(State.Modifiers);
                            break;
                        }

                        if (c == '!' && buf.Length == 0) // generic parameter
                        {
                            state.Push(State.GenericParameterName);
                            break;
                        }

                        if (buf.Length == 0 && Char.IsWhiteSpace(c))
                            break;

                        buf.Append(c);
                        break;

                    case State.AssemblyName:
                        if (c == ']') // end of current argument, returning from recursion
                        {
                            assemblyName = buf.ToString().Trim();
                            state.Top = State.AfterArgument;
                            return;
                        }

                        buf.Append(c);
                        break;

                    case State.ArrayOrGeneric:
                        if (c == ']') // Zero-based 1D array
                        {
                            modifiers.Add(0);
                            state.Top = State.Modifiers;
                            break;
                        }

                        if (c == ',') // array rank
                        {
                            state.Top = State.Array;
                            rank = 2;
                            break;
                        }

                        if (c == '*') // nonzero-based array
                        {
                            state.Top = State.Array;
                            rank = 1;
                            break;
                        }

                        goto case State.BeforeArgument;

                    case State.Modifiers:
                        if (c == ',') // type or assembly separator
                        {
                            state.Pop();

                            // AQN: switching to assembly name part
                            if (state.Top == State.FullNameOrAqn)
                            {
                                buf.Clear();
                                state.Top = State.AssemblyName;
                                break;
                            }

                            // Type name separator in generic: returning from recursion
                            if (state.Top == State.TypeName)
                            {
                                state.Top = State.BeforeArgument;
                                return;
                            }

                            goto default; // unexpected state
                        }

                        if (c == '[') // array
                        {
                            rank = 0;
                            state.Top = State.Array;
                            break;
                        }

                        if (c == ']') // end of generic type: returning from recursion
                        {
                            state.Pop();
                            state.Top = state.Top == State.FullNameOrAqn ? State.AfterArgument
                                : state.Top == State.TypeName ? State.Modifiers
                                : State.Invalid;
                            return;
                        }

                        if (c == '*') // pointer
                        {
                            modifiers.Add(pointer);
                            break;
                        }

                        if (c == '*') // pointer
                        {
                            modifiers.Add(pointer);
                            break;
                        }

                        if (c == '&') // pointer
                        {
                            modifiers.Add(byRef);
                            break;
                        }

                        if (c == ':') // generic parameter identifier
                        {
                            state.Pop();
                            Debug.Assert(state.Top.In(State.GenericParameterName, State.GenericMethodParameterName));
                            if (state.Top == State.GenericParameterName)
                                goto case State.GenericParameterName;
                            if (state.Top == State.GenericMethodParameterName)
                                goto case State.GenericMethodParameterName;
                            state.Top = State.Invalid;
                            return;
                        }

                        if (Char.IsWhiteSpace(c))
                            break;

                        state.Top = State.Invalid;
                        return;

                    case State.Array:
                        if (c == ']') // End of array
                        {
                            modifiers.Add(rank);
                            state.Top = State.Modifiers;
                            break;
                        }

                        if (c == ',') // array rank
                        {
                            rank = rank < 2 ? 2 : rank + 1;
                            break;
                        }

                        if (c == '*') // nonzero-based array
                        {
                            if (rank < 1)
                            {
                                rank = 1;
                                break;
                            }

                            state.Top = State.Invalid;
                            return;
                        }

                        if (Char.IsWhiteSpace(c))
                            break;

                        state.Top = State.Invalid;
                        return;

                    case State.BeforeArgument:
                        if (c.In(']', ',', '*'))
                        {
                            state.Top = State.Invalid;
                            return;
                        }

                        if (Char.IsWhiteSpace(c))
                            break;

                        TypeResolver arg;
                        if (c == '[') // AQN in generic: recursion
                        {
                            arg = new TypeResolver();
                            state.Top = State.FullNameOrAqn;
                            arg.Parse(new StringBuilder(), sr, state);
                            if (state.Top != State.AfterArgument)
                            {
                                state.Top = State.Invalid;
                                return;
                            }

                            genericArgs.Add(arg);
                            break;
                        }

                        // type name in generics: recursion
                        state.Top = State.TypeName;
                        arg = new TypeResolver();
                        var sb = new StringBuilder();
                        if (c == '!')
                            state.Push(State.GenericParameterName);
                        else
                            sb.Append(c);

                        arg.Parse(sb, sr, state);
                        if (!state.Top.In(State.Modifiers, State.BeforeArgument))
                        {
                            state.Top = State.Invalid;
                            return;
                        }

                        genericArgs.Add(arg);
                        break;

                    case State.AfterArgument:
                        if (c == ',') // next argument
                        {
                            state.Top = State.BeforeArgument;
                            break;
                        }

                        if (c == ']') // end of generic arguments
                        {
                            state.Top = State.Modifiers;
                            break;
                        }

                        if (Char.IsWhiteSpace(c))
                            break;

                        state.Top = State.Invalid;
                        return;

                    case State.GenericParameterName:
                        if (c == '!' && buf.Length == 0)
                        {
                            state.Top = State.GenericMethodParameterName;
                            break;
                        }

                        if (c == ':') // generic parameter declaring type
                        {
                            rootName = buf.ToString().Trim();

                            //// - below same as in MethodSignature
                            state.Pop();
                            Debug.Assert(state.Top.In(State.FullNameOrAqn, State.TypeName));
                            var def = new TypeResolver();
                            def.Parse(new StringBuilder(), sr, state);
                            if (!state.Top.In(State.None, State.AfterArgument, State.BeforeArgument, State.Modifiers))
                            {
                                state.Top = State.Invalid;
                                return;
                            }

                            declaringType = def;
                            assemblyName = def.assemblyName;
                            return;
                        }

                        //// - below same as in GenericMethodParameterName

                        if (c == '[')
                        {
                            rootName = buf.ToString().Trim();
                            state.Push(State.Array);
                            break;
                        }

                        if (c.In('*', '&'))
                        {
                            rootName = buf.ToString().Trim();
                            modifiers.Add(c == '*' ? pointer : byRef);
                            state.Push(State.Modifiers);
                            break;
                        }

                        buf.Append(c);
                        break;

                    case State.GenericMethodParameterName:
                        if (c == '!' && buf.Length == 0)
                        {
                            state.Top = State.Invalid;
                            break;
                        }

                        if (c == ':') // generic method signature
                        {
                            rootName = buf.ToString().Trim();
                            buf.Clear();
                            state.Top = State.MethodSignature;
                            break;
                        }

                        goto case State.GenericParameterName;

                    case State.MethodSignature:
                        if (c == ':')
                        {
                            declaringMethod = buf.ToString().Trim();

                            //// - below same as in GenericParameterName
                            state.Pop();
                            Debug.Assert(state.Top.In(State.FullNameOrAqn, State.TypeName));
                            var def = new TypeResolver();
                            def.Parse(new StringBuilder(), sr, state);
                            if (!state.Top.In(State.None, State.AfterArgument, State.BeforeArgument, State.Modifiers))
                            {
                                state.Top = State.Invalid;
                                return;
                            }

                            declaringType = def;
                            assemblyName = def.assemblyName;
                            return;
                        }

                        buf.Append(c);
                        break;

                    case State.Invalid:
                        return;

                    default:
                        throw new InvalidOperationException(Res.InternalError($"Unexpected state: {state.Top}"));
                }
            }

            // finishing initialization
            switch (state.Top)
            {
                // simple type without assembly name
                case State.FullNameOrAqn:
                case State.TypeName:
                    rootName = buf.ToString().Trim();
                    state.Pop();
                    break;

                case State.AssemblyName:
                    assemblyName = buf.ToString().Trim();
                    state.Pop();
                    break;

                case State.Modifiers:
                    state.Pop(); // Modifiers
                    state.Pop(); // FullName/TypeName
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

        private void DumpName(StringBuilder sb, TypeNameKind kind)
        {
            // Generic parameter indicator
            if (kind != TypeNameKind.Name && declaringType != null)
            {
                sb.Append('!');
                if (declaringMethod != null)
                    sb.Append('!');
            }

            // Base name
            sb.Append(kind == TypeNameKind.Name ? rootName.Split('.', '+').LastOrDefault() ?? String.Empty : rootName);

            // Generic arguments
            if (genericArgs.Count > 0)
            {
                sb.Append('[');
                for (int i = 0; i < genericArgs.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    TypeResolver arg = genericArgs[i];
                    bool aqn = kind == TypeNameKind.AssemblyQualifiedNameForced
                        || kind.In(TypeNameKind.AssemblyQualifiedName, removeAssemblyVersions) && !arg.assemblyName.In(null, Reflector.SystemCoreLibrariesAssemblyName);
                    if (aqn)
                        sb.Append('[');
                    arg.DumpName(sb, kind);
                    if (aqn)
                        sb.Append(']');
                }

                sb.Append(']');
            }

            // Modifiers (array ranks, pointers, ByRef)
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

            // Generic parameter identification
            if (kind != TypeNameKind.Name && declaringType != null)
            {
                if (declaringMethod != null)
                {
                    sb.Append(':');
                    sb.Append(declaringMethod);
                }

                sb.Append(':');
                declaringType.DumpName(sb, kind);
                return; // skipping assembly name because it is dumped with declaring type
            }

            // Assembly name
            if (!(kind == TypeNameKind.AssemblyQualifiedNameForced
                || kind.In(TypeNameKind.AssemblyQualifiedName, removeAssemblyVersions) && !assemblyName.In(null, Reflector.SystemCoreLibrariesAssemblyName)))
                return;

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

        private Type Resolve(bool throwError, bool loadPartiallyDefinedAssemblies, bool matchAssemblyByWeakName)
        {
            if (type != null)
                return type;

            // RootName is null if parsing was unsuccessful.
            if (rootName == null)
                return null;

            // 1. Resolving assembly if needed
            if (assembly == null && assemblyName != null)
            {
                assembly = AssemblyResolver.ResolveAssembly(assemblyName, throwError, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
                if (assembly == null)
                    return null;
            }

            // 2. Resolving root type
            Type result = ResolveRootType(throwError, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
            if (result == null)
                return null;

            // 3. Applying generic arguments
            if (genericArgs.Count > 0)
            {
                Debug.Assert(result.IsGenericTypeDefinition, "Root type is expected to be a generic type definition");
                Type[] args = new Type[genericArgs.Count];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = genericArgs[i].Resolve(throwError, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
                    if (args[i] == null)
                        return null;
                }

                result = result.GetGenericType(args);
            }

            // 4. Applying modifiers
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

        private Type ResolveRootType(bool throwError, bool loadPartiallyDefinedAssemblies, bool matchAssemblyByWeakName)
        {
            // Regular type
            if (declaringType == null)
            {
                if (assembly != null)
                    return assembly.GetType(rootName, throwError);

                // Not throwing an error from here because we will iterate the loaded assemblies if type cannot be resolved.
                Type result = Type.GetType(rootName);
                if (result != null)
                    return result;

                // Looking for the type in the loaded assemblies
                foreach (Assembly asm in Reflector.GetLoadedAssemblies())
                {
                    result = asm.GetType(rootName);
                    if (result != null)
                        return result;
                }

                return throwError ? throw new ReflectionException(Res.ReflectionNotAType(rootName)) : default(Type);
            }

            // Generic parameter
            Type t = declaringType.Resolve(throwError, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
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
