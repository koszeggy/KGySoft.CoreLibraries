#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.cs
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
        /// Per instance descriptor of a decoded type. Used on deserialization for supported collections.
        /// Static type information is in <see cref="CollectionSerializationInfo"/>.
        /// </summary>
        private sealed class DataTypeDescriptor
        {
            #region Fields

            private bool? isDictionary;
#if NET35
            private bool? isGenericDictionary;
#endif
            private bool? isSingleElement;

            #endregion

            #region Properties

            #region Internal Properties

            internal DataTypes CollectionDataType { get; }
            internal DataTypeDescriptor ParentDescriptor { get; }
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
            /// Type of elements in arrays and single-param generic types or TKey in dictionaries
            /// </summary>
            internal Type ElementType { get; private set; }

            /// <summary>
            /// Type of TValue in dictionaries
            /// </summary>
            internal Type DictionaryValueType { get; private set; }

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
                    if (DictionaryValueDescriptor != null && DictionaryValueDescriptor.CanHaveRecursion)
                        return true;

                    return false;
                }
            }

            #endregion

            #region Private Properties

            private DataTypes ElementDataType { get; }

            private DataTypeDescriptor ElementDescriptor { get; }

            private DataTypeDescriptor DictionaryValueDescriptor { get; }

            #endregion

            #endregion

            #region Constructors

            internal DataTypeDescriptor(DataTypeDescriptor parentDescriptor, DataTypes dataType, BinaryReader reader)
            {
                ParentDescriptor = parentDescriptor;
                CollectionDataType = dataType & DataTypes.CollectionTypes;
                ElementDataType = dataType & ~DataTypes.CollectionTypes;

                if (CollectionDataType == DataTypes.Null || ElementDataType == DataTypes.GenericTypeDefinition)
                    return;

                // recursion 2: nested type
                if (ElementDataType == DataTypes.Null)
                    ElementDescriptor = new DataTypeDescriptor(this, ReadDataType(reader), reader);
                // recursion 3: TValue in dictionaries
                if (IsDictionary)
                    DictionaryValueDescriptor = new DataTypeDescriptor(this, ReadDataType(reader), reader);
            }

            #endregion

            #region Methods

            #region Public Methods

            public override string ToString() => DataTypeToString(ElementDataType | CollectionDataType);

            #endregion

            #region Internal Methods

            /// <summary>
            /// Decodes self and element types
            /// </summary>
            internal void DecodeType(BinaryReader br, DeserializationManager manager)
            {
                // not a collection (in case of dictionary TValue or when a supported type is passed to AqnManeger by a SerializationInfo)
                if (CollectionDataType == DataTypes.Null)
                {
                    Type = GetElementType(ElementDataType, br, manager);
                    return;
                }

                if (ElementDataType == DataTypes.GenericTypeDefinition)
                {
                    Type = GetCollectionType(CollectionDataType);
                    return;
                }

                // simple collection element or dictionary key
                if (ElementDataType != DataTypes.Null)
                    ElementType = GetElementType(ElementDataType, br, manager);
                else // if (ElementDataType == DataTypes.Null)
                {
                    ElementDescriptor.DecodeType(br, manager);
                    ElementType = ElementDescriptor.Type;
                }

                // Dictionary TValue
                if (IsDictionary)
                {
                    DictionaryValueDescriptor.DecodeType(br, manager);
                    DictionaryValueType = DictionaryValueDescriptor.Type;
                }

                if (IsArray)
                {
                    // 0 means zero based 1D array
                    byte rank = br.ReadByte();
                    Type = rank == 0
                        ? ElementType.MakeArrayType()
                        : ElementType.MakeArrayType(rank);
                    return;
                }

                Type = GetCollectionType(CollectionDataType);
                if (!Type.ContainsGenericParameters)
                    return;

                bool isNullable = Type.IsNullable();
                Type typeDef = isNullable ? Type.GetGenericArguments()[0] : Type;
                Type result = typeDef.GetGenericArguments().Length == 1
                    ? typeDef.GetGenericType(ElementType)
                    : typeDef.GetGenericType(ElementType, DictionaryValueType);
                Type = isNullable ? Reflector.NullableType.GetGenericType(result) : result;
            }

            internal bool AreAllElementsQualified(bool isTValue)
            {
                Type elementType = GetElementType(isTValue);

                // true if element type is interface or not sealed class, false if struct or sealed class
                return elementType.CanBeDerived();
            }

            /// <summary>
            /// Gets the element instance type to deserialize (never nullable)
            /// </summary>
            internal Type GetElementType(bool isTValue)
            {
                Type result = !isTValue ? ElementType : DictionaryValueType;
                if ((GetElementDataType(isTValue) & DataTypes.Nullable) == DataTypes.Nullable)
                    return Nullable.GetUnderlyingType(result);
                return result;
            }

            internal DataTypes GetElementDataType(bool isTValue)
            {
                return !isTValue ? ElementDataType :
                    DictionaryValueDescriptor.CollectionDataType != DataTypes.Null ? DataTypes.Null : DictionaryValueDescriptor.ElementDataType;
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

            internal DataTypeDescriptor GetElementDescriptor(bool isTValue) => !isTValue ? ElementDescriptor : DictionaryValueDescriptor;

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
            private Type GetElementType(DataTypes dataType, BinaryReader br, DeserializationManager manager)
            {
                switch (dataType)
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
                        return manager.ReadType(br, true);

                    case DataTypes.BinarySerializable:
                    case DataTypes.RawStruct:
                    case DataTypes.RecursiveObjectGraph:
                        return manager.ReadType(br);
                    default:
                        // nullable
                        if ((dataType & DataTypes.Nullable) == DataTypes.Nullable)
                        {
                            Type underlyingType = GetElementType(dataType & ~DataTypes.Nullable, br, manager);
                            return Reflector.NullableType.GetGenericType(underlyingType);
                        }

                        // enum
                        if ((dataType & DataTypes.Enum) == DataTypes.Enum)
                            return manager.ReadType(br);
                        throw new InvalidOperationException(Res.BinarySerializationCannotDecodeDataType(DataTypeToString(ElementDataType)));
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

            #endregion
        }
    }
}
