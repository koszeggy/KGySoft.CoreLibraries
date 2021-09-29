#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XElementSerializer.cs
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
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Security.Cryptography;
using KGySoft.Serialization.Binary;

#endregion

#region Suppressions

#pragma warning disable CA1031 // Do not catch general exception types - Exceptions are re-thrown by Throw but FxCop ignores both [ContractAnnotation] and [DoesNotReturn] attributes

#endregion

namespace KGySoft.Serialization.Xml
{
    internal class XElementSerializer : XmlSerializerBase
    {
        #region SerializeObjectContext Struct

        private struct SerializeObjectContext
        {
            #region Fields

            internal object Object;
            internal Type Type;
            internal bool TypeNeeded;
            internal XElement Parent;
            internal DesignerSerializationVisibility Visibility;

            #endregion
        }

        #endregion

        #region Constructors

        public XElementSerializer(XmlSerializationOptions options) : base(options)
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        private static void SerializeXmlSerializable(IXmlSerializable obj, XContainer parent)
        {
            StringBuilder sb = new StringBuilder();
            using (XmlWriter xw = XmlWriter.Create(sb, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
            {
                obj.WriteXml(xw);
                xw.Flush();
            }

            Type objType = obj.GetType();
            string? contentName = null;
            object[] attrs = objType.GetCustomAttributes(typeof(XmlRootAttribute), true);
            if (attrs.Length > 0)
                contentName = ((XmlRootAttribute)attrs[0]).ElementName;

            if (String.IsNullOrEmpty(contentName))
                contentName = objType.Name;

            using (XmlReader xr = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, CloseInput = true }))
            {
                if (!xr.Read())
                    return;

                XElement content = new XElement(contentName!);
                while (!xr.EOF)
                {
                    content.Add(XNode.ReadFrom(xr));
                }
                parent.Add(content);
            }

            parent.Add(new XAttribute(XmlSerializer.AttributeFormat!, XmlSerializer.AttributeValueCustom));
        }

        #endregion

        #region Instance Methods

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
        public XElement Serialize(object? obj)
        {
            XElement result = new XElement(XmlSerializer.ElementObject!);
            if (obj == null)
                return result;

            SerializeObject(obj, true, result, DesignerSerializationVisibility.Visible);
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
        /// <remarks>
        /// If the provided object in <paramref name="obj"/> parameter is a collection, then elements will be serialized, too.
        /// If you want to serialize a primitive type, then use the <see cref="Serialize"/> method.
        /// </remarks>
        public void SerializeContent(XElement parent, object obj)
        {
            if (obj == null!)
                Throw.ArgumentNullException(Argument.obj);
            if (parent == null!)
                Throw.ArgumentNullException(Argument.parent);

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
                            Throw.NotSupportedException(Res.XmlSerializationSerializingNonPopulatableCollectionNotSupported(objType));
                        if (!objType.IsReadWriteCollection(obj))
                            Throw.NotSupportedException(Res.XmlSerializationSerializingReadOnlyCollectionNotSupported(objType));

                        SerializeCollection(enumerable, objType.GetCollectionElementType()!, false, parent, DesignerSerializationVisibility.Visible);
                        return;
                    }

                    // 3.) Any object
                    SerializeMembers(obj, parent, DesignerSerializationVisibility.Visible);
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
        private void SerializeCollection(IEnumerable? collection, Type elementType, bool typeNeeded, XContainer parent, DesignerSerializationVisibility visibility)
        {
            if (collection == null)
                return;

            // signing that collection is not null - now it will be at least <Collection></Collection> instead of <Collection />
            parent.Add(String.Empty);

            // array collection
            if (collection is Array array)
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(collection.GetType())));

                // multidimensional or nonzero-based array
                if (array.Rank > 1 || array.GetLowerBound(0) != 0)
                {
                    StringBuilder dim = new StringBuilder();
                    for (int i = 0; i < array.Rank; i++)
                    {
                        int low;
                        if ((low = array.GetLowerBound(i)) != 0)
#if NET6_0_OR_GREATER
                            dim.Append(CultureInfo.InvariantCulture, $"{low}..{low + array.GetLength(i) - 1}");
#else
                            dim.Append($"{low.ToString(CultureInfo.InvariantCulture)}..{(low + array.GetLength(i) - 1).ToString(CultureInfo.InvariantCulture)}");
#endif
                        else
                            dim.Append(array.GetLength(i).ToString(CultureInfo.InvariantCulture));

                        if (i < array.Rank - 1)
                            dim.Append(',');
                    }

                    parent.Add(new XAttribute(XmlSerializer.AttributeDim!, dim));
                }
                else
                    parent.Add(new XAttribute(XmlSerializer.AttributeLength!, array.Length.ToString(CultureInfo.InvariantCulture)));

                // array of a primitive type
                if (elementType.IsPrimitive && (Options & XmlSerializationOptions.CompactSerializationOfPrimitiveArrays) != XmlSerializationOptions.None)
                {
                    if (array.Length > 0)
                    {
                        byte[] data = new byte[Buffer.ByteLength(array)];
                        Buffer.BlockCopy(array, 0, data, 0, data.Length);
                        parent.Add(Convert.ToBase64String(data));
                        if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                            parent.Add(new XAttribute(XmlSerializer.AttributeCrc!, Crc32.CalculateHash(data).ToString("X8", CultureInfo.InvariantCulture)));
                    }

                    return;
                }

                // non-primitive type array or compact serialization is not enabled
                if (elementType.IsPointer)
                    Throw.NotSupportedException(Res.SerializationPointerArrayTypeNotSupported(collection.GetType()));
                foreach (object? item in array)
                {
                    XElement child = new XElement(XmlSerializer.ElementItem!);
                    if (item != null)
                        SerializeObject(item, !elementType.IsSealed && item.GetType() != elementType, child, visibility);
                    parent.Add(child);
                }

                return;
            }

            // non-array collection
            if (typeNeeded)
                parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(collection.GetType())));

            // serializing main properties first
            SerializeMembers(collection, parent, visibility);

            // serializing items
            foreach (object? item in collection)
            {
                XElement child = new XElement(XmlSerializer.ElementItem!);
                if (item != null)
                    SerializeObject(item, !elementType.IsSealed && item.GetType() != elementType, child, visibility);
                parent.Add(child);
            }
        }

        /// <summary>
        /// Serializes a whole object. May throw exceptions on invalid or inappropriate options.
        /// XElement version.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        private void SerializeObject(object? obj, bool typeNeeded, XElement parent, DesignerSerializationVisibility visibility)
        {
            #region Local Methods to reduce complexity

            bool TrySerializeKeyValue(ref SerializeObjectContext ctx)
            {
                // 1.) KeyValue 1: DictionaryEntry: can be serialized recursively. Just handling to avoid binary serialization.
                if (ctx.Type == Reflector.DictionaryEntryType)
                {
                    if (ctx.TypeNeeded)
                        ctx.Parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(ctx.Type)));

                    SerializeMembers(ctx.Object, ctx.Parent, ctx.Visibility);
                    return true;
                }

                // 2.) KeyValue 2: KeyValuePair: properties are read-only so special support needed
                if (ctx.Type.IsGenericTypeOf(Reflector.KeyValuePairType))
                {
                    if (ctx.TypeNeeded)
                        ctx.Parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(ctx.Type)));

                    object? key = Accessors.GetPropertyValue(ctx.Object, nameof(KeyValuePair<_, _>.Key));
                    object? value = Accessors.GetPropertyValue(ctx.Object, nameof(KeyValuePair<_, _>.Value));
                    XElement xKey = new XElement(nameof(KeyValuePair<_, _>.Key)!);
                    XElement xValue = new XElement(nameof(KeyValuePair<_, _>.Value)!);
                    ctx.Parent.Add(xKey, xValue);
                    if (key != null)
                        SerializeObject(key, key.GetType() != ctx.Type.GetGenericArguments()[0], xKey, ctx.Visibility);

                    if (value != null)
                        SerializeObject(value, value.GetType() != ctx.Type.GetGenericArguments()[1], xValue, ctx.Visibility);

                    return true;
                }

                return false;
            }

            bool TrySerializeComplexObject(ref SerializeObjectContext ctx)
            {
                // 1.) collection
                if (ctx.Object is IEnumerable enumerable)
                {
                    Type? elementType = null;

                    // if can be trusted in all circumstances
                    if (IsTrustedCollection(ctx.Type)
                        // or recursive is requested
                        || ((ctx.Visibility == DesignerSerializationVisibility.Content || RecursiveSerializationAsFallback)
                            // and is a supported collection or serialization is forced
                            && (ForceReadonlyMembersAndCollections || ctx.Type.IsSupportedCollectionForReflection(out var _, out var _, out elementType, out var _))))
                    {
                        SerializeCollection(enumerable, elementType ?? ctx.Type.GetCollectionElementType()!, ctx.TypeNeeded, ctx.Parent, ctx.Visibility);
                        return true;
                    }

                    if (ctx.Visibility == DesignerSerializationVisibility.Content || RecursiveSerializationAsFallback)
                        Throw.SerializationException(Res.XmlSerializationCannotSerializeUnsupportedCollection(ctx.Type, Options));
                    Throw.SerializationException(Res.XmlSerializationCannotSerializeCollection(ctx.Type, Options));
                }

                // 2.) recursive serialization, if enabled
                if (RecursiveSerializationAsFallback || ctx.Visibility == DesignerSerializationVisibility.Content
                    // or when it has public properties/fields only
                    || IsTrustedType(ctx.Type))
                {
                    if (ctx.TypeNeeded)
                        ctx.Parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(ctx.Type)));

                    SerializeMembers(ctx.Object, ctx.Parent, ctx.Visibility);
                    return true;
                }

                return false;
            }

            #endregion

            if (obj == null)
                return;

            Type type = obj.GetType();

            // a.) If type can be natively parsed, simple adding
            if (type.CanBeParsedNatively())
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(type)));
                WriteStringValue(obj, parent);
                return;
            }

            // b.) IXmlSerializable
            if (obj is IXmlSerializable xmlSerializable && ProcessXmlSerializable)
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(type)));

                SerializeXmlSerializable(xmlSerializable, parent);
                return;
            }

            // c.) Using type converter of the type if applicable
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(type)));
                WriteStringValue(converter.ConvertToInvariantString(obj), parent);
                return;
            }

            var context = new SerializeObjectContext { Object = obj, Type = type, TypeNeeded = typeNeeded, Parent = parent, Visibility = visibility };

            // d.) Key/Value
            if (TrySerializeKeyValue(ref context))
                return;

            // e.) value type as binary only if enabled
            if (type.IsValueType && CompactSerializationOfStructures && BinarySerializer.TrySerializeValueType((ValueType)obj, out byte[]? data))
            {
                if (typeNeeded)
                    parent.Add(new XAttribute(XmlSerializer.AttributeType!, GetTypeString(type)));

                parent.Add(new XAttribute(XmlSerializer.AttributeFormat!, XmlSerializer.AttributeValueStructBinary));
                if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                    parent.Add(new XAttribute(XmlSerializer.AttributeCrc!, Crc32.CalculateHash(data).ToString("X8", CultureInfo.InvariantCulture)));
                parent.Add(Convert.ToBase64String(data));
                return;
            }

            // f.) binary serialization: base64 format to XML
            if (BinarySerializationAsFallback && visibility != DesignerSerializationVisibility.Content)
            {
                try
                {
                    SerializeBinary(obj, parent);
                    return;
                }
                catch (Exception e)
                {
                    Throw.SerializationException(Res.XmlSerializationBinarySerializationFailed(obj.GetType(), Options, e.Message), e);
                }
            }

            RegisterSerializedObject(obj);
            try
            {
                // g.) collections and other complex objects
                if (TrySerializeComplexObject(ref context))
                    return;
            }
            finally
            {
                UnregisterSerializedObject(obj);
            }

            Throw.SerializationException(Res.XmlSerializationSerializingTypeNotSupported(type, Options));
        }

        private void SerializeMembers(object obj, XContainer parent, DesignerSerializationVisibility visibility)
        {
            // signing that object is not null
            parent.Add(String.Empty);

            foreach (Member member in GetMembersToSerialize(obj))
            {
                if (SkipMember(obj, member.MemberInfo, out object? value, ref visibility))
                    continue;

                PropertyInfo? property = member.Property;
                FieldInfo? field = member.Field;
                Type memberType = property != null ? property.PropertyType : field!.FieldType;

                XElement memberElement = new XElement(member.MemberInfo.Name!);
                if (member.SpecifyDeclaringType)
                    memberElement.Add(new XAttribute(XmlSerializer.AttributeDeclaringType!, GetTypeString(member.MemberInfo.DeclaringType!)));
                Type actualType = value?.GetType() ?? memberType;

                // a.) Using explicitly defined type converter if can convert to and from string
                // Note: ResolveType can load assemblies here. When serializing, it is not a problem since the serialized object tree is always under the consumer's control.
                Attribute[] attrs = Attribute.GetCustomAttributes(member.MemberInfo, typeof(TypeConverterAttribute), true);
                if (attrs.Length > 0 && attrs[0] is TypeConverterAttribute convAttr && Reflector.ResolveType(convAttr.ConverterTypeName) is Type convType)
                {
                    ConstructorInfo? ctor = convType.GetConstructor(new Type[] { Reflector.Type });
                    object[] ctorParams = { memberType };
                    if (ctor == null)
                    {
                        ctor = convType.GetDefaultConstructor();
                        ctorParams = Reflector.EmptyObjects;
                    }

                    if (ctor != null)
                    {
                        if (CreateInstanceAccessor.GetAccessor(ctor).CreateInstance(ctorParams) is TypeConverter converter
                            && converter.CanConvertTo(Reflector.StringType) && converter.CanConvertFrom(Reflector.StringType))
                        {
                            if (value != null)
                                WriteStringValue(converter.ConvertToInvariantString(value), memberElement);
                            parent.Add(memberElement);
                            continue;
                        }
                    }
                }

                // b.) any object
                SerializeObject(value, memberType != actualType, memberElement, visibility);
                parent.Add(memberElement);
            }
        }

        /// <summary>
        /// Serializing binary content by LinqToXml
        /// </summary>
        private void SerializeBinary(object obj, XContainer parent)
        {
            parent.Add(new XAttribute(XmlSerializer.AttributeFormat!, XmlSerializer.AttributeValueBinary));
            if (obj == null!)
                return;
            BinarySerializationOptions binSerOptions = GetBinarySerializationOptions();
            byte[] data = BinarySerializer.Serialize(obj, binSerOptions);
            if ((Options & XmlSerializationOptions.OmitCrcAttribute) == XmlSerializationOptions.None)
                parent.Add(new XAttribute(XmlSerializer.AttributeCrc!, Crc32.CalculateHash(data).ToString("X8", CultureInfo.InvariantCulture)));
            parent.Add(Convert.ToBase64String(data));
        }

        private void WriteStringValue(object? obj, XElement parent)
        {
            string? s = GetStringValue(obj, out bool spacePreserved, out bool escaped);
            if (s == null)
                return;
            if (spacePreserved)
                parent.Add(new XAttribute(XNamespace.Xml + XmlSerializer.AttributeSpace, XmlSerializer.AttributeValuePreserve));
            if (escaped)
                parent.Add(new XAttribute(XmlSerializer.AttributeEscaped!, XmlSerializer.AttributeValueTrue));

            parent.Add(s);
        }

        #endregion

        #endregion

        #endregion
    }
}
