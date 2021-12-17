#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.SerializationManager.cs
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

#region Used Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
#if !NET35
using System.Numerics;
#endif
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using System.Runtime.Serialization;
using System.Security;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

#region Used Aliases

using ReferenceEqualityComparer = KGySoft.CoreLibraries.ReferenceEqualityComparer;

#endregion

#endregion

#region Suppressions

#if NETCOREAPP3_0
#pragma warning disable CS8605 // Unboxing a possibly null value. - false alarm for iterating through a non-generic dictionary
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type. - dictionary key/value pairs are never null
#endif

#endregion

namespace KGySoft.Serialization.Binary
{
    public sealed partial class BinarySerializationFormatter
    {

        /// <summary>
        /// A manager class that provides that stored types will be built up in the same order both at serialization and deserialization for complex types.
        /// </summary>
        private sealed class SerializationManager : SerializationManagerBase
        {
            #region Constants

            private const int ticksPerMinute = 600_000_000;

            #endregion

            #region Fields

            private StringKeyedDictionary<int>? nameIndexCache;
            private Dictionary<Assembly, int>? assemblyIndexCache;
            private Dictionary<Type, int>? typeIndexCache;
            private Dictionary<Type, (string? AssemblyName, string? TypeName)>? binderCache;
            private StringKeyedDictionary<int>? assemblyNameIndexCache;
            private StringKeyedDictionary<int>? typeNameIndexCache;

            private int idCounter;
            private Dictionary<object, int>? idCacheByValue;
            private Dictionary<object, int>? idCacheByRef;

            #endregion

            #region Properties

            private Dictionary<Assembly, int> AssemblyIndexCache
            {
                get
                {
                    if (assemblyIndexCache == null)
                    {
                        assemblyIndexCache = new Dictionary<Assembly, int>(KnownAssemblies.Length + 1);
                        KnownAssemblies.ForEach(a => assemblyIndexCache.Add(a, assemblyIndexCache.Count));
                    }

                    return assemblyIndexCache;
                }
            }
            private Dictionary<Type, int> TypeIndexCache
            {
                get
                {
                    if (typeIndexCache == null)
                    {
                        typeIndexCache = new Dictionary<Type, int>(KnownTypes.Length + 1);
                        KnownTypes.ForEach(a => typeIndexCache.Add(a, typeIndexCache.Count));
                    }

                    return typeIndexCache;
                }
            }
            private StringKeyedDictionary<int> AssemblyNameIndexCache => assemblyNameIndexCache ??= new StringKeyedDictionary<int>(1);
            private StringKeyedDictionary<int> TypeNameIndexCache => typeNameIndexCache ??= new StringKeyedDictionary<int>(1);
       
            private int AssemblyIndexCacheCount => (assemblyIndexCache?.Count ?? KnownAssemblies.Length) + (assemblyNameIndexCache?.Count ?? 0);
            private int OmitAssemblyIndex => AssemblyIndexCacheCount;
            private int NewAssemblyIndex => AssemblyIndexCacheCount + 1;
            private int TypeIndexCacheCount => (typeIndexCache?.Count ?? KnownTypes.Length) + (typeNameIndexCache?.Count ?? 0);
            private int NewTypeIndex => TypeIndexCacheCount + 1;
            private int EncodedTypeIndex => TypeIndexCacheCount + 2;

            #endregion

            #region Constructors

            internal SerializationManager(StreamingContext context, BinarySerializationOptions options, SerializationBinder? binder, ISurrogateSelector? surrogateSelector) :
                base(context, options, binder, surrogateSelector)
            {
            }

            #endregion

            #region Methods

            #region Static Methods

            private static string GetTypeNameIndexCacheKey(Type type, string? binderAsmName, string? binderTypeName)
                => binderAsmName + ":" + (binderTypeName ?? type.GetName(TypeNameKind.LongName));

            /// <summary>
            /// Retrieves the value type(s) for a dictionary.
            /// </summary>
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple method with many common cases")]
            private static DataTypesEnumerator GetDictionaryValueTypes(DataTypesEnumerator dataTypes)
            {
                // descriptor must refer a generic dictionary type here
                Debug.Assert(dataTypes.Current != DataTypes.Null, "Type description is invalid: not enough data");
                Debug.Assert(IsDictionary(dataTypes.Current), $"Type description is invalid: {GetCollectionDataType(dataTypes.Current)} is not a dictionary type.");

                int skipLevel = 0;
                bool startingDictionaryResolved = false;
                dataTypes = dataTypes.Clone();
                do
                {
                    DataTypes dataType = dataTypes.Current;

                    // we reached the value
                    if (startingDictionaryResolved && skipLevel == 0)
                        break;

                    switch (GetCollectionDataType(dataType))
                    {
                        // No collection type indicated: element type belongs to an already skipped previous collection.
                        case DataTypes.Null:
                            skipLevel -= 1;
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
                        case DataTypes.ConcurrentBag:
                        case DataTypes.ConcurrentQueue:
                        case DataTypes.ConcurrentStack:
                        case DataTypes.ArrayList:
                        case DataTypes.QueueNonGeneric:
                        case DataTypes.StackNonGeneric:
                        case DataTypes.StringCollection:
                            if (IsElementType(dataType))
                                skipLevel -= 1;
                            break;

                        // Dictionary type: Entry point of the loop or skipped nested key collections.
                        // If element type is specified, value type starts on next position.
                        // Otherwise, key is a nested collection and we need to skip it.
                        case DataTypes.Dictionary:
                        case DataTypes.SortedList:
                        case DataTypes.SortedDictionary:
                        case DataTypes.CircularSortedList:
                        case DataTypes.ConcurrentDictionary:
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
                            if (!IsElementType(dataType))
                                skipLevel += 1;
                            startingDictionaryResolved = true;
                            break;
                    }
                } while (dataTypes.MoveNext());

                return dataTypes;
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
                Write7BitInt(bw, sb.MaxCapacity);
                bw.Write(sb.ToString());
            }

            private static void WriteSection(BinaryWriter bw, BitVector32.Section section)
            {
                bw.Write(section.Mask);
                bw.Write(section.Offset);
            }

#if !NET35
            private static void WriteBigInteger(BinaryWriter bw, BigInteger value)
            {
                byte[] bytes = value.ToByteArray();
                Write7BitInt(bw, bytes.Length);
                bw.Write(bytes);
            }
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            private static void WriteIndex(BinaryWriter bw, Index index) => bw.Write(index.IsFromEnd ? ~index.Value : index.Value);

            private static void WriteRange(BinaryWriter bw, Range range)
            {
                WriteIndex(bw, range.Start);
                WriteIndex(bw, range.End);
            }
#endif

#if NET5_0_OR_GREATER
            private static void WriteHalf(BinaryWriter bw, Half value) => bw.Write(Unsafe.As<Half, ushort>(ref value));
#endif

            private static void WriteGenericSpecifier(BinaryWriter bw, Type type)
            {
                if (type.IsGenericTypeDefinition)
                {
                    bw.Write((byte)GenericTypeSpecifier.TypeDefinition);
                    return;
                }

                if (type.IsGenericParameter)
                {
                    Debug.Assert(type.DeclaringMethod == null, "Generic method parameters are handled separately");
                    bw.Write((byte)GenericTypeSpecifier.GenericParameter);
                    Write7BitInt(bw, type.GenericParameterPosition);
                    return;
                }

                if (type.IsGenericType)
                    bw.Write((byte)GenericTypeSpecifier.ConstructedType);
            }

            #endregion

            #region Instance Methods

            #region Internal Methods

            /// <summary>
            /// The entry point of writing an object. Here the type is encoded by DataTypes. The basic philosophy is
            /// that we write type index everywhere else (which will be at least 1 byte longer for supported types for the first time).
            /// (Impure objects are written by index at root level, too.)
            /// </summary>>
            [SecurityCritical]
            internal void WriteRoot(BinaryWriter bw, object? obj)
            {
                // a.) null
                if (obj == null)
                {
                    WriteDataType(bw, DataTypes.Null);
                    return;
                }

                Type type = obj.GetType();
                DataTypes dataType = GetDataType(type);

                // b.) Pure simple types and enums
                if (IsPureSimpleType(dataType) || IsEnum(dataType))
                {
                    WriteSimpleObject(bw, obj, dataType, true);
                    return;
                }

                // c.) Supported collections
                if (IsCollectionType(dataType))
                {
                    WriteRootCollection(bw, obj, dataType);
                    return;
                }

                // d.) Impure types
                WriteImpureObject(bw, obj, dataType, default, true);
            }

            /// <summary>
            /// Writing a child object. Here the type is encoded by index.
            /// <paramref name="knownElementType"/> is the possible element type of a parent collection.
            /// We don't do the same for parent fields because we don't write the fields types at all.
            /// </summary>>
            [SecurityCritical]
            internal void WriteNonRoot(BinaryWriter bw, object? obj, (DataTypesEnumerator? DataTypes, Type? Type) knownElementType = default)
            {
                // If we have an impure known collection element type we mark its attributes.
                // Note: for fields it cannot be used because we don't write the field type anyway.
                if (knownElementType.Type != null && IsImpureType(knownElementType.DataTypes!.CurrentSeparated))
                    MarkAttributes(bw, knownElementType.Type, GetUnderlyingSimpleType(knownElementType.DataTypes!.Current));

                // a.) Existing object or null
                if (knownElementType.Type?.IsValueType != true)
                {
                    if (WriteId(bw, obj))
                        return;
                }

                Type type = obj!.GetType();
                DataTypes dataType = GetDataType(type);

                // Pure simple types and enums
                if (IsPureSimpleType(dataType) || IsEnum(dataType))
                {
                    WriteSimpleObject(bw, obj, dataType, false);
                    return;
                }

                // Supported collections
                if (IsCollectionType(dataType))
                {
                    WriteNonRootCollection(bw, obj, dataType, knownElementType);
                    return;
                }

                // Impure types
                WriteImpureObject(bw, obj, dataType, knownElementType, false);
            }

            #endregion

            #region Private Methods

            private void ThrowNotSupported(Type type) => Throw.NotSupportedException(Res.BinarySerializationNotSupported(type, Options));

            /// <summary>
            /// Gets the <see cref="DataTypes"/> representation of <paramref name="type"/>.
            /// </summary>
            [SecurityCritical]
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
                Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
            private DataTypes GetDataType(Type type)
            {
                #region Local methods to reduce complexity

                bool TryGetKnownDataType(Type t, out DataTypes result)
                {
                    // Primitive type
                    if (primitiveTypes.TryGetValue(t, out result))
                        return true;

                    // Primitive nullable (must be before surrogate-support checks)
                    bool isNullable = t.IsNullable();
                    if (isNullable)
                    {
                        // the Nullable<> definition or open generic types are encoded recursively
                        if (t.IsGenericTypeDefinition || t.ContainsGenericParameters)
                        {
                            result = DataTypes.RecursiveObjectGraph;
                            return true;
                        }

                        result = GetDataType(t.GetGenericArguments()[0]);
                        if (IsElementType(result) && IsPureType(result))
                        {
                            result |= DataTypes.Nullable;
                            return true;
                        }
                    }

                    // Non-primitive types that cannot be serialized recursively
                    if (t.IsArray)
                    {
                        result = DataTypes.Array;
                        return true;
                    }

                    if (t.IsPointer)
                    {
                        result = DataTypes.Pointer;
                        return true;
                    }

                    if (t.IsByRef)
                    {
                        result = DataTypes.ByRef;
                        return true;
                    }

                    // Recursion for any type (except primitives and array)
                    if (ForceRecursiveSerializationOfSupportedTypes || TryUseSurrogateSelectorForAnyType && CanUseSurrogate(t))
                    {
                        result = DataTypes.RecursiveObjectGraph;
                        if (isNullable)
                            result |= DataTypes.Nullable;
                        return true;
                    }

                    // Non-primitive nullable
                    if (isNullable)
                    {
                        // result is now the result of the recursive call
                        switch (result)
                        {
                            case DataTypes.DictionaryEntry:
                                result = DataTypes.DictionaryEntryNullable;
                                return true;
                            case DataTypes.KeyValuePair:
                                result = DataTypes.KeyValuePairNullable;
                                return true;
                            default:
                                result |= DataTypes.Nullable;
                                return true;
                        }
                    }

                    // Natively supported non-primitive type
                    if (supportedNonPrimitiveElementTypes.TryGetValue(t, out result))
                        return true;

                    // enum
                    if (t.IsEnum)
                    {
                        result = DataTypes.Enum | primitiveTypes[Enum.GetUnderlyingType(t)];
                        return true;
                    }

                    // supported collection
                    Type collType = t.IsGenericType ? t.GetGenericTypeDefinition()
                        : t.IsGenericParameter && t.DeclaringMethod == null ? t.DeclaringType!
                        : t;

                    return supportedCollections.TryGetValue(collType, out result);
                }

                DataTypes GetImpureDataType(Type t)
                {
                    // IBinarySerializable implementation
                    if (!IgnoreIBinarySerializable && binarySerializableType.IsAssignableFrom(t))
                        return DataTypes.BinarySerializable;

                    // Any struct if can be serialized
                    if (CompactSerializationOfStructures && !t.IsManaged())
                        return DataTypes.RawStruct;

                    // Recursive serialization
                    if (RecursiveSerializationAsFallback || t.IsInterface || t.IsSerializable || CanUseSurrogate(t))
                        return DataTypes.RecursiveObjectGraph;

                    // Any struct (obsolete but still supported as backward compatibility)
                    if (ForcedSerializationValueTypesAsFallback && t.IsValueType)
                        return DataTypes.RawStruct;

                    // It is alright for a collection element type. If no recursive serialization is allowed it will turn out for the items.
                    return DataTypes.RecursiveObjectGraph;
                }

                #endregion

                // a.) Well-known types or forced recursion
                if (TryGetKnownDataType(type, out DataTypes dataType))
                    return dataType;

                // b.) Non-pure types
                return GetImpureDataType(type);
            }

            [SecurityCritical]
            private void WriteSimpleObject(BinaryWriter bw, object obj, DataTypes dataType, bool isRoot)
            {
                if (IsCompressible(dataType))
                {
                    WriteCompressible(bw, obj, dataType, isRoot);
                    return;
                }

                Type type = obj.GetType();
                if (isRoot)
                {
                    WriteDataType(bw, dataType);
                    if (IsEnum(dataType))
                        WriteType(bw, type, dataType);
                }
                else
                {
                    WriteType(bw, type, dataType);
                    if (IsEnum(dataType))
                        MarkAttributes(bw, type, DataTypes.Enum);
                }

                WritePureObject(bw, obj, GetUnderlyingSimpleType(dataType));
            }

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
                Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
            [SecurityCritical]
            private void WriteCompressible(BinaryWriter bw, object obj, DataTypes dataType, bool isRoot)
            {
                #region Local Methods

                static (int, ulong) GetSizeAndValue(DataTypes dt, object val)
                {
                    switch (GetUnderlyingSimpleType(dt))
                    {
                        case DataTypes.Int16:
                            return (2, (ulong)(short)val);
                        case DataTypes.UInt16:
                            return (2, (ushort)val);
                        case DataTypes.Int32:
                            return (4, (ulong)(int)val);
                        case DataTypes.UInt32:
                            return (4, (uint)val);
                        case DataTypes.Int64:
                            return (8, (ulong)(long)val);
                        case DataTypes.UInt64:
                            return (8, (ulong)val);
                        case DataTypes.Char:
                            return (2, (char)val);
                        case DataTypes.Single:
                            return (4, BitConverter.ToUInt32(BitConverter.GetBytes((float)val), 0));
                        case DataTypes.Double:
                            return (8, (ulong)BitConverter.DoubleToInt64Bits((double)val));
                        case DataTypes.IntPtr:
                            return (8, (ulong)(IntPtr)val);
                        case DataTypes.UIntPtr:
                            return (8, (ulong)(UIntPtr)val);
                        default:
                            Throw.InternalError($"Unexpected compressible type: {dt}");
                            return default;
                    }
                }

                #endregion

                (int size, ulong value) = GetSizeAndValue(dataType, obj);
                bool compress = size == 2 && value < (1UL << 7) // up to 7 bits
                    || size == 4 && value < (1UL << 21) // up to 3*7 bits
                    || size == 8 && value < (1UL << 49); // up to 7*7 bits

                Type type = obj.GetType();
                if (compress)
                    dataType |= DataTypes.Store7BitEncoded;

                // At root level encoding by DataTypes
                if (isRoot)
                {
                    WriteDataType(bw, dataType);

                    // If enum (impure type) or non-root level: encoding by type index
                    if (IsEnum(dataType))
                        WriteType(bw, type, dataType);
                }
                else
                {
                    Type typeToWrite = compress ? compressibleType.GetGenericType(type) : type;
                    WriteType(bw, typeToWrite, dataType);
                    if (IsEnum(dataType))
                        MarkAttributes(bw, type, dataType);
                }

                // storing the value as 7-bit encoded int, which will be shorter
                if (compress)
                {
                    Write7BitLong(bw, value);
                    return;
                }

                switch (size)
                {
                    case 2:
                        bw.Write((ushort)value);
                        return;
                    case 4:
                        bw.Write((uint)value);
                        return;
                    case 8:
                        bw.Write(value);
                        return;
                }
            }

            [SecurityCritical]
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple method with many cases.")]
            private void WritePureObject(BinaryWriter bw, object obj, DataTypes dataType)
            {
                switch (dataType)
                {
                    case DataTypes.Bool:
                        bw.Write((bool)obj);
                        return;
                    case DataTypes.Int8:
                        bw.Write((sbyte)obj);
                        return;
                    case DataTypes.UInt8:
                        bw.Write((byte)obj);
                        return;
                    case DataTypes.Int16:
                        bw.Write((short)obj);
                        return;
                    case DataTypes.UInt16:
                        bw.Write((ushort)obj);
                        return;
                    case DataTypes.Int32:
                        bw.Write((int)obj);
                        return;
                    case DataTypes.UInt32:
                        bw.Write((uint)obj);
                        return;
                    case DataTypes.Int64:
                        bw.Write((long)obj);
                        return;
                    case DataTypes.UInt64:
                        bw.Write((ulong)obj);
                        return;
                    case DataTypes.Single:
                        bw.Write((float)obj);
                        return;
                    case DataTypes.Double:
                        bw.Write((double)obj);
                        return;
                    case DataTypes.Char:
                        bw.Write((ushort)(char)obj);
                        return;
                    case DataTypes.IntPtr:
                        bw.Write(((IntPtr)obj).ToInt64());
                        return;
                    case DataTypes.UIntPtr:
                        bw.Write(((UIntPtr)obj).ToUInt64());
                        return;
                    case DataTypes.String:
                        bw.Write((string)obj);
                        return;
                    case DataTypes.StringBuilder:
                        WriteStringBuilder(bw, (StringBuilder)obj);
                        return;
                    case DataTypes.Uri:
                        WriteUri(bw, (Uri)obj);
                        return;
                    case DataTypes.Decimal:
                        bw.Write((decimal)obj);
                        return;
                    case DataTypes.DateTime:
                        WriteDateTime(bw, (DateTime)obj);
                        return;
                    case DataTypes.TimeSpan:
                        bw.Write(((TimeSpan)obj).Ticks);
                        return;
                    case DataTypes.DateTimeOffset:
                        WriteDateTimeOffset(bw, (DateTimeOffset)obj);
                        return;
                    case DataTypes.Version:
                        WriteVersion(bw, (Version)obj);
                        return;
                    case DataTypes.Guid:
                        bw.Write(((Guid)obj).ToByteArray());
                        return;
                    case DataTypes.BitArray:
                        WriteBitArray(bw, (BitArray)obj);
                        return;
                    case DataTypes.BitVector32:
                        bw.Write(((BitVector32)obj).Data);
                        return;
                    case DataTypes.BitVector32Section:
                        WriteSection(bw, (BitVector32.Section)obj);
                        return;
                    case DataTypes.RuntimeType:
                        // not passing an actual DataType here because so it will be obtained on demand if needed
                        WriteType(bw, (Type)obj, true);
                        return;

                    // these types have no effective data
                    case DataTypes.Void:
                    case DataTypes.DBNull:
                    case DataTypes.Object:
                        return;

#if !NET35
                    case DataTypes.BigInteger:
                        WriteBigInteger(bw, (BigInteger)obj);
                        return;
#endif

#if NETCOREAPP3_0_OR_GREATER
                    case DataTypes.Rune:
                        bw.Write(((Rune)obj).Value);
                        return;
#endif

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    case DataTypes.Index:
                        WriteIndex(bw, (Index)obj);
                        return;
                    case DataTypes.Range:
                        WriteRange(bw, (Range)obj);
                        return;
#endif

#if NET5_0_OR_GREATER
                    case DataTypes.Half:
                        WriteHalf(bw, (Half)obj);
                        return;
#endif

#if NET6_0_OR_GREATER
                    case DataTypes.DateOnly:
                        bw.Write(((DateOnly)obj).DayNumber);
                        return;
                    case DataTypes.TimeOnly:
                        bw.Write(((TimeOnly)obj).Ticks);
                        return;
#endif

                    default:
                        Throw.InternalError($"Unexpected pure type: {dataType}");
                        return;
                }
            }

            [SecurityCritical]
            private void WriteImpureObject(BinaryWriter bw, object obj, DataTypes dataType, (DataTypesEnumerator? DataTypes, Type? Type) knownElementType, bool isRoot)
            {
                if (isRoot)
                    WriteDataType(bw, dataType);

                switch (GetUnderlyingSimpleType(dataType))
                {
                    case DataTypes.BinarySerializable:
                        WriteBinarySerializable(bw, (IBinarySerializable)obj, knownElementType, isRoot);
                        return;
                    case DataTypes.RawStruct:
                        WriteValueType(bw, (ValueType)obj, knownElementType, isRoot);
                        return;
                    case DataTypes.RecursiveObjectGraph:
                        WriteObjectGraph(bw, obj, knownElementType.Type, isRoot);
                        return;

                    // There is no ByRef instance and pointers cannot be cast to objects. These are supported as types only.
                    case DataTypes.Pointer:
                    case DataTypes.ByRef:
                    default:
                        Throw.InternalError($"Unexpected impure type: {dataType}");
                        return;
                }
            }

            [SecurityCritical]
            private void WriteRootCollection(BinaryWriter bw, object data, DataTypes dataType)
            {
                Type type = data.GetType();
                CircularList<DataTypes> collectionType = EncodeDataType(type, dataType);

                // ReSharper disable once ForCanBeConvertedToForeach - performance
                for (var i = 0; i < collectionType.Count; i++)
                    WriteDataType(bw, collectionType[i]);

                var enumerator = new DataTypesEnumerator(collectionType);
                WriteTypeNamesAndRanks(bw, type, enumerator, false);

                if (collectionType.Exists(CanHaveRecursion))
                {
                    AddToTypeCache(type);
                    if (WriteId(bw, data))
                        Throw.InternalError("Id of recursive object should be unknown at root level.");
                }

                // stepping to the fist element again and writing collection
                enumerator.Reset();
                enumerator.MoveToFirst();
                WriteCollection(bw, enumerator, data);
            }

            [SecurityCritical]
            private void WriteNonRootCollection(BinaryWriter bw, object data, DataTypes dataType, (DataTypesEnumerator? DataTypes, Type? Type) knownElementType)
            {
                Type? type = null;
                DataTypes knownElementDataType = (knownElementType.DataTypes?.CurrentSeparated).GetValueOrDefault();
                bool canUseKnown = dataType == knownElementDataType
                    || dataType == DataTypes.DictionaryEntry && knownElementDataType == DataTypes.DictionaryEntryNullable
                    || dataType == DataTypes.KeyValuePair && knownElementDataType == DataTypes.KeyValuePairNullable;

                // omitting type if collection is a struct element of a parent collection
                IList<DataTypes>? collectionType = null;
                if (knownElementType.Type?.IsSealed != true)
                {
                    collectionType = (canUseKnown ? knownElementType.DataTypes?.GetCurrentSegment() : null)
                        ?? EncodeDataType(type = data.GetType(), dataType);
                    WriteType(bw, type ?? data.GetType(), collectionType);
                }

                // Reusing enumerator of parent collection type if possible by creating a lightweight clone at the current position.
                DataTypesEnumerator collectionDataTypes = (canUseKnown ? knownElementType.DataTypes?.Clone() : null)
                    ?? new DataTypesEnumerator(collectionType ?? EncodeDataType(data.GetType(), dataType), true);

                WriteCollection(bw, collectionDataTypes, data);
            }

            /// <summary>
            /// Writes additional info after a [series of] DataType stream needed to completely describe an exact type.
            /// </summary>
            [SecurityCritical]
            private void WriteTypeNamesAndRanks(BinaryWriter bw, Type type, DataTypesEnumerator? enumerator, bool allowOpenTypes)
            {
                if (enumerator == null)
                {
                    DataTypes dataType = GetDataType(type);
                    var encoded = dataType.In(DataTypes.Array, DataTypes.Pointer, DataTypes.ByRef) 
                        ? EncodeDataType(type, dataType) 
                        : (IList<DataTypes>)new[] { dataType };
                    enumerator = new DataTypesEnumerator(encoded);
                }

                while (enumerator.MoveNextExtracted())
                {
                    DataTypes dataType = enumerator.CurrentSeparated;

                    // Impure types: type name
                    if (IsImpureType(dataType))
                    {
                        if (dataType.In(DataTypes.Pointer, DataTypes.ByRef))
                        {
                            type = type.GetElementType()!;
                            continue;
                        }

                        WriteType(bw, Nullable.GetUnderlyingType(type) ?? type, null, allowOpenTypes);
                        Debug.Assert(!enumerator.MoveNextExtracted(), $"Unprocessed element: {dataType}");
                        return;
                    }

                    // Non-abstract array: recursion for element type, then writing rank
                    if (dataType == DataTypes.Array)
                    {
                        Type elementType = type.GetElementType()!;
                        WriteTypeNamesAndRanks(bw, elementType, enumerator, allowOpenTypes);
                        int rank = type.IsZeroBasedArray() ? 0 : type.GetArrayRank();
                        bw.Write((byte)rank);
                        return;
                    }

                    // recursion for generic arguments
                    if (type.IsGenericType)
                    {
                        foreach (Type genericArgument in type.GetGenericArguments())
                            WriteTypeNamesAndRanks(bw, genericArgument, null, allowOpenTypes);
                        return;
                    }

                    Debug.Assert(IsCollectionType(dataType) || !enumerator.MoveNextExtracted(), $"Unprocessed element: {dataType}");
                    return;
                }
            }

            /// <summary>
            /// Encodes the type as a series of <see cref="DataTypes"/> elements.
            /// </summary>
            [SecurityCritical]
            private CircularList<DataTypes> EncodeDataType(Type type, DataTypes dataType)
            {
                Debug.Assert(IsElementType(dataType) || GetCollectionDataType(dataType) == dataType, $"Unexpected compound type: {dataType}");

                // array
                if (dataType == DataTypes.Array)
                    return EncodeArray(type);

                // pointer/ByRef
                if (dataType.In(DataTypes.Pointer, DataTypes.ByRef))
                {
                    Type elementType = type.GetElementType()!;
                    CircularList<DataTypes> result = EncodeDataType(elementType, GetDataType(elementType));
                    result.AddFirst(dataType);
                    return result;
                }

                type = Nullable.GetUnderlyingType(type) ?? type;

                // generic type
                if (IsCollectionType(dataType) && type.IsGenericType || type.ContainsGenericParameters)
                    return EncodeGenericCollection(type, dataType);

                // non-generic types
                switch (dataType)
                {
                    case DataTypes.ArrayList:
                    case DataTypes.QueueNonGeneric:
                    case DataTypes.StackNonGeneric:
                        return new CircularList<DataTypes> { dataType | DataTypes.Object };

                    case DataTypes.Hashtable:
                    case DataTypes.SortedListNonGeneric:
                    case DataTypes.ListDictionary:
                    case DataTypes.HybridDictionary:
                    case DataTypes.OrderedDictionary:
                    case DataTypes.DictionaryEntry:
                    case DataTypes.DictionaryEntryNullable:
                        return new CircularList<DataTypes> { dataType | DataTypes.Object, DataTypes.Object };

                    case DataTypes.StringCollection:
                        return new CircularList<DataTypes> { dataType | DataTypes.String };

                    case DataTypes.StringDictionary:
                        return new CircularList<DataTypes> { dataType | DataTypes.String, DataTypes.String };

                    default:
                        Debug.Assert(IsElementType(dataType), $"Unexpected non-element type: {dataType}");
                        return new CircularList<DataTypes> { dataType };
                }
            }

            [SecurityCritical]
            private CircularList<DataTypes> EncodeArray(Type type)
            {
                Type elementType = type.GetElementType()!;
                if (elementType.IsGenericParameter || elementType.IsGenericTypeDefinition)
                    return new CircularList<DataTypes> { DataTypes.Array | DataTypes.RecursiveObjectGraph };

                DataTypes elementDataType = GetDataType(elementType);

                if (IsElementType(elementDataType))
                {
                    if (elementDataType.In(DataTypes.Pointer, DataTypes.ByRef))
                    {
                        Type subElementType = elementType.GetElementType()!;
                        DataTypes subElementDataType = GetDataType(subElementType);
                        CircularList<DataTypes> nestedTypes = EncodeDataType(subElementType, subElementDataType);
                        nestedTypes.AddFirst(DataTypes.Array | elementDataType);
                        return nestedTypes;
                    }

                    return new CircularList<DataTypes> { DataTypes.Array | elementDataType };
                }

                Debug.Assert(IsCollectionType(elementDataType), $"Not a collection data type: {elementDataType}");
                CircularList<DataTypes> nestedCollection = EncodeDataType(elementType, elementDataType);
                nestedCollection.AddFirst(DataTypes.Array);
                return nestedCollection;
            }

            [SecurityCritical]
            private CircularList<DataTypes> EncodeGenericCollection(Type type, DataTypes collectionType)
            {
                if (type.IsGenericTypeDefinition || type.ContainsGenericParameters)
                    return new CircularList<DataTypes> { IsCollectionType(collectionType) ? collectionType | DataTypes.GenericTypeDefinition : DataTypes.RecursiveObjectGraph };

                Debug.Assert(GetCollectionDataType(collectionType) == collectionType, "Plain collection type expected");

                Type[] args = type.GetGenericArguments();
                Type elementType = args[0];
                DataTypes elementDataType = GetDataType(elementType);

                // generics with 1 argument
                if (args.Length == 1)
                {
                    if (IsElementType(elementDataType))
                        return new CircularList<DataTypes> { collectionType | elementDataType };

                    Debug.Assert(IsCollectionType(elementDataType), $"Not a collection data type: {elementDataType}");
                    CircularList<DataTypes> innerType = EncodeDataType(elementType, elementDataType);
                    innerType.AddFirst(collectionType);
                    return innerType;
                }

                // dictionaries
                Type valueType = args[1];
                DataTypes valueDataType = GetDataType(valueType);

                CircularList<DataTypes> keyTypes;
                CircularList<DataTypes> valueTypes;

                // key
                if (IsElementType(elementDataType))
                    keyTypes = new CircularList<DataTypes> { collectionType | elementDataType };
                else
                {
                    Debug.Assert(IsCollectionType(elementDataType), $"Not a collection data type: {elementDataType}");
                    keyTypes = EncodeDataType(elementType, elementDataType);
                    keyTypes.AddFirst(collectionType);
                }

                // value
                if (IsElementType(valueDataType))
                    valueTypes = new CircularList<DataTypes> { valueDataType };
                else
                {
                    Debug.Assert(IsCollectionType(valueDataType), $"Not a collection data type: {valueDataType}");
                    valueTypes = EncodeDataType(valueType, valueDataType);
                }

                keyTypes.AddRange(valueTypes);
                return keyTypes;
            }

            [SecurityCritical]
            private void WriteCollection(BinaryWriter bw, DataTypesEnumerator collectionDataTypes, object obj)
            {
                Debug.Assert(collectionDataTypes.Current != DataTypes.Null, "Type description is invalid");

                DataTypes dataType = collectionDataTypes.Current;
                DataTypes collectionDataType = GetCollectionDataType(dataType);
                Type type = obj.GetType();
                Type elementType, dictionaryValueType;

                // array
                if (collectionDataType == DataTypes.Array)
                {
                    Array array = (Array)obj;

                    // 1. Dimensions
                    for (int i = 0; i < array.Rank; i++)
                    {
                        if (i != 0 || !type.IsZeroBasedArray())
                            Write7BitInt(bw, array.GetLowerBound(i));
                        Write7BitInt(bw, array.GetLength(i));
                    }

                    // 2. Write elements
                    elementType = type.GetElementType()!;

                    // 2.a.) Primitive array
                    if (elementType.IsPrimitive)
                    {
                        if (array is not byte[] rawData)
                        {
                            rawData = new byte[Buffer.ByteLength(array)];
                            Buffer.BlockCopy(array, 0, rawData, 0, rawData.Length);
                        }

                        bw.Write(rawData);
                        return;
                    }

                    // 2.b.) Complex array
                    if (elementType.IsPointer)
                        Throw.NotSupportedException(Res.SerializationPointerArrayTypeNotSupported(type));
                    collectionDataTypes.MoveNextExtracted();
                    WriteCollectionElements(bw, array, collectionDataTypes, elementType);
                    return;
                }

                // other collections
                CollectionSerializationInfo serInfo = serializationInfo[collectionDataType];
                var enumerable = obj as IEnumerable;
               
                // as object[] for DictionaryEntry and KeyValuePair
                IEnumerable collection = enumerable ?? new[] { obj };
                
                // 1. Write specific properties
                serInfo.WriteSpecificProperties(bw, collection, this);

                // 2. Stack: reversing elements
                if (serInfo.ReverseElements)
                    collection = collection.Cast<object>().Reverse();

                // 3. Write elements
                // 3.a.) generic collection with single argument
                if (serInfo.IsGenericCollection)
                {
                    elementType = type.GetGenericArguments()[0];
                    collectionDataTypes.MoveNextExtracted();
                    WriteCollectionElements(bw, collection, collectionDataTypes, elementType);
                    return;
                }

                // 3.b.) generic dictionary
                if (serInfo.IsGenericDictionary)
                {
                    Type[] argTypes = type.GetGenericArguments();
                    elementType = argTypes[0];
                    dictionaryValueType = argTypes[1];

                    DataTypesEnumerator valueCollectionDataTypes = GetDictionaryValueTypes(collectionDataTypes);
                    collectionDataTypes.MoveNextExtracted();
                    Debug.Assert(valueCollectionDataTypes.Current != DataTypes.Null, "Dictionary type description is invalid");
                    WriteDictionaryElements(bw, collection, collectionDataTypes, valueCollectionDataTypes, elementType, dictionaryValueType);
                    return;
                }

                // 3.c.) non-generic collection
                if (serInfo.IsNonGenericCollection)
                {
                    collectionDataTypes.MoveNextExtracted();
                    elementType = collectionDataTypes.CurrentSeparated == DataTypes.String ? Reflector.StringType : Reflector.ObjectType;
                    WriteCollectionElements(bw, collection, collectionDataTypes, elementType);
                    return;
                }

                // 3.d.) non-generic dictionary
                if (serInfo.IsNonGenericDictionary)
                {
                    DataTypesEnumerator valueCollectionDataTypes = GetDictionaryValueTypes(collectionDataTypes);
                    collectionDataTypes.MoveNextExtracted();
                    elementType = dictionaryValueType = collectionDataTypes.CurrentSeparated == DataTypes.String ? Reflector.StringType : Reflector.ObjectType;
                    WriteDictionaryElements(bw, collection, collectionDataTypes, valueCollectionDataTypes, elementType, dictionaryValueType);
                    return;
                }

                Throw.InternalError("A supported collection expected here but other type found: " + collection.GetType());
            }

            [SecurityCritical]
            private void WriteCollectionElements(BinaryWriter bw, IEnumerable collection, DataTypesEnumerator elementCollectionDataTypes, Type collectionElementType)
            {
                foreach (object? element in collection)
                    WriteElement(bw, element, elementCollectionDataTypes, collectionElementType);
            }

            [SecurityCritical]
            private void WriteDictionaryElements(BinaryWriter bw, IEnumerable collection, DataTypesEnumerator keyCollectionDataTypes,
                DataTypesEnumerator valueCollectionDataTypes, Type collectionKeyType, Type collectionValueType)
            {
                if (collection is IDictionary dictionary)
                {
                    foreach (DictionaryEntry element in dictionary)
                    {
                        WriteElement(bw, element.Key, keyCollectionDataTypes, collectionKeyType);
                        WriteElement(bw, element.Value, valueCollectionDataTypes, collectionValueType);
                    }

                    return;
                }

                // If collection cannot be cast to non-generic IDictionary: Key and Value properties must be accessed by name
                foreach (object element in collection)
                {
                    WriteElement(bw, Accessors.GetPropertyValue(element!, nameof(KeyValuePair<_, _>.Key)), keyCollectionDataTypes, collectionKeyType);
                    WriteElement(bw, Accessors.GetPropertyValue(element!, nameof(KeyValuePair<_, _>.Value)), valueCollectionDataTypes, collectionValueType);
                }
            }

            [SecurityCritical]
            private void WriteElement(BinaryWriter bw, object? element, DataTypesEnumerator elementCollectionDataTypes, Type collectionElementType)
            {
                DataTypes collectionDataType = GetCollectionDataType(elementCollectionDataTypes.Current);

                // Nested collection: recursion
                if (collectionDataType != DataTypes.Null)
                {
                    // Nullable collections: writing if instance is null. Not as an id because nullables should not get an id.
                    if (IsNullable(collectionDataType))
                    {
                        bw.Write(element != null);
                        if (element == null)
                            return;
                    }

                    // full recursion because actual element can have a derived type
                    WriteNonRoot(bw, element, (elementCollectionDataTypes, collectionElementType));
                    return;
                }

                DataTypes elementDataType = GetElementDataType(elementCollectionDataTypes.Current);

                // Nullables: writing an IsNotNull value
                if (IsNullable(elementDataType))
                {
                    // Here writing a boolean value instead of id; otherwise, nullables would get an id while non-nullables would not.
                    bw.Write(element != null);
                    if (element == null)
                        return;
                }

                elementDataType = GetUnderlyingSimpleType(elementDataType);

                // As an element type, object means any type so treating along with impure types
                if (elementDataType == DataTypes.Object || IsImpureTypeButEnum(elementDataType))
                {
                    WriteNonRoot(bw, element, (elementCollectionDataTypes, collectionElementType));
                    return;
                }

                // Pure simple types
                Debug.Assert(IsPureType(elementDataType), $"Pure types are expected here but {DataTypeToString(elementDataType)} found");
                
                // Writing Id for reference types. Nullables were already checked above.
                if (element == null || !element.GetType().IsValueType)
                {
                    if (WriteId(bw, element))
                        return;
                    Debug.Assert(element != null, "When element is null, WriteId should return true");
                }

                WritePureObject(bw, element!, elementDataType);
            }

            [SecurityCritical]
            private void WriteObjectGraph(BinaryWriter bw, object data, Type? knownElementType, bool isRoot)
            {
                Debug.Assert(data is not Array, "Arrays cannot be serialized as an object graph.");

                if (isRoot)
                {
                    // at root level writing the id even if the object is value type because the boxed reference can be shared
                    if (WriteId(bw, data))
                        Throw.InternalError("Id of root level object should be unknown.");
                }

                OnSerializing(data);

                Type type = data.GetType();
                if (TryGetSurrogate(type, out ISerializationSurrogate? surrogate, out var _) || (!IgnoreISerializable && data is ISerializable))
                    WriteCustomObjectGraph(bw, data, surrogate, knownElementType);
                else
                    WriteDefaultObjectGraph(bw, data, knownElementType);

                OnSerialized(data);
            }

            [SecurityCritical]
            private void WriteDefaultObjectGraph(BinaryWriter bw, object data, Type? knownElementType)
            {
                Type type = data.GetType();
                bool writeType = knownElementType == null || !knownElementType.IsSealed;

                Debug.Assert(!type.IsArray, "Array cannot be serialized as object graph");
                if (!(RecursiveSerializationAsFallback || type.IsSerializable || ForceRecursiveSerializationOfSupportedTypes && supportedNonPrimitiveElementTypes.ContainsKey(type)))
                    ThrowNotSupported(type);

                // 1.) Writing type. For recursive objects this is the first occasion we can be sure it can be written because on custom serialization type name can be changed.
                if (writeType)
                    WriteType(bw, type);

                // 2.) false for IsCustom object graph
                MarkCustomSerialized(bw, type, false);

                // 3.) Serializing fields
                for (Type t = type; t != Reflector.ObjectType; t = t.BaseType!)
                {
                    // writing fields of current level
                    FieldInfo[] fields = SerializationHelper.GetSerializableFields(t);

                    if (fields.Length != 0 || t == type)
                    {
                        // writing name of base type
                        if (t != type)
                            WriteName(bw, t.Name);

                        // writing the fields
                        Write7BitInt(bw, fields.Length);
                        foreach (FieldInfo field in fields)
                        {
                            WriteName(bw, field.Name);
                            Type fieldType = field.FieldType;
                            object? fieldValue = field.Get(data);
                            if (fieldValue != null && fieldType.IsEnum)
                                fieldValue = Convert.ChangeType(fieldValue, Enum.GetUnderlyingType(fieldType), CultureInfo.InvariantCulture);
                            WriteNonRoot(bw, fieldValue);
                        }
                    }
                }

                // marking end of hierarchy
                WriteName(bw, String.Empty);
            }

            [SecurityCritical]
            private void WriteCustomObjectGraph(BinaryWriter bw, object data, ISerializationSurrogate? surrogate, Type? knownElementType)
            {
                Type type = data.GetType();
                SerializationInfo si = new SerializationInfo(type, new FormatterConverter());

                // Obtaining data to serialize
                if (surrogate != null)
                    surrogate.GetObjectData(data, si, Context);
                else
                {
                    if (!RecursiveSerializationAsFallback && !type.IsSerializable)
                        ThrowNotSupported(type);
                    ((ISerializable)data).GetObjectData(si, Context);
                }

                Type? typeToWrite = type;
                string explicitAsmName = si.AssemblyName;
                string? explicitTypeName = si.FullTypeName;
#if NET35
                if (explicitAsmName != type.Assembly.FullName || explicitTypeName != type.FullName)
                {
                    // First of all we try to obtain the type if exists
                    typeToWrite = !String.IsNullOrEmpty(explicitAsmName) && !String.IsNullOrEmpty(explicitTypeName)
                        ? Reflector.ResolveType(explicitTypeName + ", " + explicitAsmName, ResolveTypeOptions.None)
                        : null;
                }
#else
                if (si.ObjectType != type)
                    typeToWrite = si.ObjectType.FullName != null ? si.ObjectType : null;
                else if (si.IsAssemblyNameSetExplicit || si.IsFullTypeNameSetExplicit)
                {
                    typeToWrite = !String.IsNullOrEmpty(explicitAsmName) && !String.IsNullOrEmpty(explicitTypeName)
                        ? Reflector.ResolveType(explicitTypeName + ", " + explicitAsmName, ResolveTypeOptions.None)
                        : null;

                    // if the string name could be resolved but it has different name, then going on with string name
                    if (typeToWrite != null && (typeToWrite.Assembly.FullName != si.AssemblyName || typeToWrite.FullName != si.FullTypeName))
                        typeToWrite = null;
                }
#endif

                bool forcedType = knownElementType == null || !knownElementType.IsSealed;
                MemberInfo typeToCache = forcedType
                    ? typeToWrite ?? (MemberInfo)new TypeByString(si.AssemblyName, si.FullTypeName)
                    : type;

                // 1/a.) If type is not forced (eg. known collection element), then the IsCustom is the first to write. On custom
                // serialization we write the type anyway though, because it can be changed by SerializationInfo. Not bothering with
                // writing a bool flag whether type has changed though because for known types just a 7-bit encoded id is written.
                if (!forcedType)
                    MarkCustomSerialized(bw, typeToCache, true);

                // 2.) Writing actual type
                if (typeToWrite != null)
                    // we have a real type: normal case
                    WriteType(bw, typeToWrite, DataTypes.RecursiveObjectGraph);
                else
                    // trick: writing a bound name for our original type
                    // ReSharper disable once ConstantNullCoalescingCondition - wrong, si.FullTypeName can be null, if SetType was called with a type without FullName
                    WriteTypeWithName(bw, type, explicitAsmName, explicitTypeName ?? String.Empty);

                // 1/b.) If type must be written, then IsCustom comes after the type.
                if (forcedType)
                    MarkCustomSerialized(bw, typeToCache, true);

                // 3.) Serialization part.
                Write7BitInt(bw, si.MemberCount);
                foreach (SerializationEntry entry in si)
                {
                    // name
                    WriteName(bw, entry.Name);

                    // value
                    WriteNonRoot(bw, entry.Value);

                    // type (again, we don't bother with isDifferent flags, the type id is 1 byte for the first 127 types)
                    WriteType(bw, entry.ObjectType);
                }
            }

            private void OnSerializing(object obj) => ExecuteMethodsOfAttribute(obj, onSerializingAttribute);

            private void OnSerialized(object obj) => ExecuteMethodsOfAttribute(obj, onSerializedAttribute);

            private void WriteName(BinaryWriter bw, string name)
            {
                var names = nameIndexCache ??= new StringKeyedDictionary<int>();
                if (names.TryGetValue(name, out int id))
                {
                    Write7BitInt(bw, id);
                    return;
                }

                id = names.Count;
                names[name] = id;
                Write7BitInt(bw, id);
                bw.Write(name);
            }

            [SecurityCritical]
            private void WriteType(BinaryWriter bw, Type type, bool allowOpenTypes = false)
                => WriteType(bw, type, null, allowOpenTypes);

            [SecurityCritical]
            private void WriteType(BinaryWriter bw, Type type, DataTypes dataType, bool allowOpenTypes = false)
            {
                // WriteType writes compressed as Compressible<T>
                if (IsCompressed(dataType))
                    dataType = DataTypes.RecursiveObjectGraph;

                var dataTypes = CanBeEncoded(dataType) ? EncodeDataType(type, dataType) : (IList<DataTypes>)new[] { dataType };
                WriteType(bw, type, dataTypes, allowOpenTypes);
            }

            /// <summary>
            /// Writes a type into the serialization stream by using assembly and type index.
            /// If there is no name override from Binder it can use a special index to fallback to <see cref="DataTypes"/> encoding.
            /// <paramref name="allowOpenTypes"/> can be <see langword="true"/> only when a RuntimeType instance is serialized.
            /// </summary>
            [SecurityCritical]
            private void WriteType(BinaryWriter bw, Type type, IList<DataTypes>? encodedDataType, bool allowOpenTypes = false)
            {
                Debug.Assert(allowOpenTypes || !(type.IsGenericTypeDefinition || type.IsGenericParameter),
                    $"Generic type definitions and generic parameters are allowed only when {nameof(allowOpenTypes)} is true.");
                string? boundAsmName = null;
                string? boundTypeName = null;

                // 1.) Checking if type is already known without binder (because maybe it is an encoded supported type).
                int index = GetTypeIndex(type);

                if (index == -1)
                {
                    // 2.) Trying to encode supported types by DataTypes. Binder is not queried for supported types.
                    if (TryWriteTypeByDataType(bw, type, allowOpenTypes, ref encodedDataType))
                    {
                        AddToTypeCache(type);
                        return;
                    }

                    // 3.) Checking if type is already known. Binder is queried for root types only.
                    GetBoundNames(type, out boundAsmName, out boundTypeName);
                    if (boundAsmName != null || boundTypeName != null)
                        index = GetTypeIndex(type, boundAsmName, boundTypeName);
                }

                // The requested type (including constructed ones and parameters) is known
                if (index != -1)
                {
                    Write7BitInt(bw, index);
                    if (allowOpenTypes && type.IsGenericTypeDefinition)
                        WriteGenericSpecifier(bw, type);
                    return;
                }

                // 4.) Special handling for generic method parameters
                if (allowOpenTypes && type.IsGenericParameter && type.DeclaringMethod != null)
                {
                    WriteGenericMethodParameter(bw, type);
                    return;
                }

                // 5.) Writing new type
                WriteNewType(bw, type, allowOpenTypes, boundAsmName, boundTypeName);
            }

            private void GetBoundNames(Type type, out string? boundAsmName, out string? boundTypeName)
            {
                Debug.Assert(!type.HasElementType, $"Arrays, pointers and ByRef types should be handled by {nameof(TryWriteTypeByDataType)}");

                boundAsmName = null;
                boundTypeName = null;

                // Constructed generics, generic parameters and known types and non-root types are never bound
                if (type.IsConstructedGenericType() || TypeIndexCache.ContainsKey(type) || type.IsGenericParameter)
                    return;

                if (Binder == null)
                {
                    boundAsmName = GetForwardedAssemblyName(type, true);
                    return;
                }

                Debug.Assert(type.FullName != null, "A root type is expected here");

                binderCache ??= new Dictionary<Type, (string?, string?)>();
                if (binderCache.TryGetValue(type, out (string? AssemblyName, string? TypeName) result))
                {
                    boundAsmName = result.AssemblyName;
                    boundTypeName = result.TypeName;
                    return;
                }

                if (Binder is ISerializationBinder binder)
                    binder.BindToName(type, out boundAsmName, out boundTypeName);
#if !NET35
                else
                    Binder.BindToName(type, out boundAsmName, out boundTypeName);
#endif

                binderCache.Add(type, (boundAsmName ?? GetForwardedAssemblyName(type, true), boundTypeName));
            }

            private string? GetForwardedAssemblyName(Type type, bool omitIfCoreLibrary)
                => IgnoreTypeForwardedFromAttribute ? null : AssemblyResolver.GetForwardedAssemblyName(type, omitIfCoreLibrary);

            private int GetAssemblyIndex(Type type, ref string? boundAsmName)
            {
                if (OmitAssemblyQualifiedNames)
                    return OmitAssemblyIndex;
                if (boundAsmName == null && !IgnoreTypeForwardedFromAttribute)
                {
                    string? forwardedAsmName = GetForwardedAssemblyName(type, false);
                    if (forwardedAsmName != null && AssemblyResolver.IsCoreLibAssemblyName(forwardedAsmName))
                        return 0;
                    boundAsmName = forwardedAsmName;
                }

                return boundAsmName == null
                    ? AssemblyIndexCache.GetValueOrDefault(type.Assembly, -1)
                    : AssemblyNameIndexCache.GetValueOrDefault(boundAsmName, -1);
            }

            private int GetTypeIndex(Type type, string? boundAsmName = null, string? boundTypeName = null)
                => boundAsmName == null && boundTypeName == null
                    ? TypeIndexCache.GetValueOrDefault(type, -1)
                    : TypeNameIndexCache.GetValueOrDefault(GetTypeNameIndexCacheKey(type, boundAsmName, boundTypeName), -1);

            /// <summary>
            /// Trying to write type completely or partially by pure <see cref="DataTypes"/>.
            /// Returning <see langword="true"/> even for partial success (array, generics) because then the beginning of the type is encoded by DataTypes.
            /// </summary>
            [SecurityCritical]
            private bool TryWriteTypeByDataType(BinaryWriter bw, Type type, bool allowOpenTypes, [AllowNull]ref IList<DataTypes> encodedDataTypes)
            {
                #region Local Methods
                
                bool HandlePointerAndByRef(ref Type t, IList<DataTypes> encodedDt)
                {
                    if (!encodedDt[0].In(DataTypes.Pointer, DataTypes.ByRef))
                        return false;

                    Write7BitInt(bw, EncodedTypeIndex);

                    do
                    {
                        WriteDataType(bw, encodedDt[0]);
                        t = t.GetElementType()!;

                        Debug.Assert(!encodedDt.IsReadOnly, "Non read-only encoded types are expected for pointers and ByRef types");
                        encodedDt.RemoveAt(0);
                    } while (encodedDt[0].In(DataTypes.Pointer, DataTypes.ByRef));

                    return true;
                }
                
                #endregion

                Debug.Assert(allowOpenTypes || (!type.IsGenericTypeDefinition && !type.IsGenericParameter), $"Generic type definitions and generic parameters are allowed only when {nameof(allowOpenTypes)} is true.");

                DataTypes dataType;
                if (encodedDataTypes == null)
                {
                    dataType = GetDataType(type);
                    encodedDataTypes = CanBeEncoded(dataType) ? EncodeDataType(type, dataType) : new[] { dataType };
                }

                dataType = encodedDataTypes[0];
                bool processed = HandlePointerAndByRef(ref type, encodedDataTypes);
                if (processed)
                    dataType = encodedDataTypes[0];
                DataTypes collectionDataType = GetCollectionDataType(dataType);

                // Element types
                if (collectionDataType == DataTypes.Null)
                {
                    DataTypes elementDataType = GetElementDataType(dataType);
                    if (IsImpureType(elementDataType))
                    {
                        // Impure type: will be handled by WriteType
                        if (!processed)
                            return false;

                        // ByRef/pointer of impure type: prefixes were processed, rest is handled by WriteType
                        WriteDataType(bw, elementDataType);
                        WriteType(bw, type, encodedDataTypes, allowOpenTypes);
                        return true;
                    }

                    // Pure element type: we are done
                    if (!processed)
                        Write7BitInt(bw, EncodedTypeIndex);
                    WriteDataType(bw, elementDataType);
                    return true;
                }

                // Collection types below
                if (!processed)
                    Write7BitInt(bw, EncodedTypeIndex);

                bool isGeneric = type.IsGenericType;
                bool isTypeDef = type.IsGenericTypeDefinition;
                bool isGenericParam = type.IsGenericParameter;
                Debug.Assert(!isGenericParam || type.DeclaringMethod == null, "Generic method arguments should be written by WriteNewType");

                // Arrays or non-generic/closed generic collections
                if (!(isTypeDef || isGenericParam || (isGeneric && type.ContainsGenericParameters)))
                {
                    // ReSharper disable once ForCanBeConvertedToForeach - performance
                    for (int i = 0; i < encodedDataTypes.Count; i++)
                        WriteDataType(bw, encodedDataTypes[i]);
                    WriteTypeNamesAndRanks(bw, type, new DataTypesEnumerator(encodedDataTypes), allowOpenTypes);
                    return true;
                }

                // Here we have a supported generic type definition or a constructed generic type with unsupported or impure arguments.
                Debug.Assert(GetUnderlyingSimpleType(dataType) == DataTypes.GenericTypeDefinition, "Generic type definition element type expected");
                WriteDataType(bw, dataType); // note: no multiple DataTypes even for dictionaries!

                // If open types are allowed in current context we write a specifier after the generic type definition
                if (allowOpenTypes)
                {
                    WriteGenericSpecifier(bw, type);
                    if (isTypeDef || isGenericParam)
                        return true;
                }

                // Constructed generic type of the (partially) unsupported or impure arguments:
                // recursion for the arguments and adding the type to the index cache at the end.
                foreach (Type genericArgument in type.GetGenericArguments())
                    WriteType(bw, genericArgument, allowOpenTypes);

                return true;
            }

            private void WriteNewAssembly(BinaryWriter bw, Assembly assembly, string? boundAsmName)
            {
                // by binder or forwarded name
                if (boundAsmName != null)
                {
                    bw.Write(boundAsmName);
                    AssemblyNameIndexCache.Add(boundAsmName, AssemblyIndexCacheCount);
                    return;
                }

                bw.Write(assembly.FullName!);
                AssemblyIndexCache.Add(assembly, AssemblyIndexCacheCount);
            }

            /// <summary>
            /// Writes a new non-pure type if a binder did not handle it. Assembly part is already written.
            /// If open types are allowed a generic type definition is followed by a specifier; otherwise, by type arguments.
            /// </summary>
            [SecurityCritical]
            private void WriteNewType(BinaryWriter bw, Type type, bool allowOpenTypes, string? boundAsmName, string? boundTypeName)
            {
                Debug.Assert(allowOpenTypes || !(type.IsGenericTypeDefinition || type.IsGenericParameter), $"Generic type definitions and generic parameters are allowed only when {nameof(allowOpenTypes)} is true.");
                Type rootType = type.IsConstructedGenericType() ? type.GetGenericTypeDefinition()
                    : type.IsGenericParameter ? type.DeclaringType!
                    : type;
                bool isGeneric = type.IsGenericType;
                bool isTypeDef = type.IsGenericTypeDefinition;
                bool isGenericParam = type.IsGenericParameter;
                Type? typeDef = isGeneric || isGenericParam ? rootType : null;

                // Actualizing bound names if needed
                if (rootType != type)
                    GetBoundNames(rootType, out boundAsmName, out boundTypeName);

                // 1.) Type index
                bool isNewType = false;
                int index;

                // It can happen that the generic type definition is already known.
                if (typeDef != null && (index = GetTypeIndex(typeDef, boundAsmName, boundTypeName)) != -1)
                    Debug.Assert(type != rootType && !isTypeDef, $"If the generic type definition was the requested type and it was known, it should have been written in {nameof(WriteType)}");
                else
                {
                    index = NewTypeIndex;
                    isNewType = true;
                }

                Write7BitInt(bw, index);

                // 2.) New type: Assembly index (and name for new ones)
                if (isNewType)
                {
                    index = GetAssemblyIndex(rootType, ref boundAsmName);

                    // known assembly
                    if (index != -1)
                        Write7BitInt(bw, index);
                    // new assembly: writing also the name
                    else
                    {
                        Write7BitInt(bw, NewAssemblyIndex);
                        WriteNewAssembly(bw, rootType.Assembly, boundAsmName);
                    }

                    // 3.) Root type name of new type
                    bw.Write(boundTypeName ?? rootType.FullName!);
                    AddToTypeCache(rootType, boundAsmName, boundTypeName);

                    // for non generics we are done
                    if (typeDef == null)
                        return;
                }

                // 4.) If open types are allowed in current context we write a specifier after the generic type definition
                if (allowOpenTypes)
                {
                    WriteGenericSpecifier(bw, type);
                    if (isTypeDef) // type definition is already cached
                        return;
                }

                // 5.) Constructed generic type: arguments (it still can contain generic parameters)
                if (isGeneric)
                {
                    Debug.Assert(!isTypeDef, "Type definition is not expected here");
                    foreach (Type genericArgument in type.GetGenericArguments())
                        WriteType(bw, genericArgument, allowOpenTypes);
                }

                // 6.) Adding the original complex type to cache (binder is not used here)
                AddToTypeCache(type);
            }

            /// <summary>
            /// Writes the type using explicit names. Occurs when <see cref="SerializationInfo"/> changes the type to an unknown one.
            /// The Binder is not queried this time because there is no known type to query. Instead, we handle the names as bound type names.
            /// </summary>
            [SecurityCritical]
            private void WriteTypeWithName(BinaryWriter bw, Type origType, string explicitAsmName, string explicitTypeName)
            {
                // 1.) Checking if type is already known as bound name
                int index = GetTypeIndex(origType, explicitAsmName, explicitTypeName);

                // The requested type (including constructed ones and parameters) is known
                if (index != -1)
                {
                    Write7BitInt(bw, index);
                    return;
                }

                Write7BitInt(bw, NewTypeIndex);

                // 2.) Assembly index
                index = GetAssemblyIndex(origType, ref explicitAsmName!);

                // known assembly
                if (index != -1)
                    Write7BitInt(bw, index);
                // new assembly: writing also the name
                else
                {
                    Write7BitInt(bw, NewAssemblyIndex);
                    WriteNewAssembly(bw, origType.Assembly, explicitAsmName);
                }

                // 3.) Type name
                bw.Write(explicitTypeName);
                AddToTypeCache(origType, explicitAsmName, explicitTypeName);
            }

            [SecurityCritical]
            private void WriteGenericMethodParameter(BinaryWriter bw, Type type)
            {
                Debug.Assert(type.IsGenericParameter && type.DeclaringMethod != null, "Generic method argument is expected here");

                // Writing a special placeholder indicating the generic method parameter because the declaring type
                // of generic methods has nothing to do with the parameter so they cannot be encoded the same way as type parameters.
                WriteType(bw, genericMethodDefinitionPlaceholderType);

                // For generic method parameters no specifier is needed because the placeholder type has been written.
                // Instead, writing the declaring type, method signature and parameter index
                Type declaringType = type.DeclaringType!;
                WriteType(bw, declaringType, true);
                bw.Write(type.DeclaringMethod!.ToString()!);
                Write7BitInt(bw, type.GenericParameterPosition);

                AddToTypeCache(type);
            }

            private void AddToTypeCache(Type type, string? storedAsmName = null, string? storedTypeName = null)
            {
                // Even if current binder names are null we must use the string based cache if there is a binder
                // to avoid possibly conflicting type names between the custom and default binding and among binder type names.
                if (storedAsmName != null || storedTypeName != null)
                {
                    TypeNameIndexCache.Add(GetTypeNameIndexCacheKey(type, storedAsmName, storedTypeName), TypeIndexCacheCount);
                    return;
                }

                TypeIndexCache.Add(type, TypeIndexCacheCount);
            }

            /// <summary>
            /// Writes an ID and returns if it was already known.
            /// </summary>
            private bool WriteId(BinaryWriter bw, object? data)
            {
                static bool IsComparedByValue(Type t) =>
                    t.IsPrimitive || t.BaseType == Reflector.EnumType || // always instance so can be used than the slower IsEnum
                    t.In(Reflector.StringType, Reflector.DecimalType, Reflector.DateTimeType, Reflector.TimeSpanType, Reflector.DateTimeOffsetType, Reflector.GuidType);

                // null is always known.
                if (data == null)
                {
                    // actually 7-bit encoded 0
                    bw.Write((byte)0);
                    return true;
                }

                Type type = data.GetType();

                // some dedicated immutable type are compared by value
                if (IsComparedByValue(type))
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

            [SecurityCritical]
            private void WriteBinarySerializable(BinaryWriter bw, IBinarySerializable instance, (DataTypesEnumerator? DataTypes, Type? Type) knownElementType, bool isRoot)
            {
                bool writeType = knownElementType.Type == null || !knownElementType.Type.IsSealed;
                if (writeType)
                {
                    Type type = instance.GetType();
                    WriteType(bw, type, DataTypes.BinarySerializable);
                    if (!isRoot && knownElementType.DataTypes?.Current != DataTypes.BinarySerializable)
                        MarkAttributes(bw, type, DataTypes.BinarySerializable);
                }

                OnSerializing(instance);
                byte[] rawData = instance.Serialize(Options);
                Write7BitInt(bw, rawData.Length);
                bw.Write(rawData);
                OnSerialized(instance);
            }

            [SecurityCritical]
            private void WriteValueType(BinaryWriter bw, ValueType data, (DataTypesEnumerator? DataTypes, Type? Type) knownElementType, bool isRoot)
            {
                bool writeType = knownElementType.Type == null || !knownElementType.Type.IsSealed;
                if (writeType)
                {
                    Type type = data.GetType();
                    WriteType(bw, type, DataTypes.RawStruct);
                    if (!isRoot && knownElementType.DataTypes?.Current != DataTypes.RawStruct)
                        MarkAttributes(bw, type, DataTypes.RawStruct);
                }

                OnSerializing(data);
                byte[] rawData = BinarySerializer.SerializeValueType(data);
                Write7BitInt(bw, rawData.Length);
                bw.Write(rawData);
                OnSerialized(data);
            }

            private void WriteTypeAttributes(BinaryWriter bw, MemberInfo type, TypeAttributes attributes)
            {
                bw.Write((byte)attributes);
                TypeAttributesCache.Add(type, attributes);
            }

            private void MarkCustomSerialized(BinaryWriter bw, MemberInfo type, bool isCustomSerialized)
            {
                if (TypeAttributesCache.ContainsKey(type))
                    return;

                var attr = TypeAttributes.RecursiveObjectGraph;
                if (isCustomSerialized)
                    attr |= TypeAttributes.CustomSerialized;
                if (type is Type t)
                {
                    if (t.IsValueType)
                        attr |= TypeAttributes.ValueType;
                    if (t.IsSealed)
                        attr |= TypeAttributes.Sealed;
                }

                WriteTypeAttributes(bw, type, attr);
            }

            /// <summary>
            /// Marks the type attributes in the stream.
            /// This ensures a successful deserialization even if a type changed its sealed or class/struct attributes.
            /// </summary>
            [SecurityCritical]
            private void MarkAttributes(BinaryWriter bw, Type elementType, DataTypes dataType)
            {
                elementType = Nullable.GetUnderlyingType(elementType) ?? elementType;
                if (TypeAttributesCache.ContainsKey(elementType))
                    return;

                TypeAttributes attr = IsEnum(dataType) ? TypeAttributes.Enum
                    : dataType == DataTypes.RecursiveObjectGraph ? TypeAttributes.RecursiveObjectGraph
                    : dataType == DataTypes.BinarySerializable ? TypeAttributes.BinarySerializable
                    : dataType == DataTypes.RawStruct ? TypeAttributes.RawStruct
                    : Throw.InternalError<TypeAttributes>($"Unexpected DataType: {DataTypeToString(dataType)}");

                if (attr == TypeAttributes.RecursiveObjectGraph)
                {
                    if (!IgnoreISerializable && serializableType.IsAssignableFrom(elementType) || CanUseSurrogate(elementType))
                        attr |= TypeAttributes.CustomSerialized;
                }

                if (elementType.IsValueType)
                    attr |= TypeAttributes.ValueType;
                if (elementType.IsSealed)
                    attr |= TypeAttributes.Sealed;

                WriteTypeAttributes(bw, elementType, attr);
            }

            #endregion

            #endregion

            #endregion
        }
    }
}
