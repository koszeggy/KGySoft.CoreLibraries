#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeCacheFactory.cs
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

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Provides factory methods to create thread-safe cache instances as <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> implementations.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="Create{TKey, TValue}(Func{TKey, TValue},IEqualityComparer{TKey},ThreadSafeCacheOptionsBase)"/> method for details.
    /// </summary>
    public static class ThreadSafeCacheFactory
    {
        #region Methods

        #region Public Methods

        /// <summary>
        /// Creates a thread safe cache instance that can be accessed as an <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <typeparam name="TKey">The type of the key in the cache.</typeparam>
        /// <typeparam name="TValue">The type of the value in the cache.</typeparam>
        /// <param name="itemLoader">A delegate for loading a value, which is invoked when a key is not present in the cache.</param>
        /// <param name="comparer">An equality comparer to be used for hashing and comparing keys. If <see langword="null"/>, then a default comparison is used.</param>
        /// <param name="options">The options for creating the cache. If <see langword="null"/>, then a default <see cref="LockFreeCacheOptions"/> instance will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance that can be used to read the underlying cache in a thread-safe manner.</returns>
        /// <remarks>
        /// <note type="tip">If <typeparamref name="TKey"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
        /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the <paramref name="comparer"/> parameter for better performance.
        /// Or, you can use <see cref="StringSegmentComparer.OrdinalRandomized">StringSegmentComparer.OrdinalRandomized</see> to use a comparer with randomized hash also on
        /// platforms where default string hashing is not randomized (eg. .NET Framework 3.5).</note>
        /// <para>A cache is similar to a dictionary (in terms of using a fast, associative storage) but additionally provides capacity management and transparent access (meaning,
        /// all that is needed is to read the <see cref="IThreadSafeCacheAccessor{TKey,TValue}.this">indexer</see> of the returned <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance, and
        /// it is transparent for the consumer whether the returned item was returned from the cache or it was loaded by invoking the specified <paramref name="itemLoader"/>).</para>
        /// <para>If <paramref name="options"/> is <see langword="null"/>, then a lock-free cache instance will be created as if a <see cref="LockFreeCacheOptions"/> was used with its default settings.</para>
        /// <para>In <c>KGy SOFT Core Libraries</c> there are two predefined classes that can be used to create a thread-safe cache instance: <see cref="LockFreeCacheOptions"/> and <see cref="LockingCacheOptions"/>.</para>
        /// <note type="tip">
        /// <list type="bullet">
        /// <item><see cref="LockFreeCacheOptions"/>: Use this one if you want the fastest, well scalable solution and it is not a problem that the <paramref name="itemLoader"/> delegate might
        /// be called concurrently, or capacity management is not too strict (when cache is full, about the half of the elements are dropped at once). Though rarely, it may also happen
        /// that <paramref name="itemLoader"/> is invoked multiple times when accessing the same key consecutively and the first call occurred during an internal merge session.</item>
        /// <item><see cref="LockingCacheOptions"/>: Use this one if you need strict capacity management, you want to dispose the dropped-out values, you want to ensure that the oldest
        /// or least recent used element is dropped in the first place, you want to protect the <paramref name="itemLoader"/> delegate from calling it concurrently, or if you want
        /// to specify an expiration time period for the values. If elements are often dropped, then it also uses less memory than the lock-free implementation. Depending on the configuration
        /// the actual type of the returned instance may vary but in all cases an instance of the public <see cref="Cache{TKey,TValue}"/> type will be wrapped internally.</item>
        /// </list>
        /// </note>
        /// </remarks>
        public static IThreadSafeCacheAccessor<TKey, TValue> Create<TKey, TValue>(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer, ThreadSafeCacheOptionsBase? options = null)
            where TKey : notnull
        {
            if (itemLoader == null!)
                Throw.ArgumentNullException(Argument.itemLoader);
            options ??= LockFreeCacheOptions.DefaultOptions;
            return options.CreateInstance(itemLoader, comparer);
        }

        /// <summary>
        /// Creates a thread safe cache instance that can be accessed as an <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Create{TKey, TValue}(Func{TKey, TValue},IEqualityComparer{TKey},ThreadSafeCacheOptionsBase)"/> overload for details.
        /// </summary>
        /// <typeparam name="TKey">The type of the key in the cache.</typeparam>
        /// <typeparam name="TValue">The type of the value in the cache.</typeparam>
        /// <param name="itemLoader">A delegate for loading a value, which is invoked when a key is not present in the cache.</param>
        /// <param name="options">The options for creating the cache. If <see langword="null"/>, then a default <see cref="LockFreeCacheOptions"/> instance will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance that can be used to read the underlying cache in a thread-safe manner.</returns>
        public static IThreadSafeCacheAccessor<TKey, TValue> Create<TKey, TValue>(Func<TKey, TValue> itemLoader, ThreadSafeCacheOptionsBase? options = null) where TKey : notnull
            => Create(itemLoader, null, options);

        #endregion

        #region Internal Methods
        
        internal static IThreadSafeCacheAccessor<TKey, TValue> Create<TKey, TValue>(ConditionallyStoringItemLoader<TKey, TValue> itemLoader, LockFreeCacheOptions options)
            where TKey : notnull
        {
            return new ConditionallyStoringLockFreeCache<TKey, TValue>(itemLoader, options);
        }

        #endregion

        #endregion
    }
}