using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using KGySoft;
using KGySoft.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Tests.ComponentModel
{
    [TestClass]
    public class FastBindingListTest : TestBase
    {
        [TestMethod]
        public void RaiseItemChangedEventsDefault()
        {
            Assert.IsFalse(new FastBindingList<int>().RaiseItemChangedEvents);
            Assert.IsTrue(new FastBindingList<ObservableObjectBase>().RaiseItemChangedEvents);
        }

        [TestMethod]
        public void AllowNewDefault()
        {
            Assert.IsTrue(new FastBindingList<int>().AllowNew);
            Assert.IsFalse(new FastBindingList<int>(new ReadOnlyCollection<int>(new int[0])).AllowNew);
            Assert.IsFalse(new FastBindingList<string>().AllowNew);
        }

        [TestMethod]
        public void Find()
        {
            var coll = new FastBindingList<KeyValuePair<int, string>> { new KeyValuePair<int, string>(1, "1"), new KeyValuePair<int, string>(2, "2") };

            Throws<ArgumentException>(() => coll.Find("X", null), "No property descriptor found for property name 'X' in type 'System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]'.");
            Assert.AreEqual(-1, coll.Find(nameof(KeyValuePair<_,_>.Key), 0));
            Assert.AreEqual(0, coll.Find(nameof(KeyValuePair<_,_>.Key), 1));
        }
    }
}
