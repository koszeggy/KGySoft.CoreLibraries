#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeCachePerformanceTest.cs
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
#if !NET35
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
#endif

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    [TestFixture]
    public class ThreadSafeCachePerformanceTest
    {
        #region Nested Classes
#if !NET35

        private class ConcurrentDictionaryBasedCache<TKey, TValue> : IThreadSafeCacheAccessor<TKey, TValue>
            where TKey : notnull
        {
            #region Fields

            private readonly Func<TKey, TValue> itemLoader;
            private readonly int capacity;
            private readonly ConcurrentDictionary<TKey, TValue> dict;
            private readonly ConcurrentQueue<TKey> order;

            private int count;

            #endregion

            #region Indexers

            public TValue this[TKey key]
            {
                get
                {
                    // item found
                    if (dict.TryGetValue(key, out TValue result))
                        return result;

                    // not present, trying to load and add
                    result = itemLoader.Invoke(key);
                    if (!dict.TryAdd(key, result))
                    {
                        // lost race: overwriting and returning
                        dict[key] = result;
                        return result;
                    }

                    // NOTE: we could just check dict.Count and remove FirstOrDefault but Count is terribly slow for ConcurrentDictionary

                    // here result added as a new item
                    order.Enqueue(key);
                    int newCount = Interlocked.Increment(ref count);

                    if (newCount <= capacity)
                        return result;

                    // cache is full: dropping the oldest item
                    if (order.TryDequeue(out TKey toRemove))
                    {
                        dict.TryRemove(toRemove, out var _);
                        Interlocked.Decrement(ref count);
                    }

                    return result;
                }
            }

            #endregion

            #region Constructors

            internal ConcurrentDictionaryBasedCache(Func<TKey, TValue> itemLoader, int capacity)
            {
                this.itemLoader = itemLoader;
                this.capacity = capacity;
                dict = new ConcurrentDictionary<TKey, TValue>(Environment.ProcessorCount, capacity);
                order = new ConcurrentQueue<TKey>();
            }

            #endregion
        }

#endif
        #endregion

        #region Methods

        [Test]
        public void PopulateTestSequential()
        {
            static int Load(int i) => i;

            const int capacity = 10_000;
            const int count = capacity - 1;

            new PerformanceTest { TestName = "Sequential Populate", Iterations = 1_000 }
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity });
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, $"LockFree, Doubling until {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { InitialCapacity = capacity, ThresholdCapacity = capacity });
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, $"LockFree, Preallocating {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { InitialCapacity = capacity, ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.And });
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, $"LockFree, Preallocating {capacity:N0}, AND hash")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity });
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, $"Locking, Doubling until {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, PreallocateCapacity = true });
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, $"Locking, Preallocating {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, PreallocateCapacity = true, Behavior = CacheBehavior.RemoveOldestElement });
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, $"Locking, Preallocating {capacity:N0}, RemoveOldest")
#if !NET35
                .AddCase(() =>
                {
                    var dict = new ConcurrentDictionary<int, int>();
                    for (int i = 0; i < count; i++)
                    {
                        var _ = dict.GetOrAdd(i, Load);
                    }
                }, "ConcurrentDictionary with doubling")
                .AddCase(() =>
                {
                    var dict = new ConcurrentDictionary<int, int>(Environment.ProcessorCount, capacity);
                    for (int i = 0; i < count; i++)
                    {
                        var _ = dict.GetOrAdd(i, Load);
                    }
                }, $"ConcurrentDictionary, Preallocating {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = new ConcurrentDictionaryBasedCache<int, int>(Load, capacity);
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, "ConcurrentDictionaryBasedCache")
#endif
                .DoTest()
                .DumpResults(Console.Out);
        }

#if !NET35
        [Test]
        public void PopulateTestParallel()
        {
            static int Load(int i) => i;

            const int capacity = 10_000;
            const int count = capacity - 1;

            new PerformanceTest { TestName = "Parallel Populate", Iterations = 1_000, CpuAffinity = null }
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity });
                    Parallel.For(0, count, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"LockFree, Doubling until {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { InitialCapacity = capacity, ThresholdCapacity = capacity });
                    Parallel.For(0, count, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"LockFree, Preallocating {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { InitialCapacity = capacity, ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.And });
                    Parallel.For(0, count, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"LockFree, Preallocating {capacity:N0}, AND hash")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity });
                    Parallel.For(0, count, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"Locking, Doubling until {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, PreallocateCapacity = true });
                    Parallel.For(0, count, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"Locking, Preallocating {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, PreallocateCapacity = true, Behavior = CacheBehavior.RemoveOldestElement });
                    Parallel.For(0, count, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"Locking, Preallocating {capacity:N0}, RemoveOldest")
#if !NET35
                .AddCase(() =>
                {
                    var dict = new ConcurrentDictionary<int, int>();
                    Parallel.For(0, count, i =>
                    {
                        var _ = dict.GetOrAdd(i, Load);
                    });
                }, "ConcurrentDictionary with doubling")
                .AddCase(() =>
                {
                    var dict = new ConcurrentDictionary<int, int>(Environment.ProcessorCount, capacity);
                    Parallel.For(0, count, i =>
                    {
                        var _ = dict.GetOrAdd(i, Load);
                    });
                }, $"ConcurrentDictionary, Preallocating {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = new ConcurrentDictionaryBasedCache<int, int>(Load, capacity);
                    Parallel.For(0, count, i =>
                    {
                        var _ = cache[i];
                    });
                }, "ConcurrentDictionaryBasedCache")
#endif
                .DoTest()
                .DumpResults(Console.Out);
        }
#endif

        [Test]
        public void ExistingValuesAccessSequential()
        {
            static int Load(int i) => i;

            const int capacity = 10_000;

            var lockFreeMod = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.Modulo });
            var lockFreeAnd = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.And });
            var lockingOldest = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, Behavior = CacheBehavior.RemoveOldestElement });
            var lockingLeastUsed = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, Behavior = CacheBehavior.RemoveLeastRecentUsedElement });
#if !NET35
            var cDict = new ConcurrentDictionaryBasedCache<int, int>(Load, capacity);
#endif

            // populating once
            for (int i = 0; i < capacity; i++)
            {
                var _ = lockFreeMod[i];
                _ = lockFreeAnd[i];
                _ = lockingOldest[i];
                _ = lockingLeastUsed[i];
#if !NET35
                _ = cDict[i];
#endif
            }

            new PerformanceTest { TestName = "Sequential Access", Iterations = 10_000 }
                .AddCase(() =>
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        var _ = lockFreeMod[i];
                    }
                }, "LockFree, MOD hashing")
                .AddCase(() =>
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        var _ = lockFreeAnd[i];
                    }
                }, "LockFree, AND hashing")
                .AddCase(() =>
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        var _ = lockingOldest[i];
                    }
                }, "Locking, Remove Oldest")
                .AddCase(() =>
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        var _ = lockingLeastUsed[i];
                    }
                }, "Locking, Least Recent Used")
#if !NET35
                .AddCase(() =>
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        var _ = cDict[i];
                    }
                }, "ConcurrentDictionaryBasedCache")
#endif
                .DoTest()
                .DumpResults(Console.Out);
        }

#if !NET35
        [Test]
        public void ExistingValuesAccessParallel()
        {
            static int Load(int i) => i;

            const int capacity = 10_000;

            var lockFreeMod = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.Modulo });
            var lockFreeAnd = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.And });
            var lockingOldest = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, Behavior = CacheBehavior.RemoveOldestElement });
            var lockingLeastUsed = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, Behavior = CacheBehavior.RemoveLeastRecentUsedElement });
            var cDict = new ConcurrentDictionaryBasedCache<int, int>(Load, capacity);

            // populating once
            for (int i = 0; i < capacity; i++)
            {
                var _ = lockFreeMod[i];
                _ = lockFreeAnd[i];
                _ = lockingOldest[i];
                _ = lockingLeastUsed[i];
                _ = cDict[i];
            }

            new PerformanceTest { TestName = "Parallel Access", Iterations = 10_000, CpuAffinity = null }
                .AddCase(() =>
                {
                    Parallel.For(0, capacity, i =>
                    {
                        var _ = lockFreeMod[i];
                    });
                }, "LockFree, MOD hashing")
                .AddCase(() =>
                {
                    Parallel.For(0, capacity, i =>
                    {
                        var _ = lockFreeAnd[i];
                    });
                }, "LockFree, AND hashing")
                .AddCase(() =>
                {
                    Parallel.For(0, capacity, i =>
                    {
                        var _ = lockingOldest[i];
                    });
                }, "Locking, Remove Oldest")
                .AddCase(() =>
                {
                    Parallel.For(0, capacity, i =>
                    {
                        var _ = lockingLeastUsed[i];
                    });
                }, "Locking, Least Recent Used")
                .AddCase(() =>
                {
                    Parallel.For(0, capacity, i =>
                    {
                        var _ = cDict[i];
                    });
                }, "ConcurrentDictionaryBasedCache")
                .DoTest()
                .DumpResults(Console.Out);
        }
#endif

        [Test]
        public void RandomAccessWithDropTest()
        {
            static int Load(int i) => i;

            const int capacity = 1_000;
            const int range = capacity * 4;

            var lockFreeMod = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.Modulo });
            var lockFreeAnd = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.And });
            var lockingOldest = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, Behavior = CacheBehavior.RemoveOldestElement });
            var lockingLeastUsed = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, Behavior = CacheBehavior.RemoveLeastRecentUsedElement });
#if !NET35
            var cDict = new ConcurrentDictionaryBasedCache<int, int>(Load, capacity);
#endif

            new RandomizedPerformanceTest<int> { TestName = "Sequential Access", Iterations = 10_000_000 }
                .AddCase(rnd => lockFreeMod[rnd.Next(range)], "LockFree, MOD hashing")
                .AddCase(rnd => lockFreeAnd[rnd.Next(range)], "LockFree, AND hashing")
                .AddCase(rnd => lockingOldest[rnd.Next(range)], "Locking, Remove Oldest")
                .AddCase(rnd => lockingLeastUsed[rnd.Next(range)], "Locking, Least Recent Used")
#if !NET35
                .AddCase(rnd => cDict[rnd.Next(range)], "ConcurrentDictionaryBasedCache")
#endif
                .DoTest()
                .DumpResults(Console.Out);
        }

#if !NET35
        [Test]
        public void RandomAccessWithDropTestParallel()
        {
            static int Load(int i) => i;

            const int capacity = 1_000;
            const int range = capacity * 4;
            const int iterations = 10_000_000;

            var lockFreeMod = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.Modulo });
            var lockFreeAnd = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { ThresholdCapacity = capacity, HashingStrategy = HashingStrategy.And });
            var lockingOldest = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, Behavior = CacheBehavior.RemoveOldestElement });
            var lockingLeastUsed = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, Behavior = CacheBehavior.RemoveLeastRecentUsedElement });
            var cDict = new ConcurrentDictionaryBasedCache<int, int>(Load, capacity);

            var rnd = ThreadSafeRandom.Instance;

            new PerformanceTest { TestName = "Parallel Access", Iterations = 1, CpuAffinity = null }
                .AddCase(() =>
                {
                    Parallel.For(0, iterations, i =>
                    {
                        var _ = lockFreeMod[rnd.Next(range)];
                    });
                }, "LockFree, MOD hashing")
                .AddCase(() =>
                {
                    Parallel.For(0, iterations, i =>
                    {
                        var _ = lockFreeAnd[rnd.Next(range)];
                    });
                }, "LockFree, AND hashing")
                .AddCase(() =>
                {
                    Parallel.For(0, iterations, i =>
                    {
                        var _ = lockingOldest[rnd.Next(range)];
                    });
                }, "Locking, Remove Oldest")
                .AddCase(() =>
                {
                    Parallel.For(0, iterations, i =>
                    {
                        var _ = lockingLeastUsed[rnd.Next(range)];
                    });
                }, "Locking, Least Recent Used")
                .AddCase(() =>
                {
                    Parallel.For(0, iterations, i =>
                    {
                        var _ = cDict[rnd.Next(range)];
                    });
                }, "ConcurrentDictionaryBasedCache")
                .DoTest()
                .DumpResults(Console.Out);
        }
#endif

        #endregion
    }
}
