#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DecimalExtensionsTest.cs
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

using KGySoft.Annotations;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class DecimalExtensionsTest : TestBase
    {
        #region Fields

        private static readonly object[][] powITestSource =
        [
            [0m, 0],
            [0m, 1],
            [0m, -1],
            [1m, 0],
            [1m, 1],
            [1m, -1],
            [-1m, 0],
            [-1m, 1],
            [-1m, -1],
            [1m + DecimalExtensions.Epsilon, Int32.MaxValue],
            [-1m - DecimalExtensions.Epsilon, Int32.MaxValue],
            [1m - DecimalExtensions.Epsilon, Int32.MinValue],
            [-1m + DecimalExtensions.Epsilon, Int32.MinValue],
            [1m + DecimalExtensions.Epsilon, Int32.MinValue],
            [-1m - DecimalExtensions.Epsilon, Int32.MinValue],
            [1m - DecimalExtensions.Epsilon, Int32.MaxValue],
            [-1m + DecimalExtensions.Epsilon, Int32.MaxValue],
            [Decimal.MaxValue, Int32.MinValue],
            [Decimal.MinValue, Int32.MinValue],
        ];

        private static readonly decimal[] logTestSource =
        [
            1m,
            0.1m,
            0.00000000000001m,
            1.1m,
            2m,
            3m,
            4m,
            8m,
            9m,
            10m,
            27m,
            128m,
            256m,
            1 << 16,
            1L << 62,
            DecimalExtensions.Epsilon,
            2m * DecimalExtensions.Epsilon,
            1m / DecimalExtensions.Epsilon,
            5555m,
            Decimal.MaxValue,
            DecimalExtensions.E,
            1m / DecimalExtensions.E,
            DecimalExtensions.PI,
            2m + DecimalExtensions.Epsilon,
            2m - DecimalExtensions.Epsilon,
            1.462m,
            1.462m + DecimalExtensions.Epsilon,
            1.462m - DecimalExtensions.Epsilon,
            0.538m,
            0.538m + DecimalExtensions.Epsilon,
            0.538m - DecimalExtensions.Epsilon,
        ];

        private static readonly decimal[] expTestSource = [1m, 0m, -1m, 0.5m, -0.5m, 3.5m, -3.5m, DecimalExtensions.Epsilon, -DecimalExtensions.Epsilon, Int32.MinValue, Int32.MaxValue, Int32.MinValue - 1.5m, Int32.MaxValue + 1.5m];
        private static readonly decimal[] powETestSource = [0, 1, 2, 10, -1, -10, 0.1m, -0.1m, DecimalExtensions.Epsilon, -DecimalExtensions.Epsilon, Int16.MinValue, Int32.MinValue, Int64.MinValue, 66.500000000000000001m];
        private static readonly decimal[] powTestSource = [0.5m, -0.5m, 2, -2, 3, 10, 16, DecimalExtensions.Epsilon, -DecimalExtensions.Epsilon, 1m + 1e-15m, 1m - 1e-15m, Decimal.MaxValue, Decimal.MinValue];

        #endregion

        #region Methods

        #region Static Methods

        [AssertionMethod]
        private static void AreEqual(double expected, decimal actualDecimal)
        {
            var actual = (double)actualDecimal;
            Console.WriteLine($"{actualDecimal.ToRoundtripString()} (double: {expected.ToRoundtripString()})");
            Assert.IsTrue(expected.TolerantEquals(actual), $"{actual.ToRoundtripString()} <> {expected.ToRoundtripString()}");
        }

        [AssertionMethod]
        private static void AreEqual(string name, double expected, Func<decimal> actualDecimal)
        {
            try
            {
                decimal actual = actualDecimal.Invoke();
                Console.Write($"{name} = {actual.ToRoundtripString()}");
                Assert.IsTrue(expected.TolerantEquals((double)actual), $"{actual.ToRoundtripString()} <> {expected.ToRoundtripString()}");
            }
            catch (Exception e) when (e is not AssertionException)
            {
                Console.Write($"{name}: {e.Message}");
                Assert.IsTrue(e is OverflowException && (Double.IsNaN(expected) || Double.IsInfinity(expected) || expected > (double)Decimal.MaxValue || expected < (double)Decimal.MinValue)
                    || e is ArgumentOutOfRangeException && Double.IsNaN(expected), "OverflowException/ArgumentOutOfRangeException and NaN/Infinity/very large values are expected");
            }

            Console.WriteLine();
        }

        #endregion

        #region Instance Methods

        [TestCaseSource(nameof(powITestSource))]
        public void PowITest(decimal value, int power)
        {
            string name = $"Pow({value.ToRoundtripString()}, (int){power})";
            double expected = Math.Pow((double)value, power);
            Console.WriteLine($"Math.{name} = {expected.ToRoundtripString()}");
            AreEqual($"DecimalExtensions.{name}", expected, () => value.Pow(power));
        }

        [TestCaseSource(nameof(expTestSource))]
        public void ExpTest(decimal power)
        {
            string name = $"Exp({power})";
            double expected = Math.Exp((double)power);
            Console.WriteLine($"Math.{name} = {expected.ToRoundtripString()}");
            AreEqual($"DecimalExtensions.{name}", expected, () => power.Exp());
        }

        [TestCaseSource(nameof(logTestSource))]
        public void LogETest(decimal value)
        {
            Console.Write($"base e log of {value.ToRoundtripString()}: ");
            AreEqual(Math.Log((double)value), value.Log());
        }

        [TestCaseSource(nameof(logTestSource))]
        public void Log10Test(decimal value)
        {
            Console.Write($"base 10 log of {value.ToRoundtripString()}: ");
            AreEqual(Math.Log10((double)value), value.Log10());
        }

        [TestCaseSource(nameof(logTestSource))]
        public void LogTest(decimal value)
        {
            void TestLog(decimal d, decimal newBase)
            {
                Console.Write($"base {newBase} log of {d.ToRoundtripString()}: ");
                AreEqual(Math.Log((double)d, (double)newBase), d.Log(newBase));
            }

            TestLog(value, 2);
            TestLog(value, 3);
            TestLog(value, 10);
            TestLog(value, 16);
        }

        [TestCaseSource(nameof(powETestSource))]
        public void PowETest(decimal power)
        {
            Console.Write($"e raised to {power.ToRoundtripString()}: ");
            try
            {
                AreEqual(Math.Exp((double)power), power.Exp());
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                throw;
            }
        }

        [Test]
        public void PowETestOverflow() => Throws<OverflowException>(() => PowETest(66.6m));

        [TestCaseSource(nameof(powTestSource))]
        public void PowTest(decimal value)
        {
            static void TestPow(decimal value, decimal power)
            {
                string name = $"Pow({value.ToRoundtripString()}, {power})";
                double expected = Math.Pow((double)value, (double)power);
                Console.WriteLine($"Math.{name} = {expected.ToRoundtripString()}");
                AreEqual($"DecimalExtensions.{name}", expected, () => value.Pow(power));
                Console.WriteLine();
            }

            TestPow(value, 0);
            TestPow(value, 1);
            TestPow(value, -1);
            TestPow(value, 2);
            TestPow(value, -2);
            TestPow(value, 0.5m);
            TestPow(value, -0.5m);
            TestPow(value, 1.5m);
            TestPow(value, -1.5m);
            TestPow(value, 10);
            TestPow(value, -10);
            TestPow(value, 16);
            TestPow(value, -16);
            TestPow(value, 28);
            TestPow(value, -28);
            TestPow(value, DecimalExtensions.Epsilon);
            TestPow(value, -DecimalExtensions.Epsilon);
            TestPow(value, UInt32.MaxValue);
            TestPow(value, -UInt32.MaxValue);
            TestPow(value, UInt32.MaxValue + 0.5m);
            TestPow(value, -UInt32.MaxValue - 0.5m);
        }

        #endregion

        #endregion
    }
}
