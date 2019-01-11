using System;
using System.Collections.Generic;
using System.Linq;
using KGySoft.CoreLibraries;
using NUnit.Framework;

namespace _LibrariesTest.Tests.CoreLibraries
{
    /// <summary>
    /// Summary description for EnumToolsTest
    /// </summary>
    [TestFixture]
    public class EnumTest : TestBase
    {
        [Flags]
        private enum TestLongEnum: long
        {
            Semmi,
            Alma = 1,
            Béka = 2,
            Cica = 4,
            Kecske = 8,
            aa = 1,
            bb = -1,

            Kecskebéka = Kecske | Béka,

            Mínusz = Int64.MinValue,
            Plusz = Int64.MaxValue,
        }

        private enum TestUlongEnum: ulong
        {
            X = UInt64.MaxValue
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

        [Test]
        public void GetNamesValuesTest()
        {
            Type enumType = typeof(TestLongEnum);
            Assert.IsTrue(Enum.GetNames(enumType).SequenceEqual(Enum<TestLongEnum>.GetNames()));
            Assert.IsTrue(Enum.GetValues(enumType).Cast<TestLongEnum>().SequenceEqual(Enum<TestLongEnum>.GetValues()));

            Assert.AreEqual(Enum.GetName(enumType, TestLongEnum.Alma), Enum<TestLongEnum>.GetName(TestLongEnum.Alma));
            Assert.AreEqual(Enum.GetName(enumType, TestLongEnum.aa), Enum<TestLongEnum>.GetName(TestLongEnum.aa));
            Assert.AreEqual(Enum.GetName(enumType, 1), Enum<TestLongEnum>.GetName(1));
            Assert.AreEqual(Enum.GetName(enumType, Int64.MinValue), Enum<TestLongEnum>.GetName(Int64.MinValue));

            enumType = typeof(TestIntEnum);
            Assert.AreEqual(Enum.GetName(enumType, TestIntEnum.Risky), Enum<TestIntEnum>.GetName(TestIntEnum.Risky));
            Assert.AreEqual(Enum.GetName(enumType, 1 << 31), Enum<TestIntEnum>.GetName(1 << 31));
        }

        [Test]
        public void IsDefinedTest()
        {
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined(TestLongEnum.Cica));
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined(TestLongEnum.Cica | TestLongEnum.Mínusz));

            Assert.IsTrue(Enum<TestLongEnum>.IsDefined("Cica"));
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined("Cicamica"));

            Assert.IsTrue(Enum<TestLongEnum>.IsDefined((long)TestLongEnum.Plusz));
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined((long)TestLongEnum.Mínusz));
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined((ulong)TestLongEnum.Plusz));
            Assert.IsTrue(Enum<TestLongEnum>.IsDefined(-1));
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined(unchecked((ulong)(TestLongEnum.Mínusz))));
            Assert.IsFalse(Enum<TestLongEnum>.IsDefined(UInt64.MaxValue));

            Assert.IsTrue(Enum<TestIntEnum>.IsDefined(TestIntEnum.Risky));
            Assert.IsTrue(Enum<TestIntEnum>.IsDefined("Risky"));
            Assert.IsTrue(Enum<TestIntEnum>.IsDefined(1 << 31)); // -2147483648
            Assert.IsFalse(Enum<TestIntEnum>.IsDefined(1U << 31)); // 2147483648
        }

        [Test]
        public void ToStringTest()
        {
            Assert.AreEqual("X", Enum<TestUlongEnum>.ToString(TestUlongEnum.X));
            Assert.AreEqual("0", Enum<EmptyEnum>.ToString(default(EmptyEnum)));
            Assert.AreEqual("Semmi", Enum<TestLongEnum>.ToString(default(TestLongEnum)));

            Assert.AreNotEqual("-10", Enum<TestUlongEnum>.ToString(unchecked((TestUlongEnum)(-10))));
            Assert.AreEqual("-10", Enum<TestLongEnum>.ToString((TestLongEnum)(-10)));
            Assert.AreEqual("-10", Enum<TestIntEnum>.ToString((TestIntEnum)(-10)));

            TestLongEnum e = TestLongEnum.Cica | TestLongEnum.Kecskebéka;
            Assert.AreEqual("Cica, Kecskebéka", e.ToString(EnumFormattingOptions.Auto));
            Assert.AreEqual("14", e.ToString(EnumFormattingOptions.NonFlags));
            Assert.AreEqual("Béka, Cica, Kecske", e.ToString(EnumFormattingOptions.DistinctFlags));
            Assert.AreEqual("Cica, Kecskebéka", e.ToString(EnumFormattingOptions.CompoundFlagsOrNumber));
            Assert.AreEqual("Cica, Kecskebéka", e.ToString(EnumFormattingOptions.CompoundFlagsAndNumber));

            e = (TestLongEnum)(int)e + 16;
            Assert.AreEqual("30", e.ToString(EnumFormattingOptions.Auto));
            Assert.AreEqual("30", e.ToString(EnumFormattingOptions.NonFlags));
            Assert.AreEqual("Béka, Cica, Kecske, 16", e.ToString(EnumFormattingOptions.DistinctFlags));
            Assert.AreEqual("30", e.ToString(EnumFormattingOptions.CompoundFlagsOrNumber));
            Assert.AreEqual("16, Cica, Kecskebéka", e.ToString(EnumFormattingOptions.CompoundFlagsAndNumber));

            TestIntEnum ie = TestIntEnum.Simple | TestIntEnum.Normal | TestIntEnum.Risky;
            Assert.AreEqual("Simple, Normal, Risky", ie.ToString(EnumFormattingOptions.Auto));
        }

        [Test]
        public void EnumParseTest()
        {
            Assert.AreEqual(default(EmptyEnum), Enum<EmptyEnum>.Parse("0"));
            Assert.AreEqual(TestUlongEnum.X, Enum<TestUlongEnum>.Parse("X"));
            Assert.AreEqual(TestUlongEnum.X, Enum<TestUlongEnum>.Parse(UInt64.MaxValue.ToString()));
            Assert.AreEqual(TestLongEnum.Mínusz, Enum<TestLongEnum>.Parse("Mínusz"));
            Assert.AreEqual(TestLongEnum.Mínusz, Enum<TestLongEnum>.Parse(Int64.MinValue.ToString()));

            Assert.AreEqual(TestLongEnum.Alma, Enum<TestLongEnum>.Parse("Alma"));
            Assert.AreEqual(TestLongEnum.Alma, Enum<TestLongEnum>.Parse("aa"));
            Assert.AreEqual(TestLongEnum.aa, Enum<TestLongEnum>.Parse("aa"));
            Assert.AreEqual(TestLongEnum.aa, Enum<TestLongEnum>.Parse("Alma"));
            Assert.AreEqual(TestLongEnum.Alma, Enum<TestLongEnum>.Parse("alma", true));
            Assert.AreEqual(TestLongEnum.Alma, Enum<TestLongEnum>.Parse("AA", true));

            TestLongEnum e = TestLongEnum.Cica | TestLongEnum.Kecskebéka;
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Cica, Kecskebéka"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("14"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Béka, Cica, Kecske"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Béka Cica Kecske", " "));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Béka | Cica | Kecske", "|"));

            e = (TestLongEnum)(int)e + 16;
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("30"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("Béka, Cica, Kecske, 16"));
            Assert.AreEqual(e, Enum<TestLongEnum>.Parse("16, Cica, Kecskebéka"));

            Assert.IsFalse(Enum<TestLongEnum>.TryParse(UInt64.MaxValue.ToString(), out e));
            Assert.IsFalse(Enum<TestLongEnum>.TryParse("Béka, Cica, , Kecske, 16", out e));

            TestIntEnum ie = TestIntEnum.Simple | TestIntEnum.Normal | TestIntEnum.Risky;
            Assert.AreEqual(ie, Enum<TestIntEnum>.Parse(ie.ToString(EnumFormattingOptions.Auto)));
        }

        [Test]
        public void EnumComparerTest()
        {
            var c1 = EnumComparer<EmptyEnum>.Comparer;
            c1.Compare((EmptyEnum)(-1), (EmptyEnum)1);
            var d1 = Comparer<EmptyEnum>.Default;
            var e1 = EqualityComparer<EmptyEnum>.Default;
            var v1 = new EmptyEnum[] { (EmptyEnum)(-1), (EmptyEnum)1 };

            Assert.AreEqual(d1.Compare(v1[0], v1[1]), c1.Compare(v1[0], v1[1]));
            Assert.AreEqual(d1.Compare(v1[1], v1[0]), c1.Compare(v1[1], v1[0]));
            Assert.AreEqual(d1.Compare(v1[1], v1[1]), c1.Compare(v1[1], v1[1]));
            Assert.AreEqual(e1.Equals(v1[0], v1[1]), c1.Equals(v1[0], v1[1]));
            Assert.AreEqual(e1.Equals(v1[1], v1[1]), c1.Equals(v1[1], v1[1]));
            Assert.AreEqual(e1.GetHashCode(v1[0]), c1.GetHashCode(v1[0]));
            Assert.AreNotEqual(c1.GetHashCode(v1[0]), c1.GetHashCode(v1[1]));

            var c2 = EnumComparer<TestLongEnum>.Comparer;
            var d2 = Comparer<TestLongEnum>.Default;
            var e2 = EqualityComparer<TestLongEnum>.Default;
            var v2 = new TestLongEnum[] { TestLongEnum.Mínusz, TestLongEnum.Plusz };

            Assert.AreEqual(d2.Compare(v2[0], v2[1]), c2.Compare(v2[0], v2[1]));
            Assert.AreEqual(d2.Compare(v2[1], v2[0]), c2.Compare(v2[1], v2[0]));
            Assert.AreEqual(d2.Compare(v2[1], v2[1]), c2.Compare(v2[1], v2[1]));
            Assert.AreEqual(e2.Equals(v2[0], v2[1]), c2.Equals(v2[0], v2[1]));
            Assert.AreEqual(e2.Equals(v2[1], v2[1]), c2.Equals(v2[1], v2[1]));
            //Assert.AreEqual(e2.GetHashCode(v2[0]), c2.GetHashCode(v2[0]));
            //Assert.AreNotEqual(c2.GetHashCode(v2[0]), c2.GetHashCode(v2[1]));

            var c3 = EnumComparer<TestUlongEnum>.Comparer;
            var d3 = Comparer<TestUlongEnum>.Default;
            var e3 = EqualityComparer<TestUlongEnum>.Default;
            var v3 = new TestUlongEnum[] { TestUlongEnum.X, (TestUlongEnum)1 };

            Assert.AreEqual(d3.Compare(v3[0], v3[1]), c3.Compare(v3[0], v3[1]));
            Assert.AreEqual(d3.Compare(v3[1], v3[0]), c3.Compare(v3[1], v3[0]));
            Assert.AreEqual(d3.Compare(v3[1], v3[1]), c3.Compare(v3[1], v3[1]));
            Assert.AreEqual(e3.Equals(v3[0], v3[1]), c3.Equals(v3[0], v3[1]));
            Assert.AreEqual(e3.Equals(v3[1], v3[1]), c3.Equals(v3[1], v3[1]));
            //Assert.AreEqual(e3.GetHashCode(v3[0]), c3.GetHashCode(v3[0]));
            Assert.AreNotEqual(c3.GetHashCode(v3[0]), c3.GetHashCode(v3[1]));
        }

        [Test]
        public void GetFlagsTest()
        {
            ulong max = UInt64.MaxValue;
            Assert.AreEqual(0, Enum<TestLongEnum>.GetFlags(TestLongEnum.Semmi, true).Count());
            Assert.AreEqual(2, Enum<TestLongEnum>.GetFlags(TestLongEnum.Kecskebéka, true).Count());
            Assert.AreEqual(5, Enum<TestLongEnum>.GetFlags((TestLongEnum)max, true).Count());
            Assert.AreEqual(64, Enum<TestLongEnum>.GetFlags((TestLongEnum)max, false).Count());
            Assert.AreEqual(1, Enum<TestLongEnum>.GetFlags(TestLongEnum.Mínusz, true).Count());
            Assert.AreEqual(0, Enum<TestUlongEnum>.GetFlags((TestUlongEnum)max, true).Count());
            Assert.AreEqual(64, Enum<TestUlongEnum>.GetFlags((TestUlongEnum)max, false).Count());

            Assert.AreEqual(1, Enum<TestIntEnum>.GetFlags(TestIntEnum.Risky, true).Count());
            Assert.AreEqual(3, Enum<TestIntEnum>.GetFlags(unchecked((TestIntEnum)(int)UInt32.MaxValue), true).Count());
            Assert.AreEqual(32, Enum<TestIntEnum>.GetFlags(unchecked((TestIntEnum)(int)UInt32.MaxValue), false).Count());

            AssertItemsEqual(new[] { TestLongEnum.Alma, TestLongEnum.Béka, TestLongEnum.Cica, TestLongEnum.Kecske, TestLongEnum.Mínusz }.OrderBy(e => e), Enum<TestLongEnum>.GetFlags().OrderBy(e => e));
            AssertItemsEqual(new TestUlongEnum[0], Enum<TestUlongEnum>.GetFlags());
            AssertItemsEqual(new[] { TestIntEnum.Simple, TestIntEnum.Normal, TestIntEnum.Risky }.OrderBy(e => e), Enum<TestIntEnum>.GetFlags().OrderBy(e => e));
            AssertItemsEqual(new EmptyEnum[0], Enum<EmptyEnum>.GetFlags());
        }

        [Test]
        public void AllFlagsDefinedTest()
        {
            Assert.IsTrue(Enum<TestLongEnum>.AllFlagsDefined(TestLongEnum.Semmi));
            Assert.IsTrue(Enum<TestLongEnum>.AllFlagsDefined(TestLongEnum.Kecskebéka));
            Assert.IsFalse(Enum<TestLongEnum>.AllFlagsDefined(TestLongEnum.Plusz));
            Assert.IsTrue(Enum<TestLongEnum>.AllFlagsDefined(TestLongEnum.Mínusz));

            Assert.IsTrue(Enum<TestIntEnum>.AllFlagsDefined(TestIntEnum.None)); // Zero is defined in TestIntEnum
            Assert.IsTrue(Enum<TestIntEnum>.AllFlagsDefined(TestIntEnum.Risky));
            Assert.IsTrue(Enum<TestIntEnum>.AllFlagsDefined(1 << 31)); // -2147483648: This is the value of Risky
            Assert.IsFalse(Enum<TestIntEnum>.AllFlagsDefined(1U << 31)); // 2147483648: This is not defined (cannot be represented in int)
            Assert.IsFalse(Enum<TestIntEnum>.AllFlagsDefined(1L << 31)); // 2147483648
            Assert.IsFalse(Enum<TestIntEnum>.AllFlagsDefined(1UL << 31)); // 2147483648

            Assert.IsFalse(Enum<TestUlongEnum>.AllFlagsDefined(0UL)); // Zero is not defined in TestUlongEnum
        }

        [Test]
        public void HasFlagTest()
        {
            TestLongEnum e64 = TestLongEnum.Alma | TestLongEnum.Béka;
            Assert.IsTrue(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Semmi));
            Assert.IsTrue(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Béka));
            Assert.IsFalse(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Cica));
            Assert.IsTrue(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Alma | TestLongEnum.Béka));
            Assert.IsFalse(Enum<TestLongEnum>.HasFlag(e64, TestLongEnum.Kecskebéka));

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

            TestUlongEnum eu64 = TestUlongEnum.X;
            Assert.IsTrue(Enum<TestUlongEnum>.HasFlag(eu64, 0UL)); // Zero -> true
            Assert.IsTrue(Enum<TestUlongEnum>.HasFlag(eu64, TestUlongEnum.X)); // Zero -> true
        }

        [Test]
        public void IsSingleFlagTest()
        {
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(TestLongEnum.Semmi));
            Assert.IsTrue(Enum<TestLongEnum>.IsSingleFlag(TestLongEnum.Kecske));
            Assert.IsTrue(Enum<TestLongEnum>.IsSingleFlag(TestLongEnum.Béka));
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(TestLongEnum.Kecskebéka));
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(Int64.MaxValue));
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(1 << 63)); // this is -2147483648, which is not a single bit as a long value
            Assert.IsTrue(Enum<TestLongEnum>.IsSingleFlag(1L << 63)); // this is a single bit negative value, which is valid
            Assert.IsFalse(Enum<TestLongEnum>.IsSingleFlag(1UL << 63)); // single bit but out of range

            Assert.IsFalse(Enum<TestIntEnum>.IsSingleFlag(1L << 63)); // out of range
            Assert.IsFalse(Enum<TestUlongEnum>.IsSingleFlag(1L << 63)); // this is a negative value: out of range
        }
    }
}
