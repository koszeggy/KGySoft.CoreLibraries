using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml;
using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using KGySoft.Libraries.Collections;

namespace KGySoft.Libraries.Serialization
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
        /// <item><term>Natively supported types</term><description>Primitive types along with their <see cref="Nullable{T}"/> version and the most common framework types such as <see cref="Enum"/> instances, <see cref="DateTime"/>, <see cref="DateTimeOffset"/>,
        /// <see cref="TimeSpan"/> and even <see cref="Type"/> itself as long as it is not a standalone generic parameter.</description></item>
        /// <item><term><see cref="IXmlSerializable"/> instances</term><description>Types that implement the <see cref="IXmlSerializable"/> interface can be serialized.</description></item>
        /// <item><term>Types with <see cref="TypeConverter"/></term><description>If the converter supports serializing to and from <see cref="string"/> type.</description></item>
        /// <item><term>Simple objects</term><description>A type can be serialized with the default options if it meets the following criteria:
        /// <list type="bullet">
        /// <item>The type has parameterless constructor, or is a value type</item>
        /// <item>It has only public instance fields and properties. For properties, both accessors are public. Static members are ignored.
        /// <note>Compiler-generated backing fields are ignored so types with public auto properties are considered simple.</note></item>
        /// <item>All fields and properties can be set, or, all read-only fields and properties are either <see cref="IXmlSerializable"/> implementations or collections of the types enlisted at next main bullet point.</item>
        /// <item>None of the fields and properties are delegates.</item>
        /// <item>The type has no instance events.</item>
        /// </list>
        /// <note>A type can be serialized only if these criteria are true for the serialized properties recursively.</note></description></item>
        /// <item><term>Collections</term><description><see cref="Array"/>, <see cref="List{T}"/>, <see cref="ArrayList"/>, <see cref="LinkedList{T}"/>, <see cref="Queue{T}"/>, <see cref="Queue"/>, <see cref="Stack{T}"/>, <see cref="Stack"/>,
        /// <see cref="BitArray"/>, <see cref="CircularList{T}"/>, <see cref="ConcurrentBag{T}"/>, <see cref="ConcurrentQueue{T}"/> and <see cref="ConcurrentStack{T}"/> instances are supported by the default options. To support other collections
        /// you can use fallback options, for example <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/>.
        /// <note>The reason of fallback options or attributes have to be used even for simple collections such as <see cref="Dictionary{TKey,TValue}"/> is that they can be instantiated by special settings such as an equality comparer,
        /// which cannot be retrieved by the public members when the collection is being serialized. However, if a property or field returns a non-<see langword="null"/> instance after the container object is created, then the returned instance is tried to be used on deserialization.
        /// This makes possible to deserialize even custom-initialized dictionaries and other objects.</note>
        /// </description></item>
        /// </list>
        /// </para>
        /// </summary>
        None,

        /// <summary>
        /// <para>If enabled, collection elements and non binary-serialized complex objects will be identified by the assembly qualified type name, otherwise, only by full type name.
        /// Using fully qualified names makes possible to restore types that are declared in any external referenced assembly. While using not fully qualified type names makes possible
        /// to restore a type declared in the caller assembly even if the version of the assembly has been modified since last serialization.</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        FullyQualifiedNames = 1,

        /// <summary>
        /// <para>If a type cannot be parsed natively and has no <see cref="TypeConverter"/> with <see cref="string"/> support, then
        /// enabling this option makes possible to store its content in binary format within the XML.</para>
        /// <para>Though trusted collections and objects with only public read-write properties and fields can be serialized with <see cref="None"/> options
        /// as well, using this option will cause to serialize them in binary format, too.</para>
        /// <para>Note: If both <see cref="BinarySerializationAsFallback"/> and <see cref="RecursiveSerializationAsFallback"/> options are enabled, then binary serialization
        /// is stronger, except for properties that are marked by <see cref="DesignerSerializationVisibility.Content"/> visibility, which causes the property to be serialized recursively.</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
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
        /// is stronger, except for properties that are marked by <see cref="DesignerSerializationVisibility.Content"/> visibility, which causes the property to be serialized recursively.</para>
        /// <para><c>Key</c> and <c>Value</c> properties of <see cref="DictionaryEntry"/> and <see cref="KeyValuePair{TKey,TValue}"/> instances are always serialized recursively because these are natively supported types.</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        RecursiveSerializationAsFallback = 1 << 2,

        /// <summary>
        /// <para>If a <see cref="ValueType"/> (<see langword="struct"/>) has no <see cref="TypeConverter"/> and contains no references,
        /// then by enabling this option the instance will be serialized in a compact binary form if possible.
        /// <note>This option is stronger than fallback options (<see cref="BinarySerializationAsFallback"/> and <see cref="RecursiveSerializationAsFallback"/>),
        /// except for <see cref="DictionaryEntry"/> and <see cref="KeyValuePair{TKey,TValue}"/> instances, which are always serialized recursively.
        /// This option affects only instances, which have no reference fields at all, or have only <see cref="string"/> or <see cref="Array"/> references,
        /// which are decorated by <see cref="MarshalAsAttribute"/> using <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.</note></para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        CompactSerializationOfStructures = 1 << 3,

        /// <summary>
        /// <para>If enabled, then members without <see cref="DefaultValueAttribute"/> defined, will be treated as if they were decorated by
        /// <see cref="DefaultValueAttribute"/> with the default value of the property type (<see langword="null"/> for reference types and
        /// bitwise zero value of value types).</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        AutoGenerateDefaultValuesAsFallback = 1 << 4,

        /// <summary>
        /// <para>Ignores the original predefined <see cref="DefaultValueAttribute"/> definitions for all of the properties.</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreDefaultValueAttribute = 1 << 5,

        /// <summary>
        /// <para>Ignores the presence of <c>ShouldSerialize&lt;PropertyName&gt;</c> methods for all of the members.</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreShouldSerialize = 1 << 6,

        /// <summary>
        /// <para>If enabled, <see cref="XmlSerializer"/> ignores <see cref="IXmlSerializable"/> implementations.</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreIXmlSerializable = 1 << 7,

        /// <summary>
        /// <para>If enabled, then array of primitive types are serialized in a single XML node instead of creating XML nodes for each element in the array.</para>
        /// <para>Default at serialization methods: <strong>Enabled</strong></para>
        /// </summary>
        CompactSerializationOfPrimitiveArrays = 1 << 8,

        /// <summary>
        /// <para>Unless a well configured <see cref="XmlWriter"/> is used, newline characters of string or char values can be lost or changed during deserialization.
        /// This flag ensures that newline characters can be always deserialized regardless of the used <see cref="XmlWriterSettings.NewLineHandling"/> value.</para>
        /// <para>Default at serialization methods: <strong>Enabled</strong></para>
        /// </summary>
        EscapeNewlineCharacters = 1 << 9,

        /// <summary>
        /// <para>When this flag is enabled, binary contents will not be protected by a CRC value.
        /// Affects <see cref="CompactSerializationOfPrimitiveArrays"/>, <see cref="CompactSerializationOfStructures"/> and <see cref="BinarySerializationAsFallback"/> flags.</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        OmitCrcAttribute = 1 << 10,

        /// <summary>
        /// <para>By default <see cref="XmlSerializer"/> includes public fields in serialization, similarly to <a href="https://msdn.microsoft.com/en-us/library/System.Xml.Serialization.XmlSerializer.aspx" target="_blank">System.Xml.Serialization.XmlSerializer</a>.
        /// By enabling this option, only public properties will be serialized.</para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        ExcludeFields = 1 << 11,

        /// <summary>
        /// <para>By default read-only properties and fields are serialized only if they <see cref="IXmlSerializable"/> implementations or collections that can be populated.
        /// This options forces to serialize read-only fields and properties as well.
        /// <note>Public properties with private setter accessor are serializable even without this option.</note>
        /// <note type="caution">Enabling this option can make it possible that properties without setter accessor will not be able to deserialized.
        /// Deserialization will fail if the read-only property returns a <see langword="null"/> value or its content cannot be restored (eg. it has a simple type).</note>
        /// </para>
        /// <para>Default at serialization methods: <strong>Disabled</strong></para>
        /// </summary>
        SerializeReadOnlyMembers = 1 << 12,


    }
}
