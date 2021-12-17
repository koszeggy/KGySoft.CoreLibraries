#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.DataTypeDescriptor.cs
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
#if !NET35
using System.Collections.Concurrent; 
#endif
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization.Binary
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// Per instance descriptor of a DataTypes encoded type. Used on deserialization, mainly for supported collections.
        /// Static generic type information is in <see cref="CollectionSerializationInfo"/>.
        /// </summary>
        private sealed class DataTypeDescriptor
        {
            #region Fields

            private bool? isDictionary;
#if NET35
            private bool? isGenericDictionary;
            private bool? isGenericCollection;
#endif
            private bool? isSingleElement;

            private DataTypes dataType;

            #endregion

            #region Properties

            internal DataTypeDescriptor? ParentDescriptor { get; }
            internal DataTypes DataType => dataType;
            internal DataTypes ElementDataType => GetElementDataType(dataType);
            internal DataTypes CollectionDataType => GetCollectionDataType(dataType);
            internal bool IsCollection => IsCollectionType(dataType);
            internal bool IsArray => CollectionDataType == DataTypes.Array;
            internal bool IsDictionary => isDictionary ??= CollectionDataType != DataTypes.Null && serializationInfo[CollectionDataType].IsDictionary;
#if NET35
            internal bool IsGenericDictionary => isGenericDictionary ??= CollectionDataType != DataTypes.Null && serializationInfo[CollectionDataType].IsGenericDictionary;
            internal bool IsGenericCollection => isGenericCollection ??= CollectionDataType != DataTypes.Null && serializationInfo[CollectionDataType].IsGeneric;
#endif
            internal bool IsReadOnly { get; set; }
            internal bool IsSingleElement => isSingleElement ??= serializationInfo[CollectionDataType].IsSingleElement;
            internal bool IsNullable { get; private set; }

            /// <summary>
            /// Decoded type of self descriptor
            /// </summary>
            internal Type? Type { get; private set; }

            internal TypeByString? StoredType { get; private set; }

            /// <summary>
            /// The array rank if <see cref="IsArray"/> is <see langword="true"/>. Gets 0 for zero-based arrays.
            /// </summary>
            internal int Rank { get; private set; }

            /// <summary>
            /// The element the element/key descriptor for single collections and dictionaries.
            /// </summary>
            internal DataTypeDescriptor? ElementDescriptor { get; private set; }

            /// <summary>
            /// The value descriptor for dictionaries.
            /// </summary>
            internal DataTypeDescriptor? ValueDescriptor { get; }

            internal bool CanHaveRecursion
            {
                get
                {
                    DataTypes dt = ElementDataType;
                    if (CanHaveRecursion(dt))
                        return true;
                    if (ElementDescriptor != null && ElementDescriptor.CanHaveRecursion)
                        return true;
                    if (ValueDescriptor != null && ValueDescriptor.CanHaveRecursion)
                        return true;
                    return false;
                }
            }

            #endregion

            #region Constructors

            #region Internal Constructors

            /// <summary>
            /// Initializing from stream by encoded <see cref="DataTypes"/>.
            /// </summary>
            internal DataTypeDescriptor(DataTypeDescriptor? parentDescriptor, DataTypes dataType, BinaryReader reader)
            {
                ParentDescriptor = parentDescriptor;
                this.dataType = dataType;

                if (ElementDataType == DataTypes.GenericTypeDefinition)
                    return;

                // recursion 1: Element type in collections, pointers and ByRef types
                // (In case of simple collections ElementDescriptor will be created on decode)
                if (CollectionDataType != DataTypes.Null && ElementDataType == DataTypes.Null || ElementDataType.In(DataTypes.Pointer, DataTypes.ByRef))
                    ElementDescriptor = new DataTypeDescriptor(this, ReadDataType(reader), reader);

                // recursion 2: TValue in dictionaries
                if (IsDictionary)
                    ValueDescriptor = new DataTypeDescriptor(this, ReadDataType(reader), reader);
            }

            /// <summary>
            /// Initializing from <see cref="Type"/> by <see cref="DeserializationManager.ReadType"/>.
            /// Here every non-native type is handled as recursive object (otherwise, they are decoded from <see cref="DataTypes"/>).
            /// </summary>
            internal DataTypeDescriptor(Type type, TypeByString? storedType = null)
            {
                if (type.IsGenericTypeOf(compressibleType))
                {
                    dataType = DataTypes.Store7BitEncoded;
                    type = type.GetGenericArguments()[0];
                }

                // ReSharper disable once AssignNullToNotNullAttribute - false alarm for ReSharper in .NET Core - TODO: remove when fixed
                dataType |= primitiveTypes.GetValueOrDefault(type);
                Type = type;
                StoredType = storedType;
            }

            #endregion

            #region Private Constructors

            /// <summary>
            /// Constructor for explicitly setting already known values.
            /// </summary>
            private DataTypeDescriptor(DataTypes elementDataType, Type type, DataTypeDescriptor parent)
            {
                ParentDescriptor = parent;
                dataType = elementDataType;
                Type = type;
            }

            #endregion

            #endregion

            #region Methods

            #region Static Methods

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple switch with many cases")]
            private static Type GetCollectionType(DataTypes collectionDataType)
            {
                switch (collectionDataType)
                {
                    case DataTypes.List:
                        return Reflector.ListGenType;
                    case DataTypes.LinkedList:
                        return typeof(LinkedList<>);
                    case DataTypes.HashSet:
                        return typeof(HashSet<>);
                    case DataTypes.Queue:
                        return typeof(Queue<>);
                    case DataTypes.Stack:
                        return typeof(Stack<>);
                    case DataTypes.CircularList:
                        return typeof(CircularList<>);
#if !NET35
                    case DataTypes.SortedSet:
                        return typeof(SortedSet<>);
                    case DataTypes.ConcurrentBag:
                        return typeof(ConcurrentBag<>);
                    case DataTypes.ConcurrentQueue:
                        return typeof(ConcurrentQueue<>);
                    case DataTypes.ConcurrentStack:
                        return typeof(ConcurrentStack<>);
#endif

                    case DataTypes.ArrayList:
                        return typeof(ArrayList);
                    case DataTypes.Hashtable:
                        return typeof(Hashtable);
                    case DataTypes.QueueNonGeneric:
                        return typeof(Queue);
                    case DataTypes.StackNonGeneric:
                        return typeof(Stack);
                    case DataTypes.StringCollection:
                        return Reflector.StringCollectionType;

                    case DataTypes.Dictionary:
                        return Reflector.DictionaryGenType;
                    case DataTypes.SortedList:
                        return typeof(SortedList<,>);
                    case DataTypes.SortedDictionary:
                        return typeof(SortedDictionary<,>);
                    case DataTypes.CircularSortedList:
                        return typeof(CircularSortedList<,>);
#if !NET35
                    case DataTypes.ConcurrentDictionary:
                        return typeof(ConcurrentDictionary<,>); 
#endif

                    case DataTypes.SortedListNonGeneric:
                        return typeof(SortedList);
                    case DataTypes.ListDictionary:
                        return typeof(ListDictionary);
                    case DataTypes.HybridDictionary:
                        return typeof(HybridDictionary);
                    case DataTypes.OrderedDictionary:
                        return typeof(OrderedDictionary);
                    case DataTypes.StringDictionary:
                        return typeof(StringDictionary);

                    case DataTypes.DictionaryEntry:
                        return Reflector.DictionaryEntryType;
                    case DataTypes.DictionaryEntryNullable:
                        return typeof(DictionaryEntry?);
                    case DataTypes.KeyValuePair:
                        return Reflector.KeyValuePairType;
                    case DataTypes.KeyValuePairNullable:
                        return Reflector.NullableType.GetGenericType(Reflector.KeyValuePairType);

#if NET35
                    case DataTypes.ConcurrentDictionary:
                    case DataTypes.SortedSet:
                    case DataTypes.ConcurrentBag:
                    case DataTypes.ConcurrentQueue:
                    case DataTypes.ConcurrentStack:
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationCollectionPlatformNotSupported(DataTypeToString(collectionDataType)));
#endif

                    default:
                        return Throw.SerializationException<Type>(Res.BinarySerializationCannotDecodeCollectionType(DataTypeToString(collectionDataType)));
                }
            }

            #endregion

            #region Instance Methods

            #region Public Methods

            public override string ToString() => DataTypeToString(dataType) + ": " + Type?.GetName(TypeNameKind.ShortName);

            #endregion

            #region Internal Methods

            /// <summary>
            /// Decodes self and element types
            /// </summary>
            internal Type DecodeType(BinaryReader br, DeserializationManager manager, bool allowOpenTypes = false)
            {
                DataTypeDescriptor? existingDescriptor;

                // Simple or impure type. Handling generics occurs in recursive ReadType if needed.
                if (CollectionDataType == DataTypes.Null)
                {
                    Type = GetElementType(ElementDataType, br, manager, allowOpenTypes, out existingDescriptor);
                    StoredType = existingDescriptor?.StoredType;
                    return Type;
                }
                
                Type result;

                // generic type definition
                if (ElementDataType == DataTypes.GenericTypeDefinition)
                    result = GetCollectionType(CollectionDataType);
                else
                {
                    // simple collection element or dictionary key: Since in DataTypes the element is encoded together with the collection
                    // the element type descriptor was not created in the constructor. We create it now.
                    if (ElementDataType != DataTypes.Null)
                    {
                        ElementDescriptor = new DataTypeDescriptor(ElementDataType, GetElementType(ElementDataType, br, manager, allowOpenTypes, out existingDescriptor), this);
                        ElementDescriptor.StoredType = existingDescriptor?.StoredType;
                    }
                    // complex element type: recursive decoding
                    else
                        ElementDescriptor!.DecodeType(br, manager, allowOpenTypes);

                    // Dictionary TValue
                    if (IsDictionary)
                        ValueDescriptor!.DecodeType(br, manager, allowOpenTypes);

                    if (IsArray)
                    {
                        // 0 means zero based 1D array
                        Rank = br.ReadByte();
                        return Type = Rank == 0
                            ? ElementDescriptor.Type!.MakeArrayType()
                            : ElementDescriptor.Type!.MakeArrayType(Rank);
                    }

                    result = GetCollectionType(CollectionDataType);
                    bool isNullable = IsNullable = result.IsNullable();
                    if (!result.ContainsGenericParameters)
                        return Type = result;

                    Type typeDef = isNullable ? result.GetGenericArguments()[0] : result;
                    result = typeDef.GetGenericArguments().Length == 1
                        ? typeDef.GetGenericType(ElementDescriptor.Type!)
                        : typeDef.GetGenericType(ElementDescriptor.Type!, ValueDescriptor!.Type);
                    result = isNullable ? Reflector.NullableType.GetGenericType(result) : result;
                }

                if (result.IsGenericTypeDefinition)
                    result = manager.HandleGenericTypeDef(br, new DataTypeDescriptor(result), allowOpenTypes, false).Type!;
                return Type = result;
            }

            internal object GetAsReadOnly(object collection)
            {
                if (CollectionDataType != DataTypes.OrderedDictionary)
                    Throw.NotSupportedException(Res.BinarySerializationReadOnlyCollectionNotSupported(ToString()));
                return ((OrderedDictionary)collection).AsReadOnly();
            }

            /// <summary>
            /// If <see cref="Type"/> cannot be created/populated, then type of the instance to create can be overridden here
            /// </summary>
            internal Type GetTypeToCreate()
            {
                Debug.Assert(Type != null);
                switch (CollectionDataType)
                {
                    case DataTypes.DictionaryEntryNullable:
                    case DataTypes.KeyValuePairNullable:
                        return Nullable.GetUnderlyingType(Type!)!;
                    default:
                        return Type!;
                }
            }

            internal void ApplyAttributes(TypeAttributes attr)
            {
                Debug.Assert((dataType & ~DataTypes.Store7BitEncoded) == DataTypes.Null, "Unset DataType expected");
                if ((attr & TypeAttributes.RecursiveObjectGraph) != TypeAttributes.None)
                {
                    dataType = DataTypes.RecursiveObjectGraph;
                    return;
                }

                if ((attr & TypeAttributes.Enum) != TypeAttributes.None)
                {
                    if (!Type!.IsEnum)
                        Throw.SerializationException(Res.BinarySerializationNotAnEnum(Type));
                    dataType |= DataTypes.Enum | primitiveTypes[Enum.GetUnderlyingType(Type)];
                    return;
                }

                if ((attr & TypeAttributes.BinarySerializable) != TypeAttributes.None)
                {
                    dataType = DataTypes.BinarySerializable;
                    return;
                }

                if ((attr & TypeAttributes.RawStruct) != TypeAttributes.None)
                {
                    dataType = DataTypes.RawStruct;
                    return;
                }

                Debug.Fail($"Unexpected attributes '{attr}' for type {Type}");
            }

            #endregion

            #region Private Methods

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple switch with many cases")]
            private Type GetElementType(DataTypes dt, BinaryReader br, DeserializationManager manager, bool allowOpenTypes, out DataTypeDescriptor? existingDescriptor)
            {
                existingDescriptor = null;
                switch (dt & ~DataTypes.Store7BitEncoded)
                {
                    case DataTypes.Bool:
                        return Reflector.BoolType;
                    case DataTypes.Int8:
                        return Reflector.SByteType;
                    case DataTypes.UInt8:
                        return Reflector.ByteType;
                    case DataTypes.Int16:
                        return Reflector.ShortType;
                    case DataTypes.UInt16:
                        return Reflector.UShortType;
                    case DataTypes.Int32:
                        return Reflector.IntType;
                    case DataTypes.UInt32:
                        return Reflector.UIntType;
                    case DataTypes.Int64:
                        return Reflector.LongType;
                    case DataTypes.UInt64:
                        return Reflector.ULongType;
                    case DataTypes.Char:
                        return Reflector.CharType;
                    case DataTypes.String:
                        return Reflector.StringType;
                    case DataTypes.Single:
                        return Reflector.FloatType;
                    case DataTypes.Double:
                        return Reflector.DoubleType;
                    case DataTypes.Decimal:
                        return Reflector.DecimalType;
                    case DataTypes.DateTime:
                        return Reflector.DateTimeType;
                    case DataTypes.DBNull:
                        return Reflector.DBNullType;
                    case DataTypes.IntPtr:
                        return Reflector.IntPtrType;
                    case DataTypes.UIntPtr:
                        return Reflector.UIntPtrType;
                    case DataTypes.Version:
                        return typeof(Version);
                    case DataTypes.Guid:
                        return Reflector.GuidType;
                    case DataTypes.TimeSpan:
                        return Reflector.TimeSpanType;
                    case DataTypes.DateTimeOffset:
                        return Reflector.DateTimeOffsetType;
                    case DataTypes.Uri:
                        return typeof(Uri);
                    case DataTypes.BitArray:
                        return Reflector.BitArrayType;
                    case DataTypes.BitVector32:
                        return typeof(BitVector32);
                    case DataTypes.BitVector32Section:
                        return typeof(BitVector32.Section);
                    case DataTypes.StringBuilder:
                        return typeof(StringBuilder);
                    case DataTypes.Object:
                        return Reflector.ObjectType;
                    case DataTypes.Void:
                        return Reflector.VoidType;
                    case DataTypes.RuntimeType:
                        return Reflector.RuntimeType;

                    case DataTypes.BigInteger:
#if !NET35
                        return Reflector.BigIntegerType;
#else
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(ElementDataType)));
#endif

                    case DataTypes.Rune:
#if NETCOREAPP3_0_OR_GREATER
                        return Reflector.RuneType;
#else
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(ElementDataType)));
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    case DataTypes.Index:
                        return typeof(Index);
                    case DataTypes.Range:
                        return typeof(Range);
#else
                    case DataTypes.Index:
                    case DataTypes.Range:
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(ElementDataType)));
#endif

                    case DataTypes.Half:
#if NET5_0_OR_GREATER
                        return Reflector.HalfType;
#else
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(ElementDataType)));
#endif

#if NET6_0_OR_GREATER
                    case DataTypes.DateOnly:
                        return Reflector.DateOnlyType;
                    case DataTypes.TimeOnly:
                        return Reflector.TimeOnlyType;
#else
                    case DataTypes.DateOnly:
                    case DataTypes.TimeOnly:
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(ElementDataType)));
#endif

                    case DataTypes.Pointer:
                        return ElementDescriptor!.DecodeType(br, manager, allowOpenTypes).MakePointerType();
                    case DataTypes.ByRef:
                        return ElementDescriptor!.DecodeType(br, manager, allowOpenTypes).MakeByRefType();

                    case DataTypes.BinarySerializable:
                    case DataTypes.RawStruct:
                    case DataTypes.RecursiveObjectGraph:
                        existingDescriptor = manager.ReadType(br, allowOpenTypes);
                        return existingDescriptor.Type!;

                    default:
                        // nullable
                        if (IsNullable(dt))
                        {
                            IsNullable = true;
                            Type underlyingType = GetElementType(dt & ~DataTypes.Nullable, br, manager, allowOpenTypes, out existingDescriptor);
                            return Reflector.NullableType.GetGenericType(underlyingType);
                        }

                        // enum
                        if (IsEnum(dt))
                        {
                            existingDescriptor = manager.ReadType(br, allowOpenTypes);
                            return existingDescriptor.Type!;
                        }

                        return Throw.SerializationException<Type>(Res.BinarySerializationCannotDecodeDataType(DataTypeToString(ElementDataType)));
                }
            }

            #endregion

            #endregion

            #endregion
        }
    }
}
