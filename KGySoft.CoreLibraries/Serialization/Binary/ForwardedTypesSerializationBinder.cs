#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ForwardedTypesSerializationBinder.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides a <see cref="SerializationBinder"/> that makes possible to serialize and deserialize types with custom assembly identity.
    /// <br/>See the <strong>Remarks</strong> section for details and some examples.
    /// </summary>
    /// <remarks>
    /// <note type="security"><para>If a deserialization stream may come from an untrusted source, then make sure to set the <see cref="SafeMode"/> property
    /// to <see langword="true"/>&#160;to prevent loading assemblies when resolving types by the fallback logic.</para>
    /// <para>See the security notes at the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for more details.</para></note>
    /// <para>By default, the <see cref="ForwardedTypesSerializationBinder"/> does nothing. Resolving types from legacy
    /// assemblies works automatically if at least a chunk version of the assembly exists on the current platform containing nothing but a bunch
    /// of <see cref="TypeForwardedToAttribute"/> attributes (this is the case for the original .NET Framework assemblies on .NET Core and .NET Standard).</para>
    /// <para>To resolve types that are not forwarded by the <see cref="TypeForwardedToAttribute"/> from an existing assembly with the
    /// given identity you can use this binder for deserialization. Add the types to be handled by the <see cref="AddType">AddType</see> or
    /// <see cref="AddTypes">AddTypes</see> methods.</para>
    /// <para>If <see cref="WriteLegacyIdentity"/> is set to <see langword="true"/>, then mapping works also on serialization.
    /// For types without an explicitly set mapping the value of the <see cref="TypeForwardedFromAttribute"/> attribute will be written if it is defined.
    /// <note>By default, both <see cref="BinaryFormatter"/> and <see cref="BinarySerializationFormatter"/> dumps legacy identities on serialization
    /// for types decorated by the <see cref="TypeForwardedFromAttribute"/>. If you use this binder on serialization without adding any type
    /// just after setting the <see cref="WriteLegacyIdentity"/> to <see langword="true"/>, then the only difference will be that even the <c>mscorlib</c> assembly
    /// name will be dumped to the output stream, which would be omitted otherwise.</note></para>
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
    /// // Any type of any assembly will be mapped to SomeOtherType if their full names match.
    /// binder.AddType(typeof(SomeOtherType));
    ///
    /// // Multiple types can be enlisted without assembly identity
    /// binder.AddTypes(typeof(MyType), typeof(MyOtherType), typeof(SomeOtherType));
    ///
    /// IFormatter formatter = new BinarySerializationFormatter { Binder = binder }; // or BinaryFormatter
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
    /// IFormatter formatter = new BinaryFormatter { Binder = binder }; // or BinarySerializationFormatter
    /// formatter.Serialize(serializationStream, obj);
    /// ]]></code>
    /// </para>
    /// <note type="tip">If not only the assembly name but also the inner content of a type (ie. field names) changed, then you can use
    /// the <see cref="CustomSerializerSurrogateSelector"/> class.</note>
    /// </example>
    /// <seealso cref="WeakAssemblySerializationBinder"/>
    /// <seealso cref="CustomSerializationBinder"/>
    /// <seealso cref="BinarySerializationFormatter"/>
    public sealed class ForwardedTypesSerializationBinder : SerializationBinder, ISerializationBinder
    {
        #region Fields

        /// <summary>
        /// Key: Type.FullName for root types.
        /// Value.Key: Different types of the same full name
        /// Value.Value: AssemblyName.FullName, or empty string, if any assemblies are allowed.
        ///              Not AssemblyNames because they have no overridden Equals.
        /// </summary>
        private readonly StringKeyedDictionary<Dictionary<Type, HashSet<string>>> mapping = new StringKeyedDictionary<Dictionary<Type, HashSet<string>>>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether a legacy assembly identity is tried to be written on serializing.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <note>In Framework .NET 3.5 <see cref="BinaryFormatter"/> and most <see cref="IFormatter"/> implementations ignore the
        /// value of this property. <see cref="BinarySerializationFormatter"/> is able to use the <see cref="ForwardedTypesSerializationBinder"/>
        /// as an <see cref="ISerializationBinder"/> implementation and consider the value of this property even in .NET 3.5.</note>
        /// <para>If the value of this property is <see langword="true"/>, then on serialization a legacy identity is tried to be written for the serialized type.
        /// That is, the first <see cref="AssemblyName"/> that was specified for the type by the <see cref="AddType">AddType</see> method, or
        /// the value of the <see cref="TypeForwardedFromAttribute.AssemblyFullName">TypeForwardedFromAttribute.AssemblyFullName</see> property
        /// if it is specified for the type.</para>
        /// </remarks>
        public bool WriteLegacyIdentity { get; set; }

        /// <summary>
        /// Gets or sets whether loading assemblies is prohibited on deserialization, when there is no rule specified for a type
        /// and the default resolve logic is used.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>, and there is no rule specified for a type, then it ensures that no assembly loading will occur when using the default type resolving logic.</para>
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
        /// Adds the <paramref name="type"/> to this binder. If no <paramref name="assemblyIdentities"/> are defined, then any
        /// <see cref="Type"/> with the same full name will be resolved to <paramref name="type"/>; otherwise, only the ones,
        /// whose identities match.
        /// </summary>
        /// <param name="type">The type to be added to the handled types by this binder.</param>
        /// <param name="assemblyIdentities">The legacy assembly identities to be recognized. Can contain also partial names. If empty or contains a <see langword="null"/>
        /// element, then any type of the same full name will be resolved to <paramref name="type"/>.</param>
        /// <remarks>
        /// <para>If no assembly identities are specified, then any <see cref="Type"/> that has the same full name as <paramref name="type"/>,
        /// will be resolved to the specified <paramref name="type"/>.</para>
        /// <para>If at least one assembly identity is specified and <see cref="WriteLegacyIdentity"/> is <see langword="true"/>,
        /// then on serialization the first specified name will be returned as the assembly name of the <paramref name="type"/>.</para>
        /// <para>For generic types you should specify the generic type definition only.</para>
        /// </remarks>
        public void AddType(Type type, params AssemblyName[] assemblyIdentities)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            string? fullName = type.FullName;
            if (fullName == null || !type.IsRuntimeType() || type.HasElementType
                || type.IsConstructedGenericType()
                || type.IsGenericParameter)
                Throw.ArgumentException(Argument.type, Res.SerializationRootTypeExpected);
            Debug.Assert(type == type.GetRootType(), "Root type expected");

            // getting/creating the map by type of the same full names
            if (!mapping.TryGetValue(fullName, out Dictionary<Type, HashSet<string>>? mapByType))
            {
                mapByType = new Dictionary<Type, HashSet<string>>();
                mapping[fullName] = mapByType;
            }

            // getting/creating the identities set
            if (!mapByType.TryGetValue(type, out var identities))
            {
                identities = new HashSet<string>();
                mapByType[type] = identities;
            }

            // no assembly identities or contains null: any assembly will be accepted
            if (assemblyIdentities.IsNullOrEmpty())
            {
                identities.Add(String.Empty);
                return;
            }

            foreach (AssemblyName assemblyName in assemblyIdentities)
                identities.Add(assemblyName.FullName);
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
        /// <para>For generic types it is enough to specify the generic type definition only.</para>
        /// </remarks>
        public void AddTypes(params Type[] types)
        {
            if (types == null!)
                Throw.ArgumentNullException(Argument.types);
            if (types.Length == 0)
                Throw.ArgumentException(Argument.types, Res.CollectionEmpty);
            if (types.Contains(null!))
                Throw.ArgumentException(Argument.types, Res.ArgumentContainsNull);
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
        /// <param name="typeName">If <paramref name="assemblyName"/> is not <see langword="null"/>&#160;when this method returns, then contains the full name of the <paramref name="serializedType"/>,
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
            #region Local Methods

            AssemblyName GetAssemblyName(Type t)
            {
                Type key = t.GetRootType();

                // we have a non-empty specified assembly name 
                if (mapping.GetValueOrDefault(key.FullName!)?.GetValueOrDefault(key)?.FirstOrDefault(n => !String.IsNullOrEmpty(n)) is string asmName)
                    return new AssemblyName(asmName);

                string? legacyName = AssemblyResolver.GetForwardedAssemblyName(key, false);
                if (legacyName != null)
                    return new AssemblyName(legacyName);

                // original name
                return t.Assembly.GetName();
            }

            #endregion

            if (serializedType == null!)
                Throw.ArgumentNullException(Argument.serializedType);
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
        public override Type? BindToType(string assemblyName, string typeName)
        {
            #region Local Methods

            Type? ResolveType(AssemblyName? asmName, string typName)
            {
                // there is no rule for such type name
                if (!mapping.TryGetValue(typName, out var byTypeMap))
                    return null;

                foreach (KeyValuePair<Type, HashSet<string>> map in byTypeMap)
                {
                    if (map.Value.Any(name => name.Length == 0 || AssemblyResolver.IdentityMatches(new AssemblyName(name), asmName, false)))
                        return map.Key;
                }

                // letting the default logic take over
                return null;
            }

            #endregion

            string fullName = String.IsNullOrEmpty(assemblyName) ? typeName : typeName + "," + assemblyName;
            var options = ResolveTypeOptions.AllowPartialAssemblyMatch;
            if (!SafeMode)
                options |= ResolveTypeOptions.TryToLoadAssemblies;
            Type? result = Reflector.ResolveType(fullName, ResolveType);

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

        #endregion

        #endregion
    }
}
