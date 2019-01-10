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
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Security.Cryptography;

namespace KGySoft.Serialization
{
    internal class XmlWriterSerializer : XmlSerializerBase
    {
        public XmlWriterSerializer(XmlSerializationOptions options) : base(options)
        {
        }

        public void Serialize(XmlWriter writer, object obj)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer), Res.ArgumentNull);

            writer.WriteStartElement(XmlSerializer.ElementObject);
            if (obj == null)
            {
                writer.WriteEndElement();
                writer.Flush();
                return;
            }

            Type objType = obj.GetType();
            SerializeObject(obj, true, writer, objType, DesignerSerializationVisibility.Visible);
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
                throw new ArgumentNullException(nameof(obj), Res.ArgumentNull);
            if (writer == null)
                throw new ArgumentNullException(nameof(writer), Res.ArgumentNull);
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

                // 2.) Collection
                if (obj is IEnumerable enumerable)
                {
                    if (!objType.IsCollection())
                        throw new NotSupportedException(Res.XmlSerializationSerializingNonPopulatableCollectionNotSupported(objType));
                    if (!objType.IsReadWriteCollection(obj))
                        throw new NotSupportedException(Res.XmlSerializationSerializingReadOnlyCollectionNotSupported(objType));

                    SerializeCollection(enumerable, objType.GetCollectionElementType(), false, writer, DesignerSerializationVisibility.Visible);
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
                            dim.Append(low + ".." + (low + array.GetLength(i) - 1));
                        else
                            dim.Append(array.GetLength(i));

                        if (i < array.Rank - 1)
                            dim.Append(',');
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
                foreach (var item in array)
                {
                    writer.WriteStartElement(XmlSerializer.ElementItem);
                    Type itemType = null;
                    if (item == null)
                        writer.WriteEndElement();
                    else
                    {
                        SerializeObject(item, elementType.CanBeDerived() && (itemType = item.GetType()) != elementType, writer, itemType ?? item.GetType(), visibility);
                        writer.WriteFullEndElement();
                    }
                }

                return;
            }

            // non-array collection
            if (typeNeeded)
                writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(collection.GetType()));

            // serializing main properties first
            SerializeMembers(collection, writer);

            // serializing items
            foreach (var item in collection)
            {
                writer.WriteStartElement(XmlSerializer.ElementItem);
                Type itemType = null;
                if (item == null)
                    writer.WriteEndElement();
                else
                {
                    SerializeObject(item, elementType.CanBeDerived() && (itemType = item.GetType()) != elementType, writer, itemType ?? item.GetType(), visibility);
                    writer.WriteFullEndElement();
                }
            }
        }

        /// <summary>
        /// Serializes a whole object. May throw exceptions on invalid or inappropriate options.
        /// XmlWriter version. Start element must be opened and closed by caller.
        /// obj.GetType and type can be different (properties)
        /// </summary>
        private void SerializeObject(object obj, bool typeNeeded, XmlWriter writer, Type type, DesignerSerializationVisibility visibility)
        {
            if (obj == null)
                return;

            // a.) If type can be natively parsed, simple writing
            if (type.CanBeParsedNatively() && !(obj is Type && ((Type)obj).IsGenericParameter))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                WriteStringValue(obj, writer);
                return;
            }

            // b.) IXmlSerializable
            if (obj is IXmlSerializable xmlSerializable && ProcessXmlSerializable)
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                SerializeXmlSerializable(xmlSerializable, writer);
                return;
            }

            // c.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                // ReSharper disable once AssignNullToNotNullAttribute
                writer.WriteString(converter.ConvertToInvariantString(obj));
                return;
            }

            // d/1.) KeyValue 1: DictionaryEntry: can be serialized recursively. Just handling to avoid binary serialization.
            if (type == typeof(DictionaryEntry))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                SerializeMembers(obj, writer);
                return;
            }

            // d/2.) KeyValue 2: KeyValuePair: properties are read-only so special support needed
            if (type.IsGenericTypeOf(typeof(KeyValuePair<,>)))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                object key = Reflector.GetProperty(obj, nameof(KeyValuePair<_,_>.Key));
                object value = Reflector.GetProperty(obj, nameof(KeyValuePair<_,_>.Value));

                writer.WriteStartElement(nameof(KeyValuePair<_,_>.Key));
                if (key == null)
                    writer.WriteEndElement();
                else
                {
                    Type keyType = key.GetType();
                    SerializeObject(key, keyType != type.GetGenericArguments()[0], writer, keyType, visibility);
                    writer.WriteFullEndElement();
                }

                writer.WriteStartElement(nameof(KeyValuePair<_,_>.Value));
                if (value == null)
                    writer.WriteEndElement();
                else
                {
                    Type valueType = value.GetType();
                    SerializeObject(value, valueType != type.GetGenericArguments()[1], writer, valueType, visibility);
                    writer.WriteFullEndElement();
                }

                return;
            }

            // e.) value type as binary only if enabled
            if (type.IsValueType && IsCompactSerializationValueTypesEnabled && BinarySerializer.TrySerializeStruct((ValueType)obj, out byte[] data))
            {
                if (typeNeeded)
                    writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                writer.WriteAttributeString(XmlSerializer.AttributeFormat, XmlSerializer.AttributeValueStructBinary);
                if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                    writer.WriteAttributeString(XmlSerializer.AttributeCrc, $"{Crc32.CalculateHash(data):X8}");
                writer.WriteString(Convert.ToBase64String(data));
                return;
            }

            // f.) binary serialization: base64 format to XML
            if (IsBinarySerializationEnabled && visibility != DesignerSerializationVisibility.Content)
            {
                try
                {
                    SerializeBinary(obj, writer);
                    return;
                }
                catch (Exception e)
                {
                    throw new SerializationException(Res.XmlSerializationBinarySerializationFailed(obj.GetType(), Options, e.Message), e);
                }
            }

            RegisterSerializedObject(obj);
            try
            {
                // g.) collection: if can be trusted in all circumstances
                if (obj is IEnumerable enumerable)
                {
                    Type elementType = null;

                    // if can be trusted in all circumstances
                    if (IsTrustedCollection(type)
                        // or recursive is requested 
                        || ((visibility == DesignerSerializationVisibility.Content || IsRecursiveSerializationEnabled)
                            // and is a supported collection or serialization is forced
                            && (ForceReadonlyMembersAndCollections || type.IsSupportedCollectionForReflection(out var _, out var _, out elementType, out var _))))
                    {
                        SerializeCollection(enumerable, elementType ?? type.GetCollectionElementType(), typeNeeded, writer, visibility);
                        return;
                    }

                    if (visibility == DesignerSerializationVisibility.Content || IsRecursiveSerializationEnabled)
                        throw new SerializationException(Res.XmlSerializationCannotSerializeUnsupportedCollection(type, Options));
                    throw new SerializationException(Res.XmlSerializationCannotSerializeCollection(type, Options));
                }

                // h.) recursive serialization of any object, if requested
                if (IsRecursiveSerializationEnabled || visibility == DesignerSerializationVisibility.Content
                    // or when it has public properties/fields only
                    || IsTrustedType(type))
                {
                    if (typeNeeded)
                        writer.WriteAttributeString(XmlSerializer.AttributeType, GetTypeString(type));

                    SerializeMembers(obj, writer);
                    return;
                }
            }
            finally
            {
                UnregisterSerializedObject(obj);
            }

            throw new SerializationException(Res.XmlSerializationSerializingTypeNotSupported(type, Options));
        }

        private void SerializeMembers(object obj, XmlWriter writer)
        {
            foreach (Member member in GetMembersToSerialize(obj))
            {
                if (SkipMember(obj, member.MemberInfo, out object value, out DesignerSerializationVisibility visibility))
                    continue;

                PropertyInfo property = member.Property;
                FieldInfo field = member.Field;
                Type memberType = property != null ? property.PropertyType : field.FieldType;

                Type actualType = value?.GetType() ?? memberType;

                // a.) Using explicitly defined type converter if can convert to and from string
                Attribute[] attrs = Attribute.GetCustomAttributes(member.MemberInfo, typeof(TypeConverterAttribute), true);
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
                        if (Reflector.CreateInstance(ctor, ctorParams) is TypeConverter converter && converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
                        {
                            writer.WriteStartElement(member.MemberInfo.Name);
                            if (member.SpecifyDeclaringType)
                                writer.WriteAttributeString(XmlSerializer.AttributeDeclaringType, GetTypeString(member.MemberInfo.DeclaringType));

                            // ReSharper disable once AssignNullToNotNullAttribute - false alarm: it CAN be null
                            WriteStringValue(converter.ConvertToInvariantString(value), writer);
                            writer.WriteEndElement();
                            continue;
                        }
                    }
                }

                // b.) any object
                writer.WriteStartElement(member.MemberInfo.Name);
                if (member.SpecifyDeclaringType)
                    writer.WriteAttributeString(XmlSerializer.AttributeDeclaringType, GetTypeString(member.MemberInfo.DeclaringType));

                if (value == null)
                    writer.WriteEndElement();
                else
                {
                    SerializeObject(value, memberType != actualType, writer, actualType, visibility);
                    writer.WriteFullEndElement();
                }
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
