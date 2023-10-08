#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeHashSet.cs
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
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;
using KGySoft.Serialization.Binary;
using KGySoft.Threading;

#endregion

#region Suppressions

#if NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Implements a thread-safe hash set, which has similar characteristics to <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
    /// It can be a good alternative for <see cref="HashSet{T}"/>, <see cref="LockingCollection{T}"/>, or when one would use
    /// a <see cref="ConcurrentDictionary{TKey,TValue}"/> with ignored values.
    /// </summary>
    /// <typeparam name="T">Type of the items stored in the <see cref="ThreadSafeHashSet{T}"/>.</typeparam>
    /// <remarks>
    /// <note type="tip">If you want to wrap any <see cref="ICollection{T}"/> into a thread-safe wrapper without copying the actual items, then you can also use <see cref="LockingCollection{T}"/>.</note>
    /// <para><see cref="ThreadSafeHashSet{T}"/> uses a very similar approach to <see cref="ThreadSafeDictionary{TKey,TValue}"/>. It uses two separate
    /// internal storage: new items are added to a temporary storage using a single lock, which might regularly be merged into a faster lock-free storage,
    /// depending on the value of the <see cref="MergeInterval"/> property. Once the items are merged, their access
    /// (both read and write) becomes lock free. Even deleting and re-adding an item becomes faster after it has been merged into the lock-free storage.
    /// <note>Therefore, <see cref="ThreadSafeHashSet{T}"/> is not always a good alternative of <see cref="LockingCollection{T}"/>,
    /// or even a <see cref="ConcurrentDictionary{TKey,TValue}"/> with ignored values. If new items are continuously added, then always a shared lock is used,
    /// in which case <see cref="ConcurrentDictionary{TKey,TValue}"/> might perform better, unless you need to use
    /// some members, which are very slow in <see cref="ConcurrentDictionary{TKey,TValue}"/> (see the performance comparison table at the <strong>Remarks</strong>
    /// section of the <see cref="ThreadSafeDictionary{TKey,TValue}"/> class). If the newly added elements are regularly removed, make sure
    /// the <see cref="PreserveMergedItems"/> property is <see langword="false"/>; otherwise, the already merged items are just marked deleted when removed from
    /// the <see cref="ThreadSafeHashSet{T}"/> or when you call the <see cref="Clear">Clear</see> method. To remove even the merged items you must call
    /// the <see cref="Reset">Reset</see> method, or to remove the deleted items only you can explicitly call the <see cref="TrimExcess">TrimExcess</see> method.</note></para>
    /// <h2>Comparison with other thread-safe collections.</h2>
    /// <para><strong>When to prefer</strong>&#160;<see cref="ThreadSafeHashSet{T}"/> over <see cref="ConcurrentDictionary{TKey,TValue}"/>:
    /// <list type="bullet">
    /// <item>If you would use only the keys, without any value.</item>
    /// <item>If it is known that a fixed number of items will be used, or <see cref="Contains">Contains</see> will be used much more often than <see cref="Add">Add</see>,
    /// in which case <see cref="ThreadSafeHashSet{T}"/> may become mainly lock-free.</item>
    /// <item>If the same set of items are deleted and re-added again and again. In this case consider to set the <see cref="PreserveMergedItems"/>
    /// to <see langword="true"/>, so it is not checked whether a cleanup should be performed due to many deleted items.</item>
    /// <item>If it is needed to access <see cref="Count"/>, enumerate the items or you need to call <see cref="ToArray">ToArray</see>,
    /// which are particularly slow in case of <see cref="ConcurrentDictionary{TKey,TValue}"/>.</item>
    /// <item>If it is expected that there will be many hash collisions.</item>
    /// <item>If the collection is needed to be serialized.</item>
    /// </list>
    /// <note type="tip">If <typeparamref name="T"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
    /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the constructor for even better performance.
    /// Or, you can use <see cref="StringSegmentComparer.OrdinalRandomized">StringSegmentComparer.OrdinalRandomized</see> to use a comparer with randomized hash also on
    /// platforms where default string hashing is not randomized (eg. .NET Framework 3.5).</note></para>
    /// <para><strong>When to prefer</strong>&#160;<see cref="LockingCollection{T}"/> over <see cref="ThreadSafeHashSet{T}"/>:
    /// <list type="bullet">
    /// <item>If you just need a wrapper for an already existing <see cref="ICollection{T}"/> without copying the actual items.</item>
    /// <item>If you just need a simple thread-safe solution without additional allocations (unless if you enumerate the collection)
    /// and it's not a problem if it cannot scale well for high concurrency.</item>
    /// </list></para>
    /// <para><strong>Incompatibilities</strong> with <see cref="HashSet{T}"/>:
    /// <list type="bullet">
    /// <item>Some of the constructors have different parameters.</item>
    /// <item><see cref="ThreadSafeHashSet{T}"/> does not implement the <see cref="ISet{T}"/> interface because most of its members
    /// make little sense when the instance is used concurrently.</item>
    /// <item>It has no public <c>CopyTo</c> methods. The <see cref="ICollection{T}.CopyTo">ICollection&lt;T>.CopyTo</see> method
    /// is implemented explicitly, though it is not recommended to use it because when elements are added or removed during the operation
    /// you cannot tell how many elements were actually copied. Use the <see cref="ToArray">ToArray</see> method instead, which works in all circumstances.</item>
    /// </list></para>
    /// </remarks>
    /// <threadsafety instance="true"/>
    /// <seealso cref="LockingCollection{T}"/>
    /// <seealso cref="ThreadSafeDictionary{TKey,TValue}"/>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; T = {typeof(" + nameof(T) + ").Name}")]
    [Serializable]
    public partial class ThreadSafeHashSet<T> : ICollection<T>, ICollection, ISerializable, IDeserializationCallback
#if !(NET35 || NET40)
        , IReadOnlyCollection<T>
#endif

    {
        #region Enumerator class

        private sealed class Enumerator : IEnumerator<T>
        {
            #region Enumerations

            private enum State
            {
                NotStarted,
                Enumerating,
                Finished
            }

            #endregion

            #region Fields

            private readonly ThreadSafeHashSet<T> owner;

            private State state;
            private FixedSizeStorage.InternalEnumerator wrappedEnumerator;

            #endregion

            #region Properties

            #region Public Properties

            public T Current => wrappedEnumerator.Current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    // the non-generic IEnumerable.Current should throw an exception when used improperly
                    if (state != State.Enumerating)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return Current!;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(ThreadSafeHashSet<T> owner) => this.owner = owner;

            #endregion

            #region Methods

            public bool MoveNext()
            {
                switch (state)
                {
                    case State.NotStarted:
                        owner.EnsureMerged();
                        wrappedEnumerator = owner.fixedSizeStorage.GetInternalEnumerator();
                        state = State.Enumerating;
                        goto case State.Enumerating;

                    case State.Enumerating:
                        if (wrappedEnumerator.MoveNext())
                            return true;

                        wrappedEnumerator = default;
                        state = State.Finished;
                        return false;

                    default:
                        return false;
                }
            }

            public void Reset()
            {
                wrappedEnumerator = default;
                state = State.NotStarted;
            }

            public void Dispose()
            {
            }

            #endregion
        }

        #endregion

        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        private readonly object syncRoot = new object();

        private IEqualityComparer<T>? comparer;
        private int initialLockingCapacity;
        private bool bitwiseAndHash;
        private volatile bool preserveMergedItems;
        private volatile bool isMerging;
        private long mergeInterval = TimeHelper.GetInterval(100);
        private long nextMerge;

        /// <summary>
        /// Items here are accessed without locking.
        /// Once a new item is added, it is never removed anymore even when they are deleted
        /// </summary>
        private volatile FixedSizeStorage fixedSizeStorage;
        
        /// <summary>
        /// A temporary storage for new values. It is regularly merged with fixedSizeStorage into a new fixed size instance.
        /// </summary>
        private volatile TempStorage? expandableStorage;
        
        private SerializationInfo? deserializationInfo;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the number of elements contained in this <see cref="ThreadSafeHashSet{T}"/>.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if (expandableStorage == null)
                    return fixedSizeStorage.Count;

                lock (syncRoot)
                {
                    // lost race
                    if (expandableStorage == null)
                        return fixedSizeStorage.Count;

                    MergeIfExpired();
                    return (expandableStorage?.Count ?? 0) + fixedSizeStorage.Count;
                }
            }
        }

        /// <summary>
        /// Gets whether this <see cref="ThreadSafeHashSet{T}"/> is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                // Note: here we access even expandableStorage without locking. Though expandableStorage.Count returns an
                // expression where both operands are mutable we can accept it because we don't expect multiple changes at
                // once (changes are in locks) and expandableStorage is volatile so Count will see valid values.
                return fixedSizeStorage.Count == 0 && (expandableStorage?.Count ?? 0) == 0;
            }
        }

        /// <summary>
        /// Gets or sets whether items that have already been merged into the faster lock-free storage are preserved even when they are deleted.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>If the possible number of items in this <see cref="ThreadSafeHashSet{T}"/> is known to be a limited value, then this property
        /// can be set to <see langword="true"/>, so once the items have been merged into the faster lock-free storage, their entry is not removed anymore even if they
        /// are deleted. This ensures that removing and re-adding an item again and again remains a lock-free operation.
        /// <note>Do not set this property to <see langword="true"/>, if the number of the possibly added items is not limited.</note></para>
        /// <para>This property can be set to <see langword="true"/> even if items are never removed so it is not checked before a merge operation whether
        /// the amount of deleted items exceeds a specific limit.</para>
        /// <para>If this property is <see langword="true"/>, then the already merged items are not removed even when calling the <see cref="Clear">Clear</see> method.
        /// The memory of the deleted entries can be freed by explicitly calling the <see cref="TrimExcess">TrimExcess</see> method,
        /// whereas to remove all allocated entries you can call the <see cref="Reset">Reset</see> method.</para>
        /// <para>Even if this property is <see langword="false"/>, the removed items are not dropped immediately. Unused items are removed during a merge operation and only
        /// when their number exceeds a specific limit. You can call the <see cref="TrimExcess">TrimExcess</see> method to force removing unused items on demand.</para>
        /// </remarks>
        public bool PreserveMergedItems
        {
            get => preserveMergedItems;
            set => preserveMergedItems = value;
        }

        /// <summary>
        /// Gets or sets the minimum lifetime for the temporarily created internal locking storage when adding new items to the <see cref="ThreadSafeHashSet{T}"/>.
        /// <br/>Default value: 100 milliseconds.
        /// </summary>
        /// <remarks>
        /// <para>When adding new items, they will be put in a temporary locking storage first.
        /// Whenever the locking storage is accessed, it will be checked whether the specified time interval has been expired since its creation. If so, then
        /// it will be merged with the previous content of the fast non-locking storage into a new one.
        /// If new items are typically added together, rarely or periodically, then it is recommended to set some small positive value (up to a few seconds).</para>
        /// <para>Even if the value of this property is <see cref="TimeSpan.Zero">TimeSpan.Zero</see>, adding new items are not necessarily merged immediately
        /// to the fast-accessing storage. Depending on the targeted platform a minimum 15 ms delay is possible. Setting <see cref="TimeSpan.Zero">TimeSpan.Zero</see>
        /// is not recommended though, unless new items are almost never added at the same time.</para>
        /// <para>When the value of this property is negative (eg. <see cref="Timeout.InfiniteTimeSpan">Timeout.InfiniteTimeSpan</see>), then the locking storage is not merged
        /// automatically with the lock-free storage. You still can call the <see cref="EnsureMerged">EnsureMerged</see> method to perform a merge explicitly.</para>
        /// <para>This property is ignored if an item is accessed in the fast-accessing storage including removing and adding items that have already been merged to the lock-free storage.</para>
        /// <note>Some operations (such as enumerating the <see cref="ThreadSafeHashSet{T}"/>, calling the <see cref="ToArray">ToArray</see>
        /// or the <see cref="ICollection.CopyTo">ICollection.CopyTo</see> implementations) as well as serialization may trigger a merging operation
        /// regardless the value of this property.</note>
        /// </remarks>
        public TimeSpan MergeInterval
        {
            get => TimeHelper.GetTimeSpan(Volatile.Read(ref mergeInterval));
            set
            {
                lock (syncRoot)
                {
                    long interval = TimeHelper.GetInterval(value);

                    // adjusting to prevent immediate merging for nonzero value
                    if (interval == 0L && value != TimeSpan.Zero)
                        interval = value < TimeSpan.Zero ? -1L : 1L;

                    if (interval == mergeInterval)
                        return;

                    mergeInterval = interval;

                    if (expandableStorage == null || interval < 0L)
                        return;

                    nextMerge = TimeHelper.GetTimeStamp() + interval;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IEqualityComparer{T}"/> that is used to determine equality of items for this <see cref="ThreadSafeHashSet{T}"/>.
        /// </summary>
#if NET5_0_OR_GREATER
        public IEqualityComparer<T> Comparer => typeof(T).IsValueType
            ? comparer ?? ComparerHelper<T>.EqualityComparer
            : comparer!;
#else
        public IEqualityComparer<T> Comparer => comparer!;
#endif

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => Throw.NotSupportedException<object>();

        #endregion

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeHashSet{T}"/> class that is empty
        /// and uses the default comparer and <see cref="HashingStrategy.Auto"/> hashing strategy.
        /// </summary>
        public ThreadSafeHashSet() : this(defaultCapacity, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeHashSet{T}"/> class that is empty
        /// and uses the default comparer and the specified hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="capacity">Specifies the initial minimum capacity of the internal temporal storage for new items.
        /// If 0, then a default capacity is used.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeHashSet{T}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        public ThreadSafeHashSet(int capacity, HashingStrategy strategy = HashingStrategy.Auto)
            : this(capacity, null, strategy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeHashSet{T}"/> class that is empty
        /// and uses the specified <paramref name="comparer"/> and hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing items. If <see langword="null"/>, then <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/> item types when targeting the .NET Framework, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> in other cases. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeHashSet{T}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        /// <remarks>
        /// <note type="tip">If <typeparamref name="T"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
        /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the <paramref name="comparer"/> parameter for better performance.</note>
        /// </remarks>
        public ThreadSafeHashSet(IEqualityComparer<T>? comparer, HashingStrategy strategy = HashingStrategy.Auto)
            : this(defaultCapacity, comparer, strategy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeHashSet{T}"/> class that is empty
        /// and uses the specified <paramref name="comparer"/> and hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="capacity">Specifies the initial minimum capacity of the internal temporal storage for new items.
        /// If 0, then a default capacity is used.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing items. If <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/> item types when targeting the .NET Framework, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> in other cases. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeHashSet{T}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        /// <remarks>
        /// <note type="tip">If <typeparamref name="T"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
        /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the <paramref name="comparer"/> parameter for better performance.</note>
        /// </remarks>
        public ThreadSafeHashSet(int capacity, IEqualityComparer<T>? comparer, HashingStrategy strategy = HashingStrategy.Auto)
        {
            if (!strategy.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);
            if (capacity <= 0)
            {
                if (capacity < 0)
                    Throw.ArgumentOutOfRangeException(Argument.capacity, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                capacity = defaultCapacity;
            }

            fixedSizeStorage = FixedSizeStorage.Empty;
            initialLockingCapacity = capacity;
            this.comparer = ComparerHelper<T>.GetEqualityComparer(comparer);
            bitwiseAndHash = strategy.PreferBitwiseAndHash(this.comparer);
            Debug.Assert(this.comparer != null || typeof(T).IsValueType && ComparerHelper<T>.IsDefaultComparer(comparer));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeHashSet{T}"/> class from the specified <paramref name="collection"/>
        /// and uses the specified <paramref name="comparer"/> and hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements are coped to the new <see cref="ThreadSafeHashSet{T}"/>.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing items. If <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/> item types when targeting the .NET Framework, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> in other cases. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeHashSet{T}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        /// <remarks>
        /// <note type="tip">If <typeparamref name="T"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
        /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the <paramref name="comparer"/> parameter for better performance.</note>
        /// </remarks>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration", Justification = "TryInitialize will not enumerate when multiple enumeration could be a problem")]
        public ThreadSafeHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer = null, HashingStrategy strategy = HashingStrategy.Auto)
            : this(defaultCapacity, comparer, strategy)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            // trying to initialize directly in the fixed storage
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile - no problem, this is the constructor so there are no other threads at this point
            if (FixedSizeStorage.TryInitialize(collection, bitwiseAndHash, this.comparer, out fixedSizeStorage))
                return;
#pragma warning restore CS0420

            // initializing in the expando storage (no locking is needed here as we are in the constructor)
            expandableStorage = new TempStorage(collection, this.comparer, bitwiseAndHash);
            nextMerge = TimeHelper.GetTimeStamp() + mergeInterval;
        }

        #endregion

        #region Protected Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeHashSet{T}"/> class from serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that stores the data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this deserialization.</param>
        /// <remarks><note type="inherit">If an inherited type serializes data, which may affect the hashes of the keys, then override
        /// the <see cref="OnDeserialization">OnDeserialization</see> method and use that to restore the data of the derived instance.</note></remarks>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. - the fields will be initialized in IDeserializationCallback.OnDeserialization
        protected ThreadSafeHashSet(SerializationInfo info, StreamingContext context)
#pragma warning restore CS8618
        {
            // deferring the actual deserialization until all objects are finalized and hashes do not change anymore
            deserializationInfo = info;
        }

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Tries to add the specified <paramref name="item"/> to the <see cref="ThreadSafeHashSet{T}"/>.
        /// </summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> was added to the <see cref="ThreadSafeHashSet{T}"/>;
        /// <see langword="false"/> if the element is already present judged by the <see cref="Comparer"/>.</returns>
        /// <remarks>
        /// <para>This method is functionally the same as the <see cref="Add">Add</see> method.
        /// It is added to prevent mistakenly calling the <see cref="EnumerableExtensions.TryAdd{T}">EnumerableExtensions.TryAdd</see> extension method,
        /// which would end up in a somewhat worse performance.</para>
        /// </remarks>
        public bool TryAdd(T item)
        {
            uint hashCode = GetHashCode(item);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryAddInternal(item, hashCode);
                if (success == true)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;
                    continue;
                }

                // Duplicate item. No up-to-date check is needed here because in this case there was no change.
                if (success == false)
                    return false;

                lock (syncRoot)
                {
                    // lost race: must check the lock free values again to prevent possible duplicate items
                    if (!IsUpToDateInLock(lockFreeValues))
                        continue;

                    TempStorage lockingValues = GetCreateLockingStorage();
                    bool result = lockingValues.AddInternal(item, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Adds the specified <paramref name="item"/> to the <see cref="ThreadSafeHashSet{T}"/>.
        /// </summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> was added to the <see cref="ThreadSafeHashSet{T}"/>;
        /// <see langword="false"/> if the element is already present judged by the <see cref="Comparer"/>.</returns>
        public bool Add(T item) => TryAdd(item);

        /// <summary>
        /// Tries to get the actual stored item for the specified <paramref name="equalValue"/> in the <see cref="ThreadSafeHashSet{T}"/>.
        /// It can be useful to obtain the actually stored reference when the <see cref="Comparer"/> can consider different instances equal.
        /// </summary>
        /// <returns><see langword="true"/> if an item equal to <paramref name="equalValue"/> was found in the <see cref="ThreadSafeHashSet{T}"/>; otherwise, <see langword="false"/>.</returns>
        /// <param name="equalValue">The item to search for.</param>
        /// <param name="actualValue">When this method returns, the actually stored value, if the <paramref name="equalValue"/> is present in
        /// the <see cref="ThreadSafeHashSet{T}"/> judged by the current <see cref="Comparer"/>; otherwise, the default value for the type <typeparamref name="T"/>. This parameter is passed uninitialized.</param>
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)]out T actualValue)
        {
            uint hashCode = GetHashCode(equalValue);

            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryGetValueInternal(equalValue, hashCode, out actualValue);

                // We don't need to make sure we are up-to-date here because we don't modify anything
                if (success.HasValue)
                    return success.Value;

                // Not found: we need to check the locking storage. We try to do it without locking first.
                if (expandableStorage == null)
                {
                    if (IsUpToDate(lockFreeValues))
                        return false;
                    continue;
                }

                lock (syncRoot)
                {
                    // lost race: we need to check the fixed storage again
                    if (!IsUpToDateInLock(lockFreeValues))
                        continue;

                    Debug.Assert(expandableStorage != null, "If we are up-to-date the null check before the lock must be still valid");
                    bool result = expandableStorage!.TryGetValueInternal(equalValue, hashCode, out actualValue);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Determines whether the <see cref="ThreadSafeHashSet{T}"/> contains the specified <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The element to locate in the <see cref="ThreadSafeHashSet{T}"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="ThreadSafeHashSet{T}"/> contains the specified <paramref name="item"/>; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T item)
        {
            uint hashCode = GetHashCode(item);

            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.ContainsInternal(item, hashCode);

                // We don't need to make sure we are up-to-date here because we don't modify anything
                if (success.HasValue)
                    return success.Value;

                // Not found: we need to check the locking storage. We try to do it without locking first.
                if (expandableStorage == null)
                {
                    if (IsUpToDate(lockFreeValues))
                        return false;
                    continue;
                }

                lock (syncRoot)
                {
                    // lost race: we need to check the fixed storage again
                    if (!IsUpToDateInLock(lockFreeValues))
                        continue;

                    Debug.Assert(expandableStorage != null, "If we are up-to-date the null check before the lock must be still valid");
                    bool result = expandableStorage!.ContainsInternal(item, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Tries to remove the specified <paramref name="item"/> from the <see cref="ThreadSafeHashSet{T}"/>.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns><see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method is functionally the same as the <see cref="Remove">Remove</see> method.
        /// The only difference is that this method is not an interface implementation
        /// so using this one will not end up in a virtual method call, which provides a slightly better performance.</para>
        /// </remarks>
        public bool TryRemove(T item)
        {
            uint hashCode = GetHashCode(item);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryRemoveInternal(item, hashCode);

                // found as already deleted: quick return with no change like in Contains
                if (success == false)
                    return false;

                // just removed from fixed-size storage, or not found (success == null) and there is no locking storage
                if (success == true || expandableStorage == null)
                {
                    if (IsUpToDate(lockFreeValues))
                        return success.GetValueOrDefault();

                    continue;
                }

                lock (syncRoot)
                {
                    // lost race
                    if (!IsUpToDateInLock(lockFreeValues))
                        continue;

                    Debug.Assert(expandableStorage != null, "If we are up-to-date the null check before the lock must be still valid");
                    bool result = expandableStorage!.TryRemoveInternal(item, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Tries to remove the specified <paramref name="item"/> from the <see cref="ThreadSafeHashSet{T}"/>.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns><see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>.</returns>
        public bool Remove(T item) => TryRemove(item);

        /// <summary>
        /// Removes all items from the <see cref="ThreadSafeHashSet{T}"/>.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="PreserveMergedItems"/> is <see langword="true"/>, or when the amount of removed items does not exceed a limit, then
        /// This method is an O(n) operation where n is the number of elements present in the inner lock-free storage. Otherwise,
        /// this method calls the <see cref="Reset">Reset</see> method, which frees up all the allocated entries.</para>
        /// <note>Note that if <see cref="PreserveMergedItems"/> is <see langword="true"/>, then though this method marks all items deleted in
        /// the <see cref="ThreadSafeHashSet{T}"/>, it never actually removes the items that are already merged into the faster lock-free storage.
        /// This ensures that re-adding a previously removed item will always be a fast, lock-free operation.
        /// To actually remove all items use the <see cref="Reset">Reset</see> method instead.</note>
        /// </remarks>
        /// <seealso cref="Reset"/>
        public void Clear()
        {
            if (!preserveMergedItems && fixedSizeStorage.Capacity > 16)
            {
                Reset();
                return;
            }

            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                lockFreeValues.Clear();
                if (!IsUpToDate(lockFreeValues))
                    continue;

                // It is not a problem if a merge has been started before this line because it will nullify expandableStorage in the end anyway.
                expandableStorage = null;
                return;
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="ThreadSafeHashSet{T}"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Clear">Clear</see> method for details.
        /// </summary>
        /// <seealso cref="Clear"/>
        public void Reset()
        {
            // We can't use a loop with IsUpToDate here because we replace fixedSizeStorage reference
            // but the lock waits also for possible concurrent merges to finish
            lock (syncRoot)
            {
                fixedSizeStorage = FixedSizeStorage.Empty;
                expandableStorage = null;
            }
        }

        /// <summary>
        /// Ensures that all elements in this <see cref="ThreadSafeHashSet{T}"/> are merged into the faster lock-free storage.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="MergeInterval"/> property for details.
        /// </summary>
        public void EnsureMerged()
        {
            if (expandableStorage == null)
                return;

            lock (syncRoot)
            {
                TempStorage? lockingValues = expandableStorage;

                // lost race
                if (lockingValues == null)
                    return;

                DoMerge(!preserveMergedItems && fixedSizeStorage.IsCleanupLimitReached);
            }
        }

        /// <summary>
        /// Forces to perform a merge while removing all possibly allocated but already deleted entries from the lock-free storage,
        /// even if the <see cref="PreserveMergedItems"/> property is <see langword="true"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="PreserveMergedItems"/> property for details.
        /// </summary>
        public void TrimExcess()
        {
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                TempStorage? lockingValues = expandableStorage;

                if (lockingValues == null && lockFreeValues.DeletedCount == 0)
                {
                    // this is just needed for awaiting a possible concurrent merge session
                    if (IsUpToDate(lockFreeValues))
                        return;
                    continue;
                }

                lock (syncRoot)
                {
                    // lost race
                    if (!IsUpToDateInLock(lockFreeValues))
                        continue;

                    if (expandableStorage?.Count > 0)
                    {
                        DoMerge(true);
                        return;
                    }

                    if (lockFreeValues.DeletedCount > 0)
                    {
                        // special merge: just removing deleted entries from fixedSizeStorage
                        // As we set isMerging it is guaranteed that concurrent updates will wait in IsUpToDate after up to one update attempt
                        isMerging = true;
                        try
                        {
                            fixedSizeStorage = new FixedSizeStorage(lockFreeValues);
                        }
                        finally
                        {
                            isMerging = false;
                        }
                    }

                    expandableStorage = null;
                    return;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the items of this <see cref="ThreadSafeHashSet{T}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the <see cref="ThreadSafeHashSet{T}"/>.
        /// </returns>
        /// <remarks>
        /// <para>The returned enumerator is safe to use concurrently with reads and writes to the <see cref="ThreadSafeHashSet{T}"/>; however,
        /// it does not represent a moment-in-time snapshot. The contents exposed through the enumerator may contain modifications made to the <see cref="ThreadSafeHashSet{T}"/>
        /// after <see cref="GetEnumerator">GetEnumerator</see> was called.</para>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Copies the items stored in the <see cref="ThreadSafeHashSet{T}"/> to a new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of items copied from the <see cref="ThreadSafeHashSet{T}"/>.</returns>
        public T[] ToArray()
        {
            EnsureMerged();
            return fixedSizeStorage.ToArray();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// In a derived class populates a <see cref="SerializationInfo" /> with the additional data of the derived type needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization.</param>
        [SecurityCritical]
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }

        /// <summary>
        /// In a derived class restores the state the deserialized instance.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that stores the data.</param>
        [SecurityCritical]
        protected virtual void OnDeserialization(SerializationInfo info) { }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private uint GetHashCode(T item)
        {
            if (item == null)
                return 0U;
#if NET5_0_OR_GREATER
            if (typeof(T).IsValueType)
            {
                IEqualityComparer<T>? comp = comparer;
                return (uint)(comp == null ? item.GetHashCode() : comp.GetHashCode(item));
            }

            return (uint)comparer!.GetHashCode(item);
#else
            return (uint)comparer!.GetHashCode(item);
#endif
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private TempStorage GetCreateLockingStorage()
        {
            TempStorage? result = expandableStorage;
            if (result != null)
                return result;
            result = expandableStorage = new TempStorage(initialLockingCapacity, comparer, bitwiseAndHash);
            long interval = mergeInterval;
            if (interval >= 0L)
                nextMerge = TimeHelper.GetTimeStamp() + interval;
            return result;
        }

        private void MergeIfExpired()
        {
            // must be called in lock
            if (mergeInterval >= 0L && TimeHelper.GetTimeStamp() > nextMerge)
                DoMerge(!preserveMergedItems && fixedSizeStorage.IsCleanupLimitReached);
        }

        private void DoMerge(bool removeDeletedItems)
        {
            // Must be in a lock to work properly!
            Debug.Assert(!isMerging || expandableStorage != null, "Make sure caller is in a lock");

            TempStorage lockingValues = expandableStorage!;
            if (lockingValues.Count != 0 || removeDeletedItems)
            {
                // Indicating that from this point fixedSizeStorage cannot be considered safe even though its reference is not replaced yet.
                // Note: we could spare the flag if we just nullified fixedSizeStorage before merging but this way can prevent that items in the
                // fixed-size storage reappear in the locking one.
                isMerging = true;
                try
                {
                    fixedSizeStorage = new FixedSizeStorage(fixedSizeStorage, lockingValues, removeDeletedItems);
                }
                finally
                {
                    // now we can reset the flag because an outdated storage reference can be detected safely
                    isMerging = false;
                }
            }

            expandableStorage = null;
        }

        /// <summary>
        /// Checks if the lock free storage is still up-to-date. If returns false, the <see cref="fixedSizeStorage"/> field must be re-checked.
        /// Should be used outside of a lock. If a merge operation is in progress, it blocks the current thread without locking until the merge is finished.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private bool IsUpToDate(FixedSizeStorage lockFreeValues)
        {
            // fixed-size storage has been replaced
            if (lockFreeValues != fixedSizeStorage)
                return false;

            if (!isMerging)
                return true;

            // A merge has been started, values from lockFreeValues storage might be started to be copied:
            // preventing the current thread from checking the lock free storage again and again until the operation is finished.
            WaitWhileMerging();
            return false;
        }

        /// <summary>
        /// Checks if the lock free storage is still up-to-date. Should be used inside of a lock.
        /// If returns false, the lock block must be left immediately and the <see cref="fixedSizeStorage"/> field must be re-checked.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private bool IsUpToDateInLock(FixedSizeStorage lockFreeValues)
        {
            Debug.Assert(!isMerging);
            return lockFreeValues == fixedSizeStorage;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private void WaitWhileMerging()
        {
            var wait = new TimedSpinWait();
            while (isMerging)
                wait.SpinOnce();
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void ICollection<T>.Add(T item) => Add(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
            int length = array.Length;
            if (length - arrayIndex < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

            EnsureMerged();
            FixedSizeStorage.InternalEnumerator enumerator = fixedSizeStorage.GetInternalEnumerator();
            while (enumerator.MoveNext())
            {
                // if elements were added concurrently
                if (arrayIndex == length)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                array[arrayIndex] = enumerator.Current;
                arrayIndex += 1;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (index < 0 || index > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            int length = array.Length;
            if (length - index < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (array.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            switch (array)
            {
                case T[] keyValuePairs:
                    ((ICollection<T>)this).CopyTo(keyValuePairs, index);
                    return;

                case object?[] objectArray:
                    EnsureMerged();
                    var enumerator = fixedSizeStorage.GetInternalEnumerator();
                    while (enumerator.MoveNext())
                    {
                        // if elements were added concurrently
                        if (index == length)
                            Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                        objectArray[index] = enumerator.Current;
                        index += 1;
                    }

                    return;

                default:
                    Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
                    return;
            }
        }

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);

            info.AddValue(nameof(initialLockingCapacity), initialLockingCapacity);
            info.AddValue(nameof(bitwiseAndHash), bitwiseAndHash);
            info.AddValue(nameof(comparer), ComparerHelper<T>.IsDefaultComparer(comparer) ? null : comparer, typeof(IEqualityComparer<T>));
            info.AddValue(nameof(mergeInterval), mergeInterval);
            info.AddValue(nameof(preserveMergedItems), preserveMergedItems);
            info.AddValue("items", ToArray());

            // custom data of a derived class
            GetObjectData(info, context);
        }

        [SecuritySafeCritical]
        void IDeserializationCallback.OnDeserialization(object? sender)
        {
            SerializationInfo? info = deserializationInfo;

            // may occur with remoting, which calls OnDeserialization twice.
            if (info == null)
                return;

            initialLockingCapacity = info.GetInt32(nameof(initialLockingCapacity));
            bitwiseAndHash = info.GetBoolean(nameof(bitwiseAndHash));
            comparer = ComparerHelper<T>.GetEqualityComparer((IEqualityComparer<T>?)info.GetValue(nameof(comparer), typeof(IEqualityComparer<T>)));
            mergeInterval = info.GetInt64(nameof(mergeInterval));
            preserveMergedItems = info.GetValueOrDefault<bool>(nameof(preserveMergedItems)); // GetValueOrDefault for compatibility reasons because was added lately
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile - no problem, deserialization is a single threaded-access
            FixedSizeStorage.TryInitialize(info.GetValueOrDefault<T[]>("items"), bitwiseAndHash, comparer, out fixedSizeStorage);
#pragma warning restore CS0420

            OnDeserialization(info);

            deserializationInfo = null;
        }

        #endregion

        #endregion
    }
}
