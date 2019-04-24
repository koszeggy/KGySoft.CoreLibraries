using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using KGySoft.ComponentModel;
using NUnit.Framework;

namespace _PerformanceTest.Tests.Collections.ObjectModel
{
    [TestFixture]
    public class FastBindingListPerformanceTest
    {
        private class TestItem : ObservableObjectBase
        {
            public int IntProp { get => Get<int>(); set => Set(value); }
        }

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
    }
}
