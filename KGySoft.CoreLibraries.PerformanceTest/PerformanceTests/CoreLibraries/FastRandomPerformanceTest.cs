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
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class FastRandomPerformanceTest
    {
        #region Methods

        #region Public Methods

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
            new RandomizedPerformanceTest { TestName = "Floating point sample comparison" }
                .AddCase(rnd => rnd.NextDouble(), nameof(FastRandom.NextDouble))
                .AddCase(rnd => rnd.NextSingle(), nameof(FastRandom.NextSingle))
                .AddCase(rnd => rnd.NextDecimal(), nameof(RandomExtensions.NextDecimal))
#if NET11_0_OR_GREATER
                .AddCase(rnd => rnd.NextDecimal32(), nameof(RandomExtensions.NextDecimal32))
                .AddCase(rnd => rnd.NextDecimal64(), nameof(RandomExtensions.NextDecimal64))
                .AddCase(rnd => rnd.NextDecimal128(), nameof(RandomExtensions.NextDecimal128))
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
            var test = new RandomizedPerformanceTest<T> { TestName = $"{nameof(LinearVsLogTest)}<{typeof(T).Name}>" }
                .AddCase(rnd => rnd.NextObject<T>(), nameof(RandomExtensions.NextObject));
            if (typeof(T) == typeof(float))
            {
                test.AddCase(rnd => (T)(object)rnd.NextSingle(), $"{nameof(FastRandom.NextSingle)}");
                test.AddCase(rnd => (T)(object)rnd.NextSingle(0, 1f, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextSingle)} - linear");
                test.AddCase(rnd => (T)(object)rnd.NextSingle(0, 1f, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextSingle)} - logarithmic");
            }
            else if (typeof(T) == typeof(double))
            {
                test.AddCase(rnd => (T)(object)rnd.NextDouble(), $"{nameof(FastRandom.NextDouble)}");
                test.AddCase(rnd => (T)(object)rnd.NextDouble(0, 1d, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDouble)} - linear");
                test.AddCase(rnd => (T)(object)rnd.NextDouble(0, 1d, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDouble)} - logarithmic");
            }
            else if (typeof(T) == typeof(decimal))
            {
                test.AddCase(rnd => (T)(object)rnd.NextDecimal(), $"{nameof(RandomExtensions.NextDecimal)}");
                test.AddCase(rnd => (T)(object)rnd.NextDecimal(0, 1m, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDecimal)} - linear");
                test.AddCase(rnd => (T)(object)rnd.NextDecimal(0, 1m, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDecimal)} - logarithmic");
            }
            else if (typeof(T) == typeof(Decimal32))
            {
                test.AddCase(rnd => (T)(object)rnd.NextDecimal32(), $"{nameof(RandomExtensions.NextDecimal32)}");
                test.AddCase(rnd => (T)(object)rnd.NextDecimal32(Decimal32.Zero, Decimal32.One, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDecimal32)} - linear");
                test.AddCase(rnd => (T)(object)rnd.NextDecimal32(Decimal32.Zero, Decimal32.One, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDecimal32)} - logarithmic");
            }
            else if (typeof(T) == typeof(Decimal64))
            {
                test.AddCase(rnd => (T)(object)rnd.NextDecimal64(), $"{nameof(RandomExtensions.NextDecimal64)}");
                test.AddCase(rnd => (T)(object)rnd.NextDecimal64(Decimal64.Zero, Decimal64.One, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDecimal64)} - linear");
                test.AddCase(rnd => (T)(object)rnd.NextDecimal64(Decimal64.Zero, Decimal64.One, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDecimal64)} - logarithmic");
            }
            else if (typeof(T) == typeof(Decimal128))
            {
                test.AddCase(rnd => (T)(object)rnd.NextDecimal128(), $"{nameof(RandomExtensions.NextDecimal128)}");
                test.AddCase(rnd => (T)(object)rnd.NextDecimal128(Decimal128.Zero, Decimal128.One, FloatScale.ForceLinear), $"{nameof(RandomExtensions.NextDecimal128)} - linear");
                test.AddCase(rnd => (T)(object)rnd.NextDecimal128(Decimal128.Zero, Decimal128.One, FloatScale.ForceLogarithmic), $"{nameof(RandomExtensions.NextDecimal128)} - logarithmic");
            }

            test.DoTest().DumpResults(Console.Out);
        }
#endif

#if NETCOREAPP3_0_OR_GREATER
        [Test]
        public void GetBitMaskTest32()
        {
            new RandomizedPerformanceTest<uint> { TestName = "GetBitMaskTest 32 bit", Repeat = 3 }
                .AddCase(rnd => GetBitMask_0_Vanilla((uint)rnd.Next()), nameof(GetBitMask_0_Vanilla))
                .AddCase(rnd => GetBitMask_1_ByRoundUp((uint)rnd.Next()), nameof(GetBitMask_1_ByRoundUp))
                .AddCase(rnd => GetBitMask_2_ByLeadingZero((uint)rnd.Next()), nameof(GetBitMask_2_ByLeadingZero))
                .DoTest()
                .DumpResults(Console.Out);

            // ==[GetBitMaskTest 32 bit (.NET Core 11.0.0-rc.1.26425.128) Results]================================================
            // Test Time: 2,000 ms
            // Warming up: Yes
            // Test cases: 3
            // Repeats: 3
            // Calling GC.Collect: Yes
            // Forced CPU Affinity: No
            // Cases are sorted by fulfilled iterations (the most first)
            // --------------------------------------------------
            // 1. GetBitMask_1_ByRoundUp: 442,706,551 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 147,568,850.33
            //   #1  146,890,776 iterations in 2,000.00 ms. Adjusted: 146,890,776.00    <---- Worst
            //   #2  147,877,926 iterations in 2,000.00 ms. Adjusted: 147,877,926.00
            //   #3  147,937,849 iterations in 2,000.00 ms. Adjusted: 147,937,849.00    <---- Best
            //   Worst-Best difference: 1,047,073.00 (0.71%)
            // 2. GetBitMask_2_ByLeadingZero: 439,178,758 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 146,392,919.33 (-1,175,931.00 / 99.20%)
            //   #1  146,187,340 iterations in 2,000.00 ms. Adjusted: 146,187,340.00    <---- Worst
            //   #2  146,655,387 iterations in 2,000.00 ms. Adjusted: 146,655,387.00    <---- Best
            //   #3  146,336,031 iterations in 2,000.00 ms. Adjusted: 146,336,031.00
            //   Worst-Best difference: 468,047.00 (0.32%)
            // 3. GetBitMask_0_Vanilla: 427,731,630 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 142,577,210.00 (-4,991,640.33 / 96.62%)
            //   #1  142,362,542 iterations in 2,000.00 ms. Adjusted: 142,362,542.00    <---- Worst
            //   #2  142,715,040 iterations in 2,000.00 ms. Adjusted: 142,715,040.00    <---- Best
            //   #3  142,654,048 iterations in 2,000.00 ms. Adjusted: 142,654,048.00
            //   Worst-Best difference: 352,498.00 (0.25%)
        }

        [Test]
        public void GetBitMaskTest64()
        {
            new RandomizedPerformanceTest<ulong> { TestName = "GetBitMaskTest 64 bit", Repeat = 3 }
                .AddCase(rnd => GetBitMask_0_Vanilla((ulong)rnd.NextInt64()), nameof(GetBitMask_0_Vanilla))
                .AddCase(rnd => GetBitMask_1_ByRoundUp((ulong)rnd.NextInt64()), nameof(GetBitMask_1_ByRoundUp))
                .AddCase(rnd => GetBitMask_2_ByLeadingZero((ulong)rnd.NextInt64()), nameof(GetBitMask_2_ByLeadingZero))
                .DoTest()
                .DumpResults(Console.Out);

            // ==[GetBitMaskTest 64 bit (.NET Core 11.0.0-rc.1.26425.128) Results]================================================
            // Test Time: 2,000 ms
            // Warming up: Yes
            // Test cases: 3
            // Repeats: 3
            // Calling GC.Collect: Yes
            // Forced CPU Affinity: No
            // Cases are sorted by fulfilled iterations (the most first)
            // --------------------------------------------------
            // 1. GetBitMask_2_ByLeadingZero: 437,917,804 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 145,972,601.33
            //   #1  145,980,076 iterations in 2,000.00 ms. Adjusted: 145,980,076.00
            //   #2  146,248,090 iterations in 2,000.00 ms. Adjusted: 146,248,090.00    <---- Best
            //   #3  145,689,638 iterations in 2,000.00 ms. Adjusted: 145,689,638.00    <---- Worst
            //   Worst-Best difference: 558,452.00 (0.38%)
            // 2. GetBitMask_1_ByRoundUp: 426,136,466 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 142,045,488.67 (-3,927,112.67 / 97.31%)
            //   #1  141,997,616 iterations in 2,000.00 ms. Adjusted: 141,997,616.00
            //   #2  141,230,521 iterations in 2,000.00 ms. Adjusted: 141,230,521.00    <---- Worst
            //   #3  142,908,329 iterations in 2,000.00 ms. Adjusted: 142,908,329.00    <---- Best
            //   Worst-Best difference: 1,677,808.00 (1.19%)
            // 3. GetBitMask_0_Vanilla: 386,877,303 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 128,959,010.82 (-17,013,590.52 / 88.34%)
            //   #1  128,834,545 iterations in 2,000.00 ms. Adjusted: 128,834,274.45
            //   #2  128,381,248 iterations in 2,000.00 ms. Adjusted: 128,381,248.00    <---- Worst
            //   #3  129,661,510 iterations in 2,000.00 ms. Adjusted: 129,661,510.00    <---- Best
            //   Worst-Best difference: 1,280,262.00 (1.00%)
        }
#endif

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static uint GetBitMask_0_Vanilla(uint value)
        {
            --value;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static uint GetBitMask_1_ByRoundUp(uint value) => BitOperations.RoundUpToPowerOf2(value) - 1;

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static uint GetBitMask_2_ByLeadingZero(uint value)
            => BitOperations.IsPow2(value) ? value - 1 : UInt32.MaxValue >> BitOperations.LeadingZeroCount(value);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static ulong GetBitMask_0_Vanilla(ulong value)
        {
            --value;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            return value;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static ulong GetBitMask_1_ByRoundUp(ulong value) => BitOperations.RoundUpToPowerOf2(value) - 1;

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static ulong GetBitMask_2_ByLeadingZero(ulong value)
            => BitOperations.IsPow2(value) ? value - 1 : UInt64.MaxValue >> BitOperations.LeadingZeroCount(value);
#endif

        #endregion

        #endregion
    }
}
