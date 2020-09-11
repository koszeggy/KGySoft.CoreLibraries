#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
using KGySoft.Security.Cryptography;
#if !NET35
using System.Threading.Tasks;
#endif

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class RandomPerformanceTest
    {
        #region Methods

        [Test]
        public void FastRandomTest()
        {
            throw new NotImplementedException();
            // TODO: Next overloads, NextDouble, NextBytes
        }

        [Test]
        public void ThreadSafeRandomTest()
        {
            const int iterations = 1_000_000;
            var rnd = new Random();
            using var trnd = new ThreadSafeRandom();
            using var trndSeed = new ThreadSafeRandom(0);
            using var trndStatic = ThreadSafeRandom.Instance;
            using var trndWrappedFast = new ThreadSafeRandom(() => new FastRandom());
            using var trndWrappedSecure = new ThreadSafeRandom(() => new SecureRandom());
            using var trndWrappedThreadSafe = new ThreadSafeRandom(() => new ThreadSafeRandom());
            using var trndWrappedRandom = new ThreadSafeRandom(() => new Random());
            var fast = new FastRandom(0);
            using var secure = new SecureRandom();

            new PerformanceTest<int> { TestName = "Non-parallel", Iterations = iterations }
                .AddCase(() => rnd.Next(), "Random")
                .AddCase(() => trnd.Next(), "ThreadSafeRandom()")
                .AddCase(() => trndSeed.Next(), "ThreadSafeRandom(0)")
                .AddCase(() => trndStatic.Next(), "ThreadSafeRandom.Instance")
                .AddCase(() => trndWrappedRandom.Next(), "ThreadSafeRandom(Random)")
                .AddCase(() => trndWrappedFast.Next(), "ThreadSafeRandom(FastRandom)")
                .AddCase(() => trndWrappedSecure.Next(), "ThreadSafeRandom(SecureRandom)")
                .AddCase(() => trndWrappedThreadSafe.Next(), "ThreadSafeRandom(ThreadSafeRandom)")
                .AddCase(() => fast.Next(), "FastRandom")
                .AddCase(() => secure.Next(), "SecureRandom")
                .DoTest()
                .DumpResults(Console.Out);

#if !NET35
            new PerformanceTest { TestName = "Parallel", CpuAffinity = null, Iterations = 1 }
                .AddCase(() => Parallel.For(0, iterations, i => trnd.Next()), "ThreadSafeRandom()")
                .AddCase(() => Parallel.For(0, iterations, i => trndSeed.Next()), "ThreadSafeRandom(0)")
                .AddCase(() => Parallel.For(0, iterations, i => trndStatic.Next()), "ThreadSafeRandom.Instance")
                .AddCase(() => Parallel.For(0, iterations, i => trndWrappedRandom.Next()), "ThreadSafeRandom(Random)")
                .AddCase(() => Parallel.For(0, iterations, i => trndWrappedFast.Next()), "ThreadSafeRandom(FastRandom)")
                .AddCase(() => Parallel.For(0, iterations, i => trndWrappedSecure.Next()), "ThreadSafeRandom(SecureRandom)")
                .AddCase(() => Parallel.For(0, iterations, i => trndWrappedThreadSafe.Next()), "ThreadSafeRandom(ThreadSafeRandom)")
                .DoTest()
                .DumpResults(Console.Out);
#endif
        }

        #endregion
    }
}
