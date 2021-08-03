#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IThreadSafeCacheAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a thread-safe accessor for a cache, which provides a read-only indexer to access values.
    /// An instance can be created by the <see cref="O:KGySoft.Collections.ThreadSafeCacheFactory.Create">Create</see> methods of the <see cref="ThreadSafeCacheFactory"/> class,
    /// or if you have a <see cref="Cache{TKey,TValue}"/> instance, you can retrieve a thread-safe accessor for it by the <see cref="Cache{TKey,TValue}.GetThreadSafeAccessor">GetThreadSafeAccessor</see> method.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in the cache.</typeparam>
    /// <typeparam name="TValue">The type of the value in the cache.</typeparam>
    /// <seealso cref="ThreadSafeCacheFactory"/>
    /// <seealso cref="Cache{TKey,TValue}.GetThreadSafeAccessor"/>
    public interface IThreadSafeCacheAccessor<in TKey, out TValue>
        where TKey : notnull
    {
        #region Indexers

        /// <summary>
        /// Gets the value associated with the specified <paramref name="key"/>.
        /// If a value does not exist in the underlying cache, then the loader delegate will be invoked,
        /// which was specified when this <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance was created.
        /// </summary>
        /// <param name="key">The key of the value to be retrieved.</param>
        /// <returns>The value of the corresponding <paramref name="key"/>.</returns>
        /// <seealso cref="ThreadSafeCacheFactory"/>
        /// <seealso cref="Cache{TKey,TValue}.GetThreadSafeAccessor"/>
        TValue this[TKey key] { get; }

        #endregion
    }
}
