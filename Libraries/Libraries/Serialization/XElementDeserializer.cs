using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    /// <summary>
    /// XElement version of XML deserialization.
    /// Actually a static class with base types - hence marked as abstract. Unlike on serialization no fields are used so no instance is needed.
    /// </summary>
    internal abstract class XElementDeserializer : XmlDeserializerBase
    {
        /// <summary>
        /// Deserializes an XML content to an object.
        /// </summary>
        public static object Deserialize(XElement content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content), Res.Get(Res.ArgumentNull));

            if (content.Name.LocalName != "object")
                throw new ArgumentException(Res.Get(Res.XmlRootExpected, content.Name.LocalName), nameof(content));

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

            if (TryDeserializeObject(objType, content, out var result))
                return result;

            if (attrType == null)
                throw new ArgumentException(Res.Get(Res.XmlRootTypeMissing), nameof(content));
            throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));
        }

        /// <summary>
        /// Deserializes inner content of an object or collection.
        /// </summary>
        public static void DeserializeContent(XElement parent, object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.Get(Res.ArgumentNull));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), Res.Get(Res.ArgumentNull));
            Type objType = obj.GetType();

            // deserialize IXmlSerializable content
            XAttribute attrFormat = parent.Attribute("format");
            if (attrFormat != null && attrFormat.Value == "custom")
            {
                if (!(obj is IXmlSerializable xmlSerializable))
                    throw new ArgumentException(Res.Get(Res.NotAnIXmlSerializable, objType));
                DeserializeXmlSerializable(xmlSerializable, parent);
                return;
            }

            // deserialize array
            if (objType.IsArray)
            {
                Array array = (Array)obj;
                DeserializeArray(array, null, parent);
                return;
            }

            // Populatable collection: clearing it before restoring (root-level DeserializeContent, collection of read-only properties)
            if (objType.IsCollection())
            {
                if (!objType.IsReadWriteCollection(obj))
                    throw new SerializationException(Res.Get(Res.XmlDeserializeReadOnlyCollection, objType));

                IEnumerable collection = (IEnumerable)obj;
                collection.Clear();
            }

            DeserializePropertiesAndElements(parent, obj, objType, null);
        }

        private static object DeserializeContentByInitializerCollection(Type collectionRealType, XElement parent, ConstructorInfo collectionCtor, Type collectionElementType, bool isDictionary)
        {
            object initializerCollection = isDictionary
                ? (collectionElementType.IsGenericType ? Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(collectionElementType.GetGenericArguments())) : new Dictionary<object, object>())
                : Activator.CreateInstance(typeof(List<>).MakeGenericType(collectionElementType));

            var properties = new Dictionary<PropertyInfo, object>();
            DeserializePropertiesAndElements(parent, initializerCollection, collectionRealType, properties);
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
                            if (GetElementType(arrayToSet).IsPrimitive)
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
        /// Deserializes the properties and elements of <paramref name="objType"/>. Type of <paramref name="obj"/> can be different if
        /// a proxy collection object is populated for initialization. In this case properties have to be stored for later initialization into <paramref name="properties"/>.
        /// </summary>
        private static void DeserializePropertiesAndElements(XElement parent, object obj, Type objType, Dictionary<PropertyInfo, object> properties)
        {
            bool isCollection = obj.GetType().IsCollection();
            Type collectionElementType = isCollection ? GetElementType((IEnumerable)obj) : null;
            foreach (XElement element in parent.Elements())
            {
                PropertyInfo property = objType.GetProperty(element.Name.LocalName);
                XAttribute attrType = element.Attribute("type");
                Type elementType = null;
                if (attrType != null)
                {
                    elementType = Reflector.ResolveType(attrType.Value);
                    if (elementType == null)
                        throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType.Value));
                }
                if (elementType == null && property != null)
                    elementType = property.PropertyType;

                // 1.) real property
                if (property != null)
                {
                    // 1.a.) read-only property and it must be initialized (not a cached property for late initialization)
                    if (properties == null && !property.CanWrite)
                    {
                        object propertyValue = Reflector.GetProperty(obj, property);
                        if (propertyValue != null && element.IsEmpty)
                        {
                            if (elementType.IsArray)
                                throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetterNull, elementType, objType, property.Name));
                            if (elementType.IsCollection())
                                throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetterNull, elementType, objType, property.Name));
                        }
                        else if (propertyValue == null && !element.IsEmpty)
                        {
                            if (elementType.IsArray)
                                throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetter, elementType, objType, property.Name));
                            if (elementType.IsCollection())
                                throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetter, elementType, objType, property.Name));
                        }
                        else 
                        {
                            if (propertyValue == null && element.IsEmpty)
                                continue;
                            if (elementType != propertyValue.GetType())
                                throw new ArgumentException(Res.Get(Res.XmlPropertyTypeMismatch, objType, property.Name, elementType, propertyValue.GetType()));

                            if (elementType.IsArray)
                            {
                                DeserializeArray((Array)propertyValue, null, element);
                                continue;
                            }

                            if (elementType.IsCollection())
                            {
                                // both read-write check and Clear are in DeserializeContent
                                DeserializeContent(element, propertyValue);
                                continue;
                            }

                            throw new ArgumentException(Res.Get(Res.XmlDeserializeReadOnlyProperty, objType, property.Name, elementType));
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
                                    result = converter.ConvertFromInvariantString(ReadStringValue(element));
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
                    if (!TryDeserializeObject(elementType, element, out result))
                        throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));

                    if (properties != null)
                        properties[property] = result;
                    else
                        Reflector.SetProperty(obj, property, result);
                    continue;
                }

                if (!isCollection)
                {
                    if (element.Name.LocalName == "item")
                        throw new SerializationException(Res.Get(Res.XmlNotACollection, objType));
                    throw new ReflectionException(Res.Get(Res.XmlHasNoProperty, objType, element.Name.LocalName));
                }

                // 2.) collection element
                if (element.Name.LocalName != "item")
                    throw new ArgumentException(Res.Get(Res.XmlItemExpected, element.Name.LocalName));

                IEnumerable collection = (IEnumerable)obj;
                if (element.IsEmpty)
                {
                    collection.Add(null);
                    continue;
                }

                if (TryDeserializeObject(elementType ?? collectionElementType, element, out var item))
                {
                    collection.Add(item);
                    continue;
                }

                if (elementType == null)
                    throw new ArgumentException(Res.Get(Res.XmlCannotDetermineElementType, objType));
                throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, elementType));
            }
        }

        /// <summary>
        /// Deserialize object - XElement version
        /// </summary>
        private static bool TryDeserializeObject(Type type, XElement element, out object result)
        {
            // null value
            if (element.IsEmpty && (type == null || !type.IsValueType || type.IsNullable()))
            {
                result = null;
                return true;
            }

            if (type != null && type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            // a.) If type can be natively parsed, parsing from string
            if (type != null && Reflector.CanParseNatively(type))
            {
                string value = ReadStringValue(element);
                result = Reflector.Parse(type, value);
                return true;
            }

            // b.) Deserialize IXmlSerializable
            XAttribute attrFormat = element.Attribute("format");
            if (type != null && attrFormat != null && attrFormat.Value == "custom")
            {
                if (!(Reflector.Construct(type) is IXmlSerializable xmlSerializable))
                    throw new ArgumentException(Res.Get(Res.NotAnIXmlSerializable, type));
                DeserializeXmlSerializable(xmlSerializable, element);
                result = xmlSerializable;
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

            // d.) key/value pair
            if (type == typeof(DictionaryEntry) || type?.IsGenericTypeOf(typeof(KeyValuePair<,>)) == true)
            {
                object key;
                object value;

                // key
                XElement xItem = element.Element("Key");
                if (xItem == null)
                    throw new ArgumentException(Res.Get(Res.XmlKeyValueMissingKey));
                XAttribute xType = xItem.Attribute("type");
                Type keyType, valueType;
                if (xType != null)
                    keyType = Reflector.ResolveType(xType.Value);
                else
                {
                    keyType = Reflector.ObjectType;
                    if (type.IsGenericTypeOf(typeof(KeyValuePair<,>)))
                        keyType = type.GetGenericArguments()[0];
                }
                if (!TryDeserializeObject(keyType, xItem, out key))
                {
                    if (xType != null && keyType == null)
                        throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, xType.Value));
                    throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, keyType));
                }

                // value
                xItem = element.Element("Value");
                if (xItem == null)
                    throw new ArgumentException(Res.Get(Res.XmlKeyValueMissingValue));
                xType = xItem.Attribute("type");
                if (xType != null)
                    valueType = Reflector.ResolveType(xType.Value);
                else
                {
                    valueType = Reflector.ObjectType;
                    if (type.IsGenericTypeOf(typeof(KeyValuePair<,>)))
                        valueType = type.GetGenericArguments()[1];
                }
                if (!TryDeserializeObject(valueType, xItem, out value))
                {
                    if (xType != null && valueType == null)
                        throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, xType.Value));
                    throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, valueType));
                }

                var ctor = type.GetConstructor(new[] { keyType, valueType });
                result = Reflector.Construct(ctor, key, value);
                return true;
            }

            // e.) ValueType as binary
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

            // f.) Binary
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

            // g.) recursive deserialization (including collections)
            if (type != null && !element.IsEmpty)
            {
                if (type.IsArray)
                {
                    result = DeserializeArray(null, type.GetElementType(), element);
                    return true;
                }

                bool isCollection = type.IsSupportedCollectionForReflection(out var defaultCtor, out var collectionCtor, out var elementType, out bool isDictionary);

                // deserialize by collectionCtor only if there is no default one
                if (isCollection && defaultCtor == null && !type.IsValueType)
                {
                    result = DeserializeContentByInitializerCollection(type, element, collectionCtor, elementType, isDictionary);
                    return true;
                }

                result = Reflector.Construct(type);

                // populating will not work: try to fallback to collectionCtor if possible
                if (isCollection && !type.IsReadWriteCollection(result))
                {
                    if (collectionCtor != null)
                    {
                        result = DeserializeContentByInitializerCollection(type, element, collectionCtor, elementType, isDictionary);
                        return true;
                    }

                    throw new SerializationException(Res.Get(Res.XmlDeserializeReadOnlyCollection, type));
                }

                DeserializeContent(element, result);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Array deserialization, XElement version
        /// </summary>
        private static Array DeserializeArray(Array array, Type elementType, XElement element)
        {
            if (array == null && elementType == null)
                throw new ArgumentNullException(nameof(elementType), Res.Get(Res.ArgumentNull));

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
                array = lengths != null ? Array.CreateInstance(elementType, lengths, lowerBounds) : Array.CreateInstance(elementType, length);
            else
                CheckArray(array, length, lengths, lowerBounds);

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
                return array;
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

                if (TryDeserializeObject(itemType, item, out var value))
                    array.SetValue(value, arrayIndexer.Current);
                else
                    throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, itemType));
            }

            return array;
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

        private static string ReadStringValue(XElement element)
        {
            if (element.IsEmpty)
                return null;

            XAttribute attrEscaped = element.Attribute("escaped");
            if (attrEscaped == null || attrEscaped.Value != "true")
                return element.Value;

            return Unescape(element.Value);
        }
    }
}
