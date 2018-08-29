using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

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
    }
}
