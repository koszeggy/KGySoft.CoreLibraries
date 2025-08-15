﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

using KGySoft.Reflection;

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
            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.OrdinalRandomized.Equals(a, b));
            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.OrdinalRandomized.Equals((object)a, b));
            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.OrdinalRandomized.Equals((object)a, (StringSegment)b));
            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.OrdinalNonRandomized.Equals(a, b));
            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.OrdinalNonRandomized.Equals((object)a, b));
            Assert.AreEqual(StringComparer.Ordinal.Equals(a, b), StringSegmentComparer.OrdinalNonRandomized.Equals((object)a, (StringSegment)b));
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
            Assert.AreEqual(expectedResult >= 0, s.AsSegment().Contains(toSearch));
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch.AsSegment()));
            Assert.AreEqual(expectedResult >= 0, s.AsSegment().Contains(toSearch.AsSegment()));
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch, 0, s?.Length ?? 0));
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch.AsSegment(), 0, s?.Length ?? 0));
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf((" " + toSearch).AsSegment(1), 0, s?.Length ?? 0));
            if (s != null)
            {
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch));
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch.AsSegment()));
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf((" " + toSearch).AsSegment(1)));
            }

            if (toSearch.Length == 1)
            {
                Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch[0]));
                Assert.AreEqual(expectedResult >= 0, s.AsSegment().Contains(toSearch[0]));
                Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch[0], 0));
                Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch[0], 0, s?.Length ?? 0));
            }

#if NETCOREAPP2_1_OR_GREATER
            Assert.AreEqual(expectedResult, s.AsSegment().IndexOf(toSearch.AsSpan(), 0, s?.Length ?? 0));
            if (s != null)
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch.AsSpan(), 0, s?.Length ?? 0));
#endif
            if (s == null)
                return;

            Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch));
            foreach (StringComparison stringComparison in Enum<StringComparison>.GetValues())
            {
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).IndexOf(toSearch.AsSegment(), 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).IndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).IndexOf(toSearch.AsSegment(), 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult >= 0, (" " + s + " ").AsSegment(1, s.Length).Contains(toSearch, stringComparison));
                Assert.AreEqual(expectedResult >= 0, (" " + s + " ").AsSegment(1, s.Length).Contains(toSearch.AsSegment(), stringComparison));

                if (toSearch.Length == 1)
                {
                    Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).IndexOf(toSearch[0], stringComparison));
                    Assert.AreEqual(expectedResult >= 0, (" " + s + " ").AsSegment(1, s.Length).Contains(toSearch[0], stringComparison));
                    Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).IndexOf(toSearch[0], 0, stringComparison));
                    Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).IndexOf(toSearch[0], 0, s.Length, stringComparison));
                }
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
            Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf((" " + toSearch).AsSegment(1), 0, s.Length));
            Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch));
            Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch.AsSegment()));
            Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf((" " + toSearch).AsSegment(1)));
#if NETCOREAPP2_1_OR_GREATER
            Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf(toSearch.AsSpan(), 0, s?.Length ?? 0));
            Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch.AsSpan(), 0, s?.Length ?? 0));
#endif
            if (toSearch.Length == 1)
            {
                Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf(toSearch[0]));
                Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf(toSearch[0], 0));
                Assert.AreEqual(expectedResult, s.AsSegment().LastIndexOf(toSearch[0], 0, s.Length));
            }

            foreach (StringComparison stringComparison in Enum<StringComparison>.GetValues())
            {
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch.AsSegment(), 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s).AsSegment(1).LastIndexOf(toSearch.AsSegment(), 0, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).LastIndexOf(toSearch, 0, s.Length, stringComparison));
                Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).LastIndexOf(toSearch.AsSegment(), 0, s.Length, stringComparison));

                if (toSearch.Length == 1)
                {
                    Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).LastIndexOf(toSearch[0], stringComparison));
                    Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).LastIndexOf(toSearch[0], 0, stringComparison));
                    Assert.AreEqual(expectedResult, (" " + s + " ").AsSegment(1, s.Length).LastIndexOf(toSearch[0], 0, s.Length, stringComparison));
                }
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
            {
                Assert.AreEqual(expectedResult, s.AsSegment().StartsWith(value, stringComparison));
#if NETCOREAPP2_1_OR_GREATER
                Assert.AreEqual(expectedResult, s.AsSegment().StartsWith(value.AsSpan(), stringComparison));
#endif
            }

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
            {
                Assert.AreEqual(expectedResult, s.AsSegment().EndsWith(value.AsSegment(), stringComparison));
#if NETCOREAPP2_1_OR_GREATER
                Assert.AreEqual(expectedResult, s.AsSegment().EndsWith(value.AsSpan(), stringComparison));
#endif
            }

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
        [TestCase(",,alpha", ",")]
        [TestCase("  ,,  ", null)]
        [TestCase("  ,,  ", "")]
        [TestCase("  ,,  ", " ")]
        [TestCase("  ,,  ", "  ")]
        [TestCase("  ,,  ", ",")]
        [TestCase("  ,,  ", ",,")]
        [TestCase("  ,,  ", "x")]

        public void SplitTest(string s, string separator)
        {
            static string[] SystemSplit(string s, string[] separators, StringSegmentSplitOptions options)
            {
#if NET7_0_OR_GREATER // TrimEntries exists since .NET 5 but it worked incorrectly before .NET 7: https://github.com/dotnet/runtime/issues/73194
                return s.Split(separators, (StringSplitOptions)options);
#else
                var sso = (StringSplitOptions)options;
                sso &= (StringSplitOptions)~StringSegmentSplitOptions.TrimEntries;
                string[] result = s.Split(separators, sso);
                if (options.IsTrim)
                    result = result.Select(s => s.Trim()).Where(s => !options.IsRemoveEmpty || s.Length != 0).ToArray();
                return result;
#endif
            }

            var optionsArray = new[]
            {
                StringSegmentSplitOptions.None,
                StringSegmentSplitOptions.RemoveEmptyEntries,
                StringSegmentSplitOptions.TrimEntries,
                StringSegmentSplitOptions.TrimEntries | StringSegmentSplitOptions.RemoveEmptyEntries
            };

            foreach (StringSegmentSplitOptions options in optionsArray)
            {
                // as string
                string[] strings = SystemSplit(s, new[] { separator }, options);
                string expected = strings.Join("|");

                IList<StringSegment> segments = s.AsSegment().Split(separator, options);
                string actual = segments.Join("|");

                Console.WriteLine($@"""{s}"" vs ""{separator ?? "null"}"" ({options}) => ""{actual}""");
                Assert.AreEqual(expected, actual);

                // as StringSegment
                segments = s.AsSegment().Split(separator.AsSegment(), options);
                actual = segments.Join("|");
                Assert.AreEqual(expected, actual);

#if NETCOREAPP2_1_OR_GREATER
                // as span
                segments = s.AsSegment().Split(separator.AsSpan(), options);
                actual = segments.Join("|");
                Assert.AreEqual(expected, actual);
#endif

                // as string array with multiple values
                strings = SystemSplit(s, new[] { separator, null }, options);
                expected = strings.Join("|");

                segments = s.AsSegment().Split(new[] { separator, null }, options);
                actual = segments.Join("|");
                Assert.AreEqual(expected, actual);

                // as StringSegment array with multiple values
                segments = s.AsSegment().Split(new StringSegment[] { separator, null }, options);
                actual = segments.Join("|");
                Assert.AreEqual(expected, actual);

                // no separator (splitting by whitespaces)
                strings = SystemSplit(s, default, options);
                expected = strings.Join("|");

                segments = s.AsSegment().Split(options);
                actual = segments.Join("|");

                Console.WriteLine($@"""{s}"" no separator ({options}) => ""{actual}""");
                Assert.AreEqual(expected, actual);

                if (separator?.Length == 1)
                {
                    // as char
                    strings = SystemSplit(s, new[] { separator[0].ToString() }, options);
                    expected = strings.Join("|");

                    segments = s.AsSegment().Split(separator[0], options);
                    actual = segments.Join("|");

                    Console.WriteLine($@"""{s}"" vs '{separator[0]}' (remove empty: {options}) => ""{actual}""");
                    Assert.AreEqual(expected, actual);

                    // as char array
                    strings = SystemSplit(s, new[] { separator[0].ToString(), '\0'.ToString() }, options);
                    expected = strings.Join("|");

                    segments = s.AsSegment().Split(new[] { separator[0], '\0' }, options);
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
        [TestCase("  ,,  ", "x")]
        public void SplitTestWithLength(string s, string separator)
        {
            static string[] SystemSplit(string s, string[] separators, int count, StringSegmentSplitOptions options)
            {
                // TrimEntries exists since .NET 5 but it worked incorrectly before .NET 7: https://github.com/dotnet/runtime/issues/73194
                // And in .NET 7 String.Split with no separators (splitting by whitespace) was still wrong
#if NET8_0_OR_GREATER 
                return s.Split(separators, count, (StringSplitOptions)options);
#else
                var sso = (StringSplitOptions)options;
                sso &= (StringSplitOptions)~StringSegmentSplitOptions.TrimEntries;
                string[] result = s.Split(separators, count, sso);
                if (options.IsTrim && count > 0 && result.Length > 0)
                    result = count == 1
                        ? result[0].Trim() is { } trimmed && options.IsRemoveEmpty && trimmed.Length == 0
                            ? Reflector.EmptyArray<string>()
                            : new[] { result[0].Trim() }
                        : result.Select(s => s.Trim()).Where(s => !options.IsRemoveEmpty || s.Length != 0).ToArray();

                return result;
#endif
            }

            for (int count = 0; count < 4; count++)
            {
                var optionsArray = new[]
                {
                    StringSegmentSplitOptions.None,
                    StringSegmentSplitOptions.RemoveEmptyEntries,
                    StringSegmentSplitOptions.TrimEntries,
                    StringSegmentSplitOptions.TrimEntries | StringSegmentSplitOptions.RemoveEmptyEntries
                };

                foreach (StringSegmentSplitOptions options in optionsArray)
                {
                    // as string
                    string[] strings = SystemSplit(s, new[] { separator }, count, options);
                    string expected = strings.Join("|");

                    IList<StringSegment> segments = s.AsSegment().Split(separator, count, options);
                    string actual = segments.Join("|");

                    Console.WriteLine($@"""{s}"" vs ""{separator ?? "null"}"" (count: {count}; options: {options}) => ""{actual}""");
                    Assert.AreEqual(expected, actual);

                    // as StringSegment
                    segments = s.AsSegment().Split(separator.AsSegment(), count, options);
                    actual = segments.Join("|");
                    Assert.AreEqual(expected, actual);

                    // as string array with multiple values
                    strings = SystemSplit(s, new[] { separator, null }, count, options);
                    expected = strings.Join("|");

                    segments = s.AsSegment().Split(new[] { separator, null }, count, options);
                    actual = segments.Join("|");

                    Assert.AreEqual(expected, actual);

                    // as StringSegment array with multiple values
                    segments = s.AsSegment().Split(new StringSegment[] { separator, null }, count, options);
                    actual = segments.Join("|");
                    Assert.AreEqual(expected, actual);

                    // no separator (splitting by whitespaces)
                    strings = SystemSplit(s, default, count, options);
                    expected = strings.Join("|");

                    segments = s.AsSegment().Split(count, options);
                    actual = segments.Join("|");

                    Console.WriteLine($@"""{s}"" no separator (count: {count}; options: {options}) => ""{actual}""");
                    Assert.AreEqual(expected, actual);

                    if (separator?.Length == 1)
                    {
                        // as char
                        strings = SystemSplit(s, new[] { separator[0].ToString() }, count, options);
                        expected = strings.Join("|");

                        segments = s.AsSegment().Split(separator[0], count, options);
                        actual = segments.Join("|");
                        Console.WriteLine($@"""{s}"" vs '{separator[0]}' (count: {count}; options: {options}) => ""{actual}""");
                        Assert.AreEqual(expected, actual);

                        // as char array
                        strings = SystemSplit(s, new[] { separator[0].ToString(), '\0'.ToString() }, count, (StringSplitOptions)options);
                        expected = strings.Join("|");

                        segments = s.AsSegment().Split(new[] { separator[0], '\0' }, count, options);
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
#if NETCOREAPP2_1_OR_GREATER
            Assert.AreEqual(s.Substring(1, s.Length - 2), s.AsSegment().Substring(1, s.Length - 2));
#endif
        }

        [TestCase(null, null)]
        [TestCase("", null)]
        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase(" ", null)]
        [TestCase(" x ", null)]
        [TestCase(" x ", "")]
        [TestCase(" x ", " ")]
        [TestCase("abcab", "ab")]
        public void TrimTest(string s, string trimChars)
        {
            char[] chars = trimChars?.ToCharArray();
            StringSegment segment = s;

            // no reference case
            if (s == null)
            {
                Assert.AreEqual(segment, segment.Trim(chars));
#if NETCOREAPP2_1_OR_GREATER
                Assert.AreEqual(segment, segment.Trim(chars.AsSpan()));
#endif
                return;
            }

            string expected = s.Trim(chars);
            Assert.AreEqual(expected, segment.Trim(chars));
            if (chars?.Length == 1)
                Assert.AreEqual(expected, segment.Trim(chars[0]));
#if NETCOREAPP2_1_OR_GREATER
            Assert.AreEqual(expected, segment.Trim(chars.AsSpan()));
#endif
        }

#if NET9_0_OR_GREATER
        [Test]
        public void AlternateComparerTest()
        {
            var dictDefault = new Dictionary<string, string>(StringComparer.Ordinal);
            dictDefault[""] = "Empty";
            dictDefault["1"] = "One";

            var dictCustom = new Dictionary<string, string>(dictDefault, StringSegmentComparer.Ordinal);

            var dictDefaultSpan = dictDefault.GetAlternateLookup<ReadOnlySpan<char>>();
            var dictCustomSpan = dictCustom.GetAlternateLookup<ReadOnlySpan<char>>();

            // Span: empty string and null are considered equal
            Assert.AreEqual("Empty", dictDefaultSpan[""]);
            Assert.AreEqual("Empty", dictDefaultSpan[default]);
            Assert.AreEqual("Empty", dictCustomSpan[""]);
            Assert.AreEqual("Empty", dictCustomSpan[default]);

            // StringSegment: they can be considered different because there are exposed publicly in different ways
            var dictCustomSegment = dictCustom.GetAlternateLookup<StringSegment>();
            Assert.AreEqual("Empty", dictCustomSegment[StringSegment.Empty]);
            Assert.IsFalse(dictCustomSegment.ContainsKey(StringSegment.Null));

            // HashSet: allows null
            var set = new HashSet<string>(StringSegmentComparer.Ordinal);
            var setAsSegment = set.GetAlternateLookup<StringSegment>();
            Assert.IsTrue(setAsSegment.Add(StringSegment.Null));
            Assert.IsTrue(setAsSegment.Add(StringSegment.Empty));
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(setAsSegment.Contains(StringSegment.Null));
            Assert.IsTrue(setAsSegment.Contains(StringSegment.Empty));
            Assert.IsTrue(set.Contains(null));
            Assert.IsTrue(set.Contains(""));
        }
#endif

        #endregion
    }
}