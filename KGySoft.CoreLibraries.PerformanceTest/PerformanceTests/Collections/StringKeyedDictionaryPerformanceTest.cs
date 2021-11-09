#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringKeyedDictionaryPerformanceTest.cs
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
using System.Linq;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    [TestFixture]
    public class StringKeyedDictionaryPerformanceTest
    {
        #region Methods

        [Test]
        public void PopulateTest()
        {
            const int count = 1_000_000;
            var dict = new Dictionary<string, int>(count);
            var strDict = new StringKeyedDictionary<int>(count);
            new IteratorPerformanceTest { TestName = "Populate test", Iterations = count }
                .AddCase(i => dict[i.ToString(CultureInfo.InvariantCulture)] = i, "Dictionary")
                .AddCase(i => strDict[i.ToString(CultureInfo.InvariantCulture)] = i, "StringKeyedDictionary")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [TestCase(null)]
        [TestCase(StringComparison.Ordinal)]
        [TestCase(StringComparison.OrdinalIgnoreCase)]
        [TestCase(StringComparison.InvariantCulture)]
        [TestCase(StringComparison.InvariantCultureIgnoreCase)]
        public void AccessTest(StringComparison? comparison)
        {
            const int count = 100_000;
            StringComparer sc = null;
            StringSegmentComparer ssc = null;
            if (comparison != null)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                sc = comparison.Value switch
                {
                    StringComparison.Ordinal => StringComparer.Ordinal,
                    StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
                    StringComparison.InvariantCulture => StringComparer.InvariantCulture,
                    StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
                    _ => throw new ArgumentOutOfRangeException(nameof(comparison))
                };
#else
                sc = StringComparer.FromComparison(comparison.Value); 
#endif
                ssc = StringSegmentComparer.FromComparison(comparison.Value);
            }

            Dictionary<string, int> dictionary = Enumerable.Range(0, count).ToDictionary(i => i.ToString(CultureInfo.InvariantCulture), sc);
            var strDict = new StringKeyedDictionary<int>(dictionary, ssc);

            new IteratorPerformanceTest<int> { TestName = "Indexer access test", Iterations = count }
                .AddCase(i => dictionary[i.ToString(CultureInfo.InvariantCulture)], "Dictionary read")
                .AddCase(i => strDict[i.ToString(CultureInfo.InvariantCulture)], "StringKeyedDictionary read (string)")
                .AddCase(i => strDict[i.ToString(CultureInfo.InvariantCulture).AsSegment()], "StringKeyedDictionary read (StringSegment)")
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                .AddCase(i => strDict[i.ToString(CultureInfo.InvariantCulture).AsSpan()], "StringKeyedDictionary read (ReadOnlySpan<char>)") 
#endif
                .DoTest()
                .DumpResults(Console.Out);

        }

        [Test]
        public void EnumerationTest()
        {
            const int count = 1000;
            Dictionary<string, int> dictionary = Enumerable.Range(0, count).ToDictionary(i => i.ToString(CultureInfo.InvariantCulture));
            var strDict = new StringKeyedDictionary<int>(dictionary);

            new PerformanceTest<int> { TestName = "Indexer access test", Iterations = count }
                .AddCase(() =>
                {
                    int sum = 0;
                    foreach (KeyValuePair<string, int> item in dictionary)
                        sum += item.Value;

                    return sum;
                }, "Dictionary foreach")
                .AddCase(() => dictionary.Sum(item => item.Value), "Dictionary LINQ")
                .AddCase(() =>
                {
                    int sum = 0;
                    foreach (KeyValuePair<string, int> item in strDict)
                        sum += item.Value;

                    return sum;
                }, "StringKeyedDictionary foreach")
                .AddCase(() => strDict.Sum(item => item.Value), "StringKeyedDictionary LINQ")
                .DoTest()
                .DumpResults(Console.Out);
        }


        #endregion
    }
}
