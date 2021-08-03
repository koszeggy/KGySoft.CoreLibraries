#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeCacheTest.cs
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
using System.Linq;
#if !NET35
using System.Threading.Tasks; 
#endif

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class ThreadSafeCacheTest
    {
        #region Fields

        private static readonly object[][] usageTestSource =
        {
            new object[] { "Default settings, no comparer", false, null },
            new object[] { "Default settings with comparer", true, null },
            new object[] { "LockFree, immediate merge, no comparer", false, new LockFreeCacheOptions{ MergeInterval = TimeSpan.Zero } },
            new object[] { "LockFree, 1 ms merge, no comparer", false, new LockFreeCacheOptions{ MergeInterval = TimeSpan.FromMilliseconds(1) } },
            new object[] { "LockFree, 100 ms merge, no comparer", false, new LockFreeCacheOptions{ MergeInterval = TimeSpan.FromMilliseconds(100) } },
            new object[] { "Locking, default, no comparer", false, new LockingCacheOptions() },
            new object[] { "Locking, default with comparer", true, new LockingCacheOptions() },
            new object[] { "Locking, 100 ms expiration, no comparer", true, new LockingCacheOptions { Expiration = TimeSpan.FromMilliseconds(100) } },
        };

        #endregion

        #region Methods

        [Test]
        public void GrowOnlyDictionaryTest()
        {
            var dict = new LockFreeCache<int, object>.GrowOnlyDictionary(16, null, true);
            Assert.AreEqual(0, dict.Count);

            // Adding new
            Assert.IsTrue(dict.TryAdd(0, 0));
            Assert.IsTrue(dict.TryGetValue(0, out object value));
            Assert.AreEqual(0, value);
            Assert.AreEqual(1, dict.Count);

            // Cannot add it again
            Assert.IsFalse(dict.TryAdd(0, 0));
            Assert.AreEqual(0, dict[0]);
            Assert.AreEqual(1, dict.Count);

            // And cannot overwrite it with another value
            Assert.IsFalse(dict.TryAdd(0, 1));
            Assert.AreEqual(0, dict[0]);
            Assert.AreEqual(1, dict.Count);

            // But can add another value
            dict[1] = 1;
            Assert.AreEqual(1, dict[1]);
            Assert.AreEqual(2, dict.Count);

            // Adding a linked item in an existing bucket
            dict[16] = 16;
            Assert.AreEqual(16, dict[16]);
            Assert.AreEqual(3, dict.Count);
        }

#if !NET35
        [Test]
        public void GrowOnlyDictionaryParallelUsageTest()
        {
            const int capacity = 1024;
            const int count = 10_000;
            var dict = new LockFreeCache<int, object>.GrowOnlyDictionary(capacity, null, true);
            Parallel.For(0, count, i => Assert.IsTrue(dict.TryAdd(i, i)));

            Assert.AreEqual(count, dict.Count);
            Parallel.For(0, count, i => Assert.AreEqual(i, dict[i]));
        } 
#endif

        [TestCaseSourceGeneric(nameof(usageTestSource), TypeArguments = new[] { typeof(int) })]
        [TestCaseSourceGeneric(nameof(usageTestSource), TypeArguments = new[] { typeof(string) })]
        [TestCaseSourceGeneric(nameof(usageTestSource), TypeArguments = new[] { typeof(ConsoleColor) })]
        public void UsageTest<T>(string testName, bool useComparer, ThreadSafeCacheOptionsBase configuration)
            where T : notnull, IConvertible
        {
            Console.WriteLine($"<{typeof(T).GetName(TypeNameKind.ShortName)}> {testName}");

            static T Key(int i) => i.Convert<T>();

            ThreadSafeDictionary<T, int> triggerCount = new ThreadSafeDictionary<T, int>();

            T ItemLoader(T key)
            {
                triggerCount.AddOrUpdate(key, 1, (k, old) => old + 1);
                return key;
            }

            IThreadSafeCacheAccessor<T, T> cache = ThreadSafeCacheFactory.Create((Func<T, T>)ItemLoader, useComparer ? ComparerHelper<T>.EqualityComparer : null, configuration);
            T key = Key(-1);

            // Before usage:
            Assert.AreEqual(0, triggerCount.Count);
            Assert.IsFalse(triggerCount.ContainsKey(key));

            // First usage triggers loader
            Assert.AreEqual(key, cache[key]);
            Assert.AreEqual(1, triggerCount[key]);

            // Second usage does not trigger loader again
            Assert.AreEqual(key, cache[key]);
            Assert.AreEqual(1, triggerCount[key]);

            configuration ??= LockFreeCacheOptions.DefaultOptions;
            int maxCapacity = configuration is LockingCacheOptions locking ? locking.Capacity
                : configuration is LockFreeCacheOptions lockFree ? lockFree.ThresholdCapacity * 2
                : throw new InvalidOperationException("Unexpected configuration type");

#if NET35
            for (int i = 0; i < maxCapacity; i++)
                Assert.AreEqual(Key(i), cache[Key(i)]);
#else
            // filling the cache concurrently
            Parallel.For(0, maxCapacity, i => Assert.AreEqual(Key(i), cache[Key(i)]));
#endif

            // now key might have been dropped from cache (not necessarily true for lock free cache)
            Assert.AreEqual(key, cache[key]);
            Assert.LessOrEqual(triggerCount[key], 2);

            var occurrences = triggerCount.GroupBy(item => item.Value)
                .Select(g => new { TriggerCount = g.Key, Occurrence = g.Count() })
                .OrderBy(e => e.TriggerCount);
            Console.WriteLine("Trigger Count:\tOccurrence:");
            foreach (var item in occurrences)
                Console.WriteLine($"{item.TriggerCount,-14:N0}\t{item.Occurrence,-11:N0}");
        }

        #endregion
    }
}
