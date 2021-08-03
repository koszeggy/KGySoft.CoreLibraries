#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CachePerformanceTest.cs
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
using System.Globalization;
using System.Linq;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    /// <summary>
    /// In this test out-of-cache retrieval is fast to demonstrate that <see cref="CacheBehavior.RemoveLeastRecentUsedElement"/>
    /// behavior is just a little bit slower than <see cref="CacheBehavior.RemoveOldestElement"/>.
    /// See <see cref="ReflectorPerformanceTest.TestMethodInvoke"/> to test a simulation of re-using the most recent elements
    /// </summary>
    [TestFixture]
    public class CachePerformanceTest
    {
        #region Fields

        private static readonly Func<int, string> loader = i => i.ToString();

        #endregion

        #region Methods

        [Test]
        public void CacheOverheadBaselineIntKeyTest()
        {
            const int count = 100_000;
            Dictionary<int, string> dictionary = Enumerable.Range(0, count).ToDictionary(i => i, i => i.ToString(CultureInfo.InvariantCulture));
            var cacheRemoveOldest = new Cache<int, string>(dictionary) { Behavior = CacheBehavior.RemoveOldestElement };
            var cacheRemoveLeastRecent = new Cache<int, string>(dictionary);

            // Dictionary expected to be the fastest one, Cache with RemoveLeastRecentUsedElement has the most overhead
            new IteratorPerformanceTest<string> { TestName = "Indexer access test", Iterations = count }
                .AddCase(i => dictionary[i], "Dictionary read")
                .AddCase(i => cacheRemoveOldest[i], "Cache read (RemoveOldestElement)")
                .AddCase(i => cacheRemoveLeastRecent[i], "Cache read (RemoveLeastRecentUsedElement)")
                .DoTest()
                .DumpResults(Console.Out);

            var dict = new Dictionary<int, string>(count);
            var cache = new Cache<int, string>(count);
            new IteratorPerformanceTest { TestName = "Populate test", Iterations = count }
                .AddCase(i => dict[i] = i.ToString(CultureInfo.InvariantCulture), "Dictionary")
                .AddCase(i => cache[i] = i.ToString(CultureInfo.InvariantCulture), "Cache")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void CacheOverheadBaselineStringKeyTest()
        {
            const int count = 100_000;
            Dictionary<string, int> dictionary = Enumerable.Range(0, count).ToDictionary(i => i.ToString(CultureInfo.InvariantCulture));
            var cacheRemoveOldest = new Cache<string, int>(dictionary) { Behavior = CacheBehavior.RemoveOldestElement };
            var cacheRemoveLeastRecent = new Cache<string, int>(dictionary);

            // Dictionary expected to be the fastest one, Cache with RemoveLeastRecentUsedElement has the most overhead
            new IteratorPerformanceTest<int> { TestName = "Indexer access test", Iterations = count }
                .AddCase(i => dictionary[i.ToString(CultureInfo.InvariantCulture)], "Dictionary read")
                .AddCase(i => cacheRemoveOldest[i.ToString(CultureInfo.InvariantCulture)], "Cache read (RemoveOldestElement)")
                .AddCase(i => cacheRemoveLeastRecent[i.ToString(CultureInfo.InvariantCulture)], "Cache read (RemoveLeastRecentUsedElement)")
                .DoTest()
                .DumpResults(Console.Out);

            var dict = new Dictionary<string, int>(count);
            var cache = new Cache<string, int>(count);
            new IteratorPerformanceTest { TestName = "Populate test", Iterations = count }
                .AddCase(i => dict[i.ToString(CultureInfo.InvariantCulture)] = i, "Dictionary")
                .AddCase(i => cache[i.ToString(CultureInfo.InvariantCulture)] = i, "Cache")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void NeverDropElementsTest()
        {
            const int capacity = 10_000;
            const int iterations = 1_000_000;

            var cacheRemoveOldest = new Cache<int, string>(loader, capacity) { Behavior = CacheBehavior.RemoveOldestElement };
            var cacheRemoveLeastRecent = new Cache<int, string>(loader, capacity) { Behavior = CacheBehavior.RemoveLeastRecentUsedElement };

            new RandomizedPerformanceTest<string> { Iterations = iterations, TestName = "Using cache without dropping elements" }
                .AddCase(rnd => cacheRemoveOldest[rnd.Next(capacity)], $"{nameof(cacheRemoveOldest.Behavior)} = {cacheRemoveOldest.Behavior}")
                .AddCase(rnd => cacheRemoveLeastRecent[rnd.Next(capacity)], $"{nameof(cacheRemoveLeastRecent.Behavior)} = {cacheRemoveLeastRecent.Behavior}")
                .DoTest()
                .DumpResults(Console.Out);

            Console.WriteLine($"=========={cacheRemoveOldest.Behavior} Statistics===========");
            Console.WriteLine(cacheRemoveOldest.GetStatistics());
            Console.WriteLine($"=========={cacheRemoveLeastRecent.Behavior} Statistics===========");
            Console.WriteLine(cacheRemoveLeastRecent.GetStatistics());
        }

        [Test]
        public void DropElementsTest()
        {
            const int capacity = 10_000;
            const int iterations = 1_000_000;

            var cacheRemoveOldest = new Cache<int, string>(loader, capacity) { Behavior = CacheBehavior.RemoveOldestElement };
            var cacheRemoveLeastRecent = new Cache<int, string>(loader, capacity) { Behavior = CacheBehavior.RemoveLeastRecentUsedElement };
            int range = (int)(capacity * 1.5);

            new RandomizedPerformanceTest<string> { Iterations = iterations, TestName = "Using cache with dropping elements, random access" }
                .AddCase(rnd => cacheRemoveOldest[rnd.Next(range)], $"{nameof(cacheRemoveOldest.Behavior)} = {cacheRemoveOldest.Behavior}")
                .AddCase(rnd => cacheRemoveLeastRecent[rnd.Next(range)], $"{nameof(cacheRemoveLeastRecent.Behavior)} = {cacheRemoveLeastRecent.Behavior}")
                .DoTest()
                .DumpResults(Console.Out);

            Console.WriteLine($"=========={cacheRemoveOldest.Behavior} Statistics===========");
            Console.WriteLine(cacheRemoveOldest.GetStatistics());
            Console.WriteLine($"=========={cacheRemoveLeastRecent.Behavior} Statistics===========");
            Console.WriteLine(cacheRemoveLeastRecent.GetStatistics());
        }

        #endregion
    }
}
