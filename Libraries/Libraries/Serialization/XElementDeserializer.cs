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
    internal static class XElementDeserializer
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

            object result;
            if (!TryDeserializeObject(objType, content, out result))
            {
                if (attrType == null)
                    throw new ArgumentException(Res.Get(Res.XmlRootTypeMissing), nameof(content));

                throw new NotSupportedException(Res.Get(Res.XmlDeserializeNotSupported, objType));
            }
            return result;
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
            // collection: clearing it before restoring content and retrieving element type
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
                {
                    type = Reflector.ResolveType(attrType.Value);
                    if (type == null)
                        throw new ReflectionException(Res.Get(Res.XmlCannotResolveType, attrType.Value));
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
                            DeserializeContent(element, collection);
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
                                ConstructorInfo ctor = convType.GetConstructor(new Type[] { Reflector.Type });
                                object[] ctorParams = new object[] { property.PropertyType };
                                if (ctor == null)
                                {
                                    ctor = convType.GetConstructor(Type.EmptyTypes);
                                    ctorParams = Reflector.EmptyObjects;
                                }
                                if (ctor != null)
                                {
                                    TypeConverter converter = Reflector.Construct(ctor, ctorParams) as TypeConverter;
                                    if (converter != null && converter.CanConvertFrom(Reflector.StringType))
                                    {
                                        Reflector.SetProperty(obj, property, converter.ConvertFrom(null, CultureInfo.InvariantCulture, ReadStringValue(element)));
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
                DeserializeContent(element, child);
                result = child;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Array deserialization, XElement version
        /// </summary>
        private static void DeserializeArray(ref Array array, Type elementType, XElement element)
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

            return element.Value.Unescape();
        }
    }
}
