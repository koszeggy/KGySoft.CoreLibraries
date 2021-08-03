#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeCacheOptionsBase.cs
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
    /// Represents the options for a thread safe cache instance to be created by the <see cref="O:KGySoft.Collections.ThreadSafeCacheFactory.Create"><![CDATA[ThreadSafeCacheFactory.Create<TKey, TValue>]]></see> methods.
    /// You can use the <see cref="LockingCacheOptions"/> and <see cref="LockFreeCacheOptions"/> types as built-in implementations.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ThreadSafeCacheFactory.Create{TKey,TValue}(Func{TKey,TValue},IEqualityComparer{TKey},ThreadSafeCacheOptionsBase)"/> method for details.
    /// </summary>
    public abstract class ThreadSafeCacheOptionsBase
    {
        #region Methods

        /// <summary>
        /// Creates an <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance, whose settings are represented by this <see cref="ThreadSafeCacheOptionsBase"/> instance.
        /// </summary>
        /// <typeparam name="TKey">The type of the key in the cache.</typeparam>
        /// <typeparam name="TValue">The type of the value in the cache.</typeparam>
        /// <param name="itemLoader">A delegate that is invoked when an item is not present in the cache.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> instance to be used for comparing keys in the cache instance to create.
        /// When <see langword="null"/>, a default comparer will be used.</param>
        /// <returns>An <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance, whose settings are represented by this <see cref="ThreadSafeCacheOptionsBase"/> instance.</returns>
        protected internal abstract IThreadSafeCacheAccessor<TKey, TValue> CreateInstance<TKey, TValue>(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer)
            where TKey : notnull;

        #endregion
    }
}
