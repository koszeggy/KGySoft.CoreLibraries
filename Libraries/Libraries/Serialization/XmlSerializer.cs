using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Threading;
using KGySoft.Libraries.Annotations;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    using System.Runtime.Serialization;

    /// <summary>
    /// <see cref="XmlSerializer"/> makes possible serializing and deserializing object instances into/from XML content. The class class contans various overloads to support serializing directly into file or by
    /// <see cref="XElement"/>, <see cref="XmlWriter"/>, any <see cref="TextWriter"/> and any <see cref="Stream"/> implementations.
    /// </summary>
    /// <remarks>
    /// <see cref="XmlSerializer"/> supports serialization of any simple types and complex objects with their properties (if <see cref="XmlSerializationOptions.RecursiveSerializationAsFallback"/> is enabled 
    /// properties can be complex nested types),  arrays and any nested non-readonly collection (if collection implements either the non-generic <see cref="IList"/> or the generic <see cref="Collection{T}"/> interface).
    /// By default, it processes <see cref="IXmlSerializable"/> implementations, and as a fallback, it can serialize anything by binary serialization (if <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/> is enabled).
    /// Unlike <see cref="System.Xml.Serialization.XmlSerializer"/>, <see cref="XmlSerializer"/> supports non-public types, read-only properties of variable collections, too.
    /// <para>
    /// Problems with the original <see cref="System.Xml.Serialization.XmlSerializer"/>:
    /// <list type="bullet">
    /// <item><term>Code generation</term><description><see cref="System.Xml.Serialization.XmlSerializer"/> analyzes the objects, generates C# files and compiles them,
    /// which requires special access rights. In case of an error a standard C# compiler error may be thrown. In case of some collections generated code is syntactically wrong.</description></item>
    /// <item><term>Control</term><description>Controlling the serialization can be achieved via using a lot of attributes (or by implementing <see cref="IXmlSerializable"/> interface, which is supported also by <see cref="XmlSerializer"/>).
    /// If the source code of the class is not available serialization can be impossible by system <see cref="System.Xml.Serialization.XmlSerializer"/>.</description></item>
    /// <item><term>Design</term><description>In some cases classes must have read-write properties even for collections, otherwise serialization would fail.
    /// <see cref="XmlSerializer"/> does not require setter accessor for a collection property if the property is not <see langword="null"/> after initialization.</description></item>
    /// <item><term>Collections with base element type</term><description>If the element type of a collection is a base type or an interface, then the system serializer throws an exception for derived element types
    /// suggesting that <see cref="XmlIncludeAttribute"/> should be defined for all possible derived types. Unfortunately this attribute is applicable only for possible types of properties/fields
    /// but not for collection elements. And many times possible derived types simply cannot be predefined (for example <see cref="List{T}"/> with <see cref="object"/> type paramerer).</description></item>
    /// <item>Strings<term></term><description>If a string contains only whitespaces, then system <see cref="System.Xml.Serialization.XmlSerializer"/> cannot deserialize it properly.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class XmlSerializer
    {
        #region Constants

        private const XmlSerializationOptions DefaultOptions = XmlSerializationOptions.BinarySerializationAsFallback | XmlSerializationOptions.CompactSerializationOfPrimitiveArrays | XmlSerializationOptions.EscapeNewlineCharacters;

        #endregion

        #region Methods

        #region Public Methods

        #region Serialization - whole object

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> parameter into a new <see cref="XElement"/> object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <returns>An <see cref="XElement"/> instance that contains the serialized object.
        /// Result can be deserialized by <see cref="Deserialize(XElement)"/> method.</returns>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        public static XElement Serialize(object obj, XmlSerializationOptions options = DefaultOptions)
            => new XElementSerializer(options).Serialize(obj);

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> by the provided <see cref="XmlWriter"/> object.
        /// </summary>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The state of <paramref name="writer"/> is wrong or writer is closed.</exception>
        /// <exception cref="EncoderFallbackException">There is a character in the buffer that is a valid XML character but is not valid for the output encoding.
        /// For example, if the output encoding is ASCII but public properties of a class contain non-ASCII characters, an <see cref="EncoderFallbackException"/> is thrown.
        /// Such characters are escaped by character entity references in values when possible.</exception>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with provided <paramref name="options"/></exception>
        public static void Serialize(XmlWriter writer, object obj, XmlSerializationOptions options = DefaultOptions)
            => new XmlWriterSerializer(options).Serialize(writer, obj);

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> into the specified <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">Name of the file to create for serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> must not be null.</exception>
        /// <exception cref="IOException">File cannot be created or write error.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        public static void Serialize(string fileName, object obj, XmlSerializationOptions options = DefaultOptions)
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
        /// <br/>Default value: <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The writer is closed.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        public static void Serialize(TextWriter writer, object obj, XmlSerializationOptions options = DefaultOptions)
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
        /// <br/>Default value: <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> must not be null.</exception>
        /// <exception cref="NotSupportedException"><para>Serialization is not supported with provided <paramref name="options"/></para>
        /// <para>- or -</para>
        /// <para>The stream does not support writing.</para></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is already closed.</exception>
        public static void Serialize(Stream stream, object obj, XmlSerializationOptions options = DefaultOptions)
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
        /// <br/>Default value: <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="parent"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize(object,XmlSerializationOptions)"/> method.
        /// </remarks>
        public static void SerializeContent(XElement parent, object obj, XmlSerializationOptions options = DefaultOptions)
            => new XElementSerializer(options).SerializeContent(parent, obj);

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// by an already opened <see cref="XmlWriter"/> object given in <paramref name="writer"/> parameter
        /// with provided <paramref name="options"/>.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will not be closed after serialization.</param>
        /// <param name="options">Options for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="XmlSerializationOptions.BinarySerializationAsFallback"/>, <see cref="XmlSerializationOptions.CompactSerializationOfPrimitiveArrays"/>, <see cref="XmlSerializationOptions.EscapeNewlineCharacters"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="writer"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize(XmlWriter,object,XmlSerializationOptions)"/> method.
        /// </remarks>
        public static void SerializeContent(XmlWriter writer, object obj, XmlSerializationOptions options = DefaultOptions)
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

        #region Internal Extension Methods

        internal static string Unescape(this string s)
        {
            StringBuilder result = new StringBuilder(s);

            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == '\\')
                {
                    if (i + 1 == result.Length)
                        throw new ArgumentException(Res.Get(Res.XmlInvalidEscapedContent, s));

                    // escaped backslash
                    if (result[i + 1] == '\\')
                    {
                        result.Remove(i, 1);
                    }
                    // escaped character
                    else
                    {
                        if (i + 4 >= result.Length)
                            throw new ArgumentException(Res.Get(Res.XmlInvalidEscapedContent, s));

                        string escapedChar = result.ToString(i + 1, 4);
                        ushort charValue;
                        if (!UInt16.TryParse(escapedChar, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out charValue))
                            throw new ArgumentException(Res.Get(Res.XmlInvalidEscapedContent, s));

                        result.Replace("\\" + escapedChar, ((char)charValue).ToString(null), i, 5);
                    }
                }
            }

            return result.ToString();
        }

        internal static Type GetElementType([NoEnumeration]this IEnumerable collection)
        {
            foreach (Type i in collection.GetType().GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
                    return i.GetGenericArguments()[0];
            }
            if (collection is IDictionary)
                return typeof(DictionaryEntry);
            return typeof(object);
        }

        internal static bool IsRecursiveSerializationEnabled(this XmlSerializationOptions options)
            => (options & XmlSerializationOptions.RecursiveSerializationAsFallback) != XmlSerializationOptions.None;

#pragma warning disable 618, 612 // Disabling warning for obsolete enum member because this must be still handled
        internal static bool IsForcedSerializationValueTypesEnabled(this XmlSerializationOptions options)
            => (options & XmlSerializationOptions.ForcedSerializationValueTypesAsFallback) != XmlSerializationOptions.None;
#pragma warning restore 618, 612

        internal static bool IsBinarySerializationEnabled(this XmlSerializationOptions options) 
            => (options & XmlSerializationOptions.BinarySerializationAsFallback) != XmlSerializationOptions.None;

        internal static bool IsCompactSerializationValueTypesEnabled(this XmlSerializationOptions options) 
            => (options & XmlSerializationOptions.CompactSerializationOfStructures) != XmlSerializationOptions.None;

        internal static BinarySerializationOptions ToBinarySerializationOptions(this XmlSerializationOptions options)
        {
            // compact, recursive: always enabled when binary serializing because they cause no problem
            BinarySerializationOptions result = BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback; // | CompactSerializationOfBoolCollections

            // no fully qualified names -> omitting even in binary serializer
            if ((options & XmlSerializationOptions.FullyQualifiedNames) == XmlSerializationOptions.None)
            {
                result |= BinarySerializationOptions.OmitAssemblyQualifiedNames;
            }
            return result;
        }

        #endregion

        #endregion
    }
}

