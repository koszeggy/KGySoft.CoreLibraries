#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SortableBindingListPerformanceTest.cs
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
using System.ComponentModel;
using System.Linq;

using KGySoft.ComponentModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.ComponentModel
{
    [TestFixture]
    public class SortableBindingListPerformanceTest
    {
        #region Nested classes

        private class TestItem : ObservableObjectBase
        {
            #region Properties

            public int IntProp
            {
                get => Get<int>();
                set => Set(value);
            }

            #endregion

            #region Methods

            public override string ToString() => IntProp.ToString();

            #endregion
        }

        #endregion

        #region Methods

        [Test]
        public void AddNew()
        {
            const int count = 10_000;
            var range = Enumerable.Range(0, count).Select(i => new TestItem { IntProp = i });
            // ReSharper disable PossibleMultipleEnumeration - intended to prevent sharing elements
            var collReference = new BindingList<TestItem>(new List<TestItem>(range.ToList()));
            var collTest = new FastBindingList<TestItem>(new List<TestItem>(range.ToList()));
            var sortableTest = new SortableBindingList<TestItem>(new List<TestItem>(range.ToList()));
            var sortableAscTest = new SortableBindingList<TestItem>(new List<TestItem>(range.ToList()));
            var sortableDescTest = new SortableBindingList<TestItem>(new List<TestItem>(range.ToList()));
            sortableAscTest.ApplySort(nameof(TestItem.IntProp), ListSortDirection.Ascending);
            sortableDescTest.ApplySort(nameof(TestItem.IntProp), ListSortDirection.Descending);

            new PerformanceTest
                {
                    Iterations = 100,
                    //Repeat = 5,
                }
                .AddCase(() =>
                {
                    collReference.AddNew();
                    collReference.CancelNew(count);
                }, "BindingList.AddNew")
                .AddCase(() =>
                {
                    collTest.AddNew();
                    collTest.CancelNew(count);
                }, "FastBindingList.AddNew")
                .AddCase(() =>
                {
                    sortableTest.AddNew();
                    sortableTest.CancelNew(count);
                }, "SortableBindingList.AddNew (Unsorted)")
                .AddCase(() =>
                {
                    sortableAscTest.AddNew();
                    sortableAscTest.CancelNew(count);
                }, "SortableBindingList.AddNew (Ascending)")
                .AddCase(() =>
                {
                    sortableDescTest.AddNew();
                    sortableDescTest.CancelNew(count);
                }, "SortableBindingList.AddNew (Descending)")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void ItemChanged()
        {
            const int count = 10_000;
            var range = Enumerable.Range(0, count).Select(i => new TestItem { IntProp = i });

            // ReSharper disable PossibleMultipleEnumeration - intended to prevent sharing elements
            var collReference = new BindingList<TestItem>(new List<TestItem>(range.ToList()));
            var collTest = new FastBindingList<TestItem>(new List<TestItem>(range.ToList()));
            var sortableTest = new SortableBindingList<TestItem>(new List<TestItem>(range.ToList()));
            var sortableAscTest = new SortableBindingList<TestItem>(new List<TestItem>(range.ToList()));
            var sortableDescTest = new SortableBindingList<TestItem>(new List<TestItem>(range.ToList()));
            sortableAscTest.ApplySort(nameof(TestItem.IntProp), ListSortDirection.Ascending);
            sortableDescTest.ApplySort(nameof(TestItem.IntProp), ListSortDirection.Descending);

            new RandomizedPerformanceTest
                {
                    Iterations = 1000,
                    //Repeat = 5
                }
                .AddCase(rnd => collReference[rnd.Next(count)].IntProp = rnd.Next(), "BindingList.ItemChanged")
                .AddCase(rnd => collTest[rnd.Next(count)].IntProp = rnd.Next(), "FastBindingList.ItemChanged")
                .AddCase(rnd => sortableTest[rnd.Next(count)].IntProp = rnd.Next(), "SortableBindingList.ItemChanged")
                .AddCase(rnd => sortableAscTest[rnd.Next(count)].IntProp = rnd.Next(), "SortableBindingList.ItemChanged (Ascending)")
                .AddCase(rnd => sortableDescTest[rnd.Next(count)].IntProp = rnd.Next(), "SortableBindingList.ItemChanged (Descending)")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void EnumerationTest()
        {
            const int count = 10_000;
            var range = Enumerable.Range(0, count);

            // ReSharper disable PossibleMultipleEnumeration - intended to prevent sharing elements
            var collReference = new BindingList<int>(new List<int>(range.ToList()));
            var collTest = new FastBindingList<int>(new List<int>(range.ToList()));
            var sortableTest = new SortableBindingList<int>(new List<int>(range.ToList()));
            var sortableAscTest = new SortableBindingList<int>(new List<int>(range.ToList()));
            var sortableDescTest = new SortableBindingList<int>(new List<int>(range.ToList()));
            var sortable3Test = new SortableBindingList<int>(new List<int>(range.ToList()));
            var sortable3AscTest = new SortableBindingList<int>(new List<int>(range.ToList()));
            var sortable3DescTest = new SortableBindingList<int>(new List<int>(range.ToList()));
            sortableAscTest.ApplySort(ListSortDirection.Ascending);
            sortableDescTest.ApplySort(ListSortDirection.Descending);
            sortable3AscTest.ApplySort(ListSortDirection.Ascending);
            sortable3DescTest.ApplySort(ListSortDirection.Descending);

            new PerformanceTest<int> { Iterations = 1000 }
                .AddCase(() => collReference.Sum(), "BindingList")
                .AddCase(() => collTest.Sum(), "FastBindingList")
                .AddCase(() => sortableTest.Sum(), "SortableBindingList")
                .AddCase(() => sortableAscTest.Sum(), "SortableBindingList (Ascending)")
                .AddCase(() => sortableDescTest.Sum(), "SortableBindingList (Descending)")
                .AddCase(() => sortable3Test.Sum(), "SortableBindingList3")
                .AddCase(() => sortable3AscTest.Sum(), "SortableBindingList3 (Ascending)")
                .AddCase(() => sortable3DescTest.Sum(), "SortableBindingList3 (Descending)")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}
