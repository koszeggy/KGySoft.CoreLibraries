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
        public void ThreadSafeRandomTest()
        {
            const int iterations = 1_000_000;
            var rnd = new Random();
            using var trnd = new ThreadSafeRandom();
            using var trndSeed = new ThreadSafeRandom(0);
            using var trndStatic = ThreadSafeRandom.Instance;
            new PerformanceTest<int> { TestName = "Non-parallel", Iterations = iterations }
                .AddCase(() => rnd.Next(), "Random")
                .AddCase(() => trnd.Next(), "ThreadSafeRandom()")
                .AddCase(() => trndSeed.Next(), "ThreadSafeRandom(0)")
                .AddCase(() => trndStatic.Next(), "ThreadSafeRandom.Instance")
                .DoTest()
                .DumpResults(Console.Out);

#if !NET35
            new PerformanceTest { TestName = "Parallel", CpuAffinity = null, Iterations = 1 }
                .AddCase(() => Parallel.For(0, iterations, i => trnd.Next()), "ThreadSafeRandom()")
                .AddCase(() => Parallel.For(0, iterations, i => trndSeed.Next()), "ThreadSafeRandom(0)")
                .AddCase(() => Parallel.For(0, iterations, i => trndStatic.Next()), "ThreadSafeRandom.Instance")
                .DoTest()
                .DumpResults(Console.Out);
#endif
        }

        #endregion
    }
}
