#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: WeakAssemblySerializationBinder.cs
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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides a <see cref="SerializationBinder"/> instance for <see cref="IFormatter"/> implementations that can ignore version and token information
    /// of stored assembly name. This makes possible to deserialize objects stored in different version of the original assembly.
    /// It also can make any <see cref="IFormatter"/> safe in terms of prohibiting loading assemblies during the deserialization if the <see cref="SafeMode"/>
    /// property is <see langword="true"/>.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <note type="security"><para>If a deserialization stream may come from an untrusted source, then make sure to set the <see cref="SafeMode"/> property
    /// to <see langword="true"/>&#160;to prevent loading assemblies when resolving types.</para>
    /// <para>See the security notes at the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for more details.</para></note>
    /// <note>This binder does not use exact type mapping just tries to resolve type information automatically.
    /// To customize type mapping or use a custom resolve logic you can use the <see cref="ForwardedTypesSerializationBinder"/>
    /// or <see cref="CustomSerializationBinder"/> classes, respectively.</note>
    /// <para>The <see cref="WeakAssemblySerializationBinder"/> class allows resolving type information by weak assembly identity,
    /// or by completely ignoring assembly information (if <see cref="IgnoreAssemblyNameOnResolve"/> property is <see langword="true"/>.)</para>
    /// <para>It also makes possible to prevent loading assembles during deserialization if the <see cref="SafeMode"/> property is <see langword="true"/>.
    /// <note type="tip">You can make even a <see cref="BinaryFormatter"/> safe by assigning a <see cref="WeakAssemblySerializationBinder"/>
    /// with <see cref="SafeMode"/> = <see langword="true"/>&#160;to its <see cref="IFormatter.Binder"/> property so it cannot resolve any type
    /// whose assembly is not already loaded.
    /// </note></para>
    /// <para>If <see cref="WeakAssemblySerializationBinder"/> is used on serialization, then it can omit assembly information from the serialization stream
    /// if the <see cref="OmitAssemblyNameOnSerialize"/> property is <see langword="true"/>.</para>
    /// </remarks>
    /// <seealso cref="ForwardedTypesSerializationBinder"/>
    /// <seealso cref="CustomSerializationBinder"/>
    /// <seealso cref="BinarySerializationFormatter"/>
    public sealed class WeakAssemblySerializationBinder : SerializationBinder, ISerializationBinder
    {
        #region Constants

        private const string omittedAssemblyName = "*";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether assembly name should be completely omitted on serialization.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/>&#160;to omit assembly name on serialize; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <note>In .NET Framework 3.5 <see cref="BinaryFormatter"/> and most <see cref="IFormatter"/> implementations ignore the
        /// value of this property. <see cref="BinarySerializationFormatter"/> is able to use the <see cref="WeakAssemblySerializationBinder"/>
        /// as an <see cref="ISerializationBinder"/> implementation and consider the value of this property even in .NET 3.5.</note>
        /// <note>The value of this property is used only on serialization; however, it affects deserialization as well:
        /// when assembly name is omitted, deserialization will find the first matching type from any assembly.</note>
        /// <para>Using <see cref="BinarySerializationFormatter"/> with <see cref="BinarySerializationOptions.OmitAssemblyQualifiedNames"/>
        /// option enabled has a similar effect. However, using <see cref="WeakAssemblySerializationBinder"/> with this property set to <see langword="true"/>, assembly names
        /// can be omitted even when using <see cref="BinaryFormatter"/> or other <see cref="IFormatter"/> implementations.</para>
        /// <para>When the value of this property is <see langword="true"/>, the serialized stream will be shorter; however,
        /// deserialization might be slower, and type will be searched only in already loaded assemblies. When multiple
        /// assemblies have types with the same name the retrieved type cannot determined.</para>
        /// </remarks>
        public bool OmitAssemblyNameOnSerialize { get; set; }

        /// <summary>
        /// Gets or sets whether an existing assembly name is allowed to be completely ignored on deserialization.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>If the value of this property is <see langword="true"/>&#160;and the type cannot be resolved from an assembly on deserialization,
        /// then the type is tried to be resolved from any loaded assemblies. The effect is similar as if the
        /// <see cref="OmitAssemblyNameOnSerialize"/> property was used on serialization, except that the type is tried to be resolved
        /// from the provided assembly in the first place.</para>
        /// </remarks>
        public bool IgnoreAssemblyNameOnResolve { get; set; } = true;

        /// <summary>
        /// Gets or sets whether loading assemblies is prohibited on deserialization.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>, then no assembly loading will occur on deserialization.</para>
        /// <para>If <see cref="SafeMode"/> is <see langword="false"/>, then <see cref="BindToType">BindToType</see> may load assemblies during the deserialization.</para>
        /// <para>To prevent the consumer <see cref="IFormatter"/> from loading assemblies the <see cref="BindToType">BindToType</see> method never returns <see langword="null"/>;
        /// instead, it throws a <see cref="SerializationException"/> if a type could not be resolved.</para>
        /// <note>See also the security notes at the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for more details.</note>
        /// </remarks>
        /// <seealso cref="BinarySerializationFormatter"/>
        public bool SafeMode { get; set; }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// When <see cref="OmitAssemblyNameOnSerialize"/> is <see langword="true"/>, suppresses the assembly name on serialization.
        /// Otherwise, returns <see langword="null"/>&#160;for both assembly and type names, indicating that the original
        /// names should be used.
        /// </summary>
        /// <param name="serializedType">The type of the object that is being serialized.</param>
        /// <param name="assemblyName">If <see cref="OmitAssemblyNameOnSerialize"/> is <see langword="true"/>, then returns <c>*</c> to indicate an omitted assembly;
        /// otherwise, returns <see langword="null"/>.</param>
        /// <param name="typeName">If <see cref="OmitAssemblyNameOnSerialize"/> is <see langword="true"/>, then returns the full name of the type without assembly information;
        /// otherwise, returns <see langword="null"/>.</param>
        /// <remarks>
        /// <note>In .NET Framework 3.5 this method does not exist in the base <see cref="SerializationBinder"/> and is called only if the consumer
        /// serializer handles the <see cref="ISerializationBinder"/> interface or calls it directly.</note>
        /// </remarks>
#if !NET35
        override
#endif
        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            if (serializedType == null!)
                Throw.ArgumentNullException(Argument.serializedType);

#if NET35
            assemblyName = null;
            typeName = null;
#else
            base.BindToName(serializedType, out assemblyName, out typeName);
#endif
            if (!OmitAssemblyNameOnSerialize)
                return;

            // mscorlib/System.Private.CoreLib/netstandard is handled natively so is not omitted
            // when assembly is omitted, a non-empty string should be returned so returning a symbol, which is not a valid name
            if (!AssemblyResolver.IsCoreLibAssemblyName(serializedType.Assembly.FullName))
                assemblyName = omittedAssemblyName;

            // generic type arguments contains assembly info as well so stripping name for generics
            if (serializedType.IsGenericType && !serializedType.IsGenericTypeDefinition)
                typeName = serializedType.GetName(TypeNameKind.LongName);
        }

        /// <summary>
        /// Retrieves a type by its <paramref name="assemblyName"/> and <paramref name="typeName"/>.
        /// </summary>
        /// <returns>
        /// The type of the resolved object to create.
        /// </returns>
        /// <param name="assemblyName">Specifies the <see cref="Assembly"/> name of the serialized object.</param>
        /// <param name="typeName">Specifies the <see cref="Type"/> name of the serialized object.</param>
        /// <exception cref="SerializationException">The type cannot be resolved or the assembly cannot be loaded.</exception>
        public override Type BindToType(string assemblyName, string typeName)
        {
            Assembly? assembly = GetAssembly(assemblyName);
            var options = IgnoreAssemblyNameOnResolve
                ? ResolveTypeOptions.AllowIgnoreAssemblyName
                : ResolveTypeOptions.AllowPartialAssemblyMatch;
            if (!SafeMode)
                options |= ResolveTypeOptions.TryToLoadAssemblies;
            Type? result = assembly == null ? Reflector.ResolveType(typeName, options) : Reflector.ResolveType(assembly, typeName, options);

            if (result == null)
            {
                string message = String.IsNullOrEmpty(assemblyName)
                    ? Res.BinarySerializationCannotResolveType(typeName)
                    : SafeMode
                        ? Res.BinarySerializationCannotResolveTypeInAssemblySafe(typeName, assemblyName)
                        : Res.BinarySerializationCannotResolveTypeInAssembly(typeName, assemblyName);
                Throw.SerializationException(message);
            }

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Resolves an assembly by string
        /// </summary>
        private Assembly? GetAssembly(string name)
        {
            if (String.IsNullOrEmpty(name) || name == omittedAssemblyName)
                return null;

            var options = ResolveAssemblyOptions.AllowPartialMatch;
            if (!SafeMode)
                options |= ResolveAssemblyOptions.TryToLoadAssembly;
            Assembly? result = AssemblyResolver.ResolveAssembly(name, options);

            // Note: the ThrowError flag would throw a ReflectionException with the same message but BindToType should throw SerializationException
            if (result == null)
            {
                string message = SafeMode
                    ? Res.BinarySerializationCannotResolveAssemblySafe(name)
                    : Res.ReflectionCannotResolveAssembly(name);
                Throw.SerializationException(message);
            }

            return result;
        }

        #endregion

        #endregion
    }
}
