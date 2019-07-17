#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SortableBindingListTest.cs
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using KGySoft.Collections;
using KGySoft.ComponentModel;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class SortableBindingListTest : TestBase
    {
        #region Methods

        #region Public Methods

        [Test]
        public void AddExplicit()
        {
            var coll = new SortableBindingList<int>();
            coll.Add(1);
            coll.Add(2);
            coll.Add(1);
            coll.Insert(0, 1);
            coll.ApplySort(ListSortDirection.Ascending);
            AssertConsistency(coll);
            AssertSorted(coll);

            // adding after sorted
            coll.Add(1);
            coll.Add(2);
            coll.Add(1);
            coll.Insert(0, 1);
            AssertConsistency(coll);

            // switching on sort on change sorts immediately
            coll.SortOnChange = true;
            AssertConsistency(coll);
            AssertSorted(coll);

            // adding when SortOnChange is true sorts on add
            coll.Add(0);
            AssertConsistency(coll);
            AssertSorted(coll);
        }

        [Test]
        public void SetExplicit()
        {
            var coll = new SortableBindingList<int>(new[] { 1, 2, 3, 2, 1 }) { CheckConsistency = false };
            coll[1] = 1;
            coll[4] = 2;
            coll.ApplySort(ListSortDirection.Ascending);
            AssertConsistency(coll);

            // setting after sorted
            coll[1] = 0;
            AssertConsistency(coll);

            // switching on sort on change sorts immediately
            coll.SortOnChange = true;
            AssertConsistency(coll);
            AssertSorted(coll);

            // setting when SortOnChange is true sorts on change
            coll[0] = 13;
            AssertConsistency(coll);
            AssertSorted(coll);
        }

        [Test]
        public void RemoveExplicit()
        {
            var coll = new SortableBindingList<int>(new List<int> { 1, 2, 3, 2, 1 }) { CheckConsistency = false };
            coll.ApplySort(ListSortDirection.Ascending);
            coll.Remove(1); // first 1
            coll.RemoveAt(1); // 2
            coll.Remove(1); // last 1 (1st element)
            AssertConsistency(coll);
        }

        [Test]
        public void AddInner()
        {
            var inner = new List<string>();
            var coll = new SortableBindingList<string>(inner) { CheckConsistency = false };
            coll.Add("x");
            coll.Add("z");
            coll.Add("y");
            coll.ApplySort(ListSortDirection.Ascending);

            // causing inconsistency
            inner.Add("a");
            inner.RemoveAt(0); // making sure inner length does not change; otherwise, inconsistency is detected and fixed in Assert
            Throws<AssertionException>(() => AssertConsistency(coll));
            coll.CheckConsistency = true;

            // inconsistency detected and fixed on next insert
            coll.Insert(0, "b");
            AssertConsistency(coll);
        }

        [Test]
        public void SetInner()
        {
            var inner = new List<string> { "0", "3", "2", "3", "1" };
            var coll = new SortableBindingList<string>(inner) { CheckConsistency = false };
            coll.ApplySort(ListSortDirection.Ascending);

            // causing inconsistency
            inner[0] = null;
            Throws<AssertionException>(() => AssertConsistency(coll));

            // inconsistency detected and fixed on next set
            coll.CheckConsistency = true;
            coll[0] = "x";
            AssertConsistency(coll);
        }

        [Test]
        public void RemoveInner()
        {
            var inner = new List<string> { "1", "2", "3", "2", "1" };
            var coll = new SortableBindingList<string>(inner) { CheckConsistency = false };
            coll.ApplySort(ListSortDirection.Ascending);

            // causing inconsistency
            inner.RemoveAt(2);
            inner.Add("a"); // making sure inner length does not change; otherwise, inconsistency is detected and fixed in Assert
            Throws<AssertionException>(() => AssertConsistency(coll));

            // inconsistency detected and fixed on next remove
            coll.CheckConsistency = true;
            coll.RemoveAt(0);
            AssertConsistency(coll);
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

        #region Private Methods

        private void AssertConsistency<T>(SortableBindingList<T> coll)
        {
            var itemToIndex = new AllowNullDictionary<T, CircularList<int>>();
            for (int i = 0; i < coll.Count; i++)
            {
                T item = coll[i];
                if (!itemToIndex.TryGetValue(item, out CircularList<int> indices))
                {
                    indices = new CircularList<int>();
                    itemToIndex[item] = indices;
                }

                indices.Add(i);
            }

            var actualItemToIndex = (AllowNullDictionary<T, CircularList<int>>)Reflector.GetField(coll, "itemToSortedIndex");
            AssertItemsEqual(Sorted(itemToIndex), Sorted(actualItemToIndex));

            IEnumerable Sorted(AllowNullDictionary<T, CircularList<int>> dict)
                => new AllowNullDictionary<T, CircularList<int>>(dict.OrderBy(item => item.Key));
        }

        private void AssertSorted<T>(SortableBindingList<T> coll)
        {
            var check = new List<T>(coll);
            check.Sort();
            AssertItemsEqual(check, coll);
        }

        #endregion

        #endregion
    }
}
