#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlDeserializerBase.cs
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization.Xml
{
    internal abstract class XmlDeserializerBase
    {
        #region Nested Types

        protected struct ArrayBuilder
        {
            #region Constants

            private const int allocationThreshold = 1 << 13;

            #endregion

            #region Fields

            #region Internal Fields

            internal readonly Type ElementType;
            internal readonly int TotalLength;

            #endregion

            #region Private Fields

            private readonly int[] lengths;
            private readonly int[] lowerBounds;

            private Array? array;
            private ArrayIndexer? arrayIndexer;
            private IList? builder;
            private int current;

            #endregion

            #endregion

            #region Properties

            private Type ArrayType => lengths.Length > 1 ? ElementType.MakeArrayType(lengths.Length)
                : lowerBounds[0] == 0 ? ElementType.MakeArrayType()
                : ElementType.MakeArrayType(1);

            private IList Builder
            {
                get
                {
                    if (builder == null)
                    {
                        // allocating a List with limited initial capacity
                        int capacity = Math.Min(TotalLength, allocationThreshold / ElementType.SizeOf());
                        if (ElementType.IsValueType)
                        {
                            // for value types we use a strictly typed list for less boxing (though the elements will be added boxed)
                            ConstructorInfo ctor = Reflector.ListGenType.GetGenericType(ElementType).GetConstructor(new[] { Reflector.IntType })!;
                            builder = (IList)CreateInstanceAccessor.GetAccessor(ctor).CreateInstance(capacity);
                        }
                        else
                        {
                            // for reference elements simply using an object list
                            builder = new List<object>(capacity);
                        }
                    }

                    return builder;
                }
            }

            #endregion

            #region Constructors

            internal ArrayBuilder(Array? array, Type? elementType, string? attrLength, string? attrDim, bool canRecreateArray, bool safeMode) : this()
            {
                if (array == null && elementType == null)
                    Throw.ArgumentNullException(Argument.elementType);
                ParseArrayDimensions(attrLength, attrDim, out TotalLength, out lengths, out lowerBounds);

                if (array != null && CheckArray(array, lengths, lowerBounds, !canRecreateArray))
                    this.array = array;
                ElementType = elementType ?? array!.GetType().GetElementType()!;

                current = -1;
                if (this.array != null)
                {
                    if (lengths.Length > 1)
                        arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
                    return;
                }

                if (safeMode && ElementType.SizeOf() * (long)TotalLength > allocationThreshold)
                    return;

                // it is safe to allocate the array here
                this.array = Array.CreateInstance(ElementType, lengths, lowerBounds);
                if (lengths.Length > 1)
                    arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
            }

            #endregion

            #region Methods

            internal void AddRaw(byte[] data)
            {
                if (current >= 0)
                    Throw.ArgumentException(Res.XmlSerializationMixedArrayFormats);

                int count = data.Length / ElementType.SizeOf();
                if (TotalLength != count)
                    Throw.ArgumentException(Res.XmlSerializationInconsistentArrayLength(TotalLength, count));

                array ??= Array.CreateInstance(ElementType, lengths, lowerBounds);
                Buffer.BlockCopy(data, 0, array, 0, data.Length);
                current = TotalLength - 1;
            }

            internal void Add(object? value)
            {
                if (++current == TotalLength)
                    Throw.ArgumentException(Res.XmlSerializationArraySizeMismatch(ArrayType, TotalLength));

                // adding to the final array
                if (array != null)
                {
                    // 1D array
                    if (arrayIndexer == null)
                    {
                        array.SetValue(value, current + lowerBounds[0]);
                        return;
                    }

                    // Multidimensional array
                    arrayIndexer.MoveNext();
                    array.SetValue(value, arrayIndexer.Current);
                    return;
                }

                // appending the builder
                Builder.Add(value);
            }

            internal Array ToArray()
            {
                if (array != null)
                {
                    if (current != TotalLength - 1)
                        Throw.ArgumentException(Res.XmlSerializationInconsistentArrayLength(TotalLength, current + 1));

                    return array;
                }

                if (Builder.Count != TotalLength)
                    Throw.ArgumentException(Res.XmlSerializationInconsistentArrayLength(TotalLength, builder!.Count));

                array = Array.CreateInstance(ElementType, lengths, lowerBounds);

                // 1D array
                if (lengths.Length == 1)
                {
                    int offset = lowerBounds[0];
                    for (int i = 0; i < TotalLength; i++)
                        array.SetValue(builder![i], i + offset);

                    builder = null;
                    return array;
                }

                // multidimensional array
                arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
                for (int i = 0; i < TotalLength && arrayIndexer.MoveNext(); i++)
                    array.SetValue(builder![i], arrayIndexer.Current);

                builder = null;
                return array;
            }

            #endregion
        }

        #endregion

        #region Fields

        private static readonly StringKeyedDictionary<HashSet<Type>> unsafeMembers = new StringKeyedDictionary<HashSet<Type>>(2)
        {
            ["Capacity"] = new HashSet<Type> { Reflector.ListGenType, typeof(CircularList<>), typeof(ArrayList), typeof(SortedList), typeof(SortedList<,>), typeof(CircularSortedList<,>) },
            [nameof(Cache<_,_>.EnsureCapacity)] = new HashSet<Type> { typeof(Cache<,>) }
        };

        #endregion

        #region Properties

        #region Private Protected Properties

        private protected bool SafeMode { get; }

        #endregion

        #region Private Properties

        private ResolveTypeOptions ResolveTypeOptions => ResolveTypeOptions.AllowPartialAssemblyMatch
            | (SafeMode ? ResolveTypeOptions.None : ResolveTypeOptions.TryToLoadAssemblies);

        #endregion

        #endregion

        #region Constructors

        private protected XmlDeserializerBase(bool safeMode)
        {
            SafeMode = safeMode;
        }

        #endregion

        #region Methods

        #region Static Methods
        
        #region Private Protected Methods

        private protected static object CreateCollectionByInitializerCollection(ConstructorInfo collectionCtor, IEnumerable initializerCollection, Dictionary<MemberInfo, object?> members)
        {
            initializerCollection = initializerCollection.AdjustInitializerCollection(collectionCtor);
            object result = CreateInstanceAccessor.GetAccessor(collectionCtor).CreateInstance(initializerCollection);

            // restoring fields and properties of the final collection
            foreach (KeyValuePair<MemberInfo, object?> member in members)
            {
                PropertyInfo? property = member.Key as PropertyInfo;
                FieldInfo? field = property != null ? null : (FieldInfo)member.Key;

                // read-only property
                if (property?.CanWrite == false)
                {
                    object? existingValue = property.Get(result);
                    if (property.PropertyType.IsValueType)
                    {
                        if (Equals(existingValue, member.Value))
                            continue;
                        Throw.SerializationException(Res.XmlSerializationPropertyHasNoSetter(property.Name, collectionCtor.DeclaringType!));
                    }

                    if (existingValue == null && member.Value == null)
                        continue;
                    if (member.Value == null)
                        Throw.ReflectionException(Res.XmlSerializationPropertyHasNoSetterCantSetNull(property.Name, collectionCtor.DeclaringType!));
                    if (existingValue == null)
                        Throw.ReflectionException(Res.XmlSerializationPropertyHasNoSetterGetsNull(property.Name, collectionCtor.DeclaringType!));
                    if (existingValue.GetType() != member.Value.GetType())
                        Throw.ArgumentException(Res.XmlSerializationPropertyTypeMismatch(collectionCtor.DeclaringType!, property.Name, member.Value.GetType(), existingValue.GetType()));

                    CopyContent(member.Value, existingValue);
                    continue;
                }

                // read-write property
                if (property != null)
                {
                    property.Set(result, member.Value);
                    continue;
                }

                // field
                field!.Set(result, member.Value);
            }

            return result;
        }

        private protected static void HandleDeserializedMember(object obj, MemberInfo member, object? deserializedValue, object? existingValue, Dictionary<MemberInfo, object?>? members)
        {
            // 1/a.) Cache for later (obj is an initializer collection)
            if (members != null)
            {
                members[member] = deserializedValue;
                return;
            }

            // 1/b.) Successfully deserialized into the existing instance (or both are null)
            if (deserializedValue is not ValueType && ReferenceEquals(existingValue, deserializedValue))
                return;

            // 1.c.) Processing result
            // Field
            if (member is FieldInfo field)
            {
                field.Set(obj, deserializedValue);
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
                    Throw.SerializationException(Res.XmlSerializationPropertyHasNoSetter(property.Name, obj.GetType()));
                }

                if (existingValue == null)
                    Throw.ReflectionException(Res.XmlSerializationPropertyHasNoSetterGetsNull(property.Name, obj.GetType()));
                if (deserializedValue == null)
                    Throw.ReflectionException(Res.XmlSerializationPropertyHasNoSetterCantSetNull(property.Name, obj.GetType()));
                if (existingValue.GetType() != deserializedValue.GetType())
                    Throw.ArgumentException(Res.XmlSerializationPropertyTypeMismatch(obj.GetType(), property.Name, deserializedValue.GetType(), existingValue.GetType()));

                CopyContent(deserializedValue, existingValue);
                return;
            }

            // Read-write property
            property.Set(obj, deserializedValue);
        }

        private protected static void AssertCollectionItem(Type objRealType, Type? collectionElementType, string name)
        {
            if (collectionElementType == null)
            {
                if (name == XmlSerializer.ElementItem)
                    Throw.SerializationException(Res.XmlSerializationNotACollection(objRealType));
                Throw.ReflectionException(Res.XmlSerializationHasNoMember(objRealType, name));
            }

            if (name != XmlSerializer.ElementItem)
                Throw.ArgumentException(Res.XmlSerializationItemExpected(name));
        }

        private protected static string Unescape(string s)
        {
            var result = new StringBuilder(s);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == '\\')
                {
                    if (i + 1 == result.Length)
                        Throw.ArgumentException(Res.XmlSerializationInvalidEscapedContent(s));

                    // escaped backslash
                    if (result[i + 1] == '\\')
                    {
                        result.Remove(i, 1);
                    }
                    // escaped character
                    else
                    {
                        if (i + 4 >= result.Length)
                            Throw.ArgumentException(Res.XmlSerializationInvalidEscapedContent(s));

                        string escapedChar = result.ToString(i + 1, 4);
                        if (!UInt16.TryParse(escapedChar, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort charValue))
                            Throw.ArgumentException(Res.XmlSerializationInvalidEscapedContent(s));

                        result.Replace("\\" + escapedChar, ((char)charValue).ToString(null), i, 5);
                    }
                }
            }

            return result.ToString();
        }

        #endregion

        #region Private Methods

        private static void ParseArrayDimensions(string? attrLength, string? attrDim, out int totalLength, out int[] lengths, out int[] lowerBounds)
        {
            if (attrLength == null && attrDim == null)
                Throw.ArgumentException(Res.XmlSerializationArrayNoLength);

            if (attrLength != null)
            {
                lengths = new int[1];
                lowerBounds = new int[1];
                if (!Int32.TryParse(attrLength, NumberStyles.Integer, CultureInfo.InvariantCulture, out totalLength) || totalLength < 0)
                    Throw.ArgumentException(Res.XmlSerializationInvalidArrayLength(attrLength));
                lengths[0] = totalLength;
                return;
            }

            string[] dims = attrDim!.Split(',');
            lengths = new int[dims.Length];
            lowerBounds = new int[dims.Length];
            for (int i = 0; i < dims.Length; i++)
            {
                int boundSep = dims[i].IndexOf("..", StringComparison.Ordinal);
                if (boundSep == -1)
                {
                    lowerBounds[i] = 0;
                    if (!Int32.TryParse(dims[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out lengths[i]) || lengths[i] < 0)
                        Throw.ArgumentException(Res.XmlSerializationInvalidArrayLength(dims[i]));
                }
                else
                {
                    if (!Int32.TryParse(
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                        dims[i].AsSpan(0, boundSep),
#else
                        dims[i].Substring(0, boundSep),
#endif
                        NumberStyles.Integer, CultureInfo.InvariantCulture, out lowerBounds[i]))
                        Throw.ArgumentException(Res.XmlSerializationInvalidArrayBounds(dims[i]));
                    if (!Int32.TryParse(
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                            dims[i].AsSpan(boundSep + 2),
#else
                            dims[i].Substring(boundSep + 2),
#endif
                            NumberStyles.Integer, CultureInfo.InvariantCulture, out lengths[i])
                        || lengths[i] < lowerBounds[i]
                        || (long)lengths[i] - lowerBounds[i] >= Int32.MaxValue)
                    {
                        Throw.ArgumentException(Res.XmlSerializationInvalidArrayBounds(dims[i]));
                    }

                    // turning upper bound to length
                    lengths[i] -= lowerBounds[i] - 1;
                }
            }

            totalLength = lengths[0];
            try
            {
                for (int i = 1; i < lengths.Length; i++)
                    totalLength = checked(totalLength * lengths[i]);
            }
            catch (OverflowException e)
            {
                Throw.ArgumentException(Res.XmlSerializationInvalidArrayBounds(attrDim), e);
            }
        }

        private static bool CheckArray(Array array, int[] lengths, int[] lowerBounds, bool throwError)
        {
            if (lengths.Length != array.Rank)
            {
                if (throwError)
                    Throw.ArgumentException(Res.XmlSerializationArrayRankMismatch(array.GetType(), lengths.Length));
                return false;
            }

            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] != array.GetLength(i))
                {
                    if (!throwError)
                        return false;
                    if (lengths.Length == 1)
                        Throw.ArgumentException(Res.XmlSerializationArraySizeMismatch(array.GetType(), lengths[0]));
                    Throw.ArgumentException(Res.XmlSerializationArrayDimensionSizeMismatch(array.GetType(), i));
                }

                if (lowerBounds[i] != array.GetLowerBound(i))
                {
                    if (throwError)
                        Throw.ArgumentException(Res.XmlSerializationArrayLowerBoundMismatch(array.GetType(), i));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Restores target from source. Can be used for read-only properties when source object is already fully serialized.
        /// </summary>
        private static void CopyContent(object source, object target)
        {
            Debug.Assert(target.GetType() == source.GetType(), $"Same types are expected in {nameof(CopyContent)}.");

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
            SerializationHelper.CopyFields(source, target);
        }

        #endregion

        #endregion

        #region Instance Methods

        private protected Type ResolveType(string typeName)
            => Reflector.ResolveType(typeName, ResolveTypeOptions) ?? (SafeMode
                ? Throw.InvalidOperationException<Type>(Res.XmlSerializationCannotResolveTypeSafe(typeName))
                : Throw.ReflectionException<Type>(Res.XmlSerializationCannotResolveType(typeName)));

        private protected void ResolveMember(Type type, string memberOrItemName, string? strDeclaringType, string? strItemType, out PropertyInfo? property, out FieldInfo? field, out Type? itemType)
        {
            property = null;
            field = null;

            // declaring type of member is defined to avoid ambiguity
            if (strDeclaringType != null)
            {
                Type declaringType = ResolveType(strDeclaringType);
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
                itemType = ResolveType(strItemType);

            itemType ??= property?.PropertyType ?? field?.FieldType;
        }

        private protected bool TryDeserializeByConverter(MemberInfo member, Type memberType, Func<string?> readStringValue, out object? result)
        {
            TypeConverter? converter = null;

            // Explicitly defined type converter if can convert from string
            Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(TypeConverterAttribute), true);
            if (attrs.Length > 0 && attrs[0] is TypeConverterAttribute convAttr
                && Reflector.ResolveType(convAttr.ConverterTypeName, ResolveTypeOptions) is Type convType)
            {
                ConstructorInfo? ctor = convType.GetConstructor(new Type[] { Reflector.Type });
                object[] ctorParams = { memberType };
                if (ctor == null)
                {
                    ctor = convType.GetDefaultConstructor();
                    ctorParams = Reflector.EmptyObjects;
                }

                if (ctor != null)
                    converter = CreateInstanceAccessor.GetAccessor(ctor).CreateInstance(ctorParams) as TypeConverter;
            }

            if (converter?.CanConvertFrom(Reflector.StringType) != true)
            {
                result = null;
                return false;
            }

            // throwing an exception if the converter cannot handle the possibly null value
            result = converter.ConvertFromInvariantString(readStringValue.Invoke()!);
            return true;
        }

        private protected bool SkipMember(MemberInfo member)
        {
            if (!SafeMode || member is not PropertyInfo || !unsafeMembers.TryGetValue(member.Name, out HashSet<Type>? types))
                return false;

            // Skipping known unsafe members in SafeMode, which do not make functional difference anyway
            Type declaringType = member.DeclaringType!;
            if (declaringType.IsGenericType)
                declaringType = declaringType.GetGenericTypeDefinition();
            return types.Contains(declaringType);
        }

        #endregion

        #endregion
    }
}
