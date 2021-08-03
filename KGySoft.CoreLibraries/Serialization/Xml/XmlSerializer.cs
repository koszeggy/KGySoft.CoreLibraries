#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlSerializer.cs
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
using System.Collections.Generic;
using System.ComponentModel;
#if NET35
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using KGySoft.ComponentModel;
using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

#endregion

namespace KGySoft.Serialization.Xml
{
    /// <summary>
    /// <see cref="XmlSerializer"/> makes possible serializing and deserializing object instances into/from XML content. The class contains various overloads to support serializing directly into file or by
    /// <see cref="XElement"/>, <see cref="XmlWriter"/>, any <see cref="TextWriter"/> and any <see cref="Stream"/> implementations.
    /// <br/>See the <strong>Remarks</strong> section for the differences compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer" target="_blank">System.Xml.Serialization.XmlSerializer</a> class.
    /// </summary>
    /// <remarks>
    /// <note type="security"><para>The <see cref="XmlSerializer"/> supports polymorphism and stores type information whenever the type of a member or collection element differs from the
    /// type of the stored instance. If the XML content to deserialize is from an untrusted source make sure to use the <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.DeserializeSafe">DeserializeSafe</see>
    /// and <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.DeserializeContentSafe">DeserializeContentSafe</see> methods to prevent loading assemblies during the deserialization.
    /// Please note though that it cannot protect you from all possible threats if a type of the already loaded assemblies can be exploited for a security attack.
    /// The <see cref="XmlSerializer"/> can only create objects by using their default constructor and is able to set the public fields and properties.
    /// It can also create collections by special initializer constructors and can populate them by the standard interface implementations.</para>
    /// <para>In safe mode you must preload every assembly that are referred in the XML content. Additionally, in safe mode an <see cref="InvalidOperationException"/> is thrown for content
    /// that is serialized by <see cref="BinarySerializationFormatter"/> (see the <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/> option).</para></note>
    /// <para><see cref="XmlSerializer"/> supports serialization of any simple types and complex objects with their public properties and fields as well as several collection types.
    /// <note>Unlike the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer" target="_blank">System.Xml.Serialization.XmlSerializer</a> class,
    /// this <see cref="XmlSerializer"/> is not designed for customizing output format (though <see cref="IXmlSerializable"/> implementations are considered). Not even <c>Xml...Attribute</c>s
    /// are supported (except <see cref="XmlRootAttribute"/> for the root element of <see cref="IXmlSerializable"/> implementations). Instead, this class is
    /// designed to support XML serialization of any type as long as they have a default constructor and their state can be fully restored by their public fields and properties.</note>
    /// </para>
    /// <para>Several <a href="https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel" target="_blank">System.ComponentModel</a> techniques are supported,
    /// which also makes possible to use the <see cref="XmlSerializer"/> for types that can be edited in a property grid, such as components, configurations or any types in a custom designer.
    /// The supported component model attributes and techniques:
    /// <list type="bullet">
    /// <item><term><see cref="DesignerSerializationVisibilityAttribute"/></term><description>Use value <see cref="DesignerSerializationVisibility.Hidden"/> for public field or property to prevent its serialization
    /// and use <see cref="DesignerSerializationVisibility.Content"/> value to explicitly express that the property value can be serialized recursively (see also <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/>) option.</description></item>
    /// <item><term><see cref="DefaultValueAttribute"/></term><description>If the value of a public property or field equals to the value specified by this attribute, then its value will not be serialized
    /// (see also <see cref="XmlSerializationOptions.IgnoreDefaultValueAttribute"/> and <see cref="XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback"/> options).</description></item>
    /// <item><term><c>ShouldSerialize...</c> methods</term><description>If the type being serialized has an instance method with no parameters and a <see cref="bool"/> return type (can be private as well) named <c>ShouldSerializeMemberName</c> where <c>MemberName</c> is the name of a property or field,
    /// then its return value determines whether the member should be serialized. This technique is used in some designers and property grid controls (see also <see cref="XmlSerializationOptions.IgnoreShouldSerialize"/> option).</description></item>
    /// <item><term><see cref="TypeConverterAttribute"/></term><description>This attribute is supported both for types and property/field members. If a <see cref="TypeConverter"/> supports serialization to and from <see cref="string"/> type,
    /// then it will be used for serializing its value (see also the <see cref="CoreLibraries.TypeExtensions.RegisterTypeConverter{TConverter}">RegisterTypeConverter</see> extension method).</description></item>
    /// </list>
    /// </para>
    /// <para>Basically types with default constructors are supported. However, if a field or property value is not <see langword="null"/>&#160;after creating its parent object, then the returned instance is tried to be re-used on deserialization.
    /// <note>Objects without a default constructor can be serialized at root level also by the <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.SerializeContent">SerializeContent</see> methods into an already existing
    /// <see cref="XElement"/> node or by an <see cref="XmlWriter"/>, which already opened and XML element before calling the <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.SerializeContent">SerializeContent</see> method. When deserializing,
    /// the result object should be created by the caller, and the content can be deserialized by the <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.DeserializeContent">DeserializeContent</see> methods.</note>
    /// </para>
    /// <para><strong>Options:</strong>
    /// <br/>By specifying the <see cref="XmlSerializationOptions"/> argument in the <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.Serialize">Serialize</see> and <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.SerializeContent">SerializeContent</see>
    /// methods you can override the default behavior of serialization. The default options and the <see cref="XmlSerializationOptions.None"/> option ensure that only those types are serialized, which are guaranteed to be able to deserialized perfectly.
    /// For details see the description of the <see cref="XmlSerializationOptions.None"/> option.</para>
    /// <para>If a type cannot be serialized with the currently used options a <see cref="SerializationException"/> will be thrown.</para>
    /// <para>You can use <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/> option to enable recursive serialization of every type of objects and collections. A collection type can be serialized if
    /// it implements the <see cref="ICollection{T}"/>, <see cref="IList"/> or <see cref="IDictionary"/> interfaces, and it can be deserialized if it has a default constructor, or an initializer constructor with a single parameter that can accept an <see cref="Array"/>
    /// or <see cref="List{T}"/> instance (non-dictionaries) or a <see cref="Dictionary{TKey,TValue}"/> instance (dictionary collections). Non-collection types must have a parameterless constructor to be able to be deserialized.
    /// <note>If a field or property returns a non-<see langword="null"/>&#160;value on deserialization, then its value is tried to be used so in this case it can be created by its container object using any custom constructor.</note>
    /// <note type="caution">Enabling the <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/> option does not guarantee that the deserialized instances will be the same as the original ones.</note>
    /// </para>
    /// <para>If <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/> option is enabled, then types without a native support and appropriate <see cref="TypeConverter"/> will be serialized into a binary stream, which
    /// will be stored in the result XML. Though this provides the best compatibility of any type, it hides the whole inner structure of the serialized object. If a root level object without native support is serialized by the
    /// <see cref="O:KGySoft.Serialization.Xml.XmlSerializer.Serialize">Serialize</see> using the <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, then the whole XML result will be a single node with the binary content.
    /// <note>To use binary serialization only for some types or properties you can specify the <see cref="BinaryTypeConverter"/> by the <see cref="TypeConverterAttribute"/> for a property or type
    /// (or you can use the <see cref="CoreLibraries.TypeExtensions.RegisterTypeConverter{TConverter}">RegisterTypeConverter</see> extension method for types).</note>
    /// </para>
    /// <para>See the <see cref="XmlSerializationOptions"/> enumeration for further options.</para>
    /// <para><strong>New features and improvements</strong> compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer" target="_blank">System.Xml.Serialization.XmlSerializer</a>:
    /// <list type="bullet">
    /// <item><term>Strings</term><description>If a string contains only white spaces, then system <see cref="System.Xml.Serialization.XmlSerializer"/> cannot deserialize it properly. <see cref="string"/> instances containing
    /// invalid UTF-16 code points are also cannot be serialized. This <see cref="XmlSerializer"/> implementation handles them correctly.</description></item>
    /// <item><term>Collections with base element type</term><description>If the element type of a collection is a base type or an interface, then the system serializer throws an exception for derived element types
    /// suggesting that <see cref="XmlIncludeAttribute"/> should be defined for all possible derived types. Unfortunately this attribute is applicable only for possible types of properties/fields
    /// but not for collection elements. And in many cases it simply cannot be predefined in advance what derived types will be used at run-time.</description></item>
    /// <item><term>Collections with read-only properties</term><description>Usually collection properties can be read-only. But to be able to use the system serializer we need to define a setter for such properties; otherwise, serialization may fail.
    /// This <see cref="XmlSerializer"/> does not require setter accessor for a collection property if the property is not <see langword="null"/>&#160;after initialization and can be populated by using the usual collection interfaces.</description></item>
    /// <item><term>Objects without default constructors</term><description>The system serializer requires that the deserialized types have default constructors. On deserializing fields and properties, this <see cref="XmlSerializer"/> implementation tries to use
    /// the return value of the members. If they are not <see langword="null"/>&#160;after creating their container object, then the returned instances will be used instead of creating a new instance.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/M2dfrx" target="_blank">online</a>.</note>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.IO;
    /// using System.Text;
    /// using System.ComponentModel;
    /// using System.Collections.Generic;
    /// using System.Collections.ObjectModel;
    /// using System.Xml.Linq;
    /// using KGySoft.CoreLibraries;
    /// using KGySoft.Serialization.Xml;
    /// 
    /// // A good candidate for XML serialization:
    /// public class Person
    /// {
    ///     public string FirstName { get; set; }
    /// 
    ///     [DefaultValue(null)] // will not be serialized if null
    ///     public string MiddleName { get; set; }
    /// 
    ///     public string LastName { get; set; }
    /// 
    ///     public DateTime BirthDate { get; set; }
    /// 
    ///     // System serializer fails here: the property has no setter and its type cannot be instantiated.
    ///     public IList<string> PhoneNumbers { get; } = new Collection<string>();
    /// }
    /// 
    /// public class Program
    /// {
    ///     public static void Main()
    ///     {
    ///         var person = ThreadSafeRandom.Instance.NextObject<Person>();
    ///         var options = XmlSerializationOptions.RecursiveSerializationAsFallback;
    /// 
    ///         // serializing into XElement
    ///         XElement element = XmlSerializer.Serialize(person, options);
    ///         var clone = (Person)XmlSerializer.Deserialize(element);
    /// 
    ///         // serializing into file/Stream/TextWriter/XmlWriter are also supported: An XmlWriter will be used
    ///         var sb = new StringBuilder();
    ///         XmlSerializer.Serialize(new StringWriter(sb), person, options);
    ///         clone = (Person)XmlSerializer.Deserialize(new StringReader(sb.ToString()));
    /// 
    ///         Console.WriteLine(sb);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // <?xml version="1.0" encoding="utf-16"?>
    /// // <object type="Person">
    /// //   <FirstName>Uehaccuj</FirstName>
    /// //   <MiddleName>Rnig</MiddleName>
    /// //   <LastName>Iuvmozu</LastName>
    /// //   <BirthDate>1996-06-02T00:00:00Z</BirthDate>
    /// //   <PhoneNumbers type="System.Collections.ObjectModel.Collection`1[System.String]">
    /// //     <item>694677853</item>
    /// //     <item>6344</item>
    /// //   </PhoneNumbers>
    /// // </object>]]></code>
    /// </example>
    /// <seealso cref="XmlSerializationOptions"/>
    /// <seealso cref="BinarySerializer"/>
    /// <seealso cref="BinaryTypeConverter"/>
    public static class XmlSerializer
    {
        #region Constants

        #region Internal Constants

        internal const string NamespaceXml = "xml";
        internal const string AttributeSpace = "space";
        internal const string AttributeCrc = "crc";
        internal const string AttributeEscaped = "escaped";
        internal const string AttributeFormat = "format";
        internal const string AttributeType = "type";
        internal const string AttributeLength = "length";
        internal const string AttributeDim = "dim";
        internal const string AttributeDeclaringType = "declaringType";
        internal const string AttributeValuePreserve = "preserve";
        internal const string AttributeValueTrue = "true";
        internal const string AttributeValueCustom = "custom";
        internal const string AttributeValueStructBinary = "structBinary";
        internal const string AttributeValueBinary = "binary";
        internal const string ElementObject = "object";
        internal const string ElementItem = "item";
        internal const string MethodShouldSerialize = "ShouldSerialize";

        #endregion

        #region Private Constants

        private const XmlSerializationOptions defaultOptions = XmlSerializationOptions.CompactSerializationOfPrimitiveArrays | XmlSerializationOptions.EscapeNewlineCharacters;

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> parameter into a new <see cref="XElement"/> object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <returns>An <see cref="XElement"/> instance that contains the serialized object.
        /// Result can be deserialized by <see cref="Deserialize(XElement)"/> method.</returns>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with provided <paramref name="options"/></exception>
        public static XElement Serialize(object? obj, XmlSerializationOptions options = defaultOptions)
            => new XElementSerializer(options).Serialize(obj);

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> by the provided <see cref="XmlWriter"/> object.
        /// </summary>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will just be flushed but not closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The state of <paramref name="writer"/> is wrong or writer is closed.</exception>
        /// <exception cref="EncoderFallbackException">There is a character in the buffer that is a valid XML character but is not valid for the output encoding.
        /// For example, if the output encoding is ASCII but public properties of a class contain non-ASCII characters, an <see cref="EncoderFallbackException"/> is thrown.
        /// Such characters are escaped by character entity references in values when possible.</exception>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with provided <paramref name="options"/></exception>
        public static void Serialize(XmlWriter writer, object? obj, XmlSerializationOptions options = defaultOptions)
            => new XmlWriterSerializer(options).Serialize(writer, obj);

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> into the specified <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">Name of the file to create for serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> must not be null.</exception>
        /// <exception cref="IOException">File cannot be created or write error.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        public static void Serialize(string fileName, object? obj, XmlSerializationOptions options = defaultOptions)
        {
            if (fileName == null!)
                Throw.ArgumentNullException(Argument.fileName);

            XmlWriter xmlWriter = XmlWriter.Create(fileName, new XmlWriterSettings
            {
                Indent = true,
                NewLineHandling = NewLineHandling.Entitize,
                Encoding = Encoding.UTF8
            });

            using (xmlWriter)
            {
                Serialize(xmlWriter, obj, options);
                xmlWriter.Flush();
            }
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> by the provided <see cref="TextWriter"/> object.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> implementation (for example, a <see cref="StringWriter"/>) that will be used for serialization.
        /// The writer will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The writer is closed.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        public static void Serialize(TextWriter writer, object? obj, XmlSerializationOptions options = defaultOptions)
        {
            if (writer == null!)
                Throw.ArgumentNullException(Argument.writer);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                CloseOutput = false,
                // NewLineHandling = NewLineHandling.Entitize //- entitizes only /r and not /n. Deserialize preserves now not entitized newlines and escaping still can be enabled in options
            };
            using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
            {
                Serialize(xmlWriter, obj, options);
                xmlWriter.Flush();
            }
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> into the provided <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> used to write the XML document. The stream will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> must not be null.</exception>
        /// <exception cref="NotSupportedException"><para>Serialization is not supported with provided <paramref name="options"/></para>
        /// <para>- or -</para>
        /// <para>The stream does not support writing.</para></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is already closed.</exception>
        public static void Serialize(Stream stream, object? obj, XmlSerializationOptions options = defaultOptions)
        {
            if (stream == null!)
                Throw.ArgumentNullException(Argument.stream);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                CloseOutput = false,
                // NewLineHandling = NewLineHandling.Entitize //- entitizes only /r and not /n. Deserialize preserves now not entitized newlines and escaping still can be enabled in options
            };
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                Serialize(writer, obj, options);
                writer.Flush();
            }
        }

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// into an already existing <see cref="XElement"/> object given in <paramref name="parent"/> parameter
        /// with provided <paramref name="options"/>.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="parent">The parent under that the object will be saved. Its content can be deserialized by <see cref="DeserializeContent(XElement,object)"/> method.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="parent"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize(object,XmlSerializationOptions)"/> method.
        /// </remarks>
        public static void SerializeContent(XElement parent, object obj, XmlSerializationOptions options = defaultOptions)
            => new XElementSerializer(options).SerializeContent(parent, obj);

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// by an already opened <see cref="XmlWriter"/> object given in <paramref name="writer"/> parameter
        /// with provided <paramref name="options"/>.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will not be closed or flushed after serialization.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="writer"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize(XmlWriter,object,XmlSerializationOptions)"/> method.
        /// </remarks>
        public static void SerializeContent(XmlWriter writer, object obj, XmlSerializationOptions options = defaultOptions)
            => new XmlWriterSerializer(options).SerializeContent(writer, obj);

        /// <summary>
        /// Deserializes an XML content to an object.
        /// Works for the results of the <see cref="Serialize(object,XmlSerializationOptions)"/> method.
        /// </summary>
        /// <param name="content">XML content of the object.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        public static object? Deserialize(XElement content) => new XElementDeserializer(false).Deserialize(content);

        /// <summary>
        /// Deserializes an XML content to an object in safe mode.
        /// Works for the results of the <see cref="Serialize(object,XmlSerializationOptions)"/> method.
        /// <br/>See the security notes at the <strong>Remarks</strong> section of the <see cref="XmlSerializer"/> class for details about safe mode.
        /// </summary>
        /// <param name="content">XML content of the object.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="content"/> cannot be deserialized in safe mode.</exception>
        public static object? DeserializeSafe(XElement content) => new XElementDeserializer(true).Deserialize(content);

        /// <summary>
        /// Deserializes an object using the provided <see cref="XmlReader"/> in <paramref name="reader"/> parameter.
        /// </summary>
        /// <param name="reader">An <see cref="XmlReader"/> object to be used for deserialization.</param>
        /// <returns>The deserialized object.</returns>
        /// <remarks>
        /// <note>
        /// The <paramref name="reader"/> position must be <em>before</em> the content to deserialize.
        /// </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        public static object? Deserialize(XmlReader reader) => new XmlReaderDeserializer(false).Deserialize(reader);

        /// <summary>
        /// Deserializes an object using the provided <see cref="XmlReader"/> in <paramref name="reader"/> parameter.
        /// <br/>See the security notes at the <strong>Remarks</strong> section of the <see cref="XmlSerializer"/> class for details about safe mode.
        /// </summary>
        /// <param name="reader">An <see cref="XmlReader"/> object to be used for deserialization.</param>
        /// <returns>The deserialized object.</returns>
        /// <remarks>
        /// <note>
        /// The <paramref name="reader"/> position must be <em>before</em> the content to deserialize.
        /// </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <exception cref="InvalidOperationException">XML content cannot be deserialized in safe mode.</exception>
        public static object? DeserializeSafe(XmlReader reader) => new XmlReaderDeserializer(true).Deserialize(reader);

        /// <summary>
        /// Deserializes an object using the provided <see cref="TextReader"/> in <paramref name="reader"/> parameter.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> object to be used for deserialization. The reader is not closed after deserialization.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
#if NET35
        [SuppressMessage("Security", "CA3075:InsecureDTDProcessing", Justification = "False alarm for .NET 3.5, though the resolver is null also for that target.")]
#endif
        public static object? Deserialize(TextReader reader)
        {
            if (reader == null!)
                Throw.ArgumentNullException(Argument.reader);

            // using XmlTextReader instead of XmlReader.Create so we can avoid newlines to be normalized even if they are not entitized
            XmlTextReader xmlReader = new XmlTextReader(reader)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
#if !NET35
                DtdProcessing = DtdProcessing.Prohibit
#endif
            };

            return Deserialize(xmlReader);
        }

        /// <summary>
        /// Deserializes an object using the provided <see cref="TextReader"/> in <paramref name="reader"/> parameter.
        /// <br/>See the security notes at the <strong>Remarks</strong> section of the <see cref="XmlSerializer"/> class for details about safe mode.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> object to be used for deserialization. The reader is not closed after deserialization.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <exception cref="InvalidOperationException">XML content cannot be deserialized in safe mode.</exception>
#if NET35
        [SuppressMessage("Security", "CA3075:InsecureDTDProcessing", Justification = "False alarm for .NET 3.5, though the resolver is null also for that target.")]
#endif
        public static object? DeserializeSafe(TextReader reader)
        {
            if (reader == null!)
                Throw.ArgumentNullException(Argument.reader);

            // using XmlTextReader instead of XmlReader.Create so we can avoid newlines to be normalized even if they are not entitized
            XmlTextReader xmlReader = new XmlTextReader(reader)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
#if !NET35
                DtdProcessing = DtdProcessing.Prohibit
#endif
            };

            return DeserializeSafe(xmlReader);
        }

        /// <summary>
        /// Deserializes an object from the specified file passed in <paramref name="fileName"/> parameter.
        /// </summary>
        /// <param name="fileName">Name of the file that contains the serialized content.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
#if NET35
        [SuppressMessage("Security", "CA3075:InsecureDTDProcessing", Justification = "False alarm for .NET 3.5, though the resolver is null also for that target.")]
#endif
        public static object? Deserialize(string fileName)
        {
            if (fileName == null!)
                Throw.ArgumentNullException(Argument.fileName);

            // using XmlTextReader instead of XmlReader.Create so we can avoid newlines to be normalized even if they are not entitized
            using (var xmlReader = new XmlTextReader(fileName)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
#if !NET35
                DtdProcessing = DtdProcessing.Prohibit
#endif
            })
            {
                return Deserialize(xmlReader);
            }
        }

        /// <summary>
        /// Deserializes an object from the specified file passed in <paramref name="fileName"/> parameter.
        /// <br/>See the security notes at the <strong>Remarks</strong> section of the <see cref="XmlSerializer"/> class for details about safe mode.
        /// </summary>
        /// <param name="fileName">Name of the file that contains the serialized content.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <exception cref="InvalidOperationException">XML content cannot be deserialized in safe mode.</exception>
#if NET35
        [SuppressMessage("Security", "CA3075:InsecureDTDProcessing", Justification = "False alarm for .NET 3.5, though the resolver is null also for that target.")]
#endif
        public static object? DeserializeSafe(string fileName)
        {
            if (fileName == null!)
                Throw.ArgumentNullException(Argument.fileName);

            // using XmlTextReader instead of XmlReader.Create so we can avoid newlines to be normalized even if they are not entitized
            using (var xmlReader = new XmlTextReader(fileName)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
#if !NET35
                DtdProcessing = DtdProcessing.Prohibit
#endif
            })
            {
                return DeserializeSafe(xmlReader);
            }
        }

        /// <summary>
        /// Deserializes an object from the provided <see cref="Stream"/> in <paramref name="stream"/> parameter.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> object to be used for deserialization. The stream is not closed after deserialization.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
#if NET35
        [SuppressMessage("Security", "CA3075:InsecureDTDProcessing", Justification = "False alarm for .NET 3.5, though the resolver is null also for that target.")]
#endif
        public static object? Deserialize(Stream stream)
        {
            if (stream == null!)
                Throw.ArgumentNullException(Argument.stream);

            XmlTextReader xmlReader = new XmlTextReader(stream)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
#if !NET35
                DtdProcessing = DtdProcessing.Prohibit
#endif
            };

            //XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings
            //{
            //    ConformanceLevel = ConformanceLevel.Auto,
            //    IgnoreWhitespace = true,
            //    IgnoreComments = true
            //});
            return Deserialize(xmlReader);
        }

        /// <summary>
        /// Deserializes an object from the provided <see cref="Stream"/> in <paramref name="stream"/> parameter.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> object to be used for deserialization. The stream is not closed after deserialization.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <exception cref="InvalidOperationException">XML content cannot be deserialized in safe mode.</exception>
#if NET35
        [SuppressMessage("Security", "CA3075:InsecureDTDProcessing", Justification = "False alarm for .NET 3.5, though the resolver is null also for that target.")]
#endif
        public static object? DeserializeSafe(Stream stream)
        {
            if (stream == null!)
                Throw.ArgumentNullException(Argument.stream);

            XmlTextReader xmlReader = new XmlTextReader(stream)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
#if !NET35
                DtdProcessing = DtdProcessing.Prohibit
#endif
            };

            return DeserializeSafe(xmlReader);
        }

        /// <summary>
        /// Restores inner state of an already created object passed in <paramref name="obj"/> parameter based on a saved XML.
        /// Works for the results of the <see cref="SerializeContent(XElement,object,XmlSerializationOptions)"/> method and other <c>SerializeContent</c> overloads.
        /// </summary>
        /// <param name="obj">The already constructed object whose inner state has to be deserialized.</param>
        /// <param name="content">XML content of the object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="content"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="obj"/> must not be a value type.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        public static void DeserializeContent(XElement content, object obj) => new XElementDeserializer(false).DeserializeContent(content, obj);

        /// <summary>
        /// Restores inner state of an already created object passed in <paramref name="obj"/> parameter based on a saved XML.
        /// Works for the results of the <see cref="SerializeContent(XElement,object,XmlSerializationOptions)"/> method and other <c>SerializeContent</c> overloads.
        /// <br/>See the security notes at the <strong>Remarks</strong> section of the <see cref="XmlSerializer"/> class for details about safe mode.
        /// </summary>
        /// <param name="obj">The already constructed object whose inner state has to be deserialized.</param>
        /// <param name="content">XML content of the object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="content"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="obj"/> must not be a value type.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="content"/> cannot be deserialized in safe mode.</exception>
        public static void DeserializeContentSafe(XElement content, object obj) => new XElementDeserializer(true).DeserializeContent(content, obj);

        /// <summary>
        /// Restores inner state of an already created object passed in <paramref name="obj"/> parameter based on a saved XML.
        /// Works for the results of the <see cref="SerializeContent(XmlWriter,object,XmlSerializationOptions)"/> method and other <c>SerializeContent</c> overloads.
        /// </summary>
        /// <param name="obj">The already constructed object whose inner state has to be deserialized.</param>
        /// <param name="reader">An <see cref="XmlReader"/> instance to be used to read the XML content. Reader must be in at correct position for the successful deserialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="obj"/> must not be a value type.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        public static void DeserializeContent(XmlReader reader, object obj) => new XmlReaderDeserializer(false).DeserializeContent(reader, obj);

        /// <summary>
        /// Restores inner state of an already created object passed in <paramref name="obj"/> parameter based on a saved XML.
        /// Works for the results of the <see cref="SerializeContent(XmlWriter,object,XmlSerializationOptions)"/> method and other <c>SerializeContent</c> overloads.
        /// <br/>See the security notes at the <strong>Remarks</strong> section of the <see cref="XmlSerializer"/> class for details about safe mode.
        /// </summary>
        /// <param name="obj">The already constructed object whose inner state has to be deserialized.</param>
        /// <param name="reader">An <see cref="XmlReader"/> instance to be used to read the XML content. Reader must be in at correct position for the successful deserialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="obj"/> must not be a value type.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="InvalidOperationException">XML content cannot be deserialized in safe mode.</exception>
        public static void DeserializeContentSafe(XmlReader reader, object obj) => new XmlReaderDeserializer(true).DeserializeContent(reader, obj);

        #endregion
    }
}
