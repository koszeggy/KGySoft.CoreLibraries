#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResolveTypeOptions.cs
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
    /// Provides options for the <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods.
    /// </summary>
    [Flags]
    public enum ResolveTypeOptions
    {
        /// <summary>
        /// <para>Represents no enabled options.</para>
        /// </summary>
        None,

        /// <summary>
        /// <para>If this flag is enabled, then the <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods
        /// try to load the assemblies optionally present in the type name if they are not loaded yet. Otherwise, the assemblies are tried to be located among the already loaded assemblies.</para>
        /// <para>Default state at <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods: <strong>Enabled</strong></para>
        /// </summary>
        TryToLoadAssemblies = 1,

        /// <summary>
        /// <para>If this flag is enabled, then the <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods
        /// allow to resolve assembly names optionally present in the type name by partial name.
        /// If this flag is disabled, then the provided assembly information must match (which still can be partially specified).</para>
        /// <para>If a type name is specified without an assembly, then this flag is ignored (as if the <see cref="AllowIgnoreAssemblyName"/> was set).</para>
        /// <note>Depending on the type system of the current platform it can happen that new assemblies of unmatching identity are loaded even if this flag is disabled.
        /// In such case the loaded assemblies are ignored.</note>
        /// <note type="tip">Forwarded types can be loaded even if this flag is disabled but only if the old assembly of the exact identity can be resolved.
        /// So for example, to allow a type, which was forwarded from the <c>mscorlib 4.0</c> assembly, to be resolved by an <c>mscorlib 2.0</c> identity,
        /// this flag should be enabled.</note>
        /// <para>Default state at <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods: <strong>Enabled</strong></para>
        /// </summary>
        AllowPartialAssemblyMatch = 1 << 1,

        /// <summary>
        /// <para>If a type is specified with an assembly name, which cannot be resolved at all, then this flag makes possible to
        /// completely ignore the assembly information and that the <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see>
        /// methods can return a type by its full name from any loaded assembly.</para>
        /// <para>If a type name is specified without an assembly, then it will be tried to be resolved from the loaded assemblies as if this flag was enabled.</para>
        /// <para>If this flag is enabled, then the <see cref="AllowPartialAssemblyMatch"/> flag is ignored.</para>
        /// <para>Default state at <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods: <strong>Disabled</strong></para>
        /// </summary>
        AllowIgnoreAssemblyName = 1 << 2,

        /// <summary>
        /// <para>If this flag is enabled, then the type name is identified in a case-insensitive manner.</para>
        /// <note>Assembly name is always resolved in a case-insensitive manner.</note>
        /// <para>Default state at <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreCase = 1 << 3,

        /// <summary>
        /// <para>If this flag is enabled and the type cannot be resolved, then a <see cref="ReflectionException"/>
        /// will be thrown from the <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods.</para>
        /// <para>Default state at <see cref="O:KGySoft.Reflection.Reflector.ResolveType">Reflector.ResolveType</see> methods: <strong>Disabled</strong></para>
        /// </summary>
        ThrowError = 1 << 4,
    }
}
