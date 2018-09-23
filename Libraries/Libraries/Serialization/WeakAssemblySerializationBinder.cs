using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using KGySoft.Reflection;

namespace KGySoft.Libraries.Serialization
{
    /// <summary>
    /// Provides a <see cref="SerializationBinder"/> instance for <see cref="IFormatter"/> implementations that can version and token information
    /// of stored assembly name. This makes possible to deserialize objects stored in different version of the original assembly.
    /// </summary>
    public sealed class WeakAssemblySerializationBinder : SerializationBinder
    {
#if NET40 || NET45
        private const string omittedAssemblyName = "*";

        /// <summary>
        /// Gets or sets whether assembly name should be completely omitted on serialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to omit assembly name on serialize; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <note>This property exists only in .NET 4 and above.</note>
        /// <note>The value of this property is used only serialization; however, it affects deserialization as well:
        /// when assembly name is omitted, deserialization will find the first matching type from any assembly.</note>
        /// <para>When using <see cref="BinarySerializationFormatter"/>, <see cref="BinarySerializationOptions.OmitAssemblyQualifiedNames"/>
        /// option can be used as well, which is available in every .NET version. However, with this property assembly names
        /// can be omitted even when using <see cref="BinaryFormatter"/> or other <see cref="IFormatter"/> implementations.</para>
        /// <para>When the value of this property is <see langword="true"/>, the serialized stream will be shorter; however,
        /// deserialization might be slower, and type will be searched only in already loaded assemblies. When multiple
        /// assemblies have the same type name, the retrieved type cannot determined.</para>
        /// </remarks>
        public bool OmitAssemblyNameOnSerialize { get; set; }

        /// <summary>
        /// When <see cref="OmitAssemblyNameOnSerialize"/> is <see langword="true"/>, suppresses the assembly name on serialization.
        /// Otherwise, returns <see langword="null"/> for both assembly and type names, indicating, that the original
        /// names should be used.
        /// </summary>
        /// <param name="serializedType">The type of the object the formatter creates a new instance of.</param>
        /// <param name="assemblyName">Specifies the <see cref="Assembly"/> name of the serialized object. </param>
        /// <param name="typeName">Specifies the <see cref="Type"/> name of the serialized object. </param>
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            base.BindToName(serializedType, out assemblyName, out typeName);
            if (OmitAssemblyNameOnSerialize)
            {
                // mscorlib is handled natively so is not omitted
                // when assembly is omitted, a non-empty string should be returned so returning a symbol, which is not a valid name
                if (serializedType.Assembly != Reflector.mscorlibAssembly)
                    assemblyName = omittedAssemblyName;

                // generic type arguments contains assembly info as well so stripping name for generics
                if (serializedType.IsGenericType && !serializedType.IsGenericTypeDefinition)
                    typeName = serializedType.ToString();
            }
        }

#elif !NET35
#error .NET version is not set or not supported!
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
                throw new SerializationException(Res.Get(Res.CannotResolveTypeInAssembly, typeName, String.IsNullOrEmpty(assemblyName) ? Res.Get(Res.Undefined) : assemblyName));
            
            return result;
        }

        /// <summary>
        /// Resolves an assembly by string
        /// </summary>
        private static Assembly GetAssembly(string name)
        {
#if NET35
            if (String.IsNullOrEmpty(name))
#elif NET40 || NET45
            if (String.IsNullOrEmpty(name) || name == omittedAssemblyName)
#else
#error .NET version is not set or not supported!
#endif
                return null;

            Assembly result;
            try
            {
                result = Reflector.ResolveAssembly(name, true, true);
            }
            catch (Exception e)
            {
                throw new SerializationException(Res.Get(Res.CannotLoadAssembly, name), e);
            }

            if (result == null)
            {
                throw new SerializationException(Res.Get(Res.CannotLoadAssembly, name));
            }

            return result;
        }
    }
}
