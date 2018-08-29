﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Threading;
using KGySoft.Libraries.Annotations;
using KGySoft.Libraries.Collections;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    /// <summary>
    /// <see cref="XmlSerializer"/> makes possible serializing and deserializing object instances into/from XML content. The class contains various overloads to support serializing directly into file or by
    /// <see cref="XElement"/>, <see cref="XmlWriter"/>, any <see cref="TextWriter"/> and any <see cref="Stream"/> implementations.
    /// <br/>See the <strong>Remarks</strong> section to see the differences compared to <a href="https://msdn.microsoft.com/en-us/library/System.Xml.Serialization.XmlSerializer.aspx" target="_blank">System.Xml.Serialization.XmlSerializer</a> class.
    /// </summary>
    /// <remarks>
    /// <para><see cref="XmlSerializer"/> supports serialization of any simple types and complex objects with their public properties and fields as well as some collections.
    /// <note>Unlike the <a href="https://msdn.microsoft.com/en-us/library/System.Xml.Serialization.XmlSerializer.aspx" target="_blank">System.Xml.Serialization.XmlSerializer</a> class,
    /// this <see cref="XmlSerializer"/> is not designed for customizing output format (though <see cref="IXmlSerializable"/> implementations are considered). Instead, this class is
    /// designed to support XML serialization of any type as long as they have a default constructor and their state can be fully restored by their public fields and properties.</note>
    /// </para>
    /// <para>Several <a href="https://msdn.microsoft.com/en-us/library/System.ComponentModel.aspx" target="_blank">System.ComponentModel</a> techniques are supported,
    /// which also makes possible to use the <see cref="XmlSerializer"/> for types that can be edited in a property grid, such as components, configurations or any types in a custom designer.
    /// The supported component model attributes and techniques:
    /// <list type="bullet">
    /// <item><term><see cref="DesignerSerializationVisibilityAttribute"/></term><description>Use value <see cref="DesignerSerializationVisibility.Hidden"/> for public field or property to prevent its serialization
    /// and use <see cref="DesignerSerializationVisibility.Content"/> value to explicitly express that the property value can be serialized recursively (see also <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/>) option.</description></item>
    /// <item><term><see cref="DefaultValueAttribute"/></term><description>If the value of a public property or field equals to the value specified by this attribute, then its value will not be serialized
    /// (see also <see cref="XmlSerializationOptions.IgnoreDefaultValueAttribute"/> and <see cref="XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback"/> options).</description></item>
    /// <item><term><c>ShouldSerialize...</c> methods</term><description>If the type being serialized has an instance method with no parameters and of <see cref="bool"/> return type (can be private as well) named <c>ShouldSerializePropertyName</c> where <c>PropertyName</c> is the name of a property or field,
    /// then its return value determines whether the member should be serialized. This is the same technique used in some designers and is supported by the <a href="https://msdn.microsoft.com/en-us/library/System.Windows.Forms.PropertyGrid.aspx" target="_blank">System.Windows.Forms.PropertyGrid</a> control
    /// as well (see also <see cref="XmlSerializationOptions.IgnoreShouldSerialize"/> option).</description></item>
    /// <item><term><see cref="TypeConverterAttribute"/></term><description>This attribute is supported both for types and property/field members. If a <see cref="TypeConverter"/> supports serialization to and from <see cref="string"/> type,
    /// then it will be used for serializing its value (see also <see cref="Reflector.RegisterTypeConverter{T,TC}">Reflector.RegisterTypeConverter</see> method).</description></item>
    /// </list>
    /// </para>
    /// <para>Basically types with default constructor are supported. However, collections of read-only properties can be deserialized, if they implement <see cref="ICollection{T}"/> or <see cref="IList"/> interfaces
    /// and the property value is not <see langword="null"/> after creating the instance.
    /// <note>Objects without a default constructor can be serialized at root level by the <see cref="O:KGySoft.Libraries.Serialization.SerializeContent">SerializeContent</see> methods into an already existing
    /// <see cref="XElement"/> node or by an <see cref="XmlWriter"/>, which already opened and XML element before calling the method. When deserializing, the result object should be created by the caller, and the content can be deserialized
    /// by the <see cref="O:KGySoft.Libraries.Serialization.DeserializeContent">DeserializeContent</see> methods.</note>
    /// </para>
    /// <para><strong>Options:</strong>
    /// By specifying the <see cref="XmlSerializationOptions"/> argument in the <see cref="O:KGySoft.Libraries.Serialization.Serialize">Serialize</see> and <see cref="O:KGySoft.Libraries.Serialization.SerializeContent">SerializeContent</see>
    /// methods you can control the default behavior of serialization. The default options ensure that only those types are serialized, which are guaranteed to be able to deserialized perfectly. Such types are:
    /// <list type="bullet">
    /// <item><term>Natively supported types</term><description>Primitive types along with their <see cref="Nullable{T}"/> version and the most common framework types such as <see cref="Enum"/> instances, <see cref="DateTime"/>, <see cref="DateTimeOffset"/>,
    /// <see cref="TimeSpan"/> and even <see cref="Type"/> itself as long as it is not a standalone generic parameter.</description></item>
    /// <item><term>Types with <see cref="TypeConverter"/></term><description>If the converter supports serializing to and from <see cref="string"/> type.</description></item>
    /// <item><term>Simple objects</term><description>All of their instance fields and properties are public (for properties: both accessors) and non read-only.</description></item>
    /// <item><term>Collections</term><description><see cref="Array"/>, <see cref="List{T}"/>, <see cref="ArrayList"/>, <see cref="LinkedList{T}"/>, <see cref="Queue{T}"/>, <see cref="Queue"/>, <see cref="Stack{T}"/>, <see cref="Stack"/>,
    /// <see cref="BitArray"/>, <see cref="CircularList{T}"/>, <see cref="ConcurrentBag{T}"/>, <see cref="ConcurrentQueue{T}"/> and <see cref="ConcurrentStack{T}"/> instances are supported by default options. To support other collections
    /// you can use fallback options, for example <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/>.
    /// <note>The reason of fallback options or attributes have to be used even for simple collections such as <see cref="Dictionary{TKey,TValue}"/> is that they can be instantiated by special settings such as an equality comparer,
    /// which cannot be retrieved by the public members when the collection is being serialized. On deserialization always the default constructor is used (unless the collection is returned by a read-only property, in which case the already
    /// existing instance is used) so the collection is always instantiated by the default settings.</note>
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>If a type cannot be serialized with the currently used options a <see cref="SerializationException"/> will be thrown.</para>
    /// <para>You can use <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/> option to enable recursive serialization of every type of objects and collections. A collection type can be serialized if
    /// it implements the <see cref="ICollection{T}"/> or <see cref="IList"/> interface, is not read-only and has a default constructor; or if it has an initializer constructor with a single parameter that can accept an <see cref="Array"/>
    /// or <see cref="List{T}"/> instance (non-dictionaries) or a <see cref="Dictionary{TKey,TValue}"/> instance (dictionary collections). Non-collection types must have a parameterless constructor to be able to be deserialized.
    /// <note type="caution">Enabling the <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/> option does not guarantee that the deserialized instances will be the same as the original ones.</note>
    /// </para>
    /// <para>If <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/> option is enabled, then types without a native support and appropriate <see cref="TypeConverter"/> will be serialized into a binary stream, which
    /// will be stored in the result XML. Though this provides the best compatibility of any type, it hides the whole inner structure of the serialized object. If a root level object without native support is serialized by the
    /// <see cref="O:KGySoft.Libraries.Serialization.Serialize">Serialize</see> using the <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, then the whole XML result will be a single node with the binary content.
    /// <note>To use binary serialization only for some types or properties you can use specify the <see cref="BinaryTypeConverter"/> by the <see cref="TypeConverterAttribute"/> for a property or type
    /// (or you can use the <see cref="Reflector.RegisterTypeConverter{T,TC}">Reflector.RegisterTypeConverter</see> method for types).</note>
    /// </para>
    /// <para>See the <see cref="XmlSerializationOptions"/> enumeration for further options.</para>
    /// <para><strong>New features and improvements</strong> compared to <a href="https://msdn.microsoft.com/en-us/library/System.Xml.Serialization.XmlSerializer.aspx" target="_blank">System.Xml.Serialization.XmlSerializer</a>:
    /// <list type="bullet">
    /// <item>Strings<term></term><description>If a string contains only white spaces, then system <see cref="System.Xml.Serialization.XmlSerializer"/> cannot deserialize it properly. <see cref="string"/> instances containing
    /// invalid UTF-16 code points are also cannot be serialized.</description></item>
    /// <item><term>Collections with base element type</term><description>If the element type of a collection is a base type or an interface, then the system serializer throws an exception for derived element types
    /// suggesting that <see cref="XmlIncludeAttribute"/> should be defined for all possible derived types. Unfortunately this attribute is applicable only for possible types of properties/fields
    /// but not for collection elements. And in many cases it simply cannot be predefined in advance what derived types will be used at run-time.</description></item>
    /// <item><term>Collections with read-only properties</term><description>Usually collection properties can be read-only. But to be able to use the system serializer we need to define a setter for such properties; otherwise, serialization may fail.
    /// <see cref="XmlSerializer"/> does not require setter accessor for a collection property if the property is not <see langword="null"/> after initialization and can be populated by using the usual collection interfaces.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class XmlSerializer
    {
        #region Constants

        #region Private Constants

        private const XmlSerializationOptions defaultOptions = XmlSerializationOptions.CompactSerializationOfPrimitiveArrays | XmlSerializationOptions.EscapeNewlineCharacters;

        #endregion

        #region Internal Constants

        internal const string NamespaceXml = "xml";

        internal const string AttributeSpace = "space";
        internal const string AttributeCrc = "crc";
        internal const string AttributeEscaped = "escaped";
        internal const string AttributeFormat = "format";
        internal const string AttributeType = "type";
        internal const string AttributeLength = "length";
        internal const string AttributeDim = "dim";

        internal const string AttributeValuePreserve = "preserve";
        internal const string AttributeValueTrue = "true";
        internal const string AttributeValueCustom = "custom";
        internal const string AttributeValueStructBinary = "structBinary";
        internal const string AttributeValueBinary = "binary";

        internal const string ElementObject = "object";
        internal const string ElementItem = "item";

        internal const string MethodShouldSerialize = "ShouldSerialize";

        #endregion

        #endregion

        #region Methods

        #region Serialization - whole object

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
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        public static XElement Serialize(object obj, XmlSerializationOptions options = defaultOptions)
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
        public static void Serialize(XmlWriter writer, object obj, XmlSerializationOptions options = defaultOptions)
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
        public static void Serialize(string fileName, object obj, XmlSerializationOptions options = defaultOptions)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), Res.Get(Res.ArgumentNull));

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
        public static void Serialize(TextWriter writer, object obj, XmlSerializationOptions options = defaultOptions)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer), Res.Get(Res.ArgumentNull));

            XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
            {
                Indent = true,
                // NewLineHandling = NewLineHandling.Entitize - entitizes only /r and not /n. Deserialize preserves now not entitized newlines and escaping still can be enabled in options
            });
            Serialize(xmlWriter, obj, options);
            xmlWriter.Flush();
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
        public static void Serialize(Stream stream, object obj, XmlSerializationOptions options = defaultOptions)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), Res.Get(Res.ArgumentNull));

            XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Indent = true,
                // NewLineHandling = NewLineHandling.Entitize - entitizes only /r and not /n. Deserialize preserves now not entitized newlines and escaping still can be enabled in options
            });
            Serialize(writer, obj, options);
            writer.Flush();
        }

        #endregion

        #region Serialization - content

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
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
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
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize(XmlWriter,object,XmlSerializationOptions)"/> method.
        /// </remarks>
        public static void SerializeContent(XmlWriter writer, object obj, XmlSerializationOptions options = defaultOptions)
            => new XmlWriterSerializer(options).SerializeContent(writer, obj);

        #endregion

        #region Deserialization - whole object

        /// <summary>
        /// Deserializes an XML content to an object.
        /// Works for results of <see cref="Serialize(object,XmlSerializationOptions)"/> method.
        /// </summary>
        /// <param name="content">XML content of the object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        public static object Deserialize(XElement content) => XElementDeserializer.Deserialize(content);

        /// <summary>
        /// Deserializes an object using the provided <see cref="XmlReader"/> in <paramref name="reader"/> parameter.
        /// </summary>
        /// <remarks>
        /// <note>
        /// The <paramref name="reader"/> position must be <em>before</em> the content to deserialize.
        /// </note>
        /// </remarks>
        /// <param name="reader">An <see cref="XmlReader"/> object to be used for deserialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is incosistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(XmlReader reader) => XmlReaderDeserializer.Deserialize(reader);

        /// <summary>
        /// Deserializes an object using the provided <see cref="TextReader"/> in <paramref name="reader"/> parameter.
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> object to be used for deserialization. The reader is not closed after deserialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is incosistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader), Res.Get(Res.ArgumentNull));

            // using XmlTextReader instead of XmlReader.Create so we can avoid newlines to be normalized even if they are not entitized
            XmlTextReader xmlReader = new XmlTextReader(reader)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
            };

            return Deserialize(xmlReader);
        }

        /// <summary>
        /// Deserializes an object from the specified file passed in <paramref name="fileName"/> parameter.
        /// </summary>
        /// <param name="fileName">Name of the file that contains the serialized content.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), Res.Get(Res.ArgumentNull));

            // using XmlTextReader instead of XmlReader.Create so we can avoid newlines to be normalized even if they are not entitized
            XmlTextReader xmlReader = new XmlTextReader(fileName)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
            };

            return Deserialize(xmlReader);
        }

        /// <summary>
        /// Deserializes an object from the provided <see cref="Stream"/> in <paramref name="stream"/> parameter.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> object to be used for deserialization. The stream is not closed after deserialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is incosistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), Res.Get(Res.ArgumentNull));

            XmlTextReader xmlReader = new XmlTextReader(stream)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null
            };

            //XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings
            //{
            //    ConformanceLevel = ConformanceLevel.Auto,
            //    IgnoreWhitespace = true,
            //    IgnoreComments = true
            //});
            return Deserialize(xmlReader);
        }

        #endregion

        #region Deserialization - content

        /// <summary>
        /// Restores inner state of an already created object passed in <paramref name="obj"/> parameter based on a saved XML.
        /// Works for results of <see cref="SerializeContent(XElement,object,XmlSerializationOptions)"/> and other <c>SerializeContent</c> overloads.
        /// </summary>
        /// <param name="obj">The already constructed object whose inner state has to be deserialized.</param>
        /// <param name="content">XML content of the object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="content"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="obj"/> must not be a value type.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        public static void DeserializeContent(XElement content, object obj) => XElementDeserializer.DeserializeContent(content, obj);

        /// <summary>
        /// Restores inner state of an already created object passed in <paramref name="obj"/> parameter based on a saved XML.
        /// Works for results of <see cref="SerializeContent(XmlWriter,object,XmlSerializationOptions)"/> and other <c>SerializeContent</c> overloads.
        /// </summary>
        /// <param name="obj">The already constructed object whose inner state has to be deserialized.</param>
        /// <param name="reader">An <see cref="XmlReader"/> instance to be used to read the XML content. Reader must be in at correct position for the successful deserialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="obj"/> must not be a value type.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is inconsistent or corrupt.</exception>
        public static void DeserializeContent(XmlReader reader, object obj) => XmlReaderDeserializer.DeserializeContent(reader, obj);

        #endregion

        #endregion
    }
}

