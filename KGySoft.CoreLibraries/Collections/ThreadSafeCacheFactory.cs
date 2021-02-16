#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeCacheFactory.cs
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
    /// <summary>
    /// Provides factory methods to create thread-safe cache instances as <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> implementations.
    /// </summary>
    public static class ThreadSafeCacheFactory
    {
        #region Methods

        public static IThreadSafeCacheAccessor<TKey, TValue> Create<TKey, TValue>(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer, ThreadSafeCacheOptionsBase? options = null)
            where TKey : notnull
        {
            if (itemLoader == null!)
                Throw.ArgumentNullException(Argument.itemLoader);
            options ??= LockFreeCacheOptions.DefaultOptions;
            return options.CreateInstance(itemLoader, comparer);
        }

        public static IThreadSafeCacheAccessor<TKey, TValue> Create<TKey, TValue>(Func<TKey, TValue> itemLoader, ThreadSafeCacheOptionsBase? options = null) where TKey : notnull
            => Create(itemLoader, null, options);

        #endregion
    }
}