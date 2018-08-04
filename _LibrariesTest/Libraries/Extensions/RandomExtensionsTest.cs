using System;
using System.IO;
using System.Xml.Linq;
using KGySoft.Libraries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Extensions
{
    [TestClass]
    public class RandomExtensionsTest : TestBase
    {
        private class TestRandom : Random
        {
            private int nextBytePtr;
            private byte[] nextBytes;
            private int nextDoublePtr;
            private double[] nextDoubles;
            private int nextIntPtr;
            private int[] nextIntegers;

            internal TestRandom WithNextBytes(params byte[] nextBytes)
            {
                this.nextBytes = nextBytes;
                nextBytePtr = 0;
                return this;
            }

            internal TestRandom WithNextDoubles(params double[] nextDoubles)
            {
                this.nextDoubles = nextDoubles;
                nextDoublePtr = 0;
                return this;
            }

            internal TestRandom WithNextIntegers(params int[] nextIntegers)
            {
                this.nextIntegers = nextIntegers;
                nextIntPtr = 0;
                return this;
            }

            public override void NextBytes(byte[] buffer)
            {
                if (nextBytes == null)
                {
                    base.NextBytes(buffer);
                    return;
                }

                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = nextBytes[nextBytePtr++ % nextBytes.Length];
            }

            public override double NextDouble() => nextDoubles?[nextDoublePtr++ % nextDoubles.Length] ?? base.NextDouble();
            public override int Next() => nextIntegers?[nextIntPtr++ % nextIntegers.Length] ?? base.Next();
            public override int Next(int maxValue) => nextIntegers == null ? base.Next(maxValue) : Next();
            public override int Next(int minValue, int maxValue) => nextIntegers == null ? base.Next(minValue, maxValue) : Next();

            public void TestDouble(double min, double max)
            {
                for (RandomScale scale = 0; scale <= RandomScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random double {min.ToRoundtripString()}..{max.ToRoundtripString()} ({scale}): ");
                    double result;
                    try
                    {
                        result = this.NextDouble(min, max, scale);
                        Console.WriteLine(result.ToRoundtripString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                        throw;
                    }

                    Assert.IsTrue(result >= min && result <= max);
                }
            }

            public void TestFloat(float min, float max)
            {
                for (RandomScale scale = 0; scale <= RandomScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random float {min.ToRoundtripString()}..{max.ToRoundtripString()} {scale}: ");
                    float result;
                    try
                    {
                        result = this.NextSingle(min, max, scale);
                        Console.WriteLine(result.ToRoundtripString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                        throw;
                    }

                    Assert.IsTrue(result >= min && result <= max);
                }
            }

            public void TestDecimal(decimal min, decimal max)
            {
                for (RandomScale scale = 0; scale <= RandomScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random decimal {min.ToRoundtripString()}..{max.ToRoundtripString()} ({scale}): ");
                    decimal result;
                    try
                    {
                        result = this.NextDecimal(min, max, scale);
                        Console.WriteLine(result.ToRoundtripString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                        throw;
                    }

                    Assert.IsTrue(result >= min && result <= max);
                }
            }

            public void TestDateTimeOffset(DateTimeOffset min, DateTimeOffset max)
            {
                Console.Write($@"Random DateTimeOffset {min:O}..{max:O}: ");
                DateTimeOffset result;
                try
                {
                    result = this.NextDateTimeOffset(min, max);
                    Console.WriteLine(result.ToString("O"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                    throw;
                }

                Assert.IsTrue(result >= min && result <= max);
            }
        }


        [TestMethod]
        public void NextUInt64Test()
        {
            // full range
            var rnd = new TestRandom().WithNextBytes(0);
            Assert.AreEqual(0UL, rnd.NextUInt64());

            rnd.WithNextBytes(255);
            Assert.AreEqual(ulong.MaxValue, rnd.NextUInt64());

            // min-max
            rnd.WithNextBytes(null);
            Throws<ArgumentOutOfRangeException>(() => rnd.NextUInt64(1, 0));

            var result = rnd.NextUInt64(0, 10);
            Assert.IsTrue(result >= 0 && result < 10);
        }

        [TestMethod]
        public void NextInt64Test()
        {
            // full range
            var rnd = new TestRandom().WithNextBytes(0);
            Assert.AreEqual(0L, rnd.NextInt64());

            rnd.WithNextBytes(255);
            Assert.AreEqual(-1, rnd.NextInt64());

            // min-max
            rnd.WithNextBytes(null);
            Throws<ArgumentOutOfRangeException>(() => rnd.NextInt64(1, 0));

            var result = rnd.NextInt64(-5, 5);
            Assert.IsTrue(result >= -5 && result < 5);
        }

        [TestMethod]
        public void NextDoubleTest()
        {
            var rnd = new TestRandom();

            // edge cases
            rnd.TestDouble(double.MinValue, double.MaxValue);
            rnd.TestDouble(double.NegativeInfinity, double.PositiveInfinity);
            rnd.TestDouble(0, double.PositiveInfinity);
            rnd.TestDouble(double.MaxValue, double.PositiveInfinity);
            rnd.TestDouble(double.NegativeInfinity, double.MinValue);
            Throws<ArgumentOutOfRangeException>(() => rnd.TestDouble(double.PositiveInfinity, double.PositiveInfinity));
            Throws<ArgumentOutOfRangeException>(() => rnd.TestDouble(double.NegativeInfinity, double.NegativeInfinity));
            Throws<ArgumentOutOfRangeException>(() => rnd.TestDouble(0, double.NaN));
            rnd.TestDouble(0, double.Epsilon);
            rnd.TestDouble(Double.Epsilon, Double.Epsilon * 4);
            rnd.TestDouble(Double.MaxValue / 4, Double.MaxValue);
            rnd.TestDouble(Double.MaxValue / 2, Double.MaxValue);
            rnd.TestDouble(-Double.Epsilon, Double.Epsilon);
            rnd.TestDouble(0.000000001, 0.0000000011);
            rnd.TestDouble(10000, 11000);

            // big range
            rnd.TestDouble(long.MinValue, long.MaxValue);
            rnd.TestDouble(long.MinValue, 0);
            rnd.TestDouble(long.MaxValue, float.MaxValue);
            rnd.TestDouble(-0.1, ulong.MaxValue); // very imbalanced positive-negative ranges
            rnd.TestDouble(1L << 52, (1L << 54) + 10); // narrow exponent range
            rnd.TestDouble(long.MaxValue, (double)long.MaxValue * 4 + 10000); // small exponent range
            rnd.TestDouble((double)long.MaxValue * 1024, (double)long.MaxValue * 4100); // small exponent range
            rnd.TestDouble((double)long.MinValue * 4100, (double)long.MinValue * 1024); // small exponent range

            // small range
            rnd.TestDouble(long.MaxValue, (double)long.MaxValue * 4);
            rnd.TestDouble(long.MaxValue, (double)long.MaxValue * 4 + 1000);
            rnd.TestDouble(1L << 53, (1L << 53) + 2);
            rnd.TestDouble(1L << 52, 1L << 53);
        }

        [TestMethod]
        public void NextFloatTest()
        {
            var rnd = new TestRandom();

            rnd.WithNextDoubles(1d).TestFloat(float.MinValue, float.MaxValue);
            rnd.WithNextDoubles(null).TestFloat(float.MinValue, float.MaxValue);
            rnd.TestFloat(0, float.Epsilon);
            rnd.TestFloat(float.MaxValue, float.PositiveInfinity);
            rnd.TestFloat(float.NegativeInfinity, float.PositiveInfinity);
        }

        [TestMethod]
        public void NextDecimalTest()
        {
            var rnd = new TestRandom();

            // edge cases
            rnd.TestDecimal(decimal.MinValue, decimal.MaxValue);
            rnd.TestDecimal(0, decimal.MaxValue);
            rnd.TestDecimal(0, DecimalExtensions.Epsilon);
            rnd.TestDecimal(DecimalExtensions.Epsilon, DecimalExtensions.Epsilon * 4);
            rnd.TestDecimal(Decimal.MaxValue / 2, Decimal.MaxValue);
            rnd.TestDecimal(Decimal.MaxValue - 1, Decimal.MaxValue);
            rnd.TestDecimal(Decimal.MaxValue - DecimalExtensions.Epsilon, Decimal.MaxValue);
            rnd.TestDecimal(-DecimalExtensions.Epsilon, DecimalExtensions.Epsilon);
            rnd.TestDecimal(0.000000001m, 0.0000000011m);
            rnd.TestDecimal(10000, 11000);

            // big range
            rnd.TestDecimal(long.MinValue, long.MaxValue);
            rnd.TestDecimal(long.MinValue, 0);
            rnd.TestDecimal(-0.1m, ulong.MaxValue); // very imbalanced positive-negative ranges
            rnd.TestDecimal(1L << 52, (1L << 54) + 10); // narrow exponent range
            rnd.TestDecimal(long.MaxValue, (decimal)long.MaxValue * 4 + 10000); // small exponent range
            rnd.TestDecimal((decimal)long.MaxValue * 1024, (decimal)long.MaxValue * 4100); // small exponent range
            rnd.TestDecimal((decimal)long.MinValue * 4100, (decimal)long.MinValue * 1024); // small exponent range

            // small range
            rnd.TestDecimal(long.MaxValue, (decimal)long.MaxValue * 4);
            rnd.TestDecimal(long.MaxValue, (decimal)long.MaxValue * 4 + 1000);
            rnd.TestDecimal(1L << 53, (1L << 53) + 2);
            rnd.TestDecimal(1L << 52, 1L << 53);
        }

        [TestMethod]
        public void NextDateTimeOffsetTest()
        {
            var rnd = new TestRandom();

            rnd.TestDateTimeOffset(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
            rnd.TestDateTimeOffset(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);

            rnd.TestDateTimeOffset(DateTimeOffset.MinValue, DateTimeOffset.MinValue);
            rnd.TestDateTimeOffset(DateTimeOffset.MinValue, DateTimeOffset.MinValue.AddMinutes(1));
            rnd.TestDateTimeOffset(DateTimeOffset.MaxValue.AddMinutes(-1), DateTimeOffset.MaxValue);
            rnd.TestDateTimeOffset(DateTimeOffset.MaxValue.AddHours(-1), DateTimeOffset.MaxValue);
            rnd.TestDateTimeOffset(DateTimeOffset.MaxValue.AddDays(-1), DateTimeOffset.MaxValue);
        }

        [TestMethod]
        public void ValuesTest()
        {
            var rnd = new Random(0);
            //Console.WriteLine(Math.Log(Enumerable.Range(0, 10000).Select(_ => rnd.NextDouble()).Min(), 2));
            XElement result = new XElement("root");
            for (int i = 0; i < 10000; i++)
            {
                result.Add(new XElement("item", rnd.NextDouble(int.MaxValue, RandomScale.ForceLogarithmic)));
            }

            using (var file = File.Create(Files.GetNextFileName($@"D:\temp\rnd\NextDouble_0-MaxInt32_Log.xml")))
            {
                result.Save(file);
            }
        }
    }
}
