using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    internal class XmlReaderWriterSerializer : XmlSerializerBase
    {
        public XmlReaderWriterSerializer(XmlSerializationOptions options) : base(options)
        {
        }

        public void Serialize(XmlWriter writer, object obj)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer), Res.Get(Res.ArgumentNull));

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
                SerializeCollection(obj as IEnumerable, true, writer, DesignerSerializationVisibility.Visible);
                writer.WriteFullEndElement();
                writer.Flush();
                return;
            }
            else if (TrySerializeObject(obj, true, writer, objType, DesignerSerializationVisibility.Visible))
            {
                writer.WriteFullEndElement();
                writer.Flush();
                return;
            }

            throw new SerializationException(Res.Get(Res.XmlCannotSerialize, objType, Options));
        }

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// by an already opened <see cref="XmlWriter"/> object given in <paramref name="writer"/> parameter.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="writer">A preconfigured <see cref="XmlWriter"/> object that will be used for serialization. The writer must be in proper state to serialize <paramref name="obj"/> properly
        /// and will not be closed after serialization.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="writer"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <see cref="XmlSerializerBase.Options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize"/> method.
        /// </remarks>
        public void SerializeContent(XmlWriter writer, object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.Get(Res.ArgumentNull));
            if (writer == null)
                throw new ArgumentNullException(nameof(writer), Res.Get(Res.ArgumentNull));
            Type objType = obj.GetType();
            try
            {
                RegisterSerializedObject(obj);

                // 1.) IXmlSerializable
                if (obj is IXmlSerializable && ((Options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
                {
                    SerializeXmlSerializable((IXmlSerializable)obj, writer);
                    return;
                }

                // 2.) Collection
                if (objType.IsCollection())
                {
                    if (!objType.IsReadWriteCollection(obj))
                        throw new NotSupportedException(Res.Get(Res.XmlSerializeReadOnlyCollection, obj.GetType()));
                    SerializeCollection(obj as IEnumerable, false, writer, DesignerSerializationVisibility.Visible);
                    return;
                }

                // 3.) Any object
                SerializeProperties(obj, writer);
            }
            finally
            {
                UnregisterSerializedObject(obj);
            }
        }

        /// <summary>
        /// Serializing a collection by XmlWriter
        /// </summary>
        private void SerializeCollection(IEnumerable collection, bool typeNeeded, XmlWriter writer, DesignerSerializationVisibility visibility)
        {
            if (collection == null)
                return;

            // array collection
            if (collection is Array)
            {
                Type elementType = collection.GetType().GetElementType();
                Array array = (Array)collection;

                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(collection.GetType()));

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
                if (elementType.IsPrimitive && (Options & XmlSerializationOptions.CompactSerializationOfPrimitiveArrays) != XmlSerializationOptions.None)
                {
                    byte[] data = new byte[Buffer.ByteLength(array)];
                    Buffer.BlockCopy(array, 0, data, 0, data.Length);
                    writer.WriteAttributeString("comp", "base64");
                    if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
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
                    else if (TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, writer, itemType ?? item.GetType(), visibility))
                    {
                        writer.WriteFullEndElement();
                    }
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeArrayElement, item.GetType(), Options));
                }

                return;
            }
            // non-array collection
            else
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(collection.GetType()));

                // serializing main properties first
                SerializeProperties(collection, writer);

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
                    else if (TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, writer, itemType ?? item.GetType(), visibility))
                    {
                        writer.WriteFullEndElement();
                    }
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeCollectionElement, item.GetType(), Options));
                }
            }
        }

        /// <summary>
        /// Tries to serialize the whole object itself. Returns false when object type is not supported with current options but may throw exceptions on
        /// invalid data or inconsistent settings.
        /// XmlWriter version. Start element must be opened and closed by caller.
        /// obj.GetType and type can be different (properties)
        /// </summary>
        private bool TrySerializeObject(object obj, bool typeNeeded, XmlWriter writer, Type type, DesignerSerializationVisibility visibility)
        {
            if (obj == null)
                return true;

            // a.) If type can be natively parsed, simple writing
            if (Reflector.CanParseNatively(type) && !(obj is Type && ((Type)obj).IsGenericParameter))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type));

                WriteStringValue(obj, writer);
                return true;
            }

            // b.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type));

                writer.WriteString((string)converter.ConvertTo(null, CultureInfo.InvariantCulture, obj, typeof(string)));
                return true;
            }

            // c.) IXmlSerializable
            if (obj is IXmlSerializable && ((Options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type));

                SerializeXmlSerializable((IXmlSerializable)obj, writer);
                return true;
            }

            // d.) simple object
            if (obj.GetType() == typeof(object))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type));
                writer.WriteString(String.Empty);

                return true;
            }

            // e/1.) Keyvalue 1: DictionaryEntry: can be serialized recursively
            if (type == typeof(DictionaryEntry))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type));

                // SerializeComponent can be avoided because DE is neither IXmlSerializable nor collection and no need to register because it is a value type
                SerializeProperties(obj, writer);
                return true;
            }

            // e/2.) Keyvalue 2: KeyValuePair: properties are read-only so special support needed
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                if (typeNeeded)
                    writer.WriteAttributeString("type", GetTypeString(type));

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
                    if (!TrySerializeObject(key, elementTypeNeeded, writer, elementType, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, elementType, Options));

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
                    if (!TrySerializeObject(value, elementTypeNeeded, writer, elementType, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, elementType, Options));

                    writer.WriteFullEndElement();
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
                        writer.WriteAttributeString("type", GetTypeString(type));

                    writer.WriteAttributeString("format", "structbase64");
                    if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                        writer.WriteAttributeString("CRC", Crc32.CalculateHash(data).ToString("X8"));
                    writer.WriteString(Convert.ToBase64String(data));
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
                    SerializeBinary(obj, writer);
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
                    writer.WriteAttributeString("type", GetTypeString(type));

                SerializeContent(writer, obj);
                return true;
            }

            return false;
        }

        private void SerializeProperties(object obj, XmlWriter writer)
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

                // Skip 4.) DefaultValue equals to property value
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
                //XElement newElement = new XElement(property.Name);
                Type propType = propValue == null ? property.PropertyType : propValue.GetType();

                // a.) property is IXmlSerializable standard XmlSerialization is not disabled
                if (propValue != null && propValue is IXmlSerializable && ((Options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None))
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
                    SerializeCollection(propValue as IEnumerable, propValue != null && propType != property.PropertyType, writer, visibility);
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
                                if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(Reflector.StringType))
                                {
                                    writer.WriteStartElement(property.Name);
                                    WriteStringValue(converter.ConvertTo(null, CultureInfo.InvariantCulture, propValue, Reflector.StringType), writer);
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
                    else if (TrySerializeObject(propValue, propType != property.PropertyType, writer, propType, visibility))
                    {
                        writer.WriteFullEndElement();
                    }
                    else
                        throw new SerializationException(Res.Get(Res.XmlCannotSerializeProperty, obj.GetType(), property.Name, Options));
                }
            }
        }

        /// <summary>
        /// Writer must be in parent element, which should be closed by the parent.
        /// </summary>
        private void SerializeXmlSerializable(IXmlSerializable obj, XmlWriter writer)
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

        /// <summary>
        /// Serializing binary content by XmlWriter
        /// </summary>
        private void SerializeBinary(object obj, XmlWriter writer)
        {
            writer.WriteAttributeString("format", "base64");

            if (obj == null)
                return;

            BinarySerializationOptions binSerOptions = Options.ToBinarySerializationOptions();
            byte[] data = BinarySerializer.Serialize(obj, binSerOptions);

            if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                writer.WriteAttributeString("CRC", Crc32.CalculateHash(data).ToString("X8"));
            writer.WriteString(Convert.ToBase64String(data));
        }

        private void WriteStringValue(object obj, XmlWriter writer)
        {
            string s = GetStringValue(obj, out bool spacePreserved, out bool escaped);
            if (spacePreserved)
                writer.WriteAttributeString("xml", "space", null, "preserve");
            if (escaped)
                writer.WriteAttributeString("escaped", "true");

            writer.WriteString(s);
        }
    }
}
