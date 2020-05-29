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


        [TestCase(-1, null, "")]
        [TestCase(0, "", "")]
        [TestCase(0, "x", "")]
        [TestCase(0, " ", " ")]
        [TestCase(1, " ,, ", ",")]
        [TestCase(2, " ,, ", ", ")]
        [TestCase(1, " ,., ", ",")]
        [TestCase(3, " ,., ", ", ")]
        [TestCase(-1, " ,.", ", ")]
        public void IndexOf(int expectedResult, string s, string toSearch)
        {
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch));
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch.AsSegment()));
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch, 0, s?.Length ?? 0));
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch.AsSegment(), 0, s?.Length ?? 0));
            if (s == null)
                return;

            Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch));
            foreach (StringComparison stringComparison in Enum<StringComparison>.GetValues())
            {
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch.AsSegment(), 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).IndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).IndexOf(toSearch.AsSegment(), 0, s.Length, stringComparison));
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
            Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf(toSearch));
            Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf(toSearch.AsSegment()));
            Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf(toSearch, 0, s.Length));
            Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf(toSearch.AsSegment(), 0, s.Length));
            Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch));
            Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch.AsSegment()));

            foreach (StringComparison stringComparison in Enum<StringComparison>.GetValues())
            {
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch.AsSegment(), 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).LastIndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).LastIndexOf(toSearch.AsSegment(), 0, s.Length, stringComparison));
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

        [TestCase("", "x")]
        [TestCase("x", "")]
        [TestCase("alpha", ",")]
        [TestCase("alpha,  beta", null)]
        [TestCase("alpha,  beta", "")]
        [TestCase("alpha,  beta", " ")]
        [TestCase("alpha,  beta", ",")]
        [TestCase("alpha,  beta", ", ")]
        [TestCase("alpha,  beta", "  ")]
        [TestCase("alpha,", ",")]
        [TestCase(",alpha", ",")]
        [TestCase(",alpha,", ",")]
        [TestCase("  ,,  ", null)]
        [TestCase("  ,,  ", "")]
        [TestCase("  ,,  ", " ")]
        [TestCase("  ,,  ", "  ")]
        [TestCase("  ,,  ", ",")]
        [TestCase("  ,,  ", ",,")]
        public void SplitTest(string s, string separator)
        {
            foreach (bool removeEmpty in new[] { false, true })
            {
                // as string
                string[] strings = s.Split(new[] { separator }, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                string expected = strings.Join("|");

                IList<StringSegment> segments = s.AsSegment().Split(separator, removeEmpty);
                string actual = segments.Join("|");

                Console.WriteLine($@"""{s}"" vs ""{separator ?? "null"}"" (remove empty: {removeEmpty}) => ""{actual}""");
                Assert.AreEqual(expected, actual);

                // as StringSegment
                segments = s.AsSegment().Split(separator.AsSegment(), removeEmpty);
                actual = segments.Join("|");
                Assert.AreEqual(expected, actual);

                // as string array with multiple values
                strings = s.Split(new[] { separator, null }, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                expected = strings.Join("|");

                segments = s.AsSegment().Split(new[] { separator, null }, removeEmpty);
                actual = segments.Join("|");
                Assert.AreEqual(expected, actual);

                // as StringSegment array with multiple values
                segments = s.AsSegment().Split(new StringSegment[] { separator, null }, removeEmpty);
                actual = segments.Join("|");
                Assert.AreEqual(expected, actual);

                // no separator (splitting by whitespaces)
                strings = s.Split(default(string[]), removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                expected = strings.Join("|");

                segments = s.AsSegment().Split(removeEmpty);
                actual = segments.Join("|");

                Console.WriteLine($@"""{s}"" no separator (remove empty: {removeEmpty}) => ""{actual}""");
                Assert.AreEqual(expected, actual);

                if (separator?.Length == 1)
                {
                    // as char
                    strings = s.Split(new[] { separator[0] }, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                    expected = strings.Join("|");

                    segments = s.AsSegment().Split(separator[0], removeEmpty);
                    actual = segments.Join("|");

                    Console.WriteLine($@"""{s}"" vs '{separator[0]}' (remove empty: {removeEmpty}) => ""{actual}""");
                    Assert.AreEqual(expected, actual);

                    // as char array
                    strings = s.Split(new[] { separator[0], '\0' }, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                    expected = strings.Join("|");

                    segments = s.AsSegment().Split(new[] { separator[0], '\0' }, removeEmpty);
                    actual = segments.Join("|");

                    Assert.AreEqual(expected, actual);
                }
            }
        }

        [TestCase("", "x")]
        [TestCase("x", "")]
        [TestCase("alpha", ",")]
        [TestCase("alpha,  beta", null)]
        [TestCase("alpha,  beta", "")]
        [TestCase("alpha,  beta", " ")]
        [TestCase("alpha,  beta", ",")]
        [TestCase("alpha,  beta", ", ")]
        [TestCase("alpha,  beta", "  ")]
        [TestCase("alpha,", ",")]
        [TestCase(",alpha", ",")]
        [TestCase(",alpha,", ",")]
        [TestCase(",,alpha", ",")]
        [TestCase("  ,,  ", null)]
        [TestCase("  ,,  ", "")]
        [TestCase("  ,,  ", " ")]
        [TestCase("  ,,  ", "  ")]
        [TestCase("  ,,  ", ",")]
        [TestCase("  ,,  ", ",,")]
        public void SplitTestWithLength(string s, string separator)
        {
            for (int count = 0; count < 4; count++)
            {
                foreach (bool removeEmpty in new[] { false, true })
                {
                    // as string
                    string[] strings = s.Split(new[] { separator }, count, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                    string expected = strings.Join("|");

                    IList<StringSegment> segments = s.AsSegment().Split(separator, count, removeEmpty);
                    string actual = segments.Join("|");

                    Console.WriteLine($@"""{s}"" vs ""{separator ?? "null"}"" (count: {count}; remove empty: {removeEmpty}) => ""{actual}""");
                    Assert.AreEqual(expected, actual);

                    // as StringSegment
                    segments = s.AsSegment().Split(separator.AsSegment(), count, removeEmpty);
                    actual = segments.Join("|");
                    Assert.AreEqual(expected, actual);

                    // as string array with multiple values
                    strings = s.Split(new[] { separator, null }, count, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                    expected = strings.Join("|");

                    segments = s.AsSegment().Split(new[] { separator, null }, count, removeEmpty);
                    actual = segments.Join("|");

                    Assert.AreEqual(expected, actual);

                    // as StringSegment array with multiple values
                    segments = s.AsSegment().Split(new StringSegment[] { separator, null }, count, removeEmpty);
                    actual = segments.Join("|");
                    Assert.AreEqual(expected, actual);

                    // no separator (splitting by whitespaces)
                    strings = s.Split(default(string[]), count, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                    expected = strings.Join("|");

                    segments = s.AsSegment().Split(count, removeEmpty);
                    actual = segments.Join("|");

                    Console.WriteLine($@"""{s}"" no separator (count: {count}; remove empty: {removeEmpty}) => ""{actual}""");
                    Assert.AreEqual(expected, actual);

                    if (separator?.Length == 1)
                    {
                        // as char
                        strings = s.Split(new[] { separator[0] }, count, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                        expected = strings.Join("|");

                        segments = s.AsSegment().Split(separator[0], count, removeEmpty);
                        actual = segments.Join("|");
                        Console.WriteLine($@"""{s}"" vs '{separator[0]}' (count: {count}; remove empty: {removeEmpty}) => ""{actual}""");
                        Assert.AreEqual(expected, actual);

                        // as char array
                        strings = s.Split(new[] { separator[0], '\0' }, count, removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                        expected = strings.Join("|");

                        segments = s.AsSegment().Split(new[] { separator[0], '\0' }, count, removeEmpty);
                        actual = segments.Join("|");

                        Assert.AreEqual(expected, actual);
                    }
                }
            }
        }

        [TestCase("alpha")]
        public void SubstringTest(string s)
        {
            Assert.AreEqual(s.Substring(1), s.AsSegment().Substring(1).ToString());
            Assert.AreEqual(s.Substring(1).Substring(1), s.AsSegment().Substring(1).Substring(1).ToString());
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            Assert.AreEqual(s[1..^1], s.AsSegment()[1..^1]);
#endif
        }

        #endregion
    }
}