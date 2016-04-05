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

        #region Fields

        private static readonly object syncRootSerialize = new object(); // to lock on serObjects for detecting circular references
        private static volatile int serializationLevel;
        private static HashSet<object> serObjects;

        #endregion

        #region Methods

        #region Public Methods

        #region Serialization - whole object

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> parameter into a new <see cref="XElement"/> object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">Options for serialization.</param>
        /// <returns>An <see cref="XElement"/> instance that contains the serialized object.
        /// Result can be deserialized by <see cref="Deserialize(XElement)"/> method.</returns>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        public static XElement Serialize(object obj, XmlSerializationOptions options)
        {
            //StringBuilder sb = new StringBuilder();
            //using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
            //{
            //    Serialize(writer, obj, options);
            //    writer.Flush();
            //}

            //return XElement.Parse(sb.ToString());

            // TODO: performance test by the new (above) and old (below) code. Re-enable XElement codes only when they are faster.
            XElement result = new XElement("object");

            if (obj == null)
                return result;

            Type objType = obj.GetType();
            if (objType.IsCollection())
            {
                if (!objType.IsReadWriteCollection(obj))
                    throw new NotSupportedException(Res.Get(Res.XmlSerializeReadOnlyRoot, objType));
                SerializeCollection(obj as IEnumerable, true, result, options, DesignerSerializationVisibility.Visible);
            }
            else if (!TrySerializeObject(obj, true, result, objType, options, DesignerSerializationVisibility.Visible))
                throw new SerializationException(Res.Get(Res.XmlCannotSerialize, objType, options));
            return result;
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> parameter into a new <see cref="XElement"/> object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>An <see cref="XElement"/> instance that contains the serialized object.</returns>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        public static XElement Serialize(object obj)
        {
            return Serialize(obj, DefaultOptions);
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> by the provided <see cref="XmlWriter"/> object.
        /// </summary>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">The options to be used for serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The state of <paramref name="writer"/> is wrong or writer is closed.</exception>
        /// <exception cref="EncoderFallbackException">There is a character in the buffer that is a valid XML character but is not valid for the output encoding.
        /// For example, if the output encoding is ASCII but public properties of a class contain non-ASCII characters, an <see cref="EncoderFallbackException"/> is thrown.
        /// Such characters are escaped by character entity references in values when possible.</exception>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with provided <paramref name="options"/></exception>
        public static void Serialize(XmlWriter writer, object obj, XmlSerializationOptions options)
        {
            if (writer == null)
                throw new ArgumentNullException("writer", Res.Get(Res.ArgumentNull));

            writer.WriteStartElement("object");
            if (obj == null)
            {
                writer.WriteEndElement();
                writer.Flush();
                return;
            }

            Type objType = obj.GetType();
            if (objType.IsCollection())
            {
                if (!objType.IsReadWriteCollection(obj))
                    throw new NotSupportedException(Res.Get(Res.XmlSerializeReadOnlyRoot, objType));
                SerializeCollection(obj as IEnumerable, true, writer, options, DesignerSerializationVisibility.Visible);
                writer.WriteFullEndElement();
                writer.Flush();
                return;
            }
            else if (TrySerializeObject(obj, true, writer, objType, options, DesignerSerializationVisibility.Visible))
            {
                writer.WriteFullEndElement();
                writer.Flush();
                return;
            }

            throw new SerializationException(Res.Get(Res.XmlCannotSerialize, objType, options));
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> by the provided <see cref="XmlWriter"/> object using default options.
        /// </summary>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The state of <paramref name="writer"/> is wrong or writer is closed.</exception>
        /// <exception cref="EncoderFallbackException">There is a character in the buffer that is a valid XML character but is not valid for the output encoding.
        /// For example, if the output encoding is ASCII but public properties of a class contain non-ASCII characters, an <see cref="EncoderFallbackException"/> is thrown.
        /// Such characters are escaped by character entity references in values when possible.</exception>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with default options. Use <see cref="Serialize(XmlWriter,object,XmlSerializationOptions)"/> overload instead.</exception>
        public static void Serialize(XmlWriter writer, object obj)
        {
            Serialize(writer, obj, DefaultOptions);
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> into the specified <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">Name of the file to create for serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">The options to be used for serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> must not be null.</exception>
        /// <exception cref="IOException">File cannot be created or write error.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        public static void Serialize(string fileName, object obj, XmlSerializationOptions options)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName", Res.Get(Res.ArgumentNull));

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
        /// Serializes the object passed in <paramref name="obj"/> into the specified <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">Name of the file to create for serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The writer is closed.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with default options.
        /// Use <see cref="Serialize(TextWriter,object,XmlSerializationOptions)"/> overload instead.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        public static void Serialize(string fileName, object obj)
        {
            Serialize(fileName, obj, DefaultOptions);
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> by the provided <see cref="TextWriter"/> object.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> implementation (for example, a <see cref="StringWriter"/>) that will be used for serialization.
        /// The writer will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">The options to be used for serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The writer is closed.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        public static void Serialize(TextWriter writer, object obj, XmlSerializationOptions options)
        {
            if (writer == null)
                throw new ArgumentNullException("writer", Res.Get(Res.ArgumentNull));

            XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
            {
                Indent = true,
                NewLineHandling = NewLineHandling.Entitize
            });
            //XmlTextWriter xmlWriter = new XmlTextWriter(writer)
            //{
            //    Formatting = Formatting.Indented,
            //    Indentation = 2,
            //};
            Serialize(xmlWriter, obj, options);
            xmlWriter.Flush();
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> by the provided <see cref="TextWriter"/> object.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> implementation (for example, a <see cref="StringWriter"/>) that will be used for serialization.
        /// The writer will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> must not be null.</exception>
        /// <exception cref="InvalidOperationException">The writer is closed.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with default options.
        /// Use <see cref="Serialize(TextWriter,object,XmlSerializationOptions)"/> overload instead.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        public static void Serialize(TextWriter writer, object obj)
        {
            Serialize(writer, obj, DefaultOptions);
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> into the provided <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> used to write the XML document. The stream will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <param name="options">The options to be used for serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> must not be null.</exception>
        /// <exception cref="NotSupportedException"><para>Serialization is not supported with provided <paramref name="options"/></para>
        /// <para>- or -</para>
        /// <para>The stream does not support writing.</para></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="IOException">An I/O error occured.</exception>
        /// <exception cref="ObjectDisposedException">The stream is already closed.</exception>
        public static void Serialize(Stream stream, object obj, XmlSerializationOptions options)
        {
            if (stream == null)
                throw new ArgumentNullException("stream", Res.Get(Res.ArgumentNull));

            XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Indent = true,
                NewLineHandling = NewLineHandling.Entitize
            });
            Serialize(writer, obj, options);
            writer.Flush();
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> into the provided <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> used to write the XML document. The stream will not be closed after serialization.</param>
        /// <param name="obj">The <see cref="object"/> to serialize.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> must not be null.</exception>
        /// <exception cref="NotSupportedException"><para>Serialization is not supported with default options. Use <see cref="Serialize(Stream,object,XmlSerializationOptions)"/> overload instead.</para>
        /// <para>- or -</para>
        /// <para>The stream does not support writing.</para></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="IOException">An I/O error occured.</exception>
        /// <exception cref="ObjectDisposedException">The stream is already closed.</exception>
        public static void Serialize(Stream stream, object obj)
        {
            Serialize(stream, obj, DefaultOptions);
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
        /// <param name="options">Options for serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="parent"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then  use the overloads of <see cref="Serialize(object)"/> methods.
        /// </remarks>
        public static void SerializeContent(XElement parent, object obj, XmlSerializationOptions options)
        {
            SerializeComponent(obj, parent, options);
        }

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// into an already existing <see cref="XElement"/> object given in <paramref name="parent"/> parameter.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="parent">The parent under that the object will be saved. Its content can be deserialized by <see cref="DeserializeContent(XElement,object)"/> method.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="parent"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with default options. Try to use <see cref="SerializeContent(XElement,object,XmlSerializationOptions)"/> overload.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then  use the overloads of <see cref="Serialize(object)"/> methods.
        /// </remarks>
        public static void SerializeContent(XElement parent, object obj)
        {
            SerializeContent(parent, obj, DefaultOptions);
        }

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// by an already opened <see cref="XmlWriter"/> object given in <paramref name="writer"/> parameter
        /// with provided <paramref name="options"/>.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will not be closed after serialization.</param>
        /// <param name="options">Options for serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="writer"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the overloads of <see cref="Serialize(object)"/> methods.
        /// </remarks>
        public static void SerializeContent(XmlWriter writer, object obj, XmlSerializationOptions options)
        {
            SerializeComponent(obj, writer, options);
        }

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// by an already opened <see cref="XmlWriter"/> object given in <paramref name="writer"/> parameter.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will not be closed after serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="writer"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with default options. Try to use <see cref="SerializeContent(XmlWriter,object,XmlSerializationOptions)"/> overload.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.
        /// <br/>- or -
        /// <br/><paramref name="writer"/> is not in the correct state.
        /// </exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the overloads of <see cref="Serialize(object)"/> methods.
        /// </remarks>
        public static void SerializeContent(XmlWriter writer, object obj)
        {
            SerializeContent(writer, obj, DefaultOptions);
        }

        #endregion

        #region Deserialization - whole object

        /// <summary>
        /// Deserializes an XML content to an object.
        /// Works for results of <see cref="Serialize(object)"/> and <see cref="Serialize(object,KGySoft.Libraries.Serialization.XmlSerializationOptions)"/> methods.
        /// </summary>
        /// <param name="content">XML content of the object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is incosistent or corrupt.</exception>
        public static object Deserialize(XElement content)
        {
            if (content == null)
                throw new ArgumentNullException("content", Res.Get(Res.ArgumentNull));

            if (content.Name.LocalName != "object")
                throw new ArgumentException(Res.Get(Res.XmlRootExpected, content.Name.LocalName), "content");

            if (content.IsEmpty)
                return null;

            XAttribute attrType = content.Attribute("type");

            Type objType = null;
            if (attrType != null)
            {
                objType = Reflector.ResolveType(attrType.Value);
                if (objType == null)
                    throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType.Value));
            }

            object result;
            if (!TryDeserializeObject(objType, content, out result))
            {
                if (attrType == null)
                    throw new ArgumentException(Res.Get(Res.XmlRootTypeMissing), "content");

                throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));
            }
            return result;
        }

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
        public static object Deserialize(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader", Res.Get(Res.ArgumentNull));

            ReadToNodeType(reader, XmlNodeType.Element);
            if (reader.Name != "object")
                throw new ArgumentException(Res.Get(Res.XmlRootExpected, reader.Name), "reader");

            if (reader.IsEmptyElement)
                return null;

            string attrType = reader["type"];
            Type objType = null;
            if (attrType != null)
            {
                objType = Reflector.ResolveType(attrType);
                if (objType == null)
                    throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType));
            }

            object result;
            if (!TryDeserializeObject(objType, reader, out result))
            {
                if (attrType == null)
                    throw new ArgumentException(Res.Get(Res.XmlRootTypeMissing), "reader");

                throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));
            }
            return result;
        }

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
                throw new ArgumentNullException("reader", Res.Get(Res.ArgumentNull));

            XmlTextReader xmlReader = new XmlTextReader(reader)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
            };

            //XmlReader xmlReader = XmlReader.Create(reader, new XmlReaderSettings
            //    {
            //        ConformanceLevel = ConformanceLevel.Auto,
            //        IgnoreWhitespace = true,
            //        IgnoreComments = true
            //    });
            return Deserialize(xmlReader);
        }

        /// <summary>
        /// Deserializes an object from the specified file passed in <paramref name="fileName"/> parameter.
        /// </summary>
        /// <param name="fileName">Name of the file that contains the serialized content.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is incosistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName", Res.Get(Res.ArgumentNull));

            XmlTextReader xmlReader = new XmlTextReader(fileName)
            {
                WhitespaceHandling = WhitespaceHandling.Significant,
                Normalization = false,
                XmlResolver = null,
            };

            //XmlReader xmlReader = XmlReader.Create(fileName, new XmlReaderSettings
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
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is incosistent or corrupt.</exception>
        /// <exception cref="XmlException">An error occurred while parsing the XML.</exception>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream", Res.Get(Res.ArgumentNull));

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
        /// Works for results of <see cref="SerializeContent(XElement,object)"/> and other <c>SerializeContent</c> overloads.
        /// </summary>
        /// <param name="obj">The already constructed object whose inner state has to be deserialized.</param>
        /// <param name="content">XML content of the object.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="content"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="obj"/> must not be a value type.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is incosistent or corrupt.</exception>
        public static void DeserializeContent(XElement content, object obj)
        {
            DeserializeComponent(obj, content);
        }

        /// <summary>
        /// Restores inner state of an already created object passed in <paramref name="obj"/> parameter based on a saved XML.
        /// Works for results of <see cref="SerializeContent(XmlWriter,object)"/> and other <c>SerializeContent</c> overloads.
        /// </summary>
        /// <param name="obj">The already constructed object whose inner state has to be deserialized.</param>
        /// <param name="reader">An <see cref="XmlReader"/> instance to be used to read the XML content. Reader must be in at correct position for the successful deserialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="reader"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="obj"/> must not be a value type.</exception>
        /// <exception cref="NotSupportedException">Deserializing an inner type is not supported.</exception>
        /// <exception cref="ReflectionException">An inner type cannot be instantiated or serialized XML content is corrupt.</exception>
        /// <exception cref="ArgumentException">XML content is incosistent or corrupt.</exception>
        public static void DeserializeContent(XmlReader reader, object obj)
        {
            DeserializeComponent(obj, reader);
        }

        #endregion

        #endregion

        #region Private Methods

        #region Serialization

        /// <summary>
        /// Serializes an object or collection of objects.
        /// </summary>
        private static void SerializeComponent(object obj, XElement parent, XmlSerializationOptions options)
        {
            if (obj == null)
                throw new ArgumentNullException("obj", Res.Get(Res.ArgumentNull));
            if (parent == null)
                throw new ArgumentNullException("parent", Res.Get(Res.ArgumentNull));

            try
            {
                RegisterSerializedObject(obj);
                Type objType = obj.GetType();

                try
                {
                    // 1.) IXmlSerializable
                    if (obj is IXmlSerializable && ((options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
                    {
                        SerializeXmlSerializable((IXmlSerializable)obj, parent);
                        return;
                    }

                    // 2.) Collection
                    if (objType.IsCollection())
                    {
                        if (!objType.IsReadWriteCollection(obj))
                            throw new NotSupportedException(Res.Get(Res.XmlSerializeReadOnlyCollection, obj.GetType()));
                        SerializeCollection(obj as IEnumerable, false, parent, options, DesignerSerializationVisibility.Visible);
                        return;
                    }

                    // 3.) Any object
                    SerializeProperties(obj, parent, options);
                }
                finally
                {
                    if (parent.IsEmpty)
                        parent.Add(String.Empty);
                }
            }
            finally
            {
                UnregisterSerializedObject(obj);
            }
        }

        /// <summary>
        /// Serializes an object or collection of objects.
        /// At the start writer is in parent, it should be closed by parent.
        /// </summary>
        private static void SerializeComponent(object obj, XmlWriter writer, XmlSerializationOptions options)
        {
            if (obj == null)
                throw new ArgumentNullException("obj", Res.Get(Res.ArgumentNull));
            if (writer == null)
                throw new ArgumentNullException("writer", Res.Get(Res.ArgumentNull));
            Type objType = obj.GetType();
            try
            {
                RegisterSerializedObject(obj);

                // 1.) IXmlSerializable
                if (obj is IXmlSerializable && ((options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
                {
                    SerializeXmlSerializable((IXmlSerializable)obj, writer);
                    return;
                }

                // 2.) Collection
                if (objType.IsCollection())
                {
                    if (!objType.IsReadWriteCollection(obj))
                        throw new NotSupportedException(Res.Get(Res.XmlSerializeReadOnlyCollection, obj.GetType()));
                    SerializeCollection(obj as IEnumerable, false, writer, options, DesignerSerializationVisibility.Visible);
                    return;
                }

                // 3.) Any object
                SerializeProperties(obj, writer, options);
            }
            finally
            {
                UnregisterSerializedObject(obj);
            }
        }

        private static void SerializeXmlSerializable(IXmlSerializable obj, XContainer parent)
        {
            StringBuilder sb = new StringBuilder();
            using (XmlWriter xw = XmlWriter.Create(sb, new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment
            }))
            {
                obj.WriteXml(xw);
                // ReSharper disable PossibleNullReferenceException
                xw.Flush();
                // ReSharper restore PossibleNullReferenceException
            }

            Type objType = obj.GetType();
            string contentName = null;
            object[] attrs = objType.GetCustomAttributes(typeof(XmlRootAttribute), true);
            if (attrs.Length > 0)
                contentName = ((XmlRootAttribute)attrs[0]).ElementName;

            if (String.IsNullOrEmpty(contentName))
                contentName = objType.Name;

            using (XmlReader xr = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment
            }))
            {
                if (!xr.Read())
                    return;

                XElement content = new XElement(contentName);
                while (!xr.EOF)
                {
                    content.Add(XNode.ReadFrom(xr));
                }
                parent.Add(content);
            }

            parent.Add(new XAttribute("format", "custom"));
        }

        /// <summary>
        /// XmlWriter version. Writer must be in parent element, which should be closed by the parent.
        /// </summary>
        private static void SerializeXmlSerializable(IXmlSerializable obj, XmlWriter writer)
        {
            writer.WriteAttributeString("format", "custom");

            Type objType = obj.GetType();
            string contentName = null;
            object[] attrs = objType.GetCustomAttributes(typeof(XmlRootAttribute), true);
            if (attrs.Length > 0)
                contentName = ((XmlRootAttribute)attrs[0]).ElementName;

            if (String.IsNullOrEmpty(contentName))
                contentName = objType.Name;

            writer.WriteStartElement(contentName);
            obj.WriteXml(writer);
            writer.WriteFullEndElement();
        }

        private static void SerializeProperties(object obj, XContainer parent, XmlSerializationOptions options)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            //PropertyDescriptorCollection descriptors = TypeDescriptor.GetProperties(obj);

            // signing that object is not null
            parent.Add(String.Empty);

            foreach (PropertyInfo property in properties)
            {
                // skipping write-only properties, indexers and read-only properties except non read-only collections
                if (!property.CanRead || property.GetIndexParameters().Length > 0 || (!property.CanWrite && !property.PropertyType.IsCollection()))
                    continue;

                // skipping non-serializable properties
                // a.) hidden by DesignerSerializationVisibility
                object[] attrs = property.GetCustomAttributes(typeof(DesignerSerializationVisibilityAttribute), true);
                DesignerSerializationVisibility visibility = attrs.Length > 0 ? ((DesignerSerializationVisibilityAttribute)attrs[0]).Visibility : DesignerSerializationVisibilityAttribute.Default.Visibility;
                if (visibility == DesignerSerializationVisibility.Hidden)
                    continue;

                // b.) ShouldSerialize<PropertyName> method returns false
                if ((options & XmlSerializationOptions.IgnoreShouldSerialize) == XmlSerializationOptions.None)
                {
                    MethodInfo shouldSerializeProperty = property.DeclaringType.GetMethod("ShouldSerialize" + property.Name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);
                    if (shouldSerializeProperty != null && shouldSerializeProperty.ReturnType == typeof(bool))
                    {
                        if ((bool)Reflector.RunMethod(obj, shouldSerializeProperty) == false)
                            continue;
                    }
                }

                // c.) DefaultValue equals to property value
                bool hasDefaultValue = false;
                object defaultValue = null;
                if ((options & XmlSerializationOptions.IgnoreDefaultValueAttribute) == XmlSerializationOptions.None)
                {
                    attrs = property.GetCustomAttributes(typeof(DefaultValueAttribute), true);
                    hasDefaultValue = attrs.Length > 0;
                    if (hasDefaultValue)
                        defaultValue = ((DefaultValueAttribute)attrs[0]).Value;
                }
                if (!hasDefaultValue && (options & XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback) != XmlSerializationOptions.None)
                {
                    hasDefaultValue = true;
                    defaultValue = property.PropertyType.IsValueType ? Reflector.Construct(property.PropertyType) : null;
                }
                object propValue = Reflector.GetProperty(obj, property);
                if (hasDefaultValue && Equals(propValue, defaultValue))
                    continue;

                // -------------- property is not skipped, serializing
                XElement newElement = new XElement(property.Name);
                Type propType = propValue == null ? property.PropertyType : propValue.GetType();

                // 1.) property is IXmlSerializable standard XmlSerialization is not disabled
                if (propValue != null && propValue is IXmlSerializable && ((options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
                {
                    SerializeXmlSerializable((IXmlSerializable)propValue, newElement);
                    parent.Add(newElement);
                }
                // 2.) property is collection
                else if (propValue == null && property.CanWrite && propType.IsCollection() || propType.IsReadWriteCollection(propValue))
                {
                    SerializeCollection(propValue as IEnumerable, propValue != null && propType != property.PropertyType, newElement, options, visibility);
                    parent.Add(newElement);
                }
                // 3.) non-collection or readonly collection (that maybe still can be serialized as binary):
                else
                {
                    // d.) skipping read-only collections (if read-only in both meaning: has no setter and IsReadonly is true)
                    if (!property.CanWrite)
                        continue;

                    // Using explicitly defined type converter if can convert to and from string
                    attrs = property.GetCustomAttributes(typeof(TypeConverterAttribute), true);
                    TypeConverterAttribute convAttr = attrs.Length > 0 ? attrs[0] as TypeConverterAttribute : null;
                    if (convAttr != null)
                    {
                        Type convType = Type.GetType(convAttr.ConverterTypeName);
                        if (convType != null)
                        {
                            ConstructorInfo ctor = convType.GetConstructor(new Type[] { typeof(Type) });
                            object[] ctorParams = new object[] { property.PropertyType };
                            if (ctor == null)
                            {
                                ctor = convType.GetConstructor(Type.EmptyTypes);
                                ctorParams = Reflector.EmptyObjects;
                            }
                            if (ctor != null)
                            {
                                TypeConverter converter = Reflector.Construct(ctor, ctorParams) as TypeConverter;
                                if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
                                {
                                    newElement.Add(converter.ConvertTo(null, CultureInfo.InvariantCulture, propValue, typeof(string)));
                                    parent.Add(newElement);
                                    continue;
                                }
                            }
                        }
                    }

                    if (propValue == null || TrySerializeObject(propValue, propType != property.PropertyType, newElement, propType, options, visibility))
                        parent.Add(newElement);
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeProperty, obj.GetType(), property.Name, options));
                }

                //// adding property type if instance is not the same type
                //if (propValue != null && propType != property.PropertyType)
                //    newElement.Add(new XAttribute("type", GetTypeString(obj, options)));
            }
        }

        private static void SerializeProperties(object obj, XmlWriter writer, XmlSerializationOptions options)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                // Skip 1.) write-only properties, indexers and read-only properties except non read-only collections
                if (!property.CanRead || property.GetIndexParameters().Length > 0 || (!property.CanWrite && !property.PropertyType.IsCollection()))
                    continue;

                // skipping non-serializable properties
                // Skip 2.) hidden by DesignerSerializationVisibility
                object[] attrs = property.GetCustomAttributes(typeof(DesignerSerializationVisibilityAttribute), true);
                DesignerSerializationVisibility visibility = attrs.Length > 0 ? ((DesignerSerializationVisibilityAttribute)attrs[0]).Visibility : DesignerSerializationVisibilityAttribute.Default.Visibility;
                if (visibility == DesignerSerializationVisibility.Hidden)
                    continue;

                // Skip 3.) ShouldSerialize<PropertyName> method returns false
                if ((options & XmlSerializationOptions.IgnoreShouldSerialize) == XmlSerializationOptions.None)
                {
                    MethodInfo shouldSerializeProperty = property.DeclaringType.GetMethod("ShouldSerialize" + property.Name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);
                    if (shouldSerializeProperty != null && shouldSerializeProperty.ReturnType == typeof(bool))
                    {
                        if ((bool)Reflector.RunMethod(obj, shouldSerializeProperty) == false)
                            continue;
                    }
                }

                // Skip 4.) DefaultValue equals to property value
                bool hasDefaultValue = false;
                object defaultValue = null;
                if ((options & XmlSerializationOptions.IgnoreDefaultValueAttribute) == XmlSerializationOptions.None)
                {
                    attrs = property.GetCustomAttributes(typeof(DefaultValueAttribute), true);
                    hasDefaultValue = attrs.Length > 0;
                    if (hasDefaultValue)
                        defaultValue = ((DefaultValueAttribute)attrs[0]).Value;
                }
                if (!hasDefaultValue && (options & XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback) != XmlSerializationOptions.None)
                {
                    hasDefaultValue = true;
                    defaultValue = property.PropertyType.IsValueType ? Reflector.Construct(property.PropertyType) : null;
                }
                object propValue = Reflector.GetProperty(obj, property);
                if (hasDefaultValue && Equals(propValue, defaultValue))
                    continue;

                // -------------- property is not skipped, serializing
                //XElement newElement = new XElement(property.Name);
                Type propType = propValue == null ? property.PropertyType : propValue.GetType();

                // a.) property is IXmlSerializable standard XmlSerialization is not disabled
                if (propValue != null && propValue is IXmlSerializable && ((options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
                {
                    writer.WriteStartElement(property.Name);
                    SerializeXmlSerializable((IXmlSerializable)propValue, writer);
                    writer.WriteEndElement();
                    continue;
                }
                // b.) property is collection
                else if (propValue == null && property.CanWrite && propType.IsCollection() || propType.IsReadWriteCollection(propValue))
                {
                    writer.WriteStartElement(property.Name);
                    SerializeCollection(propValue as IEnumerable, propValue != null && propType != property.PropertyType, writer, options, visibility);
                    if (propValue != null)
                        writer.WriteFullEndElement();
                    else
                        writer.WriteEndElement();
                    continue;
                }
                // c.) single non-collection property or readonly collection (that maybe still can be serialized as binary):
                else
                {
                    // Skip 5.) skipping read-only collections (if read-only in both meaning: has no setter and IsReadonly is true)
                    if (!property.CanWrite)
                        continue;

                    // c/1.) Using explicitly defined type converter if can convert to and from string
                    attrs = property.GetCustomAttributes(typeof(TypeConverterAttribute), true);
                    TypeConverterAttribute convAttr = attrs.Length > 0 ? attrs[0] as TypeConverterAttribute : null;
                    if (convAttr != null)
                    {
                        Type convType = Type.GetType(convAttr.ConverterTypeName);
                        if (convType != null)
                        {
                            ConstructorInfo ctor = convType.GetConstructor(new Type[] { typeof(Type) });
                            object[] ctorParams = new object[] { property.PropertyType };
                            if (ctor == null)
                            {
                                ctor = convType.GetConstructor(Type.EmptyTypes);
                                ctorParams = Reflector.EmptyObjects;
                            }
                            if (ctor != null)
                            {
                                TypeConverter converter = Reflector.Construct(ctor, ctorParams) as TypeConverter;
                                if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
                                {
                                    writer.WriteStartElement(property.Name);
                                    bool spacePreserved;
                                    bool escaped;
                                    string value = GetStringValue(converter.ConvertTo(null, CultureInfo.InvariantCulture, propValue, typeof(string)), options, out spacePreserved, out escaped);
                                    if (spacePreserved)
                                        writer.WriteAttributeString("xml", "space", null, "preserve");
                                    if (escaped)
                                        writer.WriteAttributeString("escaped", "true");
                                    writer.WriteString(value);
                                    writer.WriteEndElement();
                                    continue;
                                }
                            }
                        }
                    }

                    // c/2.) Usual ways if possible
                    writer.WriteStartElement(property.Name);
                    if (propValue == null)
                    {
                        writer.WriteEndElement();
                    }
                    else if (TrySerializeObject(propValue, propType != property.PropertyType, writer, propType, options, visibility))
                    {
                        writer.WriteFullEndElement();
                    }
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeProperty, obj.GetType(), property.Name, options));
                }
            }
        }

        /// <summary>
        /// Tries to serialize the whole object itself. Returns false when object type is not supported with current options but may throw exceptions on
        /// invalid data or inconsistent settings.
        /// XElement version.
        /// </summary>
        private static bool TrySerializeObject(object obj, bool typeNeeded, XElement parent, Type type, XmlSerializationOptions options, DesignerSerializationVisibility visibility)
        {
            if (obj == null)
                return true;

            // a.) If type can be natively parsed, simple adding
            if (Reflector.CanParseNatively(type) && !(obj is Type && ((Type)obj).IsGenericParameter))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type, options)));

                bool spacePreserved, escaped;
                string s = GetStringValue(obj, options, out spacePreserved, out escaped);

                if (spacePreserved)
                    parent.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));
                if (escaped)
                    parent.Add(new XAttribute("escaped", "true"));

                parent.Add(s);
                return true;
            }

            // b.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type, options)));

                parent.Add(converter.ConvertTo(null, CultureInfo.InvariantCulture, obj, typeof(string)));
                return true;
            }

            // c.) IXmlSerializable
            if (obj is IXmlSerializable && ((options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type, options)));

                SerializeXmlSerializable((IXmlSerializable)obj, parent);
                return true;
            }

            // d.) simple object
            if (obj.GetType() == typeof(object))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type, options)));

                parent.Add(String.Empty);
                return true;
            }

            // e/1.) Keyvalue 1: DictionaryEntry: can be serialized recursively
            if (type == typeof(DictionaryEntry))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type, options)));

                // SerializeComponent can be avoided because DE is neither IXmlSerializable nor collection and no need to register because it is a value type
                SerializeProperties(obj, parent, options);
                return true;
            }

            // e/2.) Keyvalue 2: KeyValuePair: properties are read-only so special support needed
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type, options)));

                parent.Add(new XAttribute("format", "keyvalue"));
                object key = Reflector.GetInstancePropertyByName(obj, "Key");
                object value = Reflector.GetInstancePropertyByName(obj, "Value");
                XElement xKey = new XElement("Key");
                XElement xValue = new XElement("Value");
                parent.Add(xKey, xValue);
                if (key != null)
                {
                    Type elementType = key.GetType();
                    bool elementTypeNeeded = elementType != type.GetGenericArguments()[0];
                    if (!TrySerializeObject(key, elementTypeNeeded, xKey, elementType, options, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, elementType, options));
                }
                if (value != null)
                {
                    Type elementType = value.GetType();
                    bool elementTypeNeeded = elementType != type.GetGenericArguments()[1];
                    if (!TrySerializeObject(value, elementTypeNeeded, xValue, elementType, options, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, elementType, options));
                }
                return true;
            }

            // f.) value type as binary only if enabled
            if (type.IsValueType && ((options.IsForcedSerializationValueTypesEnabled() && !options.IsBinarySerializationEnabled()) || options.IsCompactSerializationValueTypesEnabled()))
            {
                byte[] data;
                if (BinarySerializer.TrySerializeStruct((ValueType)obj, out data))
                {
                    if (typeNeeded)
                        parent.Add(new XAttribute("type", GetTypeString(type, options)));

                    parent.Add(new XAttribute("format", "structbase64"));
                    if ((options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                        parent.Add(new XAttribute("CRC", Crc32.CalculateHash(data).ToString("X8")));
                    parent.Add(Convert.ToBase64String(data));
                    return true;
                }
                else if (!(visibility == DesignerSerializationVisibility.Content || options.IsRecursiveSerializationEnabled() || options.IsBinarySerializationEnabled()))
                {
                    if (options.IsForcedSerializationValueTypesEnabled())
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeValueType, obj.GetType(), options));
                }
            }

            // g.) if type of value is serializable and option is enabled, then adding binary serialized hexa content to xml
            if (visibility != DesignerSerializationVisibility.Content && options.IsBinarySerializationEnabled())
            {
                try
                {
                    SerializeBinary(obj, parent, options);
                    return true;
                }
                catch (Exception e)
                {
                    throw new SerializationException(Res.Get(Res.XmlBinarySerializationFailed, obj.GetType(), options, e.Message), e);
                }
            }

            // h.) recursive serialization, if enabled
            if (options.IsRecursiveSerializationEnabled() || visibility == DesignerSerializationVisibility.Content)
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type, options)));

                SerializeComponent(obj, parent, options);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to serialize the whole object itself. Returns false when object type is not supported with current options but may throw exceptions on
        /// invalid data or inconsistent settings.
        /// XmlWriter version. Start element must be opened and closed by caller.
        /// obj.GetType and type can be different (properties)
        /// </summary>
        private static bool TrySerializeObject(object obj, bool typeNeeded, XmlWriter writer, Type type, XmlSerializationOptions options, DesignerSerializationVisibility visibility)
        {
            if (obj == null)
                return true;

            // a.) If type can be natively parsed, simple writing
            if (Reflector.CanParseNatively(type) && !(obj is Type && ((Type)obj).IsGenericParameter))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type, options));

                bool spacePreserved, escaped;
                string s = GetStringValue(obj, options, out spacePreserved, out escaped);

                if (spacePreserved)
                    writer.WriteAttributeString("xml", "space", null, "preserve");
                if (escaped)
                    writer.WriteAttributeString("escaped", "true");

                writer.WriteString(s);
                return true;
            }

            // b.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type, options));

                writer.WriteString((string)converter.ConvertTo(null, CultureInfo.InvariantCulture, obj, typeof(string)));
                return true;
            }

            // c.) IXmlSerializable
            if (obj is IXmlSerializable && ((options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type, options));

                SerializeXmlSerializable((IXmlSerializable)obj, writer);
                return true;
            }

            // d.) simple object
            if (obj.GetType() == typeof(object))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type, options));
                writer.WriteString(String.Empty);

                return true;
            }

            // e/1.) Keyvalue 1: DictionaryEntry: can be serialized recursively
            if (type == typeof(DictionaryEntry))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type, options));

                // SerializeComponent can be avoided because DE is neither IXmlSerializable nor collection and no need to register because it is a value type
                SerializeProperties(obj, writer, options);
                return true;
            }

            // e/2.) Keyvalue 2: KeyValuePair: properties are read-only so special support needed
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type, options));

                writer.WriteAttributeString("format", "keyvalue");
                object key = Reflector.GetInstancePropertyByName(obj, "Key");
                object value = Reflector.GetInstancePropertyByName(obj, "Value");

                writer.WriteStartElement("Key");
                if (key == null)
                {
                    writer.WriteEndElement();
                }
                else
                {
                    Type elementType = key.GetType();
                    bool elementTypeNeeded = elementType != type.GetGenericArguments()[0];
                    if (!TrySerializeObject(key, elementTypeNeeded, writer, elementType, options, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, elementType, options));

                    writer.WriteFullEndElement();
                }

                writer.WriteStartElement("Value");
                if (value == null)
                {
                    writer.WriteEndElement();
                }
                else
                {
                    Type elementType = value.GetType();
                    bool elementTypeNeeded = elementType != type.GetGenericArguments()[1];
                    if (!TrySerializeObject(value, elementTypeNeeded, writer, elementType, options, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, elementType, options));

                    writer.WriteFullEndElement();
                }

                return true;
            }

            // f.) value type as binary only if enabled
            if (type.IsValueType && ((options.IsForcedSerializationValueTypesEnabled() && !options.IsBinarySerializationEnabled()) || options.IsCompactSerializationValueTypesEnabled()))
            {
                byte[] data;
                if (BinarySerializer.TrySerializeStruct((ValueType)obj, out data))
                {
                    if (typeNeeded)
                        writer.WriteAttributeString("type", GetTypeString(type, options));

                    writer.WriteAttributeString("format", "structbase64");
                    if ((options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                        writer.WriteAttributeString("CRC", Crc32.CalculateHash(data).ToString("X8"));
                    writer.WriteString(Convert.ToBase64String(data));
                    return true;
                }
                else if (!(visibility == DesignerSerializationVisibility.Content || options.IsRecursiveSerializationEnabled() || options.IsBinarySerializationEnabled()))
                {
                    if (options.IsForcedSerializationValueTypesEnabled())
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeValueType, obj.GetType(), options));
                }
            }

            // g.) if type of value is serializable and option is enabled, then adding binary serialized hexa content to xml
            if (visibility != DesignerSerializationVisibility.Content && options.IsBinarySerializationEnabled())
            {
                try
                {
                    SerializeBinary(obj, writer, options);
                    return true;
                }
                catch (Exception e)
                {
                    throw new SerializationException(Res.Get(Res.XmlBinarySerializationFailed, obj.GetType(), options, e.Message), e);
                }
            }

            // h.) recursive serialization, if enabled
            if (options.IsRecursiveSerializationEnabled() || visibility == DesignerSerializationVisibility.Content)
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type, options));

                SerializeComponent(obj, writer, options);
                return true;
            }

            return false;
        }

        private static string GetStringValue(object value, XmlSerializationOptions options, out bool spacePreserve, out bool escaped)
        {
            spacePreserve = false;
            escaped = false;

            if (value is bool)
                return XmlConvert.ToString((bool)value);
            if (value is double)
                return ((double)value).ToRoundtripString();
            if (value is float)
                return ((float)value).ToRoundtripString();
            if (value is decimal)
                return ((decimal)value).ToRoundtripString();
            if (value is DateTime)
                return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
            if (value is DateTimeOffset)
                return XmlConvert.ToString((DateTimeOffset)value);
            Type type = value as Type;
            if (type != null)
            {
                //if (value.GetType() != Reflector.RuntimeType)
                //    throw new NotSupportedException(Res.Get(Res.XmlNonRuntimeType));
                //if (type.IsGenericParameter)
                //    throw new NotSupportedException(Res.Get(Res.XmlGenericTypeParam));
                return GetTypeString(type, options);
            }

            string result = value.ToString();
            if (result.Length == 0)
                return result;

            //bool prevWhiteSpace = false;
            bool escapeNewline = (options & XmlSerializationOptions.EscapeNewlineCharacters) != XmlSerializationOptions.None;
            StringBuilder escapedResult = null;
            spacePreserve = IsWhiteSpace(result[0], escapeNewline);

            // checking result for escaping
            for (int i = 0; i < result.Length; i++)
            {
                bool isValidSurrogate;
                if (EscapeNeeded(result, i, escapeNewline, out isValidSurrogate))
                {
                    if (escapedResult == null)
                        escapedResult = new StringBuilder(result.Substring(0, i).Replace(@"\", @"\\"));

                    escapedResult.Append(@"\" + ((ushort)result[i]).ToString("X4"));
                }
                else
                {
                    if (escapedResult != null)
                    {
                        escapedResult.Append(result[i]);
                        if (result[i] == '\\')
                            escapedResult.Append('\\');
                        else if (isValidSurrogate)
                            escapedResult.Append(result[i + 1]);
                    }

                    if (isValidSurrogate)
                        i++;
                }
            }

            if (escapedResult != null)
            {
                escaped = true;
                return escapedResult.ToString();
            }

            return result;
        }

        private static bool IsWhiteSpace(char c, bool ignoreNewline)
        {
            // U+0009 = <control> HORIZONTAL TAB 
            // U+000a = <control> LINE FEED
            // U+000b = <control> VERTICAL TAB 
            // U+000c = <contorl> FORM FEED 
            // U+000d = <control> CARRIAGE RETURN
            // U+0085 = <control> NEXT LINE 
            // U+00a0 = NO-BREAK SPACE

            if (c == ' ' || c == '\t')
                return true;

            if (ignoreNewline)
                return false;

            return c == '\r' || c == '\n';

            //if ((c == ' ') || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085')
            //    return (true);
        }

        /// <summary>
        /// Gets whether a character has to be escaped
        /// </summary>
        private static bool EscapeNeeded(string s, int index, bool escapeNewlines, out bool isValidSurrogate)
        {
            isValidSurrogate = false;
            int c = s[index];
            if (c == '\t' // TAB is ok
                || (c >= 0x20 && c <= 0xD7FF) // space..HighSurrogateStart-1 are ok
                || (c >= 0xE000 && c <= 0xFFFD) // LowSurrogateEnd+1..replacement character are ok
                ||(!escapeNewlines && (c == 0xA || c == 0xD))) // \n, \r are ok if new lines are not escaped
            {
                return false;
            }

            // valid surrogate pair
            if (index < s.Length - 1 && Char.IsSurrogatePair((char)c, s[index + 1]))
            {
                isValidSurrogate = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Serializing binary content by LinqToXml
        /// </summary>
        private static void SerializeBinary(object obj, XContainer parent, XmlSerializationOptions options)
        {
            parent.Add(new XAttribute("format", "base64"));
            if (obj == null)
                return;
            BinarySerializationOptions binSerOptions = options.ToBinarySerializationOptions();
            byte[] data = BinarySerializer.Serialize(obj, binSerOptions);
            if ((options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                parent.Add(new XAttribute("CRC", Crc32.CalculateHash(data).ToString("X8")));
            parent.Add(Convert.ToBase64String(data));
        }

        /// <summary>
        /// Serializing binary content by XmlWriter
        /// </summary>
        private static void SerializeBinary(object obj, XmlWriter writer, XmlSerializationOptions options)
        {
            writer.WriteAttributeString("format", "base64");

            if (obj == null)
                return;

            BinarySerializationOptions binSerOptions = options.ToBinarySerializationOptions();
            byte[] data = BinarySerializer.Serialize(obj, binSerOptions);

            if ((options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                writer.WriteAttributeString("CRC", Crc32.CalculateHash(data).ToString("X8"));
            writer.WriteString(Convert.ToBase64String(data));
        }

        /// <summary>
        /// Serializing a collection by LinqToXml
        /// </summary>
        private static void SerializeCollection(IEnumerable collection, bool typeNeeded, XContainer parent, XmlSerializationOptions options, DesignerSerializationVisibility visibility)
        {
            if (collection == null)
                return;

            // signing that collection is not null - now it will be at least <Collection></Collection> instead of <Collection />
            parent.Add(String.Empty);

            // array collection
            if (collection is Array)
            {
                Type elementType = collection.GetType().GetElementType();
                Array array = (Array)collection;

                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(collection.GetType(), options)));

                // multidimensional or nonzero-based array
                if (array.Rank > 1 || array.GetLowerBound(0) != 0)
                {
                    StringBuilder dim = new StringBuilder();
                    for (int i = 0; i < array.Rank; i++)
                    {
                        int low;
                        if ((low = array.GetLowerBound(i)) != 0)
                        {
                            dim.Append(low + ".." + (low + array.GetLength(i) - 1));
                        }
                        else
                        {
                            dim.Append(array.GetLength(i));
                        }

                        if (i < array.Rank - 1)
                        {
                            dim.Append(',');
                        }

                    }

                    parent.Add(new XAttribute("dim", dim));
                }
                else
                    parent.Add(new XAttribute("length", array.Length.ToString(CultureInfo.InvariantCulture)));

                // array of a primitive type
                if (elementType.IsPrimitive && (options & XmlSerializationOptions.CompactSerializationOfPrimitiveArrays) != XmlSerializationOptions.None)
                {
                    if (array.Length > 0)
                    {
                        byte[] data = new byte[Buffer.ByteLength(array)];
                        Buffer.BlockCopy(array, 0, data, 0, data.Length);
                        parent.Add(new XAttribute("comp", "base64"));
                        parent.Add(Convert.ToBase64String(data));
                        if ((options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                            parent.Add(new XAttribute("CRC", Crc32.CalculateHash(data).ToString("X8")));
                    }
                }
                // non-primitive type array or compact serialization is not enabled
                else
                {
                    bool elementTypeNeeded = !(elementType.IsValueType || elementType.IsClass && elementType.IsSealed);
                    foreach (var item in array)
                    {
                        XElement child = new XElement("item");
                        Type itemType = null;
                        if (item == null || TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, child, itemType ?? item.GetType(), options, visibility))
                        {
                            parent.Add(child);
                        }
                        else
                            throw new SerializationException(Res.Get(Res.XmlCannotSerializeArrayElement, item.GetType(), options));
                    }
                }
            }
            // non-array collection
            else
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(collection.GetType(), options)));

                // serializing main properties first
                SerializeProperties(collection, parent, options);

                // determining element type
                Type elementType = collection.GetElementType();
                bool elementTypeNeeded = !(elementType.IsValueType || elementType.IsClass && elementType.IsSealed);

                // serializing items
                foreach (var item in collection)
                {
                    XElement child = new XElement("item");
                    Type itemType = null;
                    if (item == null || TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, child, itemType ?? item.GetType(), options, visibility))
                    {
                        parent.Add(child);
                    }
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeCollectionElement, item.GetType(), options));
                }
            }
        }

        /// <summary>
        /// Serializing a collection by XmlWriter
        /// </summary>
        private static void SerializeCollection(IEnumerable collection, bool typeNeeded, XmlWriter writer, XmlSerializationOptions options, DesignerSerializationVisibility visibility)
        {
            if (collection == null)
                return;

            // array collection
            if (collection is Array)
            {
                Type elementType = collection.GetType().GetElementType();
                Array array = (Array)collection;

                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(collection.GetType(), options));

                // multidimensional or nonzero-based array
                if (array.Rank > 1 || array.GetLowerBound(0) != 0)
                {
                    StringBuilder dim = new StringBuilder();
                    for (int i = 0; i < array.Rank; i++)
                    {
                        int low;
                        if ((low = array.GetLowerBound(i)) != 0)
                        {
                            dim.Append(low + ".." + (low + array.GetLength(i) - 1));
                        }
                        else
                        {
                            dim.Append(array.GetLength(i));
                        }

                        if (i < array.Rank - 1)
                        {
                            dim.Append(',');
                        }

                    }

                    writer.WriteAttributeString("dim", dim.ToString());
                }
                else
                    writer.WriteAttributeString("length", array.Length.ToString(CultureInfo.InvariantCulture));

                if (array.Length == 0)
                {
                    // signing that collection is not null - now it will be at least <Collection></Collection> instead of <Collection />
                    writer.WriteString(String.Empty);
                    return;
                }

                // array of a primitive type
                if (elementType.IsPrimitive && (options & XmlSerializationOptions.CompactSerializationOfPrimitiveArrays) != XmlSerializationOptions.None)
                {
                    byte[] data = new byte[Buffer.ByteLength(array)];
                    Buffer.BlockCopy(array, 0, data, 0, data.Length);
                    writer.WriteAttributeString("comp", "base64");
                    if ((options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                        writer.WriteAttributeString("CRC", Crc32.CalculateHash(data).ToString("X8"));
                    writer.WriteString(Convert.ToBase64String(data));

                    return;
                }

                // non-primitive type array or compact serialization is not enabled
                bool elementTypeNeeded = !(elementType.IsValueType || elementType.IsClass && elementType.IsSealed);
                foreach (var item in array)
                {
                    writer.WriteStartElement("item");
                    Type itemType = null;
                    if (item == null)
                    {
                        writer.WriteEndElement();
                    }
                    else if (TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, writer, itemType ?? item.GetType(), options, visibility))
                    {
                        writer.WriteFullEndElement();
                    }
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeArrayElement, item.GetType(), options));
                }

                return;
            }
            // non-array collection
            else
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(collection.GetType(), options));

                // serializing main properties first
                SerializeProperties(collection, writer, options);

                // determining element type
                Type elementType = collection.GetElementType();
                bool elementTypeNeeded = !(elementType.IsValueType || elementType.IsClass && elementType.IsSealed);

                // serializing items
                foreach (var item in collection)
                {
                    writer.WriteStartElement("item");
                    Type itemType = null;
                    if (item == null)
                    {
                        writer.WriteEndElement();
                    }
                    else if (TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, writer, itemType ?? item.GetType(), options, visibility))
                    {
                        writer.WriteFullEndElement();
                    }
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeCollectionElement, item.GetType(), options));
                }
            }
        }

        #endregion

        #region Deserialization

        /// <summary>
        /// Deserializes an object or collection of objects.
        /// XElement version
        /// </summary>
        private static void DeserializeComponent(object obj, XElement parent)
        {
            if (obj == null)
                throw new ArgumentNullException("obj", Res.Get(Res.ArgumentNull));
            if (parent == null)
                throw new ArgumentNullException("parent", Res.Get(Res.ArgumentNull));
            Type objType = obj.GetType();
            //if (objType.IsValueType)
            //    throw new ArgumentException("Deserialize cannot receive value type as a root object.", "obj");

            // deserialize IXmlSerializable
            XAttribute attrFormat = parent.Attribute("format");
            if (attrFormat != null && attrFormat.Value == "custom")
            {
                IXmlSerializable xmlSerializable = obj as IXmlSerializable;
                if (xmlSerializable == null)
                    throw new ArgumentException(Res.Get(Res.NotAnIXmlSerializable, objType));
                DeserializeXmlSerializable(xmlSerializable, parent);
                return;
            }

            // deserialize array
            if (objType.IsArray)
            {
                Array array = (Array)obj;
                DeserializeArray(ref array, null, parent);
                return;
            }
            // collection: clearing it before restoring content and retireving element type
            Type collectionElementType = null;
            if (objType.IsCollection())
            {
                IEnumerable collection = (IEnumerable)obj;
                collection.Clear();
                collectionElementType = collection.GetElementType();
            }

            foreach (XElement element in parent.Elements())
            {
                PropertyInfo property = objType.GetProperty(element.Name.LocalName);
                XAttribute attrType = element.Attribute("type");
                Type type = null;
                if (attrType != null)
                    type = Reflector.ResolveType(attrType.Value);
                if (type == null && property != null)
                    type = property.PropertyType;

                // real property
                if (property != null)
                {
                    // collection property
                    if (type.IsCollection())
                    {
                        // array
                        if (type.IsArray)
                        {
                            // null array
                            if (element.IsEmpty)
                            {
                                if (property.CanWrite)
                                {
                                    Reflector.SetProperty(obj, property, null);
                                    continue;
                                }

                                throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetterNull, type, objType.FullName, property.Name));
                            }

                            Array array = null;

                            // property with setter: creating a new array
                            if (property.CanWrite)
                            {
                                DeserializeArray(ref array, type.GetElementType(), element);
                                Reflector.SetProperty(obj, property, array);
                            }

                            // read-only array
                            else
                            {
                                array = Reflector.GetProperty(obj, property) as Array;
                                if (array == null)
                                    throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetter, type, objType, property.Name));

                                DeserializeArray(ref array, null, element);
                            }
                            continue;
                        }

                        // non-array collection
                        IEnumerable collection = Reflector.GetProperty(obj, property) as IEnumerable;

                        // setting null
                        if (collection != null && element.IsEmpty)
                        {
                            if (!property.CanWrite)
                                throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetterNull, type, objType, property.Name));
                            Reflector.SetProperty(obj, property, null);
                        }
                        // clearing possible existing elements (is element.HasElements is true, then the recursive call will clear the collection)
                        else if (collection != null && !element.HasElements)
                            collection.Clear();
                        // collection is null: default constructor and setter needed
                        else if (collection == null && !element.IsEmpty)
                        {
                            if (!property.CanWrite)
                                throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetter, type, objType, property.Name));
                            else
                            {
                                try
                                {
                                    collection = (IEnumerable)Reflector.Construct(type);
                                }
                                catch (Exception e)
                                {
                                    throw new ReflectionException(Res.Get(Res.XmlCannotCreateCollection, objType), e);
                                }
                                Reflector.SetProperty(obj, property, collection);
                            }
                        }
                        if (element.HasElements)
                            DeserializeComponent(collection, element);
                        continue;
                    }
                    // non-collection property
                    else
                    {
                        if (!property.CanWrite)
                            throw new ReflectionException(Res.Get(Res.XmlPropertyHasNoSetter, property, objType.Name));

                        // Using explicitly defined type converter if can convert from string
                        object[] attrs = property.GetCustomAttributes(typeof(TypeConverterAttribute), true);
                        TypeConverterAttribute convAttr = attrs.Length > 0 ? attrs[0] as TypeConverterAttribute : null;
                        if (convAttr != null)
                        {
                            Type convType = Type.GetType(convAttr.ConverterTypeName);
                            if (convType != null)
                            {
                                ConstructorInfo ctor = convType.GetConstructor(new Type[] { typeof(Type) });
                                object[] ctorParams = new object[] { property.PropertyType };
                                if (ctor == null)
                                {
                                    ctor = convType.GetConstructor(Type.EmptyTypes);
                                    ctorParams = Reflector.EmptyObjects;
                                }
                                if (ctor != null)
                                {
                                    TypeConverter converter = Reflector.Construct(ctor, ctorParams) as TypeConverter;
                                    if (converter != null && converter.CanConvertFrom(typeof(string)))
                                    {
                                        Reflector.SetProperty(obj, property, converter.ConvertFrom(null, CultureInfo.InvariantCulture, element.GetMultilineValue()));
                                        continue;
                                    }
                                }
                            }
                        }

                        object result;
                        if (TryDeserializeObject(type, element, out result))
                        {
                            Reflector.SetProperty(obj, property, result);
                            continue;
                        }

                        throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));
                    }
                }
                // collection element
                else if (objType.IsCollection())
                {
                    if (element.Name.LocalName != "item")
                        throw new ArgumentException(Res.Get(Res.XmlItemExpected, element.Name.LocalName));

                    IEnumerable collection = (IEnumerable)obj;

                    // adding null item
                    if (element.IsEmpty)
                    {
                        collection.Add(null);
                        continue;
                    }

                    object item;
                    if (TryDeserializeObject(type ?? collectionElementType, element, out item))
                    {
                        collection.Add(item);
                        continue;
                    }

                    if (type == null)
                        throw new ArgumentException(Res.Get(Res.XmlCannotDetermineElementType, objType));
                    throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, type));
                }
                if (element.Name.LocalName == "item")
                    throw new SerializationException(Res.Get(Res.XmlNotACollection, objType));
                else
                    throw new ReflectionException(Res.Get(Res.XmlHasNoProperty, objType, element.Name.LocalName));
            }

            // Disabled because of OrderedDictionary. TODO: Some similar custom interface
            //IDeserializationCallback callbackCapable = obj as IDeserializationCallback;
            //if (callbackCapable != null)
            //    callbackCapable.OnDeserialization(null);
        }

        /// <summary>
        /// Deserializes an object or collection of objects.
        /// XmlReader version. Position is before content (on parent start element). On exit position is on parent close element.
        /// </summary>
        private static void DeserializeComponent(object obj, XmlReader reader)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (reader == null)
                throw new ArgumentNullException("reader");

            Type objType = obj.GetType();

            // deserialize IXmlSerializable
            string attrFormat = reader["format"];
            if (attrFormat == "custom")
            {
                IXmlSerializable xmlSerializable = obj as IXmlSerializable;
                if (xmlSerializable == null)
                    throw new ArgumentException(Res.Get(Res.NotAnIXmlSerializable, objType));
                DeserializeXmlSerializable(xmlSerializable, reader);
                return;
            }

            // deserialize array
            if (objType.IsArray)
            {
                Array array = (Array)obj;
                DeserializeArray(ref array, null, reader);
                return;
            }

            // collection: clearing it before restoring content and retireving element type
            Type collectionElementType = null;
            if (objType.IsCollection())
            {
                IEnumerable collection = (IEnumerable)obj;
                collection.Clear();
                collectionElementType = collection.GetElementType();
            }

            while (true)
            {
                ReadToNodeType(reader, XmlNodeType.Element, XmlNodeType.EndElement);

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        PropertyInfo property = objType.GetProperty(reader.Name);
                        string attrType = reader["type"];
                        Type type = null;
                        if (attrType != null)
                            type = Reflector.ResolveType(attrType);
                        if (type == null && property != null)
                            type = property.PropertyType;

                        // real property
                        if (property != null)
                        {
                            // collection property
                            if (type.IsCollection())
                            {
                                // array
                                if (type.IsArray)
                                {
                                    // null array
                                    if (reader.IsEmptyElement)
                                    {
                                        if (property.CanWrite)
                                        {
                                            Reflector.SetProperty(obj, property, null);
                                            continue;
                                        }
                                        throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetterNull, type, objType.FullName, property.Name));
                                    }

                                    Array array = null;

                                    // property with setter: creating a new array
                                    if (property.CanWrite)
                                    {
                                        DeserializeArray(ref array, type.GetElementType(), reader);
                                        Reflector.SetProperty(obj, property, array);
                                    }

                                    // read-only array
                                    else
                                    {
                                        array = Reflector.GetProperty(obj, property) as Array;
                                        if (array == null)
                                            throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetter, type, objType, property.Name));

                                        DeserializeArray(ref array, null, reader);
                                    }
                                    continue;
                                }

                                // non-array collection
                                IEnumerable collection = Reflector.GetProperty(obj, property) as IEnumerable;

                                // 1.) collection != null, reader empty -> setting null
                                if (collection != null && reader.IsEmptyElement)
                                {
                                    if (!property.CanWrite)
                                        throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetterNull, type, objType, property.Name));
                                    Reflector.SetProperty(obj, property, null);
                                    continue;
                                }

                                // 2.) collection == null, reader not empty -> creating and setting collection property, default constructor and setter needed
                                if (collection == null && !reader.IsEmptyElement)
                                {
                                    if (!property.CanWrite)
                                        throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetter, type, objType, property.Name));
                                    else
                                    {
                                        try
                                        {
                                            collection = (IEnumerable)Reflector.Construct(type);
                                        }
                                        catch (Exception e)
                                        {
                                            throw new ReflectionException(Res.Get(Res.XmlCannotCreateCollection, objType), e);
                                        }
                                        Reflector.SetProperty(obj, property, collection);
                                    }
                                }

                                // 3.) reader not empty (collection is not null here) -> deserializing (clear is in deserialization)
                                if (!reader.IsEmptyElement)
                                    DeserializeComponent(collection, reader);

                                continue;
                            }
                            // non-collection property
                            else
                            {
                                if (!property.CanWrite)
                                    throw new ReflectionException(Res.Get(Res.XmlPropertyHasNoSetter, property, objType.Name));

                                // Using explicitly defined type converter if can convert from string
                                object[] attrs = property.GetCustomAttributes(typeof(TypeConverterAttribute), true);
                                TypeConverterAttribute convAttr = attrs.Length > 0 ? attrs[0] as TypeConverterAttribute : null;
                                if (convAttr != null)
                                {
                                    Type convType = Type.GetType(convAttr.ConverterTypeName);
                                    if (convType != null)
                                    {
                                        ConstructorInfo ctor = convType.GetConstructor(new Type[] { typeof(Type) });
                                        object[] ctorParams = new object[] { property.PropertyType };
                                        if (ctor == null)
                                        {
                                            ctor = convType.GetConstructor(Type.EmptyTypes);
                                            ctorParams = Reflector.EmptyObjects;
                                        }
                                        if (ctor != null)
                                        {
                                            TypeConverter converter = Reflector.Construct(ctor, ctorParams) as TypeConverter;
                                            if (converter != null && converter.CanConvertFrom(typeof(string)))
                                            {
                                                Reflector.SetProperty(obj, property, converter.ConvertFrom(null, CultureInfo.InvariantCulture, ReadStringValue(reader)));
                                                continue;
                                            }
                                        }
                                    }
                                }

                                object result;
                                if (TryDeserializeObject(type, reader, out result))
                                {
                                    Reflector.SetProperty(obj, property, result);
                                    continue;
                                }

                                throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));
                            }
                        }
                        // collection element
                        else if (objType.IsCollection())
                        {
                            if (reader.Name != "item")
                                throw new ArgumentException(Res.Get(Res.XmlItemExpected, reader.Name));

                            IEnumerable collection = (IEnumerable)obj;

                            // adding null item
                            if (reader.IsEmptyElement)
                            {
                                collection.Add(null);
                                continue;
                            }

                            object item;
                            if (TryDeserializeObject(type ?? collectionElementType, reader, out item))
                            {
                                collection.Add(item);
                                continue;
                            }

                            if (type == null)
                                throw new ArgumentException(Res.Get(Res.XmlCannotDetermineElementType, objType));
                            throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, type));
                        }

                        if (reader.Name == "item")
                            throw new SerializationException(Res.Get(Res.XmlNotACollection, objType));
                        else
                            throw new ReflectionException(Res.Get(Res.XmlHasNoProperty, obj, reader.Name));

                    case XmlNodeType.EndElement:
                        // Disabled because of OrderedDictionary. TODO: Some similar custom interface
                        //IDeserializationCallback callbackCapable = obj as IDeserializationCallback;
                        //if (callbackCapable != null)
                        //    callbackCapable.OnDeserialization(null);
                        return;
                }
            }
        }

        private static void DeserializeXmlSerializable(IXmlSerializable xmlSerializable, XContainer parent)
        {
            XElement content = parent.Elements().FirstOrDefault();
            if (content == null)
                throw new ArgumentException(Res.Get(Res.XmlNoContent, xmlSerializable.GetType()));
            using (XmlReader xr = XmlReader.Create(new StringReader(content.ToString()), new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreWhitespace = true
            }))
            {
                xr.Read();

                // passing the reader to the object to read itself
                xmlSerializable.ReadXml(xr);
            }
        }

        private static void DeserializeXmlSerializable(IXmlSerializable xmlSerializable, XmlReader reader)
        {
            // to XmlRoot or type name
            ReadToNodeType(reader, XmlNodeType.Element);

            // passing the reader to the object to read itself
            xmlSerializable.ReadXml(reader);

            // to end of XmlRoot or type name
            ReadToNodeType(reader, XmlNodeType.EndElement);
        }

        /// <summary>
        /// Array deserialization, XElement version
        /// </summary>
        private static void DeserializeArray(ref Array array, Type elementType, XElement element)
        {
            if (array == null && elementType == null)
                throw new ArgumentNullException("elementType", Res.Get(Res.ArgumentNull));

            int length = 0;
            int[] lengths = null;
            int[] lowerBounds = null;
            XAttribute attrLength = element.Attribute("length");
            XAttribute attrDim = element.Attribute("dim");

            if (attrLength != null)
            {
                if (!Int32.TryParse(attrLength.Value, out length))
                    throw new ArgumentException(Res.Get(Res.XmlLengthInvalidType, attrLength));
            }
            else if (attrDim != null)
            {
                string[] dims = attrDim.Value.Split(',');
                lengths = new int[dims.Length];
                lowerBounds = new int[dims.Length];
                for (int i = 0; i < dims.Length; i++)
                {
                    int boundSep = dims[i].IndexOf("..", StringComparison.InvariantCulture);
                    if (boundSep == -1)
                    {
                        lowerBounds[i] = 0;
                        lengths[i] = Int32.Parse(dims[i]);
                    }
                    else
                    {
                        lowerBounds[i] = Int32.Parse(dims[i].Substring(0, boundSep));
                        lengths[i] = Int32.Parse(dims[i].Substring(boundSep + 2)) - lowerBounds[i] + 1;
                    }
                }

                length = lengths.Aggregate(1, (acc, len) => acc * len);
            }

            // creating a new array
            if (array == null)
            {
                array = lengths != null ? Array.CreateInstance(elementType, lengths, lowerBounds) : Array.CreateInstance(elementType, length);
            }

            // checking the existing array
            else
            {
                if (length != array.Length)
                    throw new ArgumentException(Res.Get(Res.XmlArraySizeMismatch, array.GetType(), length));

                if (lengths != null)
                {
                    if (lengths.Length != array.Rank)
                        throw new ArgumentException(Res.Get(Res.XmlArrayRankMismatch, array.GetType(), lengths.Length));

                    for (int i = 0; i < lengths.Length; i++)
                    {
                        if (lengths[i] != array.GetLength(i))
                            throw new ArgumentException(Res.Get(Res.XmlArrayDimensionSizeMismatch, array.GetType(), i));

                        if (lowerBounds[i] != array.GetLowerBound(i))
                            throw new ArgumentException(Res.Get(Res.XmlArrayLowerBoundMismatch, array.GetType(), i));
                    }
                }
            }

            XElement elementData = element.Element("Data");
            // has Data element or has no elements: primitive array (can be restored by BlockCopy)
            if (elementData != null || (length > 0 && !element.HasElements))
            {
                string value = elementData != null ? elementData.Value : element.Value;
                XAttribute attrComp = element.Attribute("comp");

                byte[] data = attrComp != null && attrComp.Value == "base64" ? Convert.FromBase64String(value) : value.ParseHexBytes();

                string crc = null;
                XAttribute attrCrc = element.Attribute("CRC");
                if (attrCrc != null)
                    crc = attrCrc.Value;
                else
                {
                    XElement elementCrc = element.Element("CRC");
                    if (elementCrc != null)
                        crc = elementCrc.Value;
                }

                if (crc != null)
                {
                    if (Crc32.CalculateHash(data).ToString("X8") != crc)
                        throw new ArgumentException(Res.Get(Res.XmlCrcError));
                }

                Buffer.BlockCopy(data, 0, array, 0, data.Length);
                return;
            }

            // complex array: recursive deserialization needed
            Queue<XElement> items = new Queue<XElement>(element.Elements("item"));
            if (items.Count != array.Length)
                throw new ArgumentException(Res.Get(Res.XmlInconsistentArrayLength, array.Length, items.Count));

            ArrayIndexer arrayIndexer = new ArrayIndexer(lengths ?? new int[] { length }, lowerBounds ?? new int[] { 0 });
            while (arrayIndexer.MoveNext())
            {
                XElement item = items.Dequeue();
                Type itemType = null;
                XAttribute attrType = item.Attribute("type");
                if (attrType != null)
                    itemType = Reflector.ResolveType(attrType.Value);
                if (itemType == null)
                    itemType = array.GetType().GetElementType();

                object value;
                if (TryDeserializeObject(itemType, item, out value))
                {
                    array.SetValue(value, arrayIndexer.Current);
                }
                else
                    throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, itemType));
            }
        }

        /// <summary>
        /// Array deserialization
        /// XmlReader version. Position is before content (on parent start element). On exit position is on parent close element.
        /// Parent is not empty here.
        /// </summary>
        private static void DeserializeArray(ref Array array, Type arrayType, XmlReader reader)
        {
            if (array == null && arrayType == null)
                throw new ArgumentNullException("arrayType", Res.Get(Res.ArgumentNull));

            int length = 0;
            int[] lengths = null;
            int[] lowerBounds = null;
            string attrLength = reader["length"];
            string attrDim = reader["dim"];

            if (attrLength != null)
            {
                if (!Int32.TryParse(attrLength, out length))
                    throw new ArgumentException(Res.Get(Res.XmlLengthInvalidType, attrLength));
            }
            else if (attrDim != null)
            {
                string[] dims = attrDim.Split(',');
                lengths = new int[dims.Length];
                lowerBounds = new int[dims.Length];
                for (int i = 0; i < dims.Length; i++)
                {
                    int boundSep = dims[i].IndexOf("..", StringComparison.InvariantCulture);
                    if (boundSep == -1)
                    {
                        lowerBounds[i] = 0;
                        lengths[i] = Int32.Parse(dims[i]);
                    }
                    else
                    {
                        lowerBounds[i] = Int32.Parse(dims[i].Substring(0, boundSep));
                        lengths[i] = Int32.Parse(dims[i].Substring(boundSep + 2)) - lowerBounds[i] + 1;
                    }
                }

                length = lengths.Aggregate(1, (acc, len) => acc * len);
            }

            // creating a new array
            if (array == null)
            {
                array = lengths != null ? Array.CreateInstance(arrayType, lengths, lowerBounds) : Array.CreateInstance(arrayType, length);
            }

            // checking the existing array
            else
            {
                if (length != array.Length)
                    throw new ArgumentException(Res.Get(Res.XmlArraySizeMismatch, array.GetType(), length));

                if (lengths != null)
                {
                    if (lengths.Length != array.Rank)
                        throw new ArgumentException(Res.Get(Res.XmlArrayRankMismatch, array.GetType(), lengths.Length));

                    for (int i = 0; i < lengths.Length; i++)
                    {
                        if (lengths[i] != array.GetLength(i))
                            throw new ArgumentException(Res.Get(Res.XmlArrayDimensionSizeMismatch, array.GetType(), i));

                        if (lowerBounds[i] != array.GetLowerBound(i))
                            throw new ArgumentException(Res.Get(Res.XmlArrayLowerBoundMismatch, array.GetType(), i));
                    }
                }
            }

            string attrCrc = reader["CRC"];
            uint? origCrc = null, actualCrc = null;
            if (attrCrc != null)
            {
                uint crc;
                if (!UInt32.TryParse(attrCrc, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out crc))
                    throw new ArgumentException(Res.Get(Res.XmlCrcFormat, attrCrc));
                origCrc = crc;
            }

            string attrComp = reader["comp"];
            int deserializedItemsCount = 0;
            ArrayIndexer arrayIndexer = lengths == null ? null : new ArrayIndexer(lengths, lowerBounds);
            bool oldWay = false;
            do
            {
                ReadToNodeType(reader, XmlNodeType.Element, XmlNodeType.Text, XmlNodeType.EndElement);

                switch (reader.NodeType)
                {
                    case XmlNodeType.Text:
                        if (deserializedItemsCount > 0)
                            throw new ArgumentException(Res.Get(Res.XmlMixedArrayFormats));

                        // primitive array (can be restored by BlockCopy)
                        byte[] data = attrComp != null && attrComp == "base64" ? Convert.FromBase64String(reader.Value) : reader.Value.ParseHexBytes();

                        // non-old way: crc can be missing and in such case crc is not calculated
                        if (origCrc != null || oldWay)
                        {
                            uint crc = Crc32.CalculateHash(data);
                            if (origCrc != null)
                            {
                                if (crc != origCrc.Value)
                                    throw new ArgumentException(Res.Get(Res.XmlCrcError));
                            }
                            else
                            {
                                // crc will be checked later in CRC element
                                actualCrc = crc;
                            }
                        }

                        Buffer.BlockCopy(data, 0, array, 0, data.Length);
                        deserializedItemsCount = length;
                        break;

                    case XmlNodeType.Element:
                        // complex array: recursive deserialization needed
                        if (reader.Name == "item")
                        {
                            Type elementType = null;
                            string attrType = reader["type"];
                            if (attrType != null)
                                elementType = Reflector.ResolveType(attrType);
                            if (elementType == null)
                                elementType = array.GetType().GetElementType();

                            object item;
                            if (TryDeserializeObject(elementType, reader, out item))
                            {
                                if (arrayIndexer == null)
                                    array.SetValue(item, deserializedItemsCount);
                                else
                                {
                                    arrayIndexer.MoveNext();
                                    array.SetValue(item, arrayIndexer.Current);
                                }

                                deserializedItemsCount++;
                                continue;
                            }

                            throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, elementType));
                        }

                        //// supported for backward compatibility: primitive array is in a Data element
                        //if (reader.Name == "Data")
                        //{
                        //    if (deserializedItemsCount > 0)
                        //        throw new ArgumentException("Multiple Data elements or mixed Data and item elements occured.");

                        //    ReadToNodeType(reader, XmlNodeType.Text);
                        //    oldWay = true;
                        //    goto case XmlNodeType.Text;
                        //}

                        //// supported for backward compatibility: CRC element instead of attribute
                        //if (reader.Name == "CRC")
                        //{
                        //    if ((deserializedItemsCount > 0 && deserializedItemsCount < length) || origCrc != null)
                        //        throw new ArgumentException("Multiple CRC elements or mixed CRC and item elements occured.");

                        //    ReadToNodeType(reader, XmlNodeType.Text);
                        //    uint crc;
                        //    if (!UInt32.TryParse(reader.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out crc))
                        //        throw new ArgumentException(String.Format("CRC element value should be a hex value but '{0}' found", reader.Value));

                        //    // Data already deserialized: checking crc
                        //    if (actualCrc.HasValue)
                        //    {
                        //        if (actualCrc.Value != crc)
                        //            throw new ArgumentException(String.Format("Corrupt array data: Bad CRC"));

                        //        ReadToNodeType(reader, XmlNodeType.EndElement); // CRC
                        //        ReadToNodeType(reader, XmlNodeType.EndElement); // Parent end
                        //        return;
                        //    }

                        //    // continue deserializing
                        //    origCrc = crc;
                        //    continue;
                        //}

                        throw new ArgumentException(Res.Get(Res.XmlUnexpectedElement, reader.Name));

                    case XmlNodeType.EndElement:
                        if (reader.Name.In("Data", "CRC"))
                            continue;

                        // in end element of parent: checking items count
                        if (deserializedItemsCount != array.Length)
                            throw new ArgumentException(Res.Get(Res.XmlInconsistentArrayLength, array.Length, deserializedItemsCount));

                        return;
                }
            }
            while (true);
        }

        /// <summary>
        /// Deserialize object - XElement version
        /// </summary>
        private static bool TryDeserializeObject(Type type, XElement element, out object result)
        {
            // a.) null value
            if (element.IsEmpty && (type == null || !type.IsValueType || type.IsNullable()))
            {
                result = null;
                return true;
            }

            if (type != null && type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            // b.) If type can natively parsed, parsing from string
            if (type != null && Reflector.CanParseNatively(type))
            {
                string value = ReadStringValue(element);
                result = Reflector.Parse(type, value);
                return true;
            }

            // c.) Using type converter of the type if applicable
            if (type != null)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, ReadStringValue(element));
                    return true;
                }
            }

            // d.) simple object
            if (type == Reflector.ObjectType && !element.IsEmpty && element.Value.Length == 0)
            {
                result = new object();
                return true;
            }

            // e.) key/value pair
            XAttribute attrFormat = element.Attribute("format");
            if (attrFormat != null && attrFormat.Value == "keyvalue")
            {
                if (type == null)
                    throw new ArgumentException(Res.Get(Res.XmlKeyValueTypeMissing));

                object key;
                object value;

                // key
                XElement xItem = element.Element("Key");
                if (xItem == null)
                    throw new ArgumentException(Res.Get(Res.XmlKeyValueMissingKey));
                XAttribute xType = xItem.Attribute("type");
                Type itemType;
                if (xType != null)
                    itemType = Reflector.ResolveType(xType.Value);
                else
                {
                    itemType = typeof(object);
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        itemType = type.GetGenericArguments()[0];
                }
                if (!TryDeserializeObject(itemType, xItem, out key))
                {
                    if (xType != null && itemType == null)
                        throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, xType.Value));
                    throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, itemType));
                }

                // value
                xItem = element.Element("Value");
                if (xItem == null)
                    throw new ArgumentException(Res.Get(Res.XmlKeyValueMissingValue));
                xType = xItem.Attribute("type");
                if (xType != null)
                    itemType = Reflector.ResolveType(xType.Value);
                else
                {
                    itemType = typeof(object);
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        itemType = type.GetGenericArguments()[1];
                }
                if (!TryDeserializeObject(itemType, xItem, out value))
                {
                    if (xType != null && itemType == null)
                        throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, xType.Value));
                    throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, itemType));
                }
                result = Reflector.Construct(type, key, value);
                return true;
            }

            // f.) ValueType as binary
            if (type != null && attrFormat != null && attrFormat.Value.In("structbase64", "structbinary") && type.IsValueType)
            {
                byte[] data = attrFormat.Value == "structbase64" ? Convert.FromBase64String(element.Value) : element.Value.ParseHexBytes();
                XAttribute attrCrc = element.Attribute("CRC");
                if (attrCrc != null)
                {
                    if (Crc32.CalculateHash(data).ToString("X8") != attrCrc.Value)
                        throw new ArgumentException(Res.Get(Res.XmlCrcError));
                }

                result = BinarySerializer.DeserializeStruct(type, data);
                return true;
            }

            // g.) Binary
            if (attrFormat != null && attrFormat.Value.In("base64", "binary"))
            {
                if (element.IsEmpty)
                    result = null;
                else
                {
                    byte[] data = attrFormat.Value == "base64" ? Convert.FromBase64String(element.Value) : element.Value.ParseHexBytes();
                    XAttribute attrCrc = element.Attribute("CRC");
                    if (attrCrc != null)
                    {
                        if (Crc32.CalculateHash(data).ToString("X8") != attrCrc.Value)
                            throw new ArgumentException(Res.Get(Res.XmlCrcError));
                    }

                    result = BinarySerializer.Deserialize(data);
                }
                return true;
            }

            // h.) recursive deserialization (including IXmlSerializable)
            if (type != null && !element.IsEmpty)
            {
                if (type.IsArray)
                {
                    Array array = null;
                    DeserializeArray(ref array, type.GetElementType(), element);
                    result = array;
                    return true;
                }

                object child = Reflector.Construct(type);

                // can be null if type is nullable
                DeserializeComponent(child, element);
                result = child;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Deserialize object - XmlReader version.
        /// Reader is at open element at start and is at end element at the end.
        /// </summary>
        private static bool TryDeserializeObject(Type type, XmlReader reader, out object result)
        {
            // a.) null value
            if (reader.IsEmptyElement && (type == null || !type.IsValueType || type.IsNullable()))
            {
                result = null;
                return true;
            }

            if (type != null && type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            // b.) If type can natively parsed, parsing from string
            if (type != null && Reflector.CanParseNatively(type))
            {
                string value = ReadStringValue(reader);
                result = Reflector.Parse(type, value);
                return true;
            }

            // c.) Using type converter of the type if applicable
            if (type != null)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, ReadStringValue(reader));
                    return true;
                }
            }

            // d.) key/value pair
            string attrFormat = reader["format"];
            if (attrFormat != null && attrFormat == "keyvalue")
            {
                if (type == null)
                    throw new ArgumentException(Res.Get(Res.XmlKeyValueTypeMissing));

                bool keyRead = false;
                bool valueRead = false;
                object key = null;
                object value = null;

                while (true)
                {
                    ReadToNodeType(reader, XmlNodeType.Element, XmlNodeType.EndElement);
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "Key":
                                    if (keyRead)
                                        throw new ArgumentException(Res.Get(Res.XmlMultipleKeys));

                                    keyRead = true;
                                    string attrType = reader["type"];
                                    Type itemType;
                                    if (attrType != null)
                                        itemType = Reflector.ResolveType(attrType);
                                    else
                                    {
                                        itemType = typeof(object);
                                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                                            itemType = type.GetGenericArguments()[0];
                                    }
                                    if (!TryDeserializeObject(itemType, reader, out key))
                                    {
                                        if (attrType != null && itemType == null)
                                            throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType));
                                        throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, itemType));
                                    }
                                    break;

                                case "Value":
                                    if (valueRead)
                                        throw new ArgumentException(Res.Get(Res.XmlMultipleValues));

                                    valueRead = true;
                                    attrType = reader["type"];
                                    if (attrType != null)
                                        itemType = Reflector.ResolveType(attrType);
                                    else
                                    {
                                        itemType = typeof(object);
                                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                                            itemType = type.GetGenericArguments()[1];
                                    }
                                    if (!TryDeserializeObject(itemType, reader, out value))
                                    {
                                        if (attrType != null && itemType == null)
                                            throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType));
                                        throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, itemType));
                                    }
                                    break;

                                default:
                                    throw new ArgumentException(Res.Get(Res.XmlUnexpectedElement, reader.Name));
                            }
                            break;

                        case XmlNodeType.EndElement:
                            // end of keyvalue: checking whether both key and value have been read
                            if (!keyRead)
                                throw new ArgumentException(Res.Get(Res.XmlKeyValueMissingKey));
                            if (!valueRead)
                                throw new ArgumentException(Res.Get(Res.XmlKeyValueMissingValue));

                            result = Reflector.Construct(type, key, value);
                            return true;
                    }
                }
            }

            // e.) ValueType as binary
            if (type != null && attrFormat != null && attrFormat.In("structbase64", "structbinary") && type.IsValueType)
            {
                string attrCrc = reader["CRC"];
                ReadToNodeType(reader, XmlNodeType.Text, XmlNodeType.EndElement);
                byte[] data = reader.NodeType == XmlNodeType.Text
                    ? (attrFormat == "structbase64" ? Convert.FromBase64String(reader.Value) : reader.Value.ParseHexBytes())
                    : new byte[0];
                if (attrCrc != null)
                {
                    if (Crc32.CalculateHash(data).ToString("X8") != attrCrc)
                        throw new ArgumentException(Res.Get(Res.XmlCrcError));
                }

                result = BinarySerializer.DeserializeStruct(type, data);
                if (data.Length > 0)
                    ReadToNodeType(reader, XmlNodeType.EndElement);
                return true;
            }

            // f.) Binary
            if (attrFormat.In("base64", "binary"))
            {
                if (reader.IsEmptyElement)
                    result = null;
                else
                {
                    string attrCrc = reader["CRC"];
                    ReadToNodeType(reader, XmlNodeType.Text);
                    byte[] data = attrFormat == "base64" ? Convert.FromBase64String(reader.Value) : reader.Value.ParseHexBytes();
                    if (attrCrc != null)
                    {
                        if (Crc32.CalculateHash(data).ToString("X8") != attrCrc)
                            throw new ArgumentException(Res.Get(Res.XmlCrcError));
                    }

                    result = BinarySerializer.Deserialize(data);
                    ReadToNodeType(reader, XmlNodeType.EndElement);
                }
                return true;
            }

            // g.) recursive deserialization (including IXmlSerializable)
            if (type != null && !reader.IsEmptyElement)
            {
                if (type.IsArray)
                {
                    Array array = null;
                    DeserializeArray(ref array, type.GetElementType(), reader);
                    result = array;
                    return true;
                }

                object child = Reflector.Construct(type);
                DeserializeComponent(child, reader);
                result = child;
                return true;
            }

            result = null;
            return false;
        }

        private static string ReadStringValue(XElement element)
        {
            if (element.IsEmpty)
                return null;

            XAttribute attrEscaped = element.Attribute("escaped");
            if (attrEscaped == null || attrEscaped.Value != "true")
                return element.Value;

            return UnescapeString(element.Value);
        }

        /// <summary>
        /// Reads a string from XmlReader.
        /// On start, reader is in conteiner element, on end on the end element.
        /// </summary>
        private static string ReadStringValue(XmlReader reader)
        {
            // empty: remaining in element position and returning null
            if (reader.IsEmptyElement)
                return null;

            bool escaped = reader["escaped"] == "true";

            // non-empty: reading to en element and returning content
            StringBuilder result = new StringBuilder();
            do
            {
                reader.Read();
                if (reader.NodeType.In(XmlNodeType.Text, XmlNodeType.SignificantWhitespace, XmlNodeType.EntityReference, XmlNodeType.Whitespace))
                    result.Append(reader.Value);
            }
            while (reader.NodeType != XmlNodeType.EndElement);

            if (!escaped)
                return result.ToString();

            return UnescapeString(result.ToString());
        }

        private static string UnescapeString(string s)
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

        private static void ReadToNodeType(XmlReader reader, params XmlNodeType[] nodeTypes)
        {
            do
            {
                if (!reader.Read())
                    throw new ArgumentException(Res.Get(Res.XmlUnexpectedEnd));

                if (reader.NodeType.In(nodeTypes))
                    return;

                if (reader.NodeType.In(XmlNodeType.Whitespace, XmlNodeType.Comment, XmlNodeType.XmlDeclaration))
                    continue;

                throw new ArgumentException(Res.Get(Res.XmlUnexpectedElement, Enum<XmlNodeType>.ToString(reader.NodeType)));
            }
            while (true);
        }

        #endregion

        #region Private Helper Methods

        private static string GetTypeString(Type type, XmlSerializationOptions options)
        {   
            return type.GetTypeName((options & XmlSerializationOptions.FullyQualifiedNames) != XmlSerializationOptions.None);
        }

        /// <summary>
        /// Registers object to detect circular reference.
        /// Must be called from inside of try-finally to remove lock in finally if neccessary.
        /// </summary>
        private static void RegisterSerializedObject(object obj)
        {
            if (obj == null || obj.GetType().IsValueType)
                return;

            // putting lock when serialization of complex instance is started
            if (serializationLevel == 0)
            {
                Monitor.Enter(syncRootSerialize);
                serObjects = new HashSet<object>(ReferenceEqualityComparer.Comparer);
            }

            serializationLevel++;

            if (serObjects.Contains(obj))
            {
                throw new ReflectionException(Res.Get(Res.XmlCircularReference, obj));
            }

            serObjects.Add(obj);
        }

        private static void UnregisterSerializedObject(object obj)
        {
            if (obj == null || obj.GetType().IsValueType)
                return;

            serObjects.Remove(obj);
            serializationLevel--;

            // removing lock when serialization of the complex root instance is finished
            if (serializationLevel == 0)
            {
                serObjects = null;
                Monitor.Exit(syncRootSerialize);
            }
        }

        #endregion

        #region Private Extension Methods

        private static Type GetElementType(this IEnumerable collection)
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

        private static bool IsRecursiveSerializationEnabled(this XmlSerializationOptions options)
        {
            return (options & XmlSerializationOptions.RecursiveSerializationAsFallback) != XmlSerializationOptions.None;
        }

        private static bool IsForcedSerializationValueTypesEnabled(this XmlSerializationOptions options)
        {
#pragma warning disable 618,612 // Disabling warning for obsolete enum member because this must be still handled
            return (options & XmlSerializationOptions.ForcedSerializationValueTypesAsFallback) != XmlSerializationOptions.None;
#pragma warning restore 618,612
        }

        private static bool IsBinarySerializationEnabled(this XmlSerializationOptions options)
        {
            return (options & XmlSerializationOptions.BinarySerializationAsFallback) != XmlSerializationOptions.None;
        }

        private static bool IsCompactSerializationValueTypesEnabled(this XmlSerializationOptions options)
        {
            return (options & XmlSerializationOptions.CompactSerializationOfStructures) != XmlSerializationOptions.None;
        }

        private static BinarySerializationOptions ToBinarySerializationOptions(this XmlSerializationOptions options)
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

        #endregion
    }
}

