#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeCachePerformanceTest.cs
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
using System.Collections.Concurrent;
using System.Threading.Tasks;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    [TestFixture]
    public class ThreadSafeCachePerformanceTest
    {
        #region Methods

        [Test]
        public void GrowOnlyTestNoDroppingSequential()
        {
            static int Load(int i) => i;

            const int capacity = 10_000;
            const int count = capacity - 1;

            //var c = new LockFreeCache2<int, int>(Load, null, new LockFreeCacheOptions { InitialL2Capacity = max, MaximumL2Capacity = max, HashingStrategy = HashingStrategy.And });
            //for (int i = 0; i < max - 1; i++)
            //{
            //    var _ = c[i];
            //}
            //return;

            new PerformanceTest { TestName = "Sequential", Iterations = 1_000, Repeat = 5 }
                //.AddCase(() =>
                //{
                //    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { MaximumL2Capacity = capacity });
                //    for (int i = 0; i < count; i++)
                //    {
                //        var _ = cache[i];
                //    }
                //}, $"LockFree, Max = {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { InitialL2Capacity = capacity, MaximumL2Capacity = capacity });
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, $"LockFree, Min = Max = {capacity:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { InitialL2Capacity = capacity, MaximumL2Capacity = capacity, HashingStrategy = HashingStrategy.And });
                    for (int i = 0; i < count; i++)
                    {
                        var _ = cache[i];
                    }
                }, $"LockFree, Min = Max = {capacity:N0}, AND hash")
                //.AddCase(() =>
                //{
                //    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity });
                //    for (int i = 0; i < count; i++)
                //    {
                //        var _ = cache[i];
                //    }
                //}, $"Locking, Capacity = {max:N0}")
                //.AddCase(() =>
                //{
                //    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, PreallocateCapacity = true });
                //    for (int i = 0; i < count; i++)
                //    {
                //        var _ = cache[i];
                //    }
                //}, $"Locking, Capacity = {max:N0}, Preallocated")
                //.AddCase(() =>
                //{
                //    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = capacity, PreallocateCapacity = true, Behavior = CacheBehavior.RemoveOldestElement });
                //    for (int i = 0; i < count; i++)
                //    {
                //        var _ = cache[i];
                //    }
                //}, $"Locking, Capacity = {max:N0}, Preallocated, RemoveOldest")
#if !NET35
                //.AddCase(() =>
                //{
                //    var dict = new ConcurrentDictionary<int, int>();
                //    for (int i = 0; i < count; i++)
                //    {
                //        var _ = dict.GetOrAdd(i, Load);
                //    }
                //}, "ConcurrentDictionary")
#endif
                //.AddCase(() =>
                //{
                //    var dict = new ThreadSafeDictionary<int, int>();
                //    for (int i = 0; i < count; i++)
                //    {
                //        var _ = dict.GetOrAdd(i, Load);
                //    }
                //}, "ConcurrentDictionary")
                .DoTest()
                .DumpResults(Console.Out);
        }

#if !NET35
        [Test]
        public void GrowOnlyTestNoDroppingParallel()
        {
            static int Load(int i) => i;

            const int max = 10_000;

            new PerformanceTest { TestName = "Parallel", Iterations = 1_000, CpuAffinity = null }
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { MaximumL2Capacity = max });
                    Parallel.For(0, max, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"LockFree, Max = {max:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { InitialL2Capacity = max, MaximumL2Capacity = max });
                    Parallel.For(0, max, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"LockFree, Min = Max = {max:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockFreeCacheOptions { InitialL2Capacity = max, MaximumL2Capacity = max, HashingStrategy = HashingStrategy.And });
                    Parallel.For(0, max, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"LockFree, Min = Max = {max:N0}, AND hash")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = max });
                    Parallel.For(0, max, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"Locking, Capacity = {max:N0}")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = max, PreallocateCapacity = true });
                    Parallel.For(0, max, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"Locking, Capacity = {max:N0}, Preallocated")
                .AddCase(() =>
                {
                    var cache = ThreadSafeCacheFactory.Create<int, int>(Load, new LockingCacheOptions { Capacity = max, PreallocateCapacity = true, Behavior = CacheBehavior.RemoveOldestElement });
                    Parallel.For(0, max, i =>
                    {
                        var _ = cache[i];
                    });
                }, $"Locking, Capacity = {max:N0}, Preallocated, RemoveOldest")
#if !NET35
                .AddCase(() =>
                {
                    var dict = new ConcurrentDictionary<int, int>();
                    for (int i = 0; i < max; i++)
                    {
                        var _ = dict.GetOrAdd(i, Load);
                    }
                }, "ConcurrentDictionary")
#endif
                //.AddCase(() =>
                //{
                //    var dict = new ThreadSafeDictionary<int, int>();
                //    for (int i = 0; i < max; i++)
                //    {
                //        var _ = dict.GetOrAdd(i, Load);
                //    }
                //}, "ConcurrentDictionary")
                .DoTest()
                .DumpResults(Console.Out);
        } 
#endif

        [Test]
        public void AccessExistingValuesTest()
        {
            // TODO: lock vs lock free vs [non-]concurrent dictionary
            throw new NotImplementedException();
        }

        #endregion
    }
}
