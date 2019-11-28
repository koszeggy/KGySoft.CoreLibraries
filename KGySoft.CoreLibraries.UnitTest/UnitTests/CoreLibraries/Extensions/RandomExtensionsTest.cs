#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensionsTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
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
        private partial class Sandbox : MarshalByRefObject
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
            var rnd = new Random();

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
        }

        [Test]
        public void NextUInt32Test()
        {
            var rnd = new Random();

            // min > max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextUInt32(1, 0));

            var result = rnd.NextUInt32(1, 2);
            Assert.AreEqual(1, result);
            result = rnd.NextUInt32(UInt32.MaxValue - 1, UInt32.MaxValue);
            Assert.AreEqual(UInt32.MaxValue - 1, result);

            // big range
            result = rnd.NextUInt32(10, (uint)Int32.MaxValue + 100);
            Assert.IsTrue(result >= 10 && result < (uint)Int32.MaxValue + 100);
        }

        [Test]
        public void NextUInt64Test()
        {
            var rnd = new Random();

            // min-max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextUInt64(1, 0));

            var result = rnd.NextUInt64(0, 10);
            Assert.IsTrue(result >= 0 && result < 10);
        }

        [Test]
        public void NextInt64Test()
        {
            // full range
            var rnd = new Random();

            // min-max
            Throws<ArgumentOutOfRangeException>(() => rnd.NextInt64(1, 0));

            var result = rnd.NextInt64(-5, 5);
            Assert.IsTrue(result >= -5 && result < 5);
        }

        [Test]
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

            var rnd = new Random();
            Test(rnd, Single.MinValue, Single.MaxValue);
            Test(rnd, 0, Single.Epsilon);
            Test(rnd, Single.MaxValue, Single.PositiveInfinity);
            Test(rnd, Single.NegativeInfinity, Single.PositiveInfinity);
        }

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

        [Test]
        public void NextObjectTest()
        {
            var rnd = new Random();
            void Test<T>(GenerateObjectSettings settings = null)
            {
                var obj = rnd.NextObject<T>(settings);
                //  Console.WriteLine($"Random {typeof(T).Name}: {obj}");
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
#if !(NETCOREAPP2_0 || NETCOREAPP3_0)
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
