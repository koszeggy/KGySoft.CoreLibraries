#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResolveAssemblyOptions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Provides options for the <see cref="O:KGySoft.Reflection.Reflector.ResolveAssembly">Reflector.ResolveAssembly</see> methods.
    /// </summary>
    [Flags]
    public enum ResolveAssemblyOptions
    {
        /// <summary>
        /// <para>Represents no enabled options.</para>
        /// </summary>
        None,

        /// <summary>
        /// <para>If this flag is enabled, then the <see cref="O:KGySoft.Reflection.Reflector.ResolveAssembly">Reflector.ResolveAssembly</see> methods
        /// try to load the assembly if it is not loaded yet. Otherwise, the assembly is tried to be located among the already loaded assemblies.</para>
        /// <para>Default state at <see cref="O:KGySoft.Reflection.Reflector.ResolveAssembly">Reflector.ResolveAssembly</see> methods: <strong>Enabled</strong></para>
        /// </summary>
        TryToLoadAssembly = 1,

        /// <summary>
        /// <para>If this flag is enabled, then the <see cref="O:KGySoft.Reflection.Reflector.ResolveAssembly">Reflector.ResolveAssembly</see> methods
        /// allow to return an already loaded assembly of any version, culture and public key token, or, if the assembly
        /// is not loaded, this flag allows to load it by partial identity if possible.
        /// If this flag is disabled, then the provided assembly information must match (which still can be a partial name).</para>
        /// <note>Depending on the type system of the current platform it can happen that a new assembly of an unmatching identity is loaded even if this flag is disabled.
        /// In such case the loaded assembly is not returned though.</note>
        /// <para>Default state at <see cref="O:KGySoft.Reflection.Reflector.ResolveAssembly">Reflector.ResolveAssembly</see> methods: <strong>Enabled</strong></para>
        /// </summary>
        AllowPartialMatch = 1 << 1,

        /// <summary>
        /// <para>If this flag is enabled and the assembly cannot be resolved, then a <see cref="ReflectionException"/>
        /// will be thrown from the <see cref="O:KGySoft.Reflection.Reflector.ResolveAssembly">Reflector.ResolveAssembly</see> methods.</para>
        /// <para>Default state at <see cref="O:KGySoft.Reflection.Reflector.ResolveAssembly">Reflector.ResolveAssembly</see> methods: <strong>Disabled</strong></para>
        /// </summary>
        ThrowError = 1 << 4,
    }
}
