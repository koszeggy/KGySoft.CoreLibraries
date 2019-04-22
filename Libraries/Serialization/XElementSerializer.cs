#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XElementSerializer.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

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

using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Security.Cryptography;

#endregion

namespace KGySoft.Serialization
{
    internal class XElementSerializer : XmlSerializerBase
    {
        #region Constructors

        public XElementSerializer(XmlSerializationOptions options) : base(options)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

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
            SerializeObject(obj, true, result, objType, DesignerSerializationVisibility.Visible);
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
                throw new ArgumentNullException(nameof(obj), Res.ArgumentNull);
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), Res.ArgumentNull);

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

                    // 2.) Collection
                    if (obj is IEnumerable enumerable)
                    {
                        if (!objType.IsCollection())
                            throw new NotSupportedException(Res.XmlSerializationSerializingNonPopulatableCollectionNotSupported(objType));
                        if (!objType.IsReadWriteCollection(obj))
                            throw new NotSupportedException(Res.XmlSerializationSerializingReadOnlyCollectionNotSupported(objType));

                        SerializeCollection(enumerable, objType.GetCollectionElementType(), false, parent, DesignerSerializationVisibility.Visible);
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

        #endregion

        #region Private Methods

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

                    return;
                }

                // non-primitive type array or compact serialization is not enabled
                foreach (var item in array)
                {
                    XElement child = new XElement(XmlSerializer.ElementItem);
                    Type itemType = null;
                    if (item != null)
                        SerializeObject(item, elementType.CanBeDerived() && (itemType = item.GetType()) != elementType, child, itemType ?? item.GetType(), visibility);
                    parent.Add(child);
                }

                return;
            }

            // non-array collection
            if (typeNeeded)
                parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(collection.GetType())));

            // serializing main properties first
            SerializeMembers(collection, parent);

            // serializing items
            foreach (var item in collection)
            {
                XElement child = new XElement(XmlSerializer.ElementItem);
                Type itemType = null;
                if (item != null)
                    SerializeObject(item, elementType.CanBeDerived() && (itemType = item.GetType()) != elementType, child, itemType ?? item.GetType(), visibility);
                parent.Add(child);
            }
        }

        /// <summary>
        /// Serializes a whole object. May throw exceptions on invalid or inappropriate options.
        /// XElement version.
        /// </summary>
        private void SerializeObject(object obj, bool typeNeeded, XElement parent, Type type, DesignerSerializationVisibility visibility)
        {
            if (obj == null)
                return;

            // a.) If type can be natively parsed, simple adding
            if (type.CanBeParsedNatively() && !(obj is Type && ((Type)obj).IsGenericParameter))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));
                WriteStringValue(obj, parent);
                return;
            }

            // b.) IXmlSerializable
            if (obj is IXmlSerializable xmlSerializable && ProcessXmlSerializable)
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                SerializeXmlSerializable(xmlSerializable, parent);
                return;
            }

            // c.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));
                WriteStringValue(converter.ConvertToInvariantString(obj), parent);
                return;
            }

            // d/1.) KeyValue 1: DictionaryEntry: can be serialized recursively. Just handling to avoid binary serialization.
            if (type == Reflector.DictionaryEntryType)
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                SerializeMembers(obj, parent);
                return;
            }

            // d/2.) KeyValue 2: KeyValuePair: properties are read-only so special support needed
            if (type.IsGenericTypeOf(Reflector.KeyValuePairType))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                object key = Reflector.GetProperty(obj, nameof(KeyValuePair<_, _>.Key));
                object value = Reflector.GetProperty(obj, nameof(KeyValuePair<_, _>.Value));
                XElement xKey = new XElement(nameof(KeyValuePair<_, _>.Key));
                XElement xValue = new XElement(nameof(KeyValuePair<_, _>.Value));
                parent.Add(xKey, xValue);
                if (key != null)
                {
                    Type keyType = key.GetType();
                    SerializeObject(key, keyType != type.GetGenericArguments()[0], xKey, keyType, visibility);
                }

                if (value != null)
                {
                    Type valueType = value.GetType();
                    SerializeObject(value, valueType != type.GetGenericArguments()[1], xValue, valueType, visibility);
                }

                return;
            }

            // e.) value type as binary only if enabled
            if (type.IsValueType && IsCompactSerializationValueTypesEnabled && BinarySerializer.TrySerializeValueType((ValueType)obj, out byte[] data))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                parent.Add(new XAttribute(XmlSerializer.AttributeFormat, XmlSerializer.AttributeValueStructBinary));
                if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                    parent.Add(new XAttribute(XmlSerializer.AttributeCrc, $"{Crc32.CalculateHash(data):X8}"));
                parent.Add(Convert.ToBase64String(data));
                return;
            }

            // f.) binary serialization: base64 format to XML
            if (IsBinarySerializationEnabled && visibility != DesignerSerializationVisibility.Content)
            {
                try
                {
                    SerializeBinary(obj, parent);
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
                // g.) collection
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
                        SerializeCollection(enumerable, elementType ?? type.GetCollectionElementType(), typeNeeded, parent, visibility);
                        return;
                    }

                    if (visibility == DesignerSerializationVisibility.Content || IsRecursiveSerializationEnabled)
                        throw new SerializationException(Res.XmlSerializationCannotSerializeUnsupportedCollection(type, Options));
                    throw new SerializationException(Res.XmlSerializationCannotSerializeCollection(type, Options));
                }

                // h.) recursive serialization, if enabled
                if (IsRecursiveSerializationEnabled || visibility == DesignerSerializationVisibility.Content
                    // or when it has public properties/fields only
                    || IsTrustedType(type))
                {
                    if (typeNeeded)
                        parent.Add(new XAttribute(XmlSerializer.AttributeType, GetTypeString(type)));

                    SerializeMembers(obj, parent);
                    return;
                }
            }
            finally
            {
                UnregisterSerializedObject(obj);
            }

            throw new SerializationException(Res.XmlSerializationSerializingTypeNotSupported(type, Options));
        }

        private void SerializeMembers(object obj, XContainer parent)
        {
            // signing that object is not null
            parent.Add(String.Empty);

            foreach (Member member in GetMembersToSerialize(obj))
            {
                if (SkipMember(obj, member.MemberInfo, out object value, out DesignerSerializationVisibility visibility))
                    continue;

                PropertyInfo property = member.Property;
                FieldInfo field = member.Field;
                Type memberType = property != null ? property.PropertyType : field.FieldType;

                XElement memberElement = new XElement(member.MemberInfo.Name);
                if (member.SpecifyDeclaringType)
                    memberElement.Add(new XAttribute(XmlSerializer.AttributeDeclaringType, GetTypeString(member.MemberInfo.DeclaringType)));
                Type actualType = value?.GetType() ?? memberType;

                // a.) Using explicitly defined type converter if can convert to and from string
                Attribute[] attrs = Attribute.GetCustomAttributes(member.MemberInfo, typeof(TypeConverterAttribute), true);
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
                        if (Reflector.CreateInstance(ctor, ctorParams) is TypeConverter converter && converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
                        {
                            // ReSharper disable once AssignNullToNotNullAttribute - false alarm: it CAN be null
                            WriteStringValue(converter.ConvertToInvariantString(value), memberElement);
                            parent.Add(memberElement);
                            continue;
                        }
                    }
                }

                // b.) any object
                SerializeObject(value, memberType != actualType, memberElement, actualType, visibility);
                parent.Add(memberElement);
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

        #endregion

        #endregion
    }
}
