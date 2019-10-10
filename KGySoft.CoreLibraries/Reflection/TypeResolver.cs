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
using System.Text;

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
            /// Invalid state.
            /// </summary>
            Invalid
        }

        private enum TypeNameKind
        {
            /// <summary>
            /// Type name without namespace and generic arguments but with array/pointer/ref modifiers
            /// </summary>
            Name,

            /// <summary>
            /// Full name without assembly information (similar to <see cref="Type.ToString"/>)
            /// </summary>
            FullName,

            /// <summary>
            /// Similar to <see cref="Type.AssemblyQualifiedName"/> but does not contain assembly information for core types.
            /// </summary>
            AssemblyQualifiedName,
        }

        #endregion

        #region StateStack class

        private sealed class StateStack : Stack<State>
        {
            #region Properties

            public State Top
            {
                get => Peek();
                set
                {
                    Pop();
                    Push(value);
                }
            }

            #endregion
        }

        #endregion

        #endregion

        #region Constants

        private const int Pointer = -1;
        private const int ByRef = -2;

        #endregion

        #region Fields

        private readonly List<int> modifiers = new List<int>();
        private readonly List<TypeResolver> genericArgs = new List<TypeResolver>();

        private string name;
        private string fullName;
        private string assemblyQualifiedName;
        private Type type;

        #endregion

        #region Properties

        internal string RootName { get; private set; }

        internal string AssemblyName { get; private set; }

        internal string Name => name ??= GetName(TypeNameKind.Name);

        internal string FullName => fullName ??= GetName(TypeNameKind.FullName);

        /// <summary>
        /// Gets assembly qualified name of the type.
        /// If this instance was initialized by string, then tries to perform a type resolve!
        /// </summary>
        internal string AssemblyQualifiedName
        {
            get
            {
                if (assemblyQualifiedName != null)
                    return assemblyQualifiedName;

                // If the instance was not initialized from Type, then performing the resolve and reinitializing
                if (type == null)
                {
                    Resolve();

                    // could not resolve, returning by the original information (Resolve sets it then)
                    if (type == null)
                        return assemblyQualifiedName;
                }

                // Reinitializing by real type information
                RootName = null;
                AssemblyName = null;
                modifiers.Clear();
                genericArgs.Clear();
                Initialize(type.AssemblyQualifiedName, false);
                return assemblyQualifiedName = GetName(TypeNameKind.AssemblyQualifiedName);
            }
        }

        #endregion

        #region Constructors

        #region Internal Constructors

        internal TypeResolver(string typeName, bool throwError)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName), Res.ArgumentNull);

            Initialize(typeName, throwError);
        }

        #endregion

        #region Private Constructors

        private TypeResolver()
        {
        }

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        public override string ToString() => FullName ?? base.ToString();

        #endregion

        #region Internal Methods

        internal Type Resolve(bool loadPartiallyDefinedAssemblies = false, bool matchAssemblyByWeakName = false)
        {
            if (type != null)
                return type;

            // RootName is null if parsing was unsuccessful. If AQN is not null while type is null, then a resolve already failed earlier.
            if (RootName == null || assemblyQualifiedName != null)
                return null;

            string aqn = GetName(TypeNameKind.AssemblyQualifiedName);

            // TODO: logic from Reflector.ResolveType
            try
            {
                type = Type.GetType(aqn);
            }
            finally
            {
                if (type == null)
                    assemblyQualifiedName = aqn;
            }

            return type;
        }

        #endregion

        #region Private Methods

        private void Initialize(string typeName, bool throwError)
        {
            using (var sr = new StringReader(typeName))
            {
                var stack = new StateStack();
                stack.Push(State.FullNameOrAqn);
                Initialize(new StringBuilder(typeName.Length), sr, stack);
                if (sr.Peek() == -1 && stack.Count <= 0)
                    return;
                if (throwError)
                    throw new ArgumentException(Res.ArgumentInvalidString, nameof(typeName));
                RootName = null;
                AssemblyName = null;
                modifiers.Clear();
                genericArgs.Clear();
            }
        }

        private void Initialize(StringBuilder buf, StringReader sr, StateStack state)
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
                            RootName = buf.ToString().Trim();
                            buf.Clear();
                            state.Top = State.AssemblyName;
                            break;
                        }

                        if (c == ']') // end of current argument, returning from recursion
                        {
                            RootName = buf.ToString().Trim();
                            state.Top = State.AfterArgument;
                            return;
                        }

                        goto case State.TypeName;

                    case State.TypeName:
                        if (c == ',') // Type name separator in generic: returning from recursion
                        {
                            RootName = buf.ToString().Trim();
                            state.Top = State.BeforeArgument;
                            return;
                        }

                        if (c == '[') // array or generic type arguments
                        {
                            Debug.Assert(RootName == null);
                            RootName = buf.ToString().Trim();
                            state.Push(State.ArrayOrGeneric);
                            break;
                        }

                        if (c == ']') // end of generics, returning from recursion
                        {
                            RootName = buf.ToString().Trim();
                            state.Top = State.Modifiers;
                            return;
                        }

                        //// - common part with FullNameOrAqn

                        if (c.In('*', '&'))
                        {
                            RootName = buf.ToString().Trim();
                            modifiers.Add(c == '*' ? Pointer : ByRef);
                            state.Push(State.Modifiers);
                            break;
                        }

                        buf.Append(c);
                        break;

                    case State.AssemblyName:
                        if (c == ']') // end of current argument, returning from recursion
                        {
                            AssemblyName = buf.ToString().Trim();
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
                            modifiers.Add(Pointer);
                            break;
                        }

                        if (c == '*') // pointer
                        {
                            modifiers.Add(Pointer);
                            break;
                        }

                        if (c == '&') // pointer
                        {
                            modifiers.Add(ByRef);
                            break;
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
                                rank = 1;
                            break;
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
                            arg.Initialize(new StringBuilder(), sr, state);
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
                        arg.Initialize(new StringBuilder(new String(c, 1)), sr, state);
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
                    RootName = buf.ToString().Trim();
                    state.Pop();
                    break;

                case State.AssemblyName:
                    AssemblyName = buf.ToString().Trim();
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
            if (RootName == null)
                return null;
            var result = new StringBuilder();
            DumpName(result, kind);
            return result.ToString();
        }

        private void DumpName(StringBuilder sb, TypeNameKind kind)
        {
            // Base name
            sb.Append(kind == TypeNameKind.Name ? RootName.Split('.').LastOrDefault() ?? String.Empty : RootName);

            // Generic arguments
            if (kind != TypeNameKind.Name && genericArgs.Count > 0)
            {
                sb.Append('[');
                for (int i = 0; i < genericArgs.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    TypeResolver arg = genericArgs[i];
                    bool aqn = kind == TypeNameKind.AssemblyQualifiedName && arg.AssemblyName != null && arg.AssemblyName != Reflector.SystemCoreLibrariesAssemblyName;
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
                    case ByRef:
                        sb.Append('&');
                        break;
                    case Pointer:
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

            if (kind != TypeNameKind.AssemblyQualifiedName || AssemblyName == null || AssemblyName == Reflector.SystemCoreLibrariesAssemblyName)
                return;

            // Assembly name
            sb.Append(", ");
            sb.Append(AssemblyName);
        }

        #endregion

        #endregion
    }
}
