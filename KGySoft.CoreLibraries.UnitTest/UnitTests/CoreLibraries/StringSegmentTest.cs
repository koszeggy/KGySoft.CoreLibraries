#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentTest.cs
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
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries
{
    [TestFixture]
    public class StringSegmentTest
    {
        #region Methods

        [Test]
        public void ConversionsNullAndEmptyTest()
        {
            StringSegment ss = null;
            Assert.IsTrue(ss == null, "Compare with null works due to implicit operator and string comparison");
            Assert.IsNotNull(ss, "SS is actually a value type");
            Assert.IsTrue(ss.IsNull);
            Assert.IsNull(ss.ToString());
            Assert.IsNull((string)ss);

            ss = "";
            Assert.IsTrue(ss == "", "Implicit operator and string comparison");
            Assert.IsFalse(ss.IsNull);
            Assert.IsTrue(ss.IsNullOrEmpty);
            Assert.IsTrue(ss.Length == 0);
            Assert.AreEqual("", ss.ToString());
            Assert.IsTrue(ss == "");
        }

        [Test]
        public void EqualsTest()
        {
            StringSegment ss = null;
            Assert.IsTrue(ss.Equals(null));
            Assert.IsTrue(ss.Equals((object)null));

            ss = "";
            Assert.IsTrue(ss.Equals(""));
            Assert.IsTrue(ss.Equals((object)""));
           
            Assert.AreNotEqual(StringSegment.Null, StringSegment.Empty);
        }

        [Test]
        public void GetHashCodeTest()
        {
            Assert.AreNotEqual(StringSegment.Null.GetHashCode(), StringSegment.Empty.GetHashCode());
        }

        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase("", null)]
        [TestCase("alpha", "alpha")]
        [TestCase("alpha", "beta")]
        [TestCase("beta", "alpha")]
        [TestCase("alpha", "alphabet")]
        [TestCase("alphabet", "alpha")]
        public void ComparisonsTest(string a, string b)
        {
            Assert.AreEqual(a == b, a.AsSegment() == b.AsSegment());

            foreach (StringComparison comparison in Enum<StringComparison>.GetValues())
            {
                Assert.AreEqual(String.Equals(a, b, comparison), StringSegment.Equals(a, b, comparison));
                Assert.AreEqual(Math.Sign(String.Compare(a, b, comparison)), Math.Sign( StringSegment.Compare(a, b, comparison)));
            }

            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.Ordinal.Equals(a, b));
            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.Ordinal.Equals((object)a, b));
            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.Ordinal.Equals((object)a, (StringSegment)b));
            Assert.AreEqual(Math.Sign(StringComparer.Ordinal.Compare(a, b)), Math.Sign(StringSegmentComparer.Ordinal.Compare(a, b)));
            Assert.AreEqual(Math.Sign(StringComparer.Ordinal.Compare(a, b)), Math.Sign(StringSegmentComparer.Ordinal.Compare((object)a, b)));
            Assert.AreEqual(Math.Sign(StringComparer.Ordinal.Compare(a, b)), Math.Sign(StringSegmentComparer.Ordinal.Compare((object)a, (StringSegment)b)));
            Assert.AreEqual(StringComparer.OrdinalIgnoreCase.Equals(a, b), StringSegmentComparer.OrdinalIgnoreCase.Equals(a, b));
            Assert.AreEqual(Math.Sign(StringComparer.OrdinalIgnoreCase.Compare(a, b)), Math.Sign(StringSegmentComparer.OrdinalIgnoreCase.Compare(a, b)));
            Assert.AreEqual(StringComparer.CurrentCulture.Equals(a, b), StringSegmentComparer.CurrentCulture.Equals(a, b));
            Assert.AreEqual(Math.Sign(StringComparer.CurrentCulture.Compare(a, b)), Math.Sign(StringSegmentComparer.CurrentCulture.Compare(a, b)));
            Assert.AreEqual(StringComparer.CurrentCultureIgnoreCase.Equals(a, b), StringSegmentComparer.CurrentCultureIgnoreCase.Equals(a, b));
            Assert.AreEqual(Math.Sign(StringComparer.CurrentCultureIgnoreCase.Compare(a, b)), Math.Sign(StringSegmentComparer.CurrentCultureIgnoreCase.Compare(a, b)));
        }


        [TestCase(0, " ", " ")]
        [TestCase(1, " ,, ", ",")]
        [TestCase(2, " ,, ", ", ")]
        [TestCase(1, " ,., ", ",")]
        [TestCase(3, " ,., ", ", ")]
        [TestCase(-1, " ,.", ", ")]
        public void IndexOf(int expectedResult, string s, string toSearch)
        {
            Assert.AreEqual(expectedResult, new StringSegment(s).IndexOf(toSearch));
            Assert.AreEqual(expectedResult, new StringSegment(s).IndexOf(toSearch, 0, s.Length));
            Assert.AreEqual(expectedResult, new StringSegment(" " + s, 1).IndexOf(toSearch));

            foreach (StringComparison stringComparison in Enum<StringComparison>.GetValues())
            {
                Assert.AreEqual(expectedResult, new StringSegment(" " + s, 1).IndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult < 0 ? expectedResult : expectedResult + 1, new StringSegment(" " + s + " ").IndexOf(toSearch, 1, s.Length, stringComparison));
            }
        }

        [TestCase(0, " ", " ")]
        [TestCase(2, " ,, ", ",")]
        [TestCase(2, " ,, ", ", ")]
        [TestCase(3, " ,., ", ",")]
        [TestCase(3, " ,., ", ", ")]
        [TestCase(-1, " ,.", ", ")]
        public void LastIndexOf(int expectedResult, string s, string toSearch)
        {
            Assert.AreEqual(expectedResult, new StringSegment(s).LastIndexOf(toSearch));
            Assert.AreEqual(expectedResult, new StringSegment(s).LastIndexOf(toSearch, 0, s.Length));
            Assert.AreEqual(expectedResult, new StringSegment(" " + s, 1).LastIndexOf(toSearch));

            foreach (StringComparison stringComparison in Enum<StringComparison>.GetValues())
            {
                Assert.AreEqual(expectedResult, new StringSegment(" " + s, 1).LastIndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult < 0 ? expectedResult : expectedResult + 1, new StringSegment(" " + s + " ").LastIndexOf(toSearch, 1, s.Length, stringComparison));
            }
        }

        private const string a = "+123456789+123456789+123456789+123456789+123456789+123456789+123456789";
        private const string b = "6789+123";

        [Test]
        public void PerfTest() => new KGySoft.Diagnostics.PerformanceTest<int> { Iterations = 10_000_000 }
            //.AddCase(() => String.Compare(a, 1, b, 1, 9, StringComparison.Ordinal) == 0, "Compare")
            //.AddCase(() => a.AsSpan(1, 9).SequenceEqual(b.AsSpan(1, 9)), "AsSpan")
            .AddCase(() => A(a, b, 0, 30), "A")
            .AddCase(() => B(a, b, 0, 30), "B")
            .DoTest()
            .DumpResults(Console.Out);

        private static int A(StringSegment ss, StringSegment s, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            return ss.IndexOfInternal(s, startIndex, count);
        }

        private static int B(StringSegment ss, StringSegment s, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            int result = ss.AsSpan.Slice(startIndex, count).IndexOf(s.AsSpan, comparison);
            return result >= 0 ? result + startIndex : -1;
        }

        //private static int GetHashCode(string str)
        //{
        //    var length = str.Length;
        //    if (length == 0)
        //        return 0;

        //    //return String.GetHashCode(Span);

        //    // This is a much cheaper hash code than the one used by string
        //    // Of course, we utilize that StringString is internal and used in dictionaries for enums with typically short names.
        //    var result = 13;
        //    for (int i = 0; i < length; i++)
        //        result = result * 397 + str[i];

        //    return result;
        //}

        #endregion
    }
}