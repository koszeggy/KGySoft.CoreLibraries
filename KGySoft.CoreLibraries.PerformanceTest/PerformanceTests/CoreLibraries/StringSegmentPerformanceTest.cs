#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentPerformanceTest.cs
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
using System.Collections.Generic;
using System.Globalization;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class StringSegmentPerformanceTest
    {
        #region Constants

        private const string testStringShort = "short: 0123456789";
        private const string testStringLong = "long: 01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
        private const int limitCount = 2;

        #endregion

        #region Methods

        [TestCase(testStringShort)]
        [TestCase(testStringLong)]
        public void SubstringTest(string s) => new PerformanceTest
            {
                TestName = $"Length = {s.Length}",
                Iterations = 1_000_000,
            }
            .AddCase(() => s.Substring(1), "String.Substring")
            .AddCase(() => s.AsSegment().Substring(1), "StringSegment.Substring")
            .AddCase(() => new StringSegmentInternal(s).Substring(1), "StringSegmentInternal.Substring")
            .AddCase(() => new StringSegmentInternal(s).Slice(1), "StringSegmentInternal.Slice")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase(testStringShort)]
        [TestCase(testStringLong)]
        public void SubstringTestWithLength(string s) => new PerformanceTest
            {
                TestName = $"Length = {s.Length}",
                Iterations = 1_000_000,
            }
            .AddCase(() => s.Substring(1, 2), "String.Substring")
            .AddCase(() => s.AsSegment().Substring(1, 2), "StringSegment.Substring")
            .AddCase(() => new StringSegmentInternal(s).Substring(1, 2), "StringSegmentInternal.Substring")
            .AddCase(() => new StringSegmentInternal(s).Slice(1, 2), "StringSegmentInternal.Slice")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase(testStringShort)]
        [TestCase(testStringLong)]
        public void SplitWholeByWhitespacesTest(string s) => new PerformanceTest
            {
                TestName = $"Length: {s.Length}; Separator: White spaces",
                Iterations = 1_000_000,
            }
            .AddCase(() => s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries), "String.Split(null, RemoveEmptyEntries)")
            .AddCase(() => s.AsSegment().Split(StringSegmentSplitOptions.RemoveEmptyEntries), "StringSegment.Split(StringSegmentSplitOptions.RemoveEmptyEntries)")
            .AddCase(() =>
            {
                StringSegment rest = s.AsSegment();
                while (!rest.IsNull)
                    rest.ReadToWhiteSpace();
            }, "StringSegmentExtensions.ReadToWhiteSpace()")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase(testStringShort)]
        [TestCase(testStringLong)]
        public void SplitLimitedByWhitespacesTest(string s) => new PerformanceTest
            {
                TestName = $"Length: {s.Length}; Separator: White spaces; Count: {limitCount}",
                Iterations = 1_000_000
            }
            .AddCase(() => s.Split((char[])null, limitCount, StringSplitOptions.RemoveEmptyEntries), "String.Split(null, count, StringSplitOptions.RemoveEmptyEntries)")
            .AddCase(() => s.AsSegment().Split(limitCount, StringSegmentSplitOptions.RemoveEmptyEntries), "StringSegment.Split(count, StringSegmentSplitOptions.RemoveEmptyEntries)")
            .AddCase(() =>
            {
                StringSegment rest = s.AsSegment();
                for (int i = 0; i < limitCount - 1 && !rest.IsNull; i++)
                    rest.ReadToWhiteSpace();
            }, "StringSegmentExtensions.ReadToWhiteSpace(char)")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase('0', testStringShort)]
        [TestCase('0', testStringLong)]
        [TestCase(':', testStringShort)]
        [TestCase(':', testStringLong)]
        public void SplitWholeStringByCharTest(char sep, string s) => new PerformanceTest
            {
                TestName = $"Length: {s.Length}; Separator: '{sep}'",
                Iterations = 1_000_000
            }
            .AddCase(() => s.Split(sep), "String.Split(char)")
            .AddCase(() => s.AsSegment().Split(sep), "StringSegment.Split(char)")
            .AddCase(() =>
            {
                var rest = s.AsSegment();
                while (!rest.IsNull)
                    rest.ReadToSeparator(sep);
            }, "StringSegmentExtensions.ReadToSeparator(char)")
            .AddCase(() => new StringSegmentInternal(s).Split(sep), "StringSegmentInternal.Split(char)")
            .AddCase(() =>
            {
                var rest = new StringSegmentInternal(s);
                while (rest.TryGetNextSegment(sep, out var _)) { }
            }, "StringSegmentInternal.TryGetNextSegment(char)")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase('0', testStringShort)]
        [TestCase('0', testStringLong)]
        [TestCase(':', testStringShort)]
        [TestCase(':', testStringLong)]
        public void SplitLimitedByCharTest(char sep, string s) => new PerformanceTest
            {
                TestName = $"Length: {s.Length}; Separator: '{sep}'; Count: {limitCount}",
                Iterations = 1_000_000
            }
            .AddCase(() => s.Split(new[] { sep }, limitCount), "String.Split(char, count)")
            .AddCase(() => s.AsSegment().Split(sep, limitCount), "StringSegment.Split(char, count)")
            .AddCase(() =>
            {
                StringSegment rest = s.AsSegment();
                for (int i = 0; i < limitCount - 1 && !rest.IsNull; i++)
                    rest.ReadToSeparator(sep);
            }, "StringSegmentExtensions.ReadToSeparator(char)")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase("01", testStringShort)]
        [TestCase("01", testStringLong)]
        [TestCase(": ", testStringShort)]
        [TestCase(": ", testStringLong)]
        public void SplitWholeStringByStringTest(string sep, string s) => new PerformanceTest
            {
                TestName = $"Length: {s.Length}; Separator: \"{sep}\"",
                Iterations = 1_000_000
            }
            .AddCase(() => s.Split(new[] { sep }, StringSplitOptions.None), "String.Split(string)")
            .AddCase(() => s.AsSegment().Split(sep.AsSegment()), "StringSegment.Split(StringSegment)")
            .AddCase(() => s.AsSegment().Split(sep), "StringSegment.Split(string)")
            .AddCase(() =>
            {
                StringSegment separator = sep;
                var rest = s.AsSegment();
                while (!rest.IsNull)
                    rest.ReadToSeparator(separator);
            }, "StringSegmentExtensions.ReadToSeparator(StringSegment)")
            .AddCase(() =>
            {
                var rest = s.AsSegment();
                while (!rest.IsNull)
                    rest.ReadToSeparator(sep);
            }, "StringSegmentExtensions.ReadToSeparator(string)")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase("01", testStringShort)]
        [TestCase("01", testStringLong)]
        [TestCase(": ", testStringShort)]
        [TestCase(": ", testStringLong)]
        public void SplitLimitedByStringTest(string sep, string s) => new PerformanceTest
            {
                TestName = $"Length: {s.Length}; Separator: \"{sep}\"; Count: {limitCount}",
                Iterations = 1_000_000
            }
            .AddCase(() => s.Split(new[] { sep }, limitCount, StringSplitOptions.None), "String.Split(string, count)")
            .AddCase(() => s.AsSegment().Split(sep.AsSegment(), limitCount), "StringSegment.Split(StringSegment, count)")
            .AddCase(() => s.AsSegment().Split(sep, limitCount), "StringSegment.Split(string, count)")
            .AddCase(() =>
            {
                StringSegment separator = sep;
                StringSegment rest = s.AsSegment();
                for (int i = 0; i < limitCount - 1 && !rest.IsNull; i++)
                    rest.ReadToSeparator(separator);
            }, "StringSegmentExtensions.ReadToSeparator(StringSegment)")
            .AddCase(() =>
            {
                StringSegment rest = s.AsSegment();
                for (int i = 0; i < limitCount - 1 && !rest.IsNull; i++)
                    rest.ReadToSeparator(sep);
            }, "StringSegmentExtensions.ReadToSeparator(string)")
            .DoTest()
            .DumpResults(Console.Out);
    }

    #endregion
}
