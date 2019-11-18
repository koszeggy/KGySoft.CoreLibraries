#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CachePerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.Globalization;
using System.Linq;
using KGySoft.Collections;
using KGySoft.CoreLibraries.PerformanceTests.Reflection;

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
        #region Constants

        private const int capacity = 10000;
        private const int iterations = 1000000;

        #endregion

        #region Fields

        private static readonly Func<int, string> loader = i => i.ToString();

        #endregion

        #region Methods

        [Test]
        public void CacheOverheadBaselineTest()
        {
            const int count = 10_000;
            Dictionary<int, string> dictionary = Enumerable.Range(0, count).ToDictionary(i => i, i => i.ToString(CultureInfo.InvariantCulture));
            var cacheRemoveOldest = new Cache<int, string>(dictionary) { Behavior = CacheBehavior.RemoveOldestElement };
            var cacheRemoveLeastRecent = new Cache<int, string>(dictionary);

            // Dictionary expected to be the fastest one, Cache with RemoveLeastRecentUsedElement has the most overhead
            new PerformanceTest { TestName = "Indexer access test", Iterations = 10_000 }
                .AddCase(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        string s = dictionary[i];
                    }
                }, "Dictionary read")
                .AddCase(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        string s = cacheRemoveOldest[i];
                    }
                }, "Cache read (RemoveOldestElement)")
                .AddCase(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        string s = cacheRemoveLeastRecent[i];
                    }
                }, "Cache read (RemoveLeastRecentUsedElement)")
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest { TestName = "Populate test", Iterations = 1000 }
                .AddCase(() =>
                {
                    var dict = new Dictionary<int, string>(count);
                    for (int i = 0; i < count; i++)
                        dict[i] = i.ToString(CultureInfo.InvariantCulture);
                }, "Dictionary")
                .AddCase(() =>
                {
                    var dict = new Cache<int, string>(count);
                    for (int i = 0; i < count; i++)
                        dict[i] = i.ToString(CultureInfo.InvariantCulture);
                }, "Cache")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void NeverDropElementsTest()
        {
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
            var cacheRemoveOldest = new Cache<int, string>(loader, capacity) { Behavior = CacheBehavior.RemoveOldestElement };
            var cacheRemoveLeastRecent = new Cache<int, string>(loader, capacity) { Behavior = CacheBehavior.RemoveLeastRecentUsedElement };
            int range = (int)(capacity * 1.5);

            new RandomizedPerformanceTest<string> { Iterations = iterations, TestName = "Using cache with dropping elements" }
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
