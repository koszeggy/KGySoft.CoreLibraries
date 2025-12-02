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
            1,
            0.1m,
            1.1m,
            0.00000000000001m,
            10m,
            Decimal.MaxValue,
            DecimalExtensions.Epsilon,
            2m * DecimalExtensions.Epsilon,
            1m / DecimalExtensions.Epsilon,
            1m / DecimalExtensions.E,
            DecimalExtensions.E,
            DecimalExtensions.PI,
            0.09m,
            0.11m,
            0.5m,
            0.45m,
            0.55m,
            0.9m,
            1.5m,
            11m
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
            Console.WriteLine($"Log({value.ToRoundtripString()}): {expected.ToRoundtripString()}");
            Console.WriteLine();
            AreEqual(nameof(Extensions.Log_0_Orig), expected, value.Log_0_Orig());
            AreEqual(nameof(Extensions.Log_1_PreciseComputation), expected, value.Log_1_PreciseComputation());
            AreEqual(nameof(Extensions.Log_2a_HalleyNewtonByTaylor), expected, value.Log_2a_HalleyNewtonByTaylor());
            AreEqual(nameof(Extensions.Log_2b_HalleyNewtonByEuler), expected, value.Log_2b_HalleyNewtonByEuler());

            new PerformanceTest<decimal>
                {
                    TestName = $"Log({value})",
                    Repeat = 3
                }
                .AddCase(() => value.Log_0_Orig(), nameof(Extensions.Log_0_Orig))
                .AddCase(() => value.Log_1_PreciseComputation(), nameof(Extensions.Log_1_PreciseComputation))
                .AddCase(() => value.Log_2a_HalleyNewtonByTaylor(), nameof(Extensions.Log_2a_HalleyNewtonByTaylor))
                .AddCase(() => value.Log_2b_HalleyNewtonByEuler(), nameof(Extensions.Log_2b_HalleyNewtonByEuler))
                .DoTest()
                .DumpResults(Console.Out);
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

        private const int maxTaylorIteration = 100;
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

        public static decimal Log_0_Orig(this decimal value)
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

            return RoundInternal(LogE_0_Orig(value));
        }

        public static decimal Log_1_PreciseComputation(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            // We could just return LogE(value), but it gets very inaccurate for very small values, and also the Taylor-series way have too many iterations.
            // So normalizing the value between (0.1 and 1], and utilising that Log(123.456) = Log(0.123456 * 10^3) = Log(0.123456) + 3 * Log(10)
            int exp = 0;
            if (value > 1m)
            {
                do
                {
                    value *= 0.1m;
                    exp += 1;
                } while (value > 1m);
            }
            else
            {
                while (value <= 0.1m)
                {
                    value *= 10m;
                    exp -= 1;
                }
            }

            decimal result = LogE_1_Taylor(value);
            if (exp != 0)
                result += exp * logE10;
            return result.Normalize();
        }

        public static decimal Log_2a_HalleyNewtonByTaylor(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            // We could just return LogE(value), but it gets very inaccurate for very small values, and also the Taylor-series way have too many iterations.
            // So normalizing the value between (0.1 and 1], and utilising that Log(123.456) = Log(0.123456 * 10^3) = Log(0.123456) + 3 * Log(10)
            int exp = 0;
            if (value > 1m)
            {
                do
                {
                    value *= 0.1m;
                    exp += 1;
                } while (value > 1m);
            }
            else
            {
                while (value <= 0.1m)
                {
                    value *= 10m;
                    exp -= 1;
                }
            }

            decimal result = LogE_2a_HalleyNewtonByTaylor(value);
            if (exp != 0)
                result += exp * logE10;
            return result.Normalize();
        }

        public static decimal Log_2b_HalleyNewtonByEuler(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            // We could just return LogE(value), but it gets very inaccurate for very small values, and also the Taylor-series way have too many iterations.
            // So normalizing the value between (0.1 and 1], and utilising that Log(123.456) = Log(0.123456 * 10^3) = Log(0.123456) + 3 * Log(10)
            int exp = 0;
            if (value > 1m)
            {
                do
                {
                    value *= 0.1m;
                    exp += 1;
                } while (value > 1m);
            }
            else
            {
                while (value <= 0.1m)
                {
                    value *= 10m;
                    exp -= 1;
                }
            }

            decimal result = LogE_2b_HalleyNewtonByEuler(value);
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

            value -= 1;
            if (value == 0m)
                return count;

            // going on with Taylor series
            decimal result = 0m;
            decimal acc = 1m;
            for (int i = 1; i <= maxTaylorIteration; i++)
            {
                decimal prevResult = result;
                acc *= -value;
                result += acc / i;
                if (prevResult == result)
                {
#if DEBUG
                    Console.WriteLine($"  Taylor series ends early at iteration {i}");
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

            if (value > 1m)
            {
                do
                {
                    value *= eReciprocal;
                    count += 1;
                } while (value > 1m);
            }
            else
            {
                while (value <= eReciprocal)
                {
                    value *= e;
                    count -= 1;
                }
            }

            value -= 1;
            if (value == 0m)
                return count;

            // going on with Taylor series
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

        private static decimal LogE_2a_HalleyNewtonByTaylor(decimal value)
        {
            // based on the formula (see https://en.wikipedia.org/wiki/Natural_logarithm#High_precision),
            // yNext = yPrev + 2 * ((value - Exp(yPrev)) / (value + Exp(yPrev)))
            decimal yNext = value - 1m;
            decimal yPrev = yNext;

            while (true)
            {
                decimal expYPrev = yPrev.Exp_1_PowerSeries();
                yNext = yPrev + 2m * ((value - expYPrev) / (value + expYPrev));
                if (yNext == yPrev)
                    return yNext;
                yPrev = yNext;
            }
        }

        private static decimal LogE_2b_HalleyNewtonByEuler(decimal value)
        {
            // based on the formula (see https://en.wikipedia.org/wiki/Natural_logarithm#High_precision),
            // yNext = yPrev + 2 * ((value - Exp(yPrev)) / (value + Exp(yPrev)))
            decimal yNext = value - 1m;
            decimal yPrev = yNext;

            while (true)
            {
                decimal expYPrev = yPrev.Exp_2_Euler();
                yNext = yPrev + 2m * ((value - expYPrev) / (value + expYPrev));
                if (yNext == yPrev)
                    return yNext;
                yPrev = yNext;
            }
        }

        private static decimal RoundInternal(decimal value)
        {
            decimal round23 = Math.Round(value, 23);
            if (round23 == 0m)
                return value;
            if (Math.Round(value, 5) == round23)
                return Normalize(round23);
            return value;
        }

        #endregion

        #endregion
    }

}
