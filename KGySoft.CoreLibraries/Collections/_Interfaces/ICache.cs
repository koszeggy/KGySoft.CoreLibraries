#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICache.cs
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
using System.Collections;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Provides a non-generic access to the <see cref="Cache{TKey,TValue}"/> class.
    /// </summary>
    public interface ICache : IDictionary
    {
        #region Properties

        /// <summary>
        /// Gets or sets the capacity of the cache. If new value is smaller than elements count (value of the <see cref="ICollection.Count"/> property),
        /// then old or least used elements (depending on <see cref="Behavior"/>) will be removed from the <see cref="ICache"/>.
        /// </summary>
        int Capacity { get; set; }

        /// <summary>
        /// Gets or sets the cache behavior when cache is full and an element has to be removed.
        /// The cache is full, when <see cref="ICollection.Count"/> reaches the <see cref="Capacity"/>.
        /// </summary>
        /// <seealso cref="CacheBehavior"/>
        CacheBehavior Behavior { get; set; }

        /// <summary>
        /// Gets or sets whether adding the first item to the cache or resetting <see cref="Capacity"/> on a non-empty cache should
        /// allocate memory for all cache entries.
        /// </summary>
        bool EnsureCapacity { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Renews the value with the specified <paramref name="key"/> in the evaluation order.
        /// </summary>
        /// <param name="key">The key of the item to renew.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> does not exist in the <see cref="ICache"/>.</exception>
        void Touch(object key);

        /// <summary>
        /// Refreshes the value in the cache even if it was already loaded.
        /// </summary>
        /// <param name="key">The key of the item to refresh.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        void RefreshValue(object key);

        /// <summary>
        /// Reloads the value into the cache even if it was already loaded using the item loader that was passed to the constructor.
        /// </summary>
        /// <param name="key">The key of the item to reload.</param>
        /// <returns>Loaded value</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        object? GetValueUncached(object key);

        /// <summary>
        /// Clears the cache and resets statistics.
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets statistics of the cache.
        /// </summary>
        /// <returns>An <see cref="ICacheStatistics"/> instance that provides statistical information about the <see cref="ICache"/> instance.</returns>
        ICacheStatistics GetStatistics();

        #endregion
    }
}
