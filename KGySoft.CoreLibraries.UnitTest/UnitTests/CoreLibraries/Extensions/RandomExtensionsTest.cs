#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensionsTest.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
#if NETFRAMEWORK
using System.Diagnostics;
#endif
#if !NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
#if !NET35
using System.Numerics;
#endif
using System.Reflection;
#if NETFRAMEWORK
using System.Security;
using System.Security.Permissions;
#endif
using System.Text;
using System.Xml.Schema;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class RandomExtensionsTest : TestBase
    {
        #region Nested types

        #region Enumerations

        private enum EmptyEnum { }

        #endregion

        #region Delegates

        private delegate void OutDelegate(out string s);

        #endregion

        #region Nested classes

        #region Recursive class

        private class Recursive
        {
            #region Properties

            public Recursive Child { get; set; }

            #endregion
        }

        #endregion

        #region RecursiveCollection class

        private class RecursiveCollection : Collection<RecursiveCollection>
        {
        }

        #endregion

        #region Sandbox class

#if NETFRAMEWORK
        private class Sandbox : MarshalByRefObject
        {
            internal void NextObjectTest() => new RandomExtensionsTest().NextObjectTest();
        } 
#endif

        #endregion

        #endregion

        #endregion

        #region Methods

        [Test]
        public void NextInt32Test()
        {
            var rnd = new FastRandom();

            // min > max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextInt32(1, 0));

            var result = rnd.NextInt32(1, 2);
            Assert.AreEqual(1, result);
            result = rnd.NextInt32(Int32.MaxValue - 1, Int32.MaxValue);
            Assert.AreEqual(Int32.MaxValue - 1, result);

            // big range
            result = rnd.NextInt32(-10, Int32.MaxValue);
            Assert.IsTrue(result >= -10 && result < Int32.MaxValue);

            result = rnd.NextInt32(Int32.MaxValue, true);
            Assert.IsTrue(result >= 0);

            // no shift, largest possible range
            result = rnd.NextInt32(Int32.MinValue, Int32.MaxValue, false);
            Assert.IsTrue(result < Int32.MaxValue);

            // fallback to random bytes
            rnd.NextInt32(Int32.MinValue, Int32.MaxValue, true);

            // shift, largest possible range
            result = rnd.NextInt32(Int32.MinValue + 1, Int32.MaxValue, true);
            Assert.IsTrue(result > Int32.MinValue);

            // no range
            result = rnd.NextInt32(1, 1);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void NextUInt32Test()
        {
            var rnd = new Random();

            // min > max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextUInt32(1, 0));

            // no range
            uint result = rnd.NextUInt32(1, 1);
            Assert.AreEqual(1U, result);

            // 1 range
            result = rnd.NextUInt32(1, 2);
            Assert.AreEqual(1, result);
            result = rnd.NextUInt32(UInt32.MaxValue - 1, UInt32.MaxValue);
            Assert.AreEqual(UInt32.MaxValue - 1, result);

            for (int i = 0; i < 10_000; i++)
            {
                // small range
                result = rnd.NextUInt32(100);
                Assert.IsTrue(result < 100);
                result = rnd.NextUInt32(10, 100);
                Assert.IsTrue(result >= 10 && result < 100);

                // big range
                result = rnd.NextUInt32((uint)Int32.MaxValue + 100);
                Assert.IsTrue(result < (uint)Int32.MaxValue + 100);
                result = rnd.NextUInt32(10, (uint)Int32.MaxValue + 100);
                Assert.IsTrue(result >= 10 && result < (uint)Int32.MaxValue + 100);
            }
        }

        [Test]
#if !NET6_0_OR_GREATER
        [SuppressMessage("ReSharper", "InvokeAsExtensionMethod", Justification = "That would call the virtual NextInt64 in .NET 6 and above")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "ReSharper issue")] 
#endif
        public void NextInt64Test()
        {
            var rnd = new Random();

            // min > max
            Throws<ArgumentOutOfRangeException>(() => RandomExtensions.NextInt64(rnd, 1, 0));

            // no range
            long result = RandomExtensions.NextInt64(rnd, 0);
            Assert.AreEqual(0L, result);
            result = RandomExtensions.NextInt64(rnd, 1, 1);
            Assert.AreEqual(1L, result);

            // 1 range
            result = RandomExtensions.NextInt64(rnd, 1);
            Assert.AreEqual(0L, result);
            result = RandomExtensions.NextInt64(rnd, 1, 2);
            Assert.AreEqual(1L, result);

            for (int i = 0; i < 10_000; i++)
            {
                // small range
                result = RandomExtensions.NextInt64(rnd, 10);
                Assert.IsTrue(result >= 0 && result < 10);
                result = RandomExtensions.NextInt64(rnd, -5, 5);
                Assert.IsTrue(result >= -5 && result < 5);

                // medium range (UInt32)
                result = RandomExtensions.NextInt64(rnd, Int32.MaxValue + 5L);
                Assert.IsTrue(result >= 0L && result < Int32.MaxValue + 5L);
                result = RandomExtensions.NextInt64(rnd, -5, Int32.MaxValue);
                Assert.IsTrue(result >= -5 && result < Int32.MaxValue);

                // big range
                result = rnd.NextInt64(UInt32.MaxValue + 5L, true);
                Assert.IsTrue(result >= 0L && result <= UInt32.MaxValue + 5L);
                result = rnd.NextInt64(-1, Int64.MaxValue, true);
                Assert.IsTrue(result >= -1L);
            }
        }

        [Test]
        public void NextUInt64Test()
        {
            var rnd = new Random();

            // min > max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextUInt64(1, 0));

            // no range
            ulong result = rnd.NextUInt64(0);
            Assert.AreEqual(0UL, result);
            result = rnd.NextUInt64(1, 1);
            Assert.AreEqual(1UL, result);

            // 1 range
            result = rnd.NextUInt64(1);
            Assert.AreEqual(0UL, result);
            result = rnd.NextUInt64(1, 2);
            Assert.AreEqual(1UL, result);

            for (int i = 0; i < 10_000; i++)
            {
                // small range
                result = rnd.NextUInt64(0, 10);
                Assert.IsTrue(result < 10UL);
                result = rnd.NextUInt64(5, 15);
                Assert.IsTrue(result >= 5UL && result < 15UL);

                // medium range (UInt32)
                result = rnd.NextUInt64(Int32.MaxValue + 5L);
                Assert.IsTrue(result < Int32.MaxValue + 5L);
                result = rnd.NextUInt64(5, Int32.MaxValue + 15UL);
                Assert.IsTrue(result >= 5UL && result < Int32.MaxValue + 15UL);

                // big range
                result = rnd.NextUInt64(UInt32.MaxValue + 5UL, true);
                Assert.IsTrue(result <= UInt32.MaxValue + 5UL);
                result = rnd.NextUInt64(5, Int64.MaxValue + 15UL, true);
                Assert.IsTrue(result >= 5UL && result <= Int64.MaxValue + 15UL);
            }
        }

#if !NET35
        [Test]
        public void NextBigIntegerTest()
        {
            var rnd = new Random();

            // min > max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextBigInteger(BigInteger.One, BigInteger.Zero));

            // no range
            BigInteger result = rnd.SampleBigInteger(0);
            Assert.AreEqual(BigInteger.Zero, result);
            result = rnd.NextBigInteger(0);
            Assert.AreEqual(BigInteger.Zero, result);
            result = rnd.NextBigInteger(BigInteger.One, BigInteger.One);
            Assert.AreEqual(BigInteger.One, result);

            // 1 range
            result = rnd.NextBigInteger(BigInteger.One);
            Assert.AreEqual(BigInteger.Zero, result);
            result = rnd.NextBigInteger(BigInteger.One, new BigInteger(2));
            Assert.AreEqual(BigInteger.One, result);

            for (int i = 0; i < 10_000; i++)
            {
                // small range
                result = rnd.SampleBigInteger(1);
                Assert.IsTrue(result >= 0 && result <= 255);
                result = rnd.SampleBigInteger(1, true);
                Assert.IsTrue(result >= -128 && result <= 127);
                result = rnd.NextBigInteger(10);
                Assert.IsTrue(result >= 0 && result < 10);
                result = rnd.NextBigInteger(-5, 5);
                Assert.IsTrue(result >= -5 && result < 5);

                // medium range (UInt32, fits in Sign)
                result = rnd.SampleBigInteger(4);
                Assert.IsTrue(result >= 0U && result <= UInt32.MaxValue);
                result = rnd.SampleBigInteger(4, true);
                Assert.IsTrue(result >= Int32.MinValue && result <= Int32.MaxValue);
                result = rnd.NextBigInteger(Int32.MaxValue + 5L);
                Assert.IsTrue(result >= 0L && result < Int32.MaxValue + 5L);
                result = rnd.NextBigInteger(-5, Int32.MaxValue);
                Assert.IsTrue(result >= -5 && result < Int32.MaxValue);

                // big range
                BigInteger maxDecimal = new BigInteger(Decimal.MaxValue);
                result = rnd.NextBigInteger(maxDecimal, true);
                Assert.IsTrue(result >= 0L && result <= maxDecimal);
                result = rnd.NextBigInteger(-maxDecimal, maxDecimal, true);
                Assert.IsTrue(result >= -maxDecimal && result <= maxDecimal);
            }
        }
#endif

        [Test]
        public void NextDoubleTest()
        {
            static void Test(Random random, double min, double max)
            {
                for (FloatScale scale = 0; scale <= FloatScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random double {min.ToRoundtripString()}..{max.ToRoundtripString()} ({scale}): ");
                    double result;
                    try
                    {
                        result = min.Equals(0d) ? random.NextDouble(max, scale) : random.NextDouble(min, max, scale);
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

            var rnd = new Random();

            // edge cases
            Test(rnd, Double.MinValue, Double.MaxValue);
            Test(rnd, Double.NegativeInfinity, Double.PositiveInfinity);
            Test(rnd, 0, Double.PositiveInfinity);
            Test(rnd, Double.MaxValue, Double.PositiveInfinity);
            Test(rnd, Double.NegativeInfinity, Double.MinValue);
            Test(rnd, 1.7976931348623155E+308, Double.MaxValue);
            Throws<ArgumentOutOfRangeException>(() => Test(rnd, Double.PositiveInfinity, Double.PositiveInfinity));
            Throws<ArgumentOutOfRangeException>(() => Test(rnd, Double.NegativeInfinity, Double.NegativeInfinity));
            Throws<ArgumentOutOfRangeException>(() => Test(rnd, 0, Double.NaN));
            Test(rnd, 0, Double.Epsilon);
            Test(rnd, Double.Epsilon, Double.Epsilon * 4);
            Test(rnd, Double.MaxValue / 4, Double.MaxValue);
            Test(rnd, Double.MaxValue / 2, Double.MaxValue);
            Test(rnd, -Double.Epsilon, Double.Epsilon);
            Test(rnd, 0.000000001, 0.0000000011);
            Test(rnd, 10000, 11000);

            // big range
            Test(rnd, Int64.MinValue, Int64.MaxValue);
            Test(rnd, Int64.MinValue, 0);
            Test(rnd, Int64.MaxValue, float.MaxValue);
            Test(rnd, -0.1, UInt64.MaxValue); // very imbalanced positive-negative ranges
            Test(rnd, 1L << 52, (1L << 54) + 10); // narrow exponent range
            Test(rnd, Int64.MaxValue, (double)Int64.MaxValue * 4 + 10000); // small exponent range
            Test(rnd, (double)Int64.MaxValue * 1024, (double)Int64.MaxValue * 4100); // small exponent range
            Test(rnd, (double)Int64.MinValue * 4100, (double)Int64.MinValue * 1024); // small exponent range

            // small range
            Test(rnd, Int64.MaxValue, (double)Int64.MaxValue * 4);
            Test(rnd, Int64.MaxValue, (double)Int64.MaxValue * 4 + 1000);
            Test(rnd, 1L << 53, (1L << 53) + 2);
            Test(rnd, 1L << 52, 1L << 53);
        }

        [Test]
        public void NextFloatTest()
        {
            static void Test(Random random, float min, float max)
            {
                for (FloatScale scale = 0; scale <= FloatScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random float {min.ToRoundtripString()}..{max.ToRoundtripString()} {scale}: ");
                    float result;
                    try
                    {
                        result = min.Equals(0f) ? random.NextSingle(max, scale) : random.NextSingle(min, max, scale);
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

            var rnd = new Random();
            Test(rnd, Single.MinValue, Single.MaxValue);
            Test(rnd, 0, Single.Epsilon);
            Test(rnd, Single.MaxValue, Single.PositiveInfinity);
            Test(rnd, Single.NegativeInfinity, Single.PositiveInfinity);
        }

#if NET5_0_OR_GREATER
        [Test]
        public void NextHalfTest()
        {
            static void Test(Random random, Half min, Half max)
            {
                for (FloatScale scale = FloatScale.ForceLinear; scale <= FloatScale.ForceLogarithmic; scale++)
                {
                    Console.Write($@"Random Half {min.ToStringInternal(CultureInfo.InvariantCulture)}..{max.ToStringInternal(CultureInfo.InvariantCulture)} {scale}: ");
                    Half result;
                    try
                    {
                        result = min.Equals((Half)0f) ? random.NextHalf(max, scale) : random.NextHalf(min, max, scale);
                        Console.WriteLine(result.ToStringInternal(CultureInfo.InvariantCulture));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                        throw;
                    }

                    Assert.IsTrue(result >= min && result <= max);
                }
            }

            var rnd = new Random();
            Test(rnd, Half.MinValue, Half.MaxValue);
            Test(rnd, (Half)0f, Half.Epsilon);
            Test(rnd, Half.MaxValue, Half.PositiveInfinity);
            Test(rnd, Half.NegativeInfinity, Half.PositiveInfinity);
        }
#endif

        [Test]
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
                        result = min.Equals(0m) ? rnd.NextDecimal(max, scale) : rnd.NextDecimal(min, max, scale);
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
            Test(Decimal.MinValue, Decimal.MaxValue);
            Test(0, Decimal.MaxValue);
            Test(0, DecimalExtensions.Epsilon);
            Test(DecimalExtensions.Epsilon, DecimalExtensions.Epsilon * 4);
            Test(Decimal.MaxValue / 2, Decimal.MaxValue);
            Test(Decimal.MaxValue - 1, Decimal.MaxValue);
            Test(Decimal.MaxValue - DecimalExtensions.Epsilon, Decimal.MaxValue);
            Test(-DecimalExtensions.Epsilon, DecimalExtensions.Epsilon);
            Test(0.000000001m, 0.0000000011m);
            Test(10000, 11000);

            // big range
            Test(Int64.MinValue, Int64.MaxValue);
            Test(Int64.MinValue, 0);
            Test(-0.1m, UInt64.MaxValue); // very imbalanced positive-negative ranges
            Test(1L << 52, (1L << 54) + 10); // narrow exponent range
            Test(Int64.MaxValue, (decimal)Int64.MaxValue * 4 + 10000); // small exponent range
            Test((decimal)Int64.MaxValue * 1024, (decimal)Int64.MaxValue * 4100); // small exponent range
            Test((decimal)Int64.MinValue * 4100, (decimal)Int64.MinValue * 1024); // small exponent range

            // small range
            Test(Int64.MaxValue, (decimal)Int64.MaxValue * 4);
            Test(Int64.MaxValue, (decimal)Int64.MaxValue * 4 + 1000);
            Test(1L << 53, (1L << 53) + 2);
            Test(1L << 52, 1L << 53);
        }

        [Test]
        public void NextDateTimeTest()
        {
            var rnd = new Random();
            void Test(DateTime min, DateTime max)
            {
                Console.Write($@"Random DateTime {min:O}..{max:O}: ");
                DateTime result;
                try
                {
                    result = rnd.NextDateTime(min, max);
                    Console.WriteLine(result.ToString("O"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                    throw;
                }

                Assert.IsTrue(result >= min && result <= max);
            }

            Test(DateTime.Now, DateTime.Now.AddDays(1));
            Test(DateTime.MinValue, DateTime.MaxValue);

            Test(DateTime.MinValue, DateTime.MinValue);
            Test(DateTime.MinValue, DateTime.MinValue.AddMinutes(1));
            Test(DateTime.MaxValue.AddMinutes(-1), DateTime.MaxValue);
            Test(DateTime.MaxValue.AddHours(-1), DateTime.MaxValue);
            Test(DateTime.MaxValue.AddDays(-1), DateTime.MaxValue);
        }

        [Test]
        public void NextDateTest()
        {
            var rnd = new Random();
            void Test(DateTime min, DateTime max)
            {
                Console.Write($@"Random date {min:yyyy-MM-dd}..{max:yyyy-MM-dd}: ");
                DateTime result;
                try
                {
                    result = rnd.NextDate(min, max);
                    Console.WriteLine(result.ToString("yyyy-MM-dd"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                    throw;
                }

                Assert.IsTrue(result >= min.Date && result <= max.Date);
            }

            Test(DateTime.Now, DateTime.Now.AddDays(1));
            Test(DateTime.MinValue, DateTime.MaxValue);

            Test(DateTime.MinValue, DateTime.MinValue);
            Test(DateTime.MinValue, DateTime.MinValue.AddDays(1));
            Test(DateTime.MaxValue.AddDays(-1), DateTime.MaxValue);
        }

        [Test]
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

        [Test]
        public void NextTimeSpanTest()
        {
            var rnd = new Random();
            void Test(TimeSpan min, TimeSpan max)
            {
                Console.Write($@"Random TimeSpan {min}..{max}: ");
                TimeSpan result;
                try
                {
                    result = rnd.NextTimeSpan(min, max);
                    Console.WriteLine(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                    throw;
                }

                Assert.IsTrue(result >= min && result <= max);
            }

            Test(TimeSpan.MinValue, TimeSpan.MaxValue);
            Test(TimeSpan.MinValue, TimeSpan.MinValue);
            Test(TimeSpan.MinValue, TimeSpan.MinValue + new TimeSpan(1));
            Test(TimeSpan.MaxValue - new TimeSpan(1), TimeSpan.MaxValue);
        }

#if NET6_0_OR_GREATER
        [Test]
        public void NextDateOnlyTest()
        {
            var rnd = new Random();
            void Test(DateOnly min, DateOnly max)
            {
                Console.Write($@"Random date {min:O}..{max:O}: ");
                DateOnly result;
                try
                {
                    result = rnd.NextDateOnly(min, max);
                    Console.WriteLine(result.ToString("O"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                    throw;
                }

                Assert.IsTrue(result >= min && result <= max);
            }

            Test(DateOnly.MinValue, DateOnly.MaxValue);
            Test(DateOnly.MinValue, DateOnly.MinValue);
            Test(DateOnly.MinValue, DateOnly.MinValue.AddDays(1));
            Test(DateOnly.MaxValue.AddDays(-1), DateOnly.MaxValue);
        }

        [Test]
        public void NextTimeOnlyTest()
        {
            var rnd = new Random();
            void Test(TimeOnly min, TimeOnly max)
            {
                Console.Write($@"Random time {min:O}..{max:O}: ");
                TimeOnly result;
                try
                {
                    result = rnd.NextTimeOnly(min, max);
                    Console.WriteLine(result.ToString("O"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($@"{e.GetType().Name}: {e.Message}".Replace(Environment.NewLine, " "));
                    throw;
                }

                Assert.IsTrue(result >= min && result <= max);
            }

            Test(TimeOnly.MinValue, TimeOnly.MaxValue);
            Test(TimeOnly.MinValue, TimeOnly.MinValue);
            Test(TimeOnly.MinValue, TimeOnly.MinValue.Add(new TimeSpan(1)));
            Test(TimeOnly.MaxValue.Add(new TimeSpan(-1)), TimeOnly.MaxValue);
        }
#endif

        [TestCase(StringCreation.AnyChars)]
        [TestCase(StringCreation.AnyValidChars)]
        [TestCase(StringCreation.Ascii)]
        [TestCase(StringCreation.Digits)]
        [TestCase(StringCreation.DigitsNoLeadingZeros)]
        [TestCase(StringCreation.Letters)]
        [TestCase(StringCreation.LettersAndDigits)]
        [TestCase(StringCreation.UpperCaseLetters)]
        [TestCase(StringCreation.LowerCaseLetters)]
        [TestCase(StringCreation.TitleCaseLetters)]
        [TestCase(StringCreation.UpperCaseWord)]
        [TestCase(StringCreation.LowerCaseWord)]
        [TestCase(StringCreation.TitleCaseWord)]
        [TestCase(StringCreation.Sentence)]
        public void NextStringTest(StringCreation strategy)
        {
            var s = ThreadSafeRandom.Instance.NextString(10, strategy);
            Console.WriteLine($"{strategy}: {s}");
            Assert.AreEqual(10, s.Length);
        }

#if NETCOREAPP3_0_OR_GREATER && !NETSTANDARD_TEST
        [Test]
        public void NextRuneTest()
        {
            var rnd = new Random();

            // no range
            Rune result = rnd.NextRune((Rune)0, (Rune)0);
            Assert.AreEqual((Rune)0, result);

            // min > max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextRune((Rune)2, (Rune)1));

            // Surrogate Rune is invalid
            Throws<ArgumentOutOfRangeException>(() => rnd.NextRune(UnicodeCategory.Surrogate));

            // But other categories are valid
            foreach (UnicodeCategory category in Enum<UnicodeCategory>.GetValues())
            {
                if (category == UnicodeCategory.Surrogate)
                    continue;

                result = rnd.NextRune(category);
                Assert.AreEqual(category, Rune.GetUnicodeCategory(result));
            }

            for (int i = 0; i < 10_000; i++)
            {
                // small range
                result = rnd.NextRune((Rune)0, (Rune)1);
                Assert.IsTrue(result >= (Rune)0 && result <= (Rune)1);
                result = rnd.NextRune((Rune)0xD7FF, (Rune)0xE000);
                Assert.IsTrue(result == (Rune)0xD7FF || result == (Rune)0xE000);

                // big range
                result = rnd.NextRune(UnicodeCategory.UppercaseLetter);
                Assert.AreEqual(UnicodeCategory.UppercaseLetter, Rune.GetUnicodeCategory(result));

                // any value
                result = rnd.NextRune();
                Assert.IsTrue(result >= (Rune)0 && result <= (Rune)0x10FFFF);
            }
        }
#endif

        [Test]
        public void NextObjectTest()
        {
            var rnd = new Random();
            void Test<T>(GenerateObjectSettings settings = null)
            {
                var obj = rnd.NextObject<T>(settings);
                Console.WriteLine($"{typeof(T).GetName(TypeNameKind.ShortName)}: {AsString(obj)}");
            }

            static string AsString(object obj)
            {
                if (obj == null)
                    return "<null>";

                // KeyValuePair has a similar ToString to this one
                if (obj is DictionaryEntry de)
                    return $"[{de.Key}, {de.Value}]";

                if (obj is not IEnumerable enumerable || enumerable is string)
                    return obj.ToStringInternal(CultureInfo.InvariantCulture);

                return enumerable.Cast<object>().Select(AsString).Join(", ");
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
#if !NET35
            Test<BigInteger>();
#endif
#if NETCOREAPP3_0_OR_GREATER
            Test<Rune>();
#endif
#if NET5_0_OR_GREATER
            Test<Half>();
#endif
#if NET6_0_OR_GREATER
            Test<DateOnly>();
            Test<TimeOnly>();
#endif

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
#if NETFRAMEWORK
            Test<CounterCreationDataCollection>(new GenerateObjectSettings { SubstitutionForObjectType = typeof(CounterCreationData) }); // populate, typed object  
#endif

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

            // abstract types/interfaces
            Test<Enum>();
            Test<IConvertible>();

            // delegates
            Test<Delegate>();
            Test<MulticastDelegate>();
            Test<Func<int>>();
            Test<OutDelegate>();

            // recursive types
            Test<Recursive>(); // contains self as member
            Test<RecursiveCollection>(); // contains self as collection item
            Test<XmlSchemaObject>(); // contains self as abstract class
        }

#if NETFRAMEWORK
        [Test]
        [SecuritySafeCritical]
        public void NextObjectTest_PartiallyTrusted()
        {
            var domain = CreateSandboxDomain(
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess),
                new SecurityPermission(SecurityPermissionFlag.Execution | SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlPolicy | SecurityPermissionFlag.SkipVerification),
                new EventLogPermission(PermissionState.Unrestricted));
            var handle = Activator.CreateInstance(domain, Assembly.GetExecutingAssembly().FullName, typeof(Sandbox).FullName);
            var sandbox = (Sandbox)handle.Unwrap();
            try
            {
                sandbox.NextObjectTest();
            }
            catch (SecurityException e)
            {
                Console.WriteLine(e);
                throw;
            }
        } 
#endif

        #endregion
    }
}
