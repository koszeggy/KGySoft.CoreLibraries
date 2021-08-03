#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockFreeCache.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.CoreLibraries;

#endregion

#region Suppressions

#if NET40 || NET45 || NET472 || NETSTANDARD
#pragma warning disable CS0436 // Type conflicts with imported type - Using custom SpinWait even if available in some targets
#endif

#endregion

namespace KGySoft.Collections
{
    internal partial class LockFreeCache<TKey, TValue> : IThreadSafeCacheAccessor<TKey, TValue>
        where TKey : notnull
    {
        #region Fields

        private readonly Func<TKey, TValue> itemLoader;
        private readonly IEqualityComparer<TKey>? comparer;
        private readonly int thresholdCapacity;
        private readonly long mergeInterval; // NOTE: it could be long? but that causes VerificationException in .NET Framework when used from a partially trusted domain with SecurityRuleSet.Level2
        private readonly bool bitwiseAndHash;

        private volatile bool isMerging;
        private volatile ReadOnlyDictionary readOnlyStorage;
        private volatile GrowOnlyDictionary? growingStorage;
        private volatile int nextCapacity;
        private long nextMerge;

        #endregion

        #region Properties and Indexers

        #region Properties

        protected ReadOnlyDictionary ReadOnlyStorage => readOnlyStorage;

        #endregion

        #region Indexers

        public virtual TValue this[TKey key]
        {
            get
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);

                uint hashCode = GetHashCode(key);
                ReadOnlyDictionary l1Cache;
                TValue? result;
                do
                {
                    l1Cache = readOnlyStorage;
                    if (l1Cache.TryGetValueInternal(key, hashCode, out result))
                        return result;
                } while (!IsUpToDate(l1Cache));

                GrowOnlyDictionary l2Cache = GetCreateLevel2Cache();
                result = l2Cache.GetOrAddInternal(key, itemLoader, hashCode);
                MergeIfNeeded(l1Cache, l2Cache);
                return result;
            }
        }

        #endregion

        #endregion

        #region Constructors

        internal LockFreeCache(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer, LockFreeCacheOptions options)
        {
            if (itemLoader == null!)
                Throw.ArgumentNullException(Argument.itemLoader);
            if (options == null!)
                Throw.ArgumentNullException(Argument.options);
            if (!options.HashingStrategy.IsDefined())
                Throw.ArgumentException(Argument.options, Res.PropertyMessage(nameof(options.HashingStrategy), Res.EnumOutOfRange(options.HashingStrategy)));
            if (options.MergeInterval < TimeSpan.Zero)
                Throw.ArgumentException(Argument.options, Res.PropertyMustBeGreaterThanOrEqualTo(nameof(options.MergeInterval), TimeSpan.Zero));
            if (options.InitialCapacity <= 0)
                Throw.ArgumentException(Argument.options, Res.PropertyMustBeGreaterThan(nameof(options.InitialCapacity), 0));
            if (options.ThresholdCapacity < options.InitialCapacity)
                Throw.ArgumentException(Argument.options, Res.PropertyMustBeGreaterThanOrEqualToProperty(nameof(options.ThresholdCapacity), nameof(options.InitialCapacity)));

            this.itemLoader = itemLoader;
            this.comparer = ComparerHelper<TKey>.GetSpecialDefaultEqualityComparerOrNull(comparer);
            bitwiseAndHash = options.HashingStrategy.PreferBitwiseAndHash(comparer);
            nextCapacity = options.InitialCapacity;
            thresholdCapacity = options.ThresholdCapacity;
            mergeInterval = options.MergeInterval.HasValue ? TimeHelper.GetInterval(options.MergeInterval.Value) : -1L;
            readOnlyStorage = ReadOnlyDictionary.Empty;
        }

        #endregion

        #region Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        protected GrowOnlyDictionary GetCreateLevel2Cache()
        {
            while (true)
            {
                GrowOnlyDictionary? result = growingStorage;
                if (result != null)
                    return result;

                if (Interlocked.CompareExchange(ref growingStorage, new GrowOnlyDictionary(Math.Max(4, nextCapacity), comparer, bitwiseAndHash), null) == null && mergeInterval >= 0L)
                    Volatile.Write(ref nextMerge, TimeHelper.GetTimeStamp() + mergeInterval);
            }
        }

        protected void MergeIfNeeded(ReadOnlyDictionary l1Cache, GrowOnlyDictionary l2Cache)
        {
            if (isMerging || l2Cache.Count == 0)
                return;

            Debug.Assert(l2Cache.Count > 0 || GetType() != typeof(LockFreeCache<TKey, TValue>), "Unless using a derived type, L2 cache is not expected to be empty here");
            int threshold = nextCapacity;
            int l1Count = l1Cache.Count;
            int l2Count = l2Cache.Count;
            int max = thresholdCapacity;
            bool byCapacity = l2Count >= threshold;
            bool byInterval = !byCapacity && mergeInterval >= 0L && l1Count < max && TimeHelper.GetTimeStamp() > Volatile.Read(ref nextMerge);
            if (!(byCapacity || byInterval))
                return;

            // Indicating that from this point readOnlyStorage will be updated soon and content of growingStorage will be lost
            // Note: this flag just helps to minimize possible loss in L2 cache but actually it does not prevent it completely.
            // In worst case some values will be loaded multiple times but this is acceptable as we don't expect values changing.
            isMerging = true;

            try
            {
                if (byCapacity && threshold < max)
                    // Max(threshold, ...): guard against overflow
                    nextCapacity = Math.Min(max, Math.Max(threshold, Math.Max(l1Count + l2Count, threshold << 1)));

                growingStorage = null;
                readOnlyStorage = new ReadOnlyDictionary(max, l2Cache, l1Cache);
            }
            finally
            {
                isMerging = false;
            }
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        protected bool IsUpToDate(ReadOnlyDictionary l1Cache)
        {
            // L1 cache has been replaced
            if (l1Cache != readOnlyStorage)
                return false;

            if (!isMerging)
                return true;

            // a merge has been started, values from growingStorage storage might be started to copied: preventing current thread from consuming CPU until merge is finished
            var wait = new SpinWait();
            while (isMerging)
                wait.SpinOnce();

            return false;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        protected uint GetHashCode(TKey key)
        {
            IEqualityComparer<TKey>? comp = comparer;
            return (uint)(comp == null ? key.GetHashCode() : comp.GetHashCode(key));
        }

        #endregion
    }
}
