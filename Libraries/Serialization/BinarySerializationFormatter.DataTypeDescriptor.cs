using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

namespace KGySoft.Serialization
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// Descriptor of a decoded type
        /// </summary>
        sealed class DataTypeDescriptor
        {
            private BinarySerializationOptions serializationOptions;

            internal DataTypes CollectionDataType { get; private set; }
            private DataTypes ElementDataType { get; set; }

            private DataTypeDescriptor ElementDescriptor { get; set; }
            private DataTypeDescriptor DictionaryValueDescriptor { get; set; }

            internal DataTypeDescriptor ParentDescriptor { get; private set; }

            internal BinarySerializationOptions SerializationOptions
            {
                get { return ParentDescriptor == null ? serializationOptions : ParentDescriptor.SerializationOptions; }
            }

            internal bool IsArray
            {
                get { return CollectionDataType == DataTypes.Array; }
            }

            internal bool IsDictionary
            {
                get { return CollectionDataType != DataTypes.Null && serializationInfo[CollectionDataType].IsDictionary; }
            }

#if NET35
            internal bool IsGenericDictionary
            {
                get { return CollectionDataType != DataTypes.Null && serializationInfo[CollectionDataType].IsGenericDictionary; }
            }
#elif !(NET40 || NET45)
#error .NET version is not set or not supported!
#endif

            private bool HasOptions
            {
                get
                {
                    return ((ElementDataType & DataTypes.SimpleTypes) == DataTypes.BinarySerializable) || ((ElementDataType & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph)
                        || ElementDescriptor != null && ElementDescriptor.HasOptions
                        || DictionaryValueDescriptor != null && DictionaryValueDescriptor.HasOptions;
                }
            }

            internal bool IsReadOnly { get; set; }

            internal bool IsSingleElement
            {
                get { return serializationInfo[CollectionDataType].IsSingleElement; }
            }

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

            internal DataTypeDescriptor(DataTypeDescriptor parentDescriptor, DataTypes dataType, BinaryReader reader)
            {
                ParentDescriptor = parentDescriptor;
                CollectionDataType = dataType & DataTypes.CollectionTypes;
                ElementDataType = dataType & ~DataTypes.CollectionTypes;

                // recursion 2: nested type
                if (ElementDataType == DataTypes.Null)
                {
                    ElementDescriptor = new DataTypeDescriptor(this, (DataTypes)reader.ReadUInt16(), reader);
                }
                // recursion 3: TValue in dictionaries
                if (IsDictionary)
                {
                    DictionaryValueDescriptor = new DataTypeDescriptor(this, (DataTypes)reader.ReadUInt16(), reader);
                }
            }

            /// <summary>
            /// Reads SerializationOptions if stored
            /// </summary>
            internal void TryReadOptions(BinaryReader br)
            {
                Debug.Assert(ParentDescriptor == null, "TryReadOptions can be called only on parent");

                if (HasOptions)
                    serializationOptions = ReadOptions(br);
            }

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

                // simple collection element or dictionary key
                if (ElementDataType != DataTypes.Null)
                {
                    ElementType = GetElementType(ElementDataType, br, manager);
                }
                // nested collection type in element type or in TKey of a dictionary
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
                    byte rank = br.ReadByte();
                    //Type = ElementType.MakeArrayType(rank);
                    Type = Array.CreateInstance(ElementType, new int[rank]).GetType();
                    return;
                }

                Type = GetCollectionType(CollectionDataType);
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

            private Type GetElementType(DataTypes dataType, BinaryReader br, DeserializationManager manager)
            {
                switch (dataType)
                {
                    case DataTypes.Bool:
                        return typeof(bool);
                    case DataTypes.Int8:
                        return typeof(sbyte);
                    case DataTypes.UInt8:
                        return typeof(byte);
                    case DataTypes.Int16:
                        return typeof(short);
                    case DataTypes.UInt16:
                        return typeof(ushort);
                    case DataTypes.Int32:
                        return typeof(int);
                    case DataTypes.UInt32:
                        return typeof(uint);
                    case DataTypes.Int64:
                        return typeof(long);
                    case DataTypes.UInt64:
                        return typeof(ulong);
                    case DataTypes.Char:
                        return typeof(char);
                    case DataTypes.String:
                        return typeof(string);
                    case DataTypes.Single:
                        return typeof(float);
                    case DataTypes.Double:
                        return typeof(double);
                    case DataTypes.Decimal:
                        return typeof(decimal);
                    case DataTypes.DateTime:
                        return typeof(DateTime);
                    case DataTypes.DBNull:
                        return typeof(DBNull);
                    case DataTypes.IntPtr:
                        return typeof(IntPtr);
                    case DataTypes.UIntPtr:
                        return typeof(UIntPtr);
                    case DataTypes.Version:
                        return typeof(Version);
                    case DataTypes.Guid:
                        return typeof(Guid);
                    case DataTypes.TimeSpan:
                        return typeof(TimeSpan);
                    case DataTypes.DateTimeOffset:
                        return typeof(DateTimeOffset);
                    case DataTypes.Uri:
                        return typeof(Uri);
                    case DataTypes.BitArray:
                        return typeof(BitArray);
                    case DataTypes.BitVector32:
                        return typeof(BitVector32);
                    case DataTypes.BitVector32Section:
                        return typeof(BitVector32.Section);
                    case DataTypes.StringBuilder:
                        return typeof(StringBuilder);
                    case DataTypes.Object:
                        return typeof(object);

                    case DataTypes.BinarySerializable:
                    case DataTypes.RawStruct:
                    case DataTypes.RecursiveObjectGraph:
                        return manager.ReadType(br);
                    default:
                        // nullable
                        if ((dataType & DataTypes.Nullable) == DataTypes.Nullable)
                        {
                            Type underlyingType = GetElementType(dataType & ~DataTypes.Nullable, br, manager);
                            return typeof(Nullable<>).MakeGenericType(underlyingType);
                        }

                        // enum
                        if ((dataType & DataTypes.Enum) == DataTypes.Enum)
                            return manager.ReadType(br);
                        throw new InvalidOperationException(Res.BinarySerializationCannotDecodeDataType(BinarySerializationFormatter.ToString(ElementDataType)));
                }
            }

            private Type GetCollectionType(DataTypes collectionDataType)
            {
                switch (collectionDataType)
                {
                    case DataTypes.List:
                        return (typeof(List<>).MakeGenericType(ElementType));
                    case DataTypes.LinkedList:
                        return (typeof(LinkedList<>).MakeGenericType(ElementType));
                    case DataTypes.HashSet:
                        return (typeof(HashSet<>).MakeGenericType(ElementType));
                    case DataTypes.Queue:
                        return (typeof(Queue<>).MakeGenericType(ElementType));
                    case DataTypes.Stack:
                        return (typeof(Stack<>).MakeGenericType(ElementType));
                    case DataTypes.CircularList:
                        return (typeof(CircularList<>).MakeGenericType(ElementType));
#if NET40 || NET45
                    case DataTypes.SortedSet:
                        return (typeof(SortedSet<>).MakeGenericType(ElementType));
#elif !NET35
#error .NET version is not set or not supported!
#endif

                    case DataTypes.Dictionary:
                        return (typeof(Dictionary<,>).MakeGenericType(ElementType, DictionaryValueType));
                    case DataTypes.SortedList:
                        return (typeof(SortedList<,>).MakeGenericType(ElementType, DictionaryValueType));
                    case DataTypes.SortedDictionary:
                        return (typeof(SortedDictionary<,>).MakeGenericType(ElementType, DictionaryValueType));
                    case DataTypes.CircularSortedList:
                        return (typeof(CircularSortedList<,>).MakeGenericType(ElementType, DictionaryValueType));

                    case DataTypes.ArrayList:
                        return typeof(ArrayList);
                    case DataTypes.Hashtable:
                        return typeof(Hashtable);
                    case DataTypes.QueueNonGeneric:
                        return typeof(Queue);
                    case DataTypes.StackNonGeneric:
                        return typeof(Stack);
                    case DataTypes.StringCollection:
                        return typeof(StringCollection);

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
                        return typeof(DictionaryEntry);
                    case DataTypes.DictionaryEntryNullable:
                        return typeof(DictionaryEntry?);
                    case DataTypes.KeyValuePair:
                        return (typeof(KeyValuePair<,>).MakeGenericType(ElementType, DictionaryValueType));
                    case DataTypes.KeyValuePairNullable:
                        return typeof(Nullable<>).MakeGenericType((typeof(KeyValuePair<,>).MakeGenericType(ElementType, DictionaryValueType)));

                    default:
                        throw new SerializationException(Res.BinarySerializationCannotDecodeCollectionType(BinarySerializationFormatter.ToString(collectionDataType)));
                }
            }

            internal object GetAsReadOnly(object collection)
            {
                switch (CollectionDataType)
                {
                    case DataTypes.OrderedDictionary:
                        Reflector.SetField(collection, "_readOnly", true);
                        return collection;
                    default:
                        throw new NotSupportedException(Res.BinarySerializationReadOnlyCollectionNotSupported(ToString()));
                }
            }

            public override string ToString()
            {
                return BinarySerializationFormatter.ToString(ElementDataType | CollectionDataType);
            }

            internal DataTypeDescriptor GetElementDescriptor(bool isTValue)
            {
                return !isTValue ? ElementDescriptor : DictionaryValueDescriptor;
            }

            /// <summary>
            /// If <see cref="Type"/> cannot be created/populated, then type of the instance to create can be overridden here
            /// </summary>
            internal Type GetTypeToCreate()
            {
                switch (CollectionDataType)
                {
                    case DataTypes.DictionaryEntryNullable:
                    case DataTypes.KeyValuePairNullable:
                        // nullables: returning the underlying type because they have the key/value constructor
                        return Nullable.GetUnderlyingType(Type);
                    default:
                        return Type;
                }
            }

            internal string GetFieldNameToSet(bool isValue)
            {
                switch (CollectionDataType)
                {
                    case DataTypes.DictionaryEntry:
                    case DataTypes.DictionaryEntryNullable:
                        return !isValue ? "_key" : "_value";
                    case DataTypes.KeyValuePair:
                    case DataTypes.KeyValuePairNullable:
                        return !isValue ? "key" : "value";
                    default:
                        // should never occur, throwing internal error without resource
                        throw new InvalidOperationException("GetFieldToSet is only for single element types");
                }
            }
        }
    }
}
