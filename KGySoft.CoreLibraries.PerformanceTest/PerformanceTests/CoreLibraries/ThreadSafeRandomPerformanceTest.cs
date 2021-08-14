#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeRandomPerformanceTest.cs
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
using System.Threading.Tasks;
#endif

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class ThreadSafeRandomPerformanceTest
    {
        private class LockingRandom : Random
        {
            #region Fields

            private readonly object syncRoot = new object();

            #endregion

            #region Methods

            public override int Next()
            {
                lock (syncRoot)
                    return base.Next();
            }

            #endregion

        }

        #region Methods

        [Test]
        public void ThreadSafeRandomSequentialTest()
        {
            const int iterations = 10_000_000;
            var rnd = new Random();
            var fast = new FastRandom();
            var lrnd = new LockingRandom();
#if NET6_0_OR_GREATER
            var shared = Random.Shared; 
#endif
            using var trnd = ThreadSafeRandom.Instance;
            using var trndSeed = ThreadSafeRandom.Create(0);
            using var trndWrappedFast = ThreadSafeRandom.Create(() => new FastRandom());
            using var trndWrappedRandom = ThreadSafeRandom.Create(() => new Random());

            new PerformanceTest<int> { TestName = "Non-parallel", Iterations = iterations/*, Repeat = 5*/ }
                .AddCase(() => rnd.Next(), "Random")
#if NET6_0_OR_GREATER
                .AddCase(() => shared.Next(), "Random.Shared")
#endif
                .AddCase(() => fast.Next(), "FastRandom")
                .AddCase(() => lrnd.Next(), "LockingRandom")
                .AddCase(() => trnd.Next(), "ThreadSafeRandom.Instance")
                .AddCase(() => trndSeed.Next(), "ThreadSafeRandom.Create(0)")
                .AddCase(() => trndWrappedRandom.Next(), "ThreadSafeRandom.Create(Random)")
                .AddCase(() => trndWrappedFast.Next(), "ThreadSafeRandom.Create(FastRandom)")
                .DoTest()
                .DumpResults(Console.Out);
        }

#if !NET35
        [Test]
        public void ThreadSafeRandomParallelTest()
        {
            const int iterations = 10_000_000;
            var lrnd = new LockingRandom();
            using var trnd = ThreadSafeRandom.Instance;
            using var trndSeed = ThreadSafeRandom.Create(0);
            using var trndWrappedFast = ThreadSafeRandom.Create(() => new FastRandom());
            using var trndWrappedRandom = ThreadSafeRandom.Create(() => new Random());
#if NET6_0_OR_GREATER
            var shared = Random.Shared;
#endif

            new PerformanceTest { TestName = "Parallel", CpuAffinity = null, Iterations = 1 }
#if NET6_0_OR_GREATER
                .AddCase(() => Parallel.For(0, iterations, i => shared.Next()), "Random.Shared") 
#endif
                .AddCase(() => Parallel.For(0, iterations, i => lrnd.Next()), "LockingRandom")
                .AddCase(() => Parallel.For(0, iterations, i => trnd.Next()), "ThreadSafeRandom.Instance")
                .AddCase(() => Parallel.For(0, iterations, i => trndSeed.Next()), "ThreadSafeRandom.Create(0)")
                .AddCase(() => Parallel.For(0, iterations, i => trndWrappedRandom.Next()), "ThreadSafeRandom.Create(Random)")
                .AddCase(() => Parallel.For(0, iterations, i => trndWrappedFast.Next()), "ThreadSafeRandom.Create(FastRandom)")
                .DoTest()
                .DumpResults(Console.Out);
        }
#endif

        #endregion
    }
}
