#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastRandomPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
#if NET11_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
#endif

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
                .AddCase(rnd.Next, nameof(Random))
                .AddCase(fastRnd.Next, nameof(FastRandom))
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
                .AddCase(rnd.Next, nameof(Random))
                .AddCase(fastRnd.Next, nameof(FastRandom))
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
                .AddCase(rnd.NextDouble, nameof(Random))
                .AddCase(fastRnd.NextDouble, nameof(FastRandom))
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
                .AddCase(rnd.NextDouble, nameof(Random))
                .AddCase(fastRnd.NextDouble, nameof(FastRandom))
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
                .AddCase(rnd.NextInt64, nameof(Random))
                .AddCase(fastRnd.NextInt64, nameof(FastRandom))
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

#if NET11_0_OR_GREATER
        [Test]
        public void NextBinaryFloatTest()
        {
            static void WriteStats<T>(List<T> values, string name) where T : INumber<T>
            {
                T min = values.Min();
                T max = values.Max();
                Console.WriteLine($"{name}: {min}..{max} ({values.Distinct().Count()} values); Avg: {values.Average(Double.CreateTruncating)}; MinCount: {values.Count(v => v == min)}; MaxCount: {values.Count(v => v == max)}");
            }

            const int iterations = 100_000_000;
            var rnd = new Random();

            // Half
            var values = new List<Half>(1 << 16);
            for (int i = 0; i < iterations; i++)
                values.Add(rnd.NextHalf());
            WriteStats(values, "NextHalf");

            values.Clear();
            for (int i = 0; i < iterations; i++)
                values.Add(rnd.NextBinaryFloat<Half>());
            WriteStats(values, "NextBinaryFloat<Half>");

            new PerformanceTest<Half> { TestName = "Half", Iterations = iterations }
                .AddCase(rnd.NextHalf, "NextHalf")
                .AddCase(rnd.NextBinaryFloat<Half>, "NextBinaryFloat<Half>")
                .DoTest()
                .DumpResults(Console.Out);

            // BFloat16
            var valuesBf = new List<BFloat16>(1 << 16);
            for (int i = 0; i < iterations; i++)
                valuesBf.Add(rnd.NextBFloat16());
            WriteStats(valuesBf, "NextBFloat16");

            valuesBf.Clear();
            for (int i = 0; i < iterations; i++)
                valuesBf.Add(rnd.NextBinaryFloat<BFloat16>());
            WriteStats(valuesBf, "NextBinaryFloat<BFloat16>");

            new PerformanceTest<BFloat16> { TestName = "BFloat16", Iterations = iterations }
                .AddCase(rnd.NextBFloat16, "NextBFloat16")
                .AddCase(rnd.NextBinaryFloat<BFloat16>, "NextBinaryFloat<BFloat16>")
                .DoTest()
                .DumpResults(Console.Out);
        }
#endif

        [Test]
        public void FloatingPointSampleComparisonTest()
        {
            var rnd = new FastRandom();
            new PerformanceTest { TestName = "Floating point sample comparison" }
                .AddCase(() => rnd.NextDouble(), nameof(FastRandom.NextDouble))
                .AddCase(() => rnd.NextSingle(), nameof(FastRandom.NextSingle))
                .AddCase(() => rnd.NextDecimal(), nameof(RandomExtensions.NextDecimal))
#if NET11_0_OR_GREATER
                .AddCase(() => rnd.NextDecimal32(), nameof(RandomExtensions.NextDecimal32))
                .AddCase(() => rnd.NextDecimal64(), nameof(RandomExtensions.NextDecimal64))
                .AddCase(() => rnd.NextDecimal128(), nameof(RandomExtensions.NextDecimal128))
#endif
                .DoTest()
                .DumpResults(Console.Out);
        }

#if NET7_0_OR_GREATER
        [TestCase<float>]
        [TestCase<double>]
        [TestCase<decimal>]
#if NET11_0_OR_GREATER
        [TestCase<Decimal32>]
        [TestCase<Decimal64>]
        [TestCase<Decimal128>]
#endif
        public void LinearVsLogTest<T>()
            where T : IFloatingPoint<T>
        {
            var rnd = new FastRandom();
            var test = new PerformanceTest<T> { TestName = $"{nameof(LinearVsLogTest)}<{typeof(T).Name}>" }
                .AddCase(() => rnd.NextObject<T>(), nameof(RandomExtensions.NextObject));
            if (typeof(T) == typeof(float))
            {
                test.AddCase(() => (T)(object)rnd.NextSingle(), $"{nameof(FastRandom.NextSingle)}");
                test.AddCase(() => (T)(object)rnd.NextSingle(0, 1f, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextSingle)} - linear");
                test.AddCase(() => (T)(object)rnd.NextSingle(0, 1f, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextSingle)} - logarithmic");
            }
            else if (typeof(T) == typeof(double))
            {
                test.AddCase(() => (T)(object)rnd.NextDouble(), $"{nameof(FastRandom.NextDouble)}");
                test.AddCase(() => (T)(object)rnd.NextDouble(0, 1d, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDouble)} - linear");
                test.AddCase(() => (T)(object)rnd.NextDouble(0, 1d, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDouble)} - logarithmic");
            }
            else if (typeof(T) == typeof(decimal))
            {
                test.AddCase(() => (T)(object)rnd.NextDecimal(), $"{nameof(RandomExtensions.NextDecimal)}");
                test.AddCase(() => (T)(object)rnd.NextDecimal(0, 1m, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDecimal)} - linear");
                test.AddCase(() => (T)(object)rnd.NextDecimal(0, 1m, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDecimal)} - logarithmic");
            }
            else if (typeof(T) == typeof(Decimal32))
            {
                test.AddCase(() => (T)(object)rnd.NextDecimal32(), $"{nameof(RandomExtensions.NextDecimal32)}");
                test.AddCase(() => (T)(object)rnd.NextDecimal32(Decimal32.Zero, Decimal32.One, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDecimal32)} - linear");
                test.AddCase(() => (T)(object)rnd.NextDecimal32(Decimal32.Zero, Decimal32.One, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDecimal32)} - logarithmic");
            }
            else if (typeof(T) == typeof(Decimal64))
            {
                test.AddCase(() => (T)(object)rnd.NextDecimal64(), $"{nameof(RandomExtensions.NextDecimal64)}");
                test.AddCase(() => (T)(object)rnd.NextDecimal64(Decimal64.Zero, Decimal64.One, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDecimal64)} - linear");
                test.AddCase(() => (T)(object)rnd.NextDecimal64(Decimal64.Zero, Decimal64.One, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDecimal64)} - logarithmic");
            }
            else if (typeof(T) == typeof(Decimal128))
            {
                test.AddCase(() => (T)(object)rnd.NextDecimal128(), $"{nameof(RandomExtensions.NextDecimal128)}");
                test.AddCase(() => (T)(object)rnd.NextDecimal128(Decimal128.Zero, Decimal128.One, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDecimal128)} - linear");
                test.AddCase(() => (T)(object)rnd.NextDecimal128(Decimal128.Zero, Decimal128.One, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDecimal128)} - logarithmic");
            }

            test.DoTest().DumpResults(Console.Out);
        }
#endif

        #endregion
    }
}
