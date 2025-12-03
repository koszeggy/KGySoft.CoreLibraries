#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DecimalExtensionsPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2025 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using KGySoft.Annotations;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class DecimalExtensionsPerformanceTest
    {
        #region Fields

        private static readonly double diffTolerance = 1E-10d;

        private static readonly decimal[] expTestSource = [1m, 0m, -1m, 0.5m, -0.5m, 3.5m, -3.5m, DecimalExtensions.Epsilon, -DecimalExtensions.Epsilon, Int32.MinValue, Int32.MaxValue, Int32.MinValue - 1.5m, Int32.MaxValue + 1.5m];

        private static readonly decimal[] logETestSource =
        [
            0.1m,
            1.1m,
            2m,
            10m,
            DecimalExtensions.Epsilon,
            2m * DecimalExtensions.Epsilon,
            5555m,
            Decimal.MaxValue,
        ];

        #endregion

        #region Methods

        #region Static Methods

        [AssertionMethod]
        private static void AreEqual(string name, double expected, decimal actualDecimal)
        {
            var actual = (double)actualDecimal;
            Console.Write($"{name + ':',-35}{actualDecimal.ToRoundtripString()} ");
            Console.WriteLine($"{(expected.TolerantEquals(actual, diffTolerance) ? "OK" : "X")}");
            //Assert.IsTrue(expected.TolerantEquals(actual, diffTolerance), $"{actual.ToRoundtripString()} <> {expected.ToRoundtripString()}");
            Console.WriteLine();
        }

        [AssertionMethod]
        private static void AreEqual(string name, double expected, Func<decimal> actualDecimal)
        {
            decimal actual;
            try
            {
                actual = actualDecimal.Invoke();
                Console.Write($"{name + ':',-35}{actual.ToRoundtripString()} ");
                Console.WriteLine($"{(expected.TolerantEquals((double)actual, diffTolerance) ? "OK" : "X")}");
            }
            catch (Exception e)
            {
                Console.Write($"{name + ':',-35}{e.Message} ");
                Console.WriteLine($"{(e is OverflowException && (Double.IsNaN(expected) || Double.IsInfinity(expected)) ? "OK" : "X")}");
            }

            //Assert.IsTrue(expected.TolerantEquals(actual, diffTolerance), $"{actual.ToRoundtripString()} <> {expected.ToRoundtripString()}");
            Console.WriteLine();
        }

        #endregion

        #region Instance Methods

        [TestCaseSource(nameof(expTestSource))]
        public void ExpTest(decimal power)
        {
            string name = $"Exp({power})";
            double expected = Math.Exp((double)power);
            Console.WriteLine($"Math.{name} = {expected.ToRoundtripString()}");
            AreEqual(nameof(Extensions.Exp_1_PowerSeries), expected, () => power.Exp_1_PowerSeries());
            AreEqual(nameof(Extensions.Exp_2_Euler), expected, () => power.Exp_2_Euler());

            new PerformanceTest<decimal>
                {
                    TestName = $"Exp({power})",
                    Repeat = 3
                }
                .AddCase(() => power.Exp_1_PowerSeries(), nameof(Extensions.Exp_1_PowerSeries))
                .AddCase(() => power.Exp_2_Euler(), nameof(Extensions.Exp_2_Euler))
                .DoTest()
                .DumpResults(Console.Out);

            // Verdict: Euler's 'quickly converging' continued fraction method is still very slow, and also much less accurate than using power series.
            // For example, when power is 0.5, the results are:
            // Hi-res ref:     1.648721270700128146848650787814163571653776100710148011575079311640661021194215608632776520056366643002866637756306869036471874500719770466
            // Power series:   1.6487212707001281468486507876 - accurate to 27 fractional digits, converges in 23 iterations
            // Euler's method: 1.6487212707001281468486370718 - accurate to 21 fractional digits, converges in 6287 iterations

            // ==[Exp(0.5) (.NET Core 10.0.0) Results]================================================
            // Test Time: 2,000 ms
            // Warming up: Yes
            // Test cases: 2
            // Repeats: 3
            // Calling GC.Collect: Yes
            // Forced CPU Affinity: No
            // Cases are sorted by fulfilled iterations (the most first)
            // --------------------------------------------------
            // 1. Exp_1_PowerSeries: 5,361,245 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 1,787,081.07
            //   #1  1,789,048 iterations in 2,000.00 ms. Adjusted: 1,789,047.73
            //   #2  1,790,002 iterations in 2,000.00 ms. Adjusted: 1,790,001.28	 <---- Best
            //   #3  1,782,195 iterations in 2,000.00 ms. Adjusted: 1,782,194.20	 <---- Worst
            //   Worst-Best difference: 7,807.09 (0.44%)
            // 2. Exp_2_Euler: 2,947 iterations in 6,004.57 ms. Adjusted for 2,000 ms: 981.59 (-1,786,099.48 / 0.05%)
            //   #1  979 iterations in 2,001.92 ms. Adjusted: 978.06	 <---- Worst
            //   #2  981 iterations in 2,001.79 ms. Adjusted: 980.12
            //   #3  987 iterations in 2,000.85 ms. Adjusted: 986.58	 <---- Best
            //   Worst-Best difference: 8.52 (0.87%)
        }

        [TestCaseSource(nameof(logETestSource))]
        public void LogETest(decimal value)
        {
            double expected = Math.Log((double)value);
            Console.WriteLine($"Math.Log({value.ToRoundtripString()}): {expected.ToRoundtripString()}");
            Console.WriteLine();
            AreEqual(nameof(Extensions.Log_0_Naive), expected, value.Log_0_Naive());
            AreEqual(nameof(Extensions.Log_1a_TaylorOptimized), expected, value.Log_1a_TaylorOptimized());
            AreEqual(nameof(Extensions.Log_1b_TaylorSymmetricShift), expected, value.Log_1b_TaylorSymmetricShift());
            AreEqual(nameof(Extensions.Log_2_HalleyNewton), expected, value.Log_2_HalleyNewton());

            new PerformanceTest<decimal>
                {
                    TestName = $"Log({value})",
                    Repeat = 3
                }
                .AddCase(() => value.Log_0_Naive(), nameof(Extensions.Log_0_Naive))
                .AddCase(() => value.Log_1a_TaylorOptimized(), nameof(Extensions.Log_1a_TaylorOptimized))
                .AddCase(() => value.Log_1b_TaylorSymmetricShift(), nameof(Extensions.Log_1b_TaylorSymmetricShift))
                .AddCase(() => value.Log_2_HalleyNewton(), nameof(Extensions.Log_2_HalleyNewton))
                .DoTest()
                .DumpResults(Console.Out);

            // Verdict:
            // - Naive computation is imprecise for smaller values (except for powers of 10 due to the dictionary), and the iteration count can be more than 100.
            // - The fixed version multiplies too small numbers by 10, until it is > 0.1 to get a precise result.
            // - The input for Taylor-series can be optimized between |0.463| so the iteration count is never more than 79 for the decimal type.
            // - The symmetric shift variant normalizes the big values as well between (0.1 and 1], which does not affect precision, but in average
            //   it improves performance, because multiplication/division by 10 is very fast (especially when shifting can be used), and converges the
            //   value faster than doing the same by e (which is still done if needed to optimize the input, but now it requires up to a couple of iterations).
            //   Also, it provides fast result for powers of 10, even without the dictionary used in the naive implementation.
            // - The Halley-Newton algorithm promises very fast quick converging, which is true, but it also needs an Exp calculation in every iteration,
            //   which kills the performance, even when using the faster (but precise) Exp calculation, see ExpTest.

            ///////////////////////////////////////////////////////

            // Example 1: Very small value, which not a power of 10. The naive implementation is very inaccurate: even the first fractional digit is incorrect.
            //            The optimized version has almost the worst possible iteration count (78 of 79), which is still better than the 107 iterations of the naive version.
            //            But even in this case, the Taylor-series approach is twice as fast as the Halley-Newton algorithm.

            // Math.Log(0.0000000000000000000000000002): -63.779235423273335
            // 
            //   Trace: LogE_0_Orig(0.0000000000000000000000000002)
            //   The value has been adjusted by e 63 times.
            //   Starting Taylor series for -0.5672341176325428453560913221
            //   Taylor series ends at iteration 107
            // Log_0_Naive:                       -63.837558384576853141779050724 X
            // 
            //   The value has been shifted 27 times
            //   Trace: LogE_1_Taylor(0.2)
            //   The value has been adjusted by e 1 times.
            //   Starting Taylor series for -0.4563436343081909529279425057
            //   Taylor series ends at iteration 78
            // Log_1a_TaylorOptimized:             -63.77923542327333384308652861 OK
            // 
            //   The value has been shifted 27 times
            //   Trace: LogE_1_Taylor(0.2)
            //   The value has been adjusted by e 1 times.
            //   Starting Taylor series for -0.4563436343081909529279425057
            //   Taylor series ends at iteration 78
            // Log_1b_TaylorSymmetricShift:       -63.77923542327333384308652861 OK
            // 
            //   The value has been shifted 27 times
            //  Trace: LogE_2_HalleyNewton(0.2000000000000000000000000000)
            //   Trace: Exp_1_PowerSeries(-0.8000000000000000000000000000)
            //    Exp approximation ends at iteration 26
            //   Trace: Exp_1_PowerSeries(-1.5679588556662964808792376554)
            //    Exp approximation ends at iteration 24
            //   Trace: Exp_1_PowerSeries(-1.6094319663553655230114113886)
            //    Exp approximation ends at iteration 24
            //   Trace: Exp_1_PowerSeries(-1.6094379124341003570817025304)
            //    Exp approximation ends at iteration 24
            //   Trace: Exp_1_PowerSeries(-1.6094379124341003746007593334)
            //    Exp approximation ends at iteration 24
            //  Halley-Newton algorithm converged in 5 steps
            // Log_2_HalleyNewton:                -63.77923542327333384308652861 OK

            // ==[Log(0.0000000000000000000000000002) (.NET Core 10.0.0) Results]================================================
            // Test Time: 2,000 ms
            // Warming up: Yes
            // Test cases: 4
            // Repeats: 3
            // Calling GC.Collect: Yes
            // Forced CPU Affinity: No
            // Cases are sorted by fulfilled iterations (the most first)
            // --------------------------------------------------
            // 1. Log_1a_TaylorOptimized: 1,649,021 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 549,673.25
            //   #1  550,181 iterations in 2,000.00 ms. Adjusted: 550,180.92	 <---- Best
            //   #2  548,981 iterations in 2,000.00 ms. Adjusted: 548,980.18	 <---- Worst
            //   #3  549,859 iterations in 2,000.00 ms. Adjusted: 549,858.64
            //   Worst-Best difference: 1,200.74 (0.22%)
            // 2. Log_1b_TaylorSymmetricShift: 1,640,072 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 546,690.32 (-2,982.92 / 99.46%)
            //   #1  547,415 iterations in 2,000.00 ms. Adjusted: 547,414.73
            //   #2  548,756 iterations in 2,000.00 ms. Adjusted: 548,755.81	 <---- Best
            //   #3  543,901 iterations in 2,000.00 ms. Adjusted: 543,900.43	 <---- Worst
            //   Worst-Best difference: 4,855.38 (0.89%)
            // 3. Log_0_Naive: 884,693 iterations in 6,000.01 ms. Adjusted for 2,000 ms: 294,896.98 (-254,776.26 / 53.65%)
            //   #1  293,049 iterations in 2,000.00 ms. Adjusted: 293,048.49	 <---- Worst
            //   #2  295,528 iterations in 2,000.01 ms. Adjusted: 295,527.04
            //   #3  296,116 iterations in 2,000.00 ms. Adjusted: 296,115.42	 <---- Best
            //   Worst-Best difference: 3,066.94 (1.05%)
            // 4. Log_2_HalleyNewton: 829,570 iterations in 6,000.10 ms. Adjusted for 2,000 ms: 276,518.57 (-273,154.67 / 50.31%)
            //   #1  277,019 iterations in 2,000.09 ms. Adjusted: 277,005.88	 <---- Best
            //   #2  275,897 iterations in 2,000.00 ms. Adjusted: 275,896.34	 <---- Worst
            //   #3  276,654 iterations in 2,000.00 ms. Adjusted: 276,653.50
            //   Worst-Best difference: 1,109.55 (0.40%)

            ///////////////////////////////////////////////////////

            // Example 2: Just by looking at the number of iterations, the simple optimized version should perform better than the symmetric shift version.
            //            Still, the cost of adjusting by e is so much bigger than dividing by 10, that it's still faster, though it required 23 more iterations.

            // Math.Log(5555): 8.62245370207373
            // 
            //   Trace: LogE_0_Orig(5555)
            //   The value has been adjusted by e 9 times.
            //   Starting Taylor series for -0.3144585382984951025406281823
            //   Taylor series ends at iteration 53
            // Log_0_Naive:                       8.622453702073730369546901179 OK
            // 
            //   Trace: LogE_1_Taylor(5555)
            //   The value has been adjusted by e 9 times.
            //   Starting Taylor series for -0.3144585382984951025406281823
            //   Taylor series ends at iteration 53
            // Log_1a_TaylorOptimized:             8.622453702073730369546901179 OK
            // 
            //   The value has been shifted 4 times
            //   Trace: LogE_1_Taylor(0.5555)
            //   The value has been adjusted by e 0 times.
            //   Starting Taylor series for -0.4445
            //   Taylor series ends at iteration 76
            // Log_1b_TaylorSymmetricShift:       8.622453702073730369546901179 OK
            // 
            //   The value has been shifted 4 times
            //  Trace: LogE_2_HalleyNewton(0.5555)
            //   Trace: Exp_1_PowerSeries(-0.4445)
            //    Exp approximation ends at iteration 22
            //   Trace: Exp_1_PowerSeries(-0.5876415079169104457780997228)
            //    Exp approximation ends at iteration 24
            //   Trace: Exp_1_PowerSeries(-0.5878866699012244237127277110)
            //    Exp approximation ends at iteration 24
            //   Trace: Exp_1_PowerSeries(-0.5878866699024523665250646408)
            //    Exp approximation ends at iteration 24
            //   Trace: Exp_1_PowerSeries(-0.5878866699024523665250646406)
            //    Exp approximation ends at iteration 24
            //  Halley-Newton algorithm converged in 5 steps
            // Log_2_HalleyNewton:                8.622453702073730369546901178 OK
            //             
            // ==[Log(5555) (.NET Core 10.0.0) Results]================================================
            // Test Time: 2,000 ms
            // Warming up: Yes
            // Test cases: 4
            // Repeats: 3
            // Calling GC.Collect: Yes
            // Forced CPU Affinity: No
            // Cases are sorted by fulfilled iterations (the most first)
            // --------------------------------------------------
            // 1. Log_1b_TaylorSymmetricShift: 2,644,560 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 881,519.50
            //   #1  881,186 iterations in 2,000.00 ms. Adjusted: 881,185.78
            //   #2  882,362 iterations in 2,000.00 ms. Adjusted: 882,361.38	 <---- Best
            //   #3  881,012 iterations in 2,000.00 ms. Adjusted: 881,011.34	 <---- Worst
            //   Worst-Best difference: 1,350.04 (0.15%)
            // 2. Log_0_Naive: 2,332,291 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 777,429.85 (-104,089.65 / 88.19%)
            //   #1  770,761 iterations in 2,000.00 ms. Adjusted: 770,760.85	 <---- Worst
            //   #2  771,253 iterations in 2,000.00 ms. Adjusted: 771,252.19
            //   #3  790,277 iterations in 2,000.00 ms. Adjusted: 790,276.53	 <---- Best
            //   Worst-Best difference: 19,515.68 (2.53%)
            // 3. Log_1a_TaylorOptimized: 2,332,119 iterations in 6,000.00 ms. Adjusted for 2,000 ms: 777,372.62 (-104,146.88 / 88.19%)
            //   #1  785,548 iterations in 2,000.00 ms. Adjusted: 785,547.76	 <---- Best
            //   #2  771,650 iterations in 2,000.00 ms. Adjusted: 771,649.58	 <---- Worst
            //   #3  774,921 iterations in 2,000.00 ms. Adjusted: 774,920.54
            //   Worst-Best difference: 13,898.19 (1.80%)
            // 4. Log_2_HalleyNewton: 928,303 iterations in 6,000.01 ms. Adjusted for 2,000 ms: 309,433.90 (-572,085.61 / 35.10%)
            //   #1  308,501 iterations in 2,000.00 ms. Adjusted: 308,500.43	 <---- Worst
            //   #2  309,943 iterations in 2,000.00 ms. Adjusted: 309,942.35	 <---- Best
            //   #3  309,859 iterations in 2,000.00 ms. Adjusted: 309,858.91
            //   Worst-Best difference: 1,441.92 (0.47%)
        }

        [Test]
        public void PowTest()
        {
            //decimal value = 0.5m;
            //decimal power = 2.4m;
            decimal value = -1 - DecimalExtensions.Epsilon;
            //decimal value = 1 + DecimalExtensions.Epsilon;
            decimal power = UInt32.MaxValue;
            //decimal value = -0.5m;
            //decimal power = -3m;

            double expected = Math.Pow((double)value, (double)power);
            double expectedByExpLog = Math.Exp((double)power * Math.Log((double)value));

            decimal actual0 = value.Pow(power);
            decimal actual2 = value.Pow2(power);

            Console.WriteLine($"Expected by Math.Pow: {expected}");
            Console.WriteLine($"Expected by Math.Exp/Log: {expectedByExpLog}");
            Console.WriteLine($"Pow0: {actual0}");
            Console.WriteLine($"Pow2: {actual2}");

#if DEBUG
            Assert.Inconclusive("Switch to release build");
#endif

            new PerformanceTest<decimal>
                {
                    TestName = $"PowTest {value}^{power}",
                    Repeat = 3
                }
                .AddCase(() => value.Pow(power), nameof(DecimalExtensions.Pow))
                .AddCase(() => value.Pow2(power), nameof(DecimalExtensions.Pow2))
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion

        #endregion
    }

    internal static class Extensions
    {
        #region Constants

        private const decimal log10E = 0.4342944819032518276511289189m;
        private const decimal logE10 = 2.3025850929940456840179914547m;
        private const decimal e = 2.7182818284590452353602874714m;
        private const decimal eReciprocal = 1m / e;

        #endregion

        #region Fields

        private static readonly Dictionary<decimal, int> powerOf10 = InitPowerOf10();

        #endregion

        #region Methods

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static decimal Exp_1_PowerSeries(this decimal power)
        {
#if DEBUG
            Console.WriteLine($"  Trace: {nameof(Exp_1_PowerSeries)}({power})");
#endif
            int integerPart;
            if (power >= 1m)
            {
                decimal floor = Math.Floor(power);
                power -= floor;
                integerPart = (int)floor; // > Int32.MaxInt is not a problem, an OverflowException would be thrown later anyway
            }
            else if (power < 0m)
            {
                decimal floor = Math.Floor(-power);
                if (floor >= 66) // e^-66 < epsilon (~ 2.17e-29)
                    return 0m;

                power += floor;
                integerPart = -(int)floor;
            }
            else
                integerPart = 0;

            // Doing the power series for the fractional part of power: exp(p) = 1 + p + (p^2)/2! + (p^3)/3! + ...
            // see also https://en.wikipedia.org/wiki/Exponential_function#Power_series
            decimal result = 1m;
            decimal acc = 1m;
            for (int i = 1; ; i++)
            {
                decimal prevResult = result;
                acc *= power / i;
                result += acc;
                if (prevResult == result)
                {
#if DEBUG
                    Console.WriteLine($"   Exp approximation ends at iteration {i}"); 
#endif
                    break;
                }
            }

            if (integerPart != 0)
                result *= e.Pow(integerPart);
            return result;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static decimal Exp_2_Euler(this decimal power)
        {
#if DEBUG
            Console.WriteLine($"  Trace: {nameof(Exp_2_Euler)}({power})");
#endif
            int integerPart;
            if (power >= 1m)
            {
                decimal floor = Math.Floor(power);
                power -= floor;
                integerPart = (int)floor; // > Int32.MaxInt is not a problem, an OverflowException would be thrown later anyway
            }
            else if (power < 0m)
            {
                decimal floor = Math.Floor(-power);
                if (floor >= 66) // e^-66 < epsilon (~ 2.17e-29)
                    return 0m;

                power += floor;
                integerPart = -(int)floor;
            }
            else
                integerPart = 0;

            // Continued fraction (Euler's faster generalized CF): - https://en.wikipedia.org/wiki/Exponential_function#Continued_fractions
            // e^z = 1 + 2z / (2 - z + z^2/(6 + z^2/(10 + z^2/(14 + ...))))

            // The denominator can be evaluated by using the Lentz's algorithm - https://en.wikipedia.org/wiki/Lentz%27s_algorithm
            decimal z = power;
            decimal z2 = z * z;

            // First term (b0)
            decimal b0 = 2m - z;

            // f0 = b0, f1 = b0 + (a1/b1), f2 = b0 + (a1/(b1 + (a2/b2))), ...
            decimal f = b0 == 0m ? DecimalExtensions.Epsilon : b0;
            decimal c = f; // Cn = An/A(n-1)
            decimal d = 0m; // Dn = B(n-1)/Bn

            // iterating k to produce b_k = 6 + 4*(k-1), a_k = z^2 for all k>=1
            for (int k = 1; /*k <= maxIterations*/; k++)
            {
                decimal a = z2;
                decimal b = 6m + 4m * (k - 1);

                d = b + a * d;
                if (d == 0m)
                    d = DecimalExtensions.Epsilon;
                d = 1m / d;

                c = b + a / c;
                if (c == 0m)
                    c = DecimalExtensions.Epsilon;

                decimal delta = c * d;
                f *= delta;

                //if (Math.Abs(delta - 1m) < eps)
                if (delta == 1m)
                {
#if DEBUG
                    Console.WriteLine($"   Continued fraction converged at iteration {k}");
#endif
                    break;
                }

//                // if reached last iteration, we just accept current f
//                if (k == maxIterations && Math.Abs(delta - 1m) >= eps)
//                {
//#if DEBUG
//                    Console.WriteLine("   Continued fraction reached max iterations without full convergence");
//#endif
//                    break;
//                }
            }

            // final result
            decimal result = 1m + (2m * z) / f;
            if (integerPart != 0)
                result *= e.Pow(integerPart);
            return result;
        }

        public static decimal Normalize(this decimal value) => value / 1.0000000000000000000000000000m;

        public static decimal Log_0_Naive(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            int resultLog10;
            if (value >= 1m)
            {
                if (powerOf10.TryGetValue(value, out resultLog10))
                    return resultLog10 / log10E;
            }
            else
            {
                decimal reciprocal = 1m / value;
                if (reciprocal != 0m && powerOf10.TryGetValue(reciprocal, out resultLog10))
                    return -resultLog10 / log10E;
            }

            return LogE_0_Orig(value);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static decimal Log_1a_TaylorOptimized(this decimal value)
        {
            if (value > 0.1m)
                return LogE_1_Taylor(value).Normalize();
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            // We could just return LogE(value), but it gets very inaccurate for small values.
            // So normalizing the small values between (0.1 and 1], and utilising that Log(0.00123456) = Log(0.123456 * 10^-2) = Log(0.123456) - 3 * Log(10)
            int exp = 0;
            do
            {
                value = value.ShiftLeft();
                exp += 1;
            } while (value <= 0.1m);

#if DEBUG
            Console.WriteLine($"  The value has been shifted {exp} times");
#endif

            decimal result = (value == 1m ? 0m : LogE_1_Taylor(value)) - exp * logE10;
            return result.Normalize();
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static decimal Log_1b_TaylorSymmetricShift(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            // We could just return LogE(value), but it gets very inaccurate for very small values.
            // So normalizing the value between (0.1 and 1], and utilising that Log(123.456) = Log(0.123456 * 10^3) = Log(0.123456) + 3 * Log(10)
            int exp = 0;
            if (value > 1m)
            {
                do
                {
                    value = value.ShiftRight();
                    exp += 1;
                } while (value > 1m);
            }
            else
            {
                while (value <= 0.1m)
                {
                    value = value.ShiftLeft();
                    exp -= 1;
                }
            }

#if DEBUG
            Console.WriteLine($"  The value has been shifted {Math.Abs(exp)} times");
#endif

            decimal result = value == 1m ? 0m : LogE_1_Taylor(value);
            if (exp != 0)
                result += exp * logE10;
            return result.Normalize();
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static decimal Log_2_HalleyNewton(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            // The Halley-Newton algorithm requires the value be smaller than 2 to avoid overflow. Too small values would cause some inaccuracy, do doing the same as for Taylor.
            // So normalizing the value between (0.1 and 2), and utilising that Log(123.456) = Log(0.123456 * 10^3) = Log(0.123456) + 3 * Log(10)
            int exp = 0;
            if (value > 2m)
            {
                do
                {
                    value *= 0.1m;
                    exp += 1;
                } while (value > 2m);
            }
            else
            {
                while (value <= 0.1m)
                {
                    value *= 10m;
                    exp -= 1;
                }
            }

#if DEBUG
            Console.WriteLine($"  The value has been shifted {Math.Abs(exp)} times");
#endif

            decimal result = value == 1m ? 0m : LogE_2_HalleyNewton(value);
            if (exp != 0)
                result += exp * logE10;
            return result.Normalize();
        }

        #endregion

        #region Private Methods

        private static Dictionary<decimal, int> InitPowerOf10()
        {
            var result = new Dictionary<decimal, int> { [0m] = 1 };
            decimal value = 1m;
            for (int i = 0; i <= 28; i++)
            {
                result[value] = i;
                if (i < 28)
                    value *= 10m;
            }

            return result;
        }

        private static decimal LogE_0_Orig(decimal value)
        {
#if DEBUG
            Console.WriteLine($"  Trace: {nameof(LogE_0_Orig)}({value})");
#endif
            int count = 0;
            while (value >= 1m)
            {
                value *= eReciprocal;
                count += 1;
            }

            while (value <= eReciprocal)
            {
                value *= e;
                count -= 1;
            }

#if DEBUG
            Console.WriteLine($"  The value has been adjusted by e {Math.Abs(count)} times.");
#endif

            value -= 1;
            if (value == 0m)
                return count;

            // going on with Taylor series
#if DEBUG
            Console.WriteLine($"  Starting Taylor series for {value}");
#endif
            decimal result = 0m;
            decimal acc = 1m;
            for (int i = 1; /*i <= maxTaylorIteration*/; i++)
            {
                decimal prevResult = result;
                acc *= -value;
                result += acc / i;
                if (prevResult == result)
                {
#if DEBUG
                    Console.WriteLine($"  Taylor series ends at iteration {i}");
#endif
                    break;
                }
            }

            return count - result;
        }

        private static decimal LogE_1_Taylor(decimal value)
        {
#if DEBUG
            Console.WriteLine($"  Trace: {nameof(LogE_1_Taylor)}({value})");
#endif
            int count = 0;

            // Constants are chosen so the Taylor-series always start with |value| < 0.4625 for faster converging (up to 79 iterations)
            // 1.462 / E - 1 = -0.462160257007; 1.462 - 1 = 0.462
            if (value >= 1.462m)
            {
                do
                {
                    value *= eReciprocal;
                    count += 1;
                } while (value >= 1.462m);
            }
            else
            {
                // 0.538 * E - 1 = 0.462435623711; 0.538 - 1 = 0.462
                while (value <= 0.538m)
                {
                    value *= e;
                    count -= 1;
                }
            }

#if DEBUG
            Console.WriteLine($"  The value has been adjusted by e {Math.Abs(count)} times.");
#endif

            value -= 1;
            if (value == 0m)
                return count;

            // going on with Taylor series
#if DEBUG
            Console.WriteLine($"  Starting Taylor series for {value}");
#endif
            decimal result = 0m;
            decimal acc = 1m;
            for (int i = 1; ; i++)
            {
                decimal prevResult = result;
                acc *= -value;
                result += acc / i;
                if (prevResult == result)
                {
#if DEBUG
                    Console.WriteLine($"  Taylor series ends at iteration {i}");
#endif
                    break;
                }
            }

            return count - result;
        }

        private static decimal LogE_2_HalleyNewton(decimal value)
        {
#if DEBUG
            Console.WriteLine($" Trace: {nameof(LogE_2_HalleyNewton)}({value})");
#endif
            // based on the formula (see https://en.wikipedia.org/wiki/Natural_logarithm#High_precision),
            // yNext = yPrev + 2 * ((value - Exp(yPrev)) / (value + Exp(yPrev)))
            decimal yNext = value - 1m;
            decimal yPrev = yNext;
            decimal yPrevPrev = 0m;

            for (int i = 1; /*i < maxIteration*/; i++)
            {
                decimal expYPrev = yPrev.Exp_1_PowerSeries();
                yNext = yPrev + 2m * ((value - expYPrev) / (value + expYPrev));
                if (yNext == yPrev || yNext == yPrevPrev)
                {
#if DEBUG
                    if (yNext == yPrev)
                        Console.WriteLine($" Halley-Newton algorithm converged in {i} steps");
                    else
                        Console.WriteLine($" Halley-Newton algorithm started to oscillate in {i} steps");
#endif
                    return yNext;
                }

                yPrevPrev = yPrev;
                yPrev = yNext;
            }
        }

        #endregion

        #endregion
    }

}
