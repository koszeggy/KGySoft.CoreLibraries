using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    internal static class XmlReaderDeserializer
    {
        /// <summary>
        /// Deserializes an object using the provided <see cref="XmlReader"/> in <paramref name="reader"/> parameter.
        /// </summary>
        public static object Deserialize(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader), Res.Get(Res.ArgumentNull));

            ReadToNodeType(reader, XmlNodeType.Element);
            if (reader.Name != "object")
                throw new ArgumentException(Res.Get(Res.XmlRootExpected, reader.Name), nameof(reader));

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
                    throw new ArgumentException(Res.Get(Res.XmlRootTypeMissing), nameof(reader));

                throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));
            }
            return result;
        }

        /// <summary>
        /// Deserializes an object or collection of objects.
        /// Position is before content (on parent start element). On exit position is in parent close element.
        /// </summary>
        public static void DeserializeContent(XmlReader reader, object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

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
                        {
                            type = Reflector.ResolveType(attrType);
                            if (type == null)
                                throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType));
                        }
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
                                    DeserializeContent(reader, collection);

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
                DeserializeContent(reader, child);
                result = child;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Array deserialization
        /// XmlReader version. Position is before content (on parent start element). On exit position is on parent close element.
        /// Parent is not empty here.
        /// </summary>
        private static void DeserializeArray(ref Array array, Type arrayType, XmlReader reader)
        {
            if (array == null && arrayType == null)
                throw new ArgumentNullException(nameof(arrayType), Res.Get(Res.ArgumentNull));

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
        /// Reads a string from XmlReader.
        /// At start, reader is in container element, at the end in the end element.
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

            return result.ToString().Unescape();
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
    }
}
