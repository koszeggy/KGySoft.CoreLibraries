#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockFreeCacheOptions.cs
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

#endregion

namespace KGySoft.Collections
{
    public sealed class LockFreeCacheOptions : ThreadSafeCacheOptionsBase
    {
        #region Fields

        internal static readonly LockFreeCacheOptions DefaultOptions = new LockFreeCacheOptions();

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets the number of the required elements in the slower L2 cache, which triggers the first merge into the faster L1 cache.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <para>If the value of this property is much lower than the expected number of items to store, then a merge operation
        /// may occur too often in the beginning, which has some cost.</para>
        /// <para>If the value of this property is higher than the expected number of items to store, then cached elements might remain in the slower L2 cache.</para>
        /// <para>The L1 capacity will be doubled in each merge operation until <see cref="MaximumL2Capacity"/> is reached.
        /// A tiered cache instance may store about twice as many elements as it is specified by the <see cref="MaximumL2Capacity"/> when it is completely full.</para>
        /// <para>If it cannot be really determined how many items in the cache will stored, then you can set the <see cref="MergeInterval"/> property,
        /// which makes a regular merge operation possible even if the required capacity limit is not reached.</para>
        /// // TODO: What faster/slower L1/L2 means: depending on implementation they can be the same type with/o lock, or
        /// a fast Dictionary-like L1 cache vs a slower ConcurrentDictionary-like L2 cache (where L2 is somewhat faster than a ConcurrentDictionary and is also completely lock-free)
        /// </remarks>
        public int InitialL2Capacity { get; set; } = 16;

        public int MaximumL2Capacity { get; set; } = 1024;

        // if merge can occur also based on time. After merge the new expando capacity will be Min(doubledLockFree.Count, maxCapacity).
        // If there is no merge but swapping the swap does not occur until expando.Count < lockFree.Count
        public TimeSpan? MergeInterval { get; set; }

        public HashingStrategy HashingStrategy { get; set; }

        #endregion

        #endregion

        #region Methods

        protected internal override IThreadSafeCacheAccessor<TKey, TValue> CreateInstance<TKey, TValue>(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer)
            => new LockFreeCache<TKey, TValue>(itemLoader, comparer, this);

        #endregion
    }
}
