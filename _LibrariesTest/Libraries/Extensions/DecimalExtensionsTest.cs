using System;
using KGySoft.Annotations;
using KGySoft.Libraries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Extensions
{
    [TestClass]
    public class DecimalExtensionsTest : TestBase
    {
        private static readonly decimal decimalEpsilon = new decimal(1, 0, 0, false, 28);
        private static readonly double diffTolerance = 1E-10d;

        [TestMethod]
        public void LogETest()
        {
            TestLogE(1);
            TestLogE(1.1m);
            TestLogE(0.00000000000001m);
            TestLogE(10m);
            TestLogE(decimal.MaxValue);
            TestLogE(decimalEpsilon);
            TestLogE(1m / decimalEpsilon);
            TestLogE(DecimalExtensions.E);
            TestLogE(DecimalExtensions.PI);
        }

        [TestMethod]
        public void Log10Test()
        {
            TestLog10(1);
            TestLog10(1.1m);
            TestLog10(0.00000000000001m);
            TestLog10(10m);
            TestLog10(decimal.MaxValue);
            TestLog10(decimalEpsilon);
            TestLog10(1m / decimalEpsilon);
            TestLog10(DecimalExtensions.E);
            TestLog10(DecimalExtensions.PI);
        }

        [TestMethod]
        public void Log2Test() => LogTest(2);

        [TestMethod]
        public void Log3Test() => LogTest(3);

        [TestMethod]
        public void Log16Test() => LogTest(16);

        [TestMethod]
        public void PowETest()
        {
            TestPowE(0);
            TestPowE(1);
            TestPowE(2);
            TestPowE(10);
            TestPowE(-1);
            TestPowE(-10);
            TestPowE(0.1m);
            TestPowE(-0.1m);
            TestPowE(decimalEpsilon);
            TestPowE(-decimalEpsilon);
            TestPowE(short.MinValue);
            TestPowE(int.MinValue);
            TestPowE(long.MinValue);
            TestPowE(66.500000000000000001m);
            Throws<OverflowException>(() => TestPowE(66.6m));
        }

        [TestMethod]
        public void Pow05Test() => PowTest(0.5m);

        [TestMethod]
        public void Pow_05Test() => PowTest(-0.5m);

        [TestMethod]
        public void Pow2Test() => PowTest(2);

        [TestMethod]
        public void Pow_2Test() => PowTest(-2);

        [TestMethod]
        public void Pow3Test() => PowTest(3);

        [TestMethod]
        public void Pow10Test() => PowTest(10);

        [TestMethod]
        public void Pow16Test() => PowTest(16);

        private void LogTest(decimal @base)
        {
            TestLog(1, @base);
            TestLog(2, @base);
            TestLog(3, @base);
            TestLog(4, @base);
            TestLog(8, @base);
            TestLog(9, @base);
            TestLog(10m, @base);
            TestLog(27, @base);
            TestLog(128, @base);
            TestLog(256, @base);
            TestLog(1 << 16, @base);
            TestLog(1 << 64, @base);
            TestLog(1.1m, @base);
            TestLog(0.00000000000001m, @base);
            TestLog(decimal.MaxValue, @base);
            TestLog(decimalEpsilon, @base);
            TestLog(1m / decimalEpsilon, @base);
            TestLog(DecimalExtensions.E, @base);
            TestLog(DecimalExtensions.PI, @base);
        }

        private void TestPowE(decimal power)
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

        private void PowTest(decimal value)
        {
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

        private void TestPow(decimal value, decimal power)
        {
            Console.Write($"{value} raised to {power.ToRoundtripString()}: ");
            var doubleResult = Math.Pow((double)value, (double)power);
            try
            {
                AreEqual(doubleResult, value.Pow(power));
            }
            catch (Exception e)
            {
                Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                if (value < 0 && power != Math.Round(power))
                    Assert.IsInstanceOfType(e, typeof(ArgumentOutOfRangeException));
                else if (doubleResult > (double)decimal.MaxValue || doubleResult < (double)decimal.MinValue)
                    Assert.IsInstanceOfType(e, typeof(OverflowException));
                else
                    throw;
            }
        }

        private void TestLogE(decimal value)
        {
            Console.Write($"base e log of {value.ToRoundtripString()}: ");
            AreEqual(Math.Log((double)value), value.Log());
        }

        private void TestLog(decimal value, decimal newBase)
        {
            Console.Write($"base {newBase} log of {value.ToRoundtripString()}: ");
            AreEqual(Math.Log((double)value, (double)newBase), value.Log(newBase));
        }

        private void TestLog10(decimal value)
        {
            Console.Write($"base 10 log of {value.ToRoundtripString()}: ");
            AreEqual(Math.Log10((double)value), value.Log10());
        }

        [AssertionMethod]
        private void AreEqual(double expected, decimal actualDecimal)
        {
            var actual = (double)actualDecimal;
            Console.WriteLine($"{actualDecimal.ToRoundtripString()} (double: {expected.ToRoundtripString()})");
            Assert.IsTrue(Math.Max(expected, actual) - Math.Min(expected, actual) <= diffTolerance, $"{actual.ToRoundtripString()} <> {expected.ToRoundtripString()}");
        }
    }
}
