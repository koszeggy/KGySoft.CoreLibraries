#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CircularListPerformanceTest.cs
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

using KGySoft.Collections;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    [TestFixture]
    public class CircularListPerformanceTest
    {
        #region Enumerations

        private enum LongEnum : long { }

        #endregion

        #region Methods

        #region Static Methods

        /// <summary>
        /// Creates a list with given conditions
        /// </summary>
        private static CircularList<T> PrepareList<T>(int capacity, int startIndex, int count)
        {
            CircularList<T> result = new CircularList<T>(capacity);
            Reflector.SetField(result, "startIndex", startIndex);
            Type type = typeof(T);
            if (type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            for (int i = 0; i < count; i++)
                result.Add(i.Convert<T>());
            return result;
        }

        #endregion

        #region Instance Methods

        [Test]
        public void PopulateTest()
        {
            const int count = 1000;
            new RandomizedPerformanceTest { TestName = "Populate Lists Test", Iterations = 10000 }
                .AddCase(rnd =>
                {
                    var list = new List<int>(count);
                    for (int i = 0; i < count; i++)
                        list.Add(i);
                }, "Add to List end")
                .AddCase(rnd =>
                {
                    var llist = new LinkedList<int>();
                    for (int i = 0; i < count; i++)
                        llist.AddLast(i);
                }, "Add to LinkedList end")
                .AddCase(rnd =>
                {
                    var clist = new CircularList<int>(count);
                    for (int i = 0; i < count; i++)
                        clist.Add(i);
                }, "Add to CircularList end")
                .AddCase(rnd =>
                {
                    var list = new List<int>(count);
                    for (int i = 0; i < count; i++)
                        list.Insert(0, i);
                }, "Add to List head")
                .AddCase(rnd =>
                {
                    var llist = new LinkedList<int>();
                    for (int i = 0; i < count; i++)
                        llist.AddFirst(i);
                }, "Add to LinkedList head")
                .AddCase(rnd =>
                {
                    var clist = new CircularList<int>(count);
                    for (int i = 0; i < count; i++)
                        clist.Insert(0, i);
                }, "Add to CircularList head")
                .AddCase(rnd =>
                {
                    var list = new List<int>(count);
                    for (int i = 0; i < count; i++)
                        list.Insert(rnd.Next(i), i);
                }, "Insert to List randomly")
                .AddCase(rnd =>
                {
                    var llist = new LinkedList<int>();
                    for (int i = 0; i < count; i++)
                    {
                        int r = rnd.Next(i);
                        LinkedListNode<int> node = llist.First;
                        if (node != null)
                        {
                            for (int j = 0; j < r; j++)
                                node = node.Next;
                            llist.AddBefore(node, i);
                        }
                        else
                            llist.AddLast(i);
                    }
                }, "Insert to LinkedList randomly")
                .AddCase(rnd =>
                {
                    var clist = new CircularList<int>(count);
                    for (int i = 0; i < count; i++)
                        clist.Insert(0, i);
                }, "Insert to CircularList randomly")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void EnumeratingTest()
        {
            const int capacity = 1000;
            CircularList<int> clist = PrepareList<int>(capacity, 0, capacity);
            CircularList<int> clistShifted = PrepareList<int>(capacity, capacity >> 1, capacity);
            List<int> list = new List<int>(clist);
            LinkedList<int> llist = new LinkedList<int>(clist);

            new PerformanceTest { TestName = "Enumerating Lists Test", Iterations = 10000 }
                .AddCase(() =>
                {
                    foreach (int i in list) { }
                }, "Enumerating List by foreach")
                .AddCase(() =>
                {
                    foreach (int i in llist) { }
                }, "Enumerating LinkedList by foreach")
                .AddCase(() =>
                {
                    foreach (int i in clist) { }
                }, "Enumerating CircularList by foreach (0-aligned)")
                .AddCase(() =>
                {
                    foreach (int i in clistShifted) { }
                }, "Enumerating CircularList by foreach (shifted)")
                .AddCase(() =>
                {
                    IEnumerable<int> ilist = list;
                    foreach (int i in ilist) { }
                }, "Enumerating List as IList by foreach (eg. LINQ)")
                .AddCase(() =>
                {
                    IEnumerable<int> ilist = llist;
                    foreach (int i in ilist) { }
                }, "Enumerating LinkedList as IList by foreach")
                .AddCase(() =>
                {
                    IEnumerable<int> ilist = clist;
                    foreach (int i in ilist) { }
                }, "Enumerating CircularList as IList by foreach (0-aligned)")
                .AddCase(() =>
                {
                    IEnumerable<int> ilist = clistShifted;
                    foreach (int i in ilist) { }
                }, "Enumerating CircularList as IList by foreach (shifted)")
                .AddCase(() =>
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        int x = list[i];
                    }
                }, "Enumerating List by index")
                .AddCase(() =>
                {
                    for (int i = 0; i < clist.Count; i++)
                    {
                        int x = clist[i];
                    }
                }, "Enumerating CircularList by index (0-aligned)")
                .AddCase(() =>
                {
                    for (int i = 0; i < clistShifted.Count; i++)
                    {
                        int x = clistShifted[i];
                    }
                }, "Enumerating CircularList by index (shifted)")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void RemoveTest()
        {
            const int count = 1000;
            List<int> list = Enumerable.Range(0, count).ToList();
            CircularList<int> clist = list.ToCircularList();
            LinkedList<int> llist = new LinkedList<int>(list);
            const int lastIndex = count - 1;

            new RandomizedPerformanceTest { TestName = "Remove Item Test", Iterations = 100000 }
                .AddCase(rnd =>
                {
                    rnd.Next(count); // result not used here but makes cases comparable
                    list.RemoveAt(lastIndex);
                    list.Add(lastIndex);
                }, "Remove and re-insert last item - List")
                .AddCase(rnd =>
                {
                    rnd.Next(count); // result not used here but makes cases comparable
                    llist.RemoveLast();
                    llist.AddLast(lastIndex);
                }, "Remove and re-insert last item - LinkedList")
                .AddCase(rnd =>
                {
                    rnd.Next(count); // result not used here but makes cases comparable
                    clist.RemoveAt(lastIndex);
                    clist.Add(lastIndex);
                }, "Remove and re-insert last item - CircularList")
                .AddCase(rnd =>
                {
                    rnd.Next(count); // result not used here but makes cases comparable
                    int e = list[0];
                    list.RemoveAt(0);
                    list.Insert(0, e);
                }, "Remove and re-insert first item - List")
                .AddCase(rnd =>
                {
                    rnd.Next(count); // result not used here but makes cases comparable
                    int e = llist.First.Value;
                    llist.RemoveFirst();
                    llist.AddFirst(e);
                }, "Remove and re-insert first item - LinkedList")
                .AddCase(rnd =>
                {
                    rnd.Next(count); // result not used here but makes cases comparable
                    int e = clist[0];
                    clist.RemoveAt(0);
                    clist.AddFirst(e);
                }, "Remove and re-insert first item - CircularList")
                .AddCase(rnd =>
                {
                    int i = rnd.Next(count);
                    int e = list[i];
                    list.RemoveAt(i);
                    list.Insert(i, e);
                }, "Remove and re-insert random element - List")
                .AddCase(rnd =>
                {
                    int i = rnd.Next(count);
                    LinkedListNode<int> node = llist.First;
                    for (int j = 0; j < i; j++)
                        node = node.Next;

                    int e = node.Value;
                    LinkedListNode<int> prev = node.Previous;
                    llist.Remove(node);
                    if (prev == null)
                        llist.AddFirst(e);
                    else
                        llist.AddAfter(prev, e);
                }, "Remove and re-insert random element - LinkedList")
                .AddCase(rnd =>
                {
                    int i = rnd.Next(count);
                    int e = clist[i];
                    clist.RemoveAt(i);
                    clist.Insert(i, e);
                }, "Remove and re-insert random element - CircularList")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void SearchTest()
        {
            const int count = 10000;
            List<int> list = Enumerable.Range(0, count).ToList();
            LinkedList<int> llist = new LinkedList<int>(list);
            CircularList<int> clist = list.ToCircularList();

            List<LongEnum> listUIntEnum = list.Convert<List<LongEnum>>();
            LinkedList<LongEnum> llistUIntEnum = new LinkedList<LongEnum>(listUIntEnum);
            CircularList<LongEnum> clistUIntEnum = listUIntEnum.ToCircularList();

            new PerformanceTest { TestName = "Search Test", Iterations = 10000 }
                .AddCase(() => list.Contains(count), "List<int>.Contains")
                .AddCase(() => llist.Contains(count), "LinkedList<int>.Contains")
                .AddCase(() => clist.Contains(count), "CircularList<int>.Contains")
                .AddCase(() => list.BinarySearch(count), "List<int>.BinarySearch")
                .AddCase(() => clist.BinarySearch(count), "CircularList<int>.BinarySearch")
                .AddCase(() => listUIntEnum.Contains((LongEnum)count), "List<LongEnum>.Contains")
                .AddCase(() => llistUIntEnum.Contains((LongEnum)count), "LinkedList<LongEnum>.Contains")
                .AddCase(() => clistUIntEnum.Contains((LongEnum)count), "CircularList<LongEnum>.Contains")
                .AddCase(() => listUIntEnum.BinarySearch((LongEnum)count), "List<LongEnum>.BinarySearch")
                .AddCase(() => clistUIntEnum.BinarySearch((LongEnum)count), "CircularList<LongEnum>.BinarySearch")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion

        #endregion
    }
}
