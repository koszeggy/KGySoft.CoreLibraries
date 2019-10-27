#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DataTypeDescriptor.cs
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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
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
#endif
            private bool? isSingleElement;

            private DataTypes dataType;

            #endregion

            #region Properties

            #region Internal Properties

            internal DataTypeDescriptor ParentDescriptor { get; }

            internal DataTypes ElementDataType => GetElementDataType(dataType);
            internal DataTypes CollectionDataType => GetCollectionDataType(dataType);
            internal bool IsCollection => IsCollectionType(dataType);
            internal bool IsArray => CollectionDataType == DataTypes.Array;
            internal bool IsDictionary => isDictionary ?? (isDictionary = CollectionDataType != DataTypes.Null && serializationInfo[CollectionDataType].IsDictionary).Value;
#if NET35
            internal bool IsGenericDictionary => isGenericDictionary ?? (isGenericDictionary = CollectionDataType != DataTypes.Null && serializationInfo[CollectionDataType].IsGenericDictionary).Value;
#endif
            internal bool IsReadOnly { get; set; }
            internal bool IsSingleElement => isSingleElement ?? (isSingleElement = serializationInfo[CollectionDataType].IsSingleElement).Value;

            /// <summary>
            /// Decoded type of self descriptor
            /// </summary>
            internal Type Type { get; private set; }

            /// <summary>
            /// The array rank if <see cref="IsArray"/> is <see langword="true"/>. Gets 0 for zero-based arrays.
            /// </summary>
            internal int Rank { get; private set; }

            /// <summary>
            /// The element the element/key descriptor for single collections and dictionaries.
            /// </summary>
            internal DataTypeDescriptor ElementDescriptor { get; private set; }

            /// <summary>
            /// The value descriptor for dictionaries.
            /// </summary>
            internal DataTypeDescriptor ValueDescriptor { get; }

            internal bool IsDictionaryValue => ParentDescriptor?.ValueDescriptor == this;

            internal bool CanHaveRecursion
            {
                get
                {
                    DataTypes dt = ElementDataType;
                    if ((dt & DataTypes.SimpleTypes) == DataTypes.BinarySerializable
                        || (dt & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph
                        || (dt & DataTypes.SimpleTypes) == DataTypes.Object)
                        return true;

                    if (ElementDescriptor != null && ElementDescriptor.CanHaveRecursion)
                        return true;
                    if (ValueDescriptor != null && ValueDescriptor.CanHaveRecursion)
                        return true;

                    return false;
                }
            }

            #endregion

            #endregion

            #region Constructors

            /// <summary>
            /// Initializing from stream by encoded <see cref="DataTypes"/>.
            /// </summary>
            internal DataTypeDescriptor(DataTypeDescriptor parentDescriptor, DataTypes dataType, BinaryReader reader)
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
            /// Constructor for explicitly setting already known values.
            /// </summary>
            internal DataTypeDescriptor(DataTypes elementDataType, Type type, DataTypeDescriptor parent)
            {
                ParentDescriptor = parent;
                dataType = elementDataType;
                Type = type;
            }

            /// <summary>
            /// Initializing from <see cref="Type"/> by <see cref="DeserializationManager.ReadType"/>.
            /// Here every non-native type is handled as recursive object (otherwise, they are decoded from <see cref="DataTypes"/>).
            /// </summary>
            internal DataTypeDescriptor(Type type)
            {
                static DataTypes GetDataType(Type t)
                {
                    // Primitive type
                    if (primitiveTypes.TryGetValue(t, out DataTypes result))
                        return result;

                    if (t.IsEnum)
                        return DataTypes.Enum | GetDataType(Enum.GetUnderlyingType(t));

                    return DataTypes.RecursiveObjectGraph;
                }

                if (type.IsConstructedGenericType())
                {
                    Type typeDef = type.GetGenericTypeDefinition();

                    if (typeDef == typeof(RecursiveObjectGraph<>))
                    {
                        dataType = DataTypes.RecursiveObjectGraph;
                        Type = type.GetGenericArguments()[0];
                        return;
                    }

                    if (typeDef == typeof(RawStruct<>))
                    {
                        dataType = DataTypes.RawStruct;
                        Type = type.GetGenericArguments()[0];
                        return;
                    }

                    if (typeDef == typeof(BinarySerializable<>))
                    {
                        dataType = DataTypes.BinarySerializable;
                        Type = type.GetGenericArguments()[0];
                        return;
                    }

                    if (type.IsGenericTypeOf(typeof(Compressible<>)))
                    {
                        dataType = DataTypes.Store7BitEncoded;
                        type = type.GetGenericArguments()[0];
                    }
                }

                dataType |= GetDataType(type);
                Type = type;
            }

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

                    default:
                        throw new SerializationException(Res.BinarySerializationCannotDecodeCollectionType(DataTypeToString(collectionDataType)));
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
                // Simple or impure type. Handling generics occurs in recursive ReadType if needed.
                if (CollectionDataType == DataTypes.Null)
                    return Type = GetElementType(ElementDataType, br, manager, allowOpenTypes);
                
                Type result;

                // generic type definition
                if (ElementDataType == DataTypes.GenericTypeDefinition)
                    result = GetCollectionType(CollectionDataType);
                else
                {
                    // simple collection element or dictionary key: Since in DataTypes the element is encoded together with the collection
                    // the element type descriptor was not created in the constructor. We create it now.
                    if (ElementDataType != DataTypes.Null)
                        ElementDescriptor = new DataTypeDescriptor(ElementDataType, GetElementType(ElementDataType, br, manager, allowOpenTypes), this);
                    // complex element type: recursive decoding
                    else
                        ElementDescriptor.DecodeType(br, manager, allowOpenTypes);

                    // Dictionary TValue
                    if (IsDictionary)
                        ValueDescriptor.DecodeType(br, manager, allowOpenTypes);

                    if (IsArray)
                    {
                        // 0 means zero based 1D array
                        Rank = br.ReadByte();
                        return Type = Rank == 0
                            ? ElementDescriptor.Type.MakeArrayType()
                            : ElementDescriptor.Type.MakeArrayType(Rank);
                    }

                    result = GetCollectionType(CollectionDataType);
                    if (!result.ContainsGenericParameters)
                        return Type = result;

                    bool isNullable = result.IsNullable();
                    Type typeDef = isNullable ? result.GetGenericArguments()[0] : result;
                    result = typeDef.GetGenericArguments().Length == 1
                        ? typeDef.GetGenericType(ElementDescriptor.Type)
                        : typeDef.GetGenericType(ElementDescriptor.Type, ValueDescriptor.Type);
                    result = isNullable ? Reflector.NullableType.GetGenericType(result) : result;
                }

                if (result.IsGenericTypeDefinition)
                    result = manager.HandleGenericTypeDef(br, new DataTypeDescriptor(result), allowOpenTypes, false).Type;
                return Type = result;
            }

            internal bool AreAllElementsQualified(bool isTValue)
            {
                throw new NotImplementedException(nameof(AreAllElementsQualified));
                //Type elementType = GetElementType(isTValue);

                //// true if element type is interface or not sealed class, false if struct or sealed class
                //return elementType.CanBeDerived();
            }

            /// <summary>
            /// Gets the element instance type to deserialize (never nullable)
            /// </summary>
            internal Type GetElementType(bool isTValue)
            {
                throw new NotImplementedException("TODO: delete");
                //Type result = !isTValue ? ElementDescriptor.Type : ValueDescriptor.Type;
                //if ((GetElementDataType(isTValue) & DataTypes.Nullable) == DataTypes.Nullable)
                //    return Nullable.GetUnderlyingType(result);
                //return result;
            }

            internal object GetAsReadOnly(object collection)
            {
                switch (CollectionDataType)
                {
                    case DataTypes.OrderedDictionary:
                        return ((OrderedDictionary)collection).AsReadOnly();
                    default:
                        throw new NotSupportedException(Res.BinarySerializationReadOnlyCollectionNotSupported(ToString()));
                }
            }

            internal DataTypeDescriptor GetElementDescriptor(bool isTValue) => !isTValue ? ElementDescriptor : ValueDescriptor;

            /// <summary>
            /// If <see cref="Type"/> cannot be created/populated, then type of the instance to create can be overridden here
            /// </summary>
            internal Type GetTypeToCreate()
            {
                switch (CollectionDataType)
                {
                    case DataTypes.DictionaryEntryNullable:
                    case DataTypes.KeyValuePairNullable:
                        return Nullable.GetUnderlyingType(Type);
                    default:
                        return Type;
                }
            }

            #endregion

            #region Private Methods

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple switch with many cases")]
            private Type GetElementType(DataTypes dataType, BinaryReader br, DeserializationManager manager, bool allowOpenTypes)
            {
                switch (dataType & ~DataTypes.Store7BitEncoded)
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
                        return typeof(DBNull);
                    case DataTypes.IntPtr:
                        return Reflector.IntPtrType;
                    case DataTypes.UIntPtr:
                        return Reflector.UIntPtrType;
                    case DataTypes.Version:
                        return typeof(Version);
                    case DataTypes.Guid:
                        return typeof(Guid);
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

                    case DataTypes.Pointer:
                        return ElementDescriptor.DecodeType(br, manager, allowOpenTypes).MakePointerType();
                    case DataTypes.ByRef:
                        return ElementDescriptor.DecodeType(br, manager, allowOpenTypes).MakeByRefType();

                    case DataTypes.BinarySerializable:
                    case DataTypes.RawStruct:
                    case DataTypes.RecursiveObjectGraph:
                        return manager.ReadType(br, allowOpenTypes).Type;

                    default:
                        // nullable
                        if ((dataType & DataTypes.Nullable) == DataTypes.Nullable)
                        {
                            Type underlyingType = GetElementType(dataType & ~DataTypes.Nullable, br, manager, allowOpenTypes);
                            return Reflector.NullableType.GetGenericType(underlyingType);
                        }

                        // enum
                        if ((dataType & DataTypes.Enum) == DataTypes.Enum)
                            return manager.ReadType(br).Type;
                        throw new InvalidOperationException(Res.BinarySerializationCannotDecodeDataType(DataTypeToString(ElementDataType)));
                }
            }

            #endregion

            #endregion

            #endregion
        }
    }
}
