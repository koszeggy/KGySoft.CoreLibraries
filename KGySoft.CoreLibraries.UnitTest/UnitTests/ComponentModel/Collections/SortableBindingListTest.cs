#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SortableBindingListTest.cs
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
using System.ComponentModel;
using System.Linq;

using KGySoft.Collections;
using KGySoft.ComponentModel;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel.Collections
{
    [TestFixture]
    public class SortableBindingListTest : TestBase
    {
        #region Methods

        #region Static Methods

        private static void AssertConsistency<T>(SortableBindingList<T> coll, bool expectNull = false)
        {
            var actualItemToIndex = (AllowNullDictionary<T, int>)Reflector.GetField(coll, "itemToSortedIndex");
            if (expectNull)
            {
                Assert.IsNull(actualItemToIndex);
                return;
            }

            Assert.IsNotNull(actualItemToIndex);
            var itemToIndex = new AllowNullDictionary<T, int>();
            for (int i = 0; i < coll.Count; i++)
            {
                T item = coll[i];
                if (!itemToIndex.TryGetValue(item, out int index))
                    itemToIndex[item] = i;
                else if (index >= 0)
                    itemToIndex[item] = ~index;
            }

            AssertItemsEqual(Sorted(itemToIndex), Sorted(actualItemToIndex));

            IEnumerable Sorted(AllowNullDictionary<T, int> dict)
                => new AllowNullDictionary<T, int>(dict.OrderBy(item => item.Key));
        }

        private static void AssertSorted<T>(SortableBindingList<T> coll, bool ascending = true)
        {
            var check = new List<T>(coll);
            check.Sort();
            if (!ascending)
                check.Reverse();
            AssertItemsEqual(check, coll);
        }

        #endregion

        #region Instance Methods

        [TestCase(ListSortDirection.Ascending)]
        [TestCase(ListSortDirection.Descending)]
        public void UsageTest(ListSortDirection direction)
        {
            var coll = new SortableBindingList<int> { CheckConsistency = true };
            coll.ApplySort(direction);

            coll.Add(1);
            Assert.AreEqual(0, coll.IndexOf(1));

            coll.Add(2);
            Assert.AreEqual(1, coll.IndexOf(2));

            // adding a duplicate: still the first index is returned
            coll.Add(1);
            Assert.AreEqual(0, coll.IndexOf(1));

            // inserting at the first position
            coll.Insert(0, 1);
            Assert.AreEqual(0, coll.IndexOf(1));
            Assert.AreEqual(2, coll.IndexOf(2));
            AssertConsistency(coll);

            // non existing
            Assert.AreEqual(-1, coll.IndexOf(0));

            // adding and removing unique at the end
            coll.Add(100);
            AssertConsistency(coll);
            Assert.AreEqual(coll.Count - 1, coll.IndexOf(100));
            coll.Remove(100);
            AssertConsistency(coll);
            Assert.AreEqual(-1, coll.IndexOf(100));

            // inserting and removing clears the mapping, IndexOf rebuilds it
            coll.Insert(0, -100);
            AssertConsistency(coll, true);
            Assert.AreEqual(0, coll.IndexOf(-100));
            AssertConsistency(coll);
            coll.Remove(-100);
            AssertConsistency(coll, true);
            Assert.AreEqual(-1, coll.IndexOf(-100));
            AssertConsistency(coll);

            // setting a duplicate clears the mapping, IndexOf rebuilds it
            coll[0] = -1;
            AssertConsistency(coll, true);
            Assert.AreEqual(0, coll.IndexOf(-1));
            AssertConsistency(coll);

            // removing a duplicate at the end clears the mapping
            coll.RemoveAt(coll.Count - 1);
            AssertConsistency(coll, true);

            coll.ApplySort(direction);
            AssertSorted(coll, direction == ListSortDirection.Ascending);
        }

        [Test]
        public void AddTest()
        {
            var coll = new SortableBindingList<int>();
            coll.Add(1);
            coll.Add(2);
            coll.Add(1);
            coll.Insert(0, 1);
            coll.ApplySort(ListSortDirection.Ascending);
            AssertConsistency(coll, true);
            AssertSorted(coll);

            // adding after sorted
            coll.Add(1);
            coll.Add(2);
            coll.Add(1);
            coll.Insert(0, 1);
            AssertConsistency(coll, true);

            // switching on sort on change sorts immediately
            coll.SortOnChange = true;
            AssertConsistency(coll, true);
            AssertSorted(coll);

            // adding when SortOnChange is true sorts on add
            coll.Add(0);
            AssertConsistency(coll, true);
            AssertSorted(coll);
        }

        [Test]
        public void SetTest()
        {
            var coll = new SortableBindingList<int>(new[] { 1, 2, 3, 2, 1 }) { CheckConsistency = false };
            coll[1] = 1;
            coll[4] = 2;
            coll.ApplySort(ListSortDirection.Ascending);
            AssertConsistency(coll, true);
            AssertSorted(coll);

            // setting after sorted
            coll[1] = 0;
            AssertConsistency(coll, true);

            // switching on sort on change sorts immediately
            coll.SortOnChange = true;
            AssertConsistency(coll, true);
            AssertSorted(coll);

            // setting when SortOnChange is true sorts on change
            coll[0] = 13;
            AssertConsistency(coll, true);
            AssertSorted(coll);
        }

        [Test]
        public void RemoveTest()
        {
            var coll = new SortableBindingList<int>(new List<int> { 1, 2, 3, 2, 1 }) { CheckConsistency = false };
            coll.ApplySort(ListSortDirection.Ascending);
            AssertSorted(coll);
            AssertConsistency(coll, true);
            Assert.AreEqual(2, coll.IndexOf(2));
            AssertConsistency(coll);
            coll.Remove(2); // first 2
            AssertConsistency(coll, true); // index map is invalidated when item is removed from the middle
            Assert.AreEqual(2, coll.IndexOf(2));
            coll.RemoveAt(2); // other 2
            Assert.AreEqual(2, coll.IndexOf(3));
            coll.Remove(3); // unique 3 at last position
            AssertConsistency(coll); // Remove preserves index map when unique last item is removed
            AssertSorted(coll);
        }

        [Test]
        public void Find()
        {
            var coll = new SortableBindingList<KeyValuePair<int, string>> { new KeyValuePair<int, string>(1, "1"), new KeyValuePair<int, string>(2, "2") };
            coll.ApplySort(nameof(KeyValuePair<_, _>.Key), ListSortDirection.Ascending);

            Throws<ArgumentException>(() => coll.Find("X", null), "No property descriptor found for property name 'X' in type 'System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]'.");
            Assert.IsTrue(coll.Find(nameof(KeyValuePair<_, _>.Key), 0) < 0);
            Assert.AreEqual(0, coll.Find(nameof(KeyValuePair<_, _>.Key), 1));

            coll.ApplySort(nameof(KeyValuePair<_, _>.Key), ListSortDirection.Descending);
            Assert.AreEqual(1, coll.Find(nameof(KeyValuePair<_, _>.Key), 1));
        }

        #endregion

        #endregion
    }
}
