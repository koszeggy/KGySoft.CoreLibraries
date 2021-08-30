#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockFreeCacheOptions.cs
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
    /// Represents the options for creating a fast, lock-free, thread safe cache by the
    /// <see cref="O:KGySoft.Collections.ThreadSafeCacheFactory.Create"><![CDATA[ThreadSafeCacheFactory.Create<TKey, TValue>]]></see> methods.
    /// <br/>To see when to use <see cref="LockFreeCacheOptions"/> or <see cref="LockingCacheOptions"/> see the <strong>Remarks</strong> section.
    /// of the <see cref="ThreadSafeCacheFactory.Create{TKey,TValue}(Func{TKey,TValue},IEqualityComparer{TKey},ThreadSafeCacheOptionsBase)"/> method.
    /// </summary>
    /// <seealso cref="ThreadSafeCacheFactory" />
    /// <seealso cref="ThreadSafeCacheOptionsBase" />
    public sealed class LockFreeCacheOptions : ThreadSafeCacheOptionsBase
    {
        #region Fields

        internal static readonly LockFreeCacheOptions DefaultOptions = new LockFreeCacheOptions();
        internal static readonly LockFreeCacheOptions Profile4 = new LockFreeCacheOptions { InitialCapacity = 4, ThresholdCapacity = 4, HashingStrategy = HashingStrategy.And, MergeInterval = TimeSpan.FromSeconds(1) };
        internal static readonly LockFreeCacheOptions Profile16 = new LockFreeCacheOptions { InitialCapacity = 4, ThresholdCapacity = 16, HashingStrategy = HashingStrategy.And, MergeInterval = TimeSpan.FromSeconds(1) };
        internal static readonly LockFreeCacheOptions Profile128 = new LockFreeCacheOptions { ThresholdCapacity = 128, HashingStrategy = HashingStrategy.And, MergeInterval = TimeSpan.FromSeconds(1) };
        internal static readonly LockFreeCacheOptions Profile256 = new LockFreeCacheOptions { ThresholdCapacity = 256, HashingStrategy = HashingStrategy.And, MergeInterval = TimeSpan.FromSeconds(1) };
        internal static readonly LockFreeCacheOptions Profile1K = new LockFreeCacheOptions { ThresholdCapacity = 1024, HashingStrategy = HashingStrategy.And, MergeInterval = TimeSpan.FromSeconds(1) };
        internal static readonly LockFreeCacheOptions Profile8K = new LockFreeCacheOptions { ThresholdCapacity = 8192, HashingStrategy = HashingStrategy.And, MergeInterval = TimeSpan.FromSeconds(1) };

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the initial capacity of the cache.
        /// <br/>Default value: <c>16</c>.
        /// <br/>See also the <strong>Remarks</strong> section of the <see cref="ThresholdCapacity"/> property for details.
        /// </summary>
        public int InitialCapacity { get; set; } = 16;

        /// <summary>
        /// Gets or sets the maximum number of elements, which triggers a merge operation from the underlying dynamic growing storage into the faster read-only storage.
        /// Specifies also the number of elements to be kept when older elements are dropped from the cache. The actual maximum number of stored items may be about twice of this value.
        /// <br/>Default value: <c>1024</c>.
        /// </summary>
        /// <remarks>
        /// <para>The value of the <see cref="ThresholdCapacity"/> property must be greater or equal to the value of the <see cref="InitialCapacity"/> property.</para>
        /// <para>When the first element is about to be stored a dynamic storage is allocated that can optimally store about as many elements as specified by the <see cref="InitialCapacity"/> property.
        /// When the number of stored elements reaches <see cref="InitialCapacity"/> capacity, then content of the dynamic storage is copied into a faster read-only storage, and for additional elements
        /// a new dynamic storage is allocated with either doubled capacity or the specified <see cref="ThresholdCapacity"/>, whichever is less.</para>
        /// <para>Once the number of stored elements in the dynamically growing storage reaches <see cref="ThresholdCapacity"/>, the complete previous content of the faster read-only storage is replaced
        /// by the elements in the growing storage. Therefore, when adding new items continuously, the number of stored elements will be between <see cref="ThresholdCapacity"/> and twice of <see cref="ThresholdCapacity"/>.</para>
        /// <para>If it cannot be really estimated how many items in the cache will be stored, then you can set the <see cref="MergeInterval"/> property,
        /// which can trigger a merge operation to the faster storage by time, regardless of reaching the required capacity limit.</para>
        /// </remarks>
        public int ThresholdCapacity { get; set; } = 1024;

        /// <summary>
        /// Gets or sets a time period, which may trigger a merge operation into the faster read-only storage even when no new elements are added.
        /// If <see langword="null"/>, then time-based merging is disabled.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <para>Even if this property is set, capacity-based merging works as described in the <strong>Remarks</strong> section of the <see cref="ThresholdCapacity"/> property.</para>
        /// <para>Even if this property is set, no extra resources (such as timer) are used. No merging occurs if elements are retrieved from the faster read-only cache.
        /// A time-based merging may occur only if the dynamically growing storage is accessed.</para>
        /// <para>Once the stored number of elements reaches <see cref="ThresholdCapacity"/> the value of this property is ignored.</para>
        /// <para>Depending on the targeted platform it is possible that no time-based merging occurs more often than 15 milliseconds.</para>
        /// <note type="tip">Set this property when it is likely that the number of stored elements will not reach the specified capacities (maybe even <see cref="InitialCapacity"/>)
        /// and you still want to make sure that the stored elements are merged into the faster read-only storage even if no newer elements are added.</note>
        /// </remarks>
        public TimeSpan? MergeInterval { get; set; }

        /// <summary>
        /// Gets or sets the hashing strategy to be used by the cache.
        /// <br/>Default value: <see cref="Collections.HashingStrategy.Auto"/>.
        /// </summary>
        public HashingStrategy HashingStrategy { get; set; }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected internal override IThreadSafeCacheAccessor<TKey, TValue> CreateInstance<TKey, TValue>(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer)
            => new LockFreeCache<TKey, TValue>(itemLoader, comparer, this);

        #endregion
    }
}
