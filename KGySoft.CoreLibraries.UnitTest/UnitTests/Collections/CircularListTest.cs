#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CircularListTest.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using KGySoft.Collections;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    /// <summary>
    /// CircularList Test
    /// </summary>
    [TestFixture]
    public class CircularListTest
    {
        #region Constants

        private const string testString = "dummy";

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
            {
                result.Add((T)Convert.ChangeType(i, type));
            }
            return result;
        }

        #endregion

        #region Instance Methods

        [Test]
        public void AddLastAndFirst()
        {
            var list = new CircularList<int>();
            list.Add(1); // Resize to 4; [0] = 1
            Assert.AreEqual(list[list.Count - 1], 1);
            list.Add(2); // [1] = 2
            Assert.AreEqual(list[list.Count - 1], 2);
            list.Insert(0, 0); // [3] = 0
            Assert.AreEqual(list[0], 0);
            list.Insert(0, -1); // [2] = -1
            Assert.AreEqual(list[0], -1);
            list.Insert(0, -2); // Resize to 8; [7] = -2
            Assert.AreEqual(list[0], -2);
            list.Add(3); // [4] = 3
            Assert.AreEqual(list[list.Count - 1], 3);

            for (int i = 0; i < list.Count; i++)
                Assert.AreEqual(list[i], i - 2);
        }

        [Test]
        public void Indexer()
        {
            CircularList<int> list = PrepareList<int>(4, 3, 2);
            list[0] = 1;
            list[1] = 2;
            Assert.AreEqual(list[0], 1);
            Assert.AreEqual(list[1], 2);
        }

        [Test]
        public void InsertSimple()
        {
            CircularList<int> list = PrepareList<int>(4, 3, 2);

            list.Insert(0, Int32.MinValue); // add first
            Assert.AreEqual(Int32.MinValue, list[0]);
            list.Insert(list.Count, Int32.MaxValue); // add last
            Assert.AreEqual(Int32.MaxValue, list[list.Count - 1]);

            list.Insert(0, -1); // add first with resize
            Assert.AreEqual(-1, list[0]);
            list.Insert(1, 1); // inserting into the middle "smoke test"
            Assert.AreEqual(1, list[1]);
        }

        [Test]
        public void InsertShiftUp()
        {
            // shift up, carry -, startIndex 0
            CircularList<string> list = PrepareList<string>(8, 0, 4);
            List<string> refList = new List<string>(list);
            list.Insert(3, testString);
            Assert.AreEqual(testString, list[3]);
            refList.Insert(3, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry -, startIndex 1, single shift inside the array
            list = PrepareList<string>(8, 1, 4);
            refList = new List<string>(list);
            list.Insert(3, testString);
            Assert.AreEqual(testString, list[3]);
            refList.Insert(3, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry -, startIndex 1, single shift to the end
            list = PrepareList<string>(8, 1, 6);
            refList = new List<string>(list);
            list.Insert(4, testString);
            Assert.AreEqual(testString, list[4]);
            refList.Insert(4, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry 0, startIndex 2, toMove 1 (single element from end to head)
            list = PrepareList<string>(8, 2, 6);
            refList = new List<string>(list);
            list.Insert(5, testString);
            Assert.AreEqual(testString, list[5]);
            refList.Insert(5, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry 0, startIndex 2, toMove 2
            list = PrepareList<string>(8, 2, 7);
            refList = new List<string>(list);
            list.Insert(5, testString);
            Assert.AreEqual(testString, list[5]);
            refList.Insert(5, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry 1, startIndex 3, toMove 2, insert to the end
            list = PrepareList<string>(8, 3, 6);
            refList = new List<string>(list);
            list.Insert(4, testString);
            Assert.AreEqual(testString, list[4]);
            refList.Insert(4, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry 1, startIndex 3, toMove 1, insert to the beginning
            list = PrepareList<string>(8, 3, 6);
            refList = new List<string>(list);
            list.Insert(5, testString);
            Assert.AreEqual(testString, list[5]);
            refList.Insert(5, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry 3, startIndex 7, shifting elements only at the carried part
            list = PrepareList<string>(8, 7, 4);
            refList = new List<string>(list);
            list.Insert(2, testString);
            Assert.AreEqual(testString, list[2]);
            refList.Insert(2, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry 1, startIndex 7, shifting only carried elements
            list = PrepareList<string>(8, 7, 5);
            refList = new List<string>(list);
            list.Insert(2, testString);
            Assert.AreEqual(testString, list[2]);
            refList.Insert(2, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry 1, startIndex 6, shifting only carried elements
            list = PrepareList<string>(8, 6, 5);
            refList = new List<string>(list);
            list.Insert(2, testString);
            Assert.AreEqual(testString, list[2]);
            refList.Insert(2, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift up, carry 2, startIndex 4, shifting all kind of elements
            list = PrepareList<string>(12, 4, 10);
            refList = new List<string>(list);
            list.Insert(6, testString);
            Assert.AreEqual(testString, list[6]);
            refList.Insert(6, testString);
            Assert.IsTrue(refList.SequenceEqual(list));
        }

        [Test]
        public void InsertShiftDown()
        {
            // shift down, carry 0, startIndex 4
            CircularList<string> list = PrepareList<string>(8, 4, 4);
            List<string> refList = new List<string>(list);
            list.Insert(1, testString);
            Assert.AreEqual(testString, list[1]);
            refList.Insert(1, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift down, carry 0, startIndex 2, single shift inside the array
            list = PrepareList<string>(8, 2, 4);
            refList = new List<string>(list);
            list.Insert(1, testString);
            Assert.AreEqual(testString, list[1]);
            refList.Insert(1, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift down, carry 0, startIndex 1, single shift to the beginning
            list = PrepareList<string>(8, 1, 4);
            refList = new List<string>(list);
            list.Insert(1, testString);
            Assert.AreEqual(testString, list[1]);
            refList.Insert(1, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift down, carry 0, startIndex 0, putting one element to the end, inserting to the beginning
            list = PrepareList<string>(8, 0, 4);
            refList = new List<string>(list);
            list.Insert(1, testString);
            Assert.AreEqual(testString, list[1]);
            refList.Insert(1, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift down, carry 1, startIndex 7, shifting one element at the end, inserting to the end
            list = PrepareList<string>(8, 7, 4);
            refList = new List<string>(list);
            list.Insert(1, testString);
            Assert.AreEqual(testString, list[1]);
            refList.Insert(1, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift down, carry 2, startIndex 6, shifting one element at the end, inserting before the end
            list = PrepareList<string>(8, 6, 6);
            refList = new List<string>(list);
            list.Insert(1, testString);
            Assert.AreEqual(testString, list[1]);
            refList.Insert(1, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift down, carry 2, startIndex 6, shifting two elements at the end, inserting to the end
            list = PrepareList<string>(8, 6, 6);
            refList = new List<string>(list);
            list.Insert(2, testString);
            Assert.AreEqual(testString, list[2]);
            refList.Insert(2, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift down, carry 2, startIndex 10, shifting two elements at the end, moving first to last, inserting to the beginning
            list = PrepareList<string>(12, 10, 10);
            refList = new List<string>(list);
            list.Insert(3, testString);
            Assert.AreEqual(testString, list[3]);
            refList.Insert(3, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // shift down, carry 2, startIndex 10, shifting two elements at the end, moving first to last, moving one element at the beginning, inserting to position 1
            list = PrepareList<string>(12, 10, 10);
            refList = new List<string>(list);
            list.Insert(4, testString);
            Assert.AreEqual(testString, list[4]);
            refList.Insert(4, testString);
            Assert.IsTrue(refList.SequenceEqual(list));
        }

        [Test]
        public void InsertIncreaseCapacity()
        {
            // add last
            CircularList<string> list = PrepareList<string>(4, 0, 4);
            List<string> refList = new List<string>(list);
            list.Insert(4, testString);
            Assert.AreEqual(testString, list[4]);
            refList.Insert(4, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // add first
            list = PrepareList<string>(4, 0, 4);
            refList = new List<string>(list);
            list.Insert(0, testString);
            Assert.AreEqual(testString, list[0]);
            refList.Insert(0, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // no wrap
            list = PrepareList<string>(4, 0, 4);
            refList = new List<string>(list);
            list.Insert(2, testString);
            Assert.AreEqual(testString, list[2]);
            refList.Insert(2, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // wrap before index
            list = PrepareList<string>(4, 3, 4);
            refList = new List<string>(list);
            list.Insert(2, testString);
            Assert.AreEqual(testString, list[2]);
            refList.Insert(2, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // wrap at index
            list = PrepareList<string>(4, 2, 4);
            refList = new List<string>(list);
            list.Insert(2, testString);
            Assert.AreEqual(testString, list[2]);
            refList.Insert(2, testString);
            Assert.IsTrue(refList.SequenceEqual(list));

            // wrap after index
            list = PrepareList<string>(4, 1, 4);
            refList = new List<string>(list);
            list.Insert(2, testString);
            Assert.AreEqual(testString, list[2]);
            refList.Insert(2, testString);
            Assert.IsTrue(refList.SequenceEqual(list));
        }

        [Test]
        public void InsertRangeSimple()
        {
            CircularList<int> list = PrepareList<int>(10, 0, 2);
            List<int> refList = new List<int>(list);
            int[] toAdd = new int[] { 10, 11, 12, 13 };

            // adding without wrapping
            list.InsertRange(list.Count, toAdd);
            refList.InsertRange(refList.Count, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // adding without wrapping - fit to end
            list.InsertRange(list.Count, toAdd);
            refList.InsertRange(refList.Count, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // resizing, adding without wrapping
            list.InsertRange(list.Count, toAdd);
            refList.InsertRange(refList.Count, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // startIndex > 0, adding without wrapping
            list = PrepareList<int>(16, 3, 2);
            refList = new List<int>(list);
            list.InsertRange(list.Count, toAdd);
            refList.InsertRange(refList.Count, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));
            list.InsertRange(list.Count, toAdd);
            refList.InsertRange(refList.Count, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // adding with wrapping
            list.InsertRange(list.Count, toAdd);
            refList.InsertRange(refList.Count, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // adding without wrapping to already wrapped list
            list.InsertRange(list.Count, new int[] { 100, 101 });
            refList.InsertRange(refList.Count, new int[] { 100, 101 });
            Assert.IsTrue(refList.SequenceEqual(list));

            // resizing, adding without wrapping
            list.InsertRange(list.Count, toAdd);
            refList.InsertRange(refList.Count, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            list = PrepareList<int>(10, 8, 2);
            refList = new List<int>(list);

            // adding without wrapping
            list.InsertRange(0, toAdd);
            refList.InsertRange(0, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // adding without wrapping - fit to beginning
            list.InsertRange(0, toAdd);
            refList.InsertRange(0, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // resizing, adding with wrapping to the end
            list.InsertRange(0, toAdd);
            refList.InsertRange(0, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // startIndex > 0, adding without wrapping
            list = PrepareList<int>(16, 10, 2);
            refList = new List<int>(list);
            list.InsertRange(0, toAdd);
            refList.InsertRange(0, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));
            list.InsertRange(0, toAdd);
            refList.InsertRange(0, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // adding with wrapping
            list.InsertRange(0, toAdd);
            refList.InsertRange(0, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // adding without wrapping to already wrapped list
            list.InsertRange(0, new int[] { 100, 101 });
            refList.InsertRange(0, new int[] { 100, 101 });
            Assert.IsTrue(refList.SequenceEqual(list));

            // resizing, adding with wrapping to the end
            list.InsertRange(0, toAdd);
            refList.InsertRange(0, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));
        }

        [Test]
        public void InsertRangeShiftUp()
        {
            CircularList<string> list = PrepareList<string>(8, 1, 3);
            List<string> refList = new List<string>(list);
            string[] toAdd = new string[] { "alpha", "beta", "gamma" };

            // simple shifting up 1 element
            list.InsertRange(2, toAdd);
            refList.InsertRange(2, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // simple shifting up 2 elements, fit to end
            list = PrepareList<string>(8, 1, 4);
            refList = new List<string>(list);
            list.InsertRange(2, toAdd);
            refList.InsertRange(2, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // last moved to beginning, one to end
            list = PrepareList<string>(8, 2, 4);
            refList = new List<string>(list);
            list.InsertRange(2, toAdd);
            refList.InsertRange(2, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // two last from index 5. moved to beginning
            list = PrepareList<string>(8, 3, 4);
            refList = new List<string>(list);
            list.InsertRange(2, toAdd);
            refList.InsertRange(2, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // last at index 5. moved to beginning
            list = PrepareList<string>(8, 2, 4);
            refList = new List<string>(list);
            list.InsertRange(3, toAdd);
            refList.InsertRange(3, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // last at index 6. moved to index 1.
            list = PrepareList<string>(8, 3, 4);
            refList = new List<string>(list);
            list.InsertRange(3, toAdd);
            refList.InsertRange(3, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved up, last 3 moved to beginning, 1 moved up.
            list = PrepareList<string>(16, 6, 12);
            refList = new List<string>(list);
            list.InsertRange(6, toAdd);
            refList.InsertRange(6, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved up, last 3 moved to beginning
            list = PrepareList<string>(16, 6, 12);
            refList = new List<string>(list);
            list.InsertRange(7, toAdd);
            refList.InsertRange(7, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved up, last 2 moved to index 1
            list = PrepareList<string>(16, 6, 12);
            refList = new List<string>(list);
            list.InsertRange(8, toAdd);
            refList.InsertRange(8, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved up, last moved to index 2
            list = PrepareList<string>(16, 6, 12);
            refList = new List<string>(list);
            list.InsertRange(9, toAdd);
            refList.InsertRange(9, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved up as normal shifting
            list = PrepareList<string>(16, 6, 12);
            refList = new List<string>(list);
            list.InsertRange(10, toAdd);
            refList.InsertRange(10, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));
        }

        [Test]
        public void InsertRangeShiftDown()
        {
            CircularList<string> list = PrepareList<string>(8, 4, 4);
            List<string> refList = new List<string>(list);
            string[] toAdd = new string[] { "alpha", "beta", "gamma" };

            // simple shifting down 1 element
            list.InsertRange(1, toAdd);
            refList.InsertRange(1, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // simple shifting down 2 elements, fit to start
            list = PrepareList<string>(10, 3, 6);
            refList = new List<string>(list);
            list.InsertRange(2, toAdd);
            refList.InsertRange(2, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // first moved to end, one to beginning
            list = PrepareList<string>(10, 2, 6);
            refList = new List<string>(list);
            list.InsertRange(2, toAdd);
            refList.InsertRange(2, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // two first from index 1. moved to end
            list = PrepareList<string>(10, 1, 6);
            refList = new List<string>(list);
            list.InsertRange(2, toAdd);
            refList.InsertRange(2, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // first at index 2. moved to end
            list = PrepareList<string>(10, 2, 6);
            refList = new List<string>(list);
            list.InsertRange(1, toAdd);
            refList.InsertRange(1, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // first at index 1. moved to index 8.
            list = PrepareList<string>(10, 1, 6);
            refList = new List<string>(list);
            list.InsertRange(1, toAdd);
            refList.InsertRange(1, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved down, first 3 moved to end, 1 moved down.
            list = PrepareList<string>(18, 16, 14);
            refList = new List<string>(list);
            list.InsertRange(6, toAdd);
            refList.InsertRange(6, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved down, first 3 moved to end
            list = PrepareList<string>(16, 14, 12);
            refList = new List<string>(list);
            list.InsertRange(5, toAdd);
            refList.InsertRange(5, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved down, first 2 moved to index 13.
            list = PrepareList<string>(16, 14, 12);
            refList = new List<string>(list);
            list.InsertRange(4, toAdd);
            refList.InsertRange(4, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved down, first moved to index 13.
            list = PrepareList<string>(16, 14, 12);
            refList = new List<string>(list);
            list.InsertRange(3, toAdd);
            refList.InsertRange(3, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));

            // 2 wrapped moved down as normal shifting
            list = PrepareList<string>(16, 14, 12);
            refList = new List<string>(list);
            list.InsertRange(2, toAdd);
            refList.InsertRange(2, toAdd);
            Assert.IsTrue(refList.SequenceEqual(list));
        }

        [Test]
        public void InsertRangeIncreaseCapacity()
        {
            // add last
            CircularList<string> list = PrepareList<string>(4, 0, 4);
            List<string> refList = new List<string>(list);
            string[] toInsert = new string[] { "alpha", "beta", "gamma" };
            list.InsertRange(4, toInsert);
            refList.InsertRange(4, toInsert);
            Assert.IsTrue(refList.SequenceEqual(list));

            // add first
            list = PrepareList<string>(4, 0, 4);
            refList = new List<string>(list);
            list.InsertRange(0, toInsert);
            refList.InsertRange(0, toInsert);
            Assert.IsTrue(refList.SequenceEqual(list));

            // no wrap
            list = PrepareList<string>(4, 0, 4);
            refList = new List<string>(list);
            list.InsertRange(2, toInsert);
            refList.InsertRange(2, toInsert);
            Assert.IsTrue(refList.SequenceEqual(list));

            // wrap before index
            list = PrepareList<string>(4, 3, 4);
            refList = new List<string>(list);
            list.InsertRange(2, toInsert);
            refList.InsertRange(2, toInsert);
            Assert.IsTrue(refList.SequenceEqual(list));

            // wrap at index
            list = PrepareList<string>(4, 2, 4);
            refList = new List<string>(list);
            list.InsertRange(2, toInsert);
            refList.InsertRange(2, toInsert);
            Assert.IsTrue(refList.SequenceEqual(list));

            // wrap after index
            list = PrepareList<string>(4, 1, 4);
            refList = new List<string>(list);
            list.InsertRange(2, toInsert);
            refList.InsertRange(2, toInsert);
            Assert.IsTrue(refList.SequenceEqual(list));

            // inserting more items that fits with duplicating
            list = PrepareList<string>(2, 1, 2);
            refList = new List<string>(list);
            list.InsertRange(1, toInsert);
            refList.InsertRange(1, toInsert);
            Assert.IsTrue(refList.SequenceEqual(list));
        }

        [Test]
        public void RemoveLastAndFirst()
        {
            // normal, non-wrapped list
            CircularList<int> list = PrepareList<int>(4, 0, 4);
            list.Remove(3); // last
            list.Remove(0); // first
            list.Remove(2); // last
            list.Remove(1); // first
            Assert.AreEqual(0, list.Count);

            // wrapped list, removing lasts
            list = PrepareList<int>(4, 2, 4);
            list.Remove(3);
            list.Remove(2);
            list.Remove(1);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(0, list[0]);

            // wrapped list, removing firsts
            list = PrepareList<int>(4, 2, 4);
            list.RemoveAt(0);
            list.RemoveAt(0);
            list.RemoveAt(0);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(3, list[0]);
        }

        [Test]
        public void RemoveDetailed()
        {
            // no wrapping, moving down
            CircularList<string> list = PrepareList<string>(8, 0, 6);
            list.RemoveAt(4);
            Assert.IsFalse(list.Contains("4"));

            // no wrapping, moving up
            list = PrepareList<string>(8, 0, 6);
            list.RemoveAt(1);
            Assert.IsFalse(list.Contains("1"));

            // wrap + 1, simple moving up
            list = PrepareList<string>(8, 3, 6);
            list.RemoveAt(1);
            Assert.IsFalse(list.Contains("1"));

            // wrap + 3, moving down from wrapped
            list = PrepareList<string>(12, 5, 10);
            list.RemoveAt(8);
            Assert.IsFalse(list.Contains("8"));

            // wrap + 1, moving first to last
            list = PrepareList<string>(12, 3, 10);
            list.RemoveAt(8);
            Assert.IsFalse(list.Contains("8"));

            // wrap + 2, moving first to last, moving down from wrapped
            list = PrepareList<string>(12, 4, 10);
            list.RemoveAt(7);
            Assert.IsFalse(list.Contains("7"));

            // wrap + 1, simple moving down from end, moving first to last
            list = PrepareList<string>(12, 3, 10);
            list.RemoveAt(7);
            Assert.IsFalse(list.Contains("7"));

            // wrap + 2, simple moving down from end, moving first to last, moving down from wrapped
            list = PrepareList<string>(12, 3, 11);
            list.RemoveAt(7);
            Assert.IsFalse(list.Contains("7"));

            // wrap - 1, simple moving down
            list = PrepareList<string>(8, 7, 6);
            list.RemoveAt(4);
            Assert.IsFalse(list.Contains("4"));

            // wrap - 3, moving up from wrapped
            list = PrepareList<string>(12, 9, 10);
            list.RemoveAt(1);
            Assert.IsFalse(list.Contains("1"));

            // wrap - 1, moving last to first
            list = PrepareList<string>(12, 11, 10);
            list.RemoveAt(1);
            Assert.IsFalse(list.Contains("1"));

            // wrap - 2, moving last to first, moving up from wrapped
            list = PrepareList<string>(12, 10, 10);
            list.RemoveAt(2);
            Assert.IsFalse(list.Contains("2"));

            // wrap - 1, simple moving up beginning, moving last to first
            list = PrepareList<string>(12, 11, 10);
            list.RemoveAt(2);
            Assert.IsFalse(list.Contains("2"));

            // wrap - 2, simple moving up beginning, moving last to first, moving up wrapped
            list = PrepareList<string>(12, 10, 11);
            list.RemoveAt(3);
            Assert.IsFalse(list.Contains("3"));

            list = PrepareList<string>(12, 8, 8);
            list.Remove("5");
            list.Remove("2");
            Assert.IsFalse(list.Contains("2"));
            Assert.IsFalse(list.Contains("5"));
            list.Clear();
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void AddRangeLastAndFirst()
        {
            CircularList<int> list = PrepareList<int>(10, 0, 2);
            int[] toAdd = new int[] { 10, 11, 12, 13 };

            // adding without wrapping
            list.AddRange(toAdd);

            // adding without wrapping - fit to end
            list.AddRange(toAdd);

            // resizing, adding without wrapping
            list.AddRange(toAdd);

            // startIndex > 0, adding without wrapping
            list = PrepareList<int>(16, 3, 2);
            list.AddRange(toAdd);
            list.AddRange(toAdd);

            // adding with wrapping
            list.AddRange(toAdd);

            // adding without wrapping to already wrapped list
            list.AddRange(new int[] { 100, 101 });

            // resizing, adding without wrapping
            list.AddRange(toAdd);

            list = PrepareList<int>(10, 8, 2);

            // adding without wrapping
            list.InsertRange(0, toAdd);

            // adding without wrapping - fit to beginning
            list.InsertRange(0, toAdd);

            // resizing, adding with wrapping to the end
            list.InsertRange(0, toAdd);

            // startIndex > 0, adding without wrapping
            list = PrepareList<int>(16, 10, 2);
            list.InsertRange(0, toAdd);
            list.InsertRange(0, toAdd);

            // adding with wrapping
            list.InsertRange(0, toAdd);

            // adding without wrapping to already wrapped list
            list.InsertRange(0, new int[] { 100, 101 });

            // resizing, adding with wrapping to the end
            list.InsertRange(0, toAdd);
        }

        [Test]
        public void RemoveRangeSimple()
        {
            CircularList<int> list = PrepareList<int>(8, 4, 8);

            // remove 2 of wrapped, remains wrapped
            list.RemoveRange(6, 2); // last 2

            // remove both wrapped an non-wrapped part
            list.RemoveRange(3, 3); // last 3

            // remove from non-wrapped
            list.RemoveRange(1, 2); // last 2
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(0, list[0]);

            list = PrepareList<int>(8, 4, 8);

            // remove 2 of non-wrapped
            list.RemoveRange(0, 2);

            // remove both wrapped an non-wrapped part
            list.RemoveRange(0, 3);

            // remove from non-wrapped
            list.RemoveRange(0, 2);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(7, list[0]);

            // 1 element wrapped from down
            list = new CircularList<int>(4);
            list.InsertRange(0, new[] { 1 }); // startIndex = 3, count = 1
            list.RemoveRange(0, 1);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void RemoveRangeShiftUp()
        {
            // non-wrapped, remove from middle
            CircularList<int?> list = PrepareList<int?>(8, 2, 4);
            list.RemoveRange(1, 2);
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 3 }));

            // wrapped, remove from non-wrapped part, remains wrapped
            list = PrepareList<int?>(8, 5, 6);
            list.RemoveRange(1, 2);
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 3, 4, 5 }));

            // wrapped, remove from non-wrapped and wrapped part, remains wrapped
            list = PrepareList<int?>(8, 5, 6);
            list.RemoveRange(2, 2);
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 4, 5 }));

            // wrapped, remove from non-wrapped and wrapped part, becomes non-wrapped
            list = PrepareList<int?>(8, 5, 6);
            list.RemoveRange(1, 4);
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 5 }));
        }

        [Test]
        public void RemoveRangeShiftDown()
        {
            // non-wrapped, remove from middle
            CircularList<int?> list = PrepareList<int?>(10, 1, 8);
            list.RemoveRange(5, 2);
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 2, 3, 4, 7 }));

            // wrapped, remove from wrapped part, remains wrapped
            list = PrepareList<int?>(10, 5, 8);
            list.RemoveRange(5, 2);
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 2, 3, 4, 7 }));

            // wrapped, remove from non-wrapped and wrapped part, remains wrapped
            list = PrepareList<int?>(10, 4, 9);
            list.RemoveRange(5, 2);
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 2, 3, 4, 7, 8 }));

            // wrapped, remove from non-wrapped and wrapped part, becomes non-wrapped
            list = PrepareList<int?>(10, 3, 9);
            list.RemoveRange(5, 3);
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 2, 3, 4, 8 }));

        }

        [Test]
        public void ReplaceRange()
        {
            // replace same amount
            CircularList<int?> list = PrepareList<int?>(10, 0, 8);
            list.ReplaceRange(5, 2, new int?[] { null, -1 });
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 2, 3, 4, null, -1, 7 }));

            // remove more, add less
            list = PrepareList<int?>(10, 0, 8);
            list.ReplaceRange(5, 2, new int?[] { null });
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 2, 3, 4, null, 7 }));

            // remove less, add more as IList
            list = PrepareList<int?>(10, 0, 8);
            list.ReplaceRange(5, 2, new int?[] { null, -1, -2 });
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 2, 3, 4, null, -1, -2, 7 }));

            // remove less, add more as IEnumerable
            list = PrepareList<int?>(10, 0, 8);
            list.ReplaceRange(5, 2, new int?[] { null, -1, -2 }.Select(i => i));
            Assert.IsTrue(list.SequenceEqual(new int?[] { 0, 1, 2, 3, 4, null, -1, -2, 7 }));
        }

        [Test]
        public void RemoveAll()
        {
            CircularList<int?> clist = PrepareList<int?>(10, 4, 8);
            List<int?> list = new List<int?>(clist);

            // no removing
            list.RemoveAll(i => false);
            clist.RemoveAll(i => false);
            Assert.IsTrue(clist.SequenceEqual(list));

            // remove from the end
            list.RemoveAll(i => i > 4);
            clist.RemoveAll(i => i > 4);
            Assert.IsTrue(clist.SequenceEqual(list));

            // remove from the beginning
            clist = PrepareList<int?>(10, 8, 8);
            list = new List<int?>(clist);
            list.RemoveAll(i => i < 4);
            clist.RemoveAll(i => i < 4);
            Assert.IsTrue(clist.SequenceEqual(list));

            // remove every second
            clist = PrepareList<int?>(10, 8, 8);
            list = new List<int?>(clist);
            list.RemoveAll(i => (i & 1) == 0);
            clist.RemoveAll(i => (i & 1) == 0);
            Assert.IsTrue(clist.SequenceEqual(list));
        }

        [Test]
        public void Find()
        {
            CircularList<int> list = PrepareList<int>(10, 0, 5);
            // normal positioning
            Assert.IsTrue(list.Contains(3));
            Assert.AreEqual(3, list.IndexOf(3));
            Assert.AreEqual(3, list.LastIndexOf(3));
            Assert.AreEqual(3, list.FindIndex(i => i == 3));
            Assert.AreEqual(3, list.FindLastIndex(i => i == 3));
            Assert.AreEqual(3, list.Find(i => i == 3));
            Assert.AreEqual(3, list.FindLast(i => i == 3));
            Assert.IsTrue(list.Exists(i => i == 3));

            // wrapped
            list = PrepareList<int>(10, 6, 5);

            // requesting non-wrapped element
            Assert.IsTrue(list.Contains(3));
            Assert.AreEqual(3, list.IndexOf(3));
            Assert.AreEqual(3, list.LastIndexOf(3));
            Assert.AreEqual(3, list.FindIndex(i => i == 3));
            Assert.AreEqual(3, list.FindLastIndex(i => i == 3));
            Assert.AreEqual(3, list.Find(i => i == 3));
            Assert.AreEqual(3, list.FindLast(i => i == 3));
            Assert.IsTrue(list.Exists(i => i == 3));

            // requesting wrapped element
            Assert.IsTrue(list.Contains(4));
            Assert.AreEqual(4, list.IndexOf(4));
            Assert.AreEqual(4, list.LastIndexOf(4));
            Assert.AreEqual(4, list.FindIndex(i => i == 4));
            Assert.AreEqual(4, list.FindLastIndex(i => i == 4));
            Assert.AreEqual(4, list.Find(i => i == 4));
            Assert.AreEqual(4, list.FindLast(i => i == 4));
            Assert.AreEqual(2, list.FindAll(i => i == 3 || i == 4).Count());
            Assert.IsTrue(list.Exists(i => i == 4));

            // requesting non-existing element
            Assert.IsFalse(list.Contains(13));
            Assert.AreEqual(-1, list.IndexOf(13));
            Assert.AreEqual(-1, list.LastIndexOf(13));
            Assert.AreEqual(-1, list.FindIndex(i => i == 13));
            Assert.AreEqual(-1, list.FindLastIndex(i => i == 13));
            Assert.AreEqual(0, list.Find(i => i == 13));
            Assert.AreEqual(0, list.FindLast(i => i == 13));
            Assert.AreEqual(0, list.FindAll(i => i == 13).Count());
            Assert.IsFalse(list.Exists(i => i == 13));

            // [Find][Last]IndexOf with index[/count]
            list = PrepareList<int>(6, 4, 4);

            // non carried part
            Assert.AreEqual(1, list.IndexOf(1, 1)); // possible carry
            Assert.AreEqual(1, list.FindIndex(1, i => i == 1)); // possible carry
            Assert.AreEqual(1, list.IndexOf(1, 1, 1)); // search only in non-carry
            Assert.AreEqual(1, list.FindIndex(1, 1, i => i == 1)); // search only in non-carry
            Assert.AreEqual(1, list.LastIndexOf(1, 1)); // search only in non-carry
            Assert.AreEqual(1, list.FindLastIndex(1, i => i == 1)); // search only in non-carry
            Assert.AreEqual(1, list.LastIndexOf(1, 1, 1)); // search only in non-carry
            Assert.AreEqual(1, list.FindLastIndex(1, 1, i => i == 1)); // search only in non-carry

            // carried part
            Assert.AreEqual(3, list.IndexOf(3, 1)); // start on non-carried part
            Assert.AreEqual(3, list.FindIndex(1, i => i == 3)); // start on non-carried part
            Assert.AreEqual(3, list.IndexOf(3, 2, 2)); // start on carried part
            Assert.AreEqual(3, list.FindIndex(2, 2, i => i == 3)); // start on carried part
            Assert.AreEqual(-1, list.IndexOf(3, 2, 1)); // searching too few elements
            Assert.AreEqual(-1, list.FindIndex(2, 1, i => i == 3)); // searching too few elements
            Assert.AreEqual(1, list.LastIndexOf(1, 3)); // start on carried part
            Assert.AreEqual(1, list.FindLastIndex(3, i => i == 1)); // start on carried part
            Assert.AreEqual(1, list.LastIndexOf(1, 3, 3)); // start on carried part
            Assert.AreEqual(1, list.FindLastIndex(3, 3, i => i == 1)); // start on carried part
            Assert.AreEqual(2, list.LastIndexOf(2, 3)); // search and find in carried part
            Assert.AreEqual(2, list.FindLastIndex(3, i => i == 2)); // search and find in carried part
            Assert.AreEqual(2, list.LastIndexOf(2, 3, 2)); // search and find in carried part
            Assert.AreEqual(2, list.FindLastIndex(3, 2, i => i == 2)); // search and find in carried part
            Assert.AreEqual(-1, list.LastIndexOf(2, 3, 1)); // searching too few elements
            Assert.AreEqual(-1, list.FindLastIndex(3, 1, i => i == 2)); // searching too few elements
        }

        [Test]
        public void CopyTo()
        {
            string[] dest = new string[12];

            CircularList<string> list = PrepareList<string>(10, 5, 10);
            list.CopyTo(dest, 0);
            Assert.AreEqual("0", dest[0]);
            Assert.AreEqual("6", dest[6]);

            Array.Clear(dest, 0, 12);
            list.CopyTo(dest, 2);
            Assert.AreEqual(null, dest[0]);
            Assert.AreEqual("0", dest[2]);
            Assert.AreEqual("6", dest[8]);

            Array.Clear(dest, 0, 12);
            list.CopyTo(6, dest, 2, 2); // from wrapped part only
            Assert.AreEqual(null, dest[0]);
            Assert.AreEqual("6", dest[2]);
            Assert.AreEqual("7", dest[3]);
            Assert.AreEqual(null, dest[4]);

            Array.Clear(dest, 0, 12);
            list.CopyTo(2, dest, 2, 2); // from non wrapped part only
            Assert.AreEqual(null, dest[0]);
            Assert.AreEqual("2", dest[2]);
            Assert.AreEqual("3", dest[3]);
            Assert.AreEqual(null, dest[4]);

            Array.Clear(dest, 0, 12);
            list.CopyTo(1, dest, 2, 8); // from non wrapped and wrapped part
            Assert.AreEqual(null, dest[0]);
            Assert.AreEqual(null, dest[1]);
            for (int i = 2; i < 10; i++)
            {
                Assert.AreEqual(list[i - 1], dest[i]);
            }
            Assert.AreEqual(null, dest[10]);
        }

        [Test]
        public void Enumerator()
        {
            CircularList<string> list = PrepareList<string>(10, 7, 9);
            List<string> reference = new List<string>(list);
            Assert.IsTrue(list.SequenceEqual(reference));
        }

        [Test]
        public void Reverse()
        {
            // normal reversing
            CircularList<string> list = PrepareList<string>(6, 0, 4);
            List<string> reference = new List<string>(list);
            list.Reverse();
            reference.Reverse();
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped reversing
            list = PrepareList<string>(6, 4, 4);
            reference = new List<string>(list);
            list.Reverse();
            reference.Reverse();
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped list, reversing non wrapped part only
            list = PrepareList<string>(6, 4, 4);
            reference = new List<string>(list);
            list.Reverse(0, 2);
            reference.Reverse(0, 2);
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped list, reversing carried part only
            list = PrepareList<string>(6, 4, 4);
            reference = new List<string>(list);
            list.Reverse(2, 2);
            reference.Reverse(2, 2);
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped list, wrapped section
            list = PrepareList<string>(6, 4, 4);
            reference = new List<string>(list);
            list.Reverse(1, 2);
            reference.Reverse(1, 2);
            Assert.IsTrue(list.SequenceEqual(reference));
        }

        [Test]
        public void Sort()
        {
            // normal sorting
            CircularList<string> list = PrepareList<string>(6, 0, 4);
            List<string> reference = new List<string>(list);
            list.Reverse();
            reference.Reverse();
            list.Sort();
            reference.Sort();
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped sorting, list is full: no adjust
            list = PrepareList<string>(6, 4, 6);
            reference = new List<string>(list);
            list.Reverse();
            reference.Reverse();
            list.Sort();
            reference.Sort();
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped sorting, move down
            list = PrepareList<string>(8, 6, 6);
            reference = new List<string>(list);
            list.Reverse();
            reference.Reverse();
            list.Sort();
            reference.Sort();
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped sorting, move up
            list = PrepareList<string>(8, 4, 6);
            reference = new List<string>(list);
            list.Reverse();
            reference.Reverse();
            list.Sort();
            reference.Sort();
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped list, sorting non wrapped part only
            list = PrepareList<string>(6, 4, 4);
            reference = new List<string>(list);
            list.Reverse();
            reference.Reverse();
            list.Sort(0, 2, null);
            reference.Sort(0, 2, null);
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped list, sorting carried part only
            list = PrepareList<string>(6, 4, 4);
            reference = new List<string>(list);
            list.Reverse();
            reference.Reverse();
            list.Sort(2, 2, null);
            reference.Sort(2, 2, null);
            Assert.IsTrue(list.SequenceEqual(reference));

            // wrapped sorting, wrapped subsection: full copy
            list.Reverse();
            reference.Reverse();
            list.Sort(1, 2, null);
            reference.Sort(1, 2, null);
            Assert.IsTrue(list.SequenceEqual(reference));
        }

        [Test]
        public void BinarySearch()
        {
            CircularList<string> list = PrepareList<string>(10, 6, 8);
            List<string> reference = new List<string>(list);

            // searching in non wrapped
            Assert.AreEqual(reference.BinarySearch(1, 3, "2", null), list.BinarySearch(1, 3, "2", null)); // found
            Assert.AreEqual(reference.BinarySearch(1, 3, "4", null), list.BinarySearch(1, 3, "4", null)); // not found

            // searching in wrapped, continuous
            Assert.AreEqual(reference.BinarySearch(5, 3, "6", null), list.BinarySearch(5, 3, "6", null)); // found
            Assert.AreEqual(reference.BinarySearch(5, 3, "4", null), list.BinarySearch(5, 3, "4", null)); // not found

            // searching in wrapped area, with self helpers
            Assert.AreEqual(reference.BinarySearch(1, 6, "2", null), list.BinarySearch(1, 6, "2", null)); // as IComparable, found
            Assert.AreEqual(reference.BinarySearch(1, 6, "0", null), list.BinarySearch(1, 6, "0", null)); // as IComparable, not found
            Assert.AreEqual(reference.BinarySearch(1, 6, "2", StringComparer.Ordinal), list.BinarySearch(1, 6, "2", StringComparer.Ordinal)); // with IComparer, found
            Assert.AreEqual(reference.BinarySearch(1, 6, "0", StringComparer.Ordinal), list.BinarySearch(1, 6, "0", StringComparer.Ordinal)); // with IComparer, not found
        }

        [Test]
        public void GetRangeTest()
        {
            CircularList<string> list = PrepareList<string>(10, 6, 8);
            List<string> reference = new List<string>(list);

            Assert.IsTrue(reference.GetRange(1, 3).SequenceEqual(list.GetRange(1, 3))); // non wrapped
            Assert.IsTrue(reference.GetRange(5, 3).SequenceEqual(list.GetRange(5, 3))); // wrapped only
            Assert.IsTrue(reference.GetRange(1, 6).SequenceEqual(list.GetRange(1, 6))); // both
        }

        [Test]
        public void NonGenericAccess()
        {
            // No assert here. Just tests whether runs without throwing an exception.
            // adding null to nullable (does not work in .NET 3.5 List)
            IList ilist = new CircularList<int?>();
            ilist.Add(null);

            // enums are compatible with their underlying type
            ilist = new CircularList<int>();
            ilist.Add(ConsoleColor.Black);

            // in both direction
            ilist = new CircularList<ConsoleColor>();
            ilist.Add(1);
        }

        #endregion

        #endregion
    }
}
