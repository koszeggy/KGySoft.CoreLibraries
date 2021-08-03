#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CircularSortedListPerformanceTest.cs
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
using System.Diagnostics;

using KGySoft.Collections;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    [TestFixture]
    public class CircularSortedListPerformanceTest
    {
        #region Enumerations

        private enum LongEnum : long { }

        #endregion

        #region Methods

        #region Static Methods

        /// <summary>
        /// Creates a list with given conditions
        /// </summary>
        private static CircularSortedList<TKey, TValue> PrepareList<TKey, TValue>(int capacity, int startIndex, int count)
        {
            CircularSortedList<TKey, TValue> result = new CircularSortedList<TKey, TValue>(capacity);
            var keys = Reflector.GetField(result, "keys");
            Reflector.SetField(keys, "startIndex", startIndex);
            var values = Reflector.GetField(result, "values");
            Reflector.SetField(values, "startIndex", startIndex);

            for (int i = 0; i < count; i++)
                result.Add(i.Convert<TKey>(), i.Convert<TValue>());

            return result;
        }

        #endregion

        #region Instance Methods

        [Test]
        public void PopulateTest()
        {
            const int capacity = 10_000;

            new PerformanceTest { TestName = "Populate Test", Iterations = 100 }
                .AddCase(() =>
                {
                    var slist = new SortedList<int, string>(capacity);
                    for (int i = 0; i < capacity; i++)
                        slist.Add(i, null);
                }, "Populating SortedList from sorted data")
                .AddCase(() =>
                {
                    var sdict = new SortedDictionary<int, string>();
                    for (int i = 0; i < capacity; i++)
                        sdict.Add(i, null);
                }, "Populating SortedDictionary from sorted data")
                .AddCase(() =>
                {
                    var cslist = new CircularSortedList<int, string>(capacity);
                    for (int i = 0; i < capacity; i++)
                        cslist.Add(i, null);
                }, "Populating CircularSortedList from sorted data")
                .AddCase(() =>
                {
                    var slist = new SortedList<int, string>(capacity);
                    for (int i = capacity - 1; i >= 0; i--)
                        slist.Add(i, null);
                }, "Populating SortedList from reverse sorted data")
                .AddCase(() =>
                {
                    var sdict = new SortedDictionary<int, string>();
                    for (int i = capacity - 1; i >= 0; i--)
                        sdict.Add(i, null);
                }, "Populating SortedDictionary from reverse sorted data")
                .AddCase(() =>
                {
                    var cslist = new CircularSortedList<int, string>(capacity);
                    for (int i = capacity - 1; i >= 0; i--)
                        cslist.Add(i, null);
                }, "Populating CircularSortedList from reverse sorted data")
                .DoTest()
                .DumpResults(Console.Out);

            new RandomizedPerformanceTest { TestName = "Randomized Populate Test", Iterations = 100 }
                .AddCase(rnd =>
                {
                    var slist = new SortedList<int, string>(capacity);
                    for (int i = 0; i < capacity; i++)
                        slist[rnd.Next(Int32.MaxValue)] = null;
                }, "Populating SortedList from random data")
                .AddCase(rnd =>
                {
                    var sdict = new SortedDictionary<int, string>();
                    for (int i = 0; i < capacity; i++)
                        sdict[rnd.Next(Int32.MaxValue)] = null;
                }, "Populating SortedDictionary from random data")
                .AddCase(rnd =>
                {
                    var cslist = new CircularSortedList<int, string>(capacity);
                    for (int i = 0; i < capacity; i++)
                        cslist[rnd.Next(Int32.MaxValue)] = null;
                }, "Populating CircularSortedList from random data")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void EnumeratingTest()
        {
            const int capacity = 1000;

            CircularSortedList<int, string> cslistShifted = PrepareList<int, string>(capacity, capacity >> 1, capacity);
            CircularSortedList<int, string> cslist = new CircularSortedList<int, string>(cslistShifted);
            SortedList<int, string> slist = new SortedList<int, string>(cslist);
            SortedDictionary<int, string> sdict = new SortedDictionary<int, string>(cslist);

            new PerformanceTest { TestName = "Enumeration Test", Iterations = 10_000 }
                .AddCase(() =>
                {
                    foreach (var item in slist) { }
                }, "Enumerating SortedList")
                .AddCase(() =>
                {
                    foreach (var item in sdict) { }
                }, "Enumerating SortedDictionary")
                .AddCase(() =>
                {
                    foreach (var item in cslist) { }
                }, "Enumerating CircularSortedList (0-aligned)")
                .AddCase(() =>
                {
                    foreach (var item in cslistShifted) { }
                }, "Enumerating CircularSortedList (shifted)")
                .AddCase(() =>
                {
                    IEnumerable<KeyValuePair<int, string>> e = slist;
                    foreach (var item in e) { }
                }, "Enumerating SortedList as IEumerable")
                .AddCase(() =>
                {
                    IEnumerable<KeyValuePair<int, string>> e = sdict;
                    foreach (var item in e) { }
                }, "Enumerating SortedDictionary as IEumerable")
                .AddCase(() =>
                {
                    IEnumerable<KeyValuePair<int, string>> e = cslist;
                    foreach (var item in e) { }
                }, "Enumerating CircularSortedList (0-aligned) as IEumerable")
                .AddCase(() =>
                {
                    IEnumerable<KeyValuePair<int, string>> e = cslistShifted;
                    foreach (var item in e) { }
                }, "Enumerating CircularSortedList (shifted) as IEumerable")
                .AddCase(() =>
                {
                    for (int i = 0; i < slist.Count; i++)
                    {
                        var x = slist.Keys[i];
                    }
                }, "Enumerating SortedList.Keys by indexer")
                .AddCase(() =>
                {
                    for (int i = 0; i < cslist.Count; i++)
                    {
                        var x = cslist.Keys[i];
                    }
                }, "Enumerating CircularSortedList.Keys (0-aligned) by indexer")
                .AddCase(() =>
                {
                    for (int i = 0; i < cslistShifted.Count; i++)
                    {
                        var x = cslistShifted.Keys[i];
                    }
                }, "Enumerating CircularSortedList.Keys (shifted) by indexer")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void SearchTest()
        {
            const int capacity = 10_000;

            CircularSortedList<int, int> cslistShifted = PrepareList<int, int>(capacity, capacity >> 1, capacity);
            CircularSortedList<int, int> cslist = new CircularSortedList<int, int>(cslistShifted);
            SortedList<int, int> slist = new SortedList<int, int>(cslist);
            SortedDictionary<int, int> sdict = new SortedDictionary<int, int>(cslist);

            CircularSortedList<LongEnum, int> cslistEnumShifted = PrepareList<LongEnum, int>(capacity, 0, capacity);
            CircularSortedList<LongEnum, int> cslistEnum = new CircularSortedList<LongEnum, int>(cslistEnumShifted);
            SortedList<LongEnum, int> slistEnum = new SortedList<LongEnum, int>(cslistEnum);
            SortedDictionary<LongEnum, int> sdictEnum = new SortedDictionary<LongEnum, int>(cslistEnum);

            new PerformanceTest<bool> { TestName = "Search Test", Iterations = 1_000_000 }
                .AddCase(() => slist.ContainsKey(capacity), "SortedList<int,int>.ContainsKey")
                .AddCase(() => sdict.ContainsKey(capacity), "SortedDictionary<int,int>.ContainsKey")
                .AddCase(() => cslist.ContainsKey(capacity), "CircularSortedList<int,int>.ContainsKey (0-aligned)")
                .AddCase(() => cslistShifted.ContainsKey(capacity), "CircularSortedList<int,int>.ContainsKey (shifted)")
                .AddCase(() => slistEnum.ContainsKey((LongEnum)capacity), "SortedList<LongEnum,int>.ContainsKey")
                .AddCase(() => sdictEnum.ContainsKey((LongEnum)capacity), "SortedDictionary<LongEnum,int>.ContainsKey")
                .AddCase(() => cslistEnum.ContainsKey((LongEnum)capacity), "CircularSortedList<LongEnum,int>.ContainsKey (0-aligned)")
                .AddCase(() => cslistEnumShifted.ContainsKey((LongEnum)capacity), "CircularSortedList<LongEnum,int>.ContainsKey (shifted)")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion

        #endregion
    }
}
