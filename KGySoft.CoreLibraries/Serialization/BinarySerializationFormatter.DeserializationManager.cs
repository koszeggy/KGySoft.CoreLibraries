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

            private List<(Assembly, string)> cachedAssemblies;
            private List<DataTypeDescriptor> cachedTypes;
            private Dictionary<string, Assembly> assemblyByNameCache;
            private Dictionary<string, Type> typeByNameCache;
            private Dictionary<int, object> idCache;
            private Dictionary<IObjectReference, List<KeyValuePair<FieldInfo, object>>> objectReferences;
            private List<IDeserializationCallback> deserializationRegObjects;

            #endregion

            #region Properties

            private Dictionary<int, object> IdCache => idCache ??= new Dictionary<int, object> { { 0, null } };

            private List<(Assembly Assembly, string StoredName)> CachedAssemblies
                => cachedAssemblies ??= new List<(Assembly, string)>(KnownAssemblies.Select(a => (a, (string)null)));

            private List<DataTypeDescriptor> CachedTypes
                => cachedTypes ??= new List<DataTypeDescriptor>(KnownTypes.Select(t => new DataTypeDescriptor(t)));

            private int OmitAssemblyIndex => CachedAssemblies.Count;
            private int NewAssemblyIndex => CachedAssemblies.Count + 1;
            private int NewTypeIndex => CachedTypes.Count + 1;
            private int EncodedTypeIndex => CachedTypes.Count + 2;

            #endregion

            #region Constructors

            internal DeserializationManager(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector)
                : base(context,
                    // Considering only deserialization flags. Other info must be read from the stream.
                    options & (BinarySerializationOptions.IgnoreSerializationMethods 
                        | BinarySerializationOptions.IgnoreObjectChanges 
                        | BinarySerializationOptions.IgnoreISerializable 
                        | BinarySerializationOptions.IgnoreIObjectReference),
                    binder, surrogateSelector)
            {
            }

            #endregion

            #region Methods

            #region Static Methods

            private static object ReadEnum(BinaryReader br, DataTypeDescriptor descriptor)
            {
                Type enumType = Nullable.GetUnderlyingType(descriptor.Type) ?? descriptor.Type;
                if (!enumType.IsEnum)
                    throw new SerializationException(Res.BinarySerializationNotAnEnum(enumType));
                DataTypes dataType = descriptor.ElementDataType;
                bool is7BitEncoded = IsCompressed(dataType);
                switch (GetUnderlyingSimpleType(dataType))
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
                        throw new InvalidOperationException(Res.BinarySerializationInvalidEnumBase(DataTypeToString(GetUnderlyingSimpleType(dataType))));
                }
            }

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
                object result = ReadRoot(br);
                DeserializationCallback();
                return result;
            }

            [SecurityCritical]
            internal object ReadWithType(BinaryReader br, DataTypeDescriptor knownElementType = null)
            {
                // getting whether the current instance is in cache
                if (knownElementType == null || !IsValueType(knownElementType))
                {
                    if (TryGetCachedObject(br, out object cachedResult))
                        return cachedResult;
                }

                DataTypeDescriptor descriptor = knownElementType != null && IsSealed(knownElementType) ? knownElementType : null;

                if (descriptor == null)
                {
                    descriptor = ReadType(br, false);
                    if ((descriptor.DataType & ~DataTypes.Store7BitEncoded) == DataTypes.Null)
                        descriptor.ApplyAttributes(EnsureAttributes(br, descriptor));
                }

                bool addToCache = knownElementType == null || !IsValueType(knownElementType);

                // 2.) supported non-collection type
                if (descriptor.CollectionDataType == DataTypes.Null)
                    return ReadObject(br, addToCache, descriptor);

                // 3/a.) array
                if (descriptor.IsArray)
                    return CreateArray(br, addToCache, descriptor);

                // 3/b.) non-array collection or key-value
                return CreateCollection(br, addToCache, descriptor);
            }

            [SecurityCritical]
            internal object ReadElement(BinaryReader br, DataTypeDescriptor elementDescriptor)
            {
                // single element
                if (!elementDescriptor.IsCollection)
                    return ReadObject(br, null, elementDescriptor);

                // nested collection: full recursion because an actual collection instance can have a derived type
                if (elementDescriptor.IsNullable && !br.ReadBoolean())
                    return null;
                return ReadWithType(br, elementDescriptor);
            }

            /// <summary>
            /// Reads a type from the serialization stream.
            /// <paramref name="allowOpenTypes"/> can be <see langword="true"/> only when type is deserialized as an instance.
            /// </summary>
            internal DataTypeDescriptor ReadType(BinaryReader br, bool allowOpenTypes)
            {
                DataTypeDescriptor result;
                Type type;

                // type index
                int index = Read7BitInt(br);

                // DataTypes encoded type
                if (index == EncodedTypeIndex)
                {
                    DataTypes dataType = ReadDataType(br);
                    result = new DataTypeDescriptor(null, dataType, br);
                    result.DecodeType(br, this, allowOpenTypes);
                    CachedTypes.Add(result);
                    return result;
                }

                // known type
                if (index != NewTypeIndex)
                {
                    Debug.Assert(index >= 0 && index < CachedTypes.Count, "Invalid type index");
                    result = CachedTypes[index];
                    type = result.Type;
                    if (type.IsGenericTypeDefinition)
                        result = HandleGenericTypeDef(br, result, allowOpenTypes);
                    else if (type == genericMethodDefinitionPlaceholderType)
                        result = HandleGenericMethodParameter(br);
                    return result;
                }

                // new type: assembly index
                index = Read7BitInt(br);

                // new assembly: assembly and type names are both stored as strings
                if (index == NewAssemblyIndex)
                {
                    // assembly qualified name (GetType uses binder if set)
                    string storedAssemblyName = br.ReadString();
                    string storedTypeName = br.ReadString();
                    type = GetType(storedAssemblyName, storedTypeName);
                    result = new DataTypeDescriptor(type, new TypeByString(storedAssemblyName, storedTypeName));
                    CachedAssemblies.Add((type.Assembly, storedAssemblyName));
                    CachedTypes.Add(result);
                    if (type.IsGenericTypeDefinition)
                        result = HandleGenericTypeDef(br, result, allowOpenTypes);
                    return result;
                }

                (Assembly Assembly, string StoredName) assembly = default;

                // type with known or omitted assembly: only type name is stored as string
                if (index != OmitAssemblyIndex)
                {
                    Debug.Assert(index >= 0 && index < CachedAssemblies.Count, "Invalid assembly index");
                    assembly = CachedAssemblies[index];
                }

                string typeName = br.ReadString();
                type = ReadBoundType(assembly.StoredName, typeName)
                    ?? (assembly.Assembly == null ? Reflector.ResolveType(typeName) : Reflector.ResolveType(assembly.Assembly, typeName))
                    ?? throw new SerializationException(Res.BinarySerializationCannotResolveType(typeName));

                result = new DataTypeDescriptor(type, new TypeByString(assembly.StoredName, typeName));
                CachedTypes.Add(result);
                if (type.IsGenericTypeDefinition)
                    result = HandleGenericTypeDef(br, result, allowOpenTypes);

                return result;
            }

            internal DataTypeDescriptor HandleGenericTypeDef(BinaryReader br, DataTypeDescriptor descriptor, bool allowOpenTypes, bool addToCache = true)
            {
                Type typeDef = descriptor.Type;
                Type[] args = typeDef.GetGenericArguments();
                int len = args.Length;
                DataTypeDescriptor result;

                if (allowOpenTypes)
                {
                    switch ((GenericTypeSpecifier)br.ReadByte())
                    {
                        case GenericTypeSpecifier.TypeDefinition:
                            return descriptor;
                        case GenericTypeSpecifier.GenericParameter:
                            {
                                var index = Read7BitInt(br);
                                if (index < 0 || index >= len)
                                    throw new SerializationException(Res.BinarySerializationInvalidStreamData);
                                result = new DataTypeDescriptor(typeDef.GetGenericArguments()[index]);
                                if (addToCache)
                                    CachedTypes.Add(result);
                                return result;
                            }
                        case GenericTypeSpecifier.ConstructedType:
                            break;
                        default:
                            throw new SerializationException(Res.BinarySerializationInvalidStreamData);
                    }
                }

                // special handling for compressible types
                if (typeDef == compressibleType)
                {
                    var argDescriptor = ReadType(br, allowOpenTypes);
                    args[0] = argDescriptor.Type;
                    result = new DataTypeDescriptor(typeDef.GetGenericType(args), argDescriptor.StoredType);
                }
                else
                {
                    // reading arguments
                    for (int i = 0; i < len; i++)
                        args[i] = ReadType(br, allowOpenTypes).Type;

                    result = new DataTypeDescriptor(typeDef.GetGenericType(args));
                }

                if (addToCache)
                    CachedTypes.Add(result);

                return result;
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

            [SecurityCritical]
            private object ReadRoot(BinaryReader br)
            {
                DataTypes dataType = ReadDataType(br);

                // 1.) null value
                if (dataType == DataTypes.Null)
                    return null;

                bool addToCache = false;
                DataTypeDescriptor descriptor;

                // 2.) supported non-collection type
                if (!IsCollectionType(dataType))
                {
                    // at root level id is written for recursive objects (collections are checked below)
                    if (GetUnderlyingSimpleType(dataType) == DataTypes.RecursiveObjectGraph)
                    {
                        addToCache = true;
                        if (TryGetCachedObject(br, out var _))
                            throw new InvalidOperationException(Res.InternalError("Root level object is not expected in the cache"));
                    }

                    descriptor = new DataTypeDescriptor(null, dataType, br);
                    descriptor.DecodeType(br, this);
                    return ReadObject(br, addToCache, descriptor);
                }

                // 3.) Collection
                descriptor = new DataTypeDescriptor(null, dataType, br);
                descriptor.DecodeType(br, this);
                if (descriptor.CanHaveRecursion)
                {
                    CachedTypes.Add(descriptor);
                    addToCache = true;
                    if (TryGetCachedObject(br, out var _))
                        throw new InvalidOperationException(Res.InternalError("Root level object is not expected in the cache"));
                }

                // 3/a.) array
                if (descriptor.IsArray)
                    return CreateArray(br, addToCache, descriptor);

                // 3/b.) non-array collection or key-value
                return CreateCollection(br, addToCache, descriptor);
            }

            private Type ReadBoundType(string assemblyName, string typeName)
            {
                if (Binder is ISerializationBinder binder)
                    return binder.BindToType(assemblyName ?? String.Empty, typeName);
                return Binder?.BindToType(assemblyName ?? String.Empty, typeName);
            }

            private DataTypeDescriptor HandleGenericMethodParameter(BinaryReader br)
            {
                Type declaringType = ReadType(br, true).Type;
                string signature = br.ReadString();
                MethodInfo method = declaringType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(mi => mi.ToString() == signature);
                if (method == null)
                    throw new SerializationException(Res.BinarySerializationGenericMethodNotFound(signature, declaringType));
                int argIndex = Read7BitInt(br);
                Type[] args = method.GetGenericArguments();
                DataTypeDescriptor result = argIndex < 0 || argIndex >= args.Length
                    ? throw new SerializationException(Res.BinarySerializationInvalidStreamData)
                    : new DataTypeDescriptor(args[argIndex]);

                CachedTypes.Add(result);
                return result;
            }

            /// <summary>
            /// Creates and populates array
            /// </summary>
            [SecurityCritical]
            private object CreateArray(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                // creating the array
                int rank = descriptor.Type.GetArrayRank();
                int[] lengths = new int[rank];
                int[] lowerBounds = new int[rank];
                for (int i = 0; i < rank; i++)
                {
                    if (descriptor.Rank != 0)
                        lowerBounds[i] = Read7BitInt(br);
                    lengths[i] = Read7BitInt(br);
                }

                Type elementType = descriptor.ElementDescriptor.Type;
                Array result = Array.CreateInstance(elementType, lengths, lowerBounds);
                if (addToCache)
                    AddObjectToCache(result);

                // primitive array
                if (elementType.IsPrimitive)
                {
                    int length = Buffer.ByteLength(result);
                    Buffer.BlockCopy(br.ReadBytes(length), 0, result, 0, length);
                    return result;
                }

                // 1D array
                if (lengths.Length == 1)
                {
                    int offset = lowerBounds[0];
                    for (int i = 0; i < result.Length; i++)
                    {
                        object value = ReadElement(br, descriptor.ElementDescriptor);
                        result.SetValue(value, i + offset);
                    }

                    return result;
                }

                // multidimensional array
                var arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
                while (arrayIndexer.MoveNext())
                {
                    object value = ReadElement(br, descriptor.ElementDescriptor);
                    result.SetValue(value, arrayIndexer.Current);
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
                if (descriptor.ParentDescriptor != null && !IsValueType(descriptor))
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
                        object element = ReadElement(br, descriptor.ElementDescriptor);
                        object value = descriptor.IsDictionary ? ReadElement(br, descriptor.ValueDescriptor) : null;

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
            /// <param name="dataTypeDescriptor">The descriptor of the data type to be deserialized.</param>
            /// <returns>The deserialized object.</returns>
            [SecurityCritical]
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Long but very straightforward switch")]
            private object ReadObject(BinaryReader br, bool? addToCache, DataTypeDescriptor dataTypeDescriptor)
            {
                bool TryGetFromCache(out object cachedValue)
                {
                    if (dataTypeDescriptor.ParentDescriptor == null)
                    {
                        cachedValue = null;
                        return false;
                    }

                    Debug.Assert(addToCache == true, "Reference element types of collections should be cached");
                    return TryGetCachedObject(br, out cachedValue);
                }

                DataTypes dataType = dataTypeDescriptor.ElementDataType;
                bool is7BitEncoded = IsCompressed(dataType);

                // nullable type
                if (IsNullable(dataType))
                {
                    // there is no nullable type on root level so checking for null in any case (occurs in collection and type members)
                    if (!br.ReadBoolean())
                        return null;
                }

                if (dataTypeDescriptor.ParentDescriptor != null && IsImpureTypeButEnum(dataType))
                    EnsureAttributes(br, dataTypeDescriptor);
                if (addToCache == null)
                    addToCache = !IsValueType(dataTypeDescriptor);

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
                        case DataTypes.Void: // though it does not really make sense as an instance, even BinaryFormatter supports it
                            if (!Reflector.TryCreateUninitializedObject(Reflector.VoidType, out createdResult))
                                throw new NotSupportedException(Res.BinarySerializationCannotCreateUninitializedObject(Reflector.VoidType));
                            return createdResult;
                        case DataTypes.Object:
                            // object - returning object instance on root level, otherwise, doing recursion because can mean any type as an element type
                            if (dataTypeDescriptor.ParentDescriptor == null)
                                return createdResult = new object();
                            // result is not set here - when caching is needed, will be done in the recursion
                            return ReadWithType(br);
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
                        case DataTypes.RuntimeType:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = ReadType(br, true).Type;

                        case DataTypes.BinarySerializable:
                            return ReadBinarySerializable(br, addToCache.Value, dataTypeDescriptor);
                        case DataTypes.RecursiveObjectGraph:
                            return ReadObjectGraph(br, addToCache.Value, dataTypeDescriptor);
                        case DataTypes.RawStruct:
                            return createdResult = ReadValueType(br, dataTypeDescriptor);
                        default:
                            if (IsEnum(dataType))
                                return createdResult = ReadEnum(br, dataTypeDescriptor);
                            throw new InvalidOperationException(Res.BinarySerializationCannotDeserializeObject(DataTypeToString(dataType)));
                    }
                }
                finally
                {
                    if (addToCache.Value && createdResult != null)
                        AddObjectToCache(createdResult);
                }
            }

            [SecurityCritical]
            private object ReadBinarySerializable(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                // checking instance id
                if (descriptor.ParentDescriptor != null && !IsValueType(descriptor))
                {
                    if (TryGetCachedObject(br, out object cachedResult))
                        return cachedResult;
                }

                // actual type if needed
                Type type = descriptor.ParentDescriptor != null && !IsSealed(descriptor)
                    ? ReadType(br, false).Type
                    : descriptor.Type;

                // deserialize (object will be cached immediately after creation so circular references will be found in time)
                type = Nullable.GetUnderlyingType(type) ?? type;
                if (!binarySerializableType.IsAssignableFrom(type))
                    throw new SerializationException(Res.BinarySerializationNotBinarySerializable(type));
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

            /// <summary>
            /// Deserializing object graph with options that was used on serialization.
            /// </summary>
            [SecurityCritical]
            private object ReadObjectGraph(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                // When element types may differ, reading element with data type
                if (descriptor.ParentDescriptor != null && !IsSealed(descriptor))
                    return ReadWithType(br);

                // checking instance id
                if (descriptor.ParentDescriptor != null && !IsValueType(descriptor))
                {
                    if (TryGetCachedObject(br, out object cachedResult))
                        return cachedResult;
                }

                // deserialize
                Type type = Nullable.GetUnderlyingType(descriptor.Type) ?? descriptor.Type;

                // a.) IsDefault flag
                bool isCustomObjectGraph = IsCustomSerialized(br, descriptor);

                // b.) Types of custom serialized objects are always explicitly stored
                bool isSealedElement = descriptor.ParentDescriptor != null && IsSealed(descriptor);
                if (isCustomObjectGraph && isSealedElement)
                    type = ReadType(br, false).Type;

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
                if (!isCustomObjectGraph)
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
                        object value = ReadWithType(br);

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
                            ReadWithType(br);
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
                    object value = ReadWithType(br);
                    Type elementType = ReadType(br, false).Type;
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
                var existingNames = new Dictionary<string, int>();
                do
                {
                    // reading fields of current level
                    int count = Read7BitInt(br);
                    for (int i = 0; i < count; i++)
                    {
                        string name = br.ReadString();

                        // conflicting names can occur if there are fields of the same name in the base class
                        int usedCount = existingNames.GetValueOrDefault(name);
                        if (usedCount == 0)
                            existingNames[name] = 1;
                        else
                        {
                            existingNames[name] = ++usedCount;
                            name += usedCount.ToString(CultureInfo.InvariantCulture);
                        }

                        object value = ReadWithType(br);
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
                // Default object graph allows duplicate names but custom doesn't. We handle possible duplicates the
                // same way as in ReadDefaultObjectGraphAsCustom. Though it is not a guarantee for anything.
                Dictionary<string, FieldInfo> fields = SerializationHelper.GetFieldsWithUniqueNames(obj.GetType(), true);

                // Reading the custom content and trying to identify them as fields
                int count = Read7BitInt(br);
                for (int i = 0; i < count; i++)
                {
                    string name = br.ReadString();
                    object value = ReadWithType(br);
                    ReadType(br, false); // the element type, which is ignored now

                    if (fields.TryGetValue(name, out FieldInfo field))
                    {
                        TrySetField(field, obj, value);
                        continue;
                    }

                    if (!IgnoreObjectChanges)
                        throw new SerializationException(Res.BinarySerializationMissingField(obj.GetType(), name));
                }
            }

            [SecurityCritical]
            private object ReadValueType(BinaryReader br, DataTypeDescriptor descriptor)
            {
                Type structType = Nullable.GetUnderlyingType(descriptor.Type) ?? descriptor.Type;
                if (!structType.IsValueType)
                    throw new SerializationException(Res.BinarySerializationNotAValueType(structType));
                byte[] rawData = br.ReadBytes(Read7BitInt(br));
                object result = BinarySerializer.DeserializeValueType(structType, rawData);
                OnDeserializing(result);
                OnDeserialized(result);
                return result;
            }

            private void OnDeserializing(object obj) => ExecuteMethodsOfAttribute(obj, onDeserializingAttribute);

            private void OnDeserialized(object obj)
            {
                if (IgnoreSerializationMethods)
                    return;
                ExecuteMethodsOfAttribute(obj, onDeserializedAttribute);
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
                if (!IgnoreIObjectReference && value is IObjectReference objRef)
                {
                    // the object reference cannot be set yet so storing the new usage of the reference to be set later.
                    if (objectReferences == null)
                        objectReferences = new Dictionary<IObjectReference, List<KeyValuePair<FieldInfo, object>>>(1, ReferenceEqualityComparer<IObjectReference>.Comparer);

                    if (!objectReferences.TryGetValue(objRef, out List<KeyValuePair<FieldInfo, object>> refUsages))
                    {
                        refUsages = new List<KeyValuePair<FieldInfo, object>>();
                        objectReferences.Add(objRef, refUsages);
                    }

                    refUsages.Add(new KeyValuePair<FieldInfo, object>(field, obj));
                    return;
                }

                field.Set(obj, value);
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
                    usage.Key.Set(usage.Value, realObject);

                objectReferences.Remove(objRef);
            }

            /// <summary>
            /// Resolves a type by string. Here neither the assembly nor the type is resolved yet.
            /// </summary>
            private Type GetType(string assemblyName, string typeName)
            {
                string key = assemblyName + ":" + typeName;
                if (typeByNameCache != null && typeByNameCache.TryGetValue(key, out Type result))
                    return result;

                result = ReadBoundType(assemblyName, typeName);
                if (result != null)
                {
                    AddTypeToCache(key, result);
                    return result;
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

            private bool IsCustomSerialized(BinaryReader br, DataTypeDescriptor descriptor)
            {
                var attr = EnsureAttributes(br, descriptor);
                return (attr & TypeAttributes.CustomSerialized) != TypeAttributes.None;
            }

            private TypeAttributes EnsureAttributes(BinaryReader br, DataTypeDescriptor descriptor)
            {
                MemberInfo type = (MemberInfo)descriptor.StoredType ?? descriptor.Type;
                if (TypeAttributesCache.TryGetValue(type, out TypeAttributes result))
                    return result;
                result = (TypeAttributes)br.ReadByte();
                if (!result.AllFlagsDefined())
                    throw new SerializationException(Res.BinarySerializationInvalidStreamData);
                TypeAttributesCache.Add(type, result);
                return result;
            }

            #endregion

            #endregion

            #endregion
        }
    }
}
