#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockingCacheOptions.cs
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

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents the options for creating a thread-safe accessor by the
    /// <see cref="O:KGySoft.Collections.ThreadSafeCacheFactory.Create"><![CDATA[ThreadSafeCacheFactory.Create<TKey, TValue>]]></see> methods
    /// <br/>To see when to use <see cref="LockFreeCacheOptions"/> or <see cref="LockingCacheOptions"/> see the <strong>Remarks</strong> section.
    /// of the <see cref="ThreadSafeCacheFactory.Create{TKey,TValue}(Func{TKey,TValue},IEqualityComparer{TKey},ThreadSafeCacheOptionsBase)"/> method.
    /// </summary>
    /// <seealso cref="ThreadSafeCacheFactory" />
    /// <seealso cref="ThreadSafeCacheOptionsBase" />
    public sealed class LockingCacheOptions : ThreadSafeCacheOptionsBase
    {
        #region Properties

        /// <summary>
        /// Gets or sets the capacity of the cache to be created. If the cache is full, then the oldest or the least recent used element
        /// (depending on the <see cref="Behavior"/> property) will be dropped from the cache.
        /// <br/>Default value: <c>1024</c>.
        /// </summary>
        public int Capacity { get; set; } = 1024;

        /// <summary>
        /// Gets or sets whether adding the first item to the cache should allocate memory the full cache <see cref="Capacity"/>.
        /// If <see langword="false"/>, then the internal storage is dynamically reallocated while adding new elements until reaching <see cref="Capacity"/>.
        /// Set it to <see langword="true"/>&#160;if it is almost certain that the cache will be full when using it.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public bool PreallocateCapacity { get; set; }

        /// <summary>
        /// Gets or sets the cache behavior when cache is full and an element has to be removed.
        /// The cache is full, when the number of stored items reaches <see cref="Capacity"/>.
        /// Default value: <see cref="CacheBehavior.RemoveLeastRecentUsedElement"/>.
        /// </summary>
        public CacheBehavior Behavior { get; set; } = CacheBehavior.RemoveLeastRecentUsedElement;

        /// <summary>
        /// Gets or sets whether dropped values are disposed if they implement <see cref="IDisposable"/>.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public bool DisposeDroppedValues { get; set; }

        /// <summary>
        /// Gets or sets whether the item loader delegate that is specified by the
        /// <see cref="O:KGySoft.Collections.ThreadSafeCacheFactory.Create"><![CDATA[ThreadSafeCacheFactory.Create<TKey, TValue>]]></see>
        /// methods is protected from invoking it concurrently.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <value><see langword="true"/>&#160;to protect the item loader delegate (it will not be called concurrently);
        /// <see langword="false"/>&#160;to allow the item loader delegate to be called concurrently.</value>
        public bool ProtectItemLoader { get; set; }

        /// <summary>
        /// Gets or sets an expiration time for the values to be stored in the cache. If <see langword="null"/>, then the values will not expire.
        /// <br/>Default value: <see langword="null"/>.
        /// <remarks>
        /// <para>Even if this property is <see langword="null"/>, values might be reloaded from time to time because if the cache is full (see <see cref="Capacity"/>)
        /// oldest or least recent used elements (see <see cref="Behavior"/>) are dropped from the cache.</para>
        /// <para>Depending on the targeted platform it is possible that values will not expire for at least 15 milliseconds.</para>
        /// </remarks>
        /// </summary>
        public TimeSpan? Expiration { get; set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
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
