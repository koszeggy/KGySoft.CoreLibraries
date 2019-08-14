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

using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    /// <summary>
    /// Provides a <see cref="SerializationBinder"/> instance for <see cref="IFormatter"/> implementations that can ignore version and token information
    /// of stored assembly name. This makes possible to deserialize objects stored in different version of the original assembly.
    /// </summary>
    public sealed class WeakAssemblySerializationBinder : SerializationBinder
    {
        #region Constants

#if !NET35
        private const string omittedAssemblyName = "*";
#endif

        #endregion

        #region Properties

#if !NET35
        /// <summary>
        /// Gets or sets whether assembly name should be completely omitted on serialization.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/>&#160;to omit assembly name on serialize; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <note>This property is available only in .NET 4 and above.</note>
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
#endif

        #endregion

        #region Methods

        #region Public Methods

#if !NET35
        /// <summary>
        /// When <see cref="OmitAssemblyNameOnSerialize"/> is <see langword="true"/>, suppresses the assembly name on serialization.
        /// Otherwise, returns <see langword="null"/>&#160;for both assembly and type names, indicating, that the original
        /// names should be used.
        /// </summary>
        /// <param name="serializedType">The type of the object the formatter creates a new instance of.</param>
        /// <param name="assemblyName">Specifies the <see cref="Assembly"/> name of the serialized object.</param>
        /// <param name="typeName">Specifies the <see cref="Type"/> name of the serialized object.</param>
        /// <remarks>
        /// <note>This method is available only in .NET 4 and above.</note>
        /// </remarks>
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (serializedType == null)
                throw new ArgumentNullException(nameof(serializedType), Res.ArgumentNull);
            base.BindToName(serializedType, out assemblyName, out typeName);
            if (OmitAssemblyNameOnSerialize)
            {
                // mscorlib is handled natively so is not omitted
                // when assembly is omitted, a non-empty string should be returned so returning a symbol, which is not a valid name
                if (serializedType.Assembly != Reflector.MsCorlibAssembly)
                    assemblyName = omittedAssemblyName;

                // generic type arguments contains assembly info as well so stripping name for generics
                if (serializedType.IsGenericType && !serializedType.IsGenericTypeDefinition)
                    typeName = serializedType.ToString();
            }
        }
#endif


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
            Type result = (assembly == null ? Reflector.ResolveType(typeName) : Reflector.ResolveType(assembly, typeName))
                // trying to omit assembly info of generic parameters, too
                // may happen if this binder has not been not used for serialization or OmitAssemblyNameOnSerialize was not set
                ?? Reflector.ResolveType(typeName, true, true);

            if (result == null)
                throw new SerializationException(Res.BinarySerializationCannotResolveTypeInAssembly(typeName, String.IsNullOrEmpty(assemblyName) ? Res.Undefined : assemblyName));

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Resolves an assembly by string
        /// </summary>
        private static Assembly GetAssembly(string name)
        {
#if NET35
            if (String.IsNullOrEmpty(name))
#else
            if (String.IsNullOrEmpty(name) || name == omittedAssemblyName)
#endif
                return null;

            Assembly result;
            try
            {
                result = Reflector.ResolveAssembly(name, true, true);
            }
            catch (Exception e)
            {
                throw new SerializationException(Res.ReflectionCannotLoadAssembly(name), e);
            }

            if (result == null)
                throw new SerializationException(Res.ReflectionCannotLoadAssembly(name));

            return result;
        }

        #endregion
       
        #endregion
    }
}
