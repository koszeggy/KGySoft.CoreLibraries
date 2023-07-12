#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.CollectionSerializationInfo.cs
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization.Binary
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// Static descriptor for collection types. Instance-specific descriptor is in <see cref="DataTypeDescriptor"/>.
        /// </summary>
        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass", Justification = "Properties vs the similarly named methods with DataTypes parameter in parent class.")]
        private sealed class CollectionSerializationInfo
        {
            #region Constants

            private const int capacityThreshold = 1 << 13;

            #endregion

            #region Fields

            #region Static Fields

            internal static readonly CollectionSerializationInfo Tuple = new() { Info = CollectionInfo.IsGeneric | CollectionInfo.IsTuple };

            internal static readonly CollectionSerializationInfo FixedSizeGenericStruct = new()
            {
                Info = CollectionInfo.IsGeneric | CollectionInfo.BackingArrayHasKnownSize | CollectionInfo.CreateResultFromByteArray,
                GetBackingArray = o => BinarySerializer.SerializeValueType((ValueType)o),
                CreateArrayBackedCollectionInstanceFromArray = (_, t, a) => BinarySerializer.DeserializeValueType(t, (byte[])a)
            };

            #endregion

            #region Instance Fields

            /// <summary>
            /// Can contain more elements only for generic collections. Will be instantiated only on deserialization.
            /// Thread safe accessor because the serialization info is stored in a static shared dictionary.
            /// </summary>
            private IThreadSafeCacheAccessor<Type, CreateInstanceAccessor>? ctorCache;

            private IThreadSafeCacheAccessor<Type, MethodAccessor>? addMethodCache;

            #endregion

            #endregion

            #region Properties

            #region Internal Properties

            internal CollectionInfo Info { private get; set; }

            /// <summary>
            /// Specifies the constructor arguments to be used. Order matters!
            /// </summary>
            internal CollectionCtorArguments[]? CtorArguments { private get; set; }

            /// <summary>
            /// Should be specified only when target collection is not <see cref="IList"/>, <see cref="IDictionary"/> or <see cref="ICollection{T}"/> implementation,
            /// or when defining it results faster access than resolving the generic Add method for each access. Can refer to a generic method definition.
            /// </summary>
            internal string? SpecificAddMethod { get; set; }

            /// <summary>
            /// Should be specified if the collection is a value type and it has a publicly exposed backing array that can have more elements than the wrapper type (eg. ArraySegment),
            /// or, if it does not expose or wrap any array but it can be represented as a (fixed size) array (eg. Vector128).
            /// If the array is not exposed, then it must not contain any direct circular reference to the object itself because it makes proper deserialization possible (eg. object element type must not be supported by the collection)
            /// </summary>
            internal Func<object, Array?>? GetBackingArray { get; set; }

            /// <summary>
            /// Should be specified for any custom data for array backed collections.
            /// Can be specified also for collections, in which case <see cref="RestoreSpecificPropertiesCallback"/> has to be specified, too.
            /// If the object represents an array backed collection that has no constructor from array, then <see cref="CreateArrayBackedCollectionInstanceFromArray"/> should also be specified.
            /// </summary>
            internal Action<BinaryWriter, object>? WriteSpecificPropertiesCallback { get; set; }

            /// <summary>
            /// Can be used to restore properties that were saved by <see cref="WriteSpecificPropertiesCallback"/>.
            /// Should be used for read-write properties that can be set after creating the collection.
            /// A possible value type result must not be unboxed.
            /// </summary>
            internal Action<BinaryReader, object>? RestoreSpecificPropertiesCallback { get; set; }

            /// <summary>
            /// Should be specified for an array backed collection if specific properties were written by <see cref="WriteSpecificPropertiesCallback"/>.
            /// </summary>
            internal Func<BinaryReader, Type, Array, object>? CreateArrayBackedCollectionInstanceFromArray { get; set; }

#if !NET35
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Local", Justification = "For some targets it is needed to be internal")] 
#endif
            internal bool IsGeneric => (Info & CollectionInfo.IsGeneric) == CollectionInfo.IsGeneric;
            internal bool IsNonGenericCollection => !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None;
            internal bool IsNonGenericDictionary => !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsGenericCollection => IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None;
            internal bool IsGenericDictionary => IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsDictionary => (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsSingleElement => (Info & CollectionInfo.IsSingleElement) == CollectionInfo.IsSingleElement;
            internal bool ReverseElements => (Info & CollectionInfo.ReverseElements) == CollectionInfo.ReverseElements;
            internal bool HasNullableBackingArray => (Info & CollectionInfo.BackingArrayCanBeNull) == CollectionInfo.BackingArrayCanBeNull;
            internal bool HasKnownSizedBackingArray => (Info & CollectionInfo.BackingArrayHasKnownSize) == CollectionInfo.BackingArrayHasKnownSize;
            internal bool IsTuple => (Info & CollectionInfo.IsTuple) == CollectionInfo.IsTuple;
            internal bool HasStringItemsOrKeys => (Info & CollectionInfo.HasStringItemsOrKeys) == CollectionInfo.HasStringItemsOrKeys;
            internal bool CreateResultFromByteArray => (Info & CollectionInfo.CreateResultFromByteArray) == CollectionInfo.CreateResultFromByteArray;

            #endregion

            #region Private Properties

            private bool HasCapacity => (Info & CollectionInfo.HasCapacity) == CollectionInfo.HasCapacity;
            private bool HasEqualityComparer => (Info & CollectionInfo.HasEqualityComparer) == CollectionInfo.HasEqualityComparer;
            private bool HasStringSegmentComparer => (Info & CollectionInfo.HasStringSegmentComparer) == CollectionInfo.HasStringSegmentComparer;
            private bool HasAnyComparer => HasEqualityComparer || HasStringSegmentComparer || (Info & (CollectionInfo.HasComparer)) == CollectionInfo.HasComparer;
            private bool HasCaseInsensitivity => (Info & CollectionInfo.HasCaseInsensitivity) == CollectionInfo.HasCaseInsensitivity;
            private bool HasReadOnly => (Info & CollectionInfo.HasReadOnly) == CollectionInfo.HasReadOnly;
            private bool UsesComparerHelper => (Info & CollectionInfo.UsesComparerHelper) == CollectionInfo.UsesComparerHelper;
            private bool IsNonNullDefaultComparer => (Info & CollectionInfo.NonNullDefaultComparer) == CollectionInfo.NonNullDefaultComparer;
            private bool HasBitwiseAndHash => (Info & CollectionInfo.HasBitwiseAndHash) == CollectionInfo.HasBitwiseAndHash;

            #endregion

            #endregion

            #region Methods

            #region Public Methods

            public override string ToString() => Info.ToString<CollectionInfo>();

            #endregion

            #region Internal Methods

            internal IEnumerable GetCollectionToSerialize(object obj)
            {
                if (IsSingleElement)
                    return new[] { obj is IStrongBox strongBox ? strongBox.Value : obj };
                return (IEnumerable)obj;
            }

            internal DataTypesEnumerator GetKeyDataTypes(DataTypesEnumerator dictionaryDataTypes)
            {
                Debug.Assert(IsDictionary);
                return IsGeneric
                    ? HasStringItemsOrKeys ? new DataTypesEnumerator(DataTypes.String) : dictionaryDataTypes.ReadToNextSegment()
                    : new DataTypesEnumerator(HasStringItemsOrKeys ? DataTypes.String : DataTypes.Object);
            }

            internal DataTypesEnumerator GetValueDataTypes(DataTypesEnumerator dictionaryDataTypes)
            {
                Debug.Assert(IsDictionary);
                return IsGeneric
                    ? dictionaryDataTypes.ReadToNextSegment()
                    : new DataTypesEnumerator(HasStringItemsOrKeys ? DataTypes.String : DataTypes.Object);
            }

            /// <summary>
            /// Writes specific properties of a collection that are needed for deserialization
            /// </summary>
            [SecurityCritical]
            internal void WriteSpecificProperties(BinaryWriter bw, [NoEnumeration]IEnumerable collection, SerializationManager manager)
            {
                if (IsSingleElement)
                    return;

                // 1.) Count
                Write7BitInt(bw, collection.Count());

                // 2.) Capacity - public property in all cases
                if (HasCapacity)
                    Write7BitInt(bw, collection.Capacity());

                // 3.) Case sensitivity - only HybridDictionary
                if (HasCaseInsensitivity)
                    bw.Write(collection.IsCaseInsensitive());

                // 4.) ReadOnly
                if (HasReadOnly)
                {
                    switch (collection)
                    {
                        case IList list:
                            bw.Write(list.IsReadOnly);
                            break;
                        case IDictionary dictionary:
                            bw.Write(dictionary.IsReadOnly);
                            break;
                        default:
                            // should never occur for supported collections
                            bw.Write(false);
                            Debug.Fail("Could not write IsReadOnly state of collection " + collection.GetType());
                            break;
                    }
                }

                // 5.) Bitwise AND hashing
                if (HasBitwiseAndHash)
                    bw.Write(collection.UsesBitwiseAndHash());

                // 6.) Comparer
                if (HasAnyComparer)
                {
                    object? comparer = collection.GetComparer();
                    bool isDefaultComparer = comparer == null || IsDefaultComparer(collection, comparer);
                    bw.Write(isDefaultComparer);
                    if (!isDefaultComparer)
                        manager.WriteNonRoot(bw, comparer!);
                }

                // 7.) Any custom properties
                WriteSpecificPropertiesCallback?.Invoke(bw, collection);
            }

            /// <summary>
            /// Creates collection and reads all serialized specific properties that were written by <see cref="WriteSpecificProperties"/>.
            /// </summary>
            [SecurityCritical]
            internal object InitializeCollection(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor, DeserializationManager manager, bool safeMode, out int count)
            {
                object result;

                // StrongBox, KeyValuePair, DictionaryEntry
                if (IsSingleElement)
                {
                    // Note: If addToCache is true, then the result may contain itself via references.
                    // That's why we create the instance first and then "populate" it (just like at object graphs).

#if NET35
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression - #if
                    if (descriptor.IsStrongBox)
                    {
                        // In .NET Framework 3.5 StrongBox has no parameterless constructor
                        result = DeserializationManager.CreateKnownEmptyObject(descriptor.GetTypeToCreate());
                    }
                    else
#endif
                    {
                        result = Activator.CreateInstance(descriptor.GetTypeToCreate())!;
                    }
                    if (addToCache)
                        manager.AddObjectToCache(result);

                    count = 1;
                    return result;
                }

                // 1.) Count
                count = Read7BitInt(br);

                // 2.) Capacity
                int capacity = HasCapacity ? Read7BitInt(br) : count;
                if (safeMode && (HasCapacity || CtorArguments?.Contains(CollectionCtorArguments.Capacity) == true))
                    capacity = Math.Min(count, IsDictionary
                        ? (capacityThreshold >> 1) / descriptor.GetKeyDescriptor().Type!.SizeOf()
                        : capacityThreshold / descriptor.GetElementDescriptor().Type!.SizeOf());

                // 3.) Case sensitivity
                bool caseInsensitive = false;
                if (HasCaseInsensitivity)
                    caseInsensitive = br.ReadBoolean();

                // 4.) Read-only
                if (HasReadOnly)
                    descriptor.IsReadOnly = br.ReadBoolean();

                // 5.) Bitwise AND hashing
                bool isAndHash = false;
                if (HasBitwiseAndHash)
                    isAndHash = br.ReadBoolean();

                // In the ID cache the collection comes first and then the comparer so we add a placeholder to the cache.
                // Unlike for KeyValuePairs this works here because we can assume that a comparer does not reference the collection.
                int id = 0;
                if (addToCache)
                    manager.AddObjectToCache(null, out id);

                // 6.) Comparer
                object? comparer = HasAnyComparer
                    ? br.ReadBoolean() // is default?
                        ? IsNonNullDefaultComparer ? GetDefaultComparer(descriptor.Type!) : null
                        : manager.ReadWithType(br)
                    : null;

                // creating the result instance
                result = CreateCollection(descriptor, capacity, caseInsensitive, isAndHash, comparer);
                if (id != 0)
                    manager.ReplaceObjectInCache(id, result);

                // 7.) Restoring possible custom properties
                RestoreSpecificPropertiesCallback?.Invoke(br, result);

                return result;
            }

            internal MethodAccessor GetAddMethod(DataTypeDescriptor descriptor)
            {
                MethodAccessor GetAddMethodAccessor(Type type)
                {
                    string methodName = SpecificAddMethod ?? "Add"; // if not specified called for .NET 3.5 with null dictionary/collection values.

                    MethodInfo method = IsGeneric
                        ? type.GetMethod(methodName, type.GetGenericArguments())! // Using type arguments to eliminate ambiguity (LinkedList<T>.AddLast)
                        : type.GetMethod(methodName)!; // For non-generics arguments are not always objects (StringDictionary)
                    return MethodAccessor.GetAccessor(method);
                }

                if (addMethodCache == null)
                    Interlocked.CompareExchange(ref addMethodCache, ThreadSafeCacheFactory.Create<Type, MethodAccessor>(GetAddMethodAccessor, LockFreeCacheOptions.Profile128), null);
                return addMethodCache[descriptor.Type!];
            }

            #endregion

            #region Private Methods

            private bool IsDefaultComparer([NoEnumeration]IEnumerable collection, object? comparer)
            {
                object? defaultComparer = GetDefaultComparer(collection.GetType());
                if (Equals(defaultComparer, comparer))
                    return true;

                if (defaultComparer is Comparer def && comparer is Comparer c)
                    return Equals(def.CompareInfo(), c.CompareInfo());

                return false;
            }

            private object? GetDefaultComparer(Type type)
            {
                if (!IsGeneric)
                    return HasEqualityComparer ? null : Comparer.Default;

                Type elementType = type.GetGenericArguments()[0];
                if (UsesComparerHelper)
                    return HasEqualityComparer
                        ? typeof(ComparerHelper<>).GetPropertyValue(elementType, nameof(ComparerHelper<_>.EqualityComparer))
                        : typeof(ComparerHelper<>).GetPropertyValue(elementType, nameof(ComparerHelper<_>.Comparer));
                if (HasStringSegmentComparer)
                    return StringSegmentComparer.Ordinal;
                return HasEqualityComparer
                    ? typeof(EqualityComparer<>).GetPropertyValue(elementType, nameof(EqualityComparer<_>.Default))
                    : typeof(Comparer<>).GetPropertyValue(elementType, nameof(Comparer<_>.Default));
            }

            private object CreateCollection(DataTypeDescriptor descriptor, int capacity, bool isCaseInsensitive, bool isAndHash, object? comparer)
            {
                CreateInstanceAccessor ctor = GetInitializer(descriptor);
                if (CtorArguments == null)
                    return ctor.CreateInstance();

                object?[] parameters = new object?[CtorArguments.Length];
                for (int i = 0; i < CtorArguments.Length; i++)
                {
                    switch (CtorArguments[i])
                    {
                        case CollectionCtorArguments.Capacity:
                            parameters[i] = capacity;
                            break;
                        case CollectionCtorArguments.Comparer:
                            parameters[i] = comparer;
                            break;
                        case CollectionCtorArguments.CaseInsensitivity:
                            parameters[i] = isCaseInsensitive;
                            break;
                        case CollectionCtorArguments.HashingStrategy:
                            parameters[i] = isAndHash ? HashingStrategy.And : HashingStrategy.Modulo;
                            break;
                        default:
                            return Throw.InternalError<object>($"Unsupported {nameof(CollectionCtorArguments)}");
                    }
                }

                return ctor.CreateInstance(parameters);
            }

            private CreateInstanceAccessor GetInitializer(DataTypeDescriptor descriptor)
            {
                CreateInstanceAccessor GetCtorAccessor(Type type)
                {
                    if (CtorArguments == null)
                        return CreateInstanceAccessor.GetAccessor(type);
                    Type[] args = new Type[CtorArguments.Length];
                    for (int i = 0; i < CtorArguments.Length; i++)
                    {
                        switch (CtorArguments[i])
                        {
                            case CollectionCtorArguments.Capacity:
                                args[i] = Reflector.IntType;
                                break;
                            case CollectionCtorArguments.CaseInsensitivity:
                                args[i] = Reflector.BoolType;
                                break;
                            case CollectionCtorArguments.Comparer:
                                args[i] = IsGeneric
                                    ? HasEqualityComparer
                                        ? typeof(IEqualityComparer<>).GetGenericType(type.GetGenericArguments()[0])
                                        : HasStringSegmentComparer
                                            ? typeof(StringSegmentComparer)
                                            : typeof(IComparer<>).GetGenericType(type.GetGenericArguments()[0])
                                    : HasEqualityComparer
                                        ? typeof(IEqualityComparer)
                                        : typeof(IComparer);
                                break;
                            case CollectionCtorArguments.HashingStrategy:
                                args[i] = typeof(HashingStrategy);
                                break;
                            default:
                                return Throw.InternalError<CreateInstanceAccessor>($"Unsupported {nameof(CollectionCtorArguments)}");
                        }
                    }

                    ConstructorInfo? ctor = type.GetConstructor(args);
                    if (ctor == null)
                        Throw.SerializationException(Res.ReflectionCtorNotFound(type));
                    return CreateInstanceAccessor.GetAccessor(ctor);
                }

                if (ctorCache == null)
                    Interlocked.CompareExchange(ref ctorCache, ThreadSafeCacheFactory.Create<Type, CreateInstanceAccessor>(GetCtorAccessor, LockFreeCacheOptions.Profile128), null);
                return ctorCache[descriptor.Type!];
            }

            #endregion

            #endregion
        }
    }
}
