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
        public void NeverDropElementsTest()
        {
            var cacheRemoveOldest = new Cache<int, string>(loader, capacity) { Behavior = CacheBehavior.RemoveOldestElement };
            var cacheRemoveLeastRecent = new Cache<int, string>(loader, capacity) { Behavior = CacheBehavior.RemoveLeastRecentUsedElement };
            var rndRemoveOldest = new Random(0);
            var rndRemoveLeastRecent = new Random(0);

            new PerformanceTest<string> { Iterations = iterations, TestName = "Using cache without dropping elements" }
                .AddCase(() => cacheRemoveOldest[rndRemoveOldest.Next(capacity)], $"{nameof(cacheRemoveOldest.Behavior)} = {cacheRemoveOldest.Behavior}")
                .AddCase(() => cacheRemoveLeastRecent[rndRemoveLeastRecent.Next(capacity)], $"{nameof(cacheRemoveLeastRecent.Behavior)} = {cacheRemoveLeastRecent.Behavior}")
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
            var rndRemoveOldest = new Random(0);
            var rndRemoveLeastRecent = new Random(0);
            int range = (int)(capacity * 1.5);

            new PerformanceTest<string> { Iterations = iterations, TestName = "Using cache with dropping elements" }
                .AddCase(() => cacheRemoveOldest[rndRemoveOldest.Next(range)], $"{nameof(cacheRemoveOldest.Behavior)} = {cacheRemoveOldest.Behavior}")
                .AddCase(() => cacheRemoveLeastRecent[rndRemoveLeastRecent.Next(range)], $"{nameof(cacheRemoveLeastRecent.Behavior)} = {cacheRemoveLeastRecent.Behavior}")
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
