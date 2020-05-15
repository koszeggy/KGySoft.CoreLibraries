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
        private const string testStringShort = "short";
        private const string testStringLonger = "longer: 01234567890123456789012345678901234567890123456789";
        private const string testStringLong = "long: 01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";

        [TestCase(testStringShort)]
        [TestCase(testStringLonger)]
        [TestCase(testStringLong)]
        public void SubstringTest(string s) => new PerformanceTest { TestName = $"Length = {s.Length}", Iterations = 1_000_000 }
            .AddCase(() => s.Substring(1), "String.Substring")
            .AddCase(() => s.AsSegment().Substring(1), "StringSegment.Substring")
            .AddCase(() => s.AsSegment().SubstringInternal(1), "StringSegment.SubstringInternal")
            .AddCase(() => new MutableStringSegment(s).Substring(1), "MutableStringSegment.Substring")
            .AddCase(() => new MutableStringSegment(s).Slice(1), "MutableStringSegment.Slice")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase(testStringShort, '0')]
        [TestCase(testStringLonger, '0')]
        [TestCase(testStringLong, '0')]
        public void SplitCharWholeTest(string s, char sep) => new KGySoft.Diagnostics.PerformanceTest { TestName = $"Length = {s.Length}", Iterations = 1_000_000, Repeat = 3 }
            .AddCase(() => s.Split(sep), "String.Split(char)")
            .AddCase(() => s.AsSegment().Split(sep).ToList(), "StringSegment.Split(char).ToList")
            .AddCase(() => s.AsSegment().Split(sep).ToArray(), "StringSegment.Split(char).ToArray")
            .AddCase(() => Enumerable.ToList(s.AsSegment().Split(sep)), "Enumerable.ToList(StringSegment.Split(char))")
            .AddCase(() =>
            {
                var rest = s.AsSegment();
                while (!rest.IsNull)
                    StringSegment.GetNextSegment(ref rest, sep);
            }, "StringSegment.GetNextSegment")
            .AddCase(() =>
            {
                foreach (StringSegment stringSegment in s.AsSegment().Split(sep))
                {
                }
            }, "foreach on StringSplitter")
            .DoTest()
            .DumpResults(Console.Out);

        [TestCase(testStringShort, '0', 1)]
        [TestCase(testStringShort, '0', 2)]
        [TestCase(testStringLonger, '0', 1)]
        [TestCase(testStringLonger, '0', 2)]
        [TestCase(testStringLong, '0', 1)]
        [TestCase(testStringLong, '0', 2)]
        public void SplitCharFirstTest(string s, char sep, int count) => new KGySoft.Diagnostics.PerformanceTest { TestName = $"Length = {s.Length}; Count = {count}", Iterations = 1_000_000, Repeat = 3 }
            .AddCase(() => s.Split(new[] { sep }, count), "String.Split(char, count)")
            .AddCase(() => s.AsSegment().Split(sep).Take(count), "StringSegment.Split(char).Take(count) (enumerator)")
            .AddCase(() => s.AsSegment().Split(sep).ToList(count), "StringSegment.Split(char).ToList(count)")
            .AddCase(() => s.AsSegment().Split(sep).ToArray(count), "StringSegment.Split(char).ToArray(count)")
            .AddCase(() =>
            {
                var rest = s.AsSegment();
                for (int i = 0; i < count && !rest.IsNull; i++)
                    StringSegment.GetNextSegment(ref rest, sep);
            }, "for, StringSegment.GetNextSegment")
            .DoTest()
            .DumpResults(Console.Out);

    }
}
