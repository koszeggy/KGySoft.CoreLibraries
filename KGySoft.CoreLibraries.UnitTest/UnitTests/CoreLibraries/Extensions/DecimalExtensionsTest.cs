#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DecimalExtensionsTest.cs
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

using KGySoft.Annotations;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class DecimalExtensionsTest : TestBase
    {
        #region Fields

        private static readonly decimal decimalEpsilon = new decimal(1, 0, 0, false, 28);
        private static readonly double diffTolerance = 1E-10d;
        private static readonly decimal[] logETestSource = { 1, 1.1m, 0.00000000000001m, 10m, Decimal.MaxValue, decimalEpsilon, 1m / decimalEpsilon, DecimalExtensions.E, DecimalExtensions.PI };
        private static readonly decimal[] log10TestSource = { 1, 1.1m, 0.00000000000001m, 10m, Decimal.MaxValue, decimalEpsilon, 1m / decimalEpsilon, DecimalExtensions.E, DecimalExtensions.PI };
        private static readonly decimal[] logTestSource = { 1, 2, 3, 4, 8, 9, 10, 27, 128, 256, 1 << 16, 1L << 62, 1.1m, 0.00000000000001m, Decimal.MaxValue, decimalEpsilon, 1m / decimalEpsilon, DecimalExtensions.E, DecimalExtensions.PI };
        private static readonly decimal[] powETestSource = { 0, 1, 2, 10, -1, -10, 0.1m, -0.1m, decimalEpsilon, -decimalEpsilon, Int16.MinValue, Int32.MinValue, Int64.MinValue, 66.500000000000000001m };
        private static readonly decimal[] powTestSource = { 0.5m, -0.5m, 2, -2, 3, 10, 16 };

        #endregion

        #region Methods

        #region Static Methods

        [AssertionMethod]
        private static void AreEqual(double expected, decimal actualDecimal)
        {
            var actual = (double)actualDecimal;
            Console.WriteLine($"{actualDecimal.ToRoundtripString()} (double: {expected.ToRoundtripString()})");
            Assert.IsTrue(Math.Max(expected, actual) - Math.Min(expected, actual) <= diffTolerance, $"{actual.ToRoundtripString()} <> {expected.ToRoundtripString()}");
        }

        #endregion

        #region Instance Methods

        [TestCaseSource(nameof(logETestSource))]
        public void LogETest(decimal value)
        {
            Console.Write($"base e log of {value.ToRoundtripString()}: ");
            AreEqual(Math.Log((double)value), value.Log());
        }

        [TestCaseSource(nameof(log10TestSource))]
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
                Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                throw;
            }
        }

        [Test]
        public void PowETestOverflow() => Throws<OverflowException>(() => PowETest(66.6m));

        [TestCaseSource(nameof(powTestSource))]
        public void PowTest(decimal value)
        {
            void TestPow(decimal d, decimal power)
            {
                Console.Write($"{d} raised to {power.ToRoundtripString()}: ");
                var doubleResult = Math.Pow((double)d, (double)power);
                try
                {
                    AreEqual(doubleResult, d.Pow(power));
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                    if (d < 0 && power != Math.Round(power))
                        Assert.IsInstanceOf(typeof(ArgumentOutOfRangeException), e);
                    else if (doubleResult > (double)decimal.MaxValue || doubleResult < (double)decimal.MinValue)
                        Assert.IsInstanceOf(typeof(OverflowException), e);
                    else
                        throw;
                }
            }

            TestPow(value, 0);
            TestPow(value, 1);
            TestPow(value, -1);
            TestPow(value, 0.5m);
            TestPow(value, -0.5m);
            TestPow(value, 10);
            TestPow(value, -10);
            TestPow(value, 16);
            TestPow(value, -16);
            TestPow(value, 28);
            TestPow(value, -28);
        }

        #endregion

        #endregion
    }
}
