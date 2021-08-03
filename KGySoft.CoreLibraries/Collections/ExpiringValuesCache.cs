#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ExpiringValuesCache.cs
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
using System.Diagnostics;

#endregion

namespace KGySoft.Collections
{
    internal class ExpiringValuesCache<TKey, TValue> : IThreadSafeCacheAccessor<TKey, TValue>
        where TKey : notnull
    {
        #region Nested types

        #region ExpiringValuesCacheAllowParallelLoad class

        private class ExpiringValuesCacheAllowParallelLoad : ExpiringValuesCache<TKey, TValue>
        {
            #region Indexers

            public override TValue this[TKey key]
            {
                get
                {
                    bool expired = false;

                    lock (cache)
                    {
                        if (cache.TryGetValue(key, out ValueHolder result))
                        {
                            if (TimeHelper.GetTimeStamp() <= result.Expiration)
                                return result.Value;
                            expired = true;
                        }
                    }

                    // Here item is either expired or does not exist. We are out of lock so parallel loading is possible.
                    TValue newItem = itemLoader.Invoke(key);

                    lock (cache)
                    {
                        if (cache.TryGetValue(key, out ValueHolder result))
                        {
                            // concurrent load occurred and race is lost: dropping the new value and returning the existing one
                            if (!expired)
                            {
                                if (cache.DisposeDroppedValues && newItem is IDisposable disposableNewItem)
                                    disposableNewItem.Dispose();
                                return result.Value;
                            }

                            // There was an expired old value before the load.
                            // Note: as loading is outside of the lock, it can happen that the originally expired value has been disposed by the cache itself
                            // and TryGetValue returns another instance now but we don't differentiate these cases.
                            if (cache.DisposeDroppedValues && result.Value is IDisposable disposableOldItem)
                                disposableOldItem.Dispose();
                        }

                        // Storing the newly loaded value. If this drops another item, possible dispose is handled by the cache.
                        cache[key] = new ValueHolder
                        {
                            Value = newItem,
                            Expiration = TimeHelper.GetTimeStamp() + expiration
                        };
                    }

                    return newItem;

                }
            }

            #endregion

            #region Constructors

            internal ExpiringValuesCacheAllowParallelLoad(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer, LockingCacheOptions options)
                : base(itemLoader, comparer, options)
            {
            }

            #endregion
        }

        #endregion

        #region ValueHolder struct

        [DebuggerDisplay("{" + nameof(Value) + "}")]
        protected struct ValueHolder : IDisposable
        {
            #region Fields

            internal TValue Value;
            internal long Expiration;

            #endregion

            #region Methods

            public void Dispose() => (Value as IDisposable)?.Dispose();

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private readonly Cache<TKey, ValueHolder> cache;
        private readonly Func<TKey, TValue> itemLoader;
        private readonly long expiration;

        #endregion

        #region Indexers

        public virtual TValue this[TKey key]
        {
            get
            {
                lock (cache)
                {
                    if (cache.TryGetValue(key, out ValueHolder result))
                    {
                        if (TimeHelper.GetTimeStamp() <= result.Expiration)
                            return result.Value;

                        // manual dispose because this value will not be dropped by the cache but will be overwritten explicitly
                        if (cache.DisposeDroppedValues && result.Value is IDisposable disposable)
                            disposable.Dispose();
                    }

                    TValue newItem = itemLoader.Invoke(key);

                    cache[key] = new ValueHolder
                    {
                        Value = newItem,
                        Expiration = TimeHelper.GetTimeStamp() + expiration
                    };

                    return newItem;
                }
            }
        }

        #endregion

        #region Constructors

        protected ExpiringValuesCache(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer, LockingCacheOptions options)
        {
            this.itemLoader = itemLoader;
            expiration = TimeHelper.GetInterval(options.Expiration!.Value);

            cache = new Cache<TKey, ValueHolder>(options.Capacity, comparer)
            {
                EnsureCapacity = options.PreallocateCapacity,
                Behavior = options.Behavior,
                DisposeDroppedValues = options.DisposeDroppedValues
            };
        }

        #endregion

        #region Methods

        internal static IThreadSafeCacheAccessor<TKey, TValue> Create(Func<TKey, TValue> itemLoader, IEqualityComparer<TKey>? comparer, LockingCacheOptions options)
        {
            if (itemLoader == null!)
                Throw.ArgumentNullException(Argument.itemLoader);
            if (options == null!)
                Throw.ArgumentNullException(Argument.options);
            if (options.Expiration == null || options.Expiration < TimeSpan.Zero)
                Throw.ArgumentException(Argument.options, Res.PropertyMustBeGreaterThanOrEqualTo(nameof(options.Expiration), TimeSpan.Zero));
            // other options are validated by the Cache constructor

            return options.ProtectItemLoader
                ? new ExpiringValuesCache<TKey, TValue>(itemLoader, comparer, options)
                : new ExpiringValuesCacheAllowParallelLoad(itemLoader, comparer, options);
        }

        #endregion
    }
}
