#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XElementDeserializer.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Security.Cryptography;
using KGySoft.Serialization.Binary;

#endregion

namespace KGySoft.Serialization.Xml
{
    /// <summary>
    /// XElement version of XML deserialization.
    /// </summary>
    internal class XElementDeserializer : XmlDeserializerBase
    {
        #region TryDeserializeObjectContext Struct

        private struct TryDeserializeObjectContext
        {
            #region Fields

            internal Type? Type;
            internal XElement Element;
            internal object? ExistingInstance;
            internal object? Result;

            #endregion
        }

        #endregion

        #region Constructors

        internal XElementDeserializer(bool safeMode) : base(safeMode)
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        private static void DeserializeXmlSerializable(IXmlSerializable xmlSerializable, XContainer parent)
        {
            XElement? content = parent.Elements().FirstOrDefault();
            if (content == null)
                Throw.ArgumentException(Res.XmlSerializationNoContent(xmlSerializable.GetType()));
            using (XmlReader xr = XmlReader.Create(new StringReader(content.ToString()), new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreWhitespace = true,
                CloseInput = true
            }))
            {
                xr.Read();

                // passing the reader to the object to read itself
                xmlSerializable.ReadXml(xr);
            }
        }

        private static string? ReadStringValue(XElement element)
        {
            if (element.IsEmpty)
                return null;

            XAttribute? attrEscaped = element.Attribute(XmlSerializer.AttributeEscaped!);
            return attrEscaped == null || attrEscaped.Value != XmlSerializer.AttributeValueTrue
                ? element.Value
                : Unescape(element.Value);
        }

        #endregion

        #region Instance Methods

        #region Internal Methods

        /// <summary>
        /// Deserializes an XML content to an object.
        /// </summary>
        internal object? Deserialize(XElement content)
        {
            if (content == null!)
                Throw.ArgumentNullException(Argument.content);

            if (content.Name.LocalName != XmlSerializer.ElementObject)
                Throw.ArgumentException(Argument.content, Res.XmlSerializationRootObjectExpected(content.Name.LocalName));

            if (content.IsEmpty)
                return null;

            XAttribute? attrType = content.Attribute(XmlSerializer.AttributeType!);
            Type? objType = null;
            if (attrType != null)
                objType = ResolveType(attrType.Value);

            if (TryDeserializeObject(objType, content, null, out var result))
                return result;

            if (attrType == null)
                Throw.ArgumentException(Argument.content, Res.XmlSerializationRootTypeMissing);
            return Throw.NotSupportedException<object>(Res.XmlSerializationDeserializingTypeNotSupported(objType!));
        }

        /// <summary>
        /// Deserializes inner content of an object or collection.
        /// </summary>
        internal void DeserializeContent(XElement parent, object obj)
        {
            if (obj == null!)
                Throw.ArgumentNullException(Argument.obj);
            if (parent == null!)
                Throw.ArgumentNullException(Argument.parent);
            Type objType = obj.GetType();

            // deserialize IXmlSerializable content
            XAttribute? attrFormat = parent.Attribute(XmlSerializer.AttributeFormat!);
            if (attrFormat != null && attrFormat.Value == XmlSerializer.AttributeValueCustom)
            {
                if (obj is not IXmlSerializable xmlSerializable)
                {
                    Throw.ArgumentException(Argument.objType, Res.XmlSerializationNotAnIXmlSerializable(objType));
                    return;
                }

                DeserializeXmlSerializable(xmlSerializable, parent);
                return;
            }

            // deserialize array
            if (objType.IsArray)
            {
                Array array = (Array)obj;
                DeserializeArray(array, null, parent, false);
                return;
            }

            // Populatable collection: clearing it before restoring (root-level DeserializeContent, collection of read-only properties)
            Type? collectionElementType = null;
            if (objType.IsCollection())
            {
                if (!objType.IsReadWriteCollection(obj))
                    Throw.SerializationException(Res.XmlSerializationCannotDeserializeReadOnlyCollection(objType));

                collectionElementType = objType.GetCollectionElementType();
                IEnumerable collection = (IEnumerable)obj;
                collection.TryClear(false);
            }

            DeserializeMembersAndElements(parent, obj, objType, collectionElementType, null);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Deserializes a non-populatable collection by an initializer collection.
        /// </summary>
        private object DeserializeContentByInitializerCollection(XElement parent, ConstructorInfo collectionCtor, Type collectionElementType, bool isDictionary)
        {
            IEnumerable initializerCollection = collectionElementType.CreateInitializerCollection(isDictionary);
            var members = new Dictionary<MemberInfo, object?>();
            DeserializeMembersAndElements(parent, initializerCollection, collectionCtor.DeclaringType!, collectionElementType, members);
            return CreateCollectionByInitializerCollection(collectionCtor, initializerCollection, members);
        }

        /// <summary>
        /// Deserializes the members and elements of <paramref name="objRealType"/>.
        /// Type of <paramref name="obj"/> can be different of <paramref name="objRealType"/> if a proxy collection object is populated for initialization.
        /// In this case members have to be stored for later initialization into <paramref name="members"/> and <paramref name="obj"/> is a populatable collection for sure.
        /// <paramref name="collectionElementType"/> is <see langword="null"/>&#160;only if <paramref name="objRealType"/> is not a supported collection.
        /// </summary>
        private void DeserializeMembersAndElements(XElement parent, object obj, Type objRealType, Type? collectionElementType, Dictionary<MemberInfo, object?>? members)
        {
            foreach (XElement memberOrItem in parent.Elements())
            {
                string name = memberOrItem.Name.LocalName;
                ResolveMember(objRealType, name, memberOrItem.Attribute(XmlSerializer.AttributeDeclaringType!)?.Value, memberOrItem.Attribute(XmlSerializer.AttributeType!)?.Value,
                    out PropertyInfo? property, out FieldInfo? field, out Type? itemType);
                MemberInfo? member = (MemberInfo?)property ?? field;

                // 1.) real member
                if (member != null)
                {
                    if (SkipMember(member))
                        continue;
                    object? existingValue = members != null ? null
                        : property != null ? property.Get(obj)
                        : field!.Get(obj);
                    if (!TryDeserializeByConverter(member, itemType!, () => ReadStringValue(memberOrItem), out var result) && !TryDeserializeObject(itemType, memberOrItem, existingValue, out result))
                        Throw.NotSupportedException(Res.XmlSerializationDeserializingTypeNotSupported(itemType!));

                    HandleDeserializedMember(obj, member, result, existingValue, members);
                    continue;
                }

                // 2.) collection element
                AssertCollectionItem(objRealType, collectionElementType, name);
                IEnumerable collection = (IEnumerable)obj;
                if (memberOrItem.IsEmpty)
                {
                    collection.TryAdd(null, false);
                    continue;
                }

                if (TryDeserializeObject(itemType ?? collectionElementType, memberOrItem, null, out var item))
                {
                    collection.TryAdd(item, false);
                    continue;
                }

                if (itemType == null)
                    Throw.ArgumentException(Res.XmlSerializationCannotDetermineElementType(objRealType));
                Throw.NotSupportedException(Res.XmlSerializationDeserializingTypeNotSupported(itemType));
            }
        }

        /// <summary>
        /// Deserialize object - XElement version.
        /// If <paramref name="existingInstance"/> is not <see langword="null"/>, then it is preferred to deserialize its content instead of returning a new instance in <paramref name="result"/>.
        /// <paramref name="existingInstance"/> is considered for IXmlSerializable, arrays, collections and recursive objects.
        /// If <paramref name="result"/> is a different instance to <paramref name="existingInstance"/>, then content if existing instance cannot be deserialized.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        private bool TryDeserializeObject(Type? type, XElement element, object? existingInstance, out object? result)
        {
            #region Local Methods to reduce complexity

            bool TryDeserializeKeyValue(ref TryDeserializeObjectContext ctx)
            {
                if (ctx.Type?.IsGenericTypeOf(Reflector.KeyValuePairType) == true)
                {
                    // key
                    XElement? xItem = ctx.Element.Element(nameof(KeyValuePair<_, _>.Key)!);
                    if (xItem == null)
                        Throw.ArgumentException(Res.XmlSerializationKeyValueMissingKey);
                    XAttribute? xType = xItem.Attribute(XmlSerializer.AttributeType!);
                    Type keyType = xType != null ? ResolveType(xType.Value) : ctx.Type.GetGenericArguments()[0];
                    if (!TryDeserializeObject(keyType, xItem, null, out object? key))
                        Throw.NotSupportedException(Res.XmlSerializationDeserializingTypeNotSupported(keyType));

                    // value
                    xItem = ctx.Element.Element(nameof(KeyValuePair<_, _>.Value)!);
                    if (xItem == null)
                        Throw.ArgumentException(Res.XmlSerializationKeyValueMissingValue);
                    xType = xItem.Attribute(XmlSerializer.AttributeType!);
                    Type valueType = xType != null ? ResolveType(xType.Value) : ctx.Type.GetGenericArguments()[1];
                    if (!TryDeserializeObject(valueType, xItem, null, out object? value))
                        Throw.NotSupportedException(Res.XmlSerializationDeserializingTypeNotSupported(valueType));

                    ctx.Result = Activator.CreateInstance(ctx.Type)!;
                    Accessors.SetKeyValue(ctx.Result, key, value);
                    return true;
                }

                return false;
            }

            void DeserializeBinary(ref TryDeserializeObjectContext ctx)
            {
                if (ctx.Element.IsEmpty)
                    return;
                if (SafeMode)
                    Throw.InvalidOperationException(Res.XmlSerializationBinarySerializerSafe);

                byte[] data = Convert.FromBase64String(ctx.Element.Value);
                XAttribute? attrCrc = ctx.Element.Attribute(XmlSerializer.AttributeCrc!);
                if (attrCrc != null)
                {
                    if (Crc32.CalculateHash(data).ToString("X8", CultureInfo.InvariantCulture) != attrCrc.Value)
                        Throw.ArgumentException(Res.XmlSerializationCrcError);
                }

                ctx.Result = BinarySerializer.Deserialize(data);
            }

            bool TryDeserializeComplexObject(ref TryDeserializeObjectContext ctx)
            {
                if (ctx.Type == null || ctx.Element.IsEmpty)
                    return false;

                // 1.) array (both existing and new)
                if (ctx.Type.IsArray)
                {
                    ctx.Result = DeserializeArray((Array)ctx.ExistingInstance!, ctx.Type.GetElementType(), ctx.Element, true);
                    return true;
                }

                // 2.) existing read-write collection
                if (ctx.Type.IsReadWriteCollection(ctx.ExistingInstance))
                {
                    DeserializeContent(ctx.Element, ctx.ExistingInstance!);
                    ctx.Result = ctx.ExistingInstance;
                    return true;
                }

                bool isCollection = ctx.Type.IsSupportedCollectionForReflection(out ConstructorInfo? defaultCtor, out ConstructorInfo? collectionCtor, out Type? elementType, out bool isDictionary);

                // 3.) New collection by collectionCtor (only if there is no defaultCtor)
                if (isCollection && defaultCtor == null && !ctx.Type.IsValueType)
                {
                    ctx.Result = DeserializeContentByInitializerCollection(ctx.Element, collectionCtor!, elementType!, isDictionary);
                    return true;
                }

                ctx.Result = ctx.ExistingInstance ?? (ctx.Type.CanBeCreatedWithoutParameters()
                    ? CreateInstanceAccessor.GetAccessor(ctx.Type).CreateInstance()
                    : Throw.ReflectionException<object>(Res.XmlSerializationNoDefaultCtor(ctx.Type)));

                // 4.) New collection by collectionCtor again (there IS defaultCtor but the new instance is read-only so falling back to collectionCtor)
                if (isCollection && !ctx.Type.IsReadWriteCollection(ctx.Result))
                {
                    if (collectionCtor != null)
                    {
                        ctx.Result = DeserializeContentByInitializerCollection(ctx.Element, collectionCtor, elementType!, isDictionary);
                        return true;
                    }

                    Throw.SerializationException(Res.XmlSerializationCannotDeserializeReadOnlyCollection(ctx.Type));
                }

                // 5.) Newly created collection or any other object (both existing and new)
                DeserializeContent(ctx.Element, ctx.Result!);
                return true;
            }

            #endregion

            // null value
            if (element.IsEmpty && (type == null || !type.IsValueType || type.IsNullable()))
            {
                result = null;
                return true;
            }

            if (type != null && type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            // a.) If type can be natively parsed, parsing from string
            if (type != null && type.CanBeParsedNatively())
            {
                string? value = ReadStringValue(element);
                result = value.Parse(type, SafeMode);
                return true;
            }

            // b.) Deserialize IXmlSerializable
            string? format = element.Attribute(XmlSerializer.AttributeFormat!)?.Value;
            if (type != null && format == XmlSerializer.AttributeValueCustom)
            {
                object instance = existingInstance ?? (type.CanBeCreatedWithoutParameters()
                    ? CreateInstanceAccessor.GetAccessor(type).CreateInstance()
                    : Throw.ReflectionException<object>(Res.XmlSerializationNoDefaultCtor(type)));
                if (instance is not IXmlSerializable xmlSerializable)
                {
                    result = default;
                    Throw.ArgumentException(Res.XmlSerializationNotAnIXmlSerializable(type));
                    return default;
                }

                DeserializeXmlSerializable(xmlSerializable, element);
                result = xmlSerializable;
                return true;
            }

            // c.) Using type converter of the type if applicable
            if (type != null)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(Reflector.StringType))
                {
                    // throwing an exception if the converter cannot handle the possibly null value
                    result = converter.ConvertFromInvariantString(ReadStringValue(element)!);
                    return true;
                }
            }

            var context = new TryDeserializeObjectContext { Type = type, Element = element, ExistingInstance = existingInstance };

            // d.) KeyValuePair (DictionaryEntry is deserialized recursively because its properties are settable)
            if (TryDeserializeKeyValue(ref context))
            {
                result = context.Result;
                return true;
            }

            // e.) ValueType as binary
            if (type != null && format == XmlSerializer.AttributeValueStructBinary && type.IsValueType)
            {
                DeserializeStructBinary(ref context);
                result = context.Result;
                return true;
            }

            // f.) Binary
            if (format == XmlSerializer.AttributeValueBinary)
            {
                DeserializeBinary(ref context);
                result = context.Result;
                return true;
            }

            // g.) recursive deserialization (including collections)
            if (TryDeserializeComplexObject(ref context))
            {
                result = context.Result;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Array deserialization, XElement version
        /// </summary>
        private Array DeserializeArray(Array? array, Type? elementType, XElement element, bool canRecreateArray)
        {
            string? attrLength = element.Attribute(XmlSerializer.AttributeLength!)?.Value;
            string? attrDim = element.Attribute(XmlSerializer.AttributeDim!)?.Value;
            var builder = new ArrayBuilder(array, elementType, attrLength, attrDim, canRecreateArray, SafeMode);

            // has no elements: primitive array (can be restored by BlockCopy)
            if (builder.ElementType.IsPrimitive && !element.HasElements)
            {
                string value = element.Value;
                byte[] data = Convert.FromBase64String(value);
                XAttribute? attrCrc = element.Attribute(XmlSerializer.AttributeCrc!);
                string? crc = attrCrc?.Value;

                if (crc != null)
                {
                    if (Crc32.CalculateHash(data).ToString("X8", CultureInfo.InvariantCulture) != crc)
                        Throw.ArgumentException(Res.XmlSerializationCrcError);
                }

                builder.AddRaw(data);
                return builder.ToArray();
            }

            // complex array: recursive deserialization needed
            foreach (XElement item in element.Elements(XmlSerializer.ElementItem))
            {
                Type? itemType = null;
                XAttribute? attrType = item.Attribute(XmlSerializer.AttributeType!);
                if (attrType != null)
                    itemType = ResolveType(attrType.Value);
                if (itemType == null)
                    itemType = builder.ElementType;

                if (TryDeserializeObject(itemType, item, null, out var value))
                {
                    builder.Add(value);
                    continue;
                }

                Throw.NotSupportedException(Res.XmlSerializationDeserializingTypeNotSupported(itemType));
            }

            return builder.ToArray();
        }

        [SecuritySafeCritical]
        private void DeserializeStructBinary(ref TryDeserializeObjectContext ctx)
        {
            if (SafeMode && ctx.Type!.IsManaged())
                Throw.ArgumentException(Res.XmlSerializationValueTypeContainsReferenceSafe(ctx.Type!));
            byte[] data = Convert.FromBase64String(ctx.Element.Value);
            XAttribute? attrCrc = ctx.Element.Attribute(XmlSerializer.AttributeCrc!);
            if (attrCrc != null)
            {
                if (Crc32.CalculateHash(data).ToString("X8", CultureInfo.InvariantCulture) != attrCrc.Value)
                    Throw.ArgumentException(Res.XmlSerializationCrcError);
            }

            ctx.Result = BinarySerializer.DeserializeValueType(ctx.Type!, data);
        }

        #endregion

        #endregion

        #endregion
    }
}
