#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastBindingListPerformanceTest.cs
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
    public class FastBindingListPerformanceTest
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
                }, "SortableBindingList.AddNew")
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
            new RandomizedPerformanceTest
                {
                    Iterations = 1000,
                    //Repeat = 5
                }
                .AddCase(rnd => collReference[rnd.Next(count)].IntProp = rnd.Next(), "BindingList.ItemChanged")
                .AddCase(rnd => collTest[rnd.Next(count)].IntProp = rnd.Next(), "FastBindingList.ItemChanged")
                .AddCase(rnd => sortableTest[rnd.Next(count)].IntProp = rnd.Next(), "SortableBindingList.ItemChanged")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}
