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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
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
        /// A manager class that provides that stored types will be built up in the same order both at serialization and deserialization for complex types.
        /// </summary>
        private sealed class SerializationManager : SerializationManagerBase
        {
            #region WriteTypeContext Struct

#if !NET35
            private struct WriteTypeContext
            {
                #region Fields

                internal Type Type;
                internal string BinderAsmName;
                internal string BinderTypeName;

                #endregion
            }
#endif

            #endregion

            #region Constants

            private const int ticksPerMinute = 600_000_000;

            #endregion

            #region Fields

            private Dictionary<Assembly, int> assemblyIndexCache;
            private Dictionary<Type, int> typeIndexCache;
#if !NET35
            private Dictionary<string, int> assemblyNameIndexCache;
            private Dictionary<string, int> typeNameIndexCache;
#endif
            private int idCounter;
            private Dictionary<object, int> idCacheByValue;
            private Dictionary<object, int> idCacheByRef;

            #endregion

            #region Properties

            private int AssemblyIndexCacheCount
            {
                get
                {
                    return (assemblyIndexCache?.Count ?? KnownAssemblies.Length)
#if !NET35
                        + (assemblyNameIndexCache?.Count ?? 0)
#endif
                        ;
                }
            }

            private int TypeIndexCacheCount
            {
                get
                {
                    return (typeIndexCache?.Count ?? KnownTypes.Length)
#if !NET35
                        + (typeNameIndexCache?.Count ?? 0)
#endif
                        ;
                }
            }

            #endregion

            #region Constructors

            internal SerializationManager(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector) :
                base(context, options, binder, surrogateSelector)
            {
            }

            #endregion

            #region Methods

            #region Static Methods

            /// <summary>
            /// Writes a <paramref name="length"/> bytes length value in the possible most compact form.
            /// </summary>
            private static void WriteDynamicInt(BinaryWriter bw, DataTypes dataType, int length, ulong value)
            {
                switch (length)
                {
                    case 2:
                        if (value >= (1UL << 7)) // up to 7 bits
                        {
                            WriteDataType(bw, dataType);
                            bw.Write((ushort)value);
                            return;
                        }

                        break;

                    case 4:
                        if (value >= (1UL << 21)) // up to 3*7 bits
                        {
                            WriteDataType(bw, dataType);
                            bw.Write((uint)value);
                            return;
                        }

                        break;

                    case 8:
                        if (value >= (1UL << 49)) // up to 7*7 bits
                        {
                            WriteDataType(bw, dataType);
                            bw.Write(value);
                            return;
                        }

                        break;

                    default:
                        // should never occur, throwing internal error without resource
                        throw new ArgumentOutOfRangeException(nameof(length));
                }

                // storing the value as 7-bit encoded int, which will be shorter
                dataType |= DataTypes.Store7BitEncoded;
                WriteDataType(bw, dataType);
                Write7BitLong(bw, value);
            }

            /// <summary>
            /// Returning a true value just indicates that the type itself supported without the generic parameters or element type.
            /// </summary>
            private static bool IsSupportedCollection(Type type)
            {
                if (type.IsArray)
                    return true;
                if (type.IsValueType)
                    type = Nullable.GetUnderlyingType(type) ?? type;
                if (type.IsGenericType)
                    type = type.GetGenericTypeDefinition();
                return supportedCollections.ContainsKey(type);
            }

            private static DataTypes GetSupportedCollectionType(Type type)
            {
                if (type.IsArray)
                    return DataTypes.Array;

                if (type.IsNullable())
                {
                    switch (GetSupportedCollectionType(type.GetGenericArguments()[0]))
                    {
                        case DataTypes.DictionaryEntry:
                            return DataTypes.DictionaryEntryNullable;
                        case DataTypes.KeyValuePair:
                            return DataTypes.KeyValuePairNullable;
                        default:
                            return DataTypes.Null;
                    }
                }

                if (type.IsGenericType)
                    type = type.GetGenericTypeDefinition();
                return supportedCollections.GetValueOrDefault(type);
            }

            /// <summary>
            /// Retrieves the value type(s) for a dictionary.
            /// </summary>
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple method with many common cases")]
            private static IList<DataTypes> GetDictionaryValueTypes(IList<DataTypes> collectionTypeDescriptor)
            {
                // descriptor must refer a generic dictionary type here
                Debug.Assert(collectionTypeDescriptor.Count > 0, "Type description is invalid: not enough data");
#if DEBUG
                int collType = ((int)(collectionTypeDescriptor[0] & DataTypes.CollectionTypes) >> 8);
                Debug.Assert(collType >= 16 && collType < 32 || collType >= 48 && collType < 64, 
                    $"Type description is invalid: {collectionTypeDescriptor[0] & DataTypes.CollectionTypes} is not a dictionary type.");
#endif

                CircularList<DataTypes> result = new CircularList<DataTypes>();
                int skipLevel = 0;
                bool startingDictionaryResolved = false;
                foreach (DataTypes dataType in collectionTypeDescriptor)
                {
                    // we reached the value
                    if (startingDictionaryResolved && skipLevel == 0)
                    {
                        result.Add(dataType);
                        continue;
                    }

                    switch (dataType & DataTypes.CollectionTypes)
                    {
                        // No collection type indicated: element type belongs to an already skipped previous collection.
                        case DataTypes.Null:
                            skipLevel--;
                            break;

                        // Collections with a single element: decreasing level if element is specified.
                        // Otherwise it is a nested collection, skip level kept for the next item.
                        case DataTypes.Array:
                        case DataTypes.List:
                        case DataTypes.LinkedList:
                        case DataTypes.HashSet:
                        case DataTypes.Queue:
                        case DataTypes.Stack:
                        case DataTypes.CircularList:
                        case DataTypes.SortedSet:
                        case DataTypes.ArrayList:
                        case DataTypes.QueueNonGeneric:
                        case DataTypes.StackNonGeneric:
                        case DataTypes.StringCollection:
                            if ((dataType & ~DataTypes.CollectionTypes) != DataTypes.Null)
                                skipLevel--;
                            break;

                        // Dictionary type: Entry point of the loop or skipped nested key collections.
                        // If element type is specified, value type starts on next position.
                        // Otherwise, key is a nested collection and we need to skip it.
                        case DataTypes.Dictionary:
                        case DataTypes.SortedList:
                        case DataTypes.SortedDictionary:
                        case DataTypes.CircularSortedList:
                        case DataTypes.Hashtable:
                        case DataTypes.SortedListNonGeneric:
                        case DataTypes.ListDictionary:
                        case DataTypes.HybridDictionary:
                        case DataTypes.OrderedDictionary:
                        case DataTypes.StringDictionary:
                        case DataTypes.KeyValuePair:
                        case DataTypes.DictionaryEntry:
                        case DataTypes.KeyValuePairNullable:
                        case DataTypes.DictionaryEntryNullable:
                            // this check works because flags cannot be combined with collection types (nullable "collections" have different values)
                            if ((dataType & ~DataTypes.CollectionTypes) == DataTypes.Null)
                                skipLevel++;
                            startingDictionaryResolved = true;
                            break;
                    }
                }

                return result;
            }

            private static bool TryWritePrimitive(BinaryWriter bw, object data)
            {
                if (data == null)
                {
                    WriteDataType(bw, DataTypes.Null);
                    return true;
                }

                switch (primitiveTypes.GetValueOrDefault(data.GetType()))
                {
                    case DataTypes.Bool:
                        WriteDataType(bw, DataTypes.Bool);
                        bw.Write((bool)data);
                        return true;
                    case DataTypes.UInt8:
                        WriteDataType(bw, DataTypes.UInt8);
                        bw.Write((byte)data);
                        return true;
                    case DataTypes.Int8:
                        WriteDataType(bw, DataTypes.Int8);
                        bw.Write((sbyte)data);
                        return true;
                    case DataTypes.Int16:
                        WriteDynamicInt(bw, DataTypes.Int16, 2, (ulong)(short)data);
                        return true;
                    case DataTypes.UInt16:
                        WriteDynamicInt(bw, DataTypes.UInt16, 2, (ushort)data);
                        return true;
                    case DataTypes.Int32:
                        WriteDynamicInt(bw, DataTypes.Int32, 4, (ulong)(int)data);
                        return true;
                    case DataTypes.UInt32:
                        WriteDynamicInt(bw, DataTypes.UInt32, 4, (uint)data);
                        return true;
                    case DataTypes.Int64:
                        WriteDynamicInt(bw, DataTypes.Int64, 8, (ulong)(long)data);
                        return true;
                    case DataTypes.UInt64:
                        WriteDynamicInt(bw, DataTypes.UInt64, 8, (ulong)data);
                        return true;
                    case DataTypes.Char:
                        WriteDynamicInt(bw, DataTypes.Char, 2, (char)data);
                        return true;
                    case DataTypes.String:
                        WriteDataType(bw, DataTypes.String);
                        bw.Write((string)data);
                        return true;
                    case DataTypes.Single:
                        WriteDynamicInt(bw, DataTypes.Single, 4, BitConverter.ToUInt32(BitConverter.GetBytes((float)data), 0));
                        return true;
                    case DataTypes.Double:
                        WriteDynamicInt(bw, DataTypes.Double, 8, (ulong)BitConverter.DoubleToInt64Bits((double)data));
                        return true;
                    case DataTypes.IntPtr:
                        WriteDynamicInt(bw, DataTypes.IntPtr, 8, (ulong)(IntPtr)data);
                        return true;
                    case DataTypes.UIntPtr:
                        WriteDynamicInt(bw, DataTypes.UIntPtr, 8, (ulong)(UIntPtr)data);
                        return true;
                    default:
                        return false;
                }
            }

            private static bool TryWriteSimpleNonPrimitive(BinaryWriter bw, object data)
            {
                switch (supportedNonPrimitiveElementTypes.GetValueOrDefault(data.GetType()))
                {
                    case DataTypes.Decimal:
                        WriteDataType(bw, DataTypes.Decimal);
                        bw.Write((decimal)data);
                        return true;
                    case DataTypes.DateTime:
                        WriteDataType(bw, DataTypes.DateTime);
                        WriteDateTime(bw, (DateTime)data);
                        return true;
                    case DataTypes.DateTimeOffset:
                        WriteDataType(bw, DataTypes.DateTimeOffset);
                        WriteDateTimeOffset(bw, (DateTimeOffset)data);
                        return true;
                    case DataTypes.TimeSpan:
                        WriteDataType(bw, DataTypes.TimeSpan);
                        bw.Write(((TimeSpan)data).Ticks);
                        return true;
                    case DataTypes.DBNull:
                        WriteDataType(bw, DataTypes.DBNull);
                        return true;
                    case DataTypes.Guid:
                        WriteDataType(bw, DataTypes.Guid);
                        bw.Write(((Guid)data).ToByteArray());
                        return true;
                    case DataTypes.BitVector32:
                        WriteDataType(bw, DataTypes.BitVector32);
                        bw.Write(((BitVector32)data).Data);
                        return true;
                    case DataTypes.BitVector32Section:
                        WriteDataType(bw, DataTypes.BitVector32Section);
                        WriteSection(bw, (BitVector32.Section)data);
                        return true;
                    case DataTypes.Version:
                        WriteDataType(bw, DataTypes.Version);
                        WriteVersion(bw, (Version)data);
                        return true;
                    case DataTypes.BitArray:
                        WriteDataType(bw, DataTypes.BitArray);
                        WriteBitArray(bw, (BitArray)data);
                        return true;
                    case DataTypes.StringBuilder:
                        WriteDataType(bw, DataTypes.StringBuilder);
                        WriteStringBuilder(bw, (StringBuilder)data);
                        return true;
                    case DataTypes.Object:
                        WriteDataType(bw, DataTypes.Object);
                        return true;
                    case DataTypes.Uri:
                        WriteDataType(bw, DataTypes.Uri);
                        WriteUri(bw, (Uri)data);
                        return true;
                    default:
                        return false;
                }
            }

            private static void WriteDateTime(BinaryWriter bw, DateTime dateTime)
            {
                bw.Write((byte)dateTime.Kind);
                bw.Write(dateTime.Ticks);
            }

            private static void WriteDateTimeOffset(BinaryWriter bw, DateTimeOffset dateTimeOffset)
            {
                bw.Write(dateTimeOffset.Ticks);
                bw.Write((short)(dateTimeOffset.Offset.Ticks / ticksPerMinute));
            }

            private static void WriteVersion(BinaryWriter bw, Version version)
            {
                bw.Write(version.Major);
                bw.Write(version.Minor);
                bw.Write(version.Build);
                bw.Write(version.Revision);
            }

            private static void WriteUri(BinaryWriter bw, Uri uri)
            {
                bw.Write(uri.IsAbsoluteUri);
                bw.Write(uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped));
            }

            private static void WriteBitArray(BinaryWriter bw, BitArray bitArray)
            {
                int length = bitArray.Length;
                Write7BitInt(bw, bitArray.Length);
                if (length > 0)
                {
                    int[] value = bitArray.GetUnderlyingArray();
                    foreach (int i in value)
                        bw.Write(i);
                }
            }

            private static void WriteStringBuilder(BinaryWriter bw, StringBuilder sb)
            {
                Write7BitInt(bw, sb.Capacity);
                bw.Write(sb.ToString());
            }

            private static void WriteSection(BinaryWriter bw, BitVector32.Section section)
            {
                bw.Write(section.Mask);
                bw.Write(section.Offset);
            }

            #endregion

            #region Instance Methods

            #region Internal Methods

            /// <summary>
            /// Writing an object. Can be used both at root and object element level.
            /// </summary>>
            [SecurityCritical]
            internal void Write(BinaryWriter bw, object data, bool isRoot)
            {
                // if an existing id found, returning
                if (!isRoot && WriteId(bw, data))
                    return;

                // a.) Natively supported primitive types including string (no need to distinct nullable types here as they are boxed)
                if (TryWritePrimitive(bw, data))
                    return;

                // b.) Surrogate selector for any type
                Type type = data.GetType();
                if (ForceRecursiveSerializationOfSupportedTypes && !type.IsArray || TryUseSurrogateSelectorForAnyType && CanUseSurrogate(type))
                {
                    WriteRecursively(bw, data, isRoot);
                    return;
                }

                // c.) Natively supported non-primitive single types
                if (TryWriteSimpleNonPrimitive(bw, data))
                    return;

                // d.) enum: storing enum type, assembly qualified name and value: still shorter than by BinaryFormatter
                if (data is Enum)
                {
                    WriteEnum(bw, data);
                    return;
                }

                // e.) Supported collection or compound of collections
                if (TryWriteCollection(bw, data, isRoot))
                    return;

                // f.) BinarySerializable
                if (!IgnoreIBinarySerializable && data is IBinarySerializable binarySerializable)
                {
                    WriteDataType(bw, DataTypes.BinarySerializable);

                    if (isRoot)
                    {
                        // on root level writing the id even if the object is value type because the boxed reference can be shared
                        if (WriteId(bw, data))
                            Debug.Fail("Id of recursive object should be unknown on top level.");
                    }

                    WriteType(bw, type);
                    WriteBinarySerializable(bw, binarySerializable);
                    return;
                }

                // g.) Any struct if can serialize
                if (CompactSerializationOfStructures && type.IsValueType && BinarySerializer.CanSerializeValueType(type, false))
                {
                    WriteDataType(bw, DataTypes.RawStruct);
                    WriteType(bw, type);
                    WriteValueType(bw, data);
                    return;
                }

                // h.) Recursive serialization: if enabled or surrogate selector supports the type, or when type is serializable
                if (RecursiveSerializationAsFallback || CanUseSurrogate(type) || type.IsSerializable)
                {
                    WriteRecursively(bw, data, isRoot);
                    return;
                }

#pragma warning disable 618, 612
                // i.) Any struct (obsolete but still supported as backward compatibility)
                if (ForcedSerializationValueTypesAsFallback && type.IsValueType)
                {
                    WriteDataType(bw, DataTypes.RawStruct);
                    WriteType(bw, type);
                    WriteValueType(bw, data);
                    return;
                }
#pragma warning restore 618, 612

                ThrowNotSupported(type);
            }

            #endregion

            #region Private Methods

            private void ThrowNotSupported(Type type) => throw new NotSupportedException(Res.BinarySerializationNotSupported(type, Options));

            /// <summary>
            /// Writes AssemblyQualifiedName of element types and array ranks if needed
            /// </summary>
            [SecurityCritical]
            private void WriteTypeNamesAndRanks(BinaryWriter bw, Type type, bool pureOnly)
            {
                // Enum, BinarySerializable, RawStruct, recursive serialization: type name
                DataTypes elementType = GetSupportedElementType(type, pureOnly);
                if ((elementType & DataTypes.Enum) != DataTypes.Null
                    || (elementType & DataTypes.SimpleTypes) == DataTypes.BinarySerializable
                    || (elementType & DataTypes.SimpleTypes) == DataTypes.RawStruct
                    || (elementType & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph)
                {
                    Debug.Assert(!pureOnly, "Pure types do not require type name.");
                    if ((elementType & DataTypes.Nullable) == DataTypes.Nullable)
                        type = Nullable.GetUnderlyingType(type);
                    WriteType(bw, type);
                }
                // Array: element type name and rank
                else if (type.IsArray)
                {
                    WriteTypeNamesAndRanks(bw, type.GetElementType(), pureOnly);
                    bw.Write((byte)type.GetArrayRank());
                }
                // recursion for generic arguments
                else if (IsSupportedCollection(type))
                {
                    foreach (Type genericArgument in type.GetGenericArguments())
                        WriteTypeNamesAndRanks(bw, genericArgument, pureOnly);
                }
            }

            [SecurityCritical]
            private IEnumerable<DataTypes> EncodeCollectionType(Type type, bool pureOnly)
            {
                // array
                if (type.IsArray)
                    return EncodeArray(type, pureOnly);

                DataTypes collectionType = GetSupportedCollectionType(type);
                type = Nullable.GetUnderlyingType(type) ?? type;

                // generic type
                if (type.IsGenericType)
                    return EncodeGenericCollection(type, collectionType, pureOnly);

                // non-generic types
                switch (collectionType)
                {
                    case DataTypes.ArrayList:
                    case DataTypes.QueueNonGeneric:
                    case DataTypes.StackNonGeneric:
                        return new[] { collectionType | DataTypes.Object };

                    case DataTypes.Hashtable:
                    case DataTypes.SortedListNonGeneric:
                    case DataTypes.ListDictionary:
                    case DataTypes.HybridDictionary:
                    case DataTypes.OrderedDictionary:
                    case DataTypes.DictionaryEntry:
                    case DataTypes.DictionaryEntryNullable:
                        return new[] { collectionType | DataTypes.Object, DataTypes.Object };

                    case DataTypes.StringCollection:
                        return new[] { collectionType | DataTypes.String };

                    case DataTypes.StringDictionary:
                        return new[] { collectionType | DataTypes.String, DataTypes.String };
                    default:
                        // should never occur, throwing internal error without resource
                        throw new InvalidOperationException("Element type of non-generic collection is not defined: " + DataTypeToString(collectionType));
                }
            }

            [SecurityCritical]
            private IEnumerable<DataTypes> EncodeArray(Type type, bool pureOnly)
            {
                Type elementType = type.GetElementType();
                if (!pureOnly && TryUseSurrogateSelectorForAnyType && CanUseSurrogate(elementType))
                {
                    DataTypes[] result = { DataTypes.Array | DataTypes.RecursiveObjectGraph };
                    if (elementType.IsNullable())
                        result[0] |= DataTypes.Nullable;
                    return result;
                }

                DataTypes elementDataType = GetSupportedElementType(elementType, pureOnly);
                if (elementDataType != DataTypes.Null)
                    return new[] { DataTypes.Array | elementDataType };

                if (IsSupportedCollection(elementType))
                {
                    IEnumerable<DataTypes> innerType = EncodeCollectionType(elementType, pureOnly);
                    if (innerType != null)
                        return new[] { DataTypes.Array }.Concat(innerType);
                }

                return null;
            }

            [SecurityCritical]
            private IEnumerable<DataTypes> EncodeGenericCollection(Type type, DataTypes collectionType, bool pureOnly)
            {
                if (collectionType == DataTypes.Null)
                    return null;

                Type[] args = type.GetGenericArguments();
                Type elementType = args[0];
                DataTypes elementDataType = GetSupportedElementType(elementType, pureOnly);

                // generics with 1 argument
                if (args.Length == 1)
                {
                    if (elementDataType != DataTypes.Null)
                        return new[] { collectionType | elementDataType };

                    if (IsSupportedCollection(elementType))
                    {
                        IEnumerable<DataTypes> innerType = EncodeCollectionType(elementType, pureOnly);
                        if (innerType != null)
                            return (new[] { collectionType }).Concat(innerType);
                    }

                    return null;
                }

                // dictionaries
                Type valueType = args[1];
                DataTypes valueDataType = GetSupportedElementType(valueType, pureOnly);

                IEnumerable<DataTypes> keyTypes;
                IEnumerable<DataTypes> valueTypes;

                // key
                if (elementDataType != DataTypes.Null)
                    keyTypes = new DataTypes[] { collectionType | elementDataType };
                else if (IsSupportedCollection(elementType))
                {
                    keyTypes = EncodeCollectionType(elementType, pureOnly);
                    if (keyTypes == null)
                        return null;
                    keyTypes = (new DataTypes[] { collectionType }).Concat(keyTypes);
                }
                else
                    return null;

                // value
                if (valueDataType != DataTypes.Null)
                    valueTypes = new DataTypes[] { valueDataType };
                else if (IsSupportedCollection(valueType))
                {
                    valueTypes = EncodeCollectionType(valueType, pureOnly);
                    if (valueTypes == null)
                        return null;
                }
                else
                    return null;

                return keyTypes.Concat(valueTypes);
            }

            /// <summary>
            /// Gets the <see cref="DataTypes"/> representation of <paramref name="type"/> as an element type.
            /// If <paramref name="pureOnly"/> is <see langword="true"/>, then only types without required name are returned.
            /// </summary>
            [SecurityCritical]
            private DataTypes GetSupportedElementType(Type type, bool pureOnly)
            {
                DataTypes elementType;

                // a.) nullable (must be before surrogate-support checks)
                if (type.IsNullable())
                {
                    elementType = GetSupportedElementType(type.GetGenericArguments()[0], pureOnly);
                    if (elementType == DataTypes.Null)
                        return elementType;
                    return DataTypes.Nullable | elementType;
                }

                // b.) Natively supported primitive types
                if (primitiveTypes.TryGetValue(type, out elementType))
                    return elementType;

                // c.) recursion for any type: check even for sub-collections
                if (!pureOnly && (ForceRecursiveSerializationOfSupportedTypes && !type.IsArray || TryUseSurrogateSelectorForAnyType && CanUseSurrogate(type)))
                    return supportedNonPrimitiveElementTypes.GetValueOrDefault(type, DataTypes.RecursiveObjectGraph);

                // e.) Natively supported non-primitive types
                if (supportedNonPrimitiveElementTypes.TryGetValue(type, out elementType))
                    return elementType;

                if (pureOnly)
                    return DataTypes.Null;

                // d.) enum
                if (type.IsEnum)
                    return DataTypes.Enum | GetSupportedElementType(Enum.GetUnderlyingType(type), false);

                // if type is a collection, then returning null here
                if (GetSupportedCollectionType(type) != DataTypes.Null)
                    return DataTypes.Null;

                // f.) IBinarySerializable implementation
                if (!IgnoreIBinarySerializable && typeof(IBinarySerializable).IsAssignableFrom(type))
                    return DataTypes.BinarySerializable;

                // g.) Any struct if can be serialized
                if (CompactSerializationOfStructures && type.IsValueType && BinarySerializer.CanSerializeValueType(type, false))
                    return DataTypes.RawStruct;

                // h.) Recursive serialization
                if (RecursiveSerializationAsFallback || type.IsInterface || type.IsSerializable || CanUseSurrogate(type))
                    return DataTypes.RecursiveObjectGraph;

#pragma warning disable 618, 612
                // i.) Any struct (obsolete but still supported as backward compatibility)
                if (ForcedSerializationValueTypesAsFallback && type.IsValueType)
                    return DataTypes.RawStruct;
#pragma warning restore 618, 612

                return DataTypes.Null;
            }

            [SecurityCritical]
            private void WriteEnum(BinaryWriter bw, object enumObject)
            {
                Type type = enumObject.GetType();
                DataTypes dataType = primitiveTypes.GetValueOrDefault(Enum.GetUnderlyingType(type));

                ulong enumValue = dataType == DataTypes.UInt64 ? (ulong)enumObject : (ulong)((IConvertible)enumObject).ToInt64(null);

                bool is7Bit = false;
                int size = 1;
                switch (dataType)
                {
                    case DataTypes.Int8:
                    case DataTypes.UInt8:
                        break;
                    case DataTypes.Int16:
                    case DataTypes.UInt16:
                        size = 2;
                        is7Bit = enumValue < (1UL << 7);
                        break;
                    case DataTypes.Int32:
                    case DataTypes.UInt32:
                        size = 4;
                        is7Bit = enumValue < (1UL << 21);
                        break;
                    case DataTypes.Int64:
                    case DataTypes.UInt64:
                        size = 8;
                        is7Bit = enumValue < (1UL << 49);
                        break;
                    default:
                        // should never occur, throwing internal error without resource
                        throw new ArgumentOutOfRangeException(nameof(enumObject));
                }

                dataType |= DataTypes.Enum;
                if (is7Bit)
                    dataType |= DataTypes.Store7BitEncoded;

                WriteDataType(bw, dataType);
                WriteType(bw, type);
                if (is7Bit)
                    Write7BitLong(bw, enumValue);
                else
                    bw.Write(BitConverter.GetBytes(enumValue), 0, size);
            }

            [SecurityCritical]
            private bool TryWriteCollection(BinaryWriter bw, object data, bool isRoot)
            {
                bool CanHaveRecursion(CircularList<DataTypes> dataTypes)
                    => dataTypes.Exists(dt =>
                        (dt & DataTypes.SimpleTypes) == DataTypes.BinarySerializable
                        || (dt & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph
                        || (dt & DataTypes.SimpleTypes) == DataTypes.Object);

                Type type = data.GetType();

                if (!IsSupportedCollection(type))
                    return false;

                IEnumerable<DataTypes> collectionType = EncodeCollectionType(type, false);
                if (collectionType == null)
                    return false;

                CircularList<DataTypes> collectionTypeList = new CircularList<DataTypes>(collectionType);
                foreach (DataTypes dataType in collectionTypeList)
                    WriteDataType(bw, dataType);

                if (isRoot && CanHaveRecursion(collectionTypeList))
                {
                    if (WriteId(bw, data))
                        Debug.Fail("Id of recursive object should be unknown on top level.");
                }

                WriteTypeNamesAndRanks(bw, type, false);
                WriteCollection(bw, collectionTypeList, data);
                return true;
            }

            [SecurityCritical]
            private void WriteCollection(BinaryWriter bw, IList<DataTypes> collectionTypeDescriptor, object obj)
            {
                if (collectionTypeDescriptor.Count == 0)
                    // should never occur, throwing internal error without resource
                    throw new ArgumentException("Type description is invalid", nameof(collectionTypeDescriptor));

                DataTypes collectionDataType = collectionTypeDescriptor[0];
                DataTypes elementDataType = collectionDataType & ~(DataTypes.CollectionTypes | DataTypes.Enum);

                // array
                if ((collectionDataType & DataTypes.CollectionTypes) == DataTypes.Array)
                {
                    Array array = (Array)obj;
                    // 1. Dimensions
                    for (int i = 0; i < array.Rank; i++)
                    {
                        Write7BitInt(bw, array.GetLowerBound(i));
                        Write7BitInt(bw, array.GetLength(i));
                    }

                    // 2. Write elements
                    Type elementType = array.GetType().GetElementType();
                    // 2.a.) Primitive array
                    // ReSharper disable once PossibleNullReferenceException - it is an array
                    if (elementType.IsPrimitive)
                    {
                        if (!(array is byte[] rawData))
                        {
                            rawData = new byte[Buffer.ByteLength(array)];
                            Buffer.BlockCopy(array, 0, rawData, 0, rawData.Length);
                        }

                        bw.Write(rawData);
                        return;
                    }

                    // 2.b.) Complex array
                    collectionTypeDescriptor.RemoveAt(0);
                    WriteCollectionElements(bw, array, collectionTypeDescriptor, elementDataType, elementType);
                    return;
                }

                // other collections
                CollectionSerializationInfo serInfo = serializationInfo[collectionDataType & DataTypes.CollectionTypes];
                var enumerable = obj as IEnumerable;
                IEnumerable collection = enumerable ?? new object[] { obj };
                // as object[] for DictionaryEntry and KeyValuePair

                // 1. Write specific properties
                serInfo.WriteSpecificProperties(bw, collection, this);

                // 2. Stack: reversing elements
                if (serInfo.ReverseElements)
                    collection = collection.Cast<object>().Reverse();

                // 3. Write elements
                // 3.a.) generic collection with single argument
                if (serInfo.IsGenericCollection)
                {
                    Type elementType = collection.GetType().GetGenericArguments()[0];
                    collectionTypeDescriptor.RemoveAt(0);
                    WriteCollectionElements(bw, collection, collectionTypeDescriptor, elementDataType, elementType);
                    return;
                }

                // 3.b.) generic dictionary
                if (serInfo.IsGenericDictionary)
                {
                    Type[] argTypes = (enumerable ?? ((object[])collection)[0]).GetType().GetGenericArguments();
                    Type keyType = argTypes[0];
                    Type valueType = argTypes[1];

                    IList<DataTypes> valueCollectionDataTypes = GetDictionaryValueTypes(collectionTypeDescriptor);
                    collectionTypeDescriptor.RemoveAt(0);
                    DataTypes valueDataType = DataTypes.Null;
                    if ((valueCollectionDataTypes[0] & DataTypes.CollectionTypes) == DataTypes.Null)
                        valueDataType = valueCollectionDataTypes[0] & ~DataTypes.Enum;
                    WriteDictionaryElements(bw, collection, collectionTypeDescriptor, elementDataType, valueCollectionDataTypes, valueDataType, keyType, valueType);
                    return;
                }

                // 3.c.) non-generic collection
                if (serInfo.IsNonGenericCollection)
                {
                    WriteCollectionElements(bw, collection, null, elementDataType, null);
                    return;
                }

                // 3.d.) non-generic dictionary
                if (serInfo.IsNonGenericDictionary)
                {
                    DataTypes valueDataType = GetDictionaryValueTypes(collectionTypeDescriptor)[0];
                    WriteDictionaryElements(bw, collection, null, elementDataType, null, valueDataType, null, null);
                    return;
                }

                // should never occur, throwing internal error without resource
                throw new InvalidOperationException("A supported collection expected here but other type found: " + collection.GetType());
            }

            [SecurityCritical]
            private void WriteCollectionElements(BinaryWriter bw, IEnumerable collection, IList<DataTypes> elementCollectionDataTypes, DataTypes elementDataType, Type collectionElementType)
            {
                foreach (object element in collection)
                    WriteElement(bw, element, elementCollectionDataTypes, elementDataType, collectionElementType);
            }

            [SecurityCritical]
            private void WriteDictionaryElements(BinaryWriter bw, IEnumerable collection, IList<DataTypes> keyCollectionDataTypes, DataTypes keyDataType,
                IList<DataTypes> valueCollectionDataTypes, DataTypes valueDataType, Type collectionKeyType, Type collectionValueType)
            {
                if (collection is IDictionary dictionary)
                {
                    foreach (DictionaryEntry element in dictionary)
                    {
                        WriteElement(bw, element.Key, keyCollectionDataTypes, keyDataType, collectionKeyType);
                        WriteElement(bw, element.Value, valueCollectionDataTypes, valueDataType, collectionValueType);
                    }

                    return;
                }

                // Single KeyValuePair only: cannot be cast to a non-generic dictionary, Key and Value properties must be accessed by name
                foreach (object element in collection)
                {
                    WriteElement(bw, Accessors.GetPropertyValue(element, nameof(KeyValuePair<_, _>.Key)), keyCollectionDataTypes, keyDataType, collectionKeyType);
                    WriteElement(bw, Accessors.GetPropertyValue(element, nameof(KeyValuePair<_, _>.Value)), valueCollectionDataTypes, valueDataType, collectionValueType);
                }
            }

            /// <summary>
            /// Writes a collection element
            /// </summary>
            /// <param name="bw">Binary writer</param>
            /// <param name="element">A collection element instance (can be null)</param>
            /// <param name="elementCollectionDataTypes">Data types of embedded elements. Needed in case of arrays and generic collections where embedded types are handled.</param>
            /// <param name="elementDataType">A base data type that is valid for all elements in the collection. <see cref="DataTypes.Null"/> means that element is a nested collection.</param>
            /// <param name="collectionElementType">Needed if <paramref name="elementDataType"/> is <see cref="DataTypes.BinarySerializable"/> or <see cref="DataTypes.RecursiveObjectGraph"/>.
            /// Contains the actual generic type parameter or array base type from which <see cref="IBinarySerializable"/> or the type of the recursively serialized object is assignable.</param>
            [SecurityCritical]
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Simple switch with many cases")]
            private void WriteElement(BinaryWriter bw, object element, IEnumerable<DataTypes> elementCollectionDataTypes, DataTypes elementDataType, Type collectionElementType)
            {
                switch (elementDataType)
                {
                    case DataTypes.Null:
                        // Null element type means that element is a nested collection type: recursion.
                        // Writing id except for value types (KeyValuePair, DictionaryEntry) - for nullables IsNotNull was written in default
                        if (!collectionElementType.IsValueType || collectionElementType.IsNullable())
                        {
                            if (WriteId(bw, element))
                                break;
                            Debug.Assert(element != null, "When element is null, WriteId should return true");
                        }

                        // creating a new copy for this call be cause the processed elements will be consumed
                        WriteCollection(bw, new CircularList<DataTypes>(elementCollectionDataTypes), element);
                        break;
                    case DataTypes.Bool:
                        bw.Write((bool)element);
                        break;
                    case DataTypes.Int8:
                        bw.Write((sbyte)element);
                        break;
                    case DataTypes.UInt8:
                        bw.Write((byte)element);
                        break;
                    case DataTypes.Int16:
                        bw.Write((short)element);
                        break;
                    case DataTypes.UInt16:
                        bw.Write((ushort)element);
                        break;
                    case DataTypes.Int32:
                        bw.Write((int)element);
                        break;
                    case DataTypes.UInt32:
                        bw.Write((uint)element);
                        break;
                    case DataTypes.Int64:
                        bw.Write((long)element);
                        break;
                    case DataTypes.UInt64:
                        bw.Write((ulong)element);
                        break;
                    case DataTypes.Char:
                        bw.Write((ushort)(char)element);
                        break;
                    case DataTypes.String:
                        if (WriteId(bw, element))
                            break;
                        Debug.Assert(element != null, "When element is null, WriteId should return true");
                        bw.Write((string)element);
                        break;
                    case DataTypes.Single:
                        bw.Write((float)element);
                        break;
                    case DataTypes.Double:
                        bw.Write((double)element);
                        break;
                    case DataTypes.Decimal:
                        bw.Write((decimal)element);
                        break;
                    case DataTypes.DateTime:
                        WriteDateTime(bw, (DateTime)element);
                        break;
                    case DataTypes.DBNull:
                        // as a collection element DBNull can be also a null reference, hence writing the id.
                        WriteId(bw, element);
                        break;
                    case DataTypes.IntPtr:
                        bw.Write(((IntPtr)element).ToInt64());
                        break;
                    case DataTypes.UIntPtr:
                        bw.Write(((UIntPtr)element).ToUInt64());
                        break;
                    case DataTypes.Version:
                        if (WriteId(bw, element))
                            break;
                        WriteVersion(bw, (Version)element);
                        break;
                    case DataTypes.Guid:
                        bw.Write(((Guid)element).ToByteArray());
                        break;
                    case DataTypes.TimeSpan:
                        bw.Write(((TimeSpan)element).Ticks);
                        break;
                    case DataTypes.DateTimeOffset:
                        WriteDateTimeOffset(bw, (DateTimeOffset)element);
                        break;
                    case DataTypes.Uri:
                        if (WriteId(bw, element))
                            break;
                        Debug.Assert(element != null, "When element is null, WriteId should return true");
                        WriteUri(bw, (Uri)element);
                        break;
                    case DataTypes.BitArray:
                        if (WriteId(bw, element))
                            break;
                        Debug.Assert(element != null, "When element is null, WriteId should return true");
                        WriteBitArray(bw, (BitArray)element);
                        break;
                    case DataTypes.BitVector32:
                        bw.Write(((BitVector32)element).Data);
                        break;
                    case DataTypes.BitVector32Section:
                        WriteSection(bw, (BitVector32.Section)element);
                        break;
                    case DataTypes.StringBuilder:
                        if (WriteId(bw, element))
                            break;
                        Debug.Assert(element != null, "When element is null, WriteId should return true");
                        WriteStringBuilder(bw, (StringBuilder)element);
                        break;

                    case DataTypes.BinarySerializable:
                        // 1. instance id for classes or when element is defined as interface in the collection (for nullables IsNotNull was already written in default case)
                        if ((!collectionElementType.IsValueType) && WriteId(bw, element))
                            break;

                        Debug.Assert(element != null, "When element is null, WriteId should return true");
                        Type elementType = element.GetType();

                        // 2. Serialize (1: qualify -> is element type, 2: different type -> store type, 3: serialize)
                        bool qualifyAllElements = collectionElementType.CanBeDerived();
                        bool typeNeeded = qualifyAllElements && elementType != collectionElementType;

                        // is type the same as collection element type
                        if (qualifyAllElements)
                            bw.Write(!typeNeeded);

                        if (typeNeeded)
                            WriteType(bw, elementType);
                        WriteBinarySerializable(bw, (IBinarySerializable)element);
                        break;
                    case DataTypes.RecursiveObjectGraph:
                        // When element types may differ, writing element with data type. This prevents the following errors:
                        // - Writing array element as a graph - new IList<int>[] { new int[] {1} }
                        // - Writing primitive/enum/other supported element as a graph - new ValueType[] { 1, ConsoleColor.Black }
                        // - Writing compressible struct or IBinarySerializable as a graph - new IAnything[] { new BinarySerializable(), new MyStruct() }
                        if (collectionElementType.CanBeDerived())
                        {
                            Write(bw, element, false);
                            break;
                        }

                        // 1. instance id for classes or when element is defined as interface in the collection (for nullables IsNotNull was already written in default case)
                        if (!collectionElementType.IsValueType && WriteId(bw, element))
                            break;

                        Debug.Assert(element != null, "When element is null, WriteId should return true");

                        // 2. Serialize
                        WriteObjectGraph(bw, element, collectionElementType);
                        break;
                    case DataTypes.RawStruct:
                        WriteValueType(bw, element);
                        break;
                    case DataTypes.Object:
                        Write(bw, element, false);
                        break;
                    default:
                        if ((elementDataType & DataTypes.Nullable) == DataTypes.Nullable)
                        {
                            // When boxed, nullable elements are either a null reference or a non-nullable instance in the object.
                            // Here writing IsNotNull instead of id; otherwise, nullables would get an id while non-nullables would not.
                            bw.Write(element != null);
                            if (element != null)
                                WriteElement(bw, element, elementCollectionDataTypes, elementDataType & ~DataTypes.Nullable, collectionElementType);
                            break;
                        }

                        // should never occur, throwing internal error without resource
                        throw new InvalidOperationException("Can not serialize elementType " + DataTypeToString(elementDataType));
                }
            }

            [SecurityCritical]
            private void WriteRecursively(BinaryWriter bw, object data, bool isRoot)
            {
                WriteDataType(bw, DataTypes.RecursiveObjectGraph);

                if (isRoot)
                {
                    // on root level writing the id even if the object is value type because the boxed reference can be shared
                    if (WriteId(bw, data))
                    {
                        Debug.Fail("Id of recursive object should be unknown on top level.");
                        return;
                    }
                }

                WriteObjectGraph(bw, data, null);
            }

            /// <summary>
            /// Serializes an object graph.
            /// </summary>
            /// <param name="bw">Writer</param>
            /// <param name="data">The object to serialize</param>
            /// <param name="collectionElementType">Element type of collection or null if not in collection</param>
            [SecurityCritical]
            private void WriteObjectGraph(BinaryWriter bw, object data, Type collectionElementType)
            {
                // Common order: 1: not in a collection -> store type, 2: serialize
                OnSerializing(data);

                Type type = data.GetType();
                if (TryGetSurrogate(type, out ISerializationSurrogate surrogate, out var _) || (!IgnoreISerializable && data is ISerializable))
                    WriteCustomObjectGraph(bw, data, collectionElementType, surrogate);
                else
                {
                    // type
                    if (collectionElementType == null)
                    {
                        if (RecursiveSerializationAsFallback || type.IsSerializable || (ForceRecursiveSerializationOfSupportedTypes && supportedNonPrimitiveElementTypes.ContainsKey(type)))
                            WriteType(bw, type);
                        else
                            ThrowNotSupported(type);
                    }

                    WriteDefaultObjectGraph(bw, data);
                }

                OnSerialized(data);
            }

            [SecurityCritical]
            private void WriteDefaultObjectGraph(BinaryWriter bw, object data)
            {
                // true for IsDefault object graph
                bw.Write(true);
                Type type = data.GetType();
                Debug.Assert(!type.IsArray, "Array cannot be serialized as object graph");

                // iterating through self and base types
                // ReSharper disable once PossibleNullReferenceException - data is an object in all cases
                for (Type t = type; t != Reflector.ObjectType; t = t.BaseType)
                {
                    // writing fields of current level
                    FieldInfo[] fields = BinarySerializer.GetSerializableFields(t);

                    if (fields.Length != 0 || t == type)
                    {
                        // ReSharper disable once PossibleNullReferenceException - type is never null
                        // writing name of base type
                        if (t != type)
                            bw.Write(t.Name);

                        // writing the fields
                        Write7BitInt(bw, fields.Length);
                        foreach (FieldInfo field in fields)
                        {
                            bw.Write(field.Name);
                            Type fieldType = field.FieldType;
                            object fieldValue = FieldAccessor.GetAccessor(field).Get(data);
                            if (fieldValue != null && fieldType.IsEnum)
                                fieldValue = Convert.ChangeType(fieldValue, Enum.GetUnderlyingType(fieldType), CultureInfo.InvariantCulture);
                            Write(bw, fieldValue, false);
                        }
                    }
                }

                // marking end of hierarchy
                bw.Write(String.Empty);
            }

            [SecurityCritical]
            private void WriteCustomObjectGraph(BinaryWriter bw, object data, Type collectionElementType, ISerializationSurrogate surrogate)
            {
                // Common order: 1: not in a collection -> store type, 2: serialize

                Type type = data.GetType();
                SerializationInfo si = new SerializationInfo(type, new FormatterConverter());

                if (surrogate != null)
                    surrogate.GetObjectData(data, si, Context);
                else
                {
                    if (!RecursiveSerializationAsFallback && !type.IsSerializable)
                        ThrowNotSupported(type);
                    ((ISerializable)data).GetObjectData(si, Context);
                }

                bool typeChanged = si.AssemblyName != type.Assembly.FullName || si.FullTypeName != type.FullName;
                if (typeChanged)
                    type = Type.GetType(si.FullTypeName + ", " + si.AssemblyName);

                // 1. type if needed
                if (collectionElementType == null)
                    WriteType(bw, type);

                // 2. Serialization part.
                // a.) writing false for not default object graph method
                bw.Write(false);

                // b.) Here we can sign if type has changed while element types are the same in a collection (sealed class or struct element type)
                if (collectionElementType != null)
                {
                    bw.Write(typeChanged);
                    if (typeChanged)
                        WriteType(bw, type);
                }

                // c.) writing members
                Write7BitInt(bw, si.MemberCount);
                foreach (SerializationEntry entry in si)
                {
                    // name
                    bw.Write(entry.Name);

                    // value
                    Write(bw, entry.Value, false);

                    // type
                    bool typeMatch = entry.Value == null && entry.ObjectType == Reflector.ObjectType
                        || entry.Value != null && entry.Value.GetType() == entry.ObjectType;
                    bw.Write(typeMatch);
                    if (!typeMatch)
                        WriteType(bw, entry.ObjectType);
                }
            }

            private void OnSerializing(object obj) => ExecuteMethodsOfAttribute(obj, typeof(OnSerializingAttribute));

            private void OnSerialized(object obj) => ExecuteMethodsOfAttribute(obj, typeof(OnSerializedAttribute));

#if NET35
            /// <summary>
            /// Writes a type into the serialization stream
            /// </summary>
            /// <remarks>
            /// Assembly indices:
            /// 0..count - 1: known or already dumped assembly
            /// count: assembly is omitted
            /// count + 1: new assembly (name is to be stored)
            /// count + 2: natively supported type, assembly is not needed (retrieved from DataType)
            ///
            /// Type indices:
            /// 0..count - 1: known or already dumped type
            /// count + 1: new type (name is to be stored)
            /// </remarks>
            private void WriteType(BinaryWriter bw, Type type)
            {
                if (TryWritePureDataType(bw, type))
                    return;

                bool isGeneric;
                Type typeDef;

                // initializing asm cache if needed, determining asm index
                if (assemblyIndexCache == null)
                {
                    assemblyIndexCache = new Dictionary<Assembly, int>(KnownAssemblies.Length + 1);
                    KnownAssemblies.ForEach(a => assemblyIndexCache.Add(a, assemblyIndexCache.Count));
                }

                if (!assemblyIndexCache.TryGetValue(type.Assembly, out int index))
                    index = -1;

                // initializing type cache if needed
                if (typeIndexCache == null)
                {
                    typeIndexCache = new Dictionary<Type, int>(Math.Max(4, KnownTypes.Length + 1));
                    KnownTypes.ForEach(t => typeIndexCache.Add(t, typeIndexCache.Count));
                }

                // new assembly
                if (index == -1)
                {
                    // storing assembly and type name together and return
                    if (OmitAssemblyQualifiedNames)
                    {
                        // count: omitting assembly
                        Write7BitInt(bw, AssemblyIndexCacheCount);
                    }
                    else
                    {
                        int indexCacheCount = AssemblyIndexCacheCount;

                        // count + 1: new assembly
                        Write7BitInt(bw, indexCacheCount + 1);

                        // asm
                        bw.Write(type.Assembly.FullName);
                        assemblyIndexCache.Add(type.Assembly, indexCacheCount);

                        indexCacheCount = TypeIndexCacheCount;

                        // type: type is unknown here for sure so encoding without looking in cache
                        isGeneric = type.IsGenericType;
                        typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                        // ReSharper disable once AssignNullToNotNullAttribute
                        bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                        if (isGeneric)
                        {
                            typeIndexCache.Add(typeDef, indexCacheCount);
                            WriteGenericType(bw, type);
                        }

                        // when generic, the constructed type is added again (property value must be re-evaluated)
                        typeIndexCache.Add(type, TypeIndexCacheCount);
                        return;
                    }
                }
                // known assembly
                else
                    Write7BitInt(bw, index);

                // known type
                if (typeIndexCache.TryGetValue(type, out index))
                {
                    Write7BitInt(bw, index);
                    return;
                }

                int typeIndexCacheCount = TypeIndexCacheCount;

                // generic type definition is already known but the constructed type is not yet
                isGeneric = type.IsGenericType;
                typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                // ReSharper disable AssignNullToNotNullAttribute
                if (isGeneric && typeIndexCache.TryGetValue(typeDef, out index))
                {
                    Write7BitInt(bw, index);
                    WriteGenericType(bw, type);

                    // caching the constructed type (property value must be re-evaluated)
                    typeIndexCache.Add(type, TypeIndexCacheCount);
                    return;
                }
                // ReSharper restore AssignNullToNotNullAttribute

                // type is not known at all (count + 1: new type)
                Write7BitInt(bw, typeIndexCacheCount + 1);

                // ReSharper disable once AssignNullToNotNullAttribute
                bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                if (isGeneric)
                {
                    typeIndexCache.Add(typeDef, typeIndexCacheCount);
                    WriteGenericType(bw, type);
                }

                // when generic, the constructed type is added again (property value must be re-evaluated)
                typeIndexCache.Add(type, TypeIndexCacheCount);
            }

#else

            /// <summary>
            /// Writes a type into the serialization stream
            /// </summary>
            /// <remarks>
            /// Assembly indices:
            /// 0..count - 1: known or already dumped assembly
            /// count: assembly is omitted
            /// count + 1: new assembly (name is to be stored)
            /// count + 2: natively supported type, assembly is not needed (retrieved from DataType)
            ///
            /// Type indices:
            /// 0..count - 1: known or already dumped type
            /// count + 1: new type (name is to be stored)
            /// </remarks>
            [SecurityCritical]
            private void WriteType(BinaryWriter bw, Type type)
            {
                #region Private Methods to reduce complexity

                int GetAssemblyIndex(ref WriteTypeContext ctx)
                {
                    int index;

                    // initializing asm caches if needed, determining asm index
                    if (ctx.BinderAsmName != null)
                    {
                        // assembly by binder
                        if (assemblyNameIndexCache == null)
                            assemblyNameIndexCache = new Dictionary<string, int>(1);
                        if (!assemblyNameIndexCache.TryGetValue(ctx.BinderAsmName, out index))
                            index = -1;
                    }
                    else
                    {
                        // assembly by type
                        if (assemblyIndexCache == null)
                        {
                            assemblyIndexCache = new Dictionary<Assembly, int>(KnownAssemblies.Length + 1);
                            KnownAssemblies.ForEach(a => assemblyIndexCache.Add(a, assemblyIndexCache.Count));
                        }

                        if (!assemblyIndexCache.TryGetValue(ctx.Type.Assembly, out index))
                            index = -1;
                    }

                    // initializing type caches if needed
                    if (ctx.BinderTypeName != null)
                    {
                        if (typeNameIndexCache == null)
                            typeNameIndexCache = new Dictionary<string, int>(1);
                    }
                    else if (typeIndexCache == null)
                    {
                        typeIndexCache = new Dictionary<Type, int>(Math.Max(4, KnownTypes.Length + 1));
                        KnownTypes.ForEach(t => typeIndexCache.Add(t, typeIndexCache.Count));
                    }

                    return index;
                }

                #endregion

                var context = new WriteTypeContext { Type = type };
                Binder?.BindToName(type, out context.BinderAsmName, out context.BinderTypeName);

                if (context.BinderTypeName == null && context.BinderAsmName == null && TryWritePureDataType(bw, type))
                    return;

                int asmIndex = GetAssemblyIndex(ref context);

                bool isGeneric;
                Type typeDef;

                // new assembly
                if (asmIndex == -1)
                {
                    // storing assembly and type name together and return
                    if (OmitAssemblyQualifiedNames)
                    {
                        // count: omitting assembly
                        Write7BitInt(bw, AssemblyIndexCacheCount);
                    }
                    else
                    {
                        int indexCacheCount = AssemblyIndexCacheCount;

                        // count + 1: new assembly
                        Write7BitInt(bw, indexCacheCount + 1);

                        // asm by binder
                        if (context.BinderAsmName != null)
                        {
                            bw.Write(context.BinderAsmName);
                            assemblyNameIndexCache.Add(context.BinderAsmName, indexCacheCount);
                        }
                        // asm by itself
                        else
                        {
                            bw.Write(type.Assembly.FullName);
                            assemblyIndexCache.Add(type.Assembly, indexCacheCount);
                        }

                        indexCacheCount = TypeIndexCacheCount;

                        // type by binder: handling conflicts that can be caused by binder, generics are not handled individually
                        if (context.BinderTypeName != null)
                        {
                            bw.Write(context.BinderTypeName);

                            // binder can produce the same type name for different assemblies so prefixing with assembly
                            typeNameIndexCache.Add((context.BinderAsmName ?? type.Assembly.FullName) + ":" + context.BinderTypeName,
                                indexCacheCount);
                            return;
                        }

                        // type by itself: type is unknown here for sure so encoding without looking in cache
                        isGeneric = type.IsGenericType;
                        typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                        // ReSharper disable once AssignNullToNotNullAttribute - see the check above
                        bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                        if (isGeneric)
                        {
                            typeIndexCache.Add(typeDef, indexCacheCount);
                            WriteGenericType(bw, type);
                        }

                        // when generic, the constructed type is added again (property value must be re-evaluated)
                        typeIndexCache.Add(type, TypeIndexCacheCount);
                        return;
                    }
                }
                // known assembly
                else
                    Write7BitInt(bw, asmIndex);

                // known type
                int typeIndex;
                string key = null;
                if (context.BinderTypeName != null)
                {
                    key = (context.BinderAsmName ?? type.Assembly.FullName) + ":" + context.BinderTypeName;
                    if (typeNameIndexCache.TryGetValue(key, out typeIndex))
                    {
                        Write7BitInt(bw, typeIndex);
                        return;
                    }
                }
                else if (typeIndexCache.TryGetValue(type, out typeIndex))
                {
                    Write7BitInt(bw, typeIndex);
                    return;
                }

                int typeIndexCacheCount = TypeIndexCacheCount;

                // new type by binder (generics are not handled in a special way)
                if (context.BinderTypeName != null)
                {
                    // type is not known yet (count + 1: new type)
                    Write7BitInt(bw, typeIndexCacheCount + 1);
                    bw.Write(context.BinderTypeName);
                    typeNameIndexCache.Add(key, typeIndexCacheCount);
                    return;
                }

                // generic type definition is already known but the constructed type is not yet
                isGeneric = type.IsGenericType;
                typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                if (isGeneric && typeIndexCache.TryGetValue(typeDef, out typeIndex))
                {
                    Write7BitInt(bw, typeIndex);
                    WriteGenericType(bw, type);

                    // caching the constructed type (property value must be re-evaluated)
                    typeIndexCache.Add(type, TypeIndexCacheCount);
                    return;
                }

                // type is not known at all (count + 1: new type)
                Write7BitInt(bw, typeIndexCacheCount + 1);

                // ReSharper disable once AssignNullToNotNullAttribute
                bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                if (isGeneric)
                {
                    typeIndexCache.Add(typeDef, typeIndexCacheCount);
                    WriteGenericType(bw, type);
                }

                // when generic, the constructed type is added again (property value must be re-evaluated)
                typeIndexCache.Add(type, TypeIndexCacheCount);
            }
#endif

            /// <summary>
            /// When writing SerializationInfo types on serializing custom object graph it can happen that we want to write the name
            /// of a natively supported type. In that case we write the <see cref="DataTypes"/> for those types.
            /// As we come from <see cref="WriteType"/> we return true only when the type can be written purely by <see cref="DataTypes"/>.
            /// </summary>
            [SecurityCritical]
            private bool TryWritePureDataType(BinaryWriter bw, Type type)
            {
                DataTypes pureElementType = GetSupportedElementType(type, true);
                if (pureElementType != DataTypes.Null)
                {
                    Write7BitInt(bw, AssemblyIndexCacheCount + 2); // count + 2: natively supported type
                    WriteDataType(bw, pureElementType);
                    return true;
                }

                if (!IsSupportedCollection(type))
                    return false;

                IEnumerable<DataTypes> pureCollectionType = EncodeCollectionType(type, true);
                if (pureCollectionType != null)
                {
                    Write7BitInt(bw, AssemblyIndexCacheCount + 2); // count + 2: natively supported type
                    pureCollectionType.ForEach(dt => WriteDataType(bw, dt));

                    // As we encode pure types, no type name will be written here, only array ranks if needed.
                    WriteTypeNamesAndRanks(bw, type, true);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Writes the generic parameters of a type.
            /// </summary>
            [SecurityCritical]
            private void WriteGenericType(BinaryWriter bw, Type type)
            {
                foreach (Type genericArgument in type.GetGenericArguments())
                    WriteType(bw, genericArgument);
            }

            /// <summary>
            /// Writes an ID and returns if it was already known.
            /// </summary>
            private bool WriteId(BinaryWriter bw, object data)
            {
                bool IsComparedByValue(Type type) =>
                    type.IsPrimitive || type.BaseType == Reflector.EnumType || // always instance so can be used than the slower IsEnum
                    type.In(Reflector.StringType, Reflector.DecimalType, Reflector.DateTimeType, Reflector.TimeSpanType, Reflector.DateTimeOffsetType, typeof(Guid));

                // null is always known.
                if (data == null)
                {
                    // actually 7-bit encoded 0
                    bw.Write((byte)0);
                    return true;
                }

                // some dedicated immutable type are compared by value
                if (IsComparedByValue(data.GetType()))
                {
                    if (idCacheByValue == null)
                        idCacheByValue = new Dictionary<object, int>();
                    else
                    {
                        if (idCacheByValue.TryGetValue(data, out int id))
                        {
                            Write7BitInt(bw, id);
                            return true;
                        }
                    }

                    idCacheByValue.Add(data, ++idCounter);
                    Write7BitInt(bw, idCounter);
                    return false;
                }

                // Others are compared by reference. Structs as well, which are boxed into a reference here.
                if (idCacheByRef == null)
                    idCacheByRef = new Dictionary<object, int>(ReferenceEqualityComparer.Comparer);
                else
                {
                    if (idCacheByRef.TryGetValue(data, out int id))
                    {
                        Write7BitInt(bw, id);
                        return true;
                    }
                }

                idCacheByRef.Add(data, ++idCounter);
                Write7BitInt(bw, idCounter);
                return false;
            }

            private void WriteBinarySerializable(BinaryWriter bw, IBinarySerializable instance)
            {
                OnSerializing(instance);
                byte[] rawData = instance.Serialize(Options);
                Write7BitInt(bw, rawData.Length);
                bw.Write(rawData);
                OnSerialized(instance);
            }

            [SecurityCritical]
            private void WriteValueType(BinaryWriter bw, object data)
            {
                OnSerializing(data);
                byte[] rawData = BinarySerializer.SerializeValueType((ValueType)data);
                Write7BitInt(bw, rawData.Length);
                bw.Write(rawData);
                OnSerialized(data);
            }

            #endregion

            #endregion

            #endregion
        }
    }
}
