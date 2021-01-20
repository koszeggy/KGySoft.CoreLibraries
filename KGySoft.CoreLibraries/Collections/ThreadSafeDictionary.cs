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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    internal class ThreadSafeDictionary<TKey, TValue> //: IDictionary<TKey, TValue> // TODO: IDictionary, IReadOnlyDictionary
        where TKey : notnull
    {
        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        private readonly object syncRoot = new object();
        private readonly IEqualityComparer<TKey>? comparer;
        private readonly int initialExpandoCapacity;
        private readonly bool bitwiseAndHash;

        /// <summary>
        /// When reading, values here are accessed without locking.
        /// Once a new key is added, it is never removed anymore even for deleted entries.
        /// </summary>
        private volatile FixedSizeDictionary<TKey, TValue> fixedSizeStorage;
        /// <summary>
        /// A temporary storage for new values. It is regularly merged with fixedSizeStorage into a new fixed size instance.
        /// </summary>
        private volatile CustomDictionary<TKey, TValue>? expandoStorage;
        private TimeSpan mergeInterval = TimeSpan.FromMilliseconds(100);
        private DateTime nextMerge;
        private volatile bool isMerging;

        #endregion

        #region Properties and Indexers

        #region Properties

        public int Count
        {
            get
            {
                if (expandoStorage == null)
                    return fixedSizeStorage.Count;

                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue>? lockingStorage = expandoStorage;

                    // lost race
                    if (lockingStorage == null)
                        return fixedSizeStorage.Count;

                    int result = fixedSizeStorage.Count + lockingStorage.Count;
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

                    if (expandoStorage == null || value < TimeSpan.Zero)
                        return;

                    if (value > TimeSpan.Zero)
                        nextMerge = DateTime.UtcNow + value;
                    else
                        EnsureMerged();
                }
            }
        }

        #endregion

        #region Indexers

        public TValue this[TKey key]
        {
            get => TryGetValue(key, out TValue value) ? value : Throw.KeyNotFoundException<TValue>(Res.IDictionaryKeyNotFound);
            set => TryInsertInternal(key, value, DictionaryInsertion.OverwriteIfExists);
        }

        #endregion

        #endregion

        #region Constructors

        public ThreadSafeDictionary() : this(defaultCapacity)
        {
        }

        public ThreadSafeDictionary(IEqualityComparer<TKey>? comparer, HashingStrategy strategy = HashingStrategy.Auto)
            : this(defaultCapacity, comparer, strategy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="capacity">Specifies the initial minimum capacity of the internal temporal storage for the newly added keys. If 0, then a default value is used.</param>
        /// <param name="comparer">TODO</param>
        /// <param name="strategy">TODO</param>
        public ThreadSafeDictionary(int capacity, IEqualityComparer<TKey>? comparer = null, HashingStrategy strategy = HashingStrategy.Auto)
        {
            if (!strategy.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);
            if (capacity <= 0)
            {
                if (capacity < 0)
                    Throw.ArgumentOutOfRangeException(Argument.capacity, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                capacity = defaultCapacity;
            }

            fixedSizeStorage = FixedSizeDictionary<TKey, TValue>.Empty;
            initialExpandoCapacity = capacity;
            bitwiseAndHash = strategy.PreferBitwiseAndHash(comparer);
            this.comparer = comparer;
        }

        public ThreadSafeDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer = null, HashingStrategy strategy = HashingStrategy.Auto)
            : this(defaultCapacity, comparer, strategy)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            fixedSizeStorage = new FixedSizeDictionary<TKey, TValue>(bitwiseAndHash, dictionary, comparer);
        }

        #endregion

        #region Methods

        #region Public Methods

        public void Add(TKey key, TValue value) => TryInsertInternal(key, value, DictionaryInsertion.ThrowIfExists);

        //public bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
        //{
        //    if (key == null!)
        //        Throw.ArgumentNullException(Argument.key);

        //    uint hashCode = GetHashCode(key);
        //    //bool? found = fastStorage.TryGetValueInternal(key, out value, out hashCode);
        //    //if (found == null) // deleted
        //    //    return false;

        //    ref TValue result = ref lockFreeStorage.TryGetValueInternal(key, hashCode, out bool deleted);
        //    if (!Unsafe.IsNullRef(ref result))
        //    {
        //        value = result;
        //        return true;
        //    }

        //    if (deleted)
        //        goto NotFound;

        //    if (lockingStorage == null)
        //        goto NotFound;

        //    lock (syncRoot)
        //    {
        //        CustomDictionary<TKey, TValue>? lockingStorage = lockingStorage;

        //        // lost race
        //        if (lockingStorage == null)
        //            goto NotFound;

        //        result = ref lockingStorage.TryGetValueInternal(key, hashCode);
        //        MergeIfExpired();
        //        if (!Unsafe.IsNullRef(ref result))
        //        {
        //            value = result;
        //            return true;
        //        }
        //    }

        //    // Exit point when value was not found. Could be avoided by duplicating this code everywhere but performance is more important here.
        //NotFound:
        //    value = default;
        //    return false;
        //}

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            bool? success = fixedSizeStorage.TryGetValueInternal(key, hashCode, out value);
            if (success.HasValue)
                return success.Value;

            if (expandoStorage == null)
                return false;

            lock (syncRoot)
            {
                CustomDictionary<TKey, TValue>? lockingStorage = expandoStorage;

                // lost race
                if (lockingStorage == null)
                    return false;

                bool result = lockingStorage.TryGetValueInternal(key, hashCode, out value);
                MergeIfExpired();
                return result;
            }
        }

        public bool TryAdd(TKey key, TValue value) => TryInsertInternal(key, value, DictionaryInsertion.DoNotOverwrite);

        public bool TryUpdate(TKey key, TValue newValue, TValue originalValue)
        {
            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeDictionary<TKey, TValue>? fastStorage = fixedSizeStorage;
                bool? success = fastStorage.TryReplaceInternal(key, newValue, originalValue, hashCode);
                if (success == true)
                {
                    if (IsUpToDate(fastStorage))
                        return true;
                    continue;
                }

                // already deleted or originalValue does not match
                if (success == false)
                    return false;

                if (expandoStorage == null)
                    return false;

                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue>? lockingStorage = expandoStorage;

                    // lost race
                    if (lockingStorage == null)
                        return false;

                    bool result = lockingStorage.TryReplaceInternal(key, newValue, originalValue, hashCode);
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
                FixedSizeDictionary<TKey, TValue> fastStorage = fixedSizeStorage;
                bool? success = fastStorage.TryRemoveInternal(key, hashCode);

                // making sure that correct result is returned even if multiple tries are necessary due to merging
                if (success == true)
                    removed = true;

                // successfully removed from fixed-size storage (now, or in a previous attempt)
                if (removed)
                {
                    if (IsUpToDate(fastStorage))
                        return true;

                    continue;
                }

                // item was already deleted (if we removed it in a previous attempt, then true is returned above)
                if (success == false)
                    return false;

                if (expandoStorage == null)
                    return false;

                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue>? lockingStorage = expandoStorage;

                    // lost race
                    if (lockingStorage == null)
                        return false;

                    bool result = lockingStorage.TryRemoveInternal(key, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
            => TryRemoveInternal(key, out value);

        public void Clear()
        {
            // It is not a problem if a merge is in progress because it will nullify expandoStorage in the end anyway
            expandoStorage = null;

            while (true)
            {
                FixedSizeDictionary<TKey, TValue> fastStorage = fixedSizeStorage;
                fastStorage.Clear();
                if (IsUpToDate(fastStorage))
                    return;
            }
        }

        public void EnsureMerged()
        {
            if (expandoStorage == null)
                return;

            lock (syncRoot)
            {
                CustomDictionary<TKey, TValue>? lockingStorage = expandoStorage;

                // lost race
                if (lockingStorage == null)
                    return;

                DoMerge();
            }
        }

        #endregion

        #region Private Methods

        private bool TryInsertInternal(TKey key, TValue value, DictionaryInsertion behavior)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeDictionary<TKey, TValue> fastStorage = fixedSizeStorage;
                bool? success = fastStorage.TryInsertInternal(key, value, hashCode, behavior);
                if (success == true)
                {
                    if (IsUpToDate(fastStorage))
                        return true;
                    continue;
                }

                // duplicate key
                if (success == false)
                    return false;

                // TODO: if mergeInterval == Zero... - merge immediately (actually not really needed just for performance reasons)
                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue> lockingStorage = GetCreateExpandoStorage();
                    bool result = lockingStorage.TryInsertInternal(key, value, hashCode, behavior);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        //private bool TryRemoveInternal(TKey key, [MaybeNullWhen(false)]out TValue value)
        //{
        //    if (key == null!)
        //        Throw.ArgumentNullException(Argument.key);

        //    uint hashCode = GetHashCode(key);
        //    ref TValue toReturnRef = ref Unsafe.NullRef<TValue>();
        //    while (true)
        //    {
        //        FixedSizeDictionary3<TKey, TValue> fastStorage = lockFreeStorage;

        //        // note: StrongBox instead of an out parameter, which would cause problems with assigning toReturn
        //        var isAlreadyDeleted = new StrongBox<bool>();
        //        ref TValue removedValueRef = ref fastStorage.TryRemoveInternal(key, hashCode, isAlreadyDeleted);

        //        // making sure that removed entry is returned even if multiple tries are necessary due to merging
        //        if (!Unsafe.IsNullRef(ref removedValueRef))
        //            toReturnRef = ref removedValueRef;

        //        // successfully removed from lock-free storage (now, or in a previous attempt)
        //        if (!Unsafe.IsNullRef(ref toReturnRef))
        //        {
        //            // if we could remove the value from the lock-free storage we have to make sure the removal is persistent
        //            if (IsUpToDate(fastStorage))
        //            {
        //                Debug.Assert(!Unsafe.IsNullRef(ref toReturnRef));
        //                value = toReturnRef;
        //                return true;
        //            }

        //            continue;
        //        }

        //        // item was already deleted (if we removed it in a previous attempt, then it is returned above)
        //        if (isAlreadyDeleted.Value)
        //            goto Default;

        //        if (lockingStorage == null)
        //            goto Default;

        //        lock (syncRoot)
        //        {
        //            CustomDictionary<TKey, TValue>? lockingStorage = lockingStorage;

        //            // lost race
        //            if (lockingStorage == null)
        //                goto Default;

        //            bool result = lockingStorage.TryRemoveInternal(key, hashCode, out value);
        //            MergeIfExpired();
        //            return result;
        //        }
        //    }

        //// Exit point when key was not deleted. Could be avoided by duplicating this code everywhere but performance is more important here.
        //Default:
        //    value = default;
        //    return false;
        //}

        private bool TryRemoveInternal(TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeDictionary<TKey, TValue> fastStorage = fixedSizeStorage;
                bool? success = fastStorage.TryRemoveInternal(key, hashCode, out value);

                // making sure that removed entry is returned even if multiple tries are necessary due to merging
                if (success == true)
                {
                    if (IsUpToDate(fastStorage))
                        return true;

                    continue;
                }

                // item was already deleted
                if (success == false)
                    return false;

                if (expandoStorage == null)
                    return false;

                lock (syncRoot)
                {
                    CustomDictionary<TKey, TValue>? lockingStorage = expandoStorage;

                    // lost race
                    if (lockingStorage == null)
                        return false;

                    bool result = lockingStorage.TryRemoveInternal(key, hashCode, out value);
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

        private CustomDictionary<TKey, TValue> GetCreateExpandoStorage()
        {
            CustomDictionary<TKey, TValue>? result = expandoStorage;
            if (result != null)
                return result;
            result = expandoStorage = new CustomDictionary<TKey, TValue>(bitwiseAndHash, initialExpandoCapacity, comparer);
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
            Debug.Assert(!isMerging || expandoStorage != null, "Make sure caller is in a lock");

            CustomDictionary<TKey, TValue> lockingStorage = expandoStorage!;
            if (lockingStorage.Count != 0)
            {
                // Indicating that from this point fixedSizeStorage cannot be considered safe even though its reference is not replaced yet.
                // Note: we could spare the flag if we just nullified fixedSizeStorage before merging but this way can prevent that keys in the
                // fixed-size storage reappear in the locking one.
                isMerging = true;
                try
                {
                    fixedSizeStorage = new FixedSizeDictionary<TKey, TValue>(fixedSizeStorage, lockingStorage);
                }
                finally
                {
                    // now we can reset the flag because an outdated storage reference can be detected safely
                    isMerging = false;
                }
            }

            expandoStorage = null;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private bool IsUpToDate(FixedSizeDictionary<TKey, TValue> fastStorage)
        {
            // fixed-size storage has been replaced
            if (fastStorage != fixedSizeStorage)
                return false;

            // a merge has been started, values from fastStorage storage might be started to copied: preventing current thread from consuming CPU until merge is finished
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

        #endregion
    }
}
