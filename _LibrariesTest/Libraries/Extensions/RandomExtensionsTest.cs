using System;
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
                Console.Write($@"Random double {min.ToRoundtripString()}..{max.ToRoundtripString()}: ");
                double result;
                try
                {
                    result = this.NextDouble(min, max);
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

            // big range
            rnd.WithNextDoubles(0.99999999999999989).WithNextIntegers(63).TestDouble(0, long.MaxValue);
            rnd.WithNextDoubles(0.99999999999999989).WithNextIntegers(63).TestDouble(long.MinValue, long.MaxValue);
            rnd.WithNextDoubles(0).WithNextIntegers(63).TestDouble(long.MinValue, long.MaxValue);
            rnd.WithNextDoubles(0).WithNextIntegers(63).TestDouble(long.MinValue, 0);
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(long.MaxValue, float.MaxValue);
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(-0.1, ulong.MaxValue); // worst case with very imbalanced positive-negative ranges
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(1L << 52, (1L << 54) + 10); // narrow exponent range
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(long.MaxValue, (double)long.MaxValue * 4 + 10000); // worst case with effectively small exponent range
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble((double)long.MaxValue * 1024, (double)long.MaxValue * 4100); // worst case with effectively small exponent range
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble((double)long.MinValue * 4100, (double)long.MinValue * 1024); // worst case with effectively small exponent range

            // small range
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(long.MaxValue, (double)long.MaxValue * 4); // small
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(long.MaxValue, (double)long.MaxValue * 4 + 1000); // small
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(1L << 53, (1L << 53) + 2); // small

            // mid range
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(1L << 52, 1L << 53); // mid
            rnd.WithNextDoubles(null).WithNextIntegers(null).TestDouble(1L << 53, (1L << 53) + 4); // mid
        }
    }
}
