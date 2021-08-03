#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeNameKind.cs
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
using System.Reflection;

using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents name formatting options for the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.GetName">TypeExtensions.GetName</see> methods.
    /// </summary>
    public enum TypeNameKind
    {
        /// <summary>
        /// <para>Represents the short name of a <see cref="Type"/> without namespaces, eg.:
        /// <br/><c>SomeType[String,SomeType]</c></para>
        /// <para>
        /// Differences from <see cref="MemberInfo.Name">Type.Name</see>:
        /// <list type="bullet">
        /// <item><see cref="MemberInfo.Name">Type.Name</see> does not dump the generic type arguments for constructed generic types.</item>
        /// </list>
        /// </para>
        /// </summary>
        ShortName,

        /// <summary>
        /// <para>Represents the long name of a <see cref="Type"/> along with namespaces, eg.:
        /// <br/><c>SomeNamespace.SomeType[System.String,SomeNamespace.SomeType]</c></para>
        /// <para>If this name is unique in the loaded assemblies, then the name can be successfully parsed by
        /// the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method.</para>
        /// <para>
        /// Differences from <see cref="Type.ToString">Type.ToString</see>:
        /// <list type="bullet">
        /// <item><see cref="Type.ToString">Type.ToString</see> dumps generic argument names for generic type definitions.</item>
        /// <item><see cref="Type.ToString">Type.ToString</see> returns <see cref="MemberInfo.Name">Type.Name</see> for generic parameter types.</item>
        /// <item><see cref="Type.ToString">Type.ToString</see> dumps open generic types in a non parseable way.</item>
        /// </list>
        /// </para>
        /// </summary>
        LongName,

        /// <summary>
        /// <para>Represents the full name of a <see cref="Type"/> with assembly qualified names for generic arguments of non-core types, eg.:
        /// <br/><c>SomeNamespace.SomeType[System.String,[SomeNamespace.SomeType, SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]</c></para>
        /// <para>If this name is unique in the loaded assemblies, then the name can be successfully parsed by
        /// the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method.</para>
        /// <para>
        /// Differences from <see cref="Type.FullName">Type.FullName</see>:
        /// <list type="bullet">
        /// <item><see cref="Type.FullName">Type.FullName</see> dumps the assembly names for every generic type argument.</item>
        /// <item><see cref="Type.FullName">Type.FullName</see> returns <see langword="null"/> for generic parameter types.</item>
        /// <item><see cref="Type.FullName">Type.FullName</see> does not dump the generic arguments for constructed open generic types.</item>
        /// </list>
        /// </para>
        /// </summary>
        FullName,

        /// <summary>
        /// <para>Represents the full name of a <see cref="Type"/> with assembly qualified names for all generic arguments, eg.:
        /// <br/><c>SomeNamespace.SomeType[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[SomeNamespace.SomeType, SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]</c></para>
        /// <para>If this name is unique in the loaded assemblies, then the name can be successfully parsed by
        /// the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method.</para>
        /// <para>
        /// Differences from <see cref="Type.FullName">Type.FullName</see>:
        /// <list type="bullet">
        /// <item><see cref="Type.FullName">Type.FullName</see> returns <see langword="null"/> for generic parameter types.</item>
        /// <item><see cref="Type.FullName">Type.FullName</see> does not dump the generic arguments for constructed open generic types.</item>
        /// </list>
        /// </para>
        /// </summary>
        ForcedFullName,

        /// <summary>
        /// <para>Represents the assembly qualified name of a <see cref="Type"/> omitting assembly names for core types, eg.:
        /// <br/><c>SomeNamespace.SomeType[System.String,[SomeNamespace.SomeType, SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null</c></para>
        /// <para>If all needed assemblies are available, then the name can be successfully parsed by
        /// the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method.</para>
        /// <para>If the type does not contain generic parameter types, then the name can be parsed even by
        /// the <see cref="Type.GetType(string)">Type.GetType</see> method.</para>
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
        /// <para>Represents the assembly qualified name of a <see cref="Type"/> forcing assembly names for core types, eg.:
        /// <br/><c>SomeNamespace.SomeType[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[SomeNamespace.SomeType, SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null</c></para>
        /// <para>If all needed assemblies are available, then the name can be successfully parsed by
        /// the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method.</para>
        /// <para>If the type does not contain generic parameter types, then the name can be parsed even by
        /// the <see cref="Type.GetType(string)">Type.GetType</see> method.</para>
        /// <para>
        /// Differences from <see cref="Type.AssemblyQualifiedName">Type.AssemblyQualifiedName</see>:
        /// <list type="bullet">
        /// <item><see cref="Type.AssemblyQualifiedName">Type.AssemblyQualifiedName</see> returns <see langword="null"/> for generic parameter types.</item>
        /// <item><see cref="Type.AssemblyQualifiedName">Type.AssemblyQualifiedName</see> returns the name of the generic type definition only for constructed open generic types.</item>
        /// </list>
        /// </para>
        /// </summary>
        ForcedAssemblyQualifiedName
    }
}
