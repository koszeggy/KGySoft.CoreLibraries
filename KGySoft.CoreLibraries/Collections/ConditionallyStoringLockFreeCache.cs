#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ConditionallyStoringLockFreeCache.cs
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

#endregion

namespace KGySoft.Collections
{
    internal class ConditionallyStoringLockFreeCache<TKey, TValue> : LockFreeCache<TKey, TValue>
        where TKey : notnull
    {
        #region Fields

        #region Static Fields

        private static readonly Func<TKey, TValue> dummyLoader = _ => default!;

        #endregion

        #region Instance Fields

        private readonly ConditionallyStoringItemLoader<TKey, TValue> itemLoader;

        #endregion

        #endregion

        #region Indexers

        public override TValue this[TKey key]
        {
            get
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                uint hashCode = GetHashCode(key);
                ReadOnlyDictionary l1Cache;
                TValue? result;
                do
                {
                    l1Cache = ReadOnlyStorage;
                    if (l1Cache.TryGetValueInternal(key, hashCode, out result))
                        return result;
                } while (!IsUpToDate(l1Cache));

                GrowOnlyDictionary l2Cache = GetCreateLevel2Cache();
                if (!l2Cache.TryGetValueInternal(key, hashCode, out result))
                {
                    result = itemLoader.Invoke(key, out bool storeValue);
                    if (storeValue)
                        result = l2Cache.GetOrAddInternal(key, result, hashCode);
                }

                MergeIfNeeded(l1Cache, l2Cache);
                return result!;
            }
        }

        #endregion

        #region Constructors

        internal ConditionallyStoringLockFreeCache(ConditionallyStoringItemLoader<TKey, TValue> itemLoader, LockFreeCacheOptions options)
            : base(dummyLoader, null, options)
        {
            this.itemLoader = itemLoader;
        }

        #endregion
    }
}
