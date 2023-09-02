#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.DeserializationManager.cs
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
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Runtime.Serialization;
using System.Security;
using System.Text;

using KGySoft.Annotations;
using KGySoft.CoreLibraries;
using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

#region Used Aliases

using ReferenceEqualityComparer = KGySoft.CoreLibraries.ReferenceEqualityComparer;
using System.Xml.Linq;

#endregion

#endregion

#region Suppressions

#if NET8_0_OR_GREATER
#pragma warning disable SYSLIB0050 // ISurrogateSelector/ISerializationSurrogate is obsolete - needed by IFormatter implementation, which is maintained for compatibility reasons
#endif

#endregion

namespace KGySoft.Serialization.Binary
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// A manager class that provides that stored types will be built up in the same order both at serialization and deserialization for complex types.
        /// </summary>
        private sealed class DeserializationManager : SerializationManagerBase
        {
            #region Nested Types
            
            #region Nested classes

            #region UsageReferences class

            private sealed class UsageReferences : List<UsageReference>
            {
                #region Properties

                internal bool CanBeReplaced { get; set; } = true;

                #endregion

                #region Constructors

                internal UsageReferences() : base(1)
                {
                }

                #endregion
            }

            #endregion

            #region UsageReference class

            private abstract class UsageReference
            {
                #region Methods

                internal abstract void SetValue(object value);

                #endregion
            }

            #endregion

            #region FieldUsage class

            private sealed class FieldUsage : UsageReference
            {
                #region Fields

                private readonly object target;
                private readonly FieldInfo field;

                #endregion

                #region Constructors

                internal FieldUsage(object target, FieldInfo field)
                {
                    this.target = target;
                    this.field = field;
                }

                #endregion

                #region Methods

                internal override void SetValue(object value) => field.Set(target, value);

                #endregion
            }

            #endregion

            #region ArrayUsage class

            private sealed class ArrayUsage : UsageReference
            {
                #region Fields

                private readonly Array target;
                private readonly int[] indices;

                #endregion

                #region Constructors

                internal ArrayUsage(Array target, int[] indices)
                {
                    Debug.Assert(indices.Length > 1);
                    this.target = target;
                    this.indices = indices;
                }

                #endregion

                #region Methods

                internal override void SetValue(object value) => target.SetValue(value, indices);

                #endregion
            }

            #endregion

            #region ListUsage class

            private sealed class ListUsage : UsageReference
            {
                #region Fields

                private readonly IList target;
                private readonly int index;

                #endregion

                #region Constructors

                internal ListUsage(IList target, int index)
                {
                    this.target = target;
                    this.index = index;
                }

                #endregion

                #region Methods

                internal override void SetValue(object value) => target[index] = value;

                #endregion
            }

            #endregion

            #region CollectionUsage class

            private sealed class CollectionUsage : UsageReference
            {
                #region Fields

                private readonly object target;
                private readonly MethodAccessor addMethod;

                #endregion

                #region Constructors

                internal CollectionUsage(object target, MethodAccessor addMethod)
                {
                    this.target = target;
                    this.addMethod = addMethod;
                }

                #endregion

                #region Methods

                internal override void SetValue(object value) => addMethod.Invoke(target, value);

                #endregion
            }

            #endregion

            #region LinkedListUsage class

            private sealed class LinkedListUsage : UsageReference
            {
                #region Fields

                private readonly IEnumerable target;
                private readonly object referenceNode;
                private readonly Type genericArg;

                #endregion

                #region Constructors

                internal LinkedListUsage(IEnumerable target, object referenceNode)
                {
                    this.target = target;
                    this.referenceNode = referenceNode;
                    genericArg = target.GetType().GetGenericArguments()[0];
                }

                #endregion

                #region Methods

                internal override void SetValue(object value)
                {
                    Accessors.InvokeMethod(target, nameof(LinkedList<_>.AddAfter), new[] { referenceNode.GetType(), genericArg }, referenceNode, value);
                    Accessors.InvokeMethod(target, nameof(LinkedList<_>.Remove), referenceNode);
                }

                #endregion
            }

            #endregion

            #region DictionaryKeyUsage class

            private sealed class DictionaryKeyUsage : UsageReference
            {
                #region Fields

                private readonly IDictionary target;
                private readonly object value;

                #endregion

                #region Constructors

                internal DictionaryKeyUsage(IDictionary target, object value)
                {
                    this.target = target;
                    this.value = value;
                }

                #endregion

                #region Methods

                internal override void SetValue(object key) => target[key] = value;

                #endregion
            }

            #endregion

            #region DictionaryValueUsage class

            private sealed class DictionaryValueUsage : UsageReference
            {
                #region Fields

                private readonly IDictionary target;
                private readonly object? key;

                #endregion

                #region Constructors

                internal DictionaryValueUsage(IDictionary target, object? key)
                {
                    // null key means it will be the same as the replaced value
                    this.target = target;
                    this.key = key;
                }

                #endregion

                #region Methods

                internal override void SetValue(object value) => target[key ?? value] = value;

                #endregion
            }

            #endregion

            #region OrderedDictionaryKeyUsage class

            private sealed class OrderedDictionaryKeyUsage : UsageReference
            {
                #region Fields

                private readonly IOrderedDictionary target;
                private readonly int index;

                #endregion

                #region Constructors

                internal OrderedDictionaryKeyUsage(IOrderedDictionary target, int index)
                {
                    this.target = target;
                    this.index = index;
                }

                #endregion

                #region Methods

                internal override void SetValue(object key)
                {
                    object? value = target[index];
                    target.RemoveAt(index);
                    target.Insert(index, key, value);
                }

                #endregion
            }

            #endregion

            #region OrderedDictionaryValueUsage class

            private sealed class OrderedDictionaryValueUsage : UsageReference
            {
                #region Fields

                private readonly IOrderedDictionary target;
                private readonly int index;

                #endregion

                #region Constructors

                internal OrderedDictionaryValueUsage(IOrderedDictionary target, int index)
                {
                    this.target = target;
                    this.index = index;
                }

                #endregion

                #region Methods

                internal override void SetValue(object value) => target[index] = value;

                #endregion
            }

            #endregion

            #endregion

            #region ArrayBuilder struct

            private struct ArrayBuilder
            {
                #region Fields

                #region Internal Fields
                
                internal readonly int TotalLength;
                internal Array? Array;
                internal Dictionary<object, UsageReferences>? ObjectsBeingDeserialized;

                #endregion

                #region Private Fields
                
                private readonly BinaryReader reader;
                private readonly DataTypeDescriptor descriptor;
                private readonly int[] lengths;
                private readonly int[] lowerBounds;

                private ArrayIndexer? arrayIndexer;
                private IList? builder;
                private int current;

                #endregion

                #endregion

                #region Properties
                
                internal object ArrayProxy => (Array ?? builder)!;

                #endregion

                #region Constructors
                
                internal ArrayBuilder(BinaryReader br, DataTypeDescriptor descriptor, bool safeMode) : this()
                {
                    reader = br;
                    this.descriptor = descriptor;
                    current = -1;

                    // it is always safe to allocate by rank because it cannot be > 32 when obtained by a type
                    int rank = descriptor.IsArray ? descriptor.Type!.GetArrayRank() : 1; // IsArray is false for array backed collections
                    lengths = new int[rank];
                    lowerBounds = new int[rank];
                    TotalLength = 1;
                    int fixLength = descriptor.FixedItemsSize;
                    if (fixLength > 0)
                        lengths[0] = TotalLength = fixLength;
                    else
                    {
                        for (int i = 0; i < rank; i++)
                        {
                            if (descriptor.Rank != 0)
                                lowerBounds[i] = Read7BitInt(br);
                            int len = Read7BitInt(br);
                            lengths[i] = len;
                            TotalLength = safeMode
                                ? checked(TotalLength * len)
                                : unchecked(TotalLength * len);
                        }
                    }

                    // trying to allocate the result array at once if possible
                    Type elementType = descriptor.CreateResultFromByteArray ? Reflector.ByteType : descriptor.GetElementDescriptor().Type!;

                    int elementSize;
                    if (!safeMode || ((elementSize = elementType.SizeOf()) * (long)TotalLength) <= ArrayAllocationThreshold)
                    {
                        Array = Array.CreateInstance(elementType, lengths, lowerBounds);
                        if (rank > 1)
                            arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
                        return;
                    }

                    // otherwise, allocating just a List with limited initial capacity
                    int capacity = Math.Min(TotalLength, ArrayAllocationThreshold / elementSize);
                    if (elementType.IsPrimitive || elementType.IsValueType && !typeof(IObjectReference).IsAssignableFrom(elementType))
                    {
                        // for primitive types and non-IObjectReference value type we use a strictly typed list
                        builder = (IList)Reflector.ListGenType.GetGenericType(elementType).CreateInstance(Reflector.IntType, capacity);
                        return;
                    }

                    // for reference types or IObjectReference implementations just using an object list
                    builder = new List<object>(capacity);
                }

                #endregion

                #region Methods

                #region Internal Methods
                
                internal bool TryReadPrimitive()
                {
                    if (Array == null || !descriptor.GetElementDescriptor().Type!.IsPrimitive)
                        return false;
                    int byteLength = Buffer.ByteLength(Array);
#if NET6_0_OR_GREATER
                    // reinterpreting the primitive array as Span<byte>
                    if (reader.Read(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(Array), byteLength)) < byteLength)
                        Throw.SerializationException(Res.BinarySerializationDataLengthTooSmall);
                    return true;
#else
                    // preventing an allocation and copying if the result is a byte array anyway
                    if (Array is byte[] byteArray) // or sbyte[] but in this case it's ok to handle that, too
                    {
                        if (reader.Read(byteArray, 0, byteLength) < byteLength)
                            Throw.SerializationException(Res.BinarySerializationDataLengthTooSmall);
                        return true;
                    }

                    Buffer.BlockCopy(reader.ReadBytes(byteLength), 0, Array, 0, byteLength);
                    return true;
#endif
                }

                internal Array ToArray()
                {
                    if (Array != null)
                        return Array;

                    Debug.Assert(builder!.Count == TotalLength);
                    Array = Array.CreateInstance(descriptor.GetElementDescriptor().Type!, lengths, lowerBounds);

                    // 1D array
                    if (lengths.Length == 1)
                    {
                        int offset = lowerBounds[0];
                        for (int i = 0; i < TotalLength; i++)
                            SetArrayElement(builder![i], i + offset);

                        builder = null;
                        return Array;
                    }

                    // multidimensional array
                    arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
                    for (int i = 0; i < TotalLength && arrayIndexer.MoveNext(); i++)
                        SetArrayElement(builder![i], arrayIndexer.Current);

                    builder = null;
                    return Array;
                }

                internal void Add(object? value)
                {
                    // adding to the final array
                    if (Array != null)
                    {
                        // 1D array
                        if (arrayIndexer == null)
                        {
                            SetArrayElement(value, ++current + lowerBounds[0]);
                            return;
                        }

                        // Multidimensional array
                        arrayIndexer.MoveNext();
                        SetArrayElement(value, arrayIndexer.Current);
                        return;
                    }

                    // appending the builder
                    builder!.Add(value);
                }

                #endregion

                #region Private Methods
                
                private void SetArrayElement(object? value, int[] indices)
                {
                    Debug.Assert(indices.Length > 1);
                    UsageReferences? trackedUsages = value == null ? null : ObjectsBeingDeserialized?.GetValueOrDefault(value);
                    if (trackedUsages == null)
                    {
                        Array!.SetValue(value, indices);
                        return;
                    }

                    trackedUsages.Add(new ArrayUsage(Array!, indices));
                }

                private void SetArrayElement(object? value, int index)
                {
                    UsageReferences? trackedUsages = value == null ? null : ObjectsBeingDeserialized?.GetValueOrDefault(value);
                    if (trackedUsages == null)
                    {
                        Array!.SetValue(value, index);
                        return;
                    }

                    trackedUsages.Add(new ListUsage(Array!, index));
                }

                #endregion

                #endregion
            }

            #endregion

            #endregion

            #region Fields

            private readonly Type? rootType;
            private readonly Dictionary<(string? AssemblyName, string TypeName), Type>? expectedTypes;

            private List<string>? cachedNames;
            private List<(Assembly?, string?)>? cachedAssemblies;
            private List<DataTypeDescriptor>? cachedTypes;
            private StringKeyedDictionary<Assembly>? assemblyByNameCache;
            private Dictionary<int, object?>? idCache;
            private Dictionary<object, UsageReferences>? objectsBeingDeserialized;
            private List<IDeserializationCallback>? deserializationRegObjects;

            #endregion

            #region Properties

            private Dictionary<int, object?> IdCache => idCache ??= new Dictionary<int, object?> { { 0, null } };

            private List<(Assembly? Assembly, string? StoredName)> CachedAssemblies
                => cachedAssemblies ??= new List<(Assembly?, string?)>(KnownAssemblies.Select(a => ((Assembly?)a, a == AssemblyResolver.CoreLibrariesAssembly ? null : a.FullName)));

            private List<DataTypeDescriptor> CachedTypes
                => cachedTypes ??= new List<DataTypeDescriptor>(KnownTypes.Select(t => new DataTypeDescriptor(t)));

            private Dictionary<object, UsageReferences> ObjectsBeingDeserialized => objectsBeingDeserialized
                ??= new Dictionary<object, UsageReferences>(1, ReferenceEqualityComparer.Comparer);

            private int OmitAssemblyIndex => CachedAssemblies.Count;
            private int NewAssemblyIndex => CachedAssemblies.Count + 1;
            private int NewTypeIndex => CachedTypes.Count + 1;
            private int EncodedTypeIndex => CachedTypes.Count + 2;

            #endregion

            #region Constructors

            internal DeserializationManager(StreamingContext context, BinarySerializationOptions options, SerializationBinder? binder, ISurrogateSelector? surrogateSelector,
                IEnumerable<Type>? expectedCustomTypes, Type rootType)
                : base(context,
                    // Considering only deserialization flags. Other info must be read from the stream.
                    options & (BinarySerializationOptions.IgnoreSerializationMethods 
                        | BinarySerializationOptions.IgnoreObjectChanges 
                        | BinarySerializationOptions.IgnoreISerializable 
                        | BinarySerializationOptions.IgnoreIObjectReference
                        | BinarySerializationOptions.SafeMode
                        | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes),
                    binder, surrogateSelector)
            {
                this.rootType = rootType == Reflector.ObjectType ? null : rootType;

                if (SafeMode)
                {
                    // Further checks in safe mode
                    if (surrogateSelector != null)
                        Throw.SerializationException(Res.BinarySerializationSurrogateNotAllowedInSafeMode);
                    if (binder is not (null or ForwardedTypesSerializationBinder { SafeMode: true }))
                        Throw.SerializationException(Res.BinarySerializationBinderNotAllowedInSafeMode);
                    if (binder != null)
                        return;
                }

                expectedTypes = new Dictionary<(string?, string), Type>();

                // Safe mode without a safe binder: adding allowed types
                var types = new Queue<Type>(new[] { rootType }.Concat(expectedCustomTypes ?? Reflector.EmptyArray<Type>()));
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
                while (types.TryDequeue(out Type? type))
#else
                while (types.Count > 0)
#endif
                {
#if !(NETCOREAPP || NETSTANDARD2_1_OR_GREATER)
                    var type = types.Dequeue(); 
#endif
                    if (type == null!)
                        Throw.ArgumentException(Argument.expectedCustomTypes, Res.ArgumentContainsNull);

                    while (type!.HasElementType)
                        type = type.GetElementType();

                    if (type.IsConstructedGenericType())
                    {
                        foreach (Type arg in type.GetGenericArguments())
                            types.Enqueue(arg);
                        type = type.GetGenericTypeDefinition();
                    }

                    // skipping primitive types that are never resolved by name
                    if (primitiveTypes.ContainsKey(type))
                        continue;

                    Assembly asm = type.Assembly;
                    string? asmName = asm.FullName;
                    string typeName = type.FullName!;
                    if (typeName == null!)
                        Throw.ArgumentException(Argument.expectedCustomTypes, Res.ArgumentInvalid);

                    // actual identity
                    expectedTypes[(asmName, typeName)] = type;

                    // forwarded identity
                    (string? ForwardedAssemblyName, bool IsCoreIdentity) identity = AssemblyResolver.GetForwardedAssemblyName(type);
                    if (identity.ForwardedAssemblyName != null)
                        expectedTypes[(identity.ForwardedAssemblyName, typeName)] = type;

                    // core assembly (either by forwarded or current identity): adding also without assembly name
                    if (identity.IsCoreIdentity || asm == AssemblyResolver.CoreLibrariesAssembly)
                        expectedTypes[(null, typeName)] = type;
                }
            }

            #endregion

            #region Methods

            #region Static Methods

            #region Internal Methods

            /// <summary>
            /// Use for known types only
            /// </summary>
            [SecurityCritical]
            internal static object CreateKnownEmptyObject(Type type)
            {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);

#if NETFRAMEWORK || NETSTANDARD2_0
                if (!Reflector.TryCreateUninitializedObject(type, out object? obj))
                    Throw.SerializationException(Res.BinarySerializationCannotCreateUninitializedObject(type));
                return obj;
#else
                return RuntimeHelpers.GetUninitializedObject(type);
#endif
            }

            #endregion

            #region Private Methods

            private static object ReadEnum(BinaryReader br, DataTypeDescriptor descriptor)
            {
                Type enumType = Nullable.GetUnderlyingType(descriptor.Type!) ?? descriptor.Type!;
                if (!enumType.IsEnum)
                    Throw.SerializationException(Res.BinarySerializationNotAnEnum(enumType));
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
                        return Throw.InvalidOperationException<object>(Res.BinarySerializationInvalidEnumBase(DataTypeToString(GetUnderlyingSimpleType(dataType))));
                }
            }

            private static Version ReadVersion(BinaryReader br)
            {
                int major = br.ReadInt32();
                int minor = br.ReadInt32();
                int build = br.ReadInt32();
                int revision = br.ReadInt32();
                return revision == -1 ? new Version(major, minor)
                    : build == -1 ? new Version(major, minor, build)
                    : new Version(major, minor, build, revision);
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            private static Index ReadIndex(BinaryReader br)
            {
                int value = br.ReadInt32();
                return new Index(value < 0 ? ~value : value, value < 0);
            }
#endif

#if NET5_0_OR_GREATER
            private static Half ReadHalf(BinaryReader br)
            {
                ushort value = br.ReadUInt16();
                return Unsafe.As<ushort, Half>(ref value);
            }
#endif

#if NET7_0_OR_GREATER
            [SecurityCritical]
            private unsafe static Int128 ReadInt128(BinaryReader br)
            {
                Span<byte> bytes = stackalloc byte[sizeof(Int128)];
                br.BaseStream.ReadExactly(bytes);
                return Unsafe.As<byte, Int128>(ref MemoryMarshal.GetReference(bytes));
            }

            [SecurityCritical]
            private unsafe static UInt128 ReadUInt128(BinaryReader br)
            {
                Span<byte> bytes = stackalloc byte[sizeof(UInt128)];
                br.BaseStream.ReadExactly(bytes);
                return Unsafe.As<byte, UInt128>(ref MemoryMarshal.GetReference(bytes));
            }
#endif

            private static void ApplyPendingUsages(UsageReferences usages, object origObject, object? finalObject)
            {
                if (!usages.CanBeReplaced && origObject != finalObject)
                {
                    if (origObject is IObjectReference)
                        Throw.SerializationException(Res.BinarySerializationCircularIObjectReference);
                    Throw.SerializationException(Res.BinarySerializationSurrogateChangedObject(finalObject!.GetType()));
                }

                if (usages.Count == 0)
                    return;

                if (finalObject == null)
                    Throw.SerializationException(Res.BinarySerializationCircularIObjectReference);

                // setting even if it did not change because in most cases the tracked objects were not set during the deserialization
                foreach (UsageReference usage in usages)
                    usage.SetValue(finalObject);
            }

            private static object? GetPlaceholderValue(object? value, [NoEnumeration]IEnumerable collection)
                => value is IObjectReference ? collection.GetType().GetCollectionElementType()!.GetDefaultValue() : value;

            #endregion

            #endregion

            #region Instance Methods

            #region Internal Methods

            [SecurityCritical]
            internal object? Deserialize(BinaryReader br)
            {
                try
                {
                    object? result = ReadRoot(br);

                    // checking the result
                    if (rootType is not null)
                    {
                        if (result is null)
                        {
                            if (rootType.IsValueType && !rootType.IsNullable())
                                Throw.SerializationException(Res.SerializationNonNullResultExpected(rootType));
                        }
                        else if (!rootType.IsInstanceOfType(result))
                            Throw.SerializationException(Res.SerializationUnexpectedResult(rootType, result.GetType()));
                    }

                    DeserializationCallback();
                    return result;
                }
                catch (Exception e) when (!e.IsCriticalOr(e is SerializationException || e is NotSupportedException))
                {
                    return Throw.SerializationException<object>(Res.BinarySerializationInvalidStreamData, e);
                }
            }

            [SecurityCritical]
            internal object? ReadWithType(BinaryReader br, DataTypeDescriptor? knownElementType = null)
            {
                // 1.) getting whether the current instance is in cache
                if (knownElementType == null || !IsValueType(knownElementType))
                {
                    if (TryGetCachedObject(br, out object? cachedResult))
                        return cachedResult;
                }

                DataTypeDescriptor? descriptor = knownElementType != null && IsSealed(knownElementType) ? knownElementType : null;

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

                // 3/b.) tuple
                if (descriptor.IsTuple)
                    return CreateTuple(br, addToCache, descriptor);

                // 3/c.) array-backed collection
                if (descriptor.HasBackingArray)
                    return CreateArrayBackedCollection(br, addToCache, descriptor);

                // 3/d.) other collection or key-value
                return CreateCollection(br, addToCache, descriptor);
            }

            private string ReadName(BinaryReader br)
            {
                var names = cachedNames ??= new List<string>();
                int id = Read7BitInt(br);
                if ((uint)id > names.Count)
                    Throw.SerializationException(Res.BinarySerializationInvalidStreamData);
                if (id < names.Count)
                    return names[id];
                string name = br.ReadString();
                names.Add(name);
                return name;
            }

            /// <summary>
            /// Reads a type from the serialization stream.
            /// <paramref name="allowOpenTypes"/> can be <see langword="true"/> only when type is deserialized as an instance.
            /// </summary>
            internal DataTypeDescriptor ReadType(BinaryReader br, bool allowOpenTypes)
            {
                DataTypeDescriptor result;
                Type? type;

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
                    type = result.Type!;
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
                    // assembly qualified name
                    string storedAssemblyName = br.ReadString();
                    string storedTypeName = br.ReadString();
                    result = ReadNewTypeWithAssembly(storedAssemblyName, storedTypeName);
                    type = result.Type!;
                    if (type.IsGenericTypeDefinition)
                        result = HandleGenericTypeDef(br, result, allowOpenTypes);
                    return result;
                }

                (Assembly? Assembly, string? StoredName) assembly = default;

                // type with known or omitted assembly: only type name is stored as string
                if (index != OmitAssemblyIndex)
                {
                    Debug.Assert(index >= 0 && index < CachedAssemblies.Count, "Invalid assembly index");
                    assembly = CachedAssemblies[index];
                }

                string typeName = br.ReadString();
                type = ResolveType(assembly, typeName);
                result = new DataTypeDescriptor(type, new TypeByString(assembly.StoredName, typeName));
                CachedTypes.Add(result);
                if (type.IsGenericTypeDefinition)
                    result = HandleGenericTypeDef(br, result, allowOpenTypes);

                return result;
            }

            internal DataTypeDescriptor HandleGenericTypeDef(BinaryReader br, DataTypeDescriptor descriptor, bool allowOpenTypes, bool addToCache = true)
            {
                Type typeDef = descriptor.Type!;
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
                                    Throw.SerializationException(Res.BinarySerializationInvalidStreamData);
                                result = new DataTypeDescriptor(typeDef.GetGenericArguments()[index]);
                                if (addToCache)
                                    CachedTypes.Add(result);
                                return result;
                            }
                        case GenericTypeSpecifier.ConstructedType:
                            break;
                        default:
                            Throw.SerializationException(Res.BinarySerializationInvalidStreamData);
                            break;
                    }
                }

                // special handling for compressible types
                if (typeDef == compressibleType)
                {
                    var argDescriptor = ReadType(br, allowOpenTypes);
                    args[0] = argDescriptor.Type!;
                    result = new DataTypeDescriptor(typeDef.GetGenericType(args), argDescriptor.StoredType);
                }
                else
                {
                    // reading arguments
                    for (int i = 0; i < len; i++)
                        args[i] = ReadType(br, allowOpenTypes).Type!;

                    result = new DataTypeDescriptor(typeDef.GetGenericType(args));
                }

                if (addToCache)
                    CachedTypes.Add(result);

                return result;
            }

            internal void AddObjectToCache(object obj)
            {
                Dictionary<int, object?> cache = IdCache;
                cache.Add(cache.Count, obj);
            }

            internal void AddObjectToCache(object? obj, out int id)
            {
                Dictionary<int, object?> cache = IdCache;
                id = cache.Count;
                cache.Add(id, obj);
            }

            internal void ReplaceObjectInCache(int id, object? obj)
            {
                Dictionary<int, object?> cache = IdCache;
                cache[id] = obj;
            }

            #endregion

            #region Private Methods

            [SecurityCritical]
            private object? ReadRoot(BinaryReader br)
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
                            Throw.InternalError("Root level object is not expected in the cache");
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
                        Throw.InternalError(Res.InternalError("Root level object is not expected in the cache"));
                }

                // 3/a.) array
                if (descriptor.IsArray)
                    return CreateArray(br, addToCache, descriptor);

                // 3/b.) tuple
                if (descriptor.IsTuple)
                    return CreateTuple(br, addToCache, descriptor);

                // 3/c.) array-backed collection
                if (descriptor.HasBackingArray)
                    return CreateArrayBackedCollection(br, addToCache, descriptor);

                // 3/d.) other collection or key-value
                return CreateCollection(br, addToCache, descriptor);
            }

            private DataTypeDescriptor HandleGenericMethodParameter(BinaryReader br)
            {
                Type declaringType = ReadType(br, true).Type!;
                string signature = br.ReadString();
                MethodInfo? method = declaringType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(mi => mi.ToString() == signature);
                if (method == null)
                    Throw.SerializationException(Res.BinarySerializationGenericMethodNotFound(signature, declaringType));
                int argIndex = Read7BitInt(br);
                Type[] args = method.GetGenericArguments();
                if (argIndex < 0 || argIndex >= args.Length)
                    Throw.SerializationException(Res.BinarySerializationInvalidStreamData);
                var result = new DataTypeDescriptor(args[argIndex]);

                CachedTypes.Add(result);
                return result;
            }

            /// <summary>
            /// Creates and populates array
            /// </summary>
            [SecurityCritical]
            private Array CreateArray(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                // using a builder to prevent possible OutOfMemoryException attacks in SafeMode
                var builder = new ArrayBuilder(br, descriptor, SafeMode);
                object arrayProxy = builder.ArrayProxy;

                // if the builder uses a proxy, then the references of the array must be tracked because it will be replaced in the end
                bool trackUsages = addToCache && arrayProxy is not Array && descriptor.CanHaveRecursion;
                UsageReferences? usages = null;
                if (trackUsages)
                    ObjectsBeingDeserialized.Add(arrayProxy, usages = new UsageReferences());
                builder.ObjectsBeingDeserialized = objectsBeingDeserialized; // using the field here is intended so no unnecessary instance is created

                int id = 0;
                if (addToCache)
                    AddObjectToCache(arrayProxy, out id);

                // primitive array and the builder allocated the final capacity
                if (builder.TryReadPrimitive())
                {
                    Debug.Assert(!trackUsages && arrayProxy == builder.Array);
                    return builder.ToArray();
                }

                // non-primitive array or cannot read at once in safe mode
                DataTypeDescriptor elementDescriptor = descriptor.GetElementDescriptor();
                for (int i = 0; i < builder.TotalLength; i++)
                {
                    object? value = ReadElement(br, elementDescriptor);
                    builder.Add(value);
                }

                Array result = builder.ToArray();

                // some post administration if the array was built by a proxy
                if (trackUsages)
                {
                    ApplyPendingUsages(usages!, arrayProxy, result);

                    if (result != arrayProxy && addToCache)
                        ReplaceObjectInCache(id, result);

                    ObjectsBeingDeserialized.Remove(arrayProxy);
                }

                return result;
            }

            [SecurityCritical]
            private object CreateTuple(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                Debug.Assert(descriptor.IsTuple);
                Type type = descriptor.GetTypeToCreate();
                object result = CreateKnownEmptyObject(type);

                if (addToCache)
                    AddObjectToCache(result);

                FieldInfo[] fields = SerializationHelper.GetSerializableFields(type);
                for (int i = 0; i < fields.Length; i++)
                    SetField(fields[i], result, ReadElement(br, descriptor.GetTupleItemDescriptor(i)));

                return result;
            }

            [SecurityCritical]
            private object CreateArrayBackedCollection(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                Type type = descriptor.GetTypeToCreate();
                object result;

                // special case: backing array is null so creating a default instance
                if (descriptor.HasNullableBackingArray && !br.ReadBoolean())
                {
                    result = Activator.CreateInstance(type)!;
                    if (addToCache)
                        AddObjectToCache(result);
                    return result;
                }

                var builder = new ArrayBuilder(br, descriptor, SafeMode);

                // Unlike in CreateArray we always use a new object as a proxy here because the array is not the final object
                // and if the builder returns a real array its reference could mean both the final object and its backing array.
                // CanHaveRecursion is expected to return false if the backing array is just used to create the final object but does it not wrap the array (ie if can be an object[] it must be an actual backing array in the result)
                bool trackUsages = addToCache && descriptor.CanHaveRecursion;
                object? resultProxy = null;
                UsageReferences? usages = null;
                if (trackUsages)
                    ObjectsBeingDeserialized.Add(resultProxy = new object(), usages = new UsageReferences());
                builder.ObjectsBeingDeserialized = objectsBeingDeserialized; // using the field here is intended so no unnecessary instance is created

                int id = 0;
                if (addToCache)
                    AddObjectToCache(resultProxy, out id);

                Array backingArray;

                // primitive array and the builder allocated the final capacity
                if (builder.TryReadPrimitive())
                    backingArray = builder.ToArray();
                else
                {
                    // non-primitive array or cannot read at once in safe mode
                    DataTypeDescriptor elementDescriptor = descriptor.GetElementDescriptor();
                    for (int i = 0; i < builder.TotalLength; i++)
                    {
                        object? value = ReadElement(br, elementDescriptor);
                        builder.Add(value);
                    }

                    backingArray = builder.ToArray();
                }

                // creating the actual result
                Func<BinaryReader, Type, Array, object> callback = serializationInfo[descriptor.UnderlyingCollectionDataType].CreateArrayBackedCollectionInstanceFromArray!;
                result = callback.Invoke(br, type, backingArray);

                // some post administration if the result can recursively contain itself
                if (trackUsages)
                {
                    ApplyPendingUsages(usages!, resultProxy!, result);

                    if (addToCache)
                        ReplaceObjectInCache(id, result);

                    ObjectsBeingDeserialized.Remove(resultProxy!);
                }

                return result;
            }

            /// <summary>
            /// Creates and populates a collection
            /// </summary>
            [SecurityCritical]
            private object CreateCollection(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                Type type = descriptor.GetTypeToCreate();
                if (!(descriptor.IsSingleElement || descriptor.IsComparer) && !Reflector.IEnumerableType.IsAssignableFrom(type))
                    Throw.SerializationException(Res.BinarySerializationIEnumerableExpected(type!));

                DataTypes dataType = descriptor.CollectionDataType;
                CollectionSerializationInfo serInfo = serializationInfo[dataType];
                object result = serInfo.InitializeCollection(br, addToCache, descriptor, this, SafeMode, out int count); // TODO: out int id if a proxy will be legally replaceable once

                if (serInfo.IsComparer)
                    return result;

                if (serInfo.IsSingleElement)
                {
                    if (descriptor.IsStrongBox)
                        SetField(descriptor.Type!.GetField(nameof(StrongBox<_>.Value))!, result, ReadElement(br, descriptor.GetElementDescriptor()));
                    else
                    {
                        object? key = ReadElement(br, descriptor.GetKeyDescriptor());
                        object? value = ReadElement(br, descriptor.GetValueDescriptor());
                        SetKeyValue(result, key, value);
                    }

                    return result;
                }

                if (result is not IEnumerable collection)
                    return Throw.InternalError<object>($"Not an IEnumerable: {result.GetType()}");

                // Unlike in CreateArrayBackedCollection we can assume that the final result never can actually contain the temp collection
                // so we can use the builder as a proxy. This is needed only if we use a proxy builder so not the final result is created.
                bool trackUsages = addToCache && result.GetType() != descriptor.GetTypeToCreate() && descriptor.CanHaveRecursion;
                UsageReferences? usages = null;
                object resultProxy = collection;
                if (trackUsages)
                    ObjectsBeingDeserialized.Add(resultProxy, usages = new UsageReferences());

                MethodAccessor? addMethod = serInfo.GetSpecificAddMethod?.Invoke(result.GetType()); // result.GetType because of possible proxy builder
                if (descriptor.IsDictionary)
                {
                    for (int i = 0; i < count; i++)
                    {

                        object? key = ReadElement(br, descriptor.GetKeyDescriptor());
                        object? value = ReadElement(br, descriptor.GetValueDescriptor());

                        if (addMethod != null)
                        {
                            AddDictionaryElement(collection, addMethod, key, value);
                            continue;
                        }

#if NET35
                        if (value != null || !descriptor.IsGenericDictionary)
#endif
                        {
                            AddDictionaryElement((IDictionary)collection, key, value);
                            continue;
                        }
#if NET35
                        // generic dictionary with null value: calling generic Add because non-generic one may fail in .NET Runtime 2.x
                        addMethod = MethodAccessor.GetAccessor(Reflector.IDictionaryGenType
                            .GetGenericType(descriptor.GetKeyDescriptor().Type!, descriptor.GetValueDescriptor().Type!)
                            .GetMethod(nameof(IDictionary<_,_>.Add))!);
                        AddDictionaryElement(collection, addMethod, key, null);
                        continue;
#endif
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        object? element = ReadElement(br, descriptor.GetElementDescriptor());
                        if (addMethod != null)
                        {
                            AddCollectionElement(collection, descriptor.UnderlyingCollectionDataType, addMethod, element);
                            continue;
                        }

                        if (collection is IList list)
                        {
#if NET35
                            if (element != null || !descriptor.IsGenericCollection)
#endif
                            {
                                AddListElement(list, element);
                                continue;
                            }

#if NET35
                            // generic collection with null value: calling generic Add because non-generic one may fail in .NET Runtime 2.x
                            addMethod = MethodAccessor.GetAccessor(Reflector.ICollectionGenType.GetGenericType(descriptor.GetElementDescriptor().Type!).GetMethod(nameof(ICollection<_>.Add))!);
                            AddCollectionElement(collection, descriptor.UnderlyingCollectionDataType, addMethod, null);
                            continue;
#endif
                        }

                        Debug.Fail($"Define an Add method for type {descriptor.Type} for better performance");
                        collection.TryAdd(element, false);
                    }
                }

                // NOTE: As the possible proxy builder is never part of the final result, we do not allow circular references if the result was built by a proxy
                //       so tracking usages is just to detect this situation here. As a contrast, in CreateArrayBackedCollection we can replace the builder
                //       to the final array because the array is wrapped by the actual result in that case.
                result = serInfo.GetFinalCollection(result, descriptor);

                // check if we have non-allowed circular reference it ourselves (only if we use a proxy for creating the result)
                if (trackUsages)
                {
                    if (usages!.Count > 0)
                        Throw.SerializationException(Res.BinarySerializationCircularBuilderReferenceCollection(type));

                    ObjectsBeingDeserialized.Remove(resultProxy);
                }

                return result;
            }

            [SecurityCritical]
            private object? ReadElement(BinaryReader br, DataTypeDescriptor elementDescriptor)
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
            /// Reads a non-collection object from the stream.
            /// </summary>
            /// <param name="br">The reader</param>
            /// <param name="addToCache">When <see langword="true"/>, the result must be added to the ID cache. Otherwise, only reference types in a collection might be added to cache.
            /// False: Root level objects except recursive object graphs; Value type fields or collection elements
            /// On non-root level this parameter is <see langword="true"/> for every reference type (including boxed value types); <see langword="false"/> only for non-root value element types.
            /// Can be null for collection element types in which case will be evaluated lazily: will be false for value element types.</param>
            /// <param name="dataTypeDescriptor">The descriptor of the data type to be deserialized.</param>
            /// <returns>The deserialized object.</returns>
            [SecurityCritical]
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Long but very straightforward switch")]
            private object? ReadObject(BinaryReader br, bool? addToCache, DataTypeDescriptor dataTypeDescriptor)
            {
                int id = 0;

                #region Local Methods

                // Call it for pure reference type objects if ReadXXX does not use the objects cache
                bool TryGetFromCache(out object? cachedValue)
                {
                    Debug.Assert(addToCache.HasValue, "addToCache must be assigned before calling TryGetFromCache");
                    Debug.Assert(addToCache == true || dataTypeDescriptor.ParentDescriptor == null, "Not expected to be called for value element types");
                    if (dataTypeDescriptor.ParentDescriptor == null)
                    {
                        cachedValue = null;
                        return false;
                    }

                    return TryGetCachedObject(br, out cachedValue);
                }

                // Call it for pure reference type objects if ReadXXX uses the objects cache
                bool TryGetFromCacheOrAddPlaceholder(out object? cachedValue)
                {
                    bool result = TryGetFromCache(out cachedValue);
                    if (!result)
                        AddPlaceholder();
                    return result;
                }

                // Call it for value type objects if ReadXXX uses the objects cache
                void AddPlaceholder()
                {
                    if (addToCache == true)
                        AddObjectToCache(default, out id);
                }

                #endregion

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

                addToCache ??= !IsValueType(dataTypeDescriptor);
                object? createdResult = null;
                try
                {
                    object? cachedResult;
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
                            // object - returning object instance on (sub)root level, otherwise, doing recursion because can mean any type as an element type
                            if (dataTypeDescriptor.ParentDescriptor == null)
                                return createdResult = new object();
                            // result is not set here - when caching is needed, will be done in the recursion
                            return ReadWithType(br);
                        case DataTypes.Version:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = ReadVersion(br);
                        case DataTypes.Guid:
                            return createdResult = new Guid(br.ReadBytes(16));
                        case DataTypes.Uri:
                            return TryGetFromCacheOrAddPlaceholder(out cachedResult) ? cachedResult : createdResult = ReadUri(br, addToCache.Value);
                        case DataTypes.BitArray:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = ReadBitArray(br);
                        case DataTypes.BitVector32:
                            return createdResult = new BitVector32(br.ReadInt32());
                        case DataTypes.BitVector32Section:
                            return createdResult = ReadSection(br);
                        case DataTypes.StringSegment: // inner string is cached even if the result isn't but it's a collection element (ie. whenever it's not root)
                            AddPlaceholder();
                            return createdResult = ReadStringSegment(br, addToCache.Value || dataTypeDescriptor.ParentDescriptor != null);
                        case DataTypes.StringBuilder:
                            return TryGetFromCacheOrAddPlaceholder(out cachedResult) ? cachedResult : createdResult = ReadStringBuilder(br, addToCache.Value);
                        case DataTypes.RuntimeType:
                            return TryGetFromCache(out cachedResult) ? cachedResult : createdResult = ReadType(br, true).Type;
                        case DataTypes.CompareInfo:
                            return TryGetFromCacheOrAddPlaceholder(out cachedResult) ? cachedResult : createdResult = ReadCompareInfo(br, addToCache.Value);
                        case DataTypes.CultureInfo:
                            return TryGetFromCacheOrAddPlaceholder(out cachedResult) ? cachedResult : createdResult = ReadCultureInfo(br, addToCache.Value);
                        case DataTypes.Comparer:
                            return TryGetFromCacheOrAddPlaceholder(out cachedResult) ? cachedResult : createdResult = ReadComparer(br, addToCache.Value);
                        case DataTypes.CaseInsensitiveComparer:
                            return TryGetFromCacheOrAddPlaceholder(out cachedResult) ? cachedResult : createdResult = ReadCaseInsensitiveComparer(br, addToCache.Value);
                        case DataTypes.StringComparer:
                            return TryGetFromCacheOrAddPlaceholder(out cachedResult) ? cachedResult : createdResult = ReadStringComparer(br, addToCache.Value);
                        case DataTypes.StringSegmentComparer:
                            return TryGetFromCacheOrAddPlaceholder(out cachedResult) ? cachedResult : createdResult = ReadStringSegmentComparer(br, addToCache.Value);

#if !NET35
                        case DataTypes.BigInteger:
                            return createdResult = new BigInteger(br.ReadBytes(Read7BitInt(br)));
                        case DataTypes.Complex:
                            return createdResult = new Complex(br.ReadDouble(), br.ReadDouble());
#else
                        case DataTypes.BigInteger:
                        case DataTypes.Complex:
                            return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(dataType)));
#endif

#if NET46_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
                        case DataTypes.Vector2:
                            return createdResult = new Vector2(br.ReadSingle(), br.ReadSingle());
                        case DataTypes.Vector3:
                            return createdResult = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        case DataTypes.Vector4:
                            return createdResult = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        case DataTypes.Quaternion:
                            return createdResult = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        case DataTypes.Plane:
                            return createdResult = new Plane(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        case DataTypes.Matrix3x2:
                            return createdResult = new Matrix3x2(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        case DataTypes.Matrix4x4:
                            return createdResult = new Matrix4x4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
                                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
#else
                        case DataTypes.Vector2:
                        case DataTypes.Vector3:
                        case DataTypes.Vector4:
                        case DataTypes.Quaternion:
                        case DataTypes.Plane:
                        case DataTypes.Matrix3x2:
                        case DataTypes.Matrix4x4:
                            return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(dataType)));
#endif

                        case DataTypes.ValueTuple0:
#if NET47_OR_GREATER || !NETFRAMEWORK
                            return createdResult = new ValueTuple();
#else
                            return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(dataType)));
#endif

                        case DataTypes.Rune:
#if NETCOREAPP3_0_OR_GREATER
                            return createdResult = new Rune(br.ReadInt32());
#else
                            return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(dataType)));
#endif

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                        case DataTypes.Index:
                            return createdResult = ReadIndex(br);
                        case DataTypes.Range:
                            return createdResult = new Range(ReadIndex(br), ReadIndex(br));
#else
                        case DataTypes.Index:
                        case DataTypes.Range:
                            return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(dataType)));
#endif

                        case DataTypes.Half:
#if NET5_0_OR_GREATER
                            return createdResult = ReadHalf(br);
#else
                            return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(dataType)));
#endif

#if NET6_0_OR_GREATER
                        case DataTypes.DateOnly:
                            return createdResult = DateOnly.FromDayNumber(br.ReadInt32());
                        case DataTypes.TimeOnly:
                            return createdResult = new TimeOnly(br.ReadInt64());
#else
                        case DataTypes.DateOnly:
                        case DataTypes.TimeOnly:
                            return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(dataType)));
#endif

#if NET7_0_OR_GREATER
                        case DataTypes.Int128:
                            return createdResult = ReadInt128(br);
                        case DataTypes.UInt128:
                            return createdResult = ReadUInt128(br);
#else
                        case DataTypes.Int128:
                        case DataTypes.UInt128:
                            return Throw.PlatformNotSupportedException<Type>(Res.BinarySerializationTypePlatformNotSupported(DataTypeToString(dataType)));
#endif

                        case DataTypes.BinarySerializable:
                            return ReadBinarySerializable(br, addToCache.Value, dataTypeDescriptor);
                        case DataTypes.RecursiveObjectGraph:
                            return ReadObjectGraph(br, addToCache.Value, dataTypeDescriptor);
                        case DataTypes.RawStruct:
                            return createdResult = ReadValueType(br, dataTypeDescriptor);
                        default:
                            if (IsEnum(dataType))
                                return createdResult = ReadEnum(br, dataTypeDescriptor);
                            Throw.SerializationException(Res.BinarySerializationCannotDeserializeObject(DataTypeToString(dataType)));
                            return default;
                    }
                }
                finally
                {
                    if (addToCache == true && createdResult != null)
                    {
                        if (id == 0)
                            AddObjectToCache(createdResult);
                        else
                            ReplaceObjectInCache(id, createdResult);
                    }
                }
            }

            private string ReadStringValue(BinaryReader br, bool addToCache)
            {
                if (addToCache && TryGetCachedObject(br, out object? cachedResult))
                {
                    if (cachedResult is not string)
                        Throw.SerializationException(Res.BinarySerializationInvalidStreamData);
                    return (string)cachedResult;
                }

                string result = br.ReadString();
                if (addToCache)
                    AddObjectToCache(result);
                return result;
            }

            private Uri ReadUri(BinaryReader br, bool addToCache)
            {
                bool isAbsolute = br.ReadBoolean();
                return new Uri(ReadStringValue(br, addToCache), isAbsolute ? UriKind.Absolute : UriKind.Relative);
            }

            private StringBuilder ReadStringBuilder(BinaryReader br, bool addToCache)
            {
                int capacity = Read7BitInt(br);
                int maxCapacity = Read7BitInt(br);
                if (maxCapacity == Int32.MaxValue)
                    return new StringBuilder(ReadStringValue(br, addToCache), capacity);
                var result = new StringBuilder(capacity, maxCapacity);
                result.Append(ReadStringValue(br, addToCache));
                return result;
            }

            private StringSegment ReadStringSegment(BinaryReader br, bool addToCache) => !br.ReadBoolean()
                ? StringSegment.Null
                : new StringSegment(ReadStringValue(br, addToCache), Read7BitInt(br), Read7BitInt(br));

            private CompareInfo ReadCompareInfo(BinaryReader br, bool addToCache)
            {
                string name = ReadStringValue(br, addToCache);
                if (name.Length == 0 && br.ReadBoolean())
                    return CultureInfo.InvariantCulture.CompareInfo;
                return CompareInfo.GetCompareInfo(name);
            }

            private CultureInfo ReadCultureInfo(BinaryReader br, bool addToCache)
            {
                string name = ReadStringValue(br, addToCache);
                if (name.Length == 0 && br.ReadBoolean())
                    return CultureInfo.InvariantCulture;
                return CultureInfo.GetCultureInfo(name);
            }

            private Comparer ReadComparer(BinaryReader br, bool addToCache) => (ComparerType)br.ReadByte() switch
            {
                ComparerType.Invariant => Comparer.DefaultInvariant,
                ComparerType.CultureSpecific => new Comparer(CultureInfo.GetCultureInfo(ReadStringValue(br, addToCache))),
                ComparerType.Default => Comparer.Default, // only as a fallback if Comparer implementation was unrecognized during serialization
                _ => Throw.SerializationException<Comparer>(Res.BinarySerializationInvalidStreamData)
            };

            private CaseInsensitiveComparer ReadCaseInsensitiveComparer(BinaryReader br, bool addToCache) => (ComparerType)br.ReadByte() switch
            {
                ComparerType.Invariant => CaseInsensitiveComparer.DefaultInvariant,
                ComparerType.CultureSpecific => new CaseInsensitiveComparer(CultureInfo.GetCultureInfo(ReadStringValue(br, addToCache))),
                ComparerType.Default => CaseInsensitiveComparer.Default, // only as a fallback if CaseInsensitiveComparer implementation was unrecognized during serialization
                _ => Throw.SerializationException<CaseInsensitiveComparer>(Res.BinarySerializationInvalidStreamData)
            };

            private StringComparer ReadStringComparer(BinaryReader br, bool addToCache) => (ComparerType)br.ReadByte() switch
            {
                ComparerType.Ordinal => StringComparer.Ordinal,
                ComparerType.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
                ComparerType.Invariant => StringComparer.InvariantCulture,
                ComparerType.InvariantIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                ComparerType.CultureSpecific => StringComparer.Create(CultureInfo.GetCultureInfo(ReadStringValue(br, addToCache)), (CompareOptions)Read7BitInt(br)),
#else
                ComparerType.CultureSpecific => StringComparer.Create(CultureInfo.GetCultureInfo(ReadStringValue(br, addToCache)), (CompareOptions)Read7BitInt(br) == CompareOptions.IgnoreCase),
#endif
#if NET9_0_OR_GREATER
#error check if already available - https://github.com/dotnet/runtime/issues/77679
                ComparerType.OrdinalNonRandomized => StringComparer.OrdinalNonRandomized,
                ComparerType.OrdinalIgnoreCaseNonRandomized => StringComparer.OrdinalIgnoreCaseNonRandomized,
#else
                ComparerType.OrdinalNonRandomized => Throw.PlatformNotSupportedException<StringComparer>(Res.BinarySerializationTypePlatformNotSupported($"{nameof(StringComparer)}.{nameof(ComparerType.OrdinalNonRandomized)}")),
                ComparerType.OrdinalIgnoreCaseNonRandomized => Throw.PlatformNotSupportedException<StringComparer>(Res.BinarySerializationTypePlatformNotSupported($"{nameof(StringComparer)}.{nameof(ComparerType.OrdinalIgnoreCaseNonRandomized)}")),
#endif
                ComparerType.Default => StringComparer.InvariantCulture, // only as a fallback if culture aware StringComparer implementation was unrecognized during serialization
                _ => Throw.SerializationException<StringComparer>(Res.BinarySerializationInvalidStreamData)
            };

            private StringSegmentComparer ReadStringSegmentComparer(BinaryReader br, bool addToCache) => (ComparerType)br.ReadByte() switch
            {
                ComparerType.Ordinal => StringSegmentComparer.Ordinal,
                ComparerType.OrdinalIgnoreCase => StringSegmentComparer.OrdinalIgnoreCase,
                ComparerType.Invariant => StringSegmentComparer.InvariantCulture,
                ComparerType.InvariantIgnoreCase => StringSegmentComparer.InvariantCultureIgnoreCase,
#if NET35 || NET40 || NET45
                ComparerType.CultureSpecific => StringSegmentComparer.Create(CultureInfo.GetCultureInfo(ReadStringValue(br, addToCache)), (CompareOptions)Read7BitInt(br) == CompareOptions.IgnoreCase),
#else
                ComparerType.CultureSpecific => StringSegmentComparer.Create(CultureInfo.GetCultureInfo(ReadStringValue(br, addToCache)), (CompareOptions)Read7BitInt(br)),
#endif
                ComparerType.OrdinalRandomized => StringSegmentComparer.OrdinalRandomized,
                ComparerType.OrdinalIgnoreCaseRandomized => StringSegmentComparer.OrdinalIgnoreCaseRandomized,
                ComparerType.OrdinalNonRandomized => StringSegmentComparer.OrdinalNonRandomized,
                ComparerType.OrdinalIgnoreCaseNonRandomized => StringSegmentComparer.OrdinalIgnoreCaseNonRandomized,
                _ => Throw.SerializationException<StringSegmentComparer>(Res.BinarySerializationInvalidStreamData)
            };

            [SecurityCritical]
            private object ReadBinarySerializable(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                // checking instance id
                if (descriptor.ParentDescriptor != null && !IsValueType(descriptor))
                {
                    if (TryGetCachedObject(br, out object? cachedResult))
                        return cachedResult!;
                }

                // actual type if needed
                Type type = descriptor.ParentDescriptor != null && !IsSealed(descriptor)
                    ? ReadType(br, false).Type!
                    : descriptor.Type!;

                // deserialize (object will be cached immediately after creation so circular references will be found in time)
                type = Nullable.GetUnderlyingType(type) ?? type;
                if (!binarySerializableType.IsAssignableFrom(type))
                    Throw.SerializationException(Res.BinarySerializationNotBinarySerializable(type));
                byte[] serData = br.ReadBytes(Read7BitInt(br));

                if (!Reflector.TryCreateEmptyObject(type, false, true, out object? result))
                    Throw.SerializationException(Res.BinarySerializationCannotCreateUninitializedObject(type));

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
            private object? ReadObjectGraph(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor)
            {
                // When element types may differ, reading element with data type
                if (descriptor.ParentDescriptor != null && !IsSealed(descriptor))
                    return ReadWithType(br);

                // checking instance id
                if (descriptor.ParentDescriptor != null && !IsValueType(descriptor))
                {
                    if (TryGetCachedObject(br, out object? cachedResult))
                        return cachedResult;
                }

                Type type = Nullable.GetUnderlyingType(descriptor.Type!) ?? descriptor.Type!;

                // IsDefault flag
                bool isCustomObjectGraph = IsCustomSerialized(br, descriptor);

                // Types of custom serialized objects are always explicitly stored
                bool isSealedElement = descriptor.ParentDescriptor != null && IsSealed(descriptor);
                if (isCustomObjectGraph && isSealedElement)
                    type = ReadType(br, false).Type!;

                // Creating initial instance, registration
                bool useSurrogate = TryGetSurrogate(type, out ISerializationSurrogate? surrogate, out ISurrogateSelector? selector);
                object obj = CreateEmptyObject(useSurrogate, type);
                bool isISerializable = !IgnoreISerializable && obj is ISerializable;
                IObjectReference? objRef = IgnoreIObjectReference ? null : obj as IObjectReference;
                int id = 0;
                UsageReferences? usages = null;
                bool trackUsages = addToCache && (useSurrogate || objRef != null);

                // if the object can be possibly changed, then we prepare tracking its usage
                if (trackUsages)
                    ObjectsBeingDeserialized.Add(obj, usages = new UsageReferences());

                if (addToCache)
                    AddObjectToCache(obj, out id);

                OnDeserializing(obj);

                // The actual deserialization
                // ReSharper disable once VariableCanBeNotNullable - false alarm, IObjectReference.GetRealObject actually can return null
                object? result = obj;
                if (isISerializable || useSurrogate)
                {
                    result = isCustomObjectGraph
                        ? ReadCustomObjectGraph(br, obj, surrogate, selector)
                        : ReadDefaultObjectGraphAsCustom(br, obj, surrogate, selector);
                }
                else if (isCustomObjectGraph)
                    ReadCustomObjectGraphAsDefault(br, obj);
                else
                    ReadDefaultObjectGraph(br, obj);

                // if type result is IObjectReference, then calling its GetRealObject to return something
                if (objRef != null)
                    result = objRef.GetRealObject(Context);

                // some post administration if the object was registered for tracking usages
                if (trackUsages)
                {
                    ApplyPendingUsages(usages!, obj, result);

                    if (result != obj && addToCache)
                        ReplaceObjectInCache(id, result);

                    ObjectsBeingDeserialized.Remove(obj);
                }

                OnDeserialized(result);
                return result;
            }

            [SecurityCritical]
            private void ReadDefaultObjectGraph(BinaryReader br, object obj)
            {
                Type type = obj.GetType();

                // iterating through self and base types
                for (Type t = type; t != Reflector.ObjectType; t = t.BaseType!)
                {
                    // checking name of base type
                    if (t != type)
                    {
                        string name = ReadName(br);
                        while (t.Name != name && t != Reflector.ObjectType)
                            t = t.BaseType!;

                        if (name.Length == 0 && t == Reflector.ObjectType)
                            return;

                        if (t.Name != name && !IgnoreObjectChanges)
                            Throw.SerializationException(Res.BinarySerializationObjectHierarchyChanged(type));
                    }

                    // reading fields of current level
                    int count = Read7BitInt(br);
                    for (int i = 0; i < count; i++)
                    {
                        string name = ReadName(br);
                        object? value = ReadWithType(br);

                        FieldInfo? field = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        if (field == null)
                        {
                            if (!IgnoreObjectChanges)
                            {
                                if (t == type)
                                    Throw.SerializationException(Res.BinarySerializationMissingField(type, name));
                                Throw.SerializationException(Res.BinarySerializationMissingFieldBase(type, name, t));
                            }

                            continue;
                        }

                        if (field.IsNotSerialized)
                            continue;

                        SetField(field, obj, value);
                    }
                }

                // checking end of hierarchy
                if (ReadName(br).Length != 0)
                {
                    if (!IgnoreObjectChanges)
                        Throw.SerializationException(Res.BinarySerializationObjectHierarchyChanged(type));

                    // skipping fields until the end of the serialized hierarchy
                    do
                    {
                        int count = Read7BitInt(br);
                        for (int i = 0; i < count; i++)
                        {
                            ReadName(br);
                            ReadWithType(br);
                        }
                    } while (ReadName(br).Length != 0);
                }
            }

            [SecurityCritical]
            private object ReadCustomObjectGraph(BinaryReader br, object obj, ISerializationSurrogate? surrogate, ISurrogateSelector? selector)
            {
                Type type = obj.GetType();
                SerializationInfo si = new SerializationInfo(type, new FormatterConverter());
                int count = Read7BitInt(br);

                // reading content into si
                for (int i = 0; i < count; i++)
                {
                    string name = ReadName(br);
                    object? value = ReadWithType(br);
                    Type elementType = ReadType(br, false).Type!;
                    si.AddValue(name, value, elementType);
                }

                CheckReferences(si);
                if (surrogate == null)
                {
                    if (!Accessors.TryInvokeCtor(obj, si, Context))
                        Throw.SerializationException(Res.BinarySerializationMissingISerializableCtor(type));
                    return obj;
                }

                // Using surrogate
                return surrogate.SetObjectData(obj, si, Context, selector);
            }

            [SecurityCritical]
            private object ReadDefaultObjectGraphAsCustom(BinaryReader br, object obj, ISerializationSurrogate? surrogate, ISurrogateSelector? selector)
            {
                Type type = obj.GetType();

                // reading original fields into si
                SerializationInfo si = new SerializationInfo(type, new FormatterConverter());
                var existingNames = new StringKeyedDictionary<int>();
                string? currentTypeName = null;
                do
                {
                    // reading fields of current level
                    int count = Read7BitInt(br);
                    for (int i = 0; i < count; i++)
                    {
                        string name = ReadName(br);

                        // conflicting names can occur if there are fields of the same name in the base class
                        int usedCount = existingNames.GetValueOrDefault(name);
                        if (usedCount == 0)
                            existingNames[name] = 1;
                        else
                        {
                            // conflicting name 1st try: prefixing by type name
                            string prefixedName = currentTypeName + "+" + name;
                            if (existingNames.GetValueOrDefault(prefixedName) == 0)
                            {
                                name = prefixedName;
                                existingNames[prefixedName] = 1;
                            }
                            else
                            {
                                // 1st try didn't work, using numeric postfix
                                existingNames[name] = ++usedCount;
                                name += usedCount.ToString(CultureInfo.InvariantCulture);
                            }
                        }

                        object? value = ReadWithType(br);
                        si.AddValue(name, value);
                    }

                    // end level is marked with empty string
                    currentTypeName = ReadName(br);
                } while (currentTypeName.Length != 0);

                CheckReferences(si);
                if (surrogate == null)
                {
                    // As ISerializable: Invoking serialization constructor
                    if (!Accessors.TryInvokeCtor(obj, si, Context))
                        Throw.SerializationException(Res.BinarySerializationMissingISerializableCtor(type));
                    return obj;
                }

                // Using surrogate
                return surrogate.SetObjectData(obj, si, Context, selector);
            }

            [SecurityCritical]
            private void ReadCustomObjectGraphAsDefault(BinaryReader br, object obj)
            {
                // Default object graph allows duplicate names but custom doesn't. We handle possible duplicates the
                // same way as in ReadDefaultObjectGraphAsCustom. Though it is not a guarantee for anything.
                StringKeyedDictionary<FieldInfo> fields = SerializationHelper.GetFieldsWithUniqueNames(obj.GetType(), false);

                // Reading the custom content and trying to identify them as fields
                int count = Read7BitInt(br);
                for (int i = 0; i < count; i++)
                {
                    string name = ReadName(br);
                    object? value = ReadWithType(br);
                    ReadType(br, false); // the element type, which is ignored now

                    if (fields.TryGetValue(name, out FieldInfo? field))
                    {
                        if (field.IsNotSerialized)
                            continue;
                        SetField(field, obj, value);
                        continue;
                    }

                    if (!IgnoreObjectChanges)
                        Throw.SerializationException(Res.BinarySerializationMissingField(obj.GetType(), name));
                }
            }

            [SecurityCritical]
            private object ReadValueType(BinaryReader br, DataTypeDescriptor descriptor)
            {
                Type structType = Nullable.GetUnderlyingType(descriptor.Type!) ?? descriptor.Type!;
                if (!structType.IsValueType)
                    Throw.SerializationException(Res.BinarySerializationNotAValueType(structType));
                if (SafeMode && structType.IsManaged())
                    Throw.SerializationException(Res.BinarySerializationValueTypeContainsReferenceSafe(structType));
                byte[] rawData = br.ReadBytes(Read7BitInt(br));
                object result = BinarySerializer.DeserializeValueType(structType, rawData);
                OnDeserializing(result);
                OnDeserialized(result);
                return result;
            }

            [SecurityCritical]
            private object CreateEmptyObject(bool useSurrogate, Type type)
            {
                if (!useSurrogate && SafeMode)
                {
                    if (AllowNonSerializableExpectedCustomTypes)
                    {
                        if (SerializationHelper.IsUnsafeType(type))
                            Throw.SerializationException(Res.BinarySerializationCannotCreateObjectSafe(type));
                    }
                    else if (!SerializationHelper.IsSafeType(type))
                        Throw.SerializationException(Res.BinarySerializationCannotCreateSerializableObjectSafe(type));
                }

                if (!Reflector.TryCreateEmptyObject(type, false, true, out object? obj))
                    Throw.SerializationException(Res.BinarySerializationCannotCreateUninitializedObject(type));
                return obj;
            }

            private void OnDeserializing(object obj) => ExecuteMethodsOfAttribute(obj, onDeserializingAttribute);

            private void OnDeserialized(object? obj)
            {
                if (obj == null || IgnoreSerializationMethods)
                    return;
                ExecuteMethodsOfAttribute(obj, onDeserializedAttribute);
                RegisterDeserializedObject(obj as IDeserializationCallback);
            }

            private void RegisterDeserializedObject(IDeserializationCallback? obj)
            {
                if (obj == null)
                    return;

                deserializationRegObjects ??= new List<IDeserializationCallback>();
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

            private bool TryGetCachedObject(BinaryReader br, out object? result)
            {
                Dictionary<int, object?> cache = IdCache;
                int id = Read7BitInt(br);
                if (cache.TryGetValue(id, out result))
                    return true;

                if (id > cache.Count)
                    Throw.SerializationException(Res.BinarySerializationDeserializeUnexpectedId);
                return false;
            }

            private void SetField(FieldInfo field, object obj, object? value)
            {
                UsageReferences? trackedUsages = value == null ? null : objectsBeingDeserialized?.GetValueOrDefault(value);
                if (trackedUsages == null)
                {
                    field.Set(obj, value);
                    return;
                }

                trackedUsages.Add(new FieldUsage(obj, field));
            }

            private void AddListElement(IList list, object? value)
            {
                UsageReferences? trackedUsages = value == null ? null : objectsBeingDeserialized?.GetValueOrDefault(value);
                if (trackedUsages == null)
                {
                    list.Add(value);
                    return;
                }

                // though we can't add the final item now we add a placeholder so the index will be valid
                int index = list.Count;
                list.Add(GetPlaceholderValue(value, list));
                trackedUsages.Add(new ListUsage(list, index));
            }

            private void AddCollectionElement([NoEnumeration]IEnumerable collection, DataTypes collectionDataType, MethodAccessor addMethod, object? value)
            {
                UsageReferences? trackedUsages = value == null ? null : objectsBeingDeserialized?.GetValueOrDefault(value);
                if (trackedUsages == null)
                {
                    addMethod.Invoke(collection, value);
                    return;
                }

                Type type = collection.GetType();

                // LinkedList: adding a placeholder node that can be replaced later
                if (collectionDataType == DataTypes.LinkedList)
                {
                    Type genericArg = type.GetGenericArguments()[0];
                    Type nodeType = typeof(LinkedListNode<>).GetGenericType(genericArg);
                    object node = nodeType.CreateInstance(genericArg, GetPlaceholderValue(value, collection));
                    Accessors.InvokeMethod(collection, nameof(LinkedList<_>.AddLast), new[] { nodeType }, node);
                    trackedUsages.Add(new LinkedListUsage(collection, node));
                    return;
                }

                // Any other generic ICollection: supposing that collection is unordered
                if (type.IsImplementationOfGenericType(Reflector.ICollectionGenType)
#if !NET35
                    || collectionDataType == DataTypes.ConcurrentBag
#endif
                )
                {
                    trackedUsages.Add(new CollectionUsage(collection, addMethod));
                    return;
                }

                // Neither ICollection nor has specific Add method: adding if item is compatible, its reference cannot be replaced anymore
                if (!addMethod.ParameterTypes[0].CanAcceptValue(value))
                    Throw.SerializationException(Res.BinarySerializationCircularIObjectReferenceCollection(type));

                trackedUsages.CanBeReplaced = false;
                addMethod.Invoke(collection, value);
            }

            private void AddDictionaryElement(IDictionary dict, object? key, object? value)
            {
                UsageReferences? keyUsages = key == null ? null : objectsBeingDeserialized?.GetValueOrDefault(key);
                UsageReferences? valueUsages = value == null ? null : objectsBeingDeserialized?.GetValueOrDefault(value);
                if (objectsBeingDeserialized == null || keyUsages == null && valueUsages == null)
                {
                    // though a null key is really a problem at most dictionaries we let the exception come if the key is really null
                    dict.Add(key!, value);
                    return;
                }

                // OrderedDictionary: we need to add a placeholder item to maintain the correct order
                if (dict is IOrderedDictionary orderedDictionary)
                {
                    int index = dict.Count;

                    // we exploit that the supported ordered dictionary is not generic
                    object placeholderKey = keyUsages == null ? key! : new object();
                    object? placeholderValue = valueUsages == null ? value : null;
                    dict.Add(placeholderKey, placeholderValue);

                    keyUsages?.Add(new OrderedDictionaryKeyUsage(orderedDictionary, index));
                    valueUsages?.Add(new OrderedDictionaryValueUsage(orderedDictionary, index));
                    return;
                }
               
                // Unordered dictionaries: if both key and value are being deserialized, than that is an issue unless they are not replaced.
                if (keyUsages != null && valueUsages != null)
                {
                    // the same values: null key indicates that it will be same as the resolved value
                    if (key == value)
                    {
                        valueUsages.Add(new DictionaryValueUsage(dict, null));
                        return;
                    }

                    // They are different: we don't support their replacement. This could be solved if we put every resolved object in a cache first and then
                    // do the replacements but this edge-case scenario isn't worth the effort. And this can be avoided by forcing recursive serialization.
                    keyUsages.CanBeReplaced = false;
                    valueUsages.CanBeReplaced = false;
                    Type type = dict.GetType();
                    Type[] elementTypes = type.GetCollectionElementType()!.GetGenericArguments();
                    object keyToAdd = elementTypes.Length == 0 || elementTypes[0].CanAcceptValue(key)
                        ? key!
                        : Throw.SerializationException<object>(Res.BinarySerializationCircularIObjectReferenceCollection(type));
                    object valueToAdd = elementTypes.Length == 0 || elementTypes[1].CanAcceptValue(value)
                        ? value!
                        : Throw.SerializationException<object>(Res.BinarySerializationCircularIObjectReferenceCollection(type));
                    dict.Add(keyToAdd, valueToAdd);
                    return;
                }

                // Adding the possible usages. Both key and value can be replaced at the same time for ordered dictionaries only.
                keyUsages?.Add(new DictionaryKeyUsage(dict, value!));
                valueUsages?.Add(new DictionaryValueUsage(dict, key));
            }

            private void AddDictionaryElement(object dictionary, MethodAccessor addMethod, object? key, object? value)
            {
                UsageReferences? keyUsages = key == null ? null : objectsBeingDeserialized?.GetValueOrDefault(key);
                UsageReferences? valueUsages = value == null ? null : objectsBeingDeserialized?.GetValueOrDefault(value);
                if (keyUsages != null || valueUsages != null)
                {
                    AddDictionaryElement((IDictionary)dictionary, key, value);
                    return;
                }

                addMethod.Invoke(dictionary, key, value);
            }

            private void SetKeyValue(object obj, object? key, object? value)
            {
                UsageReferences? keyUsages = key == null ? null : objectsBeingDeserialized?.GetValueOrDefault(key);
                UsageReferences? valueUsages = value == null ? null : objectsBeingDeserialized?.GetValueOrDefault(value);
                if (objectsBeingDeserialized == null || keyUsages == null && valueUsages == null)
                {
                    Accessors.SetKeyValue(obj, key, value);
                    return;
                }

                // Since KeyValuePair/DictionaryEntry are value types, late setting the key/value works only if the boxed reference "obj"
                // is the final object itself. Otherwise, the late setting will not occur at the real destination.
                // It still can be alright though, if an IObjectReference.GetRealObject returns a correct instance and obj is discarded anyway.
                Type type = obj.GetType();

                FieldInfo keyField = type.GetFieldInfo(nameof(key));
                if (keyUsages == null)
                    keyField.Set(obj, key);
                else
                    keyUsages.Add(new FieldUsage(obj, keyField));

                FieldInfo valueField = type.GetFieldInfo(nameof(value));
                if (valueUsages == null)
                    valueField.Set(obj, value);
                else
                    valueUsages.Add(new FieldUsage(obj, valueField));
            }

            private void CheckReferences(SerializationInfo si)
            {
                if (objectsBeingDeserialized == null)
                    return;

                // circular IObjectReferences can be resolved after all, except if custom deserialization is used for unresolved references
                foreach (SerializationEntry entry in si)
                {
                    if (entry.Value == null)
                        continue;
                    if (objectsBeingDeserialized.TryGetValue(entry.Value, out UsageReferences? usages))
                    {
                        if (entry.Value is IObjectReference)
                            Throw.SerializationException(Res.BinarySerializationCircularIObjectReference);
                        usages.CanBeReplaced = false;
                    }
                }
            }

            private DataTypeDescriptor ReadNewTypeWithAssembly(string assemblyName, string typeName)
            {
                // 1.) Binder
                Type? type = ReadBoundType(assemblyName, typeName);
                
                // 2.) Expected type
                if (type is null)
                    expectedTypes?.TryGetValue((assemblyName, typeName), out type);

                if (type is not null)
                    CachedAssemblies.Add((null, assemblyName));
                else if (SafeMode)
                    Throw.SerializationException(Res.BinarySerializationCannotResolveExpectedTypeInAssemblySafe(typeName, assemblyName));
                else
                {
                    // 3.) Actual resolve
                    Assembly assembly = ResolveAssembly(assemblyName);
                    type = TypeResolver.ResolveType(assembly, typeName, ResolveTypeOptions.None);
                    if (type is null)
                        Throw.SerializationException(Res.BinarySerializationCannotResolveTypeInAssembly(typeName, assemblyName));
                    CachedAssemblies.Add((assembly, assemblyName));
                }

                var result = new DataTypeDescriptor(type, new TypeByString(assemblyName, typeName));
                CachedTypes.Add(result);
                return result;
            }

            private Type? ReadBoundType(string? assemblyName, string typeName)
            {
                if (Binder is ISerializationBinder binder)
                    return binder.BindToType(assemblyName ?? String.Empty, typeName);
                return Binder?.BindToType(assemblyName ?? String.Empty, typeName);
            }

            private Assembly ResolveAssembly(string name)
            {
                Debug.Assert(!SafeMode);
                if (assemblyByNameCache?.TryGetValue(name, out Assembly? result) == true)
                    return result;

                // 1.) Iterating through loaded assemblies
                result = Reflector.GetLoadedAssemblies().FirstOrDefault(asm => asm.FullName == name);
                if (result is not null)
                    return result;

                // 2.) Trying to load assembly. Not using AssemblyResolver because Assembly.Load allows version mismatch for some System assemblies.
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
                        Throw.SerializationException(Res.ReflectionCannotLoadAssembly(name), ex);
                    }
                }

                assemblyByNameCache ??= new StringKeyedDictionary<Assembly>(1);
                assemblyByNameCache.Add(name, result);
                return result;
            }

            private Type ResolveType((Assembly? Assembly, string? StoredName) assembly, string typeName)
            {
                // 1.) Binder
                Type? result = ReadBoundType(assembly.StoredName, typeName);
                if (result is not null)
                    return result;

                // 2.) Expected types cache
                if (expectedTypes != null)
                {
                    // Assembly names are omitted: in SafeMode we accept unique names only
                    if (SafeMode && assembly.Assembly is null && assembly.StoredName is null)
                    {
                        // This affects performance a LOT but omitting assembly names is not recommended in safe mode anyway so not using a cache
                        // for the lookup itself (and the result is mapped to a new type index so will be cached anyway).

                        // NOTE: The following line would be somewhat faster but it does not work if the same type name is added with multiple assembly identities
                        //if (expectedTypes.Where(t => t.Key.TypeName == typeName).Take(2).Select(t => t.Value).SingleOrDefault() is Type type)
                        //    return type;
                        Type[] types = expectedTypes.Where(t => t.Key.TypeName == typeName).Select(t => t.Value).Distinct().Take(2).ToArray();
                        if (types.Length == 1)
                            return types[0];

                        string message = types.Length == 0
                            ? Res.BinarySerializationCannotResolveExpectedTypeSafe(typeName)
                            : Res.BinarySerializationExpectedTypeOmittedAssemblyNameSafe(typeName);
                        Throw.SerializationException(message);
                    }

                    // NOTE: It's important that we don't check this first if assembly names are omitted because in safe mode it would be possible to
                    //       mistakenly map a custom type to core type of the same full name otherwise.
                    if (expectedTypes.TryGetValue((assembly.StoredName, typeName), out result))
                        return result;
                }

                if (SafeMode)
                {
                    string message = assembly.StoredName == null
                        ? Res.BinarySerializationCannotResolveExpectedTypeSafe(typeName)
                        : Res.BinarySerializationCannotResolveExpectedTypeInAssemblySafe(typeName, assembly.StoredName);
                    Throw.SerializationException(message);
                }

                // 3.) Actual resolve. ResolveType should not resolve any further assemblies (generic type parameters are loaded separately)
                if (assembly.Assembly is not null)
                {
                    // 3/a.) From core libraries: trying without an assembly first
                    if (assembly.StoredName == null)
                        result = Reflector.ResolveType(typeName, ResolveTypeOptions.None);

                    // 3/b.) From assembly
                    result ??= Reflector.ResolveType(assembly.Assembly, typeName, ResolveTypeOptions.None);
                }
                else
                {
                    // 3/a.) From assembly name or just by name
                    result = assembly.StoredName != null
                        ? Reflector.ResolveType(ResolveAssembly(assembly.StoredName), typeName, ResolveTypeOptions.None)
                        : Reflector.ResolveType(typeName, ResolveTypeOptions.None);
                }

                if (result is null)
                {
                    string message = assembly.StoredName == null
                        ? Res.BinarySerializationCannotResolveType(typeName)
                        : Res.BinarySerializationCannotResolveTypeInAssembly(typeName, assembly.StoredName);
                    Throw.SerializationException(message);
                }

                return result;
            }

            private bool IsCustomSerialized(BinaryReader br, DataTypeDescriptor descriptor)
            {
                var attr = EnsureAttributes(br, descriptor);
                return (attr & TypeAttributes.CustomSerialized) != TypeAttributes.None;
            }

            private TypeAttributes EnsureAttributes(BinaryReader br, DataTypeDescriptor descriptor)
            {
                MemberInfo type = (MemberInfo?)descriptor.StoredType ?? descriptor.GetTypeToCreate()!;
                if (TypeAttributesCache.TryGetValue(type, out TypeAttributes result))
                    return result;
                result = (TypeAttributes)br.ReadByte();
                if (!result.AllFlagsDefined())
                    Throw.SerializationException(Res.BinarySerializationInvalidStreamData);
                TypeAttributesCache.Add(type, result);
                return result;
            }

            #endregion

            #endregion

            #endregion
        }
    }
}
