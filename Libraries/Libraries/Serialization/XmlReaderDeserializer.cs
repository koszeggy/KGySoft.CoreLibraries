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
    /// <summary>
    /// XmlReader version of XML deserialization.
    /// Actually a static class with base types - hence marked as abstract. Unlike on serialization no fields are used so no instance is needed.
    /// </summary>
    internal abstract class XmlReaderDeserializer : XmlDeserializerBase
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

            if (TryDeserializeObject(objType, reader, out var result))
                return result;

            if (attrType == null)
                throw new ArgumentException(Res.Get(Res.XmlRootTypeMissing), nameof(reader));
            throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));
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

            // deserialize IXmlSerializable content
            string attrFormat = reader["format"];
            if (attrFormat == "custom")
            {
                if (!(obj is IXmlSerializable xmlSerializable))
                    throw new ArgumentException(Res.Get(Res.NotAnIXmlSerializable, objType));
                DeserializeXmlSerializable(xmlSerializable, reader);
                return;
            }

            // deserialize array
            if (objType.IsArray)
            {
                Array array = (Array)obj;
                DeserializeArray(array, null, reader);
                return;
            }

            // Populatable collection: clearing it before restoring (root-level DeserializeContent, collection of read-only properties)
            Type collectionElementType = null;
            if (objType.IsCollection())
            {
                if (!objType.IsReadWriteCollection(obj))
                    throw new SerializationException(Res.Get(Res.XmlDeserializeReadOnlyCollection, objType));

                collectionElementType = objType.GetCollectionElementType();
                IEnumerable collection = (IEnumerable)obj;
                collection.Clear();
            }

            DeserializePropertiesAndElements(reader, obj, objType, collectionElementType, null);
        }

        /// <summary>
        /// Deserializes a non-populatable collection by an initializer collection.
        /// </summary>
        private static object DeserializeContentByInitializerCollection(Type collectionRealType, XmlReader reader, ConstructorInfo collectionCtor, Type collectionElementType, bool isDictionary)
        {
            object initializerCollection = isDictionary
                ? (collectionElementType.IsGenericType ? Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(collectionElementType.GetGenericArguments())) : new Dictionary<object, object>())
                : Activator.CreateInstance(typeof(List<>).MakeGenericType(collectionElementType));

            var properties = new Dictionary<PropertyInfo, object>();
            DeserializePropertiesAndElements(reader, initializerCollection, collectionRealType, collectionElementType, properties);
            AdjustInitializerCollection(ref initializerCollection, collectionRealType, collectionCtor);

            object result = Reflector.Construct(collectionCtor, initializerCollection);
            foreach (var property in properties)
            {
                // read-only property
                if (!property.Key.CanWrite)
                {
                    object existingValue = Reflector.GetProperty(result, property.Key);
                    Type createdType = property.Value?.GetType();
                    Type propertyType = property.Key.PropertyType;
                    if (existingValue != null && property.Value == null)
                    {
                        if (propertyType.IsArray)
                            throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetterNull, propertyType, collectionRealType, property.Key.Name));
                        if (propertyType.IsCollection())
                            throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetterNull, propertyType, collectionRealType, property.Key.Name));
                    }
                    else if (existingValue == null && property.Value != null)
                    {
                        if (propertyType.IsArray)
                            throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetter, propertyType, collectionRealType, property.Key.Name));
                        if (propertyType.IsCollection())
                            throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetter, propertyType, collectionRealType, property.Key.Name));
                    }
                    else
                    {
                        if (existingValue == null && property.Value == null)
                            continue;
                        if (propertyType != createdType)
                            throw new ArgumentException(Res.Get(Res.XmlPropertyTypeMismatch, collectionRealType, property.Key.Name, createdType, propertyType));

                        // copy from existing array
                        if (existingValue is Array existingArray && property.Value is Array arrayToSet)
                        {
                            int[] lengths = new int[arrayToSet.Rank];
                            int[] lowerBounds = new int[arrayToSet.Rank];
                            for (int i = 0; i < arrayToSet.Rank; i++)
                            {
                                lengths[i] = arrayToSet.GetLength(i);
                                lowerBounds[i] = arrayToSet.GetLowerBound(i);
                            }

                            CheckArray(existingArray, arrayToSet.Length, lengths, lowerBounds);
                            if (collectionElementType.IsPrimitive)
                                Buffer.BlockCopy(arrayToSet, 0, existingArray, 0, Buffer.ByteLength(arrayToSet));
                            else
                            {
                                var indices = new ArrayIndexer(lengths, lowerBounds);
                                while (indices.MoveNext())
                                    existingArray.SetValue(existingArray.GetValue(indices.Current), indices.Current);
                            }

                            continue;
                        }

                        // copy from existing collection
                        if (propertyType.IsCollection())
                        {
                            if (!propertyType.IsReadWriteCollection(existingValue))
                                throw new SerializationException(Res.Get(Res.XmlDeserializeReadOnlyCollection, propertyType));

                            IEnumerable collection = (IEnumerable)existingValue;
                            collection.Clear();
                            foreach (var item in (IEnumerable)property.Value)
                                collection.Add(item);

                            continue;
                        }

                        throw new ArgumentException(Res.Get(Res.XmlDeserializeReadOnlyProperty, collectionRealType, property.Key.Name, propertyType));
                    }
                }

                // read-write property
                Reflector.SetProperty(result, property.Key, property.Value);
            }

            return result;
        }

        /// <summary>
        /// Deserializes the properties and elements of <paramref name="objRealType"/>.
        /// Type of <paramref name="obj"/> can be different of <paramref name="objRealType"/> if a proxy collection object is populated for initialization.
        /// In this case properties have to be stored for later initialization into <paramref name="properties"/> and <paramref name="obj"/> is a populatable collection for sure.
        /// <paramref name="collectionElementType"/> is <see langword="null"/> only if <paramref name="objRealType"/> is not a supported collection.
        /// </summary>
        private static void DeserializePropertiesAndElements(XmlReader reader, object obj, Type objRealType, Type collectionElementType, Dictionary<PropertyInfo, object> properties)
        {
            while (true)
            {
                ReadToNodeType(reader, XmlNodeType.Element, XmlNodeType.EndElement);

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        PropertyInfo property = objRealType.GetProperty(reader.Name);
                        string attrType = reader["type"];
                        Type itemType = null;
                        if (attrType != null)
                        {
                            itemType = Reflector.ResolveType(attrType);
                            if (itemType == null)
                                throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType));
                        }
                        if (itemType == null && property != null)
                            itemType = property.PropertyType;

                        // 1.) real property
                        if (property != null)
                        {
                            // 1.a.) read-only property and it must be initialized (not a cached property for late initialization)
                            if (properties == null && !property.CanWrite)
                            {
                                object propertyValue = Reflector.GetProperty(obj, property);
                                if (propertyValue != null && reader.IsEmptyElement)
                                {
                                    if (itemType.IsArray)
                                        throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetterNull, itemType, objRealType, property.Name));
                                    if (itemType.IsCollection())
                                        throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetterNull, itemType, objRealType, property.Name));
                                }
                                else if (propertyValue == null && !reader.IsEmptyElement)
                                {
                                    if (itemType.IsArray)
                                        throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetter, itemType, objRealType, property.Name));
                                    if (itemType.IsCollection())
                                        throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetter, itemType, objRealType, property.Name));
                                }
                                else
                                {
                                    if (propertyValue == null && reader.IsEmptyElement)
                                        continue;
                                    if (itemType != propertyValue.GetType())
                                        throw new ArgumentException(Res.Get(Res.XmlPropertyTypeMismatch, objRealType, property.Name, itemType, propertyValue.GetType()));

                                    if (itemType.IsArray)
                                    {
                                        DeserializeArray((Array)propertyValue, null, reader);
                                        continue;
                                    }

                                    if (itemType.IsCollection())
                                    {
                                        // both read-write check and Clear are in DeserializeContent
                                        DeserializeContent(reader, propertyValue);
                                        continue;
                                    }

                                    throw new ArgumentException(Res.Get(Res.XmlDeserializeReadOnlyProperty, objRealType, property.Name, itemType));
                                }
                            }

                            // 1.b.) read-write property or property value is to be cached
                            object result;

                            // 1.b.i.) Using explicitly defined type converter if can convert from string
                            object[] attrs = property.GetCustomAttributes(typeof(TypeConverterAttribute), true);
                            TypeConverterAttribute convAttr = attrs.Length > 0 ? attrs[0] as TypeConverterAttribute : null;
                            if (convAttr != null)
                            {
                                Type convType = Type.GetType(convAttr.ConverterTypeName);
                                if (convType != null)
                                {
                                    ConstructorInfo ctor = convType.GetConstructor(new Type[] { Reflector.Type });
                                    object[] ctorParams = { property.PropertyType };
                                    if (ctor == null)
                                    {
                                        ctor = convType.GetDefaultConstructor();
                                        ctorParams = Reflector.EmptyObjects;
                                    }
                                    if (ctor != null)
                                    {
                                        if (Reflector.Construct(ctor, ctorParams) is TypeConverter converter && converter.CanConvertFrom(Reflector.StringType))
                                        {
                                            result = converter.ConvertFromInvariantString(ReadStringValue(reader));
                                            if (properties != null)
                                                properties[property] = result;
                                            else
                                                Reflector.SetProperty(obj, property, result);
                                            continue;
                                        }
                                    }
                                }
                            }

                            // 1.b.ii.) Any object
                            if (!TryDeserializeObject(itemType, reader, out result))
                                throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objRealType));

                            if (properties != null)
                                properties[property] = result;
                            else
                                Reflector.SetProperty(obj, property, result);
                            continue;
                        }

                        if (collectionElementType == null)
                        {
                            if (reader.Name == "item")
                                throw new SerializationException(Res.Get(Res.XmlNotACollection, objRealType));
                            throw new ReflectionException(Res.Get(Res.XmlHasNoProperty, objRealType, reader.Name));
                        }

                        // 2.) collection element
                        if (reader.Name != "item")
                            throw new ArgumentException(Res.Get(Res.XmlItemExpected, reader.Name));

                        IEnumerable collection = (IEnumerable)obj;
                        if (reader.IsEmptyElement)
                        {
                            collection.Add(null);
                            continue;
                        }

                        if (TryDeserializeObject(itemType ?? collectionElementType, reader, out var item))
                        {
                            collection.Add(item);
                            continue;
                        }

                        if (itemType == null)
                            throw new ArgumentException(Res.Get(Res.XmlCannotDetermineElementType, objRealType));
                        throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, itemType));

                    case XmlNodeType.EndElement:
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
            // null value
            if (reader.IsEmptyElement && (type == null || !type.IsValueType || type.IsNullable()))
            {
                result = null;
                return true;
            }

            if (type != null && type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            // a.) If type can be natively parsed, parsing from string
            if (type != null && Reflector.CanParseNatively(type))
            {
                string value = ReadStringValue(reader);
                result = Reflector.Parse(type, value);
                return true;
            }

            // b.) Deserialize IXmlSerializable
            string attrFormat = reader["format"];
            if (type != null && attrFormat != null && attrFormat == "custom")
            {
                if (!(Reflector.Construct(type) is IXmlSerializable xmlSerializable))
                    throw new ArgumentException(Res.Get(Res.NotAnIXmlSerializable, type));
                DeserializeXmlSerializable(xmlSerializable, reader);
                result = xmlSerializable;
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
            if (type == typeof(DictionaryEntry) || type?.IsGenericTypeOf(typeof(KeyValuePair<,>)) == true)
            {
                bool keyRead = false;
                bool valueRead = false;
                object key = null;
                object value = null;
                Type keyType = null, valueType = null;

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
                                    if (attrType != null)
                                        keyType = Reflector.ResolveType(attrType);
                                    else
                                    {
                                        keyType = typeof(object);
                                        if (type.IsGenericTypeOf(typeof(KeyValuePair<,>)))
                                            keyType = type.GetGenericArguments()[0];
                                    }
                                    if (!TryDeserializeObject(keyType, reader, out key))
                                    {
                                        if (attrType != null && keyType == null)
                                            throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType));
                                        throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, keyType));
                                    }
                                    break;

                                case "Value":
                                    if (valueRead)
                                        throw new ArgumentException(Res.Get(Res.XmlMultipleValues));

                                    valueRead = true;
                                    attrType = reader["type"];
                                    if (attrType != null)
                                        valueType = Reflector.ResolveType(attrType);
                                    else
                                    {
                                        valueType = Reflector.ObjectType;
                                        if (type.IsGenericTypeOf(typeof(KeyValuePair<,>)))
                                            valueType = type.GetGenericArguments()[1];
                                    }
                                    if (!TryDeserializeObject(valueType, reader, out value))
                                    {
                                        if (attrType != null && valueType == null)
                                            throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType));
                                        throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, valueType));
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

                            var ctor = type.GetConstructor(new[] { keyType, valueType });
                            result = Reflector.Construct(ctor, key, value);
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

            // g.) recursive deserialization (including collections)
            if (type != null && !reader.IsEmptyElement)
            {
                if (type.IsArray)
                {
                    result = DeserializeArray(null, type.GetElementType(), reader);
                    return true;
                }

                bool isCollection = type.IsSupportedCollectionForReflection(out var defaultCtor, out var collectionCtor, out var elementType, out bool isDictionary);

                // deserialize by collectionCtor only if there is no default one
                if (isCollection && defaultCtor == null && !type.IsValueType)
                {
                    result = DeserializeContentByInitializerCollection(type, reader, collectionCtor, elementType, isDictionary);
                    return true;
                }

                result = Reflector.Construct(type);

                // populating will not work: try to fallback to collectionCtor if possible
                if (isCollection && !type.IsReadWriteCollection(result))
                {
                    if (collectionCtor != null)
                    {
                        result = DeserializeContentByInitializerCollection(type, reader, collectionCtor, elementType, isDictionary);
                        return true;
                    }

                    throw new SerializationException(Res.Get(Res.XmlDeserializeReadOnlyCollection, type));
                }

                DeserializeContent(reader, result);
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
        private static Array DeserializeArray(Array array, Type elementType, XmlReader reader)
        {
            if (array == null && elementType == null)
                throw new ArgumentNullException(nameof(elementType), Res.Get(Res.ArgumentNull));

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
                array = lengths != null ? Array.CreateInstance(elementType, lengths, lowerBounds) : Array.CreateInstance(elementType, length);
            else
                CheckArray(array, length, lengths, lowerBounds);

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
                            Type itemType = null;
                            string attrType = reader["type"];
                            if (attrType != null)
                                itemType = Reflector.ResolveType(attrType);
                            if (itemType == null)
                                itemType = array.GetType().GetElementType();

                            if (TryDeserializeObject(itemType, reader, out var item))
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

                            throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, itemType));
                        }

                        throw new ArgumentException(Res.Get(Res.XmlUnexpectedElement, reader.Name));

                    case XmlNodeType.EndElement:
                        if (reader.Name.In("Data", "CRC"))
                            continue;

                        // in end element of parent: checking items count
                        if (deserializedItemsCount != array.Length)
                            throw new ArgumentException(Res.Get(Res.XmlInconsistentArrayLength, array.Length, deserializedItemsCount));

                        return array;
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

            return Unescape(result.ToString());
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
