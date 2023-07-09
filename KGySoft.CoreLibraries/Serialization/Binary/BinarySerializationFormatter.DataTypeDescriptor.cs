#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.DataTypeDescriptor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
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
using System.Linq;
#if !NET35
using System.Numerics;
#endif
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
        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass", Justification = "Properties vs the similarly named methods with DataTypes parameter in parent class.")]
        private sealed class DataTypeDescriptor
        {
            #region Fields

            private DataTypes dataType;
            private CollectionSerializationInfo? collectionSerializationInfo;

            #endregion

            #region Properties

            #region Internal Properties
            internal DataTypeDescriptor? ParentDescriptor { get; }
            internal DataTypes DataType => dataType;
            internal DataTypes ElementDataType => GetElementDataType(dataType);
            internal DataTypes CollectionDataType => GetCollectionDataType(dataType);
            internal DataTypes UnderlyingCollectionDataType => GetUnderlyingCollectionDataType(dataType);
            internal bool IsCollection => IsCollectionType(dataType);
            internal bool IsArray => CollectionDataType == DataTypes.Array;
            internal bool IsDictionary => CollectionDataType != DataTypes.Null && CollectionSerializationInfo.IsDictionary;
#if NET35
            internal bool IsGenericDictionary => CollectionDataType != DataTypes.Null && CollectionSerializationInfo.IsGenericDictionary;
            internal bool IsGenericCollection => CollectionDataType != DataTypes.Null && CollectionSerializationInfo.IsGeneric;
#endif
            internal bool IsReadOnly { get; set; }
            internal bool IsSingleElement => CollectionSerializationInfo.IsSingleElement;
            internal bool IsNullable { get; private set; }
            internal bool HasBackingArray => CollectionSerializationInfo.GetBackingArray != null;
            internal bool HasNullableBackingArray => CollectionSerializationInfo.HasNullableBackingArray;
            internal bool IsTuple => UnderlyingCollectionDataType is >= DataTypes.Tuple1 and <= DataTypes.Tuple8 or >= DataTypes.ValueTuple1 and <= DataTypes.ValueTuple8;

            internal int FixedItemsSize
            {
                get
                {
                    DataTypes dt = UnderlyingCollectionDataType;
                    if (IsTuple(dt))
                        return GetNumberOfTupleElements(dt);
                    return dt switch
                    {
                        // TODO: VectorN, etc.
                        _ => -1,
                    };
                }
            }

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
            /// Descriptors for generic arguments.
            /// </summary>
            internal DataTypeDescriptor[] ArgumentDescriptors { get; } = Reflector.EmptyArray<DataTypeDescriptor>();

            internal bool CanHaveRecursion => CanHaveRecursion(ElementDataType) || Array.Exists(ArgumentDescriptors, i => i.CanHaveRecursion);

            #endregion

            #region Private Properties

            private CollectionSerializationInfo CollectionSerializationInfo => collectionSerializationInfo ??= CollectionDataType == DataTypes.Null
                ? Throw.InvalidOperationException<CollectionSerializationInfo>(Res.InternalError($"Not a collection: {dataType}"))
                : serializationInfo[UnderlyingCollectionDataType];

            #endregion

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

                int elements = GetNumberOfElementTypes(dataType);
                if (elements == 0 || ElementDataType == DataTypes.GenericTypeDefinition)
                    return;

                ArgumentDescriptors = new DataTypeDescriptor[elements];

                // recursion 1: Element type in collections, pointers and ByRef types
                // (In case of non nested collections/keys/1st tuple element the descriptor will be created on decode)
                if (CollectionDataType != DataTypes.Null && ElementDataType == DataTypes.Null || ElementDataType is DataTypes.Pointer or DataTypes.ByRef)
                    ArgumentDescriptors[0] = new DataTypeDescriptor(this, ReadDataType(reader), reader);

                // recursion 2..n: TValue in dictionaries or tuple item2..n
                for (int i = 1; i < ArgumentDescriptors.Length; i++)
                    ArgumentDescriptors[i] = new DataTypeDescriptor(this, ReadDataType(reader), reader);
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
                Debug.Assert(IsElementType(elementDataType));
                ParentDescriptor = parent;
                dataType = elementDataType;
                Type = type;
            }

            #endregion

            #endregion

            #region Methods

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
                    // simple collection element/dictionary key/1st tuple element: Since in DataTypes the element is encoded together with the collection
                    // the element type descriptor was not created in the constructor. We create it now.
                    if (ElementDataType != DataTypes.Null)
                    {
                        ArgumentDescriptors[0] = new DataTypeDescriptor(ElementDataType, GetElementType(ElementDataType, br, manager, allowOpenTypes, out existingDescriptor), this)
                        {
                            StoredType = existingDescriptor?.StoredType
                        };
                    }
                    // complex element type: recursive decoding
                    else
                        ArgumentDescriptors[0].DecodeType(br, manager, allowOpenTypes);

                    // Dictionary TValue/2..n
                    for (int i = 1; i < ArgumentDescriptors.Length; i++)
                        ArgumentDescriptors[i].DecodeType(br, manager, allowOpenTypes);

                    if (IsArray)
                    {
                        // 0 means zero based 1D array
                        Rank = br.ReadByte();
                        return Type = Rank == 0
                            ? ArgumentDescriptors[0].Type!.MakeArrayType()
                            : ArgumentDescriptors[0].Type!.MakeArrayType(Rank);
                    }

                    result = GetCollectionType(CollectionDataType);
                    bool isNullable = IsNullable = result.IsNullable();
                    if (!result.ContainsGenericParameters)
                        return Type = result;

                    Type typeDef = isNullable ? result.GetGenericArguments()[0] : result;
                    result = IsTuple ? typeDef.GetGenericType(ArgumentDescriptors.Select(d => d.Type!).ToArray())
                        : !IsDictionary ? typeDef.GetGenericType(ArgumentDescriptors[0].Type!)
                        : typeDef.GetGenericType(ArgumentDescriptors[0].Type!, ArgumentDescriptors[1].Type);
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

            internal Type GetTypeToCreate()
            {
                Debug.Assert(Type != null);
                return IsNullable(CollectionDataType) ? Nullable.GetUnderlyingType(Type!)! : Type!;
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
                    case DataTypes.StringSegment:
                        return typeof(StringSegment);
                    case DataTypes.StringBuilder:
                        return typeof(StringBuilder);
                    case DataTypes.Object:
                        return Reflector.ObjectType;
                    case DataTypes.Void:
                        return Reflector.VoidType;
                    case DataTypes.RuntimeType:
                        return Reflector.RuntimeType;

#if !NET35
                    case DataTypes.BigInteger:
                        return Reflector.BigIntegerType;
                    case DataTypes.Complex:
                        return typeof(Complex);
#else
                    case DataTypes.BigInteger:
                    case DataTypes.Complex:
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(ElementDataType)));
#endif

                    case DataTypes.ValueTuple0:
#if NET47_OR_GREATER || !NETFRAMEWORK
                        return typeof(ValueTuple);
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

#if NET7_0_OR_GREATER
                    case DataTypes.Int128:
                        return Reflector.Int128Type;
                    case DataTypes.UInt128:
                        return Reflector.UInt128Type;
#else
                    case DataTypes.Int128:
                    case DataTypes.UInt128:
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(ElementDataType)));
#endif

                    case DataTypes.Pointer:
                        return ArgumentDescriptors[0].DecodeType(br, manager, allowOpenTypes).MakePointerType();
                    case DataTypes.ByRef:
                        return ArgumentDescriptors[0].DecodeType(br, manager, allowOpenTypes).MakeByRefType();

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

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple switch with many cases")]
            private Type GetCollectionType(DataTypes collectionDataType)
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

#if !NET35
                    case DataTypes.Tuple1:
                        return typeof(Tuple<>);
                    case DataTypes.Tuple2:
                        return typeof(Tuple<,>);
                    case DataTypes.Tuple3:
                        return typeof(Tuple<,,>);
                    case DataTypes.Tuple4:
                        return typeof(Tuple<,,,>);
                    case DataTypes.Tuple5:
                        return typeof(Tuple<,,,,>);
                    case DataTypes.Tuple6:
                        return typeof(Tuple<,,,,,>);
                    case DataTypes.Tuple7:
                        return typeof(Tuple<,,,,,,>);
                    case DataTypes.Tuple8:
                        return typeof(Tuple<,,,,,,,>);
#endif

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

                    case DataTypes.ArraySegment:
                        return typeof(ArraySegment<>);

#if NET35
                    case DataTypes.ConcurrentDictionary:
                    case DataTypes.SortedSet:
                    case DataTypes.ConcurrentBag:
                    case DataTypes.ConcurrentQueue:
                    case DataTypes.ConcurrentStack:
                    case DataTypes.Tuple1:
                    case DataTypes.Tuple2:
                    case DataTypes.Tuple3:
                    case DataTypes.Tuple4:
                    case DataTypes.Tuple5:
                    case DataTypes.Tuple6:
                    case DataTypes.Tuple7:
                    case DataTypes.Tuple8:
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationCollectionPlatformNotSupported(DataTypeToString(collectionDataType)));
#endif

#if NET47_OR_GREATER || !NETFRAMEWORK
                    case DataTypes.ValueTuple1:
                        return typeof(ValueTuple<>);
                    case DataTypes.ValueTuple2:
                        return typeof(ValueTuple<,>);
                    case DataTypes.ValueTuple3:
                        return typeof(ValueTuple<,,>);
                    case DataTypes.ValueTuple4:
                        return typeof(ValueTuple<,,,>);
                    case DataTypes.ValueTuple5:
                        return typeof(ValueTuple<,,,,>);
                    case DataTypes.ValueTuple6:
                        return typeof(ValueTuple<,,,,,>);
                    case DataTypes.ValueTuple7:
                        return typeof(ValueTuple<,,,,,,>);
                    case DataTypes.ValueTuple8:
                        return typeof(ValueTuple<,,,,,,,>);
#else
                    case DataTypes.ValueTuple1:
                    case DataTypes.ValueTuple2:
                    case DataTypes.ValueTuple3:
                    case DataTypes.ValueTuple4:
                    case DataTypes.ValueTuple5:
                    case DataTypes.ValueTuple6:
                    case DataTypes.ValueTuple7:
                    case DataTypes.ValueTuple8:
                        return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationCollectionPlatformNotSupported(DataTypeToString(collectionDataType)));
#endif

                    default:
                        // nullable
                        if (IsNullable(collectionDataType))
                        {
                            IsNullable = true;
                            Type underlyingType = GetCollectionType(collectionDataType & ~DataTypes.NullableExtendedCollection);
                            return Reflector.NullableType.GetGenericType(underlyingType);
                        }

                        return Throw.SerializationException<Type>(Res.BinarySerializationCannotDecodeCollectionType(DataTypeToString(collectionDataType)));
                }
            }

            #endregion

            #endregion
        }
    }
}
