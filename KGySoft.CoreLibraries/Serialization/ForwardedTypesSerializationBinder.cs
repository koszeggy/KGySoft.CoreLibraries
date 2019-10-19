#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ForwardedTypesSerializationBinder.cs
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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    /// <summary>
    /// Provides a <see cref="SerializationBinder"/> that makes possible to serialize and deserialize types with custom assembly identity.
    /// <br/>See the <strong>Remarks</strong> section for details and some examples.
    /// </summary>
    /// <remarks>
    /// <para>By default, the <see cref="ForwardedTypesSerializationBinder"/> does nothing. Resolving types from legacy
    /// assemblies work automatically if at least a chunk version of the assembly exists on the current platform with the
    /// appropriate <see cref="TypeForwardedToAttribute"/> attributes.</para>
    /// <para>If <see cref="WriteLegacyIdentity"/> is set to <see langword="true"/>, and no types are added to the binder, then
    /// types, which are decorated by the <see cref="TypeForwardedFromAttribute"/> attribute will be serialized by the old identity.
    /// For deserializing such types this binder is not needed to be set.</para>
    /// <para>To resolve types that are no forwarded by the <see cref="TypeForwardedToAttribute"/> from an existing assembly with the
    /// given identity use this binder for deserialization. Add the types to be handled by the <see cref="AddType">AddType</see> or
    /// <see cref="AddTypes">AddTypes</see> methods.</para>
    /// <para>To serialize types with arbitrary assembly identity, use this binder both for serialization and deserialization.
    /// Add the type to be handled by the <see cref="AddType">AddType</see> method and specify at least one <see cref="AssemblyName"/>
    /// for each added <see cref="Type"/>.</para>
    /// </remarks>
    /// <example>
    /// <para>The following example demonstrates the usage of the <see cref="ForwardedTypesSerializationBinder"/> when types have to be deserialized from a stream,
    /// which have been originally serialized by another version of the assembly.
    /// <code lang="C#"><![CDATA[
    /// var binder = new ForwardedTypesSerializationBinder();
    ///
    /// // MyType will be able to be deserialized if the assembly name in the
    /// // serialization stream matches any of the enlisted ones.
    /// binder.AddType(typeof(MyType),
    ///     new AssemblyName("MyOldAssembly, Version=1.0.0.0"),
    ///     new AssemblyName("MyOldAssembly, Version=1.2.0.0"),
    ///     new AssemblyName("MyNewAssembly, Version=1.5.0.0"),
    ///     new AssemblyName("MyNewAssembly, Version=2.0.0.0"));
    ///
    /// // MyOtherType will be able to be deserialized if it was serialized by any versions of MyAssembly.
    /// binder.AddType(typeof(MyOtherType), new AssemblyName("MyAssembly"));
    ///
    /// // Any type of any assembly will be will be mapped to SomeOtherType if their full names match.
    /// binder.AddType(typeof(SomeOtherType));
    ///
    /// // Multiple types can be enlisted without assembly identity
    /// binder.AddTypes(typeof(MyType), typeof(MyOtherType), typeof(SomeOtherType));
    ///
    /// // Works also with the traditional BinaryFormatter!
    /// IFormatter formatter = new BinarySerializationFormatter { Binder = binder };
    /// object result = formatter.Deserialize(serializationStream);
    /// ]]></code></para>
    /// <note>The <see cref="WeakAssemblySerializationBinder"/> is also able to ignore assembly information but if there are two different
    /// types of the same name in the same namespace, then the behavior of <see cref="WeakAssemblySerializationBinder"/> is not deterministic.</note>
    /// <para>
    /// The following example demonstrates how to control the serialized assembly name.
    /// <code lang="C#"><![CDATA[
    /// // Setting the WriteLegacyIdentity allows to use arbitrary custom indenity on serialization.
    /// var binder = new ForwardedTypesSerializationBinder { WriteLegacyIdentity = true };
    ///
    /// // When serializing a MyType instance, it will be saved with the firstly specified identity
    /// binder.AddType(typeof(MyType),
    ///     new AssemblyName("MyOldAssembly, Version=1.0.0.0"),
    ///     new AssemblyName("MyOldAssembly, Version=1.2.0.0"),
    ///     new AssemblyName("MyNewAssembly, Version=1.5.0.0"),
    ///     new AssemblyName("MyNewAssembly, Version=2.0.0.0"));
    ///
    /// // If WriteLegacyIdentity is true, types with TypeForwardedFromAttribute will be automatically written
    /// // with their old identity stored in the TypeForwardedFromAttribute.AssemblyFullName property.
    /// // List<T> has a TypeForwardedFromAttribute in .NET Core and Standard so it will be serialized with
    /// // mscorlib assembly identity in every platform:
    /// var obj = new List<MyType> { new MyType() };
    /// 
    /// // Works also with KGy SOFT BinarySerializationFormatter!
    /// IFormatter formatter = new BinaryFormatter { Binder = binder };
    /// formatter.Serialize(serializationStream, obj);
    /// ]]></code>
    /// </para>
    /// </example>
    /// <seealso cref="WeakAssemblySerializationBinder"/>
    public sealed class ForwardedTypesSerializationBinder : SerializationBinder, ISerializationBinder
    {
        #region Fields

        /// <summary>
        /// Key: Type.FullName for root types.
        /// Value.Key: Different types of the same full name
        /// Value.Value: AssemblyName.FullName, or empty string, if any assemblies are allowed.
        ///              Not AssemblyNames because they have no overridden Equals.
        /// </summary>
        private readonly Dictionary<string, Dictionary<Type, HashSet<string>>> mapping = new Dictionary<string, Dictionary<Type, HashSet<string>>>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether a legacy assembly identity is tried to be written on serializing.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <note>In .NET 3.5 <see cref="BinaryFormatter"/> and most <see cref="IFormatter"/> implementations ignore the
        /// value of this property. <see cref="BinarySerializationFormatter"/> is able to use the <see cref="ForwardedTypesSerializationBinder"/>
        /// as an <see cref="ISerializationBinder"/> implementation and consider the value of this property even in .NET 3.5.</note>
        /// <para>If the value of this property is <see langword="true"/>, then on serialization a legacy identity is tried to be written for the serialized type.
        /// That is, the first <see cref="AssemblyName"/> that was specified for the type by the <see cref="AddType">AddType</see> method, or
        /// the value of the <see cref="TypeForwardedFromAttribute.AssemblyFullName">TypeForwardedFromAttribute.AssemblyFullName</see> property
        /// if it is specified for the type.</para>
        /// </remarks>
        public bool WriteLegacyIdentity { get; set; }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Adds the <paramref name="type"/> to this binder. If no <paramref name="assemblyIdentities"/> are defined, then any
        /// <see cref="Type"/> with the same full name will be resolved to <paramref name="type"/>; otherwise, only the ones,
        /// whose identities match.
        /// </summary>
        /// <param name="type">The type to be added to the handled types by this binder/</param>
        /// <param name="assemblyIdentities">The legacy assembly identities to be recognized. Can contain also partial names. If empty or contains a <see langword="null"/>
        /// element, then any type of the same full name will be resolved to <paramref name="type"/>.</param>
        /// <remarks>
        /// <para>If no assembly identities are specified, then any <see cref="Type"/> that has the same full name as <paramref name="type"/>,
        /// will be resolved to the specified <paramref name="type"/>.</para>
        /// <para>If at least one assembly identity is specified and <see cref="WriteLegacyIdentity"/> is <see langword="true"/>,
        /// then on serialization the first specified name will be returned as the assembly name of the <paramref name="type"/>.</para>
        /// <para>For generic types you should specify the generic type definition.</para>
        /// </remarks>
        public void AddType(Type type, params AssemblyName[] assemblyIdentities)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            string fullName = type.FullName;
            if (fullName == null || !type.IsRuntimeType() || type.HasElementType
                || type.IsGenericType && !type.IsGenericTypeDefinition
                || type.IsGenericParameter)
                throw new ArgumentException(Res.SerializationRootTypeExpected, nameof(type));
            Debug.Assert(type == type.GetRootType(), "Root type expected");

            // getting/creating the map by type of the same full names
            if (!mapping.TryGetValue(fullName, out Dictionary<Type, HashSet<string>> mapByType))
            {
                mapByType = new Dictionary<Type, HashSet<string>>();
                mapping[fullName] = mapByType;
            }

            // getting/creating the identities set
            if (!mapByType.TryGetValue(type, out var identites))
            {
                identites = new HashSet<string>();
                mapByType[type] = identites;
            }

            // no assembly identities or contains null: any assembly will be accepted
            if (assemblyIdentities.IsNullOrEmpty())
            {
                identites.Add(String.Empty);
                return;
            }

            foreach (AssemblyName assemblyName in assemblyIdentities)
                identites.Add(assemblyName.FullName);
        }

        /// <summary>
        /// Adds the <paramref name="types"/> to this binder without any specific assembly identity. Each <see cref="Type"/>
        /// that have the same full name as one of the given types, will be able to be resolved from any assembly.
        /// </summary>
        /// <param name="types">The types to be added to the handled types by this binder.</param>
        /// <remarks>
        /// <para>To resolve types from specific assemblies only use the <see cref="AddType">AddType</see> method.</para>
        /// <para>If <see cref="WriteLegacyIdentity"/> is <see langword="true"/>, then on serialization a legacy assembly name
        /// will be used only for those types, which are decorated by the <see cref="TypeForwardedFromAttribute"/> attribute.</para>
        /// <para>For generic types it is enough to specify the generic type definition.</para>
        /// </remarks>
        public void AddTypes(params Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types), Res.ArgumentNull);
            if (types.Length == 0)
                throw new ArgumentException(Res.CollectionEmpty, nameof(types));
            if (types.Contains(null))
                throw new ArgumentException(Res.ArgumentContainsNull, nameof(types));
            foreach (Type type in types)
                AddType(type);
        }

        /// <summary>
        /// When <see cref="WriteLegacyIdentity"/> is <see langword="true"/>, then tries to return a legacy assembly identity
        /// for the specified <paramref name="serializedType"/>.
        /// </summary>
        /// <param name="serializedType">The type of the object that is being serialized.</param>
        /// <param name="assemblyName">If <see cref="WriteLegacyIdentity"/> is <see langword="true"/>, then tries to return an old assembly identity for <paramref name="serializedType"/>.
        /// If at least one <see cref="AssemblyName"/> was specified for the type by the <see cref="AddType">AddType</see> method, then the firstly specified name will be used.
        /// Otherwise, if the type has a <see cref="TypeForwardedFromAttribute"/> is specified for the type, its value will be used.
        /// Otherwise, returns <see langword="null"/>&#160;so the formatter will emit a default identity.</param>
        /// <param name="typeName">If <paramref name="assemblyName"/> is not <see langword="null"/>&#160;when this method returns, then contains the full name of the <paramref name="serializedType"/>,<see cref="OmitAssemblyNameOnSerialize"/> is <see langword="true"/>, then returns the full name of the type without assembly information;
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
            #region Local Methods

            AssemblyName GetAssemblyName(Type t)
            {
                Type key = t.GetRootType();

                // we have a non-empty specified assembly name 
                if (mapping.GetValueOrDefault(key.FullName)?.GetValueOrDefault(key).FirstOrDefault(n => !String.IsNullOrEmpty(n)) is string asmName)
                    return new AssemblyName(asmName);

                // TypeForwardedFromAttribute is specified for the type
                if (Attribute.GetCustomAttribute(key, typeof(TypeForwardedFromAttribute), false) is TypeForwardedFromAttribute attr)
                    return new AssemblyName(attr.AssemblyFullName);

                // original name
                return t.Assembly.GetName();
            }

            #endregion

            if (serializedType == null)
                throw new ArgumentNullException(nameof(serializedType), Res.ArgumentNull);
#if NET35
            assemblyName = null;
            typeName = null;
#else
            base.BindToName(serializedType, out assemblyName, out typeName);
#endif
            // We are not interested in serialization
            if (!WriteLegacyIdentity)
                return;

            string boundAsmName = GetAssemblyName(serializedType).FullName;
            string boundTypeName = serializedType.GetName(TypeNameKind.FullName, GetAssemblyName, null);

            if (boundAsmName != serializedType.Assembly.FullName)
                assemblyName = boundAsmName;
            if (boundTypeName != serializedType.FullName)
                typeName = boundTypeName;
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
            #region Local Methods

            Type ResolveType(AssemblyName assemblyName, string typeName)
            {
                // there is no rule for such type name
                if (!mapping.TryGetValue(typeName, out var byTypeMap))
                    return null;

                foreach (KeyValuePair<Type, HashSet<string>> map in byTypeMap)
                {
                    if (map.Value.Any(name => name.Length == 0 || AssemblyResolver.IdentityMatches(new AssemblyName(name), assemblyName, false)))
                        return map.Key;
                }

                // letting the default logic take over
                return null;
            }

            #endregion

            string fullName = String.IsNullOrEmpty(assemblyName) ? typeName : typeName + "," + assemblyName;
            return Reflector.ResolveType(fullName, ResolveType);
        }

        #endregion

        #region Private Methods

        #endregion
       
        #endregion
    }
}
