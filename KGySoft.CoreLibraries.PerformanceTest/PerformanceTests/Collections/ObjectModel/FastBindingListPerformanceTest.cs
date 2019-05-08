#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastBindingListPerformanceTest.cs
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
using System.ComponentModel;
using System.Linq;

using KGySoft.ComponentModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections.ObjectModel
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
            var range = Enumerable.Range(0, 10000).Select(i => new TestItem { IntProp = i });
            // ReSharper disable PossibleMultipleEnumeration - intended to prevent sharing elements
            var collReference = new BindingList<TestItem>(new List<TestItem>(range.ToList()));
            var collTest = new FastBindingList<TestItem>(new List<TestItem>(range.ToList()));

            new PerformanceTest
                {
                    Iterations = 100,
                    Repeat = 5,
                }
                .AddCase(() =>
                {
                    collReference.AddNew();
                    collReference.RemoveAt(collReference.Count - 1);
                }, "BindingList.AddNew")
                .AddCase(() =>
                {
                    collTest.AddNew();
                    collTest.RemoveAt(collTest.Count - 1);
                }, "FastBindingList.AddNew")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void ItemChanged()
        {
            // item property change involves a search for the index of the changed item
            var range = Enumerable.Range(0, 10000).Select(i => new TestItem { IntProp = i });

            // ReSharper disable PossibleMultipleEnumeration - intended to prevent sharing elements
            var collReference = new BindingList<TestItem>(new List<TestItem>(range.ToList()));
            var collTest = new FastBindingList<TestItem>(new List<TestItem>(range.ToList()));
            var rnd = new Random(0);
            new PerformanceTest
                {
                    Iterations = 1000,
                    Repeat = 5
                }
                .AddCase(() => collReference[rnd.Next(collReference.Count)].IntProp = rnd.Next(), "BindingList.ItemChanged")
                .AddCase(() => collTest[rnd.Next(collTest.Count)].IntProp = rnd.Next(), "FastBindingList.ItemChanged")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}
