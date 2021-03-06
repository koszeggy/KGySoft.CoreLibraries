﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastRandomPerformanceTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class FastRandomPerformanceTest
    {
        #region Methods

        [Test]
        public void NextTest()
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
        public void NextDoubleTest()
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
        public void NextBytesTest()
        {
            const int iterations = 1_000_000;
            var rnd = new Random(0);
            var fastRnd = new FastRandom(0);
            var bytes = new byte[100];
            new PerformanceTest { TestName = "NextBytes(byte[1000])", Iterations = iterations }
                .AddCase(() => rnd.NextBytes(bytes), nameof(Random))
                .AddCase(() => fastRnd.NextBytes(bytes), nameof(FastRandom))
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}
