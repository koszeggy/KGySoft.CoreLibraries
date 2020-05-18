#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
                Iterations = 1_000_000
            }
            .AddCase(() => s.Substring(1), "String.Substring")
            .AddCase(() => s.AsSegment().Substring(1), "StringSegment.Substring")
            .AddCase(() => s.AsSegment().SubstringInternal(1), "StringSegment.SubstringInternal")
            .AddCase(() => new MutableStringSegment(s).Substring(1), "MutableStringSegment.Substring")
            .AddCase(() => new MutableStringSegment(s).Slice(1), "MutableStringSegment.Slice")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase(testStringShort)]
        [TestCase(testStringLong)]
        public void SplitLimitedByWhitespacesTest(string s) => new PerformanceTest
            {
                TestName = $"Length: {s.Length}; Separator: White spaces; Count: {limitCount}",
                Iterations = 1_000_000
            }
            .AddCase(() => s.Split((char[])null, limitCount, StringSplitOptions.RemoveEmptyEntries), "String.Split(count)")
            .AddCase(() => s.AsSegment().Split(limitCount), "StringSegment.Split(count)")
            .AddCase(() =>
            {
                StringSegment rest = s.AsSegment();
                for (int i = 0; i < limitCount - 1 && !rest.IsNull; i++)
                    rest.ReadToWhiteSpace();
            }, "StringSegmentExtensions.ReadToSeparator(char)")
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
            .AddCase(() => s.Split(sep, limitCount), "String.Split(char, count)")
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
            .AddCase(() => s.Split(sep), "String.Split(string)")
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
            .AddCase(() => s.Split(sep, limitCount), "String.Split(string, count)")
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
