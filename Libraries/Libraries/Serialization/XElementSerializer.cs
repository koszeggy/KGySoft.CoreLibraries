using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    internal class XElementSerializer : XmlSerializerBase
    {
        public XElementSerializer(XmlSerializationOptions options) : base(options)
        {
        }

        /// <summary>
        /// Serializes the object passed in <paramref name="obj"/> parameter into a new <see cref="XElement"/> object.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>An <see cref="XElement"/> instance that contains the serialized object.
        /// Result can be deserialized by <see cref="Deserialize(XElement)"/> method.</returns>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with provided <paramref name="options"/></exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        public XElement Serialize(object obj)
        {
            XElement result = new XElement("object");
            if (obj == null)
                return result;

            Type objType = obj.GetType();
            if (objType.IsCollection())
            {
                if (!objType.IsReadWriteCollection(obj))
                    throw new NotSupportedException(Res.Get(Res.XmlSerializeReadOnlyRoot, objType));
                SerializeCollection(obj as IEnumerable, true, result, DesignerSerializationVisibility.Visible);
            }
            else if (!TrySerializeObject(obj, true, result, objType, DesignerSerializationVisibility.Visible))
                throw new SerializationException(Res.Get(Res.XmlCannotSerialize, objType, Options));
            return result;
        }

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// into an already existing <see cref="XElement"/> object given in <paramref name="parent"/> parameter.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="parent">The parent under that the object will be saved. Its content can be deserialized by <see cref="DeserializeContent(XElement,object)"/> method.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="parent"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <see cref="XmlSerializerBase.Options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize"/> method.
        /// </remarks>
        public void SerializeContent(object obj, XElement parent)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.Get(Res.ArgumentNull));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), Res.Get(Res.ArgumentNull));

            try
            {
                RegisterSerializedObject(obj);
                Type objType = obj.GetType();

                try
                {
                    // 1.) IXmlSerializable
                    if (obj is IXmlSerializable && ((Options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
                    {
                        SerializeXmlSerializable((IXmlSerializable)obj, parent);
                        return;
                    }

                    // 2.) Collection
                    if (objType.IsCollection())
                    {
                        if (!objType.IsReadWriteCollection(obj))
                            throw new NotSupportedException(Res.Get(Res.XmlSerializeReadOnlyCollection, obj.GetType()));
                        SerializeCollection(obj as IEnumerable, false, parent, DesignerSerializationVisibility.Visible);
                        return;
                    }

                    // 3.) Any object
                    SerializeProperties(obj, parent);
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
        /// Serializing a collection by LinqToXml
        /// </summary>
        private void SerializeCollection(IEnumerable collection, bool typeNeeded, XContainer parent, DesignerSerializationVisibility visibility)
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
                    parent.Add(new XAttribute("type", GetTypeString(collection.GetType())));

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
                if (elementType.IsPrimitive && (Options & XmlSerializationOptions.CompactSerializationOfPrimitiveArrays) != XmlSerializationOptions.None)
                {
                    if (array.Length > 0)
                    {
                        byte[] data = new byte[Buffer.ByteLength(array)];
                        Buffer.BlockCopy(array, 0, data, 0, data.Length);
                        parent.Add(new XAttribute("comp", "base64"));
                        parent.Add(Convert.ToBase64String(data));
                        if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
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
                        if (item == null || TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, child, itemType ?? item.GetType(), visibility))
                        {
                            parent.Add(child);
                        }
                        else
                            throw new SerializationException(Res.Get(Res.XmlCannotSerializeArrayElement, item.GetType(), Options));
                    }
                }
            }
            // non-array collection
            else
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(collection.GetType())));

                // serializing main properties first
                SerializeProperties(collection, parent);

                // determining element type
                Type elementType = collection.GetElementType();
                bool elementTypeNeeded = !(elementType.IsValueType || elementType.IsClass && elementType.IsSealed);

                // serializing items
                foreach (var item in collection)
                {
                    XElement child = new XElement("item");
                    Type itemType = null;
                    if (item == null || TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, child, itemType ?? item.GetType(), visibility))
                    {
                        parent.Add(child);
                    }
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeCollectionElement, item.GetType(), Options));
                }
            }
        }

        /// <summary>
        /// Tries to serialize the whole object itself. Returns false when object type is not supported with current options but may throw exceptions on
        /// invalid data or inconsistent settings.
        /// XElement version.
        /// </summary>
        private bool TrySerializeObject(object obj, bool typeNeeded, XElement parent, Type type, DesignerSerializationVisibility visibility)
        {
            if (obj == null)
                return true;

            // a.) If type can be natively parsed, simple adding
            if (Reflector.CanParseNatively(type) && !(obj is Type && ((Type)obj).IsGenericParameter))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type)));
                WriteStringValue(obj, parent);
                return true;
            }

            // b.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter != null && converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type)));
                WriteStringValue(converter.ConvertTo(null, CultureInfo.InvariantCulture, obj, Reflector.StringType), parent);
                return true;
            }

            // c.) IXmlSerializable
            if (obj is IXmlSerializable && ((Options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type)));

                SerializeXmlSerializable((IXmlSerializable)obj, parent);
                return true;
            }

            // d.) simple object
            if (obj.GetType() == typeof(object))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type)));

                parent.Add(String.Empty);
                return true;
            }

            // e/1.) Keyvalue 1: DictionaryEntry: can be serialized recursively
            if (type == typeof(DictionaryEntry))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type)));

                // SerializeComponent can be avoided because DE is neither IXmlSerializable nor collection and no need to register because it is a value type
                SerializeProperties(obj, parent);
                return true;
            }

            // e/2.) Keyvalue 2: KeyValuePair: properties are read-only so special support needed
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type)));

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
                    if (!TrySerializeObject(key, elementTypeNeeded, xKey, elementType, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, elementType, Options));
                }
                if (value != null)
                {
                    Type elementType = value.GetType();
                    bool elementTypeNeeded = elementType != type.GetGenericArguments()[1];
                    if (!TrySerializeObject(value, elementTypeNeeded, xValue, elementType, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, elementType, Options));
                }
                return true;
            }

            // f.) value type as binary only if enabled
            if (type.IsValueType && ((Options.IsForcedSerializationValueTypesEnabled() && !Options.IsBinarySerializationEnabled()) || Options.IsCompactSerializationValueTypesEnabled()))
            {
                byte[] data;
                if (BinarySerializer.TrySerializeStruct((ValueType)obj, out data))
                {
                    if (typeNeeded)
                        parent.Add(new XAttribute("type", GetTypeString(type)));

                    parent.Add(new XAttribute("format", "structbase64"));
                    if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                        parent.Add(new XAttribute("CRC", Crc32.CalculateHash(data).ToString("X8")));
                    parent.Add(Convert.ToBase64String(data));
                    return true;
                }
                else if (!(visibility == DesignerSerializationVisibility.Content || Options.IsRecursiveSerializationEnabled() || Options.IsBinarySerializationEnabled()))
                {
                    if (Options.IsForcedSerializationValueTypesEnabled())
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeValueType, obj.GetType(), Options));
                }
            }

            // g.) if type of value is serializable and option is enabled, then adding binary serialized hexa content to xml
            if (visibility != DesignerSerializationVisibility.Content && Options.IsBinarySerializationEnabled())
            {
                try
                {
                    SerializeBinary(obj, parent);
                    return true;
                }
                catch (Exception e)
                {
                    throw new SerializationException(Res.Get(Res.XmlBinarySerializationFailed, obj.GetType(), Options, e.Message), e);
                }
            }

            // h.) recursive serialization, if enabled
            if (Options.IsRecursiveSerializationEnabled() || visibility == DesignerSerializationVisibility.Content)
            {
                if (typeNeeded)
                    parent.Add(new XAttribute("type", GetTypeString(type)));

                SerializeContent(obj, parent);
                return true;
            }

            return false;
        }

        private void SerializeProperties(object obj, XContainer parent)
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
                if ((Options & XmlSerializationOptions.IgnoreShouldSerialize) == XmlSerializationOptions.None)
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
                if ((Options & XmlSerializationOptions.IgnoreDefaultValueAttribute) == XmlSerializationOptions.None)
                {
                    attrs = property.GetCustomAttributes(typeof(DefaultValueAttribute), true);
                    hasDefaultValue = attrs.Length > 0;
                    if (hasDefaultValue)
                        defaultValue = ((DefaultValueAttribute)attrs[0]).Value;
                }
                if (!hasDefaultValue && (Options & XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback) != XmlSerializationOptions.None)
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
                if (propValue != null && propValue is IXmlSerializable && ((Options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
                {
                    SerializeXmlSerializable((IXmlSerializable)propValue, newElement);
                    parent.Add(newElement);
                }
                // 2.) property is collection
                else if (propValue == null && property.CanWrite && propType.IsCollection() || propType.IsReadWriteCollection(propValue))
                {
                    SerializeCollection(propValue as IEnumerable, propValue != null && propType != property.PropertyType, newElement, visibility);
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
                                if (converter != null && converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
                                {
                                    WriteStringValue(converter.ConvertTo(null, CultureInfo.InvariantCulture, propValue, Reflector.StringType), newElement);
                                    parent.Add(newElement);
                                    continue;
                                }
                            }
                        }
                    }

                    if (propValue == null || TrySerializeObject(propValue, propType != property.PropertyType, newElement, propType, visibility))
                        parent.Add(newElement);
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeProperty, obj.GetType(), property.Name, Options));
                }

                //// adding property type if instance is not the same type
                //if (propValue != null && propType != property.PropertyType)
                //    newElement.Add(new XAttribute("type", GetTypeString(obj, options)));
            }
        }

        private void SerializeXmlSerializable(IXmlSerializable obj, XContainer parent)
        {
            StringBuilder sb = new StringBuilder();
            using (XmlWriter xw = XmlWriter.Create(sb, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
            {
                obj.WriteXml(xw);
                xw.Flush();
            }

            Type objType = obj.GetType();
            string contentName = null;
            object[] attrs = objType.GetCustomAttributes(typeof(XmlRootAttribute), true);
            if (attrs.Length > 0)
                contentName = ((XmlRootAttribute)attrs[0]).ElementName;

            if (String.IsNullOrEmpty(contentName))
                contentName = objType.Name;

            using (XmlReader xr = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment }))
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
        /// Serializing binary content by LinqToXml
        /// </summary>
        private void SerializeBinary(object obj, XContainer parent)
        {
            parent.Add(new XAttribute("format", "base64"));
            if (obj == null)
                return;
            BinarySerializationOptions binSerOptions = Options.ToBinarySerializationOptions();
            byte[] data = BinarySerializer.Serialize(obj, binSerOptions);
            if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                parent.Add(new XAttribute("CRC", Crc32.CalculateHash(data).ToString("X8")));
            parent.Add(Convert.ToBase64String(data));
        }

        private void WriteStringValue(object obj, XElement parent)
        {
            string s = GetStringValue(obj, out bool spacePreserved, out bool escaped);
            if (spacePreserved)
                parent.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));
            if (escaped)
                parent.Add(new XAttribute("escaped", "true"));

            parent.Add(s);
        }
    }
}
