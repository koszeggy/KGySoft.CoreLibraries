using System;
using System.Collections.Generic;
using System.Diagnostics;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest.Tests.Collections
{
    [TestClass]
    public class CircularSortedListPerformanceTest
    {
        [TestMethod]
        public void PopulateTest()
        {
            const int capacity = 1000;
            const int iterations = 1000;

            CircularSortedList<int, string> cslist = new CircularSortedList<int, string>(capacity);
            SortedList<int, string> slist = new SortedList<int, string>(capacity);
            SortedDictionary<int, string> sdict = new SortedDictionary<int, string>();

            Console.WriteLine("==========Populate (iterations: {0:N0}, elements: {1:N0})===========", iterations, capacity);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                slist.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    slist.Add(j, null);
                }
            }
            watch.Stop();
            Console.WriteLine("Populating SortedList from sorted data: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                sdict.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    sdict.Add(j, null);
                }
            }
            watch.Stop();
            Console.WriteLine("Populating SordedDictionary from sorted data: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslist.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    cslist.Add(j, null);
                }
            }
            watch.Stop();
            Console.WriteLine("Populating CircularSortedList from sorted data: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();
         
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                slist.Clear();
                for (int j = capacity - 1; j >= 0; j--)
                {
                    slist.Add(j, null);                    
                }
            }
            watch.Stop();
            Console.WriteLine("Populating SortedList from reverse sorted data: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                sdict.Clear();
                for (int j = capacity - 1; j >= 0; j--)
                {
                    sdict.Add(j, null);
                }
            }
            watch.Stop();
            Console.WriteLine("Populating SordedDictionary from reverse sorted data: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslist.Clear();
                for (int j = capacity - 1; j >= 0; j--)
                {
                    cslist.Add(j, null);
                }
            }
            watch.Stop();
            Console.WriteLine("Populating CircularSortedList from reverse sorted data: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            Random rnd = new Random(0);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                slist.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    slist[rnd.Next(Int32.MaxValue)] = null;
                }
            }
            watch.Stop();
            Console.WriteLine("Populating SortedList from random data: {0} ms", watch.ElapsedMilliseconds);

            rnd = new Random(0);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                sdict.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    sdict[rnd.Next(Int32.MaxValue)] = null;
                }
            }
            watch.Stop();
            Console.WriteLine("Populating SordedDictionary from random data: {0} ms", watch.ElapsedMilliseconds);

            rnd = new Random(0);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslist.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    cslist[rnd.Next(Int32.MaxValue)] = null;
                }
            }
            watch.Stop();
            Console.WriteLine("Populating CircularSortedList from random data: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            // TODO: enum key
        }

        [TestMethod]
        public void EnumeratingTest()
        {
            const int capacity = 1000;
            const int iterations = 10000;

            CircularSortedList<int, string> cslist = PrepareList<int, string>(capacity, 0, capacity);
            CircularSortedList<int, string> cslistwrap = PrepareList<int, string>(capacity, capacity >> 1, capacity);
            SortedList<int, string> slist = new SortedList<int, string>(cslist);
            SortedDictionary<int, string> sdict = new SortedDictionary<int, string>(cslist);

            Console.WriteLine("==========Enumerating (iterations: {0:N0}, elements: {1:N0})===========", iterations, capacity);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var item in slist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating SortedList: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var item in sdict)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating SortedDictionary: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var item in cslist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularCircularList (non wrapped): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var item in cslistwrap)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularSortedList (wrapped): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            IEnumerable<KeyValuePair<int, string>> ilist = slist;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var item in ilist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating SortedList as IEnumerable: {0} ms", watch.ElapsedMilliseconds);

            ilist = sdict;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var item in ilist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating SortedDictionary as IEnumerable: {0} ms", watch.ElapsedMilliseconds);

            ilist = cslist;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var item in ilist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularSortedList as IEnumerable (non wrapped): {0} ms", watch.ElapsedMilliseconds);

            ilist = cslistwrap;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (var item in ilist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularSortedList as IEnumerable (wrapped): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            int x;
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < slist.Count; j++)
                {
                    x = slist.Keys[j];
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating SortedList.Keys via indexer: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < cslist.Count; j++)
                {
                    x = cslist.Keys[j];
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularSortedList.Keys via indexer (non-wrapped): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < cslist.Count; j++)
                {
                    x = cslistwrap.Keys[j];
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularSortedList.Keys via indexer (wrapped): {0} ms", watch.ElapsedMilliseconds);
            // TODO: enum key
        }

        private enum TestIntEnum { }
        private enum TestUIntEnum : uint { }
        [TestMethod]
        public void SearchTest()
        {
            const int capacity = 10000;
            const int iterations = 100000;

            CircularSortedList<int, int> cslist = PrepareList<int, int>(capacity, 0, capacity);
            CircularSortedList<int, int> cslistw = PrepareList<int, int>(capacity, capacity >> 1, capacity);
            SortedList<int, int> slist = new SortedList<int, int>(cslist);
            SortedDictionary<int, int> sdict = new SortedDictionary<int, int>(cslist);
            Console.WriteLine("==========Search (iterations: {0:N0}, elements: {1:N0})===========", iterations, capacity);

            // warm-up (creating comparer, initializing first-time things)
            slist.ContainsKey(capacity);
            sdict.ContainsKey(capacity);
            cslist.ContainsKey(capacity);
            cslistw.ContainsKey(capacity);

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                slist.ContainsKey(capacity);
            }
            watch.Stop();
            Console.WriteLine("SortedList<int,int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                sdict.ContainsKey(capacity);
            }
            watch.Stop();
            Console.WriteLine("SortedDictionary<int,int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslist.ContainsKey(capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularSortedList<int,int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslistw.ContainsKey(capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularSortedList<int,int>.ContainsKey (wrapped): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            CircularSortedList<TestIntEnum, int> cslistEnum = PrepareList<TestIntEnum, int>(capacity, 0, capacity);
            CircularSortedList<TestIntEnum, int> cslistwEnum = PrepareList<TestIntEnum, int>(capacity, capacity >> 1, capacity);
            SortedList<TestIntEnum, int> slistEnum = new SortedList<TestIntEnum, int>(cslistEnum);
            SortedDictionary<TestIntEnum, int> sdictEnum = new SortedDictionary<TestIntEnum, int>(cslistEnum);

            // warm-up (creating comparer, initializing first-time things)
            slistEnum.ContainsKey((TestIntEnum)capacity);
            sdictEnum.ContainsKey((TestIntEnum)capacity);
            cslistEnum.ContainsKey((TestIntEnum)capacity);
            cslistwEnum.ContainsKey((TestIntEnum)capacity);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                slistEnum.ContainsKey((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("SortedList<TestIntEnum, int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                sdictEnum.ContainsKey((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("SortedDictionary<TestIntEnum, int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslistEnum.ContainsKey((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularSortedList<TestIntEnum, int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslistwEnum.ContainsKey((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularSortedList<TestIntEnum, int>.ContainsKey (wrapped): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            CircularSortedList<TestUIntEnum, int> cslistUIntEnum = PrepareList<TestUIntEnum, int>(capacity, 0, capacity);
            CircularSortedList<TestUIntEnum, int> cslistwUIntEnum = PrepareList<TestUIntEnum, int>(capacity, capacity >> 1, capacity);
            SortedList<TestUIntEnum, int> slistUIntEnum = new SortedList<TestUIntEnum, int>(cslistUIntEnum);
            SortedDictionary<TestUIntEnum, int> sdictUIntEnum = new SortedDictionary<TestUIntEnum, int>(cslistUIntEnum);

            // warm-up (creating comparer, initializing first-time things)
            slistUIntEnum.ContainsKey((TestUIntEnum)capacity);
            sdictUIntEnum.ContainsKey((TestUIntEnum)capacity);
            cslistUIntEnum.ContainsKey((TestUIntEnum)capacity);
            cslistwUIntEnum.ContainsKey((TestUIntEnum)capacity);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                slistUIntEnum.ContainsKey((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("SortedList<TestUIntEnum, int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                sdictUIntEnum.ContainsKey((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("SortedDictionary<TestUIntEnum, int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslistUIntEnum.ContainsKey((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularSortedList<TestUIntEnum, int>.ContainsKey: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                cslistwUIntEnum.ContainsKey((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularSortedList<TestUIntEnum, int>.ContainsKey (wrapped): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();
        }

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

            Type typeKey = typeof(TKey);
            if (typeKey.IsNullable())
                typeKey = Nullable.GetUnderlyingType(typeKey);
            Type typeValue = typeof(TValue);
            if (typeValue.IsNullable())
                typeValue = Nullable.GetUnderlyingType(typeValue);
            bool keyEnum = typeKey.IsEnum;
            bool valueEnum = typeValue.IsEnum;

            for (int i = 0; i < count; i++)
            {
                TKey key = keyEnum ? (TKey)Enum.ToObject(typeKey, i) : (TKey)Convert.ChangeType(i, typeKey);
                TValue value = valueEnum ? (TValue)Enum.ToObject(typeValue, i) : (TValue)Convert.ChangeType(i, typeValue);
                result.Add(key, value);
            }

            return result;
        }

    }
}
