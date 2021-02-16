#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockingCacheOptions.cs
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
    public sealed class LockingCacheOptions : ThreadSafeCacheOptionsBase
    {
        #region Properties

        public int Capacity { get; set; } = 1024;

        public bool PreallocateCapacity { get; set; }

        public CacheBehavior Behavior { get; set; } = CacheBehavior.RemoveLeastRecentUsedElement;

        public bool DisposeDroppedValues { get; set; }

        public bool ProtectItemLoader { get; set; }

        public TimeSpan? Expiration { get; set; }

        #endregion

        #region Methods

        protected internal override IThreadSafeCacheAccessor<TKey, TValue> CreateInstance<TKey, TValue>(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer)
        {
            // Regular Cache instance with thread safe accessor
            if (Expiration == null)
            {
                return new Cache<TKey, TValue>(itemLoader, Capacity, comparer)
                {
                    EnsureCapacity = PreallocateCapacity,
                    Behavior = Behavior,
                    DisposeDroppedValues = DisposeDroppedValues

                }.GetThreadSafeAccessor(ProtectItemLoader);
            }

            // Special Cache with expiring elements
            return ExpiringValuesCache<TKey, TValue>.Create(itemLoader, comparer, this);
        }

        #endregion
    }
}
