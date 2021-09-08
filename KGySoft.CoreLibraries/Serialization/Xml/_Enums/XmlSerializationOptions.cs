#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlSerializationOptions.cs
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
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
#if !NET35
using System.Runtime.CompilerServices; 
#endif
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

using KGySoft.Collections;
using KGySoft.Serialization.Binary;

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.Serialization.Xml
{
    /// <summary>
    /// Options for serializer methods of <see cref="XmlSerializer"/> class.
    /// </summary>
    [Flags]
    public enum XmlSerializationOptions
    {
        /// <summary>
        /// <para>Represents no enabled options.</para>
        /// <para>
        /// With every options disabled only those types are serialized, which are guaranteed to be able to deserialized perfectly. Such types are:
        /// <list type="bullet">
        /// <item><term>Natively supported types</term><description>Primitive types along with their <see cref="Nullable{T}"/> counterpart and the most common framework types such as <see cref="Enum"/> instances, <see cref="DateTime"/>, <see cref="DateTimeOffset"/>,
        /// <see cref="TimeSpan"/> and even <see cref="Type"/> itself as long as it is a runtime type instance.</description></item>
        /// <item><term><see cref="IXmlSerializable"/> instances</term><description>Types that implement the <see cref="IXmlSerializable"/> interface can be serialized.</description></item>
        /// <item><term>Types with <see cref="TypeConverter"/></term><description>If the converter supports serializing to and from <see cref="string"/> type.</description></item>
        /// <item><term>Simple objects</term><description>A type can be serialized with the default options if it meets the following criteria:
        /// <list type="bullet">
        /// <item>The type has a parameterless constructor, or is a value type</item>
        /// <item>It has only public instance fields and properties. For properties, both accessors are public. Static members are ignored.
        /// <note>Compiler-generated backing fields are ignored so types with public auto properties are considered simple.</note></item>
        /// <item>All fields and properties can be set, or, all read-only fields and properties are either <see cref="IXmlSerializable"/> implementations or collections of the types enlisted at next main bullet point.</item>
        /// <item>None of the fields and properties are delegates.</item>
        /// <item>The type has no instance events.</item>
        /// </list>
        /// <note>A type can be serialized if these criteria are true for the serialized properties and fields recursively.</note></description></item>
        /// <item><term>Collections</term><description><see cref="Array"/>, <see cref="List{T}"/>, <see cref="LinkedList{T}"/>, <see cref="Queue{T}"/>, <see cref="Stack{T}"/>,
        /// <see cref="ArrayList"/>, <see cref="Queue"/>,  <see cref="Stack"/>, <see cref="BitArray"/>, <see cref="StringCollection"/>,
        /// <see cref="CircularList{T}"/>, <see cref="ConcurrentBag{T}"/>, <see cref="ConcurrentQueue{T}"/> and <see cref="ConcurrentStack{T}"/> instances are supported by the default options. To support other collections
        /// you can use fallback options, for example <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/>.
        /// <note>The reason of fallback options or attributes have to be used even for simple collections such as <see cref="Dictionary{TKey,TValue}"/> is that they can be instantiated by special settings such as an equality comparer,
        /// which cannot be retrieved by the public members when the collection is being serialized. However, if a property or field returns a non-<see langword="null"/>&#160;instance after the container object is created, then the returned instance is tried to be used on deserialization.
        /// This makes possible to deserialize even custom-initialized dictionaries and other objects.</note>
        /// </description></item>
        /// </list>
        /// </para>
        /// </summary>
        None,

        /// <summary>
        /// <para>If enabled, collection elements and non binary-serialized complex objects will be identified by the assembly qualified type name; otherwise, only by full type name.
        /// Using fully qualified names makes possible to automatically load the assembly of a referenced type (unless safe mode is used on deserialization). Partial identity match is allowed,
        /// so type resolving tolerates assembly version change. When resolving non-fully qualified type names, its assembly must be loaded before the deserialization;
        /// otherwise, the type resolving will fail.</para>
        /// <note type="security">When using <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.DeserializeSafe">DeserializeSafe</see>
        /// and <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.DeserializeContentSafe">DeserializeContentSafe</see> methods, no assemblies will be loaded
        /// during the deserialization, even when types use fully qualified names.</note>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        FullyQualifiedNames = 1,

        /// <summary>
        /// <para>If a type cannot be parsed natively and has no <see cref="TypeConverter"/> with <see cref="string"/> support, then
        /// enabling this option makes possible to store its content in binary format (using the <see cref="BinarySerializationFormatter"/> class) within the XML.</para>
        /// <para>Though trusted collections and objects with only public read-write properties and fields can be serialized with the <see cref="None"/> options
        /// as well, using this option will cause to serialize them in binary format, too.</para>
        /// <para>If both <see cref="BinarySerializationAsFallback"/> and <see cref="RecursiveSerializationAsFallback"/> options are enabled, then binary serialization
        /// has higher priority, except for properties that are marked by <see cref="DesignerSerializationVisibility.Content"/> visibility, which causes the property to be serialized recursively.</para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// <note type="security">When using <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.DeserializeSafe">DeserializeSafe</see>
        /// and <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.DeserializeContentSafe">DeserializeContentSafe</see> methods, then
        /// deserialization will throw an <see cref="InvalidOperationException"/> if the XML stream contains such content.</note>
        /// </summary>
        BinarySerializationAsFallback = 1 << 1,

        /// <summary>
        /// <para>If a type cannot be parsed natively, has no <see cref="TypeConverter"/> with <see cref="string"/> support
        /// or binary serialization is disabled, then enabling this option makes possible to serialize the object by serializing its public properties, fields and collection items recursively.
        /// If a property or collection element cannot be serialized, then a <see cref="SerializationException"/> will be thrown.</para>
        /// <para>Properties can be marked by <see cref="DesignerSerializationVisibilityAttribute"/> with <see cref="DesignerSerializationVisibility.Content"/> value to
        /// indicate that they should be serialized recursively without using this fallback option.
        /// <note type="caution">Enabling this option will not guarantee that deserialization of the object will be the same as the original instance.
        /// Use this option only when serialized types can be restored by setting public properties and fields, and the type has a default constructor.
        /// To avoid circular references use <see cref="DesignerSerializationVisibilityAttribute"/> with <see cref="DesignerSerializationVisibility.Hidden"/> value on back-referencing properties.</note></para>
        /// <para>If both <see cref="BinarySerializationAsFallback"/> and <see cref="RecursiveSerializationAsFallback"/> options are enabled, then binary serialization
        /// has higher priority, except for properties that are marked by <see cref="DesignerSerializationVisibility.Content"/> visibility, which causes the property to be serialized recursively.</para>
        /// <para><c>Key</c> and <c>Value</c> properties of <see cref="DictionaryEntry"/> and <see cref="KeyValuePair{TKey,TValue}"/> instances are always serialized recursively because these are natively supported types.</para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        RecursiveSerializationAsFallback = 1 << 2,

        /// <summary>
        /// <para>If a <see cref="ValueType"/> (<see langword="struct"/>) has no <see cref="TypeConverter"/> and contains no references,
        /// then by enabling this option the instance will be serialized in a compact binary form.
        /// <note>This option has higher priority than fallback options (<see cref="BinarySerializationAsFallback"/> and <see cref="RecursiveSerializationAsFallback"/>),
        /// except for <see cref="DictionaryEntry"/> and <see cref="KeyValuePair{TKey,TValue}"/> instances, which are always serialized recursively.
        /// This option affects only instances, which have no reference fields at all.</note></para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        CompactSerializationOfStructures = 1 << 3,

        /// <summary>
        /// <para>If enabled, then members without <see cref="DefaultValueAttribute"/> defined, will be treated as if they were decorated by
        /// <see cref="DefaultValueAttribute"/> with the default value of the property type (<see langword="null"/>&#160;for reference types and
        /// bitwise zero value of value types). This causes to skip serializing members, whose value equals to the default value of their type.</para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        AutoGenerateDefaultValuesAsFallback = 1 << 4,

        /// <summary>
        /// <para>Ignores the originally defined <see cref="DefaultValueAttribute"/> definitions for all of the properties. This causes that all members will be serialized regardless of their values.</para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreDefaultValueAttribute = 1 << 5,

        /// <summary>
        /// <para>Ignores the presence of <c>ShouldSerialize&lt;PropertyName&gt;</c> methods for all of the members.</para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreShouldSerialize = 1 << 6,

        /// <summary>
        /// <para>If enabled, <see cref="XmlSerializer"/> ignores <see cref="IXmlSerializable"/> implementations.</para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreIXmlSerializable = 1 << 7,

        /// <summary>
        /// <para>If enabled, then array of primitive types are serialized in a single XML node instead of creating XML nodes for each element in the array.</para>
        /// <para>Default state at serialization methods: <strong>Enabled</strong></para>
        /// </summary>
        CompactSerializationOfPrimitiveArrays = 1 << 8,

        /// <summary>
        /// <para>Unless a well configured <see cref="XmlWriter"/> is used, newline characters of string or char values can be lost or changed during deserialization.
        /// This flag ensures that newline characters can be always deserialized regardless of the used <see cref="XmlWriterSettings.NewLineHandling"/> value of an <see cref="XmlWriter"/>.</para>
        /// <para>Default state at serialization methods: <strong>Enabled</strong></para>
        /// </summary>
        EscapeNewlineCharacters = 1 << 9,

        /// <summary>
        /// <para>When this flag is enabled, binary contents will not be protected by a CRC value.
        /// Affects <see cref="CompactSerializationOfPrimitiveArrays"/>, <see cref="CompactSerializationOfStructures"/> and <see cref="BinarySerializationAsFallback"/> flags.</para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        OmitCrcAttribute = 1 << 10,

        /// <summary>
        /// <para>By default <see cref="Xml.XmlSerializer"/> includes public fields in serialization, similarly to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer" target="_blank">System.Xml.Serialization.XmlSerializer</a>.
        /// By enabling this option, only public properties will be serialized.</para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        ExcludeFields = 1 << 11,

        /// <summary>
        /// <para>By default read-only properties and fields are serialized only if they are <see cref="IXmlSerializable"/> implementations or collections that can be populated.
        /// This option forces to serialize read-only fields and properties, as well as collections that are read-only and have no recognizable initializer constructor.
        /// <note>Public properties with private setter accessor are serializable even without this option.</note>
        /// <note>Read-only collections witch recognizable collection initializer constructor are serializable even without this option.</note>
        /// <note type="caution">Enabling this option can make it possible that properties without setter accessor will not be able to deserialized.
        /// Deserialization will fail if the read-only property returns a <see langword="null"/>&#160;value or its content cannot be restored (eg. it has a simple type or is a read-only collection).
        /// Use this option only if an object has to be serialized only for information (eg. in logs) and deserialization is not necessary.</note>
        /// </para>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        ForcedSerializationOfReadOnlyMembersAndCollections = 1 << 12,

        /// <summary>
        /// <para>When both <see cref="FullyQualifiedNames"/> and this flag are enabled, then every type will be serialized with its actual assembly identity rather than considering
        /// the value of an existing <see cref="TypeForwardedFromAttribute"/>.</para>
        /// <para>This flag is ignored if <see cref="FullyQualifiedNames"/> is disabled.</para>
        /// <note>Enabling this flag may cause that the type will not be able to be deserialized on a different platform.</note>
        /// <para>Default state at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreTypeForwardedFromAttribute = 1 << 13,
    }
}
