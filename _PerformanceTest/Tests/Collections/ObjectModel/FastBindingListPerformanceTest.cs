using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using KGySoft.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest.Tests.Collections.ObjectModel
{
    [TestClass]
    public class FastBindingListPerformanceTest : TestBase
    {
        private class TestItem : ObservableObjectBase
        {
            public int IntProp { get => Get<int>(); set => Set(value); }
        }

        [TestMethod]
        public void AddNew()
        {
            var range = Enumerable.Range(0, 10000).Select(i => new TestItem { IntProp = i });
            // ReSharper disable PossibleMultipleEnumeration - intended to prevent sharing elements
            var collReference = new BindingList<TestItem>(new List<TestItem>(range.ToList()));
            var collTest = new FastBindingList<TestItem>(new List<TestItem>(range.ToList()));

            new TestOperation
            {
                RefOpName = "BindingList.AddNew",
                ReferenceOperation = () =>
                {
                    collReference.AddNew();
                    collReference.RemoveAt(collReference.Count - 1);
                },
                TestOpName = "FastBindingList.AddNew",
                TestOperation = () =>
                {
                    collTest.AddNew();
                    collTest.RemoveAt(collTest.Count - 1);
                },
                Iterations = 100,
                Repeat = 5,
            }.DoTest();
        }

        [TestMethod]
        public void ItemChanged()
        {
            // item property change involves a search for the index of the changed item
            var range = Enumerable.Range(0, 10000).Select(i => new TestItem { IntProp = i });
            // ReSharper disable PossibleMultipleEnumeration - intended to prevent sharing elements
            var collReference = new BindingList<TestItem>(new List<TestItem>(range.ToList()));
            var collTest = new FastBindingList<TestItem>(new List<TestItem>(range.ToList()));
            var rnd = new Random(0);
            const int iterations = 1000;
            const int repeat = 5;

            new TestOperation
            {
                TestOpName = "BindingList.ItemChanged",
                TestOperation = () => collReference[rnd.Next(collReference.Count)].IntProp = rnd.Next(),
                Iterations = iterations,
                Repeat = repeat
            }.DoTest();

            rnd = new Random(0);
            new TestOperation
            {
                TestOpName = "FastBindingList.ItemChanged",
                TestOperation = () => collTest[rnd.Next(collTest.Count)].IntProp = rnd.Next(),
                Iterations = iterations,
                Repeat = repeat
            }.DoTest();
        }
    }
}
