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
        private enum TestEnum: long
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
            Type enumType = typeof(TestEnum);
            Assert.IsTrue(Enum.GetNames(enumType).SequenceEqual(Enum<TestEnum>.GetNames()));
            Assert.IsTrue(Enum.GetValues(enumType).Cast<TestEnum>().SequenceEqual(Enum<TestEnum>.GetValues()));

            Assert.AreEqual(Enum.GetName(enumType, TestEnum.Alma), Enum<TestEnum>.GetName(TestEnum.Alma));
            Assert.AreEqual(Enum.GetName(enumType, TestEnum.aa), Enum<TestEnum>.GetName(TestEnum.aa));
            Assert.AreEqual(Enum.GetName(enumType, 1), Enum<TestEnum>.GetName(1));
            Assert.AreEqual(Enum.GetName(enumType, Int64.MinValue), Enum<TestEnum>.GetName(Int64.MinValue));

            enumType = typeof(TestIntEnum);
            Assert.AreEqual(Enum.GetName(enumType, TestIntEnum.Risky), Enum<TestIntEnum>.GetName(TestIntEnum.Risky));
            Assert.AreEqual(Enum.GetName(enumType, 1 << 31), Enum<TestIntEnum>.GetName(1 << 31));
        }

        [Test]
        public void IsDefinedTest()
        {
            Assert.IsTrue(Enum<TestEnum>.IsDefined(TestEnum.Cica));
            Assert.IsFalse(Enum<TestEnum>.IsDefined(TestEnum.Cica | TestEnum.Mínusz));

            Assert.IsTrue(Enum<TestEnum>.IsDefined("Cica"));
            Assert.IsFalse(Enum<TestEnum>.IsDefined("Cicamica"));

            Assert.IsTrue(Enum<TestEnum>.IsDefined((long)TestEnum.Plusz));
            Assert.IsTrue(Enum<TestEnum>.IsDefined((long)TestEnum.Mínusz));
            Assert.IsTrue(Enum<TestEnum>.IsDefined((ulong)TestEnum.Plusz));
            Assert.IsTrue(Enum<TestEnum>.IsDefined(-1));
            Assert.IsFalse(Enum<TestEnum>.IsDefined(unchecked((ulong)(TestEnum.Mínusz))));
            Assert.IsFalse(Enum<TestEnum>.IsDefined(UInt64.MaxValue));

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
            Assert.AreEqual("Semmi", Enum<TestEnum>.ToString(default(TestEnum)));

            Assert.AreNotEqual("-10", Enum<TestUlongEnum>.ToString(unchecked((TestUlongEnum)(-10))));
            Assert.AreEqual("-10", Enum<TestEnum>.ToString((TestEnum)(-10)));
            Assert.AreEqual("-10", Enum<TestIntEnum>.ToString((TestIntEnum)(-10)));

            TestEnum e = TestEnum.Cica | TestEnum.Kecskebéka;
            Assert.AreEqual("Cica, Kecskebéka", e.ToString(EnumFormattingOptions.Auto));
            Assert.AreEqual("14", e.ToString(EnumFormattingOptions.NonFlags));
            Assert.AreEqual("Béka, Cica, Kecske", e.ToString(EnumFormattingOptions.DistinctFlags));
            Assert.AreEqual("Cica, Kecskebéka", e.ToString(EnumFormattingOptions.CompoundFlagsOrNumber));
            Assert.AreEqual("Cica, Kecskebéka", e.ToString(EnumFormattingOptions.CompoundFlagsAndNumber));

            e = (TestEnum)(int)e + 16;
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
            Assert.AreEqual(TestEnum.Mínusz, Enum<TestEnum>.Parse("Mínusz"));
            Assert.AreEqual(TestEnum.Mínusz, Enum<TestEnum>.Parse(Int64.MinValue.ToString()));

            Assert.AreEqual(TestEnum.Alma, Enum<TestEnum>.Parse("Alma"));
            Assert.AreEqual(TestEnum.Alma, Enum<TestEnum>.Parse("aa"));
            Assert.AreEqual(TestEnum.aa, Enum<TestEnum>.Parse("aa"));
            Assert.AreEqual(TestEnum.aa, Enum<TestEnum>.Parse("Alma"));
            Assert.AreEqual(TestEnum.Alma, Enum<TestEnum>.Parse("alma", true));
            Assert.AreEqual(TestEnum.Alma, Enum<TestEnum>.Parse("AA", true));

            TestEnum e = TestEnum.Cica | TestEnum.Kecskebéka;
            Assert.AreEqual(e, Enum<TestEnum>.Parse("Cica, Kecskebéka"));
            Assert.AreEqual(e, Enum<TestEnum>.Parse("14"));
            Assert.AreEqual(e, Enum<TestEnum>.Parse("Béka, Cica, Kecske"));
            Assert.AreEqual(e, Enum<TestEnum>.Parse("Béka Cica Kecske", " "));
            Assert.AreEqual(e, Enum<TestEnum>.Parse("Béka | Cica | Kecske", "|"));

            e = (TestEnum)(int)e + 16;
            Assert.AreEqual(e, Enum<TestEnum>.Parse("30"));
            Assert.AreEqual(e, Enum<TestEnum>.Parse("Béka, Cica, Kecske, 16"));
            Assert.AreEqual(e, Enum<TestEnum>.Parse("16, Cica, Kecskebéka"));

            Assert.IsFalse(Enum<TestEnum>.TryParse(UInt64.MaxValue.ToString(), out e));
            Assert.IsFalse(Enum<TestEnum>.TryParse("Béka, Cica, , Kecske, 16", out e));

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

            var c2 = EnumComparer<TestEnum>.Comparer;
            var d2 = Comparer<TestEnum>.Default;
            var e2 = EqualityComparer<TestEnum>.Default;
            var v2 = new TestEnum[] { TestEnum.Mínusz, TestEnum.Plusz };

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
            Assert.AreEqual(0, Enum<TestEnum>.GetFlags(TestEnum.Semmi, true).Count());
            Assert.AreEqual(2, Enum<TestEnum>.GetFlags(TestEnum.Kecskebéka, true).Count());
            Assert.AreEqual(5, Enum<TestEnum>.GetFlags((TestEnum)max, true).Count());
            Assert.AreEqual(64, Enum<TestEnum>.GetFlags((TestEnum)max, false).Count());
            Assert.AreEqual(1, Enum<TestEnum>.GetFlags(TestEnum.Mínusz, true).Count());
            Assert.AreEqual(0, Enum<TestUlongEnum>.GetFlags((TestUlongEnum)max, true).Count());
            Assert.AreEqual(64, Enum<TestUlongEnum>.GetFlags((TestUlongEnum)max, false).Count());

            Assert.AreEqual(1, Enum<TestIntEnum>.GetFlags(TestIntEnum.Risky, true).Count());
            Assert.AreEqual(3, Enum<TestIntEnum>.GetFlags(unchecked((TestIntEnum)(int)UInt32.MaxValue), true).Count());
            Assert.AreEqual(32, Enum<TestIntEnum>.GetFlags(unchecked((TestIntEnum)(int)UInt32.MaxValue), false).Count());

            AssertItemsEqual(new[] { TestEnum.Alma, TestEnum.Béka, TestEnum.Cica, TestEnum.Kecske, TestEnum.Mínusz }.OrderBy(e => e), Enum<TestEnum>.GetFlags().OrderBy(e => e));
            AssertItemsEqual(new TestUlongEnum[0], Enum<TestUlongEnum>.GetFlags());
            AssertItemsEqual(new[] { TestIntEnum.Simple, TestIntEnum.Normal, TestIntEnum.Risky }.OrderBy(e => e), Enum<TestIntEnum>.GetFlags().OrderBy(e => e));
            AssertItemsEqual(new EmptyEnum[0], Enum<EmptyEnum>.GetFlags());
        }

        [Test]
        public void AllFlagsDefinedTest()
        {
            Assert.IsTrue(Enum<TestEnum>.AllFlagsDefined(TestEnum.Semmi));
            Assert.IsTrue(Enum<TestEnum>.AllFlagsDefined(TestEnum.Kecskebéka));
            Assert.IsFalse(Enum<TestEnum>.AllFlagsDefined(TestEnum.Plusz));
            Assert.IsTrue(Enum<TestEnum>.AllFlagsDefined(TestEnum.Mínusz));

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
            TestEnum e64 = TestEnum.Alma | TestEnum.Béka;
            Assert.IsTrue(Enum<TestEnum>.HasFlag(e64, TestEnum.Semmi));
            Assert.IsTrue(Enum<TestEnum>.HasFlag(e64, TestEnum.Béka));
            Assert.IsFalse(Enum<TestEnum>.HasFlag(e64, TestEnum.Cica));
            Assert.IsTrue(Enum<TestEnum>.HasFlag(e64, TestEnum.Alma | TestEnum.Béka));
            Assert.IsFalse(Enum<TestEnum>.HasFlag(e64, TestEnum.Kecskebéka));

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
            Assert.IsFalse(Enum<TestEnum>.IsSingleFlag(TestEnum.Semmi));
            Assert.IsTrue(Enum<TestEnum>.IsSingleFlag(TestEnum.Kecske));
            Assert.IsTrue(Enum<TestEnum>.IsSingleFlag(TestEnum.Béka));
            Assert.IsFalse(Enum<TestEnum>.IsSingleFlag(TestEnum.Kecskebéka));
            Assert.IsFalse(Enum<TestEnum>.IsSingleFlag(Int64.MaxValue));
            Assert.IsFalse(Enum<TestEnum>.IsSingleFlag(1 << 63)); // this is -2147483648, which is not a single bit as a long value
            Assert.IsTrue(Enum<TestEnum>.IsSingleFlag(1L << 63)); // this is a single bit negative value, which is valid
            Assert.IsFalse(Enum<TestEnum>.IsSingleFlag(1UL << 63)); // single bit but out of range

            Assert.IsFalse(Enum<TestIntEnum>.IsSingleFlag(1L << 63)); // out of range
            Assert.IsFalse(Enum<TestUlongEnum>.IsSingleFlag(1L << 63)); // this is a negative value: out of range
        }
    }
}
