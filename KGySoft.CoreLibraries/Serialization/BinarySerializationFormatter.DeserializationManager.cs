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
        private sealed class DeserializationManager : SerializationManagerBase
        {
            #region Fields

            private List<Assembly> readAssemblies;
            private List<Type> readTypes;
            private Dictionary<string, Assembly> assemblyByNameCache;
            private Dictionary<string, Type> typeByNameCache;
            private Dictionary<int, object> idCache;
            private Dictionary<object, List<KeyValuePair<FieldInfo, object>>> objectReferences;
            private List<IDeserializationCallback> deserializationRegObjects;

            #endregion

            #region Properties

            private Dictionary<int, object> IdCache => idCache ?? (idCache = new Dictionary<int, object> { { 0, null } });

            #endregion

            #region Constructors

            internal DeserializationManager(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector)
                : base(context, options, binder, surrogateSelector)
            {
            }

            #endregion

            #region Methods

            #region Static Methods

            private static Version ReadVersion(BinaryReader br)
            {
                int major = br.ReadInt32();
                int minor = br.ReadInt32();
                int build = br.ReadInt32();
                int revision = br.ReadInt32();
                if (revision == -1)
                    return new Version(major, minor);
                if (build == -1)
                    return new Version(major, minor, build);
                return new Version(major, minor, build, revision);
            }

            private static Uri ReadUri(BinaryReader br)
            {
                bool isAbsolute = br.ReadBoolean();
                return new Uri(br.ReadString(), isAbsolute ? UriKind.Absolute : UriKind.Relative);
            }

            private static BitArray ReadBitArray(BinaryReader br)
            {
                int length = Read7BitInt(br);
                var values = new int[(length + 31) >> 5];
                if (length > 0)
                {
                    for (int i = 0; i < values.Length; i++)
                        values[i] = br.ReadInt32();
                }

                return new BitArray(values) { Length = length };
            }

            private static BitVector32.Section ReadSection(BinaryReader br)
            {
                short mask = br.ReadInt16();
                short offset = br.ReadInt16();
                if (offset == 0)
                    return BitVector32.CreateSection(mask);

                BitVector32.Section shift = BitVector32.CreateSection(1);
                while (shift.Offset < offset - 1)
                    shift = BitVector32.CreateSection(1, shift);
                return BitVector32.CreateSection(mask, shift);
            }

            private static StringBuilder ReadStringBuilder(BinaryReader br)
            {
                int capacity = Read7BitInt(br);
                return new StringBuilder(br.ReadString(), capacity);
            }

            #endregion

            #region Instance Methods

            #region Internal Methods

            [SecurityCritical]
            internal object Deserialize(BinaryReader br)
            {
                object result = Read(br, true);
                DeserializationCallback();
                return result;
            }

            [SecurityCritical]
            internal object Read(BinaryReader br, bool isRoot)
            {
                if (!isRoot && TryGetCachedObject(br, out object result))
                    return result;

                DataTypes dataType = ReadDataType(br);

                // 1.) null value
                if (dataType == DataTypes.Null)
                    return null;

                bool addToCache = !isRoot;

                // 2.) other supported non-collection type
                if ((dataType & DataTypes.CollectionTypes) == DataTypes.Null)
                {
                    // on root level id is written for IBinarySerializable and recursive objects (collections are checked below)
                    if (isRoot && (dataType == DataTypes.BinarySerializable || dataType == DataTypes.RecursiveObjectGraph))
                    {
                        addToCache = true;
                        if (TryGetCachedObject(br, out result))
                        {
                            Debug.Fail("Root level object is not expected in the cache");
                            return result;
                        }
                    }

                    return ReadObject(br, addToCache, dataType, null, false);
                }

                // 3.) compound collection type
                DataTypeDescriptor descriptor = new DataTypeDescriptor(null, dataType, br);

                // on root level id is written after data type only when the collection can have recursion
                if (isRoot && descriptor.CanHaveRecursion)
                {
                    addToCache = true;
                    if (TryGetCachedObject(br, out result))
                    {
                        Debug.Fail("Root level object is not expected in the cache");
                        return result;
                    }
                }

                descriptor.DecodeType(br, this);

                // 3/a.) array
                if (descriptor.IsArray)
                    return CreateArray(br, addToCache, descriptor);

                // 3/b.) non-array collection or key-value
                return CreateCollection(br, addToCache, descriptor);
            }

            [SecurityCritical]
            internal object ReadElement(BinaryReader br, DataTypeDescriptor collectionDescriptor, bool isTValue)
            {
                DataTypes elementDataType = collectionDescriptor.GetElementDataType(isTValue);

                // single element
                if (elementDataType != DataTypes.Null)
                    return ReadObject(br, !collectionDescriptor.GetElementType(isTValue).IsValueType, elementDataType, collectionDescriptor, isTValue);

                DataTypeDescriptor elementDescriptor = collectionDescriptor.GetElementDescriptor(isTValue);
                // nested array
                if (elementDescriptor.IsArray)
                    return CreateArray(br, true, elementDescriptor);

                // other nested collection
                return CreateCollection(br, !elementDescriptor.Type.IsValueType || elementDescriptor.Type.IsNullable(), elementDescriptor);
            }

            /// <summary>
            /// Reads a type from the serialization stream
            /// </summary>
            internal Type ReadType(BinaryReader br)
            {
                // assembly index
                int index = Read7BitInt(br);
                if (readAssemblies == null)
                    readAssemblies = new List<Assembly>(KnownAssemblies);

                // natively supported type
                if (index == readAssemblies.Count + 2)
                {
                    DataTypes dataType = ReadDataType(br);
                    DataTypeDescriptor desc = new DataTypeDescriptor(null, dataType, br);
                    desc.DecodeType(br, this);
                    return desc.Type;
                }

                if (readTypes == null)
                {
                    readTypes = new List<Type>(Math.Max(4, KnownTypes.Length));
                    readTypes.AddRange(KnownTypes);
                }

                // new assembly: assembly and type are stored together
                if (index == readAssemblies.Count + 1)
                {
                    // assembly qualified name (GetType uses binder if set)
                    Type type = GetType(br.ReadString(), br.ReadString());
                    readAssemblies.Add(type.Assembly);
                    readTypes.Add(type);
                    if (type.IsGenericTypeDefinition)
                    {
                        type = ReadGenericType(br, type);
                    }

                    return type;
                }

                Assembly assembly = null;

                // type with assembly (unless assembly is omitted)
                if (index != readAssemblies.Count)
                {
                    Debug.Assert(index >= 0 && index < readAssemblies.Count, "Invalid assembly index");
                    assembly = readAssemblies[index];
                }

                // type index
                index = Read7BitInt(br);

                // reading type
                if (index == readTypes.Count + 1)
                {
                    string typeName = br.ReadString();
                    Type type = null;
                    // ReSharper disable AssignNullToNotNullAttribute
                    if (Binder != null)
                        type = Binder.BindToType(assembly == null ? String.Empty : assembly.FullName, typeName);
                    // ReSharper restore AssignNullToNotNullAttribute
                    if (type == null)
                        type = assembly == null
                            ? Reflector.ResolveType(typeName)
                            : Reflector.ResolveType(assembly, typeName);
                    if (type == null)
                        throw new SerializationException(Res.BinarySerializationCannotResolveType(typeName));
                    readTypes.Add(type);
                    if (type.IsGenericTypeDefinition)
                        type = ReadGenericType(br, type);

                    return type;
                }

                Debug.Assert(index >= 0 && index < readTypes.Count, "Invalid type index");
                Type result = readTypes[index];
                if (result.IsGenericTypeDefinition)
                    result = ReadGenericType(br, result);

                // ReSharper disable AssignNullToNotNullAttribute
                return Binder != null
                    ? (Binder.BindToType(assembly == null ? String.Empty : assembly.FullName, result.FullName) ?? result)
                    : result;
                // ReSharper restore AssignNullToNotNullAttribute
            }

            internal void AddObjectToCache(object obj)
            {
                Dictionary<int, object> cache = IdCache;
                cache.Add(idCache.Count, obj);
            }

            internal void AddObjectToCache(object obj, out int id)
            {
                Dictionary<int, object> cache = IdCache;
                id = idCache.Count;
                cache.Add(id, obj);
            }

            internal void ReplaceObjectInCache(int id, object obj)
            {
                Dictionary<int, object> cache = IdCache;
                cache[id] = obj;
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// Creates and populates array
            /// </summary>
            [SecurityCritical]
            private object CreateArray(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                // getting whether the current instance is in cache
                if (descriptor.ParentDescriptor != null)
                {
                    if (TryGetCachedObject(br, out object cachedResult))
                        return cachedResult;
                }

                // creating the array
                int rank = descriptor.Type.GetArrayRank();
                int[] lengths = new int[rank];
                int[] lowerBounds = new int[rank];
                for (int i = 0; i < rank; i++)
                {
                    lowerBounds[i] = Read7BitInt(br);
                    lengths[i] = Read7BitInt(br);
                }

                Array result = Array.CreateInstance(descriptor.ElementType, lengths, lowerBounds);
                if (addToCache)
                    AddObjectToCache(result);

                // primitive array
                if (descriptor.ElementType.IsPrimitive)
                {
                    int length = Buffer.ByteLength(result);
                    Buffer.BlockCopy(br.ReadBytes(length), 0, result, 0, length);
                }

                // 1D array
                else if (lengths.Length == 1)
                {
                    int offset = lowerBounds[0];
                    for (int i = 0; i < result.Length; i++)
                    {
                        object value = ReadElement(br, descriptor, false);
                        result.SetValue(value, i + offset);
                    }
                }

                // multidimensional array
                else
                {
                    var arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
                    while (arrayIndexer.MoveNext())
                    {
                        object value = ReadElement(br, descriptor, false);
                        result.SetValue(value, arrayIndexer.Current);
                    }
                }

                return result;
            }

            /// <summary>
            /// Creates and populates a collection
            /// </summary>
            [SecurityCritical]
            private object CreateCollection(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                if (!descriptor.IsSingleElement && !Reflector.IEnumerableType.IsAssignableFrom(descriptor.Type))
                    throw new InvalidOperationException(Res.BinarySerializationIEnumerableExpected(descriptor.Type));

                // getting whether the current instance is in cache
                if (descriptor.ParentDescriptor != null && (!descriptor.Type.IsValueType || descriptor.Type.IsNullable()))
                {
                    if (TryGetCachedObject(br, out object cachedResult))
                        return cachedResult;
                }

                CollectionSerializationInfo serInfo = serializationInfo[descriptor.CollectionDataType];
                object result = serInfo.InitializeCollection(br, addToCache, descriptor, this, out int count);
                if (result is IEnumerable collection)
                {
                    MethodAccessor addMethod = serInfo.SpecificAddMethod == null ? null : serInfo.GetAddMethod(descriptor);
                    for (int i = 0; i < count; i++)
                    {
                        object element = ReadElement(br, descriptor, false);
                        object value = descriptor.IsDictionary ? ReadElement(br, descriptor, true) : null;

                        if (descriptor.IsDictionary)
                        {
                            if (addMethod != null)
                            {
                                addMethod.Invoke(collection, element, value);
                                continue;
                            }

#if NET35
                            if (value != null || !descriptor.IsGenericDictionary)
#endif

                            {
                                ((IDictionary)collection).Add(element, value);
                                continue;
                            }
#if NET35

                            // generic dictionary with null value: calling generic Add because non-generic one may fail in .NET Runtime 2.x
                            addMethod = serInfo.GetAddMethod(descriptor);
                            addMethod.Invoke(collection, element, null);
                            continue;
#endif

                        }

                        if (addMethod != null)
                        {
                            addMethod.Invoke(collection, element);
                            continue;
                        }

                        if (collection is IList list)
                        {
                            list.Add(element);
                            continue;
                        }

                        Debug.Fail($"Define an Add method for type {descriptor.Type} for better performance");
                        collection.TryAdd(element, false);
                    }
                }

                return descriptor.IsReadOnly ? descriptor.GetAsReadOnly(result) : result;
            }

            /// <summary>
            /// Reads a non-collection object from the stream.
            /// </summary>
            /// <param name="br">The reader</param>
            /// <param name="addToCache">When <see langword="true"/>, the result must be added to the ID cache. Otherwise, only reference types in a collection might be added to cache.</param>
            /// <param name="dataType">The already read data type of the object.</param>
            /// <param name="collectionDescriptor">When a collection element is deserialized, the collection descriptor.</param>
            /// <param name="isTValue"><see langword="true"/>, when element to deserialize is the value in a dictionary collection.</param>
            /// <returns>The deserialized object.</returns>
            [SecurityCritical]
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Long but very straightforward switch")]
            private object ReadObject(BinaryReader br, bool addToCache, DataTypes dataType, DataTypeDescriptor collectionDescriptor, bool isTValue)
            {
                bool TryGetFromCache(out object cachedValue)
                {
                    if (collectionDescriptor == null)
                    {
                        cachedValue = null;
                        return false;
                    }

                    Debug.Assert(addToCache, "Reference element types of collections should be cached");
                    return TryGetCachedObject(br, out cachedValue);
                }

                bool is7BitEncoded = (dataType & DataTypes.Store7BitEncoded) != DataTypes.Null;

                // nullable type
                if ((dataType & DataTypes.Nullable) == DataTypes.Nullable)
                {
                    // there is no nullable type on root level so checking for null in any case (occurs in collection and type members)
                    if (!br.ReadBoolean())
                        return null;
                }

                object createdResult = null;
                try
                {
                    object cachedResult;
                    switch (dataType & ~DataTypes.Store7BitEncoded & ~DataTypes.Nullable)
                    {
                        case DataTypes.Bool:
                            return createdResult = br.ReadBoolean();
                        case DataTypes.UInt8:
                            return createdResult = br.ReadByte();
                        case DataTypes.Int8:
                            return createdResult = br.ReadSByte();
                        case DataTypes.Int16:
                            return createdResult = is7BitEncoded ? (short)Read7BitInt(br) : br.ReadInt16();
                        case DataTypes.UInt16:
                            return createdResult = is7BitEncoded ? (ushort)Read7BitInt(br) : br.ReadUInt16();
                        case DataTypes.Int32:
                            return createdResult = is7BitEncoded ? Read7BitInt(br) : br.ReadInt32();
                        case DataTypes.UInt32:
                            return createdResult = is7BitEncoded ? (uint)Read7BitInt(br) : br.ReadUInt32();
                        case DataTypes.Int64:
                            return createdResult = is7BitEncoded ? Read7BitLong(br) : br.ReadInt64();
                        case DataTypes.UInt64:
                            return createdResult = is7BitEncoded ? (ulong)Read7BitLong(br) : br.ReadUInt64();
                        case DataTypes.Char:
                            return createdResult = is7BitEncoded ? (char)Read7BitInt(br) : (char)br.ReadUInt16();
                        case DataTypes.String:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = br.ReadString();
                        case DataTypes.Single:
                            return createdResult = is7BitEncoded ? BitConverter.ToSingle(BitConverter.GetBytes(Read7BitInt(br)), 0) : br.ReadSingle();
                        case DataTypes.Double:
                            return createdResult = is7BitEncoded ? BitConverter.Int64BitsToDouble(Read7BitLong(br)) : br.ReadDouble();
                        case DataTypes.Decimal:
                            return createdResult = br.ReadDecimal();
                        case DataTypes.DateTime:
                            DateTimeKind kind = (DateTimeKind)br.ReadByte();
                            return createdResult = new DateTime(br.ReadInt64(), kind);
                        case DataTypes.TimeSpan:
                            return createdResult = new TimeSpan(br.ReadInt64());
                        case DataTypes.DateTimeOffset:
                            createdResult = new DateTimeOffset(br.ReadInt64(), TimeSpan.FromMinutes(br.ReadInt16()));
                            return createdResult;
                        case DataTypes.DBNull:
                            // the cached id for a DBNull is actually either 0 for null or a single id
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = DBNull.Value;
                        case DataTypes.IntPtr:
                            return createdResult = new IntPtr(is7BitEncoded ? Read7BitLong(br) : br.ReadInt64());
                        case DataTypes.UIntPtr:
                            return createdResult = new UIntPtr(is7BitEncoded ? (ulong)Read7BitLong(br) : br.ReadUInt64());
                        case DataTypes.Object:
                            // object - returning object instance on root level, otherwise, doing recursion because can mean any type as an element type
                            if (collectionDescriptor == null)
                                return createdResult = new object();
                            // result is not set here - when caching is needed, will be done in the recursion
                            return Read(br, false);
                        case DataTypes.Version:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = ReadVersion(br);
                        case DataTypes.Guid:
                            return createdResult = new Guid(br.ReadBytes(16));
                        case DataTypes.Uri:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = ReadUri(br);
                        case DataTypes.BitArray:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = ReadBitArray(br);
                        case DataTypes.BitVector32:
                            return createdResult = new BitVector32(br.ReadInt32());
                        case DataTypes.BitVector32Section:
                            return createdResult = ReadSection(br);
                        case DataTypes.StringBuilder:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = ReadStringBuilder(br);
                        case DataTypes.BinarySerializable:
                            return ReadBinarySerializable(br, addToCache, collectionDescriptor, isTValue);
                        case DataTypes.RecursiveObjectGraph:
                            return ReadObjectGraph(br, addToCache, collectionDescriptor, isTValue);
                        case DataTypes.RawStruct:
                            return createdResult = ReadValueType(br, collectionDescriptor, isTValue);
                        default:
                            if ((dataType & DataTypes.Enum) == DataTypes.Enum)
                                return createdResult = ReadEnum(br, dataType, collectionDescriptor, isTValue);
                            throw new InvalidOperationException(Res.BinarySerializationCannotDeserializeObject(DataTypeToString(dataType)));
                    }
                }
                finally
                {
                    if (addToCache && createdResult != null)
                        AddObjectToCache(createdResult);
                }
            }

            [SecurityCritical]
            private object ReadBinarySerializable(BinaryReader br, bool addToCache, DataTypeDescriptor collectionDescriptor, bool isTValue)
            {
                // checking instance id
                Type elementType = null;
                if (collectionDescriptor != null &&
                    (!(elementType = collectionDescriptor.GetElementType(isTValue)).IsValueType))
                {
                    if (TryGetCachedObject(br, out object cachedResult))
                        return cachedResult;
                }

                Type objType;
                if (collectionDescriptor == null)
                    objType = ReadType(br);
                else
                {
                    // Common order: 1: qualify -> is element type, 2: different type -> read type, 3: deserialize
                    // 1. If elements should be qualified and element is not the same as collection element type
                    if (collectionDescriptor.AreAllElementsQualified(isTValue) && !br.ReadBoolean())
                    {
                        // 2. then read type
                        objType = ReadType(br);
                    }
                    else
                        objType = elementType ?? collectionDescriptor.GetElementType(isTValue);
                }

                // 3. deserialize (result is not set here - object will be cached immediately after creation so circular references will be found in time)
                return DoReadBinarySerializable(br, addToCache, objType);
            }

            [SecurityCritical]
            private object DoReadBinarySerializable(BinaryReader br, bool addToCache, Type type)
            {
                byte[] serData = br.ReadBytes(Read7BitInt(br));

                if (!Reflector.TryCreateEmptyObject(type, false, true, out object result))
                    throw new SerializationException(Res.BinarySerializationCannotCreateUninitializedObject(type));

                if (addToCache)
                    AddObjectToCache(result);
                OnDeserializing(result);

                // Trying to use a deserializer constructor in the first place.
                if (!Accessors.TryInvokeCtor(result, Options, serData))
                {
                    // Otherwise, using default constructor (if any) + deserializing method
                    Accessors.TryInvokeCtor(result);
                    ((IBinarySerializable)result).Deserialize(Options, serData);
                }

                OnDeserialized(result);
                return result;
            }

            [SecurityCritical]
            private object ReadObjectGraph(BinaryReader br, bool addToCache, DataTypeDescriptor collectionDescriptor, bool isTValue)
            {
                // When element types may differ, reading element with data type
                if (collectionDescriptor != null && collectionDescriptor.AreAllElementsQualified(isTValue))
                    return Read(br, false);

                // checking instance id
                Type elementType = null;
                if (collectionDescriptor != null &&
                    (!(elementType = collectionDescriptor.GetElementType(isTValue)).IsValueType))
                {
                    if (TryGetCachedObject(br, out object cachedResult))
                        return cachedResult;
                }

                // in collection, type is already known, otherwise, reading it
                Type objType = collectionDescriptor == null
                    ? ReadType(br)
                    : (elementType ?? collectionDescriptor.GetElementType(isTValue));

                // 2. deserialize (result is not set here - object will be cached immediately after creation so circular references will be found in time)
                return DoReadObjectGraph(br, addToCache, objType, collectionDescriptor != null);
            }

            /// <summary>
            /// Deserializing object graph with options that was used on serialization.
            /// </summary>
            [SecurityCritical]
            private object DoReadObjectGraph(BinaryReader br, bool addToCache, Type type, bool refineType)
            {
                // a.) Graph method
                bool isDefaultObjectGraph = br.ReadBoolean();

                // b.) Possible type change
                if (!isDefaultObjectGraph && refineType && br.ReadBoolean())
                    type = ReadType(br);

                // c.) Reading members
                if (!Reflector.TryCreateEmptyObject(type, false, true, out object result))
                    throw new SerializationException(Res.BinarySerializationCannotCreateUninitializedObject(type));
                int id = 0;
                if (addToCache)
                    AddObjectToCache(result, out id);
                OnDeserializing(result);

                bool useSurrogate = TryGetSurrogate(type, out ISerializationSurrogate surrogate, out ISurrogateSelector selector);
                bool isISerializable = !IgnoreISerializable && result is ISerializable;

                // default graph was serialized
                if (isDefaultObjectGraph)
                {
                    if (!isISerializable && !useSurrogate)
                    {
                        // default graph should be deserialized
                        ReadDefaultObjectGraph(br, result);
                    }
                    else
                    {
                        // the default graph should be deserialized either as ISerializable or by a surrogate
                        ReadDefaultObjectGraphAsCustom(br, result, surrogate, selector);
                    }
                }
                // custom graph was serialized
                else if (isISerializable || useSurrogate)
                {
                    // custom graph should be deserialized
                    ReadCustomObjectGraph(br, result, surrogate, selector);
                }
                else
                {
                    // the custom graph should be deserialized as a default object by setting fields
                    ReadCustomObjectGraphAsDefault(br, result);
                }

                OnDeserialized(result);

                // if type result is IObjectReference, then calling its GetRealObject to return something
                if (!IgnoreIObjectReference && result is IObjectReference objRef)
                {
                    result = objRef.GetRealObject(Context);
                    UpdateReferences(objRef, result);
                    if (addToCache)
                        ReplaceObjectInCache(id, result);
                }

                return result;
            }

            [SecurityCritical]
            private void ReadDefaultObjectGraph(BinaryReader br, object obj)
            {
                Type type = obj.GetType();

                // iterating through self and base types
                for (Type t = type; t != Reflector.ObjectType; t = t.BaseType)
                {
                    // checking name of base type
                    if (t != type)
                    {
                        string name = br.ReadString();

                        // ReSharper disable once PossibleNullReferenceException - obj is object in all cases
                        while (t.Name != name && t != Reflector.ObjectType)
                            t = t.BaseType;

                        if (name.Length == 0 && t == Reflector.ObjectType)
                            return;

                        if (t.Name != name && !IgnoreObjectChanges)
                            throw new SerializationException(Res.BinarySerializationObjectHierarchyChanged(type));
                    }

                    // reading fields of current level
                    int count = Read7BitInt(br);
                    for (int i = 0; i < count; i++)
                    {
                        string name = br.ReadString();
                        object value = Read(br, false);

                        FieldInfo field = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        if (field == null)
                        {
                            if (!IgnoreObjectChanges)
                            {
                                if (t == type)
                                    throw new SerializationException(Res.BinarySerializationMissingField(type, name));
                                throw new SerializationException(Res.BinarySerializationMissingFieldBase(type, name, t));
                            }

                            continue;
                        }

                        if (field.IsNotSerialized)
                            continue;

                        TrySetField(field, obj, value);
                    }
                }

                // checking end of hierarchy
                if (br.ReadString().Length != 0)
                {
                    if (!IgnoreObjectChanges)
                        throw new SerializationException(Res.BinarySerializationObjectHierarchyChanged(type));

                    // skipping fields until the end of the serialized hierarchy
                    do
                    {
                        int count = Read7BitInt(br);
                        for (int i = 0; i < count; i++)
                        {
                            br.ReadString();
                            Read(br, false);
                        }
                    } while (br.ReadString().Length != 0);
                }
            }

            [SecurityCritical]
            private void ReadCustomObjectGraph(BinaryReader br, object obj, ISerializationSurrogate surrogate, ISurrogateSelector selector)
            {
                Type type = obj.GetType();
                SerializationInfo si = new SerializationInfo(type, new FormatterConverter());
                int count = Read7BitInt(br);

                // reading content into si
                for (int i = 0; i < count; i++)
                {
                    string name = br.ReadString();
                    object value = Read(br, false);
                    Type elementType = value?.GetType() ?? Reflector.ObjectType;
                    if (!br.ReadBoolean())
                        elementType = ReadType(br);
                    si.AddValue(name, value, elementType);
                }

                CheckReferences(si);
                if (surrogate == null)
                {
                    if (!Accessors.TryInvokeCtor(obj, si, Context))
                        throw new SerializationException(Res.BinarySerializationMissingISerializableCtor(type));
                }
                else
                {
                    // Using surrogate
                    object result = surrogate.SetObjectData(obj, si, Context, selector);
                    if (obj != result)
                        throw new NotSupportedException(Res.BinarySerializationSurrogateChangedObject(type));
                }
            }

            [SecurityCritical]
            private void ReadDefaultObjectGraphAsCustom(BinaryReader br, object obj, ISerializationSurrogate surrogate, ISurrogateSelector selector)
            {
                Type type = obj.GetType();

                // reading original fields into si
                SerializationInfo si = new SerializationInfo(type, new FormatterConverter());
                do
                {
                    // reading fields of current level
                    int count = Read7BitInt(br);
                    for (int i = 0; i < count; i++)
                    {
                        string name = br.ReadString();
                        object value = Read(br, false);
                        si.AddValue(name, value);
                    }

                    // end level is marked with empty string
                } while (br.ReadString().Length != 0);

                CheckReferences(si);
                if (surrogate == null)
                {
                    // As ISerializable: Invoking serialization constructor
                    if (!Accessors.TryInvokeCtor(obj, si, Context))
                        throw new SerializationException(Res.BinarySerializationMissingISerializableCtor(type));
                }
                else
                {
                    // Using surrogate
                    object result = surrogate.SetObjectData(obj, si, Context, selector);
                    if (obj != result)
                        throw new NotSupportedException(Res.BinarySerializationSurrogateChangedObject(type));
                }
            }

            [SecurityCritical]
            private void ReadCustomObjectGraphAsDefault(BinaryReader br, object obj)
            {
                int count = Read7BitInt(br);
                Dictionary<string, object> elements = new Dictionary<string, object>(count);

                // reading content into the dictionary
                for (int i = 0; i < count; i++)
                {
                    string name = br.ReadString();
                    object value = Read(br, false);
                    if (!br.ReadBoolean())
                    {
                        Type elementType = ReadType(br);
                        if (value != null && value.GetType() != elementType)
                            value = Convert.ChangeType(value, elementType, CultureInfo.InvariantCulture); // this is what FormatterConverter does as well on SerializationInfo.GetValue
                    }

                    elements[name] = value;
                }

                if (count == 0)
                    return;

                bool checkFields = !IgnoreObjectChanges;

                // ReSharper disable once PossibleNullReferenceException - every type is derived from object here
                // iterating through fields and setting found elements
                for (Type t = obj.GetType(); t != Reflector.ObjectType; t = t.BaseType)
                {
                    FieldInfo[] fields = BinarySerializer.GetSerializableFields(t);
                    foreach (FieldInfo field in fields)
                    {
                        if (elements.TryGetValue(field.Name, out object value))
                        {
                            TrySetField(field, obj, value);
                            if (checkFields)
                                elements.Remove(field.Name);
                        }
                    }
                }

                if (checkFields && elements.Count > 0)
                    throw new SerializationException(Res.BinarySerializationMissingField(obj.GetType(), elements.First().Key));
            }

            [SecurityCritical]
            private object ReadValueType(BinaryReader br, DataTypeDescriptor collectionDescriptor, bool isTValue)
            {
                Type structType = collectionDescriptor == null
                    ? ReadType(br)
                    : collectionDescriptor.GetElementType(isTValue);
                byte[] rawData = br.ReadBytes(Read7BitInt(br));
                object result = BinarySerializer.DeserializeValueType(structType, rawData);
                OnDeserializing(result);
                OnDeserialized(result);
                return result;
            }

            private object ReadEnum(BinaryReader br, DataTypes dataType, DataTypeDescriptor collectionDescriptor, bool isTValue)
            {
                Type enumType = collectionDescriptor == null
                    ? ReadType(br)
                    : collectionDescriptor.GetElementType(isTValue);
                bool is7BitEncoded = (dataType & DataTypes.Store7BitEncoded) != DataTypes.Null;
                switch (dataType & DataTypes.SimpleTypes)
                {
                    case DataTypes.Int8:
                        return Enum.ToObject(enumType, br.ReadSByte());
                    case DataTypes.UInt8:
                        return Enum.ToObject(enumType, br.ReadByte());
                    case DataTypes.Int16:
                        return Enum.ToObject(enumType,
                            is7BitEncoded ? (short)Read7BitInt(br) : br.ReadInt16());
                    case DataTypes.UInt16:
                        return Enum.ToObject(enumType,
                            is7BitEncoded ? (ushort)Read7BitInt(br) : br.ReadUInt16());
                    case DataTypes.Int32:
                        return Enum.ToObject(enumType, is7BitEncoded ? Read7BitInt(br) : br.ReadInt32());
                    case DataTypes.UInt32:
                        return Enum.ToObject(enumType,
                            is7BitEncoded ? (uint)Read7BitInt(br) : br.ReadUInt32());
                    case DataTypes.Int64:
                        return Enum.ToObject(enumType, is7BitEncoded ? Read7BitLong(br) : br.ReadInt64());
                    case DataTypes.UInt64:
                        return Enum.ToObject(enumType,
                            is7BitEncoded ? (ulong)Read7BitLong(br) : br.ReadUInt64());
                    default:
                        throw new InvalidOperationException(Res.BinarySerializationInvalidEnumBase(DataTypeToString(dataType & DataTypes.SimpleTypes)));
                }
            }

            private void OnDeserializing(object obj) => ExecuteMethodsOfAttribute(obj, typeof(OnDeserializingAttribute));

            private void OnDeserialized(object obj)
            {
                if (IgnoreSerializationMethods)
                    return;
                ExecuteMethodsOfAttribute(obj, typeof(OnDeserializedAttribute));
                RegisterDeserializedObject(obj as IDeserializationCallback);
            }

            private void RegisterDeserializedObject(IDeserializationCallback obj)
            {
                if (obj == null)
                    return;

                if (deserializationRegObjects == null)
                    deserializationRegObjects = new List<IDeserializationCallback>();
                deserializationRegObjects.Add(obj);
            }

            private void DeserializationCallback()
            {
                if (deserializationRegObjects == null)
                    return;

                for (int i = deserializationRegObjects.Count - 1; i >= 0; i--)
                    deserializationRegObjects[i].OnDeserialization(this);
                deserializationRegObjects = null;
            }

            private bool TryGetCachedObject(BinaryReader br, out object result)
            {
                Dictionary<int, object> cache = IdCache;
                int id = Read7BitInt(br);
                if (cache.TryGetValue(id, out result))
                    return true;

                if (id > cache.Count)
                    throw new SerializationException(Res.BinarySerializationDeserializeUnexpectedId);
                return false;
            }

            private void TrySetField(FieldInfo field, object obj, object value)
            {
                IObjectReference objRef;
                if ((Options & BinarySerializationOptions.IgnoreIObjectReference) == BinarySerializationOptions.None
                    && (objRef = value as IObjectReference) != null)
                {
                    // the object reference cannot be set yet so storing the new usage of the reference to be set later.
                    if (objectReferences == null)
                        objectReferences = new Dictionary<object, List<KeyValuePair<FieldInfo, object>>>(1, ReferenceEqualityComparer.Comparer);

                    if (!objectReferences.TryGetValue(objRef, out List<KeyValuePair<FieldInfo, object>> refUsages))
                    {
                        refUsages = new List<KeyValuePair<FieldInfo, object>>();
                        objectReferences.Add(objRef, refUsages);
                    }

                    refUsages.Add(new KeyValuePair<FieldInfo, object>(field, obj));
                    return;
                }

                FieldAccessor.GetAccessor(field).Set(obj, value);
            }

            private void CheckReferences(SerializationInfo si)
            {
                if (objectReferences == null)
                    return;

                // circular IObjectReferences can be resolved after all, except if custom deserialization is used for unresolved references
                foreach (SerializationEntry entry in si)
                {
                    if (entry.Value is IObjectReference objRef && objectReferences.ContainsKey(objRef))
                        throw new SerializationException(Res.BinarySerializationCircularIObjectReference);
                }
            }

            private void UpdateReferences(IObjectReference objRef, object realObject)
            {
                if (objectReferences == null || !objectReferences.TryGetValue(objRef, out List<KeyValuePair<FieldInfo, object>> refUsages))
                    return;

                foreach (KeyValuePair<FieldInfo, object> usage in refUsages)
                    FieldAccessor.GetAccessor(usage.Key).Set(usage.Value, realObject);

                objectReferences.Remove(objRef);
            }

            /// <summary>
            /// Resolves a type by string
            /// </summary>
            private Type GetType(string assemblyName, string typeName)
            {
                string key = assemblyName + ":" + typeName;
                if (typeByNameCache != null && typeByNameCache.TryGetValue(key, out Type result))
                    return result;

                if (Binder != null)
                {
                    result = Binder.BindToType(assemblyName, typeName);
                    if (result != null)
                    {
                        AddTypeToCache(key, result);
                        return result;
                    }
                }

                Assembly assembly = GetAssembly(assemblyName);
                result = Reflector.ResolveType(assembly, typeName);
                if (result == null)
                    throw new SerializationException(Res.BinarySerializationCannotResolveTypeInAssembly(typeName, assemblyName));

                AddTypeToCache(key, result);
                return result;
            }

            private void AddTypeToCache(string key, Type result)
            {
                if (typeByNameCache == null)
                    typeByNameCache = new Dictionary<string, Type>();
                typeByNameCache.Add(key, result);
            }

            private Type ReadGenericType(BinaryReader br, Type genTypeDef)
            {
                int len = genTypeDef.GetGenericArguments().Length;
                Type[] args = new Type[len];
                for (int i = 0; i < len; i++)
                    args[i] = ReadType(br);

                Type result = genTypeDef.MakeGenericType(args);
                readTypes.Add(result);

                // ReSharper disable once AssignNullToNotNullAttribute
                return Binder != null ? (Binder.BindToType(result.Assembly.FullName, result.FullName) ?? result) : result;
            }

            /// <summary>
            /// Resolves an assembly by string
            /// </summary>
            private Assembly GetAssembly(string name)
            {
                if (assemblyByNameCache != null && assemblyByNameCache.TryGetValue(name, out Assembly result))
                    return result;

                // 1.) Iterating through loaded assemblies
                result = Reflector.GetLoadedAssemblies().FirstOrDefault(asm => asm.FullName == name);

                // 2.) Trying to load assembly
                if (result == null)
                {
                    try
                    {
                        result = Assembly.Load(new AssemblyName(name));
                    }
                    catch (Exception e) when (!e.IsCritical())
                    {
                        try
                        {
                            result = Assembly.Load(name);
                        }
                        catch (Exception ex) when (!ex.IsCritical())
                        {
                            throw new SerializationException(Res.ReflectionCannotLoadAssembly(name), ex);
                        }
                    }
                }

                if (result == null)
                    throw new SerializationException(Res.ReflectionCannotLoadAssembly(name));
                if (assemblyByNameCache == null)
                    assemblyByNameCache = new Dictionary<string, Assembly>(1);
                assemblyByNameCache.Add(name, result);
                return result;
            }

            #endregion

            #endregion

            #endregion
        }
    }
}
