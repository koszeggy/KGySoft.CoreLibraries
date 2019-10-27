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
            #region Constants

            private const int ticksPerMinute = 600_000_000;

            #endregion

            #region Fields

            private Dictionary<Assembly, int> assemblyIndexCache;
            private Dictionary<Type, int> typeIndexCache;
            private Dictionary<Type, (string AssemblyName, string TypeName)> binderCache;
            private Dictionary<string, int> assemblyNameIndexCache;
            private Dictionary<string, int> typeNameIndexCache;

            private int idCounter;
            private Dictionary<object, int> idCacheByValue;
            private Dictionary<object, int> idCacheByRef;

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

            private Dictionary<string, int> AssemblyNameIndexCache => assemblyNameIndexCache ??= new Dictionary<string, int>(1);
            private Dictionary<string, int> TypeNameIndexCache => typeNameIndexCache ??= new Dictionary<string, int>(1);
            private int AssemblyIndexCacheCount => (assemblyIndexCache?.Count ?? KnownAssemblies.Length) + (assemblyNameIndexCache?.Count ?? 0);
            private int OmitAssemblyIndex => AssemblyIndexCacheCount;
            private int NewAssemblyIndex => AssemblyIndexCacheCount + 1;
            private int TypeIndexCacheCount => (typeIndexCache?.Count ?? KnownTypes.Length) + (typeNameIndexCache?.Count ?? 0);
            private int NewTypeIndex => TypeIndexCacheCount + 1;
            private int EncodedTypeIndex => TypeIndexCacheCount + 2;

            #endregion

            #region Constructors

            internal SerializationManager(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector) :
                base(context, options, binder, surrogateSelector)
            {
            }

            #endregion

            #region Methods

            #region Static Methods

            private static string GetTypeNameIndexCacheKey(Type type, string binderAsmName, string binderTypeName)
                => (binderAsmName ?? type.Assembly.FullName) + ":" + (binderTypeName ?? type.GetName(TypeNameKind.LongName));

            /// <summary>
            /// Retrieves the value type(s) for a dictionary.
            /// </summary>
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple method with many common cases")]
            private static IList<DataTypes> GetDictionaryValueTypes(IList<DataTypes> collectionTypeDescriptor)
            {
                // descriptor must refer a generic dictionary type here
                Debug.Assert(collectionTypeDescriptor.Count > 0, "Type description is invalid: not enough data");
                Debug.Assert((collectionTypeDescriptor[0] & DataTypes.Dictionary) != DataTypes.Null, $"Type description is invalid: {GetCollectionDataType(collectionTypeDescriptor[0])} is not a dictionary type.");

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

                    switch (GetCollectionDataType(dataType))
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
                            if (IsElementType(dataType))
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
                            if (!IsElementType(dataType))
                                skipLevel++;
                            startingDictionaryResolved = true;
                            break;
                    }
                }

                return result;
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
            /// that we write type index everywhere else (which will be at least 1 byte longer for the first time).
            /// So if the a natively supported root type occurs in the graph again, it will be cached only for the second time.
            /// (Impure objects are cached at root level, too.)
            /// </summary>>
            [SecurityCritical]
            internal void WriteRoot(BinaryWriter bw, object obj)
            {
                // a.) null
                if (obj == null)
                {
                    WriteDataType(bw, DataTypes.Null);
                    return;
                }

                Type type = obj.GetType();
                DataTypes dataType = GetDataType(type); // here collection and element types are not combined yet

                // b.) Pure simple types and enums
                if (IsPureSimpleType(dataType) || (dataType & DataTypes.Enum) != DataTypes.Null)
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
                WriteImpureRootObject(bw, obj, dataType);
            }

            /// <summary>
            /// Writing a child object.
            /// Here the type is encoded by index.
            /// </summary>>
            [SecurityCritical]
            internal void WriteNonRoot(BinaryWriter bw, object obj, bool writeType = true)
            {
                // a.) Existing object
                if (WriteId(bw, obj, writeType))
                    return;

                Type type = obj.GetType();
                DataTypes dataType = GetDataType(type); // here collection and element types are not combined yet

                // b.) Pure simple types and enums
                if (IsPureSimpleType(dataType) || (dataType & DataTypes.Enum) != DataTypes.Null)
                {
                    WriteSimpleObject(bw, obj, dataType, false);
                    return;
                }

                // c.) Supported collections
                if (IsCollectionType(dataType))
                {
                    WriteNonRootCollection(bw, obj, dataType);
                    return;
                }

                // d.) Impure types
                WriteImpureNonRootObject(bw, obj, dataType);
            }

            #endregion

            #region Private Methods

            private void ThrowNotSupported(Type type) => throw new NotSupportedException(Res.BinarySerializationNotSupported(type, Options));

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
                        : t.IsGenericParameter && t.DeclaringMethod == null ? t.DeclaringType
                        : t;

                    // ReSharper disable once AssignNullToNotNullAttribute
                    if (supportedCollections.TryGetValue(collType, out result))
                        return true;

                    return false;
                }

                DataTypes GetImpureDataType(Type t)
                {
                    // IBinarySerializable implementation
                    if (!IgnoreIBinarySerializable && typeof(IBinarySerializable).IsAssignableFrom(t))
                        return DataTypes.BinarySerializable;

                    // Any struct if can be serialized
                    if (CompactSerializationOfStructures && t.IsValueType && BinarySerializer.CanSerializeValueType(t, false))
                        return DataTypes.RawStruct;

                    // Recursive serialization
                    if (RecursiveSerializationAsFallback || t.IsInterface || t.IsSerializable || CanUseSurrogate(t))
                        return DataTypes.RecursiveObjectGraph;

#pragma warning disable 618, 612
                    // Any struct (obsolete but still supported as backward compatibility)
                    if (ForcedSerializationValueTypesAsFallback && t.IsValueType)
                        return DataTypes.RawStruct;
#pragma warning restore 618, 612

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
                Debug.Assert(obj != null, $"{nameof(obj)} must not be null in {nameof(WriteSimpleObject)}");
                if (IsCompressible(dataType))
                {
                    WriteCompressible(bw, obj, dataType, isRoot);
                    return;
                }

                if (isRoot)
                {
                    WriteDataType(bw, dataType);

                    // Enums are impure so they need an additional type
                    if ((dataType & DataTypes.Enum) != DataTypes.Null)
                        WriteType(bw, obj.GetType());
                }
                else
                    WriteType(bw, obj.GetType());

                WritePureObject(bw, obj, GetUnderlyingSimpleType(dataType));
            }

            [SecurityCritical]
            private void WriteCompressible(BinaryWriter bw, object obj, DataTypes dataType, bool isRoot)
            {
                (int, ulong) GetSizeAndValue()
                {
                    switch (GetUnderlyingSimpleType(dataType))
                    {
                        case DataTypes.Int16:
                            return (2, (ulong)(short)obj);
                        case DataTypes.UInt16:
                            return (2, (ushort)obj);
                        case DataTypes.Int32:
                            return (4, (ulong)(int)obj);
                        case DataTypes.UInt32:
                            return (4, (uint)obj);
                        case DataTypes.Int64:
                            return (8, (ulong)(long)obj);
                        case DataTypes.UInt64:
                            return (8, (ulong)obj);
                        case DataTypes.Char:
                            return (2, (char)obj);
                        case DataTypes.Single:
                            return (4, BitConverter.ToUInt32(BitConverter.GetBytes((float)obj), 0));
                        case DataTypes.Double:
                            return (8, (ulong)BitConverter.DoubleToInt64Bits((double)obj));
                        case DataTypes.IntPtr:
                            return (8, (ulong)(IntPtr)obj);
                        case DataTypes.UIntPtr:
                            return (8, (ulong)(UIntPtr)obj);
                        default:
                            throw new InvalidOperationException(Res.InternalError($"Unexpected compressible type: {dataType}"));
                    }
                }

                (int size, ulong value) = GetSizeAndValue();
                bool compress = size == 2 && value < (1UL << 7) // up to 7 bits
                    || size == 4 && value < (1UL << 21) // up to 3*7 bits
                    || size == 8 && value < (1UL << 49); // up to 7*7 bits

                Type type = obj.GetType();

                // At root level encoding by DataTypes
                if (isRoot)
                {
                    if (compress)
                        dataType |= DataTypes.Store7BitEncoded;
                    WriteDataType(bw, dataType);
                }
                else if (compress)
                    type = typeof(Compressible<>).GetGenericType(type);

                // If enum (impure type) or non-root level: encoding by type index
                if (!isRoot || (dataType & DataTypes.Enum) != DataTypes.Null)
                    WriteType(bw, type);

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
                Debug.Assert(obj != null, $"{nameof(obj)} must not be null in {nameof(WritePureObject)}");

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
                        WriteType(bw, (Type)obj, true);
                        return;

                    // these types have no effective data
                    case DataTypes.Void:
                    case DataTypes.DBNull:
                    case DataTypes.Object:
                        return;

                    default:
                        throw new InvalidOperationException($"Unexpected pure type: {dataType}");
                }
            }

            [SecurityCritical]
            private void WriteImpureRootObject(BinaryWriter bw, object obj, DataTypes dataType)
            {
                WriteDataType(bw, dataType);

                // Writing the type first and then the id (as for collections) would be maybe more reasonable
                // but a surrogate (or a standard ISerializable) can change the type name so we start with the id.
                if (CanContainReferenceToSelf(dataType))
                {
                    // at root level writing the id even if the object is value type because the boxed reference can be shared
                    if (WriteId(bw, obj))
                        throw new InvalidOperationException(Res.InternalError("Id of root level object should be unknown."));
                }

                switch (GetUnderlyingSimpleType(dataType))
                {
                    case DataTypes.BinarySerializable:
                        WriteBinarySerializable(bw, (IBinarySerializable)obj, true, true);
                        return;
                    case DataTypes.RawStruct:
                        WriteValueType(bw, (ValueType)obj, true, true);
                        return;
                    case DataTypes.RecursiveObjectGraph:
                        WriteObjectGraph(bw, obj, true, true);
                        return;

                    // There is no ByRef instance and pointers cannot be cast to objects. These are supported as types only.
                    case DataTypes.Pointer:
                    case DataTypes.ByRef:
                    default:
                        throw new InvalidOperationException($"Unexpected impure type: {dataType}");
                }
            }

            [SecurityCritical]
            private void WriteImpureNonRootObject(BinaryWriter bw, object obj, DataTypes dataType, /*TODO*/ bool writeType = true)
#warning TODO: writeType: based on collection element/field
            {
                switch (GetUnderlyingSimpleType(dataType))
                {
                    case DataTypes.BinarySerializable:
                        WriteBinarySerializable(bw, (IBinarySerializable)obj, writeType, false);
                        return;
                    case DataTypes.RawStruct:
                        WriteValueType(bw, (ValueType)obj, writeType, false);
                        return;
                    case DataTypes.RecursiveObjectGraph:
                        WriteObjectGraph(bw, obj, writeType, false);
                        return;

                    // There is no ByRef instance and pointers cannot be cast to objects. These are supported as types only.
                    case DataTypes.Pointer:
                    case DataTypes.ByRef:
                    default:
                        throw new InvalidOperationException($"Unexpected impure type: {dataType}");
                }
            }

            [SecurityCritical]
            private void WriteRootCollection(BinaryWriter bw, object data, DataTypes dataType)
            {
                static bool CanHaveRecursion(CircularList<DataTypes> dataTypes)
                    => dataTypes.Exists(dt =>
                        (dt & DataTypes.SimpleTypes) == DataTypes.BinarySerializable
                        || (dt & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph
                        || (dt & DataTypes.SimpleTypes) == DataTypes.Object);

                Type type = data.GetType();
                CircularList<DataTypes> collectionType = EncodeDataType(type, dataType);
                collectionType.ForEach(dt => WriteDataType(bw, dt));
                WriteTypeNamesAndRanks(bw, type, dataType, false);

                if (CanHaveRecursion(collectionType))
                {
                    if (WriteId(bw, data))
                        throw new InvalidOperationException(Res.InternalError("Id of recursive object should be unknown at root level."));
                }

                WriteCollection(bw, collectionType, data);
            }

            [SecurityCritical]
            private void WriteNonRootCollection(BinaryWriter bw, object data, DataTypes dataType)
            {
                // id is already written if needed
                Type type = data.GetType();
                WriteType(bw, type);
                CircularList<DataTypes> collectionType = EncodeDataType(type, dataType);
                WriteCollection(bw, collectionType, data);
            }

            /// <summary>
            /// Writes additional info after a [series of] DataType stream needed to completely describe an exact type.
            /// </summary>
            [SecurityCritical]
            private void WriteTypeNamesAndRanks(BinaryWriter bw, Type type, DataTypes dataType, bool allowOpenTypes)
            {
                Debug.Assert(IsElementType(dataType) || GetCollectionDataType(dataType) == dataType, $"Unexpected compound type: {dataType}");

                // Impure types: type name
                if (!IsPureType(dataType))
                {
                    if (dataType.In(DataTypes.Pointer, DataTypes.ByRef))
                    {
                        Type elementType = type.GetElementType();
                        WriteTypeNamesAndRanks(bw, elementType, GetDataType(elementType), allowOpenTypes);
                        return;
                    }

                    if ((dataType & DataTypes.Nullable) != DataTypes.Null)
                        type = Nullable.GetUnderlyingType(type);
                    WriteType(bw, type, allowOpenTypes);
                    return;
                }

                // Non-abstract array: recursion for element type, then writing rank
                if (GetCollectionDataType(dataType) == DataTypes.Array)
                {
                    Type elementType = type.GetElementType();

                    // ReSharper disable once PossibleNullReferenceException - arrays have element types
                    DataTypes elementDataType = elementType.IsGenericTypeDefinition || elementType.IsGenericParameter ? DataTypes.RecursiveObjectGraph : GetDataType(elementType);
                    WriteTypeNamesAndRanks(bw, elementType, elementDataType, allowOpenTypes);
                    int rank = type.IsZeroBasedArray() ? 0 : type.GetArrayRank();
                    bw.Write((byte)rank);
                    return;
                }

                // recursion for generic arguments
                if (type.IsGenericType)
                {
                    foreach (Type genericArgument in type.GetGenericArguments())
                        WriteTypeNamesAndRanks(bw, genericArgument, GetDataType(genericArgument), allowOpenTypes);
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

                Debug.Assert(!type.IsGenericTypeDefinition, $"Generic type definition is not expected in {nameof(EncodeDataType)}");
                type = Nullable.GetUnderlyingType(type) ?? type;

                // generic type
                if (type.IsGenericType)
                    return EncodeGenericCollection(type, dataType);

                // non-generic types
                switch (dataType)
                {
                    case DataTypes.Pointer:
                    case DataTypes.ByRef:
                        Type elementType = type.GetElementType();
                        CircularList<DataTypes> result = EncodeDataType(elementType, GetDataType(elementType));
                        result.AddFirst(dataType);
                        return result;

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
                Type elementType = type.GetElementType();

                // ReSharper disable once PossibleNullReferenceException - arrays have element types
                if (elementType.IsGenericParameter || elementType.IsGenericTypeDefinition)
                    return new CircularList<DataTypes> { DataTypes.Array | DataTypes.RecursiveObjectGraph };

                DataTypes elementDataType = GetDataType(elementType);

                if (IsElementType(elementDataType))
                {
                    if (elementDataType.In(DataTypes.Pointer, DataTypes.ByRef))
                    {
                        Type subElementType = elementType.GetElementType();
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
                Debug.Assert(GetCollectionDataType(collectionType) == collectionType, "Plain collection type expected");
                Debug.Assert(!type.ContainsGenericParameters, $"Constructed open generic types are not expected in {nameof(EncodeGenericCollection)}");

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
            private void WriteCollection(BinaryWriter bw, CircularList<DataTypes> collectionTypeDescriptor, object obj)
            {
                if (collectionTypeDescriptor.Count == 0)
                    throw new ArgumentException(Res.InternalError("Type description is invalid"), nameof(collectionTypeDescriptor));

                DataTypes dataType = collectionTypeDescriptor[0];
                DataTypes elementDataType = GetElementDataType(dataType);
                DataTypes collectionDataType = GetCollectionDataType(dataType);

                // array
                if (collectionDataType == DataTypes.Array)
                {
                    Array array = (Array)obj;
                    Type type = obj.GetType();

                    // 1. Dimensions
                    for (int i = 0; i < array.Rank; i++)
                    {
                        if (i != 0 || !type.IsZeroBasedArray())
                            Write7BitInt(bw, array.GetLowerBound(i));
                        Write7BitInt(bw, array.GetLength(i));
                    }

                    // 2. Write elements
                    Type elementType = type.GetElementType();
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
                    if (elementType.IsPointer)
                        throw new NotSupportedException(Res.SerializationPointerArrayTypeNotSupported(type));
                    collectionTypeDescriptor.RemoveFirst();
                    WriteCollectionElements(bw, array, collectionTypeDescriptor, elementDataType, elementType);
                    return;
                }

                // other collections
                CollectionSerializationInfo serInfo = serializationInfo[collectionDataType];
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
                    collectionTypeDescriptor.RemoveFirst();
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
                    collectionTypeDescriptor.RemoveFirst();
                    DataTypes valueDataType = DataTypes.Null;
                    if (!IsCollectionType(valueCollectionDataTypes[0]))
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

                throw new InvalidOperationException(Res.InternalError("A supported collection expected here but other type found: " + collection.GetType()));
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
            private void WriteElement(BinaryWriter bw, object element, IEnumerable<DataTypes> elementCollectionDataTypes, DataTypes elementDataType, Type collectionElementType)
            {
                // a.) Special cases
                // Null element type means that element is a nested collection type: recursion.
                if (elementDataType == DataTypes.Null)
                {
                    // Writing id except for value types (KeyValuePair, DictionaryEntry)
                    if (!collectionElementType.IsValueType || collectionElementType.IsNullable())
                    {
                        if (WriteId(bw, element))
                            return;
                        Debug.Assert(element != null, "When element is null, WriteId should return true");
                    }

                    // creating a new copy for this call because the processed elements will be consumed
                    WriteCollection(bw, new CircularList<DataTypes>(elementCollectionDataTypes), element);
                    return;
                }

                // As an element type, object means any type
                if (elementDataType == DataTypes.Object)
                {
                    WriteNonRoot(bw, element);
                    return;
                }

                // Nullables: writing an IsNotNull value
                if ((elementDataType & DataTypes.Nullable) != DataTypes.Null)
                {
                    // Here writing a boolean value instead of id; otherwise, nullables would get an id while non-nullables would not.
                    bw.Write(element != null);
                    if (element == null)
                        return;
                }

                elementDataType = GetUnderlyingSimpleType(elementDataType);

                // b.) Pure simple types
                if (IsPureType(elementDataType))
                {
                    // Writing Id for reference types. Nullables were already checked above.
                    if (element == null || !element.GetType().IsValueType)
                    {
                        if (WriteId(bw, element))
                            return;
                        Debug.Assert(element != null, "When element is null, WriteId should return true");
                    }

                    WritePureObject(bw, element, elementDataType);
                    return;
                }

                // c.) Impure types
                WriteImpureElement(bw, element, elementDataType, collectionElementType);
            }

            [SecurityCritical]
            private void WriteImpureElement(BinaryWriter bw, object element, DataTypes elementDataType, Type collectionElementType)
            {
                switch (elementDataType)
                {
                    case DataTypes.BinarySerializable:
                        // 1. instance id for classes or when element is defined as interface in the collection (for nullables IsNotNull was already written)
                        if (!collectionElementType.IsValueType && WriteId(bw, element))
                            return;

                        Debug.Assert(element != null, "When element is null, WriteId should return true");

                        // 2. Serialize
                        bool typeNeeded = collectionElementType.CanBeDerived();
                        WriteBinarySerializable(bw, (IBinarySerializable)element, typeNeeded, false);
                        return;

                    case DataTypes.RecursiveObjectGraph:

                        // When element types may differ, writing element as a completely new object (will have the same size).
                        // This prevents a lot of issues such as forced recursive serialization of primitives.
                        if (collectionElementType.CanBeDerived())
                        {
                            WriteNonRoot(bw, element);
                            return;
                        }

                        // 1. instance id for classes or when element is defined as interface in the collection (for nullables IsNotNull was already written in default case)
                        if (!collectionElementType.IsValueType && WriteId(bw, element))
                            return;

                        Debug.Assert(element != null, "When element is null, WriteId should return true");

                        // 2. Serialize (here element types are always the same due to the first check)
                        WriteObjectGraph(bw, element, false, false);
                        return;

                    case DataTypes.RawStruct:
                        WriteValueType(bw, (ValueType)element, false, false);
                        return;

                    // There is no ByRef instance and pointers cannot be cast to objects. These are supported as types only.
                    case DataTypes.Pointer:
                    case DataTypes.ByRef:
                    default:
                        throw new InvalidOperationException($"Unexpected impure type: {elementDataType}");
                }
            }

            [SecurityCritical]
            private void WriteObjectGraph(BinaryWriter bw, object data, bool writeType, bool isRoot)
            {
                Debug.Assert(!(data is Array), "Arrays cannot be serialized as an object graph.");

                // Common order: 1: not in a collection -> store type, 2: serialize
                OnSerializing(data);

                Type type = data.GetType();
                if (TryGetSurrogate(type, out ISerializationSurrogate surrogate, out var _) || (!IgnoreISerializable && data is ISerializable))
                    WriteCustomObjectGraph(bw, data, surrogate, writeType, isRoot);
                else
                    WriteDefaultObjectGraph(bw, data, writeType, isRoot);

                OnSerialized(data);
            }

            [SecurityCritical]
            private void WriteDefaultObjectGraph(BinaryWriter bw, object data, bool writeType, bool isRoot)
            {
                Type type = data.GetType();
                Debug.Assert(!type.IsArray, "Array cannot be serialized as object graph");
                if (!(RecursiveSerializationAsFallback || type.IsSerializable || ForceRecursiveSerializationOfSupportedTypes && supportedNonPrimitiveElementTypes.ContainsKey(type)))
                    ThrowNotSupported(type);

                // 1.) Writing type. For recursive objects this is the first occasion we can be sure it can be written because on custom serialization type name can be changed.
                if (writeType)
                    WriteType(bw, isRoot ? type : typeof(RecursiveObjectGraph<>).GetGenericType(type));

                // 2.) false for IsCustom object graph
                bw.Write(false);

                // 3.) Serializing fields
                // ReSharper disable once PossibleNullReferenceException - data is an object in all cases
                for (Type t = type; t != Reflector.ObjectType; t = t.BaseType)
                {
                    // writing fields of current level
                    FieldInfo[] fields = SerializationHelper.GetSerializableFields(t);

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
                            object fieldValue = field.Get(data);
                            if (fieldValue != null && fieldType.IsEnum)
                                fieldValue = Convert.ChangeType(fieldValue, Enum.GetUnderlyingType(fieldType), CultureInfo.InvariantCulture);
                            WriteNonRoot(bw, fieldValue);
                        }
                    }
                }

                // marking end of hierarchy
                bw.Write(String.Empty);
            }

            [SecurityCritical]
            private void WriteCustomObjectGraph(BinaryWriter bw, object data, ISerializationSurrogate surrogate, bool forcedType, bool isRoot)
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

                // 1/a.) If type must be written it precedes IsDefault field to follow general type pattern
                if (forcedType)
                    WriteTypeCustom(bw, type, si, isRoot);

                // 2.) true for IsCustom object graph method
                bw.Write(true);

                // 1/b.) If type is not forced (eg. known collection element), then the IsDefault is the first to write. On custom
                // serialization we write the type anyway though because it can be changed by SerializationInfo. Not bothering with
                // bool flags whether type has changed though because for known types just a 7-bit encoded id is written.
                if (!forcedType)
                    WriteTypeCustom(bw, type, si, isRoot);

                // 3.) Serialization part.
                Write7BitInt(bw, si.MemberCount);
                foreach (SerializationEntry entry in si)
                {
                    // name
                    bw.Write(entry.Name);

                    // value
                    WriteNonRoot(bw, entry.Value);

                    // type (again, we don't bother with isDifferent flags, the type id is 1 byte for the first 127 types)
                    WriteType(bw, entry.ObjectType);
                }
            }

            private void WriteTypeCustom(BinaryWriter bw, Type type, SerializationInfo si, bool isRoot)
            {
                Type typeToWrite = type;
#if NET35
                string explicitAsmName = si.AssemblyName;
                string explicitTypeName = si.FullTypeName;
                if (explicitAsmName != type.Assembly.FullName || explicitTypeName != type.FullName)
                {
                    // First of all we try to obtain the type if exists
                    typeToWrite = !String.IsNullOrEmpty(explicitAsmName) && !String.IsNullOrEmpty(explicitTypeName)
                        ? Reflector.ResolveType(explicitAsmName + "," + explicitTypeName, ResolveTypeOptions.None)
                        : null;
                }
#else
#error Implement this
                    type = Type.GetType(si.FullTypeName + ", " + si.AssemblyName);
#endif

                // writing type normally
                if (typeToWrite != null)
                {
                    WriteType(bw, isRoot ? typeToWrite : typeof(RecursiveObjectGraph<>).GetGenericType(typeToWrite));
                    return;
                }

                // writing an unknown type by name
                if (!isRoot)
                {
                    // trick: writing as a closed type before the actual type so on read it will create a RecursiveObjectGraph<explicitTypeName>
                    WriteType(bw, typeof(RecursiveObjectGraph<>));
                }

                WriteTypeByName(bw, type, explicitAsmName ?? String.Empty, explicitTypeName ?? String.Empty);
            }

            private void OnSerializing(object obj) => ExecuteMethodsOfAttribute(obj, typeof(OnSerializingAttribute));

            private void OnSerialized(object obj) => ExecuteMethodsOfAttribute(obj, typeof(OnSerializedAttribute));

            /// <summary>
            /// Writes a type into the serialization stream by using assembly and type index.
            /// If there is no name override from Binder it can use a special index to fallback to <see cref="DataTypes"/> encoding.
            /// <paramref name="allowOpenTypes"/> can be <see langword="true"/> only when a RuntimeType instance is serialized.
            /// </summary>
            [SecurityCritical]
            private void WriteType(BinaryWriter bw, Type type, bool allowOpenTypes = false)
            {
                Debug.Assert(allowOpenTypes || !(type.IsGenericTypeDefinition || type.IsGenericParameter) || type == typeof(RecursiveObjectGraph<>),
                    $"Generic type definitions and generic parameters are allowed only when {nameof(allowOpenTypes)} is true.");
                string binderAsmName = null;
                string binderTypeName = null;

                // 1.) Checking if type is already known without binder (because maybe it is an encoded supported type).
                int index = GetTypeIndex(type);

                if (index == -1)
                {
                    // 2.) Trying to encode supported types by DataTypes. Binder is not queried for supported types.
                    if (TryWriteTypeByDataType(bw, type, allowOpenTypes))
                    {
                        AddToTypeCache(type);
                        return;
                    }

                    // 3.) Checking if type is already known. Binder is queried for root types only.
                    if (Binder != null)
                    {
                        GetBoundNames(type, out binderAsmName, out binderTypeName);
                        index = GetTypeIndex(type, binderAsmName, binderTypeName);
                    }
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
                WriteNewType(bw, type, allowOpenTypes, binderAsmName, binderTypeName);
            }

            private void GetBoundNames(Type type, out string binderAsmName, out string binderTypeName)
            {
                Debug.Assert(!type.HasElementType, $"Arrays, pointers and ByRef types should be handled by {nameof(TryWriteTypeByDataType)}");

                binderAsmName = null;
                binderTypeName = null;

                // Constructed generics, generic parameters and known types and non-root types are never bound
                if (Binder == null || type.IsConstructedGenericType() || TypeIndexCache.ContainsKey(type) || type.IsGenericParameter)
                    return;

                Debug.Assert(type.FullName != null, "A root type is expected here");

                if (binderCache == null)
                    binderCache = new Dictionary<Type, (string, string)>();

                if (binderCache.TryGetValue(type, out (string AssemblyName, string TypeName) result))
                {
                    binderAsmName = result.AssemblyName;
                    binderTypeName = result.TypeName;
                    return;
                }

                if (Binder is ISerializationBinder binder)
                    binder.BindToName(type, out binderAsmName, out binderTypeName);
#if !NET35
                else
                    Binder.BindToName(type, out binderAsmName, out binderTypeName);
#endif

                binderCache.Add(type, (binderAsmName, binderTypeName));
            }

            private int GetAssemblyIndex(Type type, string binderAsmName)
                => OmitAssemblyQualifiedNames
                    ? OmitAssemblyIndex
                    : binderAsmName == null
                        ? AssemblyIndexCache.GetValueOrDefault(type.Assembly, -1)
                        : AssemblyNameIndexCache.GetValueOrDefault(binderAsmName, -1);

            private int GetTypeIndex(Type type, string boundAsmName = null, string boundTypeName = null)
                => boundAsmName == null && boundTypeName == null
                    ? TypeIndexCache.GetValueOrDefault(type, -1)
                    // even if we have a bound name we look for the type in the unbound cache first so we can avoid storing the known types as new ones
                    : TypeIndexCache.GetValueOrDefault(type, () => TypeNameIndexCache.GetValueOrDefault(GetTypeNameIndexCacheKey(type, boundAsmName, boundTypeName), -1));

            /// <summary>
            /// Trying to write type completely or partially by pure <see cref="DataTypes"/>.
            /// Returning <see langword="true"/> even for partial success (array, generics) because then the beginning of the type is encoded by DataTypes.
            /// </summary>
            [SecurityCritical]
            private bool TryWriteTypeByDataType(BinaryWriter bw, Type type, bool allowOpenTypes)
            {
                bool HandlePointerAndByRef(ref Type t, ref DataTypes dt)
                {
                    if (!dt.In(DataTypes.Pointer, DataTypes.ByRef))
                        return false;

                    Write7BitInt(bw, EncodedTypeIndex);

                    do
                    {
                        WriteDataType(bw, dt);

                        // ReSharper disable once PossibleNullReferenceException - Pointers and ByRef types have element type
                        t = t.GetElementType();
                        dt = GetDataType(t);
                    } while (dt.In(DataTypes.Pointer, DataTypes.ByRef));

                    return true;
                }

                Debug.Assert(allowOpenTypes || (!type.IsGenericTypeDefinition && !type.IsGenericParameter), $"Generic type definitions and generic parameters are allowed only when {nameof(allowOpenTypes)} is true.");
                DataTypes dataType = GetDataType(type);

                bool indexWritten = HandlePointerAndByRef(ref type, ref dataType);
                if (IsElementType(dataType))
                {
                    if (!IsPureType(dataType))
                    {
                        if (!indexWritten)
                            return false;

                        WriteDataType(bw, dataType);
                        WriteType(bw, type, allowOpenTypes);
                        return true;
                    }

                    if (!indexWritten)
                        Write7BitInt(bw, EncodedTypeIndex);

                    WriteDataType(bw, dataType);
                    return true;
                }

                if (!indexWritten)
                    Write7BitInt(bw, EncodedTypeIndex);

                Debug.Assert(IsCollectionType(dataType), $"Not a collection data type: {dataType}");

                bool isGeneric = type.IsGenericType;
                bool isTypeDef = type.IsGenericTypeDefinition;
                bool isGenericParam = type.IsGenericParameter;
                Debug.Assert(!isGenericParam || type.DeclaringMethod == null, "Generics method arguments should be written by WriteNewType");

                Type typeDef = isTypeDef ? type
                    : isGeneric ? type.GetGenericTypeDefinition()
                    : isGenericParam ? type.DeclaringType
                    : null;

                // Arrays or non-generic/closed generic collections
                if (!(isTypeDef || isGenericParam || (isGeneric && type.ContainsGenericParameters)))
                {
                    CircularList<DataTypes> encodedCollectionType = EncodeDataType(type, dataType);
                    encodedCollectionType.ForEach(dt => WriteDataType(bw, dt));
                    WriteTypeNamesAndRanks(bw, type, dataType, allowOpenTypes);
                    return true;
                }

                Debug.Assert(typeDef != null, "Generics are expected at this point");

                // Here we have a supported generic type definition or a constructed generic type with unsupported or impure arguments.
                WriteDataType(bw, dataType | DataTypes.GenericTypeDefinition); // note: no multiple DataTypes even for dictionaries!

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

            private void WriteNewAssembly(BinaryWriter bw, Assembly assembly, string binderAsmName)
            {
                // by binder
                if (binderAsmName != null)
                {
                    bw.Write(binderAsmName);
                    AssemblyNameIndexCache.Add(binderAsmName, AssemblyIndexCacheCount);
                    return;
                }

                bw.Write(assembly.FullName);
                AssemblyIndexCache.Add(assembly, AssemblyIndexCacheCount);
            }

            /// <summary>
            /// Writes a new non-pure type if a binder did not handle it. Assembly part is already written.
            /// If open types are allowed a generic type definition is followed by a specifier; otherwise, by type arguments.
            /// </summary>
            [SecurityCritical]
            private void WriteNewType(BinaryWriter bw, Type type, bool allowOpenTypes, string binderAsmName, string binderTypeName)
            {
                Debug.Assert(allowOpenTypes || !(type.IsGenericTypeDefinition || type.IsGenericParameter), $"Generic type definitions and generic parameters are allowed only when {nameof(allowOpenTypes)} is true.");
                Type rootType = type.IsConstructedGenericType() ? type.GetGenericTypeDefinition()
                    : type.IsGenericParameter ? type.DeclaringType
                    : type;
                bool isGeneric = type.IsGenericType;
                bool isTypeDef = type.IsGenericTypeDefinition;
                bool isGenericParam = type.IsGenericParameter;
                Type typeDef = isGeneric || isGenericParam ? rootType : null;

                // Actualizing bound names if needed
                if (rootType != type)
                    GetBoundNames(rootType, out binderAsmName, out binderTypeName);

                // 1.) Type index
                bool isNewType = false;
                int index;

                // It can happen that the generic type definition is already known.
                if (typeDef != null && (index = GetTypeIndex(typeDef, binderAsmName, binderTypeName)) != -1)
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
                    index = GetAssemblyIndex(rootType, binderAsmName);

                    // known assembly
                    if (index != -1)
                        Write7BitInt(bw, index);
                    // new assembly: writing also the name
                    else
                    {
                        Write7BitInt(bw, NewAssemblyIndex);
                        WriteNewAssembly(bw, rootType.Assembly, binderAsmName);
                    }

                    // 3.) Root type name of new type
                    // ReSharper disable once AssignNullToNotNullAttribute - FullName is not null for the root type
                    bw.Write(binderTypeName ?? rootType.FullName);
                    AddToTypeCache(rootType, binderAsmName, binderTypeName);

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
                    Debug.Assert(!isTypeDef, "Type definition ");
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
            private void WriteTypeByName(BinaryWriter bw, Type origType, string explicitAsmName, string explicitTypeName)
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
                index = GetAssemblyIndex(origType, explicitAsmName);

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
                // ReSharper disable once AssignNullToNotNullAttribute - FullName is not null for the root type
                bw.Write(explicitTypeName);
                AddToTypeCache(origType, explicitAsmName, explicitTypeName);
            }

            private void WriteGenericMethodParameter(BinaryWriter bw, Type type)
            {
                Debug.Assert(type.IsGenericParameter && type.DeclaringMethod != null, "Generic method argument is expected here");

                // Writing a special placeholder indicating the generic method parameter because the declaring type
                // of generic methods has nothing to do with the parameter so they cannot be encoded the same way as type parameters.
                WriteType(bw, typeof(GenericMethodDefinitionPlaceholder), true);

                // For generic method parameters no specifier is needed because the placeholder type has been written.
                // Instead, writing the declaring type, method signature and parameter index
                Type declaringType = type.DeclaringType;
                WriteType(bw, declaringType, true);

                // ReSharper disable once PossibleNullReferenceException - false alarm in release build, see assert above
                bw.Write(type.DeclaringMethod.ToString());
                Write7BitInt(bw, type.GenericParameterPosition);

                AddToTypeCache(type, null, null);
            }

            private void AddToTypeCache(Type type, string binderAsmName = null, string binderTypeName = null)
            {
                // Even if current binder names are null we must use the string based cache if there is a binder
                // to avoid possibly conflicting type names between the custom and default binding and among binder type names.
                if (binderAsmName != null || binderTypeName != null)
                {
                    TypeNameIndexCache.Add(GetTypeNameIndexCacheKey(type, binderAsmName, binderTypeName), TypeIndexCacheCount);
                    return;
                }

                TypeIndexCache.Add(type, TypeIndexCacheCount);
            }

            /// <summary>
            /// Writes an ID and returns if it was already known.
            /// If forceStructs is false, then only known immutable structs will get an id.
            /// </summary>
            private bool WriteId(BinaryWriter bw, object data, bool forceStructs = true)
            {
                static bool IsComparedByValue(Type t) =>
                    t.IsPrimitive || t.BaseType == Reflector.EnumType || // always instance so can be used than the slower IsEnum
                    t.In(Reflector.StringType, Reflector.DecimalType, Reflector.DateTimeType, Reflector.TimeSpanType, Reflector.DateTimeOffsetType, typeof(Guid));

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

                if (!forceStructs && type.IsValueType)
                    return false;

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

            private void WriteBinarySerializable(BinaryWriter bw, IBinarySerializable instance, bool writeType, bool isRoot)
            {
                if (writeType)
                {
                    Type type = instance.GetType();
                    if (!isRoot)
                        type = typeof(BinarySerializable<>).GetGenericType(type);
                    WriteType(bw, type);
                }

                OnSerializing(instance);
                byte[] rawData = instance.Serialize(Options);
                Write7BitInt(bw, rawData.Length);
                bw.Write(rawData);
                OnSerialized(instance);
            }

            [SecurityCritical]
            private void WriteValueType(BinaryWriter bw, ValueType data, bool writeType, bool isRoot)
            {
                if (writeType)
                {
                    Type type = data.GetType();
                    if (!isRoot)
                        type = typeof(RawStruct<>).GetGenericType(type);
                    WriteType(bw, type);
                }

                OnSerializing(data);
                byte[] rawData = BinarySerializer.SerializeValueType(data);
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
