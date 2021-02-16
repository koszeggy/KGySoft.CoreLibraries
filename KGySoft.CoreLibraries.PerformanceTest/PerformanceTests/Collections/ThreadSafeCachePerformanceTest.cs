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
using System.Threading;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    [TestFixture]
    public class ThreadSafeCachePerformanceTest
    {
        #region Methods

        #region Static Methods

        static int LoadSlow(int key)
        {
            Thread.Sleep(0);
            return key;
        }

        static int LoadFast(int key) => key;

        #endregion

        #region Instance Methods

        [Test]
        public void GrowTest()
        {
            const int iterations = 10_000;
            const int max = 2048;

            // TODO: lock vs lock free vs concurrent dictionary
            //new PerformanceTest{ TestName = "Fast, Only new items", Repeat = 5, Iterations = 10_000 }
            //    .AddCase(() =>
            //    {
            //        var drop = ThreadSafeCache.Create<int, int>(LoadFast, null, new LockFreeCacheOptions { DropElementsWhileGrowing = true });
            //        for (int i = 0; i < max; i++)
            //        {
            //            var _ = drop[i];
            //        }
            //    }, "drop")
            //    .AddCase(() =>
            //    {
            //        var noDrop = ThreadSafeCache.Create<int, int>(LoadFast, null, new LockFreeCacheOptions { DropElementsWhileGrowing = false });
            //        for (int i = 0; i < max; i++)
            //        {
            //            var _ = noDrop[i];
            //        }
            //    }, "noDrop")
            //    .DoTest()
            //    .DumpResults(Console.Out);

            //new PerformanceTest { TestName = "Slow" }
            //    .AddCase(() =>
            //    {
            //        var drop = ThreadSafeCache.Create<int, int>(LoadSlow, null, new LockFreeCacheOptions { DropElementsWhileGrowing = true });
            //        for (int i = 0; i < max; i++)
            //        {
            //            var _ = drop[i];
            //        }
            //    }, "drop")
            //    .AddCase(() =>
            //    {
            //        var noDrop = ThreadSafeCache.Create<int, int>(LoadSlow, null, new LockFreeCacheOptions { DropElementsWhileGrowing = false });
            //        for (int i = 0; i < max; i++)
            //        {
            //            var _ = noDrop[i];
            //        }
            //    }, "noDrop")
            //    .DoTest()
            //    .DumpResults(Console.Out);

            //new RandomizedPerformanceTest { TestName = "Fast, Random", Repeat = 5 }
            //    .AddCase(rnd =>
            //    {
            //        var drop = ThreadSafeCache.Create<int, int>(LoadFast, null, new LockFreeCacheOptions { DropElementsWhileGrowing = true });
            //        for (int i = 0; i < iterations; i++)
            //        {
            //            var _ = drop[rnd.Next(max)];
            //        }
            //    }, "drop")
            //    .AddCase(rnd =>
            //    {
            //        var noDrop = ThreadSafeCache.Create<int, int>(LoadFast, null, new LockFreeCacheOptions { DropElementsWhileGrowing = false });
            //        for (int i = 0; i < iterations; i++)
            //        {
            //            var _ = noDrop[rnd.Next(max)];
            //        }
            //    }, "noDrop")
            //    .DoTest()
            //    .DumpResults(Console.Out);


            //new RandomizedPerformanceTest { TestName = "Slow, Random", Repeat = 5 }
            //    .AddCase(rnd =>
            //    {
            //        var drop = ThreadSafeCache.Create<int, int>(LoadSlow, null, new LockFreeCacheOptions { DropElementsWhileGrowing = true });
            //        for (int i = 0; i < iterations; i++)
            //        {
            //            var _ = drop[rnd.Next(max)];
            //        }
            //    }, "drop")
            //    .AddCase(rnd =>
            //    {
            //        var noDrop = ThreadSafeCache.Create<int, int>(LoadSlow, null, new LockFreeCacheOptions { DropElementsWhileGrowing = false });
            //        for (int i = 0; i < iterations; i++)
            //        {
            //            var _ = noDrop[rnd.Next(max)];
            //        }
            //    }, "noDrop")
            //    .DoTest()
            //    .DumpResults(Console.Out);

            // TODO: parallel
            throw new NotImplementedException();
        }

        [Test]
        public void AccessExistingValuesTest()
        {
            // TODO: lock vs lock free vs [non-]concurrent dictionary
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
