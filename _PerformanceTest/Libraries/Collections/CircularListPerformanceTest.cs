using System;
using System.Collections.Generic;
using System.Diagnostics;
using KGySoft.Libraries;
using KGySoft.Libraries.Collections;
using KGySoft.Libraries.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest.Libraries.Collections
{
    [TestClass]
    public class CircularListPerformanceTest
    {
        const int capacity = 1000;

        [TestMethod]
        public void PopulateTest()
        {
            List<int> list = new List<int>();
            CircularList<int> clist = new CircularList<int>();
            LinkedList<int> llist = new LinkedList<int>();
            const int iterations = 10000;
            Console.WriteLine("==========Populate (iterations: {0:N0}, elements: {1:N0})===========", iterations, capacity);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                list.Clear();
                list.TrimExcess();
                for (int j = 0; j < capacity; j++)
                {
                    list.Add(j);
                }
            }
            watch.Stop();
            Console.WriteLine("Add to List end: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                llist.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    llist.AddLast(j);
                }
            }
            watch.Stop();
            Console.WriteLine("Add to LinkedList end: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clist.Clear();
                clist.TrimExcess();
                for (int j = 0; j < capacity; j++)
                {
                    clist.Add(j);
                }
            }
            watch.Stop();
            Console.WriteLine("Add to CircularList end: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                list.Clear();
                list.TrimExcess();
                for (int j = 0; j < capacity; j++)
                {
                    list.Insert(0, j);
                }
            }
            watch.Stop();
            Console.WriteLine("Add to List head: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                llist.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    llist.AddFirst(j);
                }
            }
            watch.Stop();
            Console.WriteLine("Add to LinkedList head: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clist.Clear();
                clist.TrimExcess();
                for (int j = 0; j < capacity; j++)
                {
                    clist.Insert(0, j);
                }
            }
            watch.Stop();
            Console.WriteLine("Add to CircularList head: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            Random rnd = new Random(0);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                list.Clear();
                list.TrimExcess();
                for (int j = 0; j < capacity; j++)
                {
                    list.Insert(rnd.Next(j), j);
                }
            }
            watch.Stop();
            Console.WriteLine("Insert to List random: {0} ms", watch.ElapsedMilliseconds);

            rnd = new Random(0);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                llist.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    int r = rnd.Next(j);
                    LinkedListNode<int> node = llist.First;
                    if (node != null)
                    {
                        for (int k = 0; k < r; k++)
                        {
                            node = node.Next;
                        }
                        llist.AddBefore(node, j);
                    }
                    else
                    {
                        llist.AddLast(j);
                    }
                }
            }
            watch.Stop();
            Console.WriteLine("Insert to LinkedList random: {0} ms", watch.ElapsedMilliseconds);

            rnd = new Random(0);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clist.Clear();
                clist.TrimExcess();
                for (int j = 0; j < capacity; j++)
                {
                    clist.Insert(rnd.Next(j), j);
                }
            }
            watch.Stop();
            Console.WriteLine("Insert to CircularList random: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            //insert range
        }

        [TestMethod]
        public void EnumeratingTest()
        {
            CircularList<int> clist = PrepareList<int>(capacity, 0, capacity);
            CircularList<int> clistwrap = PrepareList<int>(capacity, capacity >> 1, capacity);
            List<int> list = new List<int>(clist);
            LinkedList<int> llist = new LinkedList<int>(clist);
            const int iterations = 10000;
            Console.WriteLine("==========Enumerating (iterations: {0:N0}, elements: {1:N0})===========", iterations, capacity);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (int num in list)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating List: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (int num in llist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating LinkedList: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (int num in clist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularList (non wrapped): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (int num in clistwrap)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularList (wrapped): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            IEnumerable<int> ilist = list;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (int num in ilist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating List as IList: {0} ms", watch.ElapsedMilliseconds);

            ilist = llist;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (int num in ilist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating LinkedList as IList: {0} ms", watch.ElapsedMilliseconds);

            ilist = clist;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (int num in ilist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularList as IList (non wrapped): {0} ms", watch.ElapsedMilliseconds);

            ilist = clistwrap;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                foreach (int num in ilist)
                {
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularList as IList (wrapped): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            int x;
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    x = list[j];
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating List via indexer: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < clist.Count; j++)
                {
                    x = clist[j];
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularList via indexer (non-wrapped): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < clist.Count; j++)
                {
                    x = clistwrap[j];
                }
            }
            watch.Stop();
            Console.WriteLine("Enumerating CircularList via indexer (wrapped): {0} ms", watch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void RemoveTest()
        {
            CircularList<int> clist = PrepareList<int>(capacity, 0, capacity);
            List<int> list = new List<int>(clist);
            LinkedList<int> llist = new LinkedList<int>(clist);
            const int iterations = 100000;
            const int lastIndex = capacity - 1;
            Console.WriteLine("==========Remove (iterations: {0:N0}, elements: {1:N0})===========", iterations, capacity);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                list.RemoveAt(lastIndex);
                list.Add(lastIndex);
            }
            watch.Stop();
            Console.WriteLine("Remove and re-insert last List item: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                llist.RemoveLast();
                llist.AddLast(lastIndex);
            }
            watch.Stop();
            Console.WriteLine("Remove and re-insert last LinkedList item: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clist.RemoveAt(lastIndex);
                clist.Add(lastIndex);
            }
            watch.Stop();
            Console.WriteLine("Remove and re-insert last CircularList item: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                int e = list[0];
                list.RemoveAt(0);
                list.Add(e);
            }
            watch.Stop();
            Console.WriteLine("Remove first element and add to end - List: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                int e = llist.First.Value;
                llist.RemoveFirst();
                llist.AddLast(e);
            }
            watch.Stop();
            Console.WriteLine("Remove first element and add to end - LinkedList: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                int e = clist[0];
                clist.RemoveAt(0);
                clist.Add(e);
            }
            watch.Stop();
            Console.WriteLine("Remove first element and add to end - CircularList: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            Random rnd = new Random(0);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                int e = rnd.Next(capacity);
                list.Remove(e);
                list.Add(e);
            }
            watch.Stop();
            Console.WriteLine("Remove random element and add to end - List: {0} ms", watch.ElapsedMilliseconds);

            rnd = new Random(0);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                int e = rnd.Next(capacity);
                llist.Remove(e);
                llist.AddLast(e);
            }
            watch.Stop();
            Console.WriteLine("Remove random element and add to end - LinkedList: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                int e = rnd.Next(capacity);
                clist.Remove(e);
                clist.Add(e);
            }
            watch.Stop();
            Console.WriteLine("Remove random element and add to end - CircularList: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            // remove range first/last/random
            // remove element first/last/random int/enum
        }

        private enum TestIntEnum { }
        private enum TestUIntEnum: uint { }
        [TestMethod]
        public void SearchTest()
        {
            CircularList<int> clist = PrepareList<int>(capacity, 0, capacity);
            List<int> list = new List<int>(clist);
            LinkedList<int> llist = new LinkedList<int>(clist);
            const int iterations = 10000;
            Console.WriteLine("==========Search (iterations: {0:N0}, elements: {1:N0})===========", iterations, capacity);

            // warm-up (creating comparer, initializing first-time things)
            list.Contains(capacity);
            llist.Contains(capacity);
            clist.Contains(capacity);
            // warm-up (creating comparer, initializing first-time things)
            list.BinarySearch(capacity);
            clist.BinarySearch(capacity);

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                list.Contains(capacity);
            }
            watch.Stop();
            Console.WriteLine("List<int>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                list.IndexOf(capacity);
            }
            watch.Stop();
            Console.WriteLine("List<int>.IndexOf: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                llist.Contains(capacity);
            }
            watch.Stop();
            Console.WriteLine("LinkedList<int>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clist.Contains(capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<int>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clist.IndexOf(capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<int>.IndexOf: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                list.BinarySearch(capacity);
            }
            watch.Stop();
            Console.WriteLine("List<int>.BinarySearch: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clist.BinarySearch(capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<int>.BinarySearch, non-wrapped: {0} ms", watch.ElapsedMilliseconds);

            clist = PrepareList<int>(capacity, capacity >> 1, capacity);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clist.BinarySearch(capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<int>.BinarySearch, wrapped: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            CircularList<TestIntEnum> clistEnum = PrepareList<TestIntEnum>(capacity, 0, capacity);
            List<TestIntEnum> listEnum = new List<TestIntEnum>(clistEnum);
            LinkedList<TestIntEnum> llistEnum = new LinkedList<TestIntEnum>(clistEnum);

            // warm-up (creating comparer, initializing first-time things)
            listEnum.Contains((TestIntEnum)capacity);
            llistEnum.Contains((TestIntEnum)capacity);
            clistEnum.Contains((TestIntEnum)capacity);
            listEnum.BinarySearch((TestIntEnum)capacity);
            clistEnum.BinarySearch((TestIntEnum)capacity);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                listEnum.Contains((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("List<TestIntEnum>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                listEnum.IndexOf((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("List<TestIntEnum>.IndexOf: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                llistEnum.Contains((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("LinkedList<TestIntEnum>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clistEnum.Contains((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<TestIntEnum>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clistEnum.IndexOf((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<TestIntEnum>.IndexOf: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                listEnum.BinarySearch((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("List<TestIntEnum>.BinarySearch: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clistEnum.BinarySearch((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<TestIntEnum>.BinarySearch, non-wrapped: {0} ms", watch.ElapsedMilliseconds);

            clistEnum = PrepareList<TestIntEnum>(capacity, capacity >> 1, capacity);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clistEnum.BinarySearch((TestIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<TestIntEnum>.BinarySearch, wrapped: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            CircularList<TestUIntEnum> clistUIntEnum = PrepareList<TestUIntEnum>(capacity, 0, capacity);
            List<TestUIntEnum> listUIntEnum = new List<TestUIntEnum>(clistUIntEnum);
            LinkedList<TestUIntEnum> llistUIntEnum = new LinkedList<TestUIntEnum>(clistUIntEnum);

            // warm-up (creating comparer, initializing first-time things)
            listUIntEnum.Contains((TestUIntEnum)capacity);
            llistUIntEnum.Contains((TestUIntEnum)capacity);
            clistUIntEnum.Contains((TestUIntEnum)capacity);
            listUIntEnum.BinarySearch((TestUIntEnum)capacity);
            clistUIntEnum.BinarySearch((TestUIntEnum)capacity);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                listUIntEnum.Contains((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("List<TestUIntEnum>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                listUIntEnum.IndexOf((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("List<TestUIntEnum>.IndexOf: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                llistUIntEnum.Contains((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("LinkedList<TestUIntEnum>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clistUIntEnum.Contains((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<TestUIntEnum>.Contains: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clistUIntEnum.IndexOf((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<TestUIntEnum>.IndexOf: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                listUIntEnum.BinarySearch((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("List<TestUIntEnum>.BinarySearch: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clistUIntEnum.BinarySearch((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<TestUIntEnum>.BinarySearch, non-wrapped: {0} ms", watch.ElapsedMilliseconds);

            clistUIntEnum = PrepareList<TestUIntEnum>(capacity, capacity >> 1, capacity);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                clistUIntEnum.BinarySearch((TestUIntEnum)capacity);
            }
            watch.Stop();
            Console.WriteLine("CircularList<TestUIntEnum>.BinarySearch, wrapped: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();
        }

        /// <summary>
        /// Creates a list with given conditions
        /// </summary>
        private static CircularList<T> PrepareList<T>(int capacity, int startIndex, int count)
        {
            CircularList<T> result = new CircularList<T>(capacity);
            Reflector.SetInstanceFieldByName(result, "startIndex", startIndex);
            Type type = typeof(T);
            if (type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            if (type.IsEnum)
            {
                for (int i = 0; i < count; i++)
                {
                    result.Add((T)Enum.ToObject(type, i));
                }
                return result;
            }
            for (int i = 0; i < count; i++)
            {
                result.Add((T)Convert.ChangeType(i, type));
            }
            return result;
        }

    }
}
