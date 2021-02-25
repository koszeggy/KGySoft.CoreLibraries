#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeDictionary.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
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
#if !NET35
using System.Collections.Concurrent; 
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Implements a thread-safe dictionary, which can be a good alternative for <see cref="ConcurrentDictionary{TKey,TValue}"/> where it is not available (.NET 3.5),
    /// or where <see cref="ConcurrentDictionary{TKey,TValue}"/> has a poorer performance.
    /// <br/>See the <strong>Remarks</strong> section for details and for a comparison between <see cref="ThreadSafeDictionary{TKey,TValue}"/> and <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <para>TODO</para>
    /// TODO:
    /// - TSD vs CD (vs LD):
    ///   - Fixed size lock-free storage + a temporary locking storage vs. Uses separate locks for updates, and for some operations acquires all locks at the same time (eg. Count)
    ///   - Memory consumption
    ///     - When adding new values
    ///     - When merged (note for reference-containing TKey: deleted keys are not removed)
    ///     - When TValue is not atomic (but this behavior is the same as for CD and it is mentioned at when to use, so maybe not needed here)
    ///   - When to use TSD
    ///     - Rare new items, mostly overwrite + get, maybe delete + re-add the same key
    ///     - Count
    ///     - Many possible collisions
    ///     - Large struct values (non-atomic size)
    ///   - Incompatibility:
    ///     - Keys and Values property is not a snapshot but a living wrapper
    ///     - ICollection.SyncRoot property throws a NotSupportedException even for Keys and Values
    /// - Member by member performance comparison in .NET 5
    ///   - TryGetValue
    ///   - Setter
    ///   - Count
    ///   - GetEnumerator + enumeration
    ///   - ...
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ").Name}; TValue = {typeof(" + nameof(TValue) + ").Name}")]
    public partial class ThreadSafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
        where TKey : notnull
    {
        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        #region Static Fields

        private static readonly IEqualityComparer<TKey> defaultComparer = ComparerHelper<TKey>.EqualityComparer;
        private static readonly IEqualityComparer<TValue> valueComparer = ComparerHelper<TValue>.EqualityComparer;

        #endregion

        #region Instance Fields

        private readonly object syncRoot = new object();
        private readonly IEqualityComparer<TKey>? comparer;
        private readonly int initialLockingCapacity;
        private readonly bool bitwiseAndHash;

        /// <summary>
        /// Values here are accessed without locking.
        /// Once a new key is added, it is never removed anymore even for deleted entries.
        /// </summary>
        private volatile FixedSizeStorage fixedSizeStorage;

        /// <summary>
        /// A temporary storage for new values. It is regularly merged with fixedSizeStorage into a new fixed size instance.
        /// </summary>
        private volatile TempStorage? expandableStorage;

        private volatile KeysCollection? keysCollection;
        private volatile ValuesCollection? valuesCollection;

        private long mergeInterval = TimeHelper.GetInterval(100);
        private long nextMerge;
        private volatile bool isMerging;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the number of elements contained in this <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
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
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return fixedSizeStorage.Count;

                    int result = fixedSizeStorage.Count + lockingValues.Count;
                    MergeIfExpired();
                    return result;
                }
            }
        }

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
        /// Gets or sets the minimum lifetime for the temporarily created internal locking storage when adding new keys to the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// <br/>Default value: 100 milliseconds.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <para>When the value of this property is <see cref="TimeSpan.Zero">TimeSpan.Zero</see>, then adding new items are immediately merged to a new fast-accessing storage.
        /// This is not recommended unless new items are almost never added at the same time.</para>
        /// <para>When the value of this property is positive (the default is 100 milliseconds), then adding new items will be put in a temporary locking storage.
        /// Whenever the locking storage is accessed, it will be checked whether the specified time interval has been expired since its creation. If so, then
        /// it will be merged with the previous content of the fast storage into a new instance. This is the recommended behavior
        /// if new keys are typically added together, rarely or periodically.</para>
        /// <para>When the value of this property is negative (eg. <see cref="Timeout.InfiniteTimeSpan">Timeout.InfiniteTimeSpan</see>), then the locking storage is never merged automatically
        /// with the lock-free storage. You still can call the <see cref="EnsureMerged">EnsureMerged</see> method to perform a merge explicitly.</para>
        /// <para>This property does not affect overwriting, removing or re-adding previously deleted keys that already present in the fast-accessing storage.</para>
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

                    if (interval > 0L)
                        nextMerge = TimeHelper.GetTimeStamp() + interval;
                    else
                        EnsureMerged();
                }
            }
        }

        /// <summary>
        /// Gets a collection reflecting the keys stored in this <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the keys in the original <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TKey> Keys => keysCollection ??= new KeysCollection(this);

        /// <summary>
        /// Gets a collection reflecting the values stored in this <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the values in the original <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TValue> Values => valuesCollection ??= new ValuesCollection(this);

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        ICollection IDictionary.Keys => new KeysCollection(this);
        ICollection IDictionary.Values => new ValuesCollection(this);
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => Throw.NotSupportedException<object>();

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        /// <summary>
        /// Gets or sets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value of the specified <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get => TryGetValue(key, out TValue? value) ? value : Throw.KeyNotFoundException<TValue>(Res.IDictionaryKeyNotFound);
            set
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.OverwriteIfExists);
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object? IDictionary.this[object key]
        {
            get => key switch
            {
                TKey k => TryGetValue(k, out TValue? value) ? value : null,
                null => Throw.ArgumentNullException<object>(Argument.key),
                _ => null
            };
            set
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                Throw.ThrowIfNullIsInvalid<TValue>(value);
                if (key is TKey k)
                {
                    if (value is TValue v)
                        this[k] = v;
                    else
                        Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeof(TValue)));
                }
                else
                    Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, typeof(TKey)));
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class that is empty
        /// and uses the default comparer and <see cref="HashingStrategy.Auto"/> hashing strategy.
        /// </summary>
        public ThreadSafeDictionary() : this(defaultCapacity, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class that is empty
        /// and uses the default comparer and the specified hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="capacity">Specifies the initial minimum capacity of the internal temporal storage for values with new keys.
        /// If 0, then a default capacity is used.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeDictionary{TKey, TValue}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        public ThreadSafeDictionary(int capacity, HashingStrategy strategy = HashingStrategy.Auto)
            : this(capacity, null, strategy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class that is empty
        /// and uses the specified <paramref name="comparer"/> and hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="capacity">Specifies the initial minimum capacity of the internal temporal storage for values with new keys.
        /// If 0, then a default capacity is used.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeDictionary{TKey, TValue}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        public ThreadSafeDictionary(int capacity, IEqualityComparer<TKey>? comparer, HashingStrategy strategy = HashingStrategy.Auto)
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
            bitwiseAndHash = strategy.PreferBitwiseAndHash(comparer);
            this.comparer = comparer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class from the specified <paramref name="collection"/>
        /// and uses the specified <paramref name="comparer"/> and hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements are coped to the new <see cref="ThreadSafeDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeDictionary{TKey, TValue}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        public ThreadSafeDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer = null, HashingStrategy strategy = HashingStrategy.Auto)
            : this(defaultCapacity, comparer, strategy)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            // trying to initialize directly in the fixed storage
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile - no problem, this is the constructor so there are no other threads at this point
            if (collection is ICollection<KeyValuePair<TKey, TValue>> c && FixedSizeStorage.TryInitialize(c, bitwiseAndHash, comparer, out fixedSizeStorage))
                return;
#pragma warning restore CS0420

            // initializing in the expando storage (no locking is needed here as we are in the constructor)
            fixedSizeStorage = FixedSizeStorage.Empty;
            expandableStorage = new TempStorage(collection, comparer, bitwiseAndHash);
            nextMerge = TimeHelper.GetTimeStamp() + mergeInterval;
        }

        #endregion

        #region Methods

        #region Public Methods

        public void Add(TKey key, TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.ThrowIfExists);
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.DoNotOverwrite);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            return TryGetValueInternal(key, hashCode, out value);
        }

        public bool ContainsKey(TKey key) => TryGetValue(key, out var _);

        public bool ContainsValue(TValue value)
        {
            FixedSizeStorage.InternalEnumerator lockFreeEnumerator = fixedSizeStorage.GetInternalEnumerator();
            while (lockFreeEnumerator.MoveNext())
            {
                if (valueComparer.Equals(value, lockFreeEnumerator.Current.Value))
                    return true;
            }

            if (expandableStorage == null)
                return false;

            lock (syncRoot)
            {
                TempStorage? lockingValues = expandableStorage;

                // lost race
                if (lockingValues == null)
                    return false;

                bool result = false;
                TempStorage.InternalEnumerator lockingEnumerator = lockingValues.GetInternalEnumerator();
                while (!result && lockingEnumerator.MoveNext())
                    result = valueComparer.Equals(value, lockingEnumerator.Current.Value);
                MergeIfExpired();
                return result;
            }
        }

        public bool TryUpdate(TKey key, TValue newValue, TValue originalValue)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            return TryReplaceInternal(key, hashCode, newValue, originalValue);
        }

        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (updateValueFactory == null!)
                Throw.ArgumentNullException(nameof(updateValueFactory));

            uint hashCode = GetHashCode(key);
            TValue result;
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                if (lockFreeValues.TryAddOrUpdate(key, addValue, updateValueFactory, hashCode, out result))
                {
                    if (IsUpToDate(lockFreeValues))
                        return result;
                    continue;
                }

                // TODO: if mergeInterval == Zero... - merge immediately (actually not really needed just for performance reasons)
                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    result = lockingValues.AddOrUpdate(key, addValue, updateValueFactory, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        public bool Remove(TKey key)
        {
            // Note: we could re-use TryRemove but it is faster to do it this way
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            bool removed = false;
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryRemoveInternal(key, hashCode);

                // making sure that correct result is returned even if multiple tries are necessary due to merging
                if (success == true)
                    removed = true;

                // successfully removed from fixed-size storage (now, or in a previous attempt)
                if (removed)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;

                    continue;
                }

                // item was already deleted (if we removed it in a previous attempt, then true is returned above)
                if (success == false)
                    return false;

                if (expandableStorage == null)
                    return false;

                lock (syncRoot)
                {
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return false;

                    bool result = lockingValues.TryRemoveInternal(key, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        public bool TryRemove(TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryRemoveInternal(key, hashCode, out value);

                // making sure that removed entry is returned even if multiple tries are necessary due to merging
                if (success == true)
                {
                    if (IsUpToDate(lockFreeValues))
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition. - false alarm, TryRemoveInternal returned true
                        return true;
#pragma warning restore CS8762

                    continue;
                }

                // item was already deleted
                if (success == false)
                    return false;

                if (expandableStorage == null)
                    return false;

                lock (syncRoot)
                {
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return false;

                    bool result = lockingValues.TryRemoveInternal(key, hashCode, out value);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        public void Clear()
        {
            // It is not a problem if a merge is in progress because it will nullify expandableStorage in the end anyway
            expandableStorage = null;

            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                lockFreeValues.Clear();
                if (IsUpToDate(lockFreeValues))
                    return;
            }
        }

        public void Reset()
        {
            if (isMerging)
                WaitWhileMerging();
            fixedSizeStorage = FixedSizeStorage.Empty;
            expandableStorage = null;
        }

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

                DoMerge();
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this, true);

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            EnsureMerged();
            return fixedSizeStorage.ToArray();
        }

        #endregion

        #region Private Methods

        private bool TryGetValueInternal(TKey key, uint hashCode, [MaybeNullWhen(false)]out TValue value)
        {
            bool? success = fixedSizeStorage.TryGetValueInternal(key, hashCode, out value);
            if (success.HasValue)
                return success.Value;

            if (expandableStorage == null)
                return false;

            lock (syncRoot)
            {
                TempStorage? lockingValues = expandableStorage;

                // lost race
                if (lockingValues == null)
                    return false;

                bool result = lockingValues.TryGetValueInternal(key, hashCode, out value);
                MergeIfExpired();
                return result;
            }
        }

        private bool TryInsertInternal(TKey key, TValue value, uint hashCode, DictionaryInsertion behavior)
        {
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryInsertInternal(key, value, hashCode, behavior);
                if (success == true)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;
                    continue;
                }

                // duplicate key
                if (success == false)
                    return false;

                // TODO: if mergeInterval == Zero... - merge immediately (actually not really needed just for performance reasons)
                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    bool result = lockingValues.TryInsertInternal(key, value, hashCode, behavior);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        private bool TryReplaceInternal(TKey key, uint hashCode, TValue newValue, TValue originalValue)
        {
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryReplaceInternal(key, newValue, originalValue, hashCode);
                if (success == true)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;
                    continue;
                }

                // already deleted or originalValue does not match
                if (success == false)
                    return false;

                if (expandableStorage == null)
                    return false;

                lock (syncRoot)
                {
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return false;

                    bool result = lockingValues.TryReplaceInternal(key, newValue, originalValue, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private uint GetHashCode(TKey key)
        {
            IEqualityComparer<TKey>? comp = comparer;
            return (uint)(comp == null ? key.GetHashCode() : comp.GetHashCode(key));
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private TempStorage GetCreateLockingStorage()
        {
            TempStorage? result = expandableStorage;
            if (result != null)
                return result;
            result = expandableStorage = new TempStorage(initialLockingCapacity, comparer, bitwiseAndHash);
            long interval = mergeInterval;
            if (interval > 0L)
                nextMerge = TimeHelper.GetTimeStamp() + interval;
            return result;
        }

        private void MergeIfExpired()
        {
            // must be called in lock
            if (mergeInterval == 0L || TimeHelper.GetTimeStamp() > nextMerge)
                DoMerge();
        }

        private void DoMerge()
        {
            // Must be in a lock to work properly!
            Debug.Assert(!isMerging || expandableStorage != null, "Make sure caller is in a lock");

            TempStorage lockingValues = expandableStorage!;
            if (lockingValues.Count != 0)
            {
                // Indicating that from this point fixedSizeStorage cannot be considered safe even though its reference is not replaced yet.
                // Note: we could spare the flag if we just nullified fixedSizeStorage before merging but this way can prevent that keys in the
                // fixed-size storage reappear in the locking one.
                isMerging = true;
                try
                {
                    fixedSizeStorage = new FixedSizeStorage(fixedSizeStorage, lockingValues);
                }
                finally
                {
                    // now we can reset the flag because an outdated storage reference can be detected safely
                    isMerging = false;
                }
            }

            expandableStorage = null;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private bool IsUpToDate(FixedSizeStorage lockFreeValues)
        {
            // fixed-size storage has been replaced
            if (lockFreeValues != fixedSizeStorage)
                return false;

            if (!isMerging)
                return true;

            // a merge has been started, values from lockFreeValues storage might be started to be copied:
            // preventing current thread from consuming CPU until merge is finished
            WaitWhileMerging();
            return false;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private void WaitWhileMerging()
        {
            var wait = new SpinWait();
            while (isMerging)
                wait.SpinOnce();
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => ((IDictionary<TKey, TValue>)this).Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => TryGetValue(item.Key, out TValue? value) && ComparerHelper<TValue>.EqualityComparer.Equals(value, item.Value);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
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
                array[arrayIndex] = new KeyValuePair<TKey, TValue>(enumerator.Current.Key, enumerator.Current.Value);
                arrayIndex += 1;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            Debug.Fail("It is not expected to call this method. A TryRemove(key, value) must be implemented if it will be a public class.");
            while (true)
            {
                if (!TryRemove(item.Key, out TValue? value))
                    return false;
                
                if (ComparerHelper<TValue>.EqualityComparer.Equals(value, item.Value))
                    return true;

                // oops, we removed the wrong value
                TryAdd(item.Key, value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, false);

        void IDictionary.Add(object key, object? value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            Throw.ThrowIfNullIsInvalid<TValue>(value);
            if (key is TKey k)
            {
                if (value is TValue v)
                    Add(k, v);
                else
                    Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeof(TValue)));
            }
            else
                Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, typeof(TKey)));
        }

        bool IDictionary.Contains(object key)
            => key switch
            {
                TKey k => ContainsKey(k),
                null => Throw.ArgumentNullException<bool>(Argument.key),
                _ => false
            };

        void IDictionary.Remove(object key)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (key is TKey k)
                Remove(k);
        }

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
                case KeyValuePair<TKey, TValue>[] keyValuePairs:
                    ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(keyValuePairs, index);
                    return;

                case DictionaryEntry[] dictionaryEntries:
                    EnsureMerged();
                    FixedSizeStorage.InternalEnumerator enumerator = fixedSizeStorage.GetInternalEnumerator();
                    while (enumerator.MoveNext())
                    {
                        // if elements were added concurrently
                        if (index == length)
                            Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                        dictionaryEntries[index] = new DictionaryEntry(enumerator.Current.Key, enumerator.Current.Value);
                        index += 1;
                    }

                    return;

                case object[] objectArray:
                    EnsureMerged();
                    enumerator = fixedSizeStorage.GetInternalEnumerator();
                    while (enumerator.MoveNext())
                    {
                        // if elements were added concurrently
                        if (index == length)
                            Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                        objectArray[index] = new KeyValuePair<TKey,TValue>(enumerator.Current.Key, enumerator.Current.Value);
                        index += 1;
                    }

                    return;

                default:
                    Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
                    return;
            }
        }

        #endregion

        #endregion
    }
}
