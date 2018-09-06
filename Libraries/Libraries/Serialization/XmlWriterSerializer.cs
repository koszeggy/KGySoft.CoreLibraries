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
    internal class XmlWriterSerializer : XmlSerializerBase
    {
        public XmlWriterSerializer(XmlSerializationOptions options) : base(options)
        {
        }

        public void Serialize(XmlWriter writer, object obj)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer), Res.Get(Res.ArgumentNull));

            writer.WriteStartElement(XmlSerializer.ElementObject);
            if (obj == null)
            {
                writer.WriteEndElement();
                writer.Flush();
                return;
            }

            Type objType = obj.GetType();
            if (!TrySerializeObject(obj, true, writer, objType, DesignerSerializationVisibility.Visible))
                throw new SerializationException(Res.Get(Res.XmlCannotSerialize, objType, Options));
            writer.WriteFullEndElement();
            writer.Flush();
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
                if (obj is IXmlSerializable xmlSerializable && ProcessXmlSerializable)
                {
                    SerializeXmlSerializable(xmlSerializable, writer);
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

                    SerializeCollection((IEnumerable)obj, elementType, false, writer, DesignerSerializationVisibility.Visible);
                    return;
                }

                // 3.) Any object
                SerializeMembers(obj, writer);
            }
            finally
            {
                UnregisterSerializedObject(obj);
            }
        }

        /// <summary>
        /// Serializing a collection by XmlWriter
        /// </summary>
        private void SerializeCollection(IEnumerable collection, Type elementType, bool typeNeeded, XmlWriter writer, DesignerSerializationVisibility visibility)
        {
            if (collection == null)
                return;

            // array collection
            if (collection is Array array)
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(collection.GetType()));

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

                    writer.WriteAttributeString(XmlSerializer.AttributeDim, dim.ToString());
                }
                else
                    writer.WriteAttributeString(XmlSerializer.AttributeLength, array.Length.ToString(CultureInfo.InvariantCulture));

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
                    if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                        writer.WriteAttributeString(XmlSerializer.AttributeCrc, $"{Crc32.CalculateHash(data):X8}");
                    writer.WriteString(Convert.ToBase64String(data));

                    return;
                }

                // non-primitive type array or compact serialization is not enabled
                bool elementTypeNeeded = elementType.CanBeDerived();
                foreach (var item in array)
                {
                    writer.WriteStartElement(XmlSerializer.ElementItem);
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
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(collection.GetType()));

                // serializing main properties first
                SerializeMembers(collection, writer);

                // serializing items
                bool elementTypeNeeded = elementType.CanBeDerived();
                foreach (var item in collection)
                {
                    writer.WriteStartElement(XmlSerializer.ElementItem);
                    Type itemType = null;
                    if (item == null)
                        writer.WriteEndElement();
                    else if (TrySerializeObject(item, elementTypeNeeded && (itemType = item.GetType()) != elementType, writer, itemType ?? item.GetType(), visibility))
                        writer.WriteFullEndElement();
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
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                WriteStringValue(obj, writer);
                return true;
            }

            // b.) IXmlSerializable
            if (obj is IXmlSerializable xmlSerializable && ProcessXmlSerializable)
            {
                if (!type.CanBeCreatedWithoutParameters())
                    throw new SerializationException(Res.Get(Res.XmlSerializableNoDefaultCtor, type));
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                SerializeXmlSerializable(xmlSerializable, writer);
                return true;
            }

            // c.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                // ReSharper disable once AssignNullToNotNullAttribute
                writer.WriteString(converter.ConvertToInvariantString(obj));
                return true;
            }

            // d.) simple object
            if (obj.GetType() == Reflector.ObjectType)
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));
                writer.WriteString(String.Empty);

                return true;
            }

            // e/1.) Keyvalue 1: DictionaryEntry: can be serialized recursively. Just handling to avoid binary serialization.
            if (type == typeof(DictionaryEntry))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                // SerializeComponent can be avoided because DE is neither IXmlSerializable nor collection and no need to register because it is a value type
                SerializeMembers(obj, writer);
                return true;
            }

            // e/2.) Keyvalue 2: KeyValuePair: properties are read-only so special support needed
            if (type.IsGenericTypeOf(typeof(KeyValuePair<,>)))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                object key = Reflector.GetInstancePropertyByName(obj, nameof(KeyValuePair<_,_>.Key));
                object value = Reflector.GetInstancePropertyByName(obj, nameof(KeyValuePair<_,_>.Value));

                writer.WriteStartElement(nameof(KeyValuePair<_,_>.Key));
                if (key == null)
                {
                    writer.WriteEndElement();
                }
                else
                {
                    Type keyType = key.GetType();
                    bool elementTypeNeeded = keyType != type.GetGenericArguments()[0];
                    if (!TrySerializeObject(key, elementTypeNeeded, writer, keyType, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, keyType, Options));

                    writer.WriteFullEndElement();
                }

                writer.WriteStartElement(nameof(KeyValuePair<_,_>.Value));
                if (value == null)
                {
                    writer.WriteEndElement();
                }
                else
                {
                    Type valueType = value.GetType();
                    bool elementTypeNeeded = valueType != type.GetGenericArguments()[1];
                    if (!TrySerializeObject(value, elementTypeNeeded, writer, valueType, visibility))
                        throw new SerializationException(Res.Get(Res.XmlCannotSerialize, valueType, Options));

                    writer.WriteFullEndElement();
                }

                return true;
            }

            // f.) value type as binary only if enabled
            if (type.IsValueType && IsCompactSerializationValueTypesEnabled && BinarySerializer.TrySerializeStruct((ValueType)obj, out byte[] data))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                writer.WriteAttributeString(XmlSerializer.AttributeFormat, XmlSerializer.AttributeValueStructBinary);
                if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                    writer.WriteAttributeString(XmlSerializer.AttributeCrc, $"{Crc32.CalculateHash(data):X8}");
                writer.WriteString(Convert.ToBase64String(data));
                return true;
            }

            // g.) binary serialization: base64 format to XML
            if (IsBinarySerializationEnabled && visibility != DesignerSerializationVisibility.Content)
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

            // h.) collection: if can be trusted in all circumstances
            Type elementType = null;
            if (IsTrustedCollection(type)
                // or recursive is requested 
                || ((visibility == DesignerSerializationVisibility.Content || IsRecursiveSerializationEnabled)
                    // and can populate collection by general ways or by an initializer constructor
                    && type.IsSupportedCollectionForReflection(out var _, out var collectionCtor, out elementType, out var _)
                    && (collectionCtor != null || type.IsReadWriteCollection(obj))))
            {
                SerializeCollection((IEnumerable)obj, elementType ?? type.GetCollectionElementType(), true, writer, visibility);
                return true;
            }

            // i.) recursive serialization of any object, if requested
            bool hasDefaultCtor = type.CanBeCreatedWithoutParameters();
            if (IsRecursiveSerializationEnabled || visibility == DesignerSerializationVisibility.Content
                // or when it has public properties/fields only
                || IsTrustedType(type))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                SerializeContent(writer, obj);
                return true;
            }

            return false;
        }

        private void SerializeMembers(object obj, XmlWriter writer)
        {
            // getting read-write non-indexer instance properties, and read-only ones with populatable collections, same for fields
            IEnumerable<MemberInfo> properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0 && p.CanRead && (p.CanWrite || ForceReadonlyMembers || p.PropertyType.IsCollection()));
            IEnumerable<MemberInfo> fields = ExcludeFields
                ? (IEnumerable<MemberInfo>)new MemberInfo[0]
                : obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsInitOnly || ForceReadonlyMembers || f.FieldType.IsCollection());

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
                Type actualType = value?.GetType() ?? memberType;

                // a.) Read-only property
                if (property?.CanWrite == false)
                {
                    // populatable collection
                    if (actualType.IsReadWriteCollection(value))
                    {
                        writer.WriteStartElement(property.Name);
                        SerializeCollection((IEnumerable)value, actualType.GetCollectionElementType(), memberType != property.PropertyType, writer, visibility);
                        writer.WriteFullEndElement();
                    }
                    // XmlSerializable
                    else if (ProcessXmlSerializable && value is IXmlSerializable xmlSerializable)
                    {
                        writer.WriteStartElement(property.Name);
                        SerializeXmlSerializable(xmlSerializable, writer);
                        writer.WriteFullEndElement();
                    }

                    continue;
                }

                // b.) Using explicitly defined type converter if can convert to and from string
                attrs = Attribute.GetCustomAttributes(member, typeof(TypeConverterAttribute), true);
                if (attrs.Length > 0 && attrs[0] is TypeConverterAttribute convAttr && Reflector.ResolveType(convAttr.ConverterTypeName) is Type convType)
                {
                    ConstructorInfo ctor = convType.GetConstructor(new Type[] { typeof(Type) });
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
                            writer.WriteStartElement(property.Name);
                            WriteStringValue(converter.ConvertToInvariantString(value), writer);
                            writer.WriteEndElement();
                            continue;
                        }
                    }
                }

                // c.) any object
                writer.WriteStartElement(property.Name);
                if (value == null)
                    writer.WriteEndElement();
                else if (TrySerializeObject(value, memberType != actualType, writer, actualType, visibility))
                    writer.WriteFullEndElement();
                else
                    throw new SerializationException(Res.Get(Res.XmlCannotSerializeProperty, obj.GetType(), property.Name, Options));
            }
        }

        /// <summary>
        /// Writer must be in parent element, which should be closed by the parent.
        /// </summary>
        private void SerializeXmlSerializable(IXmlSerializable obj, XmlWriter writer)
        {
            writer.WriteAttributeString(XmlSerializer.AttributeFormat, XmlSerializer.AttributeValueCustom);

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
            writer.WriteAttributeString(XmlSerializer.AttributeFormat, XmlSerializer.AttributeValueBinary);

            if (obj == null)
                return;

            BinarySerializationOptions binSerOptions = GetBinarySerializationOptions();
            byte[] data = BinarySerializer.Serialize(obj, binSerOptions);

            if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                writer.WriteAttributeString(XmlSerializer.AttributeCrc, $"{Crc32.CalculateHash(data):X8}");
            writer.WriteString(Convert.ToBase64String(data));
        }

        private void WriteStringValue(object obj, XmlWriter writer)
        {
            string s = GetStringValue(obj, out bool spacePreserved, out bool escaped);
            if (spacePreserved)
                writer.WriteAttributeString(XmlSerializer.NamespaceXml, XmlSerializer.AttributeSpace, null, XmlSerializer.AttributeValuePreserve);
            if (escaped)
                writer.WriteAttributeString(XmlSerializer.AttributeEscaped, XmlSerializer.AttributeValueTrue);

            writer.WriteString(s);
        }
    }
}
