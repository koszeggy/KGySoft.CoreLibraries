#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.CollectionSerializationInfo.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
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
#if !NET35
using System.Collections.Concurrent;
#endif
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Collections.Generic;
#if NETCOREAPP
using System.Collections.Immutable;
#endif
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
#if NET6_0_OR_GREATER || !NET10_0_OR_GREATER
using System.Linq;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
#if NET10_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
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
            #region Nested Classes

            private sealed class CollectionWrapper : IEnumerable
            {
                #region Fields

                private readonly object wrappedCollection;
                private readonly IEnumerable asEnumerable;

                #endregion

                #region Properties

                internal Type ActualType => wrappedCollection.GetType();
                internal int Count { get; }
                internal int Capacity { get; }
                internal object? Comparer { get; }

                #endregion

                #region Constructors

                [RequiresUnreferencedCode("GetPropertyValue")]
                internal CollectionWrapper(object obj, CollectionInfo info, Func<object, IEnumerable> toCollection)
                {
                    Debug.Assert(obj is not IEnumerable, "Wrapper is not needed for enumerable collections");
                    wrappedCollection = obj;
                    Count = (int)Accessors.GetPropertyValue(obj, "Count")!;
                    asEnumerable = toCollection.Invoke(obj);
                    Capacity = (info & CollectionInfo.HasCapacity) != 0 ? (int)Accessors.GetPropertyValue(obj, "Capacity")! : Count;
                    if ((info & CollectionInfo.HasComparer) != 0)
                        Comparer = Accessors.GetPropertyValue(obj, "Comparer")!;
                }

                #endregion

                #region Methods

                public IEnumerator GetEnumerator() => asEnumerable.GetEnumerator();

                #endregion
            }

            #endregion

            #region Constants

            private const int capacityThreshold = 1 << 13;

            #endregion

            #region Fields

            #region Static Fields

            internal static readonly CollectionSerializationInfo DictionaryEntry = new()
            {
                Info = CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement,
                GetCollectionToSerialize = (o, _) => new[] { o }
            };

            internal static readonly CollectionSerializationInfo KeyValuePair = new()
            {
                Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement,
                GetCollectionToSerialize = (o, _) => new[] { o }
            };

#if !NET35
            internal static readonly CollectionSerializationInfo Tuple = new() { Info = CollectionInfo.IsGeneric | CollectionInfo.IsTuple };
#endif
#if NET10_0_OR_GREATER
            // Single-type tuple-like types are value types with two or more elements of the same element type inlined in the struct, e.g. Complex<>, Argb<>, InlineArray##<>.
            [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "ArrayToSingleTypeTuple and ObjectListToSingleTypeTuple are used only in non-AOT mode; ObjectListToSingleTypeTuple handles missing metadata")]
            [UnconditionalSuppressMessage("TrimAnalysis", "IL2111:DynamicallyAccessedMembersAttributeViaReflection",
                Justification = "ArrayToSingleTypeTuple and ObjectListToSingleTypeTuple are used only in non-AOT mode; ObjectListToSingleTypeTuple handles missing metadata")]
            [UnconditionalSuppressMessage("TrimAnalysis", "IL3050:RequiresDynamicCode",
                Justification = "ObjectListToSingleTypeTuple handles if dynamic code cannot be used.")]
            internal static readonly CollectionSerializationInfo TupleSingleType = new()
            {
                Info = CollectionInfo.IsGeneric | CollectionInfo.BackingArrayHasKnownSize,
                GetBackingArray = SingleTypeTupleToArray,
                CreateArrayBackedCollectionInstanceFromArray = RuntimeFeature.IsDynamicCodeSupported ? ArrayToSingleTypeTuple : null,
                CreateInstanceCallback = RuntimeFeature.IsDynamicCodeSupported ? null : (_, _) => new List<object>(),
                CreateFinalCollectionCallback = RuntimeFeature.IsDynamicCodeSupported ? null : ObjectListToSingleTypeTuple,
            };
#endif

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            internal static readonly CollectionSerializationInfo Memory = new() { Info = CollectionInfo.IsGeneric | CollectionInfo.IsMemory };
#endif

#if NET6_0_OR_GREATER
            [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "See the [DynamicDependency] attributes.")]
            [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch", Justification = "See the [DynamicDependency] attributes")]
            internal static readonly CollectionSerializationInfo PriorityQueue = new CollectionSerializationInfo
            {
                // NOTE: a public PriorityQueue<,>.Capacity property has only been added in .NET10. For binary compatibility, storing capacity even in lower versions.
                Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasCapacity | CollectionInfo.HasComparer | CollectionInfo.ComparerUsesSecondArgument,
                CtorArguments = [CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer],
                GetCollectionToSerialize =
                    [DynamicDependency(nameof(PriorityQueue<,>.Count), typeof(PriorityQueue<,>))]
#if NET10_0_OR_GREATER
                    [DynamicDependency(nameof(PriorityQueue<,>.Capacity), typeof(PriorityQueue<,>))]
#endif
                    [DynamicDependency(nameof(PriorityQueue<,>.Comparer), typeof(PriorityQueue<,>))]
                    [DynamicDependency(nameof(PriorityQueue<,>.UnorderedItems), typeof(PriorityQueue<,>))]
                    (o, info) => new CollectionWrapper(o,
#if NET10_0_OR_GREATER
                        info,
#else
                        info & ~CollectionInfo.HasCapacity,
#endif
                        queue =>
                    {
                        var items = (IEnumerable)Accessors.GetPropertyValue(queue, nameof(PriorityQueue<,>.UnorderedItems))!;
                        return items.Cast<ITuple>().Select(t => new DictionaryEntry(t[0]!, t[1]));
                    }),
                GetSpecificAddMethod =
                    [DynamicDependency(nameof(PriorityQueue<,>.Enqueue), typeof(PriorityQueue<,>))]
                    (t) => MethodAccessor.GetAccessor(t.GetMethod(nameof(PriorityQueue<,>.Enqueue))!),
            };
#endif

            #endregion

            #region Instance Fields

            /// <summary>
            /// Can contain more elements only for generic collections. Will be instantiated only on deserialization.
            /// Thread safe accessor because the serialization info is stored in a static shared dictionary.
            /// </summary>
            private LockFreeCache<Type, CreateInstanceAccessor>? ctorCache;

            #endregion

            #endregion

            #region Properties

            #region Internal Properties

            internal CollectionInfo Info { get; set; }

            /// <summary>
            /// Specifies the constructor arguments to be used. Order matters!
            /// Can be used also to specify the arguments for <see cref="CreateInstanceCallback"/>.
            /// </summary>
            internal CollectionCtorArguments[]? CtorArguments { private get; set; }

            /// <summary>
            /// Should be specified only when target collection is not <see cref="IList"/>, <see cref="IDictionary"/> or <see cref="ICollection{T}"/> implementation,
            /// or when defining it results faster access than resolving the generic Add method for each access.
            /// It can also be used for special cases, such as inserting the final resolved items into an ordered dictionary whereas in normal case the <see cref="IDictionary"/> members are used.
            /// </summary>
            internal Func<Type, MethodAccessor>? GetSpecificAddMethod { get; set; }

            /// <summary>
            /// Should be specified if the collection is a value type, and it has a publicly exposed backing array that can have more elements than the wrapper type (e.g. ArraySegment),
            /// or, if it does not expose or wrap any array, but it can be represented as a (fixed size) array (e.g. Vector128).
            /// If the collection does not actually wrap the array, then it must not contain any direct circular reference to the object itself because it makes proper deserialization impossible (e.g. object element type must not be supported by the collection).
            /// For deserialization <see cref="CreateArrayBackedCollectionInstanceFromArray"/> must be used if the array is actually stored.
            /// Otherwise, <see cref="CreateInstanceCallback"/> with <see cref="CreateFinalCollectionCallback"/> also can be used to restore the collection.
            /// </summary>
            internal Func<object, Array?>? GetBackingArray
            {
                [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)] // GetGenericType usages in the callback methods
                [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
                get;
                set;
            }

            internal Func<object, CollectionInfo, IEnumerable>? GetCollectionToSerialize { get; set; }

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
            /// Should be specified to instantiate an array backed collection. Should also read specific properties written by <see cref="WriteSpecificPropertiesCallback"/>.
            /// Must be used when the collection actually stores the underlying array. Can also be used when the non-stored array can be represented as a known array type (e.g. byte[]).
            /// If the array is used for serialization only, but is not actually stored and can have any element type, use <see cref="CreateInstanceCallback"/>
            /// and <see cref="CreateFinalCollectionCallback"/> instead to support AOT mode so no actual array has to be created with an arbitrary element type.
            /// </summary>
            internal Func<BinaryReader, Type, Array, object>? CreateArrayBackedCollectionInstanceFromArray
            {
                [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)] // GetGenericType usages in the callback methods
                [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
                get;
                set;
            }

            /// <summary>
            /// Gets a delegate that can instantiate the (possibly proxy) collection instance to populate. The parameters depend on <see cref="CtorArguments"/>.
            /// If this is not the final instance the <see cref="CreateFinalCollectionCallback"/> should be also set.
            /// </summary>
            internal Func<Type, object?[], object>? CreateInstanceCallback
            {
                [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)] // GetGenericType usages in the callback methods
                [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
                get;
                set;
            }

            /// <summary>
            /// If <see cref="CreateInstanceCallback"/> returns a proxy type, then this delegate can return the final result from the builder collection.
            /// NOTE: Using this property may prevent deserialization of circular references.
            /// </summary>
            internal Func<Type, object, object>? CreateFinalCollectionCallback
            {
                [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)] // GetGenericType usages in the callback methods
                [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
                get;
                set;
            }

            /// <summary>
            /// Should be set if the supported public type is an abstract generic type, and we need to support its non-public derived instances (e.g. comparers, frozen collections).
            /// Required only for generic types if the possible derived instances may have different type arguments (if any).
            /// </summary>
            internal Type? ReferenceAbstractGenericType { get; set; }

#if !NET35
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Local", Justification = "For some targets it is needed to be internal")] 
#endif
            internal bool IsGeneric => (Info & CollectionInfo.IsGeneric) == CollectionInfo.IsGeneric;
            internal bool IsNonGenericCollection => !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None;
            internal bool IsNonGenericDictionary => !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsGenericCollection => IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None;
            internal bool IsGenericDictionary => IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsDictionary => (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsOrdered => (Info & CollectionInfo.IsOrdered) == CollectionInfo.IsOrdered;
            internal bool IsSingleElement => (Info & CollectionInfo.IsSingleElement) == CollectionInfo.IsSingleElement;
            internal bool ReverseElements => (Info & CollectionInfo.ReverseElements) == CollectionInfo.ReverseElements;
            internal bool IsBackingArrayActuallyStored => (Info & CollectionInfo.IsBackingArrayActuallyStored) == CollectionInfo.IsBackingArrayActuallyStored;
            internal bool HasKnownSizedBackingArray => (Info & CollectionInfo.BackingArrayHasKnownSize) == CollectionInfo.BackingArrayHasKnownSize;
            internal bool IsTuple => (Info & CollectionInfo.IsTuple) == CollectionInfo.IsTuple;
            internal bool HasStringItemsOrKeys => (Info & CollectionInfo.HasStringItemsOrKeys) == CollectionInfo.HasStringItemsOrKeys;
            internal bool CreateResultFromByteArray => (Info & CollectionInfo.CreateResultFromByteArray) == CollectionInfo.CreateResultFromByteArray;
            internal bool IsComparer => (Info & CollectionInfo.IsComparer) == CollectionInfo.IsComparer;
            internal bool IsMemory => (Info & CollectionInfo.IsMemory) == CollectionInfo.IsMemory;

            #endregion

            #region Private Properties

            private bool HasCapacity => (Info & CollectionInfo.HasCapacity) == CollectionInfo.HasCapacity;
            private bool HasEqualityComparer => (Info & CollectionInfo.HasEqualityComparer) == CollectionInfo.HasEqualityComparer;
            private bool HasStringSegmentComparer => (Info & CollectionInfo.HasStringSegmentComparer) == CollectionInfo.HasStringSegmentComparer;
            private bool HasAnyComparer => (Info & (CollectionInfo.HasComparer | CollectionInfo.HasEqualityComparer | CollectionInfo.HasStringSegmentComparer)) != CollectionInfo.None;
            private bool HasValueComparer => (Info & CollectionInfo.HasValueComparer) != CollectionInfo.None;
            private bool HasCaseInsensitivity => (Info & CollectionInfo.HasCaseInsensitivity) == CollectionInfo.HasCaseInsensitivity;
            private bool HasReadOnly => (Info & CollectionInfo.HasReadOnly) == CollectionInfo.HasReadOnly;
            private bool UsesComparerHelper => (Info & CollectionInfo.UsesComparerHelper) == CollectionInfo.UsesComparerHelper;
            private bool ComparerUsesSecondArgument => (Info & CollectionInfo.ComparerUsesSecondArgument) == CollectionInfo.ComparerUsesSecondArgument;
            private bool IsNonNullDefaultComparer => (Info & CollectionInfo.NonNullDefaultComparer) == CollectionInfo.NonNullDefaultComparer;
            private bool HasBitwiseAndHash => (Info & CollectionInfo.HasBitwiseAndHash) == CollectionInfo.HasBitwiseAndHash;

            #endregion

            #endregion

            #region Methods

            #region Public Methods

            public override string ToString() => Info.ToString<CollectionInfo>();

            #endregion

            #region Internal Methods

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
            /// <returns>true if the whole write is finished (e.g. default instance); otherwise, false</returns>
            [SecurityCritical]
            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            internal bool WriteSpecificProperties(BinaryWriter bw, [NoEnumeration]IEnumerable collection, SerializationManager manager)
            {
                #region Local Methods

                [RequiresUnreferencedCode("TryGetPropertyValue, GetFieldValue")]
                [DynamicDependency(nameof(HashSet<>.Count), typeof(HashSet<>))]
#if NETCOREAPP
                [DynamicDependency(nameof(ImmutableArray<>.Builder.Count), typeof(ImmutableArray<>.Builder))]
                [DynamicDependency(nameof(ImmutableHashSet<>.Builder.Count), typeof(ImmutableHashSet<>.Builder))]
#endif
                [DynamicDependency(nameof(StringDictionary.Count), typeof(StringDictionary))]
                static void WriteCount(BinaryWriter bw, [NoEnumeration]IEnumerable collection)
                {
                    int count = collection switch
                    {
                        ICollection coll => coll.Count,
                        CollectionWrapper wrapper => wrapper.Count,
                        _ => (int)Accessors.GetPropertyValue(collection, "Count")!
                    };

                    Write7BitInt(bw, count);
                }

                [RequiresUnreferencedCode("GetPropertyValue")]
                [DynamicDependency(nameof(List<>.Capacity), typeof(List<>))]
                [DynamicDependency(nameof(CircularList<>.Capacity), typeof(CircularList<>))]
                [DynamicDependency(nameof(SortedList<,>.Capacity), typeof(SortedList<,>))]
                [DynamicDependency(nameof(SortedList.Capacity), typeof(SortedList))]
                [DynamicDependency(nameof(CircularSortedList<,>.Capacity), typeof(CircularSortedList<,>))]
                [DynamicDependency(nameof(ArrayList.Capacity), typeof(ArrayList))]
#if NET9_0_OR_GREATER
                [DynamicDependency(nameof(OrderedDictionary<,>.Capacity), typeof(OrderedDictionary<,>))]
#endif
#if NETCOREAPP
                [DynamicDependency(nameof(ImmutableArray<>.Builder.Capacity), typeof(ImmutableArray<>.Builder))]
#endif
                static void WriteCapacity(BinaryWriter bw, [NoEnumeration]IEnumerable collection)
                {
                    int capacity = collection is CollectionWrapper wrapper
                        ? wrapper.Capacity
                        : (int)Accessors.GetPropertyValue(collection, "Capacity")!;
                    Write7BitInt(bw, capacity);
                }

                [RequiresUnreferencedCode("GetProperty, GetField")]
                [DynamicDependency(nameof(HashSet<>.Comparer), typeof(HashSet<>))]
                [DynamicDependency(nameof(ThreadSafeHashSet<>.Comparer), typeof(ThreadSafeHashSet<>))]
#if !NET35
                [DynamicDependency(nameof(SortedSet<>.Comparer), typeof(SortedSet<>))]
#endif
                [DynamicDependency(nameof(Dictionary<,>.Comparer), typeof(Dictionary<,>))]
                [DynamicDependency(nameof(SortedList<,>.Comparer), typeof(SortedList<,>))]
                [DynamicDependency(nameof(SortedDictionary<,>.Comparer), typeof(SortedDictionary<,>))]
                [DynamicDependency(nameof(CircularSortedList<,>.Comparer), typeof(CircularSortedList<,>))]
#if NET6_0_OR_GREATER
                [DynamicDependency(nameof(ConcurrentDictionary<,>.Comparer), typeof(ConcurrentDictionary<,>))]
#elif !NET35
                [DynamicDependency("_comparer", typeof(ConcurrentDictionary<,>))]
#endif
                [DynamicDependency(nameof(ThreadSafeDictionary<,>.Comparer), typeof(ThreadSafeDictionary<,>))]
                [DynamicDependency(nameof(AllowNullDictionary<,>.Comparer), typeof(AllowNullDictionary<,>))]
#if NET9_0_OR_GREATER
                [DynamicDependency(nameof(OrderedDictionary<,>.Comparer), typeof(OrderedDictionary<,>))]
#endif
                [DynamicDependency("EqualityComparer", typeof(Hashtable))]
                [DynamicDependency("comparer", typeof(SortedList))]
                [DynamicDependency("comparer", typeof(ListDictionary))]
                [DynamicDependency("_comparer", typeof(OrderedDictionary))]
                [DynamicDependency(nameof(StringKeyedDictionary<>.Comparer), typeof(StringKeyedDictionary<>))]
#if NETCOREAPP
                [DynamicDependency(nameof(ImmutableHashSet<>.KeyComparer), typeof(ImmutableHashSet<>))]
                [DynamicDependency(nameof(ImmutableHashSet<>.Builder.KeyComparer), typeof(ImmutableHashSet<>.Builder))]
                [DynamicDependency(nameof(ImmutableSortedSet<>.KeyComparer), typeof(ImmutableSortedSet<>))]
                [DynamicDependency(nameof(ImmutableSortedSet<>.Builder.KeyComparer), typeof(ImmutableSortedSet<>.Builder))]
                [DynamicDependency(nameof(ImmutableDictionary<,>.KeyComparer), typeof(ImmutableDictionary<,>))]
                [DynamicDependency(nameof(ImmutableDictionary<,>.Builder.KeyComparer), typeof(ImmutableDictionary<,>.Builder))]
                [DynamicDependency(nameof(ImmutableSortedDictionary<,>.KeyComparer), typeof(ImmutableSortedDictionary<,>))]
                [DynamicDependency(nameof(ImmutableSortedDictionary<,>.Builder.KeyComparer), typeof(ImmutableSortedDictionary<,>.Builder))]
#endif
#if NET8_0_OR_GREATER
                [DynamicDependency(nameof(FrozenSet<>.Comparer), typeof(FrozenSet<>))]
                [DynamicDependency(nameof(FrozenDictionary<,>.Comparer), typeof(FrozenDictionary<,>))]
#endif
                static object? GetComparer([NoEnumeration]IEnumerable collection)
                {
                    if (collection is CollectionWrapper wrapper)
                        return wrapper.Comparer;

                    // by Comparer/EqualityComparer/KeyComparer property
                    Type type = collection.GetType();
                    string propertyName = type.Name switch
                    {
                        nameof(Hashtable) => "EqualityComparer",
                        "ImmutableHashSet`1" or "ImmutableSortedSet`1" or "ImmutableDictionary`2" or "ImmutableSortedDictionary`2" or "Builder" => "KeyComparer",
                        _ => "Comparer" // Dictionary<TKey, TValue>, HashSet<T>, SortedSet<T>, SortedList<TKey, TValue>, SortedDictionary<TKey, TValue>, CircularSortedList<TKey, TValue>, ThreadSafeHashSet<T>, ThreadSafeDictionary<TKey, TValue>, StringKeyedDictionary<TValue>
                    };

                    if (Accessors.TryGetPropertyValue(collection, propertyName, out object? comparer))
                        return comparer;

                    // by *comparer* field
                    return Accessors.GetFieldValue(collection, "comparer");
                }

                [RequiresUnreferencedCode("GetPropertyValue")]
#if NETCOREAPP
                [DynamicDependency(nameof(ImmutableDictionary<,>.ValueComparer), typeof(ImmutableDictionary<,>))]
                [DynamicDependency(nameof(ImmutableDictionary<,>.Builder.ValueComparer), typeof(ImmutableDictionary<,>.Builder))]
                [DynamicDependency(nameof(ImmutableSortedDictionary<,>.ValueComparer), typeof(ImmutableSortedDictionary<,>))]
                [DynamicDependency(nameof(ImmutableSortedDictionary<,>.Builder.ValueComparer), typeof(ImmutableSortedDictionary<,>.Builder))]
#endif
                static object? GetValueComparer([NoEnumeration]IEnumerable collection) => Accessors.GetPropertyValue(collection, "ValueComparer");

                #endregion

                if (IsSingleElement)
                    return false;

                // 1.) Count
                WriteCount(bw, collection);

                // 2.) Capacity - public property in all cases
                if (HasCapacity)
                    WriteCapacity(bw, collection);

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
                    object? comparer = GetComparer(collection);
                    bool isDefaultComparer = comparer == null || IsDefaultComparer(collection, comparer);
                    bw.Write(isDefaultComparer);
                    if (!isDefaultComparer)
                        manager.WriteNonRoot(bw, comparer!);

                    if (HasValueComparer)
                    {
                        comparer = GetValueComparer(collection);
                        isDefaultComparer = comparer == null || IsDefaultValueComparer(collection, comparer);
                        bw.Write(isDefaultComparer);
                        if (!isDefaultComparer)
                            manager.WriteNonRoot(bw, comparer!);
                    }
                }

                // 7.) Any custom properties
                WriteSpecificPropertiesCallback?.Invoke(bw, collection);

                return false;
            }

            /// <summary>
            /// Creates collection and reads all serialized specific properties that were written by <see cref="WriteSpecificProperties"/>.
            /// </summary>
            [SecurityCritical]
            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            internal object InitializeCollection(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor, DeserializationManager manager, bool safeMode, out int count, out int id)
            {
                object result;
                id = 0;
                if (IsComparer)
                {
                    Debug.Assert(CreateInstanceCallback != null, $"{nameof(CreateInstanceCallback)} must be specified for comparers");
                    result = CreateInstanceCallback!.Invoke(descriptor.GetTypeToCreate(), Reflector.EmptyObjects);
                    if (addToCache)
                        manager.AddObjectToCache(result, out id);

                    count = 0;
                    return result;
                }

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
                        manager.AddObjectToCache(result, out id);

                    count = 1;
                    return result;
                }

                // 1.) Count
#if NET10_0_OR_GREATER
                if (descriptor.IsTupleSingleType)
                {
                    count = descriptor.FixedItemsSize;
                    Debug.Assert(CreateInstanceCallback != null, $"{nameof(CreateInstanceCallback)} must be specified for single type tuples");
                    result = CreateInstanceCallback!.Invoke(descriptor.GetTypeToCreate(), Reflector.EmptyObjects);
                    if (addToCache)
                        manager.AddObjectToCache(result, out id);

                    return result;
                }
#endif

                count = Read7BitInt(br);

                // 2.) Capacity
                int capacity = HasCapacity ? Read7BitInt(br) : count;
                if (safeMode && (HasCapacity || CtorArguments?.Contains(CollectionCtorArguments.Capacity) == true))
                {
                    int threshold = IsDictionary
                        ? (capacityThreshold >> 1) / descriptor.GetKeyDescriptor().Type!.SizeOf(true)
                        : capacityThreshold / descriptor.GetElementDescriptor().Type!.SizeOf(true);

                    if (capacity > threshold)
                        capacity = Math.Min(count, threshold);
                }

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
                if (addToCache)
                    manager.AddObjectToCache(null, out id);

                // 6.) Comparer
                object? comparer = HasAnyComparer
                    ? br.ReadBoolean() // is default?
                        ? IsNonNullDefaultComparer ? GetDefaultComparer(descriptor.Type!) : null
                        : manager.ReadWithType(br)
                    : null;
                object? valueComparer = HasValueComparer
                    ? br.ReadBoolean() // is default?
                        ? null
                        : manager.ReadWithType(br)
                    : null;

                // creating the result instance
                result = CreateCollection(descriptor, capacity, caseInsensitive, isAndHash, comparer, valueComparer);
                if (id != 0)
                    manager.ReplaceObjectInCache(id, result); // it still can be replaced later if result is just a proxy builder

                // 7.) Restoring possible custom properties
                RestoreSpecificPropertiesCallback?.Invoke(br, result);

                return result;
            }

            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            internal object GetFinalCollection(object result, DataTypeDescriptor descriptor)
            {
                if (descriptor.IsReadOnly)
                    result = descriptor.GetAsReadOnly(result);
                return CreateFinalCollectionCallback?.Invoke(descriptor.Type!, result) ?? result;
            }

            internal Type GetStoredType(Type type)
            {
                // If encoding as a known implementation of a public abstract type, then returning the constructed abstract generic type
                if (ReferenceAbstractGenericType is Type toReturn)
                {
                    while (!type.IsGenericType || type.GetGenericTypeDefinition() != toReturn)
                        type = type.BaseType!;
                }

                return type;
            }

            internal Type GetElementType(Type type) => HasStringItemsOrKeys ? Reflector.StringType : GetStoredType(type).GetGenericArguments()[0];
            internal Type GetValueType(Type type) => HasStringItemsOrKeys ? type.GetGenericArguments()[0] : GetStoredType(type).GetGenericArguments()[1];

            #endregion

            #region Private Methods

            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            private bool IsDefaultComparer([NoEnumeration]IEnumerable collection, object? comparer)
            {
                object? defaultComparer = GetDefaultComparer((collection as CollectionWrapper)?.ActualType ?? collection.GetType());
                if (Equals(defaultComparer, comparer))
                    return true;

                if (defaultComparer is Comparer def && comparer is Comparer c)
                    return Equals(def.CompareInfo(), c.CompareInfo());

                return false;
            }

            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            private bool IsDefaultValueComparer([NoEnumeration]IEnumerable collection, object? comparer)
            {
                object? defaultComparer = GetDefaultValueComparer(collection.GetType());
                return Equals(defaultComparer, comparer);
            }

            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            [DynamicDependency(nameof(ComparerHelper<>.EqualityComparer), typeof(ComparerHelper<>))]
            [DynamicDependency(nameof(ComparerHelper<>.Comparer), typeof(ComparerHelper<>))]
            [DynamicDependency(nameof(EqualityComparer<>.Default), typeof(EqualityComparer<>))]
            [DynamicDependency(nameof(Comparer<>.Default), typeof(Comparer<>))]
            private object? GetDefaultComparer(Type type)
            {
                if (!IsGeneric)
                    return HasEqualityComparer ? null : Comparer.Default;
                if (HasStringSegmentComparer)
                    return StringSegmentComparer.Ordinal;

                Type elementType = ComparerUsesSecondArgument ? GetValueType(type) : GetElementType(type);
                if (UsesComparerHelper)
                    return HasEqualityComparer
                        ? typeof(ComparerHelper<>).GetPropertyValue(elementType, nameof(ComparerHelper<>.EqualityComparer))
                        : typeof(ComparerHelper<>).GetPropertyValue(elementType, nameof(ComparerHelper<>.Comparer));
                return HasEqualityComparer
                    ? typeof(EqualityComparer<>).GetPropertyValue(elementType, nameof(EqualityComparer<>.Default))
                    : typeof(Comparer<>).GetPropertyValue(elementType, nameof(Comparer<>.Default));
            }

            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            [DynamicDependency(nameof(EqualityComparer<>.Default), typeof(EqualityComparer<>))]
            private object? GetDefaultValueComparer(Type type)
            {
                Debug.Assert(IsGeneric && IsDictionary && !UsesComparerHelper && !HasStringSegmentComparer);

                Type valueType = type.GetGenericArguments()[1];
                return typeof(EqualityComparer<>).GetPropertyValue(valueType, nameof(EqualityComparer<>.Default));
            }

            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            private object CreateCollection(DataTypeDescriptor descriptor, int capacity, bool isCaseInsensitive, bool isAndHash, object? comparer, object? valueComparer)
            {
                object?[] parameters = Reflector.EmptyObjects;

                if (CtorArguments?.Length > 0)
                {
                    parameters = new object?[CtorArguments.Length];
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
                            case CollectionCtorArguments.ValueComparer:
                                parameters[i] = valueComparer;
                                break;
                            default:
                                return Throw.InternalError<object>($"Unsupported {nameof(CollectionCtorArguments)}");
                        }
                    }
                }

                if (CreateInstanceCallback is Func<Type, object?[], object> createCollectionToPopulateCallback)
                    return createCollectionToPopulateCallback.Invoke(descriptor.GetTypeToCreate(), parameters);

                CreateInstanceAccessor ctor = GetInitializer(descriptor);
                return ctor.CreateInstance(parameters);
            }

            [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
            [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
            private CreateInstanceAccessor GetInitializer(DataTypeDescriptor descriptor)
            {
                #region Local Methods

                [RequiresDynamicCode(BinarySerializer.RequiresDynamicCodeMessage)]
                [RequiresUnreferencedCode(BinarySerializer.RequiresUnreferencedCodeMessage)]
                CreateInstanceAccessor GetCtorAccessor([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors)]Type type)
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
                                Type? typeArg = !type.IsGenericType
                                    ? null
                                    : type.GetGenericArguments()[ComparerUsesSecondArgument ? 1 : 0];
                                args[i] = IsGeneric
                                    ? HasEqualityComparer
                                        ? typeof(IEqualityComparer<>).GetGenericType(typeArg!)
                                        : HasStringSegmentComparer
                                            ? typeof(StringSegmentComparer)
                                            : typeof(IComparer<>).GetGenericType(typeArg!)
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

                #endregion

                if (ctorCache == null)
                    Interlocked.CompareExchange(ref ctorCache, new LockFreeCache<Type, CreateInstanceAccessor>(GetCtorAccessor, null, LockFreeCacheOptions.Profile128), null);
                return ctorCache[descriptor.Type!];
            }

#if NET10_0_OR_GREATER
            /// <summary>
            /// Copies a single-type tuple-like struct (e.g. Complex`1, InlineArray##`1) into a real array.
            /// Primitive generic arguments produce a byte[] (with adjusted endianness), which is processed very quickly by the caller.
            /// Otherwise, the element type of the array matches the generic argument, except in AOT mode, in which case an object[] is returned.
            /// </summary>
            [RequiresDynamicCode("IsManaged, CopyManagedSpan, CopyManagedItem, SerializeValueTypeInternal")]
            [RequiresUnreferencedCode("CopyManagedSpan, CopyManagedItem")]
            private static Array SingleTypeTupleToArray(object obj)
            {
                Debug.Assert(obj is ValueType);
                Type type = obj.GetType();
                Type elementType = type.GenericTypeArguments[0];

                int size = type.SizeOf(false);
                int elementSize = elementType.SizeOf(false);
                int length = size / elementSize;
                Debug.Assert(size % elementSize == 0, "Bad tuple or element size");

                if (elementType.IsValueType)
                {
                    // primitive element type: as byte[] with proper endianness
                    if (elementType.IsPrimitive)
                    {
                        byte[] result = BinarySerializer.SerializeValueTypeInternal((ValueType)obj);
                        EnsureEndianness(type, result);
                        return result;
                    }

                    // non-primitive value element type: in non-AOT mode returning a strongly typed array of the elements
                    if (RuntimeFeature.IsDynamicCodeSupported)
                    {
                        Array result = Array.CreateInstance(elementType, length);
#if NET12_0_OR_GREATER
#error check if already available - see https://github.com/dotnet/runtime/issues/135159
                        RuntimeHelpers.Copy(ref Reflector.GetRawData(obj), ref Reflector.GetRawData(result), elementType.TypeHandle, length);
#else
                        // If the element type has references, we must copy the element with a write barrier, but Buffer.BulkMoveWithWriteBarrier is not public
                        if (!(Nullable.GetUnderlyingType(elementType) ?? elementType).IsManaged())
                            Unsafe.CopyBlock(ref Reflector.GetRawData(result), ref Reflector.GetRawData(obj), (uint)size);
                        else if (!MemoryHelper.CopyManagedSpan(elementType, obj, result, length))
                            Throw.NotSupportedException(Res.BinarySerializationInlineArrayNotSupportedAot(elementType));
#endif
                        return result;
                    }
                }

                // reference type: just copying as span of objects
                object[] asObjArray = new object[length];
                if (!elementType.IsValueType)
                {
                    MemoryMarshal.CreateSpan(ref obj.As<StrongBox<object>>().Value!, length).CopyTo(asObjArray);
                    return asObjArray;
                }

                // Value type in AOT mode: creating a copy of each element in the result object array.
                // If element size is 1, just assigning the bytes directly instead of bulk copying.
                Span<byte> rawBytes = Reflector.GetRawBytes(obj);
                if (elementSize == 1)
                {
                    for (int i = 0; i < length; i++)
                    {
                        object item = RuntimeHelpers.GetUninitializedObject(elementType);
                        Reflector.GetRawData(item) = rawBytes[i];
                        asObjArray[i] = item;
                    }

                    return asObjArray;
                }

                bool isNullable = elementType.IsNullable();
                Type underlyingElementType = isNullable ? Nullable.GetUnderlyingType(elementType)! : elementType;
                int underlyingElementSize = underlyingElementType.SizeOf(false);
                int nullableOffset = isNullable ? elementSize - underlyingElementType.SizeOf(false) : 0;
                bool isUnmanaged = !underlyingElementType.IsManaged();
                for (int i = 0; i < length; i++)
                {
                    // skipping null elements: checking Nullable<T>.hasValue
                    if (isNullable && rawBytes[i * elementSize] == 0)
                        continue;

                    // asObjArray[i] = (object)obj<elementType>[i]
                    object item = RuntimeHelpers.GetUninitializedObject(underlyingElementType);
#if NET12_0_OR_GREATER
                    TODO: RuntimeHelpers.Copy; // see https://github.com/dotnet/runtime/issues/135159
#endif
                    if (isUnmanaged)
                        Unsafe.CopyBlockUnaligned(ref Reflector.GetRawData(item), ref rawBytes[i * elementSize + nullableOffset], (uint)underlyingElementSize);
                    else if (!MemoryHelper.CopyManagedItem(underlyingElementType, obj, i * elementSize + nullableOffset, item, 0))
                        Throw.NotSupportedException(Res.BinarySerializationInlineArrayNotSupportedAot(elementType));

                    asObjArray[i] = item;
                }

                return asObjArray;
            }

            [RequiresDynamicCode("IsManaged, CopyManagedSpan")]
            [RequiresUnreferencedCode("CopyManagedSpan")]
            private static object ArrayToSingleTypeTuple(BinaryReader _, [DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors)]Type type, Array array)
            {
                Type elementType = type.GenericTypeArguments[0];
                Debug.Assert(array.GetType().GetElementType() == elementType);

                object result = RuntimeHelpers.GetUninitializedObject(type);
#if NET12_0_OR_GREATER
                    TODO: RuntimeHelpers.Copy; // see https://github.com/dotnet/runtime/issues/135159
#endif
                if (!elementType.IsValueType)
                {
                    Span<object> src = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, object>(ref MemoryMarshal.GetArrayDataReference(array)), array.Length);
                    Span<object> dst = MemoryMarshal.CreateSpan(ref Unsafe.As<StrongBox<object>>(result).Value!, src.Length);
                    src.CopyTo(dst);
                }
                else if (!elementType.IsManaged())
                    Unsafe.CopyBlock(ref Reflector.GetRawData(result), ref Reflector.GetRawData(array), (uint)type.SizeOf(false));
                else if (!MemoryHelper.CopyManagedSpan(elementType, array, result, array.Length))
                    Throw.NotSupportedException(Res.BinarySerializationInlineArrayNotSupportedAot(elementType));

                return result;
            }

            [RequiresDynamicCode("IsManaged, CopyManagedItem")]
            [RequiresUnreferencedCode("CopyManagedItem")]
            private static object ObjectListToSingleTypeTuple(Type type, object list)
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
                Debug.Assert(!RuntimeFeature.IsDynamicCodeSupported && type.IsValueType && type.IsGenericType);
                Type elementType = type.GenericTypeArguments[0];
                var items = (List<object?>)list;
                int elementSize = elementType.SizeOf(false);
                Debug.Assert(type.SizeOf(false) % elementSize == 0 && items.Count == type.SizeOf(false) / elementSize);
                object result = RuntimeHelpers.GetUninitializedObject(type);

                // reference element type: simply copying the list elements into the result
                if (!elementType.IsValueType)
                {
                    CollectionsMarshal.AsSpan(items).CopyTo(MemoryMarshal.CreateSpan(ref result.As<StrongBox<object?>>().Value, items.Count));
                    return result;
                }

                Span<byte> rawBytes = Reflector.GetRawBytes(result);

                // shortcut: assigning the bytes directly, instead of bulk copying the items
                if (elementSize == 1)
                {
                    Debug.Assert(!items.Contains(null));
                    for (int i = 0; i < items.Count; i++)
                        rawBytes[i] = Reflector.GetRawData(items[i]!);

                    return result;
                }

                bool isNullable = elementType.IsNullable();
                Type underlyingElementType = isNullable ? Nullable.GetUnderlyingType(elementType)! : elementType;
                int underlyingElementSize = underlyingElementType.SizeOf(false);
                int nullableOffset = isNullable ? elementSize - underlyingElementType.SizeOf(false) : 0;
                bool isUnmanaged = !underlyingElementType.IsManaged();

                // Copying the item data in the result tuple. Nullable items with null value are null references in the object list, but are Nullable<elementType> items in result.
                for (int i = 0; i < items.Count; i++)
                {
                    object? item = items[i];
                    if (item == null)
                    {
                        // skipping nullable null values (no need to zero out the element, RuntimeHelpers.GetUninitializedObject returns a bitwise-zero instance)
                        Debug.Assert(isNullable);
                        continue;
                    }

                    if (isNullable)
                        rawBytes[i * elementSize] = Convert.ToByte(true); // setting Nullable<elementType>.hasValue to true

#if NET12_0_OR_GREATER
                    TODO: RuntimeHelpers.Copy; // see https://github.com/dotnet/runtime/issues/135159
#endif
                    if (isUnmanaged)
                        Unsafe.CopyBlockUnaligned(ref rawBytes[i * elementSize + nullableOffset], ref Reflector.GetRawData(item), (uint)underlyingElementSize);
                    else if (!MemoryHelper.CopyManagedItem(elementType, item, 0, result, i * elementSize + nullableOffset))
                        Throw.NotSupportedException(Res.BinarySerializationInlineArrayNotSupportedAot(elementType));
                }

                return result;
            }
#endif

            #endregion

            #endregion
        }
    }
}
