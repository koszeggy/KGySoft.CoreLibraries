#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IThreadSafeCacheAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a read-only indexer for a <see cref="Cache{TKey,TValue}"/> instance, which provides thread-safe access to the cache.
    /// Can be retrieved by the <see cref="Cache{TKey,TValue}.GetThreadSafeAccessor">GetThreadSafeAccessor</see> method.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in the cache.</typeparam>
    /// <typeparam name="TValue">The type of the value in the cache.</typeparam>
    public interface IThreadSafeCacheAccessor<in TKey, out TValue>
        where TKey : notnull
    {
        #region Indexers

        /// <summary>
        /// Gets the value associated with the specified <paramref name="key"/>.
        /// If an element does not exist in the underlying <see cref="Cache{TKey,TValue}"/> instance, then the loader delegate will be
        /// invoked, which was passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
        /// </summary>
        /// <param name="key">Key of the element to get.</param>
        /// <returns>The element with the specified <paramref name="key"/>.</returns>
        TValue this[TKey key] { get; }

        #endregion
    }
}
