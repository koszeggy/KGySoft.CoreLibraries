#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlDeserializerBase.cs
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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    internal abstract class XmlDeserializerBase
    {
        #region Methods

        #region Protected Methods

        protected static void ParseArrayDimensions(string attrLength, string attrDim, out int[] lengths, out int[] lowerBounds)
        {
            if (attrLength == null && attrDim == null)
                throw new ArgumentException(Res.XmlSerializationArrayNoLength);

            if (attrLength != null)
            {
                lengths = new int[1];
                lowerBounds = new int[1];
                if (!Int32.TryParse(attrLength, out lengths[0]))
                    throw new ArgumentException(Res.XmlSerializationLengthInvalidType(attrLength));
                return;
            }

            string[] dims = attrDim.Split(',');
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
        }

        protected static bool CheckArray(Array array, int[] lengths, int[] lowerBounds, bool throwError)
        {
            if (lengths.Length != array.Rank)
                return throwError ? throw new ArgumentException(Res.XmlSerializationArrayRankMismatch(array.GetType(), lengths.Length)) : false;

            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] != array.GetLength(i))
                {
                    if (!throwError)
                        return false;
                    if (lengths[0] == 1)
                        throw new ArgumentException(Res.XmlSerializationArraySizeMismatch(array.GetType(), lengths[0]));
                    throw new ArgumentException(Res.XmlSerializationArrayDimensionSizeMismatch(array.GetType(), i));
                }

                if (lowerBounds[i] != array.GetLowerBound(i))
                    return throwError ? throw new ArgumentException(Res.XmlSerializationArrayLowerBoundMismatch(array.GetType(), i)) : false;
            }

            return true;
        }

        protected static object CreateCollectionByInitializerCollection(ConstructorInfo collectionCtor, IEnumerable initializerCollection, Dictionary<MemberInfo, object> members)
        {
            initializerCollection = initializerCollection.AdjustInitializerCollection(collectionCtor);
            object result = Reflector.CreateInstance(collectionCtor, initializerCollection);

            // restoring fields and properties of the final collection
            foreach (KeyValuePair<MemberInfo, object> member in members)
            {
                var property = member.Key as PropertyInfo;
                var field = property != null ? null : member.Key as FieldInfo;

                // read-only property
                if (property?.CanWrite == false)
                {
                    object existingValue = Reflector.GetProperty(result, property);
                    if (property.PropertyType.IsValueType)
                    {
                        if (Equals(existingValue, member.Value))
                            continue;
                        throw new SerializationException(Res.XmlSerializationPropertyHasNoSetter(property.Name, collectionCtor.DeclaringType));
                    }

                    if (existingValue == null && member.Value == null)
                        continue;
                    if (member.Value == null)
                        throw new ReflectionException(Res.XmlSerializationPropertyHasNoSetterCantSetNull(property.Name, collectionCtor.DeclaringType));
                    if (existingValue == null)
                        throw new ReflectionException(Res.XmlSerializationPropertyHasNoSetterGetsNull(property.Name, collectionCtor.DeclaringType));
                    if (existingValue.GetType() != member.Value.GetType())
                        throw new ArgumentException(Res.XmlSerializationPropertyTypeMismatch(collectionCtor.DeclaringType, property.Name, member.Value.GetType(), existingValue.GetType()));

                    CopyContent(existingValue, member.Value);
                    continue;
                }

                // read-write property
                if (property != null)
                {
                    Reflector.SetProperty(result, property, member.Value);
                    continue;
                }

                // field
                Reflector.SetField(result, field, member.Value);
            }

            return result;
        }

        protected static void ResolveMember(Type type, string memberOrItemName, string strDeclaringType, string strItemType, out PropertyInfo property, out FieldInfo field, out Type itemType)
        {
            property = null;
            field = null;

            // declaring type of member is defined to avoid ambiguity
            if (strDeclaringType != null)
            {
                Type declaringType = Reflector.ResolveType(strDeclaringType);
                if (declaringType == null)
                    throw new ReflectionException(Res.XmlSerializationCannotResolveType(strDeclaringType));
                property = declaringType.GetProperty(memberOrItemName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (property == null)
                    field = declaringType.GetField(memberOrItemName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }
            // no declaringType so "item" in a collection means an item, in any other case we have a member
            else if (!(Reflector.IEnumerableType.IsAssignableFrom(type)) || memberOrItemName != XmlSerializer.ElementItem)
            {
                property = type.GetProperty(memberOrItemName);
                if (property == null)
                    field = type.GetField(memberOrItemName);
            }

            itemType = null;
            if (strItemType != null)
            {
                itemType = Reflector.ResolveType(strItemType);
                if (itemType == null)
                    throw new ReflectionException(Res.XmlSerializationCannotResolveType(strItemType));
            }

            if (itemType == null)
                itemType = property?.PropertyType ?? field?.FieldType;
        }

        protected static bool TryDeserializeByConverter(MemberInfo member, Type memberType, Func<string> readStringValue, out object result)
        {
            TypeConverter converter = null;

            // Explicitly defined type converter if can convert from string
            Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(TypeConverterAttribute), true);
            if (attrs.Length > 0 && attrs[0] is TypeConverterAttribute convAttr
                && Reflector.ResolveType(convAttr.ConverterTypeName) is Type convType)
            {
                ConstructorInfo ctor = convType.GetConstructor(new Type[] { Reflector.Type });
                object[] ctorParams = { memberType };
                if (ctor == null)
                {
                    ctor = convType.GetDefaultConstructor();
                    ctorParams = Reflector.EmptyObjects;
                }

                if (ctor != null)
                    converter = Reflector.CreateInstance(ctor, ctorParams) as TypeConverter;
            }

            if (converter?.CanConvertFrom(Reflector.StringType) != true)
            {
                result = null;
                return false;
            }

            result = converter.ConvertFromInvariantString(readStringValue.Invoke());
            return true;
        }

        protected static void HandleDeserializedMember(object obj, MemberInfo member, object deserializedValue, object existingValue, Dictionary<MemberInfo, object> members)
        {
            // 1/a.) Cache for later (obj is an initializer collection)
            if (members != null)
            {
                members[member] = deserializedValue;
                return;
            }

            // 1/b.) Successfully deserialized into the existing instance (or both are null)
            if (!(deserializedValue is ValueType) && ReferenceEquals(existingValue, deserializedValue))
                return;

            // 1.c.) Processing result
            // Field
            if (member is FieldInfo field)
            {
                Reflector.SetField(obj, field, deserializedValue);
                return;
            }

            var property = (PropertyInfo)member;

            // Read-only property
            if (!property.CanWrite)
            {
                if (property.PropertyType.IsValueType)
                {
                    if (Equals(existingValue, deserializedValue))
                        return;
                    throw new SerializationException(Res.XmlSerializationPropertyHasNoSetter(property.Name, obj.GetType()));
                }

                if (existingValue == null)
                    throw new ReflectionException(Res.XmlSerializationPropertyHasNoSetterGetsNull(property.Name, obj.GetType()));
                if (deserializedValue == null)
                    throw new ReflectionException(Res.XmlSerializationPropertyHasNoSetterCantSetNull(property.Name, obj.GetType()));
                if (existingValue.GetType() != deserializedValue.GetType())
                    throw new ArgumentException(Res.XmlSerializationPropertyTypeMismatch(obj.GetType(), property.Name, deserializedValue.GetType(), existingValue.GetType()));

                CopyContent(existingValue, deserializedValue);
                return;
            }

            // Read-write property
            Reflector.SetProperty(obj, property, deserializedValue);
        }

        protected static void AssertCollectionItem(Type objRealType, Type collectionElementType, string name)
        {
            if (collectionElementType == null)
            {
                if (name == XmlSerializer.ElementItem)
                    throw new SerializationException(Res.XmlSerializationNotACollection(objRealType));
                throw new ReflectionException(Res.XmlSerializationHasNoMember(objRealType, name));
            }

            if (name != XmlSerializer.ElementItem)
                throw new ArgumentException(Res.XmlSerializationItemExpected(name));
        }

        protected static string Unescape(string s)
        {
            StringBuilder result = new StringBuilder(s);

            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == '\\')
                {
                    if (i + 1 == result.Length)
                        throw new ArgumentException(Res.XmlSerializationInvalidEscapedContent(s));

                    // escaped backslash
                    if (result[i + 1] == '\\')
                    {
                        result.Remove(i, 1);
                    }
                    // escaped character
                    else
                    {
                        if (i + 4 >= result.Length)
                            throw new ArgumentException(Res.XmlSerializationInvalidEscapedContent(s));

                        string escapedChar = result.ToString(i + 1, 4);
                        ushort charValue;
                        if (!UInt16.TryParse(escapedChar, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out charValue))
                            throw new ArgumentException(Res.XmlSerializationInvalidEscapedContent(s));

                        result.Replace("\\" + escapedChar, ((char)charValue).ToString(null), i, 5);
                    }
                }
            }

            return result.ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Restores target from source. Can be used for read-only properties when source object is already fully serialized.
        /// </summary>
        private static void CopyContent(object target, object source)
        {
            Debug.Assert(target != null && source != null && target.GetType() == source.GetType(), $"Same types are expected in {nameof(CopyContent)}.");

            // 1.) Array
            if (target is Array targetArray && source is Array sourceArray)
            {
                int[] lengths = new int[sourceArray.Rank];
                int[] lowerBounds = new int[sourceArray.Rank];
                for (int i = 0; i < sourceArray.Rank; i++)
                {
                    lengths[i] = sourceArray.GetLength(i);
                    lowerBounds[i] = sourceArray.GetLowerBound(i);
                }

                CheckArray(targetArray, lengths, lowerBounds, true);
                if (targetArray.GetType().GetElementType()?.IsPrimitive == true)
                    Buffer.BlockCopy(sourceArray, 0, targetArray, 0, Buffer.ByteLength(sourceArray));
                else if (lengths.Length == 1)
                    Array.Copy(sourceArray, targetArray, sourceArray.Length);
                else
                {
                    var indices = new ArrayIndexer(lengths, lowerBounds);
                    while (indices.MoveNext())
                        targetArray.SetValue(sourceArray.GetValue(indices.Current), indices.Current);
                }

                return;
            }

            // 2.) non-array: every fields (here we don't know how was the instance serialized but we have a deserialized source)
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    Reflector.SetField(target, field, Reflector.GetField(source, field));
            }
        }

        #endregion

        #endregion
    }
}
