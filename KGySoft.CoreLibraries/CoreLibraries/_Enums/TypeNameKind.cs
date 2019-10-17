#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeNameKind.cs
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
using System.Reflection;
using System.Runtime.CompilerServices;
using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a name formatting kind for the <see cref="TypeExtensions.GetName">TypeExtensions.GetName</see> method.
    /// </summary>
    public enum TypeNameKind
    {
        /// <summary>
        /// Represents the name of a <see cref="Type"/> without namespace.
        /// <para>
        /// Differences from <see cref="MemberInfo.Name">Type.Name</see>:
        /// <list type="bullet">
        /// <item><see cref="MemberInfo.Name">Type.Name</see> does not dump the generic type arguments for constructed generic types.</item>
        /// </list>
        /// </para>
        /// </summary>
        Name,

        /// <summary>
        /// Represents the full name of a <see cref="Type"/> along with namespace.
        /// <br/>If this name is unique in the loaded assemblies, then the name can be successfully parsed by
        /// the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method.
        /// <para>
        /// Differences from <see cref="Type.FullName">Type.FullName</see>:
        /// <list type="bullet">
        /// <item><see cref="Type.FullName">Type.FullName</see> dumps the assembly names for closed generic type names.</item>
        /// <item><see cref="Type.FullName">Type.FullName</see> returns <see langword="null"/> for generic parameter types.</item>
        /// <item><see cref="Type.FullName">Type.FullName</see> does not dump the generic arguments for constructed open generic types.</item>
        /// </list>
        /// </para>
        /// <para>
        /// Differences from <see cref="Type.ToString">Type.ToString</see>:
        /// <list type="bullet">
        /// <item><see cref="Type.ToString">Type.ToString</see> dumps generic argument names for generic type definitions.</item>
        /// <item><see cref="Type.ToString">Type.ToString</see> returns <see cref="MemberInfo.Name">Type.Name</see> for generic parameter types.</item>
        /// <item><see cref="Type.ToString">Type.ToString</see> dumps open generic types in a non parseable way.</item>
        /// </list>
        /// </para>
        /// </summary>
        FullName,

        /// <summary>
        /// Represents the assembly qualified name of a <see cref="Type"/>. Assembly identity is dumped for non-core types only.
        /// <br/>If all needed assemblies are available, then the name can be successfully parsed by
        /// the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method.
        /// <br/>If the type does not contain generic parameter types, then the name can be parsed even by
        /// the <see cref="Type.GetType(string)">Type.GetType</see> method.
        /// <para>
        /// Differences from <see cref="Type.AssemblyQualifiedName">Type.AssemblyQualifiedName</see>:
        /// <list type="bullet">
        /// <item><see cref="Type.AssemblyQualifiedName">Type.AssemblyQualifiedName</see> dumps assembly names even for core library types.
        /// For a similar result use the <see cref="ForcedAssemblyQualifiedName"/> option. This is not needed for <see cref="Type.GetType(string)">Type.GetType</see> though.</item>
        /// <item><see cref="Type.AssemblyQualifiedName">Type.AssemblyQualifiedName</see> returns <see langword="null"/> for generic parameter types.</item>
        /// <item><see cref="Type.AssemblyQualifiedName">Type.AssemblyQualifiedName</see> returns the name of the generic type definition only for constructed open generic types.</item>
        /// </list>
        /// </para>
        /// </summary>
        AssemblyQualifiedName,

        /// <summary>
        /// Represents the assembly qualified name of a <see cref="Type"/>. Assembly identity is dumped even for core types,
        /// similarly to the <see cref="Type.AssemblyQualifiedName">Type.AssemblyQualifiedName</see> property. Otherwise, the same applies
        /// as for the <see cref="AssemblyQualifiedName"/> option.
        /// </summary>
        ForcedAssemblyQualifiedName,

        /// <summary>
        /// Represents the assembly qualified name of a <see cref="Type"/> with the assembly from which the type has been forwarded.
        /// Assembly identity is dumped for non-core types only.
        /// If <see cref="TypeForwardedFromAttribute"/> is not defined for a type, then the result is the same as in case the <see cref="AssemblyQualifiedName"/>.
        /// </summary>
        AssemblyQualifiedNameLegacyIdentity,

        /// <summary>
        /// Represents the assembly qualified name of a <see cref="Type"/> with the assembly from which the type has been forwarded.
        /// If <see cref="TypeForwardedFromAttribute"/> is not defined for a type, then the result is the same as in case the <see cref="ForcedAssemblyQualifiedName"/>.
        /// </summary>
        ForcedAssemblyQualifiedNameLegacyIdentity,
    }
}
