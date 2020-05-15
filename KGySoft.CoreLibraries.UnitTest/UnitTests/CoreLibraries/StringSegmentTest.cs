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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
                Assert.AreEqual(Math.Sign(String.Compare(a, b, comparison)), Math.Sign(StringSegment.Compare(a, b, comparison)));
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

        [TestCase(true, " ", "")]
        [TestCase(true, " ", " ")]
        [TestCase(false, "", " ")]
        [TestCase(false, " ,, ", ",")]
        [TestCase(false, ",, ", ", ")]
        [TestCase(true, " ,, ", " ,")]
        public void StartsWith(bool expectedResult, string s, string value)
        {
            foreach (StringComparison stringComparison in Enum<StringComparison>.GetValues())
                Assert.AreEqual(expectedResult, s.AsSegment().StartsWith(value, stringComparison));

            if (value.Length == 1)
                Assert.AreEqual(expectedResult, s.AsSegment().StartsWith(value[0]));
        }

        [TestCase(true, " ", "")]
        [TestCase(true, " ", " ")]
        [TestCase(false, "", " ")]
        [TestCase(false, " ,, ", ",")]
        [TestCase(true, ",, ", ", ")]
        [TestCase(false, " ,, ", " ,")]
        public void EndsWith(bool expectedResult, string s, string value)
        {
            foreach (StringComparison stringComparison in Enum<StringComparison>.GetValues())
                Assert.AreEqual(expectedResult, s.AsSegment().EndsWith(value, stringComparison));

            if (value.Length == 1)
                Assert.AreEqual(expectedResult, s.AsSegment().EndsWith(value[0]));
        }

        [TestCase("", "x", true, 1)]
        [TestCase("", "x", false, 0)]
        [TestCase("x", "", true, 1)]
        [TestCase("x", "", false, 1)]
        [TestCase("alpha", ",", true, 1)]
        [TestCase("alpha", ",", false, 1)]
        [TestCase("alpha, beta", ",", true, 2)]
        [TestCase("alpha, beta", ",", false, 2)]
        [TestCase("alpha, beta", ", ", true, 2)]
        [TestCase("alpha, beta", ", ", false, 2)]
        [TestCase("alpha,beta", ", ", true, 1)]
        [TestCase("alpha,beta", ", ", false, 1)]
        [TestCase("alpha,", ",", true, 2)]
        [TestCase("alpha,", ",", false, 1)]
        [TestCase(",alpha", ",", true, 2)]
        [TestCase(",alpha", ",", false, 1)]
        [TestCase(",alpha,", ",", true, 3)]
        [TestCase(",alpha,", ",", false, 1)]
        [TestCase(",,", ",", true, 3)]
        [TestCase(",,", ",", false, 0)]
        public void SplitTest(string s, string separator, bool allowEmpty, int expectedCount)
        {
            StringSegment[] segments = s.AsSegment().Split(separator, allowEmpty).ToArray();
            Console.WriteLine($@"""{s}"" vs ""{separator}"" ({(allowEmpty ? "allow empty" : "remove empty")}) => ""{String.Join('|', segments)}""");
            Assert.AreEqual(expectedCount, segments.Length);
            
            if (separator.Length == 1)
            {
                Assert.AreEqual(expectedCount, s.AsSegment().Split(separator[0], allowEmpty).ToCircularList().Count); // enumerable
                Assert.AreEqual(expectedCount, s.AsSegment().Split(separator[0], allowEmpty).ToList().Count);
            }
        }

        [TestCase("", "x", true, 1)]
        [TestCase("", "x", false, 2)]
        //[TestCase("x", "", true, 1)]
        //[TestCase("x", "", false, 2)]
        [TestCase("alpha", ",", true, 0)]
        [TestCase("alpha", ",", false, 1)]
        [TestCase("alpha, beta", ",", true, 0)]
        [TestCase("alpha, beta", ",", false, 1)]
        //[TestCase("alpha, beta", ", ", true, 2)]
        //[TestCase("alpha, beta", ", ", false, 3)]
        //[TestCase("alpha,beta", ", ", true, 0)]
        //[TestCase("alpha,beta", ", ", true, 1)]
        //[TestCase("alpha,beta", ", ", false, 0)]
        //[TestCase("alpha,beta", ", ", false, 1)]
        [TestCase("alpha,,", ",", true, 0)]
        [TestCase("alpha,,", ",", true, 1)]
        [TestCase("alpha,,", ",", true, 2)]
        [TestCase("alpha,,", ",", true, 3)]
        [TestCase("alpha,,", ",", false, 0)]
        [TestCase("alpha,,", ",", false, 1)]
        [TestCase("alpha,,", ",", false, 2)]
        [TestCase("alpha,,", ",", false, 3)]
        [TestCase(",,", ",", true, 0)]
        [TestCase(",,", ",", true, 1)]
        [TestCase(",,", ",", true, 2)]
        [TestCase(",,", ",", true, 3)]
        [TestCase(",,", ",", true, 4)]
        [TestCase(",,", ",", false, 0)]
        [TestCase(",,", ",", false, 1)]
        [TestCase(",,", ",", false, 2)]
        [TestCase(",,", ",", false, 3)]
        [TestCase(",,", ",", false, 4)]
        [TestCase(",,alpha", ",", false, 0)]
        [TestCase(",,alpha", ",", false, 1)]
        [TestCase(",,alpha", ",", false, 2)]
        [TestCase(",,alpha", ",", false, 3)]
        [TestCase(",,alpha", ",", false, 4)]
        [TestCase(",,alpha", ",", true, 0)]
        [TestCase(",,alpha", ",", true, 1)]
        [TestCase(",,alpha", ",", true, 2)]
        [TestCase(",,alpha", ",", true, 3)]
        [TestCase(",,alpha", ",", true, 4)]
        public void SplitTestWithLength(string s, string separator, bool allowEmpty, int count)
        {
            string[] strings = s.Split(new[] { separator }, count, allowEmpty ? StringSplitOptions.None : StringSplitOptions.RemoveEmptyEntries);
            string expected = String.Join('|', strings);
            StringSegment[] segments = s.AsSegment().Split(separator, allowEmpty).ToArray(count);
            string actual = String.Join('|', segments);
            Console.WriteLine($@"""{s}"" vs ""{separator}"" count={count} ({(allowEmpty ? "allow empty" : "remove empty")}) => ""{actual}""");
            Assert.AreEqual(expected, actual);

            if (separator.Length == 1)
            {
                segments = s.AsSegment().Split(separator[0], allowEmpty).ToArray(count);
                actual = String.Join('|', segments);
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("alpha")]
        public void SubstringTest(string s)
        {
            Assert.AreEqual(s.Substring(1), s.AsSegment().Substring(1).ToString());
            Assert.AreEqual(s.Substring(1).Substring(1), s.AsSegment().Substring(1).Substring(1).ToString());
        }

        #endregion
    }
}