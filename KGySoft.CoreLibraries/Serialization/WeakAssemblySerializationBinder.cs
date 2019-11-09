#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: WeakAssemblySerializationBinder.cs
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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    /// <summary>
    /// Provides a <see cref="SerializationBinder"/> instance for <see cref="IFormatter"/> implementations that can ignore version and token information
    /// of stored assembly name. This makes possible to deserialize objects stored in different version of the original assembly.
    /// <br/>See the also the <strong>Remarks</strong> section of the <see cref="ForwardedTypesSerializationBinder"/> for details and some examples.
    /// </summary>
    /// <seealso cref="ForwardedTypesSerializationBinder"/>
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
        /// <note>In .NET 3.5 <see cref="BinaryFormatter"/> and most <see cref="IFormatter"/> implementations ignore the
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
        /// <note>In .NET 3.5 this method does not exist in the base <see cref="SerializationBinder"/> and is called only if the consumer
        /// serializer handles the <see cref="ISerializationBinder"/> interface or calls it directly.</note>
        /// </remarks>
#if !NET35
        override
#endif
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (serializedType == null)
                throw new ArgumentNullException(nameof(serializedType), Res.ArgumentNull);
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
            Assembly assembly = GetAssembly(assemblyName);
            var options = ResolveTypeOptions.TryToLoadAssemblies | ResolveTypeOptions.AllowPartialAssemblyMatch;
            if (IgnoreAssemblyNameOnResolve)
                options |= ResolveTypeOptions.AllowIgnoreAssemblyName;
            Type result = assembly == null ? Reflector.ResolveType(typeName, options) : Reflector.ResolveType(assembly, typeName, options);

            if (result == null)
                throw new SerializationException(Res.BinarySerializationCannotResolveTypeInAssembly(typeName, String.IsNullOrEmpty(assemblyName) ? Res.Undefined : assemblyName));

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Resolves an assembly by string
        /// </summary>
        private Assembly GetAssembly(string name)
        {
            if (String.IsNullOrEmpty(name) || name == omittedAssemblyName)
                return null;

            var options = ResolveAssemblyOptions.TryToLoadAssembly | ResolveAssemblyOptions.AllowPartialMatch;
            if (!IgnoreAssemblyNameOnResolve)
                options |= ResolveAssemblyOptions.ThrowError;
            return AssemblyResolver.ResolveAssembly(name, options);
        }

        #endregion
       
        #endregion
    }
}
