using System;
using System.Collections;
using System.Collections.Generic;
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
        /// Result can be deserialized by <see cref="XElementDeserializer.Deserialize(XElement)"/> method.</returns>
        /// <exception cref="NotSupportedException">Root object is a read-only collection.</exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.<br/>-or-<br/>
        /// Serialization is not supported with provided options.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        public XElement Serialize(object obj)
        {
            XElement result = new XElement(XmlSerializer.ElementObject);
            if (obj == null)
                return result;

            Type objType = obj.GetType();
            if (!TrySerializeObject(obj, true, result, objType, DesignerSerializationVisibility.Visible))
                throw new SerializationException(Res.Get(Res.XmlCannotSerialize, objType, Options));
            return result;
        }

        /// <summary>
        /// Saves public properties or collection elements of an object given in <paramref name="obj"/> parameter
        /// into an already existing <see cref="XElement"/> object given in <paramref name="parent"/> parameter.
        /// </summary>
        /// <param name="obj">The object, which inner content should be serialized. Parameter value must not be <see langword="null"/>.</param>
        /// <param name="parent">The parent under that the object will be saved. Its content can be deserialized by <see cref="XElementDeserializer.DeserializeContent(XElement,object)"/> method.</param>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> and <paramref name="parent"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Serialization is not supported with provided <see cref="XmlSerializerBase.Options"/></exception>
        /// <exception cref="ReflectionException">The object hierarchy to serialize contains circular reference.</exception>
        /// <exception cref="InvalidOperationException">This method cannot be called parallelly from different threads.</exception>
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize"/> method.
        /// </remarks>
        public void SerializeContent(XElement parent, object obj)
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
                    if (obj is IXmlSerializable xmlSerializable && ProcessXmlSerializable)
                    {
                        SerializeXmlSerializable(xmlSerializable, parent);
                        return;
                    }

                    // 2.) Collection - as content, collectionCtor version cannot be accepted because the empty collection will already be precreated.
                    if (objType.IsSupportedCollectionForReflection(out var _, out var _, out Type elementType, out var _))
                    {
                        // if has only default constructor but the collection is read-only
                        if (!objType.IsCollection())
                            throw new NotSupportedException(Res.Get(Res.XmlSerializeNonPopulatableCollection, obj.GetType()));
                        if (!objType.IsReadWriteCollection(obj))
                            throw new NotSupportedException(Res.Get(Res.XmlSerializeReadOnlyCollection, obj.GetType()));

                        SerializeCollection((IEnumerable)obj, elementType, false, parent, DesignerSerializationVisibility.Visible);
                        return;
                    }

                    // 3.) Any object
                    SerializeMembers(obj, parent);
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
        private void SerializeCollection(IEnumerable collection, Type elementType, bool typeNeeded, XContainer parent, DesignerSerializationVisibility visibility)
        {
            if (collection == null)
                return;

            // signing that collection is not null - now it will be at least <Collection></Collection> instead of <Collection />
            parent.Add(String.Empty);

            // array collection
            if (collection is Array array)
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(collection.GetType())));

                // multidimensional or nonzero-based array
                if (array.Rank > 1 || array.GetLowerBound(0) != 0)
                {
                    StringBuilder dim = new StringBuilder();
                    for (int i = 0; i < array.Rank; i++)
                    {
                        int low;
                        if ((low = array.GetLowerBound(i)) != 0)
                            dim.Append($"{low.ToString(CultureInfo.InvariantCulture)}..{(low + array.GetLength(i) - 1).ToString(CultureInfo.InvariantCulture)}");
                        else
                            dim.Append(array.GetLength(i).ToString(CultureInfo.InvariantCulture));

                        if (i < array.Rank - 1)
                            dim.Append(',');
                    }

                    parent.Add(new XAttribute(XmlSerializer.AttributeDim, dim));
                }
                else
                    parent.Add(new XAttribute(XmlSerializer.AttributeLength, array.Length.ToString(CultureInfo.InvariantCulture)));

                // array of a primitive type
                if (elementType.IsPrimitive && (Options & XmlSerializationOptions.CompactSerializationOfPrimitiveArrays) != XmlSerializationOptions.None)
                {
                    if (array.Length > 0)
                    {
                        byte[] data = new byte[Buffer.ByteLength(array)];
                        Buffer.BlockCopy(array, 0, data, 0, data.Length);
                        parent.Add(Convert.ToBase64String(data));
                        if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                            parent.Add(new XAttribute(XmlSerializer.AttributeCrc, $"{Crc32.CalculateHash(data):X8}"));
                    }
                }
                // non-primitive type array or compact serialization is not enabled
                else
                {
                    bool elementTypeNeeded = elementType.CanBeDerived();
                    foreach (var item in array)
                    {
                        XElement child = new XElement(XmlSerializer.ElementItem);
                        Type itemType = null;
                        if (item == null || TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, child, itemType ?? item.GetType(), visibility))
                            parent.Add(child);
                        else
                            throw new SerializationException(Res.Get(Res.XmlCannotSerializeArrayElement, item.GetType(), Options));
                    }
                }
            }
            // non-array collection
            else
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(collection.GetType())));

                // serializing main properties first
                SerializeMembers(collection, parent);

                // serializing items
                bool elementTypeNeeded = elementType.CanBeDerived();
                foreach (var item in collection)
                {
                    XElement child = new XElement(XmlSerializer.ElementItem);
                    Type itemType = null;
                    if (item == null || TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, child, itemType ?? item.GetType(), visibility))
                        parent.Add(child);
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
        private bool TrySerializeObject(object obj, bool typeNeeded, XElement parent, Type type, DesignerSerializationVisibility visibility, bool isPropertyOrField)
        {
            if (obj == null)
                return true;

            // a.) If type can be natively parsed, simple adding
            if (Reflector.CanParseNatively(type) && !(obj is Type && ((Type)obj).IsGenericParameter))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));
                WriteStringValue(obj, parent);
                return true;
            }

            // b.) IXmlSerializable
            if (obj is IXmlSerializable xmlSerializable && ProcessXmlSerializable)
            {
                if (!isPropertyOrField && !type.CanBeCreatedWithoutParameters())
                    throw new SerializationException(Res.Get(Res.XmlSerializableNoDefaultCtor, type));
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                SerializeXmlSerializable(xmlSerializable, parent);
                return true;
            }

            // c.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));
                WriteStringValue(converter.ConvertToInvariantString(obj), parent);
                return true;
            }

            // d.) simple object
            if (type == Reflector.ObjectType)
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                parent.Add(String.Empty);
                return true;
            }

            // e/1.) Keyvalue 1: DictionaryEntry: can be serialized recursively. Just handling to avoid binary serialization.
            if (type == typeof(DictionaryEntry))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                // SerializeComponent can be avoided because DE is neither IXmlSerializable nor collection and no need to register because it is a value type
                SerializeMembers(obj, parent);
                return true;
            }

            // e/2.) Keyvalue 2: KeyValuePair: properties are read-only so special support needed
            if (type.IsGenericTypeOf(typeof(KeyValuePair<,>)))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                object key = Reflector.GetInstancePropertyByName(obj, nameof(KeyValuePair<_,_>.Key));
                object value = Reflector.GetInstancePropertyByName(obj, nameof(KeyValuePair<_,_>.Value));
                XElement xKey = new XElement(nameof(KeyValuePair<_,_>.Key));
                XElement xValue = new XElement(nameof(KeyValuePair<_,_>.Value));
                parent.Add(xKey, xValue);
                if (key != null)
                {
                    Type keyType = key.GetType();
                    bool elementTypeNeeded = keyType != type.GetGenericArguments()[0];
                    if (!TrySerializeObject(key, elementTypeNeeded, xKey, keyType, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, keyType, Options));
                }
                if (value != null)
                {
                    Type valueType = value.GetType();
                    bool elementTypeNeeded = valueType != type.GetGenericArguments()[1];
                    if (!TrySerializeObject(value, elementTypeNeeded, xValue, valueType, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, valueType, Options));
                }
                return true;
            }

            // f.) value type as binary only if enabled
            if (type.IsValueType && IsCompactSerializationValueTypesEnabled && BinarySerializer.TrySerializeStruct((ValueType)obj, out byte[] data))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                parent.Add(new XAttribute(XmlSerializer.AttributeFormat, XmlSerializer.AttributeValueStructBinary));
                if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                    parent.Add(new XAttribute(XmlSerializer.AttributeCrc, $"{Crc32.CalculateHash(data):X8}"));
                parent.Add(Convert.ToBase64String(data));
                return true;
            }

            // g.) binary serialization: base64 format to XML
            if (IsBinarySerializationEnabled && visibility != DesignerSerializationVisibility.Content)
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

            // h.) collection: if can be trusted in all circumstances
            Type elementType = null;
            if (IsTrustedCollection(type)
                // or recursive is requested 
                || ((visibility == DesignerSerializationVisibility.Content || IsRecursiveSerializationEnabled)
                    // and can populate collection by general ways or by an initializer constructor
                    && ((!isPropertyOrField && type.IsSupportedCollectionForReflection(out var _, out var collectionCtor, out elementType, out var _) && (collectionCtor != null || type.IsReadWriteCollection(obj)))
                        // or if member value, an existing instance can be populated without using any constructor
                        || (isPropertyOrField && type.IsReadWriteCollection(obj)))))
            {
                SerializeCollection((IEnumerable)obj, elementType ?? type.GetCollectionElementType(), true, parent, visibility);
                return true;
            }

            // i.) recursive serialization, if enabled
            if (IsRecursiveSerializationEnabled || visibility == DesignerSerializationVisibility.Content
                // or when it has public properties/fields only
                || IsTrustedType(type))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                SerializeContent(parent, obj);
                return true;
            }

            return false;
        }

        private void SerializeMembers(object obj, XContainer parent)
        {
            // getting read-write non-indexer instance properties, and read-only ones with populatable collections, same for fields
            IEnumerable<MemberInfo> properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0 && p.CanRead && (p.CanWrite || ForceReadonlyMembers || (ProcessXmlSerializable && typeof(IXmlSerializable).IsAssignableFrom(p.PropertyType)) || p.PropertyType.IsCollection()));
            IEnumerable<MemberInfo> fields = ExcludeFields
                ? (IEnumerable<MemberInfo>)new MemberInfo[0]
                : obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsInitOnly || ForceReadonlyMembers || f.FieldType.IsCollection());

            // signing that object is not null
            parent.Add(String.Empty);

            foreach (MemberInfo member in fields.Concat(properties))
            {
                // skipping non-serializable members
                // Skip 1.) hidden by DesignerSerializationVisibility
                Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(DesignerSerializationVisibilityAttribute), true);
                DesignerSerializationVisibility visibility = attrs.Length > 0 ? ((DesignerSerializationVisibilityAttribute)attrs[0]).Visibility : DesignerSerializationVisibilityAttribute.Default.Visibility;
                if (visibility == DesignerSerializationVisibility.Hidden)
                    continue;

                // Skip 2.) ShouldSerialize<MemberName> method returns false
                if ((Options & XmlSerializationOptions.IgnoreShouldSerialize) == XmlSerializationOptions.None)
                {
                    MethodInfo shouldSerializeMethod = member.DeclaringType.GetMethod(XmlSerializer.MethodShouldSerialize + member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (shouldSerializeMethod != null && shouldSerializeMethod.ReturnType == typeof(bool) && !(bool)Reflector.RunMethod(obj, shouldSerializeMethod))
                        continue;
                }

                // Skip 3.) DefaultValue equals to property value
                bool hasDefaultValue = false;
                object defaultValue = null;
                if ((Options & XmlSerializationOptions.IgnoreDefaultValueAttribute) == XmlSerializationOptions.None)
                {
                    attrs = Attribute.GetCustomAttributes(member, typeof(DefaultValueAttribute), true);
                    hasDefaultValue = attrs.Length > 0;
                    if (hasDefaultValue)
                        defaultValue = ((DefaultValueAttribute)attrs[0]).Value;
                }

                PropertyInfo property = member as PropertyInfo;
                FieldInfo field = property == null ? member as FieldInfo : null;
                Type memberType = property != null ? property.PropertyType : field.FieldType;
                if (!hasDefaultValue && (Options & XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback) != XmlSerializationOptions.None)
                {
                    hasDefaultValue = true;
                    defaultValue = memberType.IsValueType ? Reflector.Construct(memberType) : null;
                }

                object value = property != null ? Reflector.GetProperty(obj, property) : Reflector.GetField(obj, field);
                if (hasDefaultValue && Equals(value, defaultValue))
                    continue;

                // -------------- not skipped, serializing
                XElement newElement = new XElement(member.Name);
                Type actualType = value?.GetType() ?? memberType;

                // a.) Using explicitly defined type converter if can convert to and from string
                attrs = Attribute.GetCustomAttributes(member, typeof(TypeConverterAttribute), true);
                if (attrs.Length > 0 && attrs[0] is TypeConverterAttribute convAttr && Reflector.ResolveType(convAttr.ConverterTypeName) is Type convType)
                {
                    ConstructorInfo ctor = convType.GetConstructor(new Type[] { Reflector.Type });
                    object[] ctorParams = { memberType };
                    if (ctor == null)
                    {
                        ctor = convType.GetDefaultConstructor();
                        ctorParams = Reflector.EmptyObjects;
                    }

                    if (ctor != null)
                    {
                        if (Reflector.Construct(ctor, ctorParams) is TypeConverter converter && converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
                        {
                            WriteStringValue(converter.ConvertToInvariantString(value), newElement);
                            parent.Add(newElement);
                            continue;
                        }
                    }
                }

                // b.) any object
                if (value == null || TrySerializeObject(value, memberType != actualType, newElement, actualType, visibility, true))
                    parent.Add(newElement);
                else
                    throw new SerializationException(Res.Get(Res.XmlCannotSerializeProperty, obj.GetType(), property.Name, Options));
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

            parent.Add(new XAttribute(XmlSerializer.AttributeFormat, XmlSerializer.AttributeValueCustom));
        }

        /// <summary>
        /// Serializing binary content by LinqToXml
        /// </summary>
        private void SerializeBinary(object obj, XContainer parent)
        {
            parent.Add(new XAttribute(XmlSerializer.AttributeFormat, XmlSerializer.AttributeValueBinary));
            if (obj == null)
                return;
            BinarySerializationOptions binSerOptions = GetBinarySerializationOptions();
            byte[] data = BinarySerializer.Serialize(obj, binSerOptions);
            if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                parent.Add(new XAttribute(XmlSerializer.AttributeCrc, $"{Crc32.CalculateHash(data):X8}"));
            parent.Add(Convert.ToBase64String(data));
        }

        private void WriteStringValue(object obj, XElement parent)
        {
            string s = GetStringValue(obj, out bool spacePreserved, out bool escaped);
            if (spacePreserved)
                parent.Add(new XAttribute(XNamespace.Xml + XmlSerializer.AttributeSpace, XmlSerializer.AttributeValuePreserve));
            if (escaped)
                parent.Add(new XAttribute(XmlSerializer.AttributeEscaped, XmlSerializer.AttributeValueTrue));

            parent.Add(s);
        }
    }
}
