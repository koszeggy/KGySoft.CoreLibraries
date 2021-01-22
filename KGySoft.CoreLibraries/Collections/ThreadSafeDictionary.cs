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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// A thread-safe dictionary, which can be very fast when usually existing keys are read and written and set and new items are relatively rarely added.
    /// Similar to ConcurrentDictionary, which is not available in .NET 3.5.
    /// NOTE: Should not be used for TValues larger that cannot be written atomically.
    /// </summary>
    internal class ThreadSafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        private readonly object syncRoot = new object();
        private readonly IEqualityComparer<TKey>? comparer;
        private readonly int initialLockingCapacity;
        private readonly bool bitwiseAndHash;

        /// <summary>
        /// When reading, values here are accessed without locking.
        /// Once a new key is added, it is never removed anymore even for deleted entries.
        /// </summary>
        private volatile FixedSizeDictionary<TKey, TValue> lockFreeStorage;

        /// <summary>
        /// A temporary storage for new values. It is regularly merged with lockFreeStorage into a new fixed size instance.
        /// </summary>
        private volatile CustomDictionary<TKey, TValue>? lockingStorage;

        private TimeSpan mergeInterval = TimeSpan.FromMilliseconds(100);
        private DateTime nextMerge;
        private volatile bool isMerging;

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        public int Count
        {
            get
            {
                if (lockingStorage == null)
                    return lockFreeStorage.Count;

                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue>? lockingValues = lockingStorage;

                    // lost race
                    if (lockingValues == null)
                        return lockFreeStorage.Count;

                    int result = lockFreeStorage.Count + lockingValues.Count;
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum lifetime for the temporarily created internal locking storage when adding new keys to the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// <br/>Default value: 100 milliseconds.
        /// </summary>
        /// <remarks>
        /// <para>When the value of this property is <see cref="TimeSpan.Zero">TimeSpan.Zero</see>, then adding new items are immediately merged to a new fast-accessing storage.
        /// This is recommended only if new items are never added at the same time.</para>
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
            get
            {
                lock (syncRoot)
                    return mergeInterval;
            }
            set
            {
                lock (syncRoot)
                {
                    if (value == mergeInterval)
                        return;

                    mergeInterval = value;

                    if (lockingStorage == null || value < TimeSpan.Zero)
                        return;

                    if (value > TimeSpan.Zero)
                        nextMerge = DateTime.UtcNow + value;
                    else
                        EnsureMerged();
                }
            }
        }

        #endregion

        #region Internal Properties

        internal IEnumerable<TKey> Keys
        {
            get
            {
                foreach (KeyValuePair<TKey, TValue> item in this)
                    yield return item.Key;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        // TODO: If this class is made public, then these solutions should be optimized:
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys.ToList();
        ICollection<TValue> IDictionary<TKey, TValue>.Values => this.Select(item => item.Value).ToList();

        #endregion

        #endregion

        #region Indexers

        public TValue this[TKey key]
        {
            get => TryGetValue(key, out TValue value) ? value : Throw.KeyNotFoundException<TValue>(Res.IDictionaryKeyNotFound);
            set
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.OverwriteIfExists);
            }
        }

        #endregion

        #endregion

        #region Constructors

        internal ThreadSafeDictionary(int capacity, HashingStrategy strategy = HashingStrategy.Auto)
            : this(capacity, null, strategy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="capacity">Specifies the initial minimum capacity of the internal temporal storage for the newly added keys. If 0, then a default value is used.</param>
        /// <param name="comparer">TODO</param>
        /// <param name="strategy">TODO</param>
        internal ThreadSafeDictionary(int capacity, IEqualityComparer<TKey>? comparer = null, HashingStrategy strategy = HashingStrategy.Auto)
        {
            if (!strategy.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);
            if (capacity <= 0)
            {
                if (capacity < 0)
                    Throw.ArgumentOutOfRangeException(Argument.capacity, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                capacity = defaultCapacity;
            }

            lockFreeStorage = FixedSizeDictionary<TKey, TValue>.Empty;
            initialLockingCapacity = capacity;
            bitwiseAndHash = strategy.PreferBitwiseAndHash(comparer);
            this.comparer = comparer;
        }

        internal ThreadSafeDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer = null, HashingStrategy strategy = HashingStrategy.Auto)
            : this(defaultCapacity, comparer, strategy)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            lockFreeStorage = new FixedSizeDictionary<TKey, TValue>(bitwiseAndHash, dictionary, comparer);
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

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            return TryGetValueInternal(key, hashCode, out value);
        }

        public bool ContainsKey(TKey key) => TryGetValue(key, out var _);

        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.DoNotOverwrite);
        }

        public bool TryUpdate(TKey key, TValue newValue, TValue originalValue)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            return TryReplaceInternal(key, hashCode, newValue, originalValue);
        }

        public TValue AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue>? updateValueFactory = null)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            
            uint hashCode = GetHashCode(key);
            if (updateValueFactory == null)
            {
                TryInsertInternal(key, value, hashCode, DictionaryInsertion.OverwriteIfExists);
                return value;
            }

            while (true)
            {
                if (TryGetValueInternal(key, hashCode, out TValue oldValue))
                {
                    // Exists, trying to update
                    TValue newValue = updateValueFactory.Invoke(key, oldValue);
                    if (TryReplaceInternal(key, hashCode, newValue, oldValue))
                        return newValue;
                }
                else
                {
                    // Does not exist, try to add
                    if (TryInsertInternal(key, value, hashCode, DictionaryInsertion.DoNotOverwrite))
                        return value;
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
                FixedSizeDictionary<TKey, TValue> lockFreeValues = lockFreeStorage;
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

                if (lockingStorage == null)
                    return false;

                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue>? lockingValues = lockingStorage;

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
                FixedSizeDictionary<TKey, TValue> lockFreeValues = lockFreeStorage;
                bool? success = lockFreeValues.TryRemoveInternal(key, hashCode, out value);

                // making sure that removed entry is returned even if multiple tries are necessary due to merging
                if (success == true)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;

                    continue;
                }

                // item was already deleted
                if (success == false)
                    return false;

                if (lockingStorage == null)
                    return false;

                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue>? lockingValues = lockingStorage;

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
            // It is not a problem if a merge is in progress because it will nullify lockingStorage in the end anyway
            lockingStorage = null;

            while (true)
            {
                FixedSizeDictionary<TKey, TValue> lockFreeValues = lockFreeStorage;
                lockFreeValues.Clear();
                if (IsUpToDate(lockFreeValues))
                    return;
            }
        }

        public void EnsureMerged()
        {
            if (lockingStorage == null)
                return;

            lock (syncRoot)
            {
                CustomDictionary<TKey, TValue>? lockingValues = lockingStorage;

                // lost race
                if (lockingValues == null)
                    return;

                DoMerge();
            }
        }

        public FixedSizeDictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            EnsureMerged();
            return lockFreeStorage.GetEnumerator();
        }

        #endregion

        #region Private Methods

        private bool TryGetValueInternal(TKey key, uint hashCode, out TValue value)
        {
            bool? success = lockFreeStorage.TryGetValueInternal(key, hashCode, out value);
            if (success.HasValue)
                return success.Value;

            if (lockingStorage == null)
                return false;

            lock (syncRoot)
            {
                CustomDictionary<TKey, TValue>? lockingValues = lockingStorage;

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
                FixedSizeDictionary<TKey, TValue> lockFreeValues = lockFreeStorage;
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
                    CustomDictionary<TKey, TValue> lockingValues = GetCreatelockingStorage();
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
                FixedSizeDictionary<TKey, TValue> lockFreeValues = lockFreeStorage;
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

                if (lockingStorage == null)
                    return false;

                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue>? lockingValues = lockingStorage;

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

        private CustomDictionary<TKey, TValue> GetCreatelockingStorage()
        {
            CustomDictionary<TKey, TValue>? result = lockingStorage;
            if (result != null)
                return result;
            result = lockingStorage = new CustomDictionary<TKey, TValue>(bitwiseAndHash, initialLockingCapacity, comparer);
            nextMerge = DateTime.UtcNow + mergeInterval;
            return result;
        }

        private void MergeIfExpired()
        {
            // must be called in lock
            if (DateTime.UtcNow < nextMerge)
                return;
            DoMerge();
        }

        private void DoMerge()
        {
            // Must be in a lock to work properly!
            Debug.Assert(!isMerging || lockingStorage != null, "Make sure caller is in a lock");

            CustomDictionary<TKey, TValue> lockingValues = lockingStorage!;
            if (lockingValues.Count != 0)
            {
                // Indicating that from this point lockFreeStorage cannot be considered safe even though its reference is not replaced yet.
                // Note: we could spare the flag if we just nullified lockFreeStorage before merging but this way can prevent that keys in the
                // fixed-size storage reappear in the locking one.
                isMerging = true;
                try
                {
                    lockFreeStorage = new FixedSizeDictionary<TKey, TValue>(lockFreeStorage, lockingValues);
                }
                finally
                {
                    // now we can reset the flag because an outdated storage reference can be detected safely
                    isMerging = false;
                }
            }

            lockingStorage = null;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private bool IsUpToDate(FixedSizeDictionary<TKey, TValue> lockFreeValues)
        {
            // fixed-size storage has been replaced
            if (lockFreeValues != lockFreeStorage)
                return false;

            // a merge has been started, values from lockFreeValues storage might be started to copied: preventing current thread from consuming CPU until merge is finished
            if (!isMerging)
                return true;

#if !NET35
            var wait = new SpinWait();
#endif
            while (isMerging)
            {
#if NET35
                Thread.Sleep(1);
#elif NETFRAMEWORK
                wait.SpinOnce();
#else
                wait.SpinOnce(1);
#endif
            }

            return false;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (KeyValuePair<TKey, TValue> item in this)
                yield return item;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => ((IDictionary<TKey, TValue>)this).Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => TryGetValue(item.Key, out TValue value) && ComparerHelper<TValue>.EqualityComparer.Equals(value, item.Value);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Debug.Fail("It is not expected to call this method. Must be optimized if this will be a public class.");
            this.ToList().CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            Debug.Fail("It is not expected to call this method. A TryRemove(key, value) must be implemented if it will be a public class.");
            while (true)
            {
                if (!TryRemove(item.Key, out TValue value))
                    return false;
                
                if (ComparerHelper<TValue>.EqualityComparer.Equals(value, item.Value))
                    return true;

                // oops, we removed the wrong value
                TryAdd(item.Key, value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();

        #endregion

        #endregion
    }
}
