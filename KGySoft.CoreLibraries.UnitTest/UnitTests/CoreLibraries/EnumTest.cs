#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumTest.cs
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
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis; 
#endif
using System.Linq;
#if NETFRAMEWORK
using System.Reflection;
using System.Security;
using System.Security.Permissions;
#endif

using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries
{
    [TestFixture]
    public class EnumTest : TestBase
    {
        #region Nested Types
        
        #region Enumerations

        [Flags]
#if NET5_0_OR_GREATER
        [SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "Redeclaration is intended, it is part of the test")] 
#endif
        private enum TestLongEnum : long
        {
            None,
            Alpha = 1,
            Beta = 2,
            Gamma = 4,
            Delta = 8,

            AlphaRedefined = 1,
            Negative = -1,

            Alphabet = Alpha | Beta,

            Min = Int64.MinValue,
            Max = Int64.MaxValue,
        }

        private enum TestULongEnum : ulong
        {
            Max = UInt64.MaxValue
        }

        [Flags]
        private enum TestIntEnum
        {
            None = 0,
            Simple = 1,
            Normal = 1 << 5,
            Risky = 1 << 31 // This is a negative value. Converting to Int64, this is not a single bit any more.
        }

        private enum EmptyEnum { }

        #endregion

        #region Sandbox class

#if NETFRAMEWORK
        private class Sandbox : MarshalByRefObject
        {
            internal void TestEnum()
            {
#if !NET35
                Assert.IsFalse(AppDomain.CurrentDomain.IsFullyTrusted);
#endif
                var test = new EnumTest();
                test.ToStringTest();
                test.ParseTest();
                test.GetFlagsTest(); // uses also the non-public generated converter members internally
            }
        }
#endif

        #endregion

        #endregion

        #region Methods

        [Test]
        public void GetNamesValuesTest()
        {
            Type enumType = typeof(TestLongEnum);
            Assert.IsTrue(Enum.GetNames(enumType).SequenceEqual(Enum<TestLongEnum>.GetNames()));
            Assert.IsTrue(Enum.GetValues(enumType).Cast<TestLongEnum>().SequenceEqual(Enum<TestLongEnum>.GetValues()));

            Assert.AreEqual(Enum.GetName(enumType, TestLongEnum.Alpha), Enum<TestLongEnum>.GetName(TestLongEnum.Alpha));
            Assert.AreEqual(Enum.GetName(enumType, TestLongEnum.AlphaRedefined), Enum<TestLongEnum>.GetName(TestLongEnum.AlphaRedefined));
            Assert.AreEqual(Enum.GetName(enumType, 1), Enum<TestLongEnum>.GetName(1));
            Assert.AreEqual(Enum.GetName(enumType, Int64.MinValue), Enum<TestLongEnum>.GetName(Int64.MinValue));

            enumType = typeof(TestIntEnum);
            Assert.AreEqual(Enum.GetName(enumType, TestIntEnum.Risky), Enum<TestIntEnum>.GetName(TestIntEnum.Risky));
            Assert.AreEqual(Enum.GetName(enumType, 1 << 31), Enum<TestIntEnum>.GetName(1 << 31));
        }

        [Test]
        public void IsDefinedTest()
        {
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined(TestLongEnum.Gamma));
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined(TestLongEnum.Gamma | TestLongEnum.Min));

            Assert.IsTrue(Enum<TestLongEnum>.IsDefined("Gamma"));
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined("Gamma".AsSegment()));
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined("Gamma".AsSpan()));
#endif
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined("Omega"));
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined("Omega".AsSegment()));
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined("Omega".AsSpan()));
#endif

            Assert.IsTrue(Enum<TestLongEnum>.IsDefined((long)TestLongEnum.Max));
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined((long)TestLongEnum.Min));
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined((ulong)TestLongEnum.Max));
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined(-1));
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined(unchecked((ulong)(TestLongEnum.Min))));
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined(UInt64.MaxValue));

            Assert.IsTrue(Enum<TestIntEnum>.IsDefined(TestIntEnum.Risky));
            Assert.IsTrue(Enum<TestIntEnum>.IsDefined("Risky"));
            Assert.IsTrue(Enum<TestIntEnum>.IsDefined(1 << 31)); // -2147483648
            Assert.IsFalse(Enum<TestIntEnum>.IsDefined(1U << 31)); // 2147483648
        }

        [Test]
        public void ToStringTest()
        {
            static void Test<TEnum>(string expectedValue, TEnum value, EnumFormattingOptions format = EnumFormattingOptions.Auto)
                where TEnum : struct, Enum
            {
                Assert.AreEqual(expectedValue, Enum<TEnum>.ToString(value, format));
#if NETCOREAPP2_1_OR_GREATER
                Span<char> buffer = stackalloc char[expectedValue.Length];
                Assert.IsTrue(Enum<TEnum>.TryFormat(value, buffer, out int charsWritten, format));
                Assert.AreEqual(expectedValue.Length, charsWritten);
                Assert.AreEqual(expectedValue, buffer.ToString());
#endif
            }

            Test("Max", TestULongEnum.Max);
            Test("0", default(EmptyEnum));
            Test("None", default(TestLongEnum));
            Test("Alpha", TestLongEnum.Alpha);
            Test("1", TestLongEnum.Alpha, EnumFormattingOptions.Number);
            Test("-2147483648", (EmptyEnum)(1 << 31));
            Test("-2147483647", (EmptyEnum)((1 << 31) | 1));
            Test("1, -2147483648", (EmptyEnum)((1 << 31) | 1), EnumFormattingOptions.DistinctFlags);

            Assert.AreNotEqual("-10", Enum<TestULongEnum>.ToString(unchecked((TestULongEnum)(-10))));
            Test("-10", (TestLongEnum)(-10));
            Test("-10", (TestIntEnum)(-10));

            TestLongEnum e = TestLongEnum.Gamma | TestLongEnum.Alphabet;
            Test("Alphabet, Gamma", e);
            Test("7", e, EnumFormattingOptions.NonFlags);
            Test("Alpha, Beta, Gamma", e, EnumFormattingOptions.DistinctFlags);
            Test("Alphabet, Gamma", e, EnumFormattingOptions.CompoundFlagsOrNumber);
            Test("Alphabet, Gamma", e, EnumFormattingOptions.CompoundFlagsAndNumber);

            e += 16;
            Test("23", e);
            Test("23", e, EnumFormattingOptions.NonFlags);
            Test("Alpha, Beta, Gamma, 16", e, EnumFormattingOptions.DistinctFlags);
            Test("23", e, EnumFormattingOptions.CompoundFlagsOrNumber);
            Test("16, Alphabet, Gamma", e, EnumFormattingOptions.CompoundFlagsAndNumber);

            TestIntEnum ie = TestIntEnum.Simple | TestIntEnum.Normal | TestIntEnum.Risky;
            Test("Simple, Normal, Risky", ie);
        }

        [Test]
        public void ParseTest()
        {
            Assert.AreEqual(default(EmptyEnum), Enum<EmptyEnum>.Parse("0"));
            Assert.AreEqual(default(EmptyEnum), Enum<EmptyEnum>.Parse("0".AsSegment()));
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.AreEqual(default(EmptyEnum), Enum<EmptyEnum>.Parse("0".AsSpan()));
#endif
            Assert.AreEqual(TestULongEnum.Max, Enum<TestULongEnum>.Parse("Max"));
            Assert.AreEqual(TestULongEnum.Max, Enum<TestULongEnum>.Parse("Max".AsSegment()));
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.AreEqual(TestULongEnum.Max, Enum<TestULongEnum>.Parse("Max".AsSpan()));
#endif
            Assert.AreEqual(TestULongEnum.Max, Enum<TestULongEnum>.Parse(UInt64.MaxValue.ToString()));
            Assert.AreEqual(TestLongEnum.Min, Enum<TestLongEnum>.Parse("Min"));
            Assert.AreEqual(TestLongEnum.Min, Enum<TestLongEnum>.Parse(Int64.MinValue.ToString()));
            Assert.AreEqual(TestLongEnum.Min, Enum<TestLongEnum>.Parse(" -9223372036854775808 "));
            Assert.AreEqual(TestLongEnum.Max, Enum<TestLongEnum>.Parse("9223372036854775807"));
            Assert.AreEqual((EmptyEnum)(-10), Enum<EmptyEnum>.Parse("-10"));

            Assert.AreEqual(TestLongEnum.Alpha, Enum<TestLongEnum>.Parse("Alpha"));
            Assert.AreEqual(TestLongEnum.Alpha, Enum<TestLongEnum>.Parse("AlphaRedefined"));
            Assert.AreEqual(TestLongEnum.AlphaRedefined, Enum<TestLongEnum>.Parse("AlphaRedefined"));
            Assert.AreEqual(TestLongEnum.AlphaRedefined, Enum<TestLongEnum>.Parse("Alpha"));
            Assert.AreEqual(TestLongEnum.Alpha, Enum<TestLongEnum>.Parse("alpha", true));
            Assert.AreEqual(TestLongEnum.Alpha, Enum<TestLongEnum>.Parse("ALPHAREDEFINED", true));
            Assert.AreEqual(TestLongEnum.Alpha, Enum<TestLongEnum>.Parse("ALPHAREDEFINED".AsSegment(), true));
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.AreEqual(TestLongEnum.Alpha, Enum<TestLongEnum>.Parse("ALPHAREDEFINED".AsSpan(), true));
#endif

            TestLongEnum e = TestLongEnum.Gamma | TestLongEnum.Alphabet;
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Gamma, Alphabet"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Gamma, Alphabet".AsSegment()));
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Gamma, Alphabet".AsSpan()));
#endif
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("7"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Alpha, Beta, Gamma"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Alpha Beta Gamma", " "));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Alpha | Beta | Gamma", "|"));

            e += 16;
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("23"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Alpha, Beta, Gamma, 16"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("16, Gamma, Alphabet"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("16, Gamma, Alphabet".AsSegment()));
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("16, Gamma, Alphabet".AsSpan()));
#endif

            Assert.IsFalse(Enum<TestLongEnum>.TryParse(UInt64.MaxValue.ToString(), out var _));
            Assert.IsFalse(Enum<TestLongEnum>.TryParse("Beta, Gamma, , Delta, 16", out var _));
            Assert.IsFalse(Enum<TestLongEnum>.TryParse(" ", out var _));
            Assert.IsFalse(Enum<TestLongEnum>.TryParse("9223372036854775808", out var _));
            Assert.IsFalse(Enum<TestLongEnum>.TryParse(" -9223372036854775809 ", out var _));

            TestIntEnum ie = TestIntEnum.Simple | TestIntEnum.Normal | TestIntEnum.Risky;
            Assert.AreEqual(ie, Enum<TestIntEnum>.Parse(ie.ToString(EnumFormattingOptions.Auto)));
        }

        [Test]
        public void GetFlagsTest()
        {
            ulong max = UInt64.MaxValue;
            Assert.AreEqual(0, Enum<TestLongEnum>.GetFlags(TestLongEnum.None, true).Count());
            Assert.AreEqual(2, Enum<TestLongEnum>.GetFlags(TestLongEnum.Alphabet, true).Count());
            Assert.AreEqual(5, Enum<TestLongEnum>.GetFlags((TestLongEnum)max, true).Count());
            Assert.AreEqual(64, Enum<TestLongEnum>.GetFlags((TestLongEnum)max, false).Count());
            Assert.AreEqual(1, Enum<TestLongEnum>.GetFlags(TestLongEnum.Min, true).Count());
            Assert.AreEqual(0, Enum<TestULongEnum>.GetFlags((TestULongEnum)max, true).Count());
            Assert.AreEqual(64, Enum<TestULongEnum>.GetFlags((TestULongEnum)max, false).Count());

            Assert.AreEqual(1, Enum<TestIntEnum>.GetFlags(TestIntEnum.Risky, true).Count());
            Assert.AreEqual(3, Enum<TestIntEnum>.GetFlags(unchecked((TestIntEnum)(int)UInt32.MaxValue), true).Count());
            Assert.AreEqual(32, Enum<TestIntEnum>.GetFlags(unchecked((TestIntEnum)(int)UInt32.MaxValue), false).Count());

            AssertItemsEqual(new[] { TestLongEnum.Alpha, TestLongEnum.Beta, TestLongEnum.Gamma, TestLongEnum.Delta, TestLongEnum.Min }.OrderBy(e => e), Enum<TestLongEnum>.GetFlags().OrderBy(e => e));
            AssertItemsEqual(Reflector.EmptyArray<TestULongEnum>(), Enum<TestULongEnum>.GetFlags());
            AssertItemsEqual(new[] { TestIntEnum.Simple, TestIntEnum.Normal, TestIntEnum.Risky }.OrderBy(e => e), Enum<TestIntEnum>.GetFlags().OrderBy(e => e));
            AssertItemsEqual(Reflector.EmptyArray<EmptyEnum>(), Enum<EmptyEnum>.GetFlags());
        }

        [Test]
        public void AllFlagsDefinedTest()
        {
            Assert.IsTrue(Enum<TestLongEnum>.AllFlagsDefined(TestLongEnum.None));
            Assert.IsTrue(Enum<TestLongEnum>.AllFlagsDefined(TestLongEnum.Alphabet));
            Assert.IsFalse(Enum<TestLongEnum>.AllFlagsDefined(TestLongEnum.Max));
            Assert.IsTrue(Enum<TestLongEnum>.AllFlagsDefined(TestLongEnum.Min));

            Assert.IsTrue(Enum<TestIntEnum>.AllFlagsDefined(TestIntEnum.None)); // Zero is defined in TestIntEnum
            Assert.IsTrue(Enum<TestIntEnum>.AllFlagsDefined(TestIntEnum.Risky));
            Assert.IsTrue(Enum<TestIntEnum>.AllFlagsDefined(1 << 31)); // -2147483648: This is the value of Risky
            Assert.IsFalse(Enum<TestIntEnum>.AllFlagsDefined(1U << 31)); // 2147483648: This is not defined (cannot be represented in int)
            Assert.IsFalse(Enum<TestIntEnum>.AllFlagsDefined(1L << 31)); // 2147483648
            Assert.IsFalse(Enum<TestIntEnum>.AllFlagsDefined(1UL << 31)); // 2147483648

            Assert.IsFalse(Enum<TestULongEnum>.AllFlagsDefined(0UL)); // Zero is not defined in TestULongEnum
        }

        [Test]
        public void HasFlagTest()
        {
            TestLongEnum e64 = TestLongEnum.Alpha | TestLongEnum.Beta;
            Assert.IsTrue(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.None));
            Assert.IsTrue(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Beta));
            Assert.IsFalse(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Gamma));
            Assert.IsTrue(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Alphabet));
            Assert.IsFalse(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Alpha | TestLongEnum.Gamma));

            TestIntEnum e32 = TestIntEnum.Simple | TestIntEnum.Risky;
            Assert.IsTrue(Enum<TestIntEnum>.HasFlag(e32, TestIntEnum.None)); // Zero -> true
            Assert.IsFalse(Enum<TestIntEnum>.HasFlag(e32, TestIntEnum.Normal));
            Assert.IsTrue(Enum<TestIntEnum>.HasFlag(e32, TestIntEnum.Risky));
            Assert.IsTrue(Enum<TestIntEnum>.HasFlag(e32, (int)TestIntEnum.Risky));
            Assert.IsTrue(Enum<TestIntEnum>.HasFlag(e32, (long)TestIntEnum.Risky));
            Assert.IsTrue(Enum<TestIntEnum>.HasFlag(e32, 1 << 31)); // -2147483648: This is the value of Risky
            Assert.IsFalse(Enum<TestIntEnum>.HasFlag(e32, 1U << 31)); //  2147483648: This is not defined (cannot be represented in int)
            Assert.IsFalse(Enum<TestIntEnum>.HasFlag(e32, 1L << 31)); //  2147483648: This is not defined
            Assert.IsFalse(Enum<TestIntEnum>.HasFlag(e32, 1UL << 31)); //  2147483648: This is not defined

            TestULongEnum eu64 = TestULongEnum.Max;
            Assert.IsTrue(Enum<TestULongEnum>.HasFlag(eu64, 0UL)); // Zero -> true
            Assert.IsTrue(Enum<TestULongEnum>.HasFlag(eu64, TestULongEnum.Max));
        }

        [Test]
        public void IsSingleFlagTest()
        {
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(TestLongEnum.None));
            Assert.IsTrue(Enum<TestLongEnum>.IsSingleFlag(TestLongEnum.Delta));
            Assert.IsTrue(Enum<TestLongEnum>.IsSingleFlag(TestLongEnum.Beta));
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(TestLongEnum.Alphabet));
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(Int64.MaxValue));
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(1 << 63)); // this is -2147483648, which is not a single bit as a long value
            Assert.IsTrue(Enum<TestLongEnum>.IsSingleFlag(1L << 63)); // this is a single bit negative value, which is valid
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(1UL << 63)); // single bit but out of range

            Assert.IsFalse(Enum<TestIntEnum>.IsSingleFlag(1L << 63)); // out of range
            Assert.IsFalse(Enum<TestULongEnum>.IsSingleFlag(1L << 63)); // this is a negative value: out of range
        }

        [Test]
        public void GetFlagsCountTest()
        {
            Assert.AreEqual(0, Enum<TestLongEnum>.GetFlagsCount(TestLongEnum.None));
            Assert.AreEqual(0, Enum<TestLongEnum>.GetFlagsCount(0L));
            Assert.AreEqual(0, Enum<TestLongEnum>.GetFlagsCount(0UL));
            Assert.AreEqual(1, Enum<TestLongEnum>.GetFlagsCount(TestLongEnum.Alpha));
            Assert.AreEqual(1, Enum<TestLongEnum>.GetFlagsCount(1L));
            Assert.AreEqual(-1, Enum<TestLongEnum>.GetFlagsCount(UInt64.MaxValue));
            Assert.AreEqual(-1, Enum<TestULongEnum>.GetFlagsCount(Int64.MinValue));
        }

#if NETFRAMEWORK
        [Test]
        [SecuritySafeCritical]
        public void TestEnum_PartiallyTrusted()
        {
            var domain = CreateSandboxDomain(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
            var handle = Activator.CreateInstance(domain, Assembly.GetExecutingAssembly().FullName, typeof(Sandbox).FullName!);
            var sandbox = (Sandbox)handle.Unwrap();
            try
            {
                sandbox.TestEnum();
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
