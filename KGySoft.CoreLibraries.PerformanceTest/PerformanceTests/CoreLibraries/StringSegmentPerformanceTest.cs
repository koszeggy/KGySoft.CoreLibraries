using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class StringSegmentPerformanceTest
    {
        private const string testStringShort = "short: 0123456789";
        private const string testStringLong = "long: 01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";

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
                TestName = $"Length: {s.Length}; Separator: '{sep}'; Count: 2",
                Iterations = 1_000_000
            }
            .AddCase(() => s.Split(sep, 2), "String.Split(char, count)")
            .AddCase(() => s.AsSegment().Split(sep, 2), "StringSegment.Split(char, count)")
            .AddCase(() =>
            {
                var rest = s.AsSegment();
                for (int i = 0; i < 2 && !rest.IsNull; i++)
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
                TestName = $"Length: {s.Length}; Separator: \"{sep}\"; Count: 2",
                Iterations = 1_000_000
            }
            .AddCase(() => s.Split(sep, 2), "String.Split(string, count)")
            .AddCase(() => s.AsSegment().Split(sep.AsSegment(), 2), "StringSegment.Split(StringSegment, count)")
            .AddCase(() => s.AsSegment().Split(sep, 2), "StringSegment.Split(string, count)")
            .AddCase(() =>
            {
                StringSegment separator = sep;
                var rest = s.AsSegment();
                for (int i = 0; i < 2 && !rest.IsNull; i++)
                    rest.ReadToSeparator(separator);
            }, "StringSegmentExtensions.ReadToSeparator(StringSegment)")
            .AddCase(() =>
            {
                var rest = s.AsSegment();
                for (int i = 0; i < 2 && !rest.IsNull; i++)
                    rest.ReadToSeparator(sep);
            }, "StringSegmentExtensions.ReadToSeparator(string)")
            .DoTest()
            .DumpResults(Console.Out);
    }
}
