using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using KGySoft.Libraries;
using KGySoft.Libraries.Collections;
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
            void Test(Random random, double min, double max)
            {
                for (FloatScale scale = 0; scale <= FloatScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random double {min.ToRoundtripString()}..{max.ToRoundtripString()} ({scale}): ");
                    double result;
                    try
                    {
                        result = random.NextDouble(min, max, scale);
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

            var rnd = new TestRandom();

            // edge cases
            Test(rnd, double.MinValue, double.MaxValue);
            Test(rnd, double.NegativeInfinity, double.PositiveInfinity);
            Test(rnd, 0, double.PositiveInfinity);
            Test(rnd, double.MaxValue, double.PositiveInfinity);
            Test(rnd, double.NegativeInfinity, double.MinValue);
            Throws<ArgumentOutOfRangeException>(() => Test(rnd, double.PositiveInfinity, double.PositiveInfinity));
            Throws<ArgumentOutOfRangeException>(() => Test(rnd, double.NegativeInfinity, double.NegativeInfinity));
            Throws<ArgumentOutOfRangeException>(() => Test(rnd, 0, double.NaN));
            Test(rnd, 0, double.Epsilon);
            Test(rnd, Double.Epsilon, Double.Epsilon * 4);
            Test(rnd, Double.MaxValue / 4, Double.MaxValue);
            Test(rnd, Double.MaxValue / 2, Double.MaxValue);
            Test(rnd, -Double.Epsilon, Double.Epsilon);
            Test(rnd, 0.000000001, 0.0000000011);
            Test(rnd, 10000, 11000);

            // big range
            Test(rnd, long.MinValue, long.MaxValue);
            Test(rnd, long.MinValue, 0);
            Test(rnd, long.MaxValue, float.MaxValue);
            Test(rnd, -0.1, ulong.MaxValue); // very imbalanced positive-negative ranges
            Test(rnd, 1L << 52, (1L << 54) + 10); // narrow exponent range
            Test(rnd, long.MaxValue, (double)long.MaxValue * 4 + 10000); // small exponent range
            Test(rnd, (double)long.MaxValue * 1024, (double)long.MaxValue * 4100); // small exponent range
            Test(rnd, (double)long.MinValue * 4100, (double)long.MinValue * 1024); // small exponent range

            // small range
            Test(rnd, long.MaxValue, (double)long.MaxValue * 4);
            Test(rnd, long.MaxValue, (double)long.MaxValue * 4 + 1000);
            Test(rnd, 1L << 53, (1L << 53) + 2);
            Test(rnd, 1L << 52, 1L << 53);
        }

        [TestMethod]
        public void NextFloatTest()
        {
            void Test(Random random, float min, float max)
            {
                for (FloatScale scale = 0; scale <= FloatScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random float {min.ToRoundtripString()}..{max.ToRoundtripString()} {scale}: ");
                    float result;
                    try
                    {
                        result = random.NextSingle(min, max, scale);
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

            var rnd = new TestRandom();
            Test(rnd.WithNextDoubles(1d), float.MinValue, float.MaxValue);
            Test(rnd.WithNextDoubles(null), float.MinValue, float.MaxValue);
            Test(rnd, 0, float.Epsilon);
            Test(rnd, float.MaxValue, float.PositiveInfinity);
            Test(rnd, float.NegativeInfinity, float.PositiveInfinity);
        }

        [TestMethod]
        public void NextDecimalTest()
        {
            var rnd = new Random();
            void Test(decimal min, decimal max)
            {
                for (FloatScale scale = 0; scale <= FloatScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random decimal {min.ToRoundtripString()}..{max.ToRoundtripString()} ({scale}): ");
                    decimal result;
                    try
                    {
                        result = rnd.NextDecimal(min, max, scale);
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

            // edge cases
            Test(decimal.MinValue, decimal.MaxValue);
            Test(0, decimal.MaxValue);
            Test(0, DecimalExtensions.Epsilon);
            Test(DecimalExtensions.Epsilon, DecimalExtensions.Epsilon * 4);
            Test(Decimal.MaxValue / 2, Decimal.MaxValue);
            Test(Decimal.MaxValue - 1, Decimal.MaxValue);
            Test(Decimal.MaxValue - DecimalExtensions.Epsilon, Decimal.MaxValue);
            Test(-DecimalExtensions.Epsilon, DecimalExtensions.Epsilon);
            Test(0.000000001m, 0.0000000011m);
            Test(10000, 11000);

            // big range
            Test(long.MinValue, long.MaxValue);
            Test(long.MinValue, 0);
            Test(-0.1m, ulong.MaxValue); // very imbalanced positive-negative ranges
            Test(1L << 52, (1L << 54) + 10); // narrow exponent range
            Test(long.MaxValue, (decimal)long.MaxValue * 4 + 10000); // small exponent range
            Test((decimal)long.MaxValue * 1024, (decimal)long.MaxValue * 4100); // small exponent range
            Test((decimal)long.MinValue * 4100, (decimal)long.MinValue * 1024); // small exponent range

            // small range
            Test(long.MaxValue, (decimal)long.MaxValue * 4);
            Test(long.MaxValue, (decimal)long.MaxValue * 4 + 1000);
            Test(1L << 53, (1L << 53) + 2);
            Test(1L << 52, 1L << 53);
        }

        [TestMethod]
        public void NextDateTimeOffsetTest()
        {
            var rnd = new Random();
            void Test(DateTimeOffset min, DateTimeOffset max)
            {
                Console.Write($@"Random DateTimeOffset {min:O}..{max:O}: ");
                DateTimeOffset result;
                try
                {
                    result = rnd.NextDateTimeOffset(min, max);
                    Console.WriteLine(result.ToString("O"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                    throw;
                }

                Assert.IsTrue(result >= min && result <= max);
            }

            Test(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
            Test(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);

            Test(DateTimeOffset.MinValue, DateTimeOffset.MinValue);
            Test(DateTimeOffset.MinValue, DateTimeOffset.MinValue.AddMinutes(1));
            Test(DateTimeOffset.MaxValue.AddMinutes(-1), DateTimeOffset.MaxValue);
            Test(DateTimeOffset.MaxValue.AddHours(-1), DateTimeOffset.MaxValue);
            Test(DateTimeOffset.MaxValue.AddDays(-1), DateTimeOffset.MaxValue);
        }

        private enum EmptyEnum { }
        private delegate void OutDelegate(out string s);

        private class Recursive
        {
            public Recursive Child { get; set; }
        }

        [TestMethod]
        public void NextObjectTest()
        {
            var rnd = new Random();
            void Test<T>(GenerateObjectSettings settings = null)
            {
                Console.WriteLine($"Random {typeof(T).Name}: {rnd.NextObject<T>(settings)}");
            }

            // native types
            Test<bool>();
            Test<byte>();
            Test<sbyte>();
            Test<char>();
            Test<short>();
            Test<ushort>();
            Test<int>();
            Test<uint>();
            Test<long>();
            Test<ulong>();
            Test<float>();
            Test<double>();
            Test<decimal>();
            Test<string>();
            Test<StringBuilder>();
            Test<Uri>();
            Test<Guid>();
            Test<DateTime>();
            Test<DateTimeOffset>();
            Test<TimeSpan>();
            Test<IntPtr>();
            Test<UIntPtr>();
            Test<byte?>();

            // enums
            Test<EmptyEnum>();
            Test<ConsoleColor>();
            Test<Enum>();

            // arrays
            Test<byte[]>();
            Test<byte?[]>();
            Test<byte[,]>();

            // collections
            Test<List<int>>(); // populate
            Test<Dictionary<int, string>>(); // populate
            Test<ArrayList>(); // populate
            Test<Hashtable>(); // populate
            Test<BitArray>(); // array ctor
            Test<ReadOnlyCollection<int>>(); // IList<T> ctor
            Test<ArraySegment<int>>(); // array ctor
            Test<Cache<int, int>>(); // populate
            Test<Queue>(); // ICollection ctor
            Test<CounterCreationDataCollection>(new GenerateObjectSettings { SubstitutionForObjectType = typeof(CounterCreationData) }); // populate, typed object

            // key-value
            Test<DictionaryEntry>();
            Test<KeyValuePair<int, string>>();

            // reflection types
            Test<Assembly>();
            Test<Type>();
            Test<MethodBase>();
            Test<MemberInfo>();

            // base types
            var cfg = new GenerateObjectSettings { AllowDerivedTypesForNonSealedClasses = true };
            Test<EventArgs>(cfg);
            Test<Exception>(cfg);

            // abstract types/interfaces
            Test<Enum>();
            Test<IConvertible>();

            // delegates
            Test<Delegate>();
            Test<MulticastDelegate>();
            Test<Func<int>>();
            Test<OutDelegate>();

            // recursive type
            Test<Recursive>();
        }
    }
}
