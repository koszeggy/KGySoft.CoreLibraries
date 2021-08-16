#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastRandomPerformanceTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class FastRandomPerformanceTest
    {
        #region Methods

        [Test]
        public void NextWithSeedTest()
        {
            const int iterations = 10_000_000;
            var rnd = new Random(0);
            var fastRnd = new FastRandom(0);
            new PerformanceTest<int> { TestName = "Next()", Iterations = iterations }
                .AddCase(() => rnd.Next(), nameof(Random))
                .AddCase(() => fastRnd.Next(), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Next(100)", Iterations = iterations }
                .AddCase(() => rnd.Next(100), nameof(Random))
                .AddCase(() => fastRnd.Next(100), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Next(-100, 100)", Iterations = iterations }
                .AddCase(() => rnd.Next(-100, 100), nameof(Random))
                .AddCase(() => fastRnd.Next(-100, 100), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Next(Int32.MinValue, Int32.MaxValue))", Iterations = iterations }
                .AddCase(() => rnd.Next(Int32.MinValue, Int32.MaxValue), nameof(Random))
                .AddCase(() => fastRnd.Next(Int32.MinValue, Int32.MaxValue), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void NextNoSeedTest()
        {
            const int iterations = 10_000_000;
            var rnd = new Random();
            var fastRnd = new FastRandom();
            new PerformanceTest<int> { TestName = "Next()", Iterations = iterations }
                .AddCase(() => rnd.Next(), nameof(Random))
                .AddCase(() => fastRnd.Next(), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Next(100)", Iterations = iterations }
                .AddCase(() => rnd.Next(100), nameof(Random))
                .AddCase(() => fastRnd.Next(100), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Next(Int32.MaxValue)", Iterations = iterations }
                .AddCase(() => rnd.Next(Int32.MaxValue), nameof(Random))
                .AddCase(() => fastRnd.Next(Int32.MaxValue), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Next(-100, 100)", Iterations = iterations }
                .AddCase(() => rnd.Next(-100, 100), nameof(Random))
                .AddCase(() => fastRnd.Next(-100, 100), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Next(Int32.MinValue, Int32.MaxValue))", Iterations = iterations }
                .AddCase(() => rnd.Next(Int32.MinValue, Int32.MaxValue), nameof(Random))
                .AddCase(() => fastRnd.Next(Int32.MinValue, Int32.MaxValue), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void NextDoubleSeedTest()
        {
            const int iterations = 10_000_000;
            var rnd = new Random(0);
            var fastRnd = new FastRandom(0);

            new PerformanceTest<double> { TestName = "NextDouble()", Iterations = iterations }
                .AddCase(() => rnd.NextDouble(), nameof(Random))
                .AddCase(() => fastRnd.NextDouble(), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void NextDoubleNoSeedTest()
        {
            const int iterations = 10_000_000;
            var rnd = new Random();
            var fastRnd = new FastRandom();

            new PerformanceTest<double> { TestName = "NextDouble()", Iterations = iterations }
                .AddCase(() => rnd.NextDouble(), nameof(Random))
                .AddCase(() => fastRnd.NextDouble(), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void NextBytesSeedTest()
        {
            const int iterations = 1_000_000;
            var rnd = new Random(0);
            var fastRnd = new FastRandom(0);
            var bytes = new byte[100];
            new PerformanceTest { TestName = "NextBytes(byte[100])", Iterations = iterations }
                .AddCase(() => rnd.NextBytes(bytes), nameof(Random))
                .AddCase(() => fastRnd.NextBytes(bytes), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void NextBytesNoSeedTest()
        {
            const int iterations = 1_000_000;
            var rnd = new Random();
            var fastRnd = new FastRandom();
            var bytes = new byte[100];
            new PerformanceTest { TestName = "NextBytes(byte[100])", Iterations = iterations }
                .AddCase(() => rnd.NextBytes(bytes), nameof(Random))
                .AddCase(() => fastRnd.NextBytes(bytes), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }

#if NET6_0_OR_GREATER
        [Test]
        public void NextInt64NoSeedTest()
        {
            const int iterations = 10_000_000;
            var rnd = new Random();
            var fastRnd = new FastRandom();
            new PerformanceTest<long> { TestName = "NextInt64()", Iterations = iterations }
                .AddCase(() => rnd.NextInt64(), nameof(Random))
                .AddCase(() => fastRnd.NextInt64(), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<long> { TestName = "NextInt64(100)", Iterations = iterations }
                .AddCase(() => rnd.NextInt64(100), nameof(Random))
                .AddCase(() => fastRnd.NextInt64(100), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<long> { TestName = "NextInt64(Int64.MaxValue)", Iterations = iterations }
                .AddCase(() => rnd.NextInt64(Int64.MaxValue), nameof(Random))
                .AddCase(() => fastRnd.NextInt64(Int64.MaxValue), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<long> { TestName = "NextInt64(-100, 100)", Iterations = iterations }
                .AddCase(() => rnd.NextInt64(-100, 100), nameof(Random))
                .AddCase(() => fastRnd.NextInt64(-100, 100), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<long> { TestName = "NextInt64(Int64.MinValue, Int64.MaxValue))", Iterations = iterations }
                .AddCase(() => rnd.NextInt64(Int64.MinValue, Int64.MaxValue), nameof(Random))
                .AddCase(() => fastRnd.NextInt64(Int64.MinValue, Int64.MaxValue), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void NextSingleNoSeedTest()
        {
            const int iterations = 10_000_000;
            var rnd = new Random();
            var fastRnd = new FastRandom();

            new PerformanceTest<double> { TestName = "NextSingle()", Iterations = iterations }
                .AddCase(() => rnd.NextSingle(), nameof(Random))
                .AddCase(() => fastRnd.NextSingle(), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }
#endif

        #endregion
    }
}
