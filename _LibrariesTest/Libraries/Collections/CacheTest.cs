using System;
using System.Linq;
using KGySoft.Libraries.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Collections
{
    /// <summary>
    /// Summary description for CacheTest
    /// </summary>
    [TestClass]
    public class CacheTest
    {
        [TestMethod]
        public void SimpleUsage()
        {
            Cache<string, string> cache = new Cache<string, string>(s => s.ToUpperInvariant());
            Assert.AreEqual("ALMA", cache["alma"]);
        }

        [TestMethod]
        public void CacheFullDropOldest()
        {
            Cache<string, string> cache = new Cache<string, string>(s => s.ToUpperInvariant(), 2) { Behavior = CacheBehavior.RemoveOldestElement };
            Console.WriteLine(cache["alma"]);
            Console.WriteLine(cache["béka"]);
            Console.WriteLine(cache["alma"]);
            Console.WriteLine(cache["cica"]);

            Assert.IsFalse(cache.ContainsKey("alma")); // alma was the oldest
            Assert.AreEqual(2, cache.Count);
            Assert.AreEqual(2, cache.Count());
            Assert.AreEqual(2, cache.Keys.Count());
            Assert.AreEqual(2, cache.Values.Count());

            // reloading cica
            Console.WriteLine(cache.GetValueUncached("cica"));
        }

        [TestMethod]
        public void CacheFullDropLeastRecentUsed()
        {
            Cache<string, string> cache = new Cache<string, string>(s => s.ToUpperInvariant(), 2) { Behavior = CacheBehavior.RemoveLeastRecentUsedElement };
            Console.WriteLine(cache["alma"]);
            Console.WriteLine(cache["béka"]);
            Console.WriteLine(cache["alma"]);
            Console.WriteLine(cache["cica"]);

            Assert.IsFalse(cache.ContainsKey("béka")); // béka was the least recent used

            Assert.AreEqual(2, cache.Count);
            Assert.AreEqual(2, cache.Count());
            Assert.AreEqual(2, cache.Keys.Count());
            Assert.AreEqual(2, cache.Values.Count());
        }

        [TestMethod]
        public void RemoveTest()
        {
            Cache<string, string> cache = new Cache<string, string>(s => s.ToUpperInvariant());
            Console.WriteLine(cache["alma"]);
            Console.WriteLine(cache["béka"]);
            Console.WriteLine(cache["cica"]);
            Console.WriteLine(cache["dinnye"]);
            Console.WriteLine(cache["egér"]);

            // remove middle
            cache.Remove("cica");
            Assert.AreEqual(4, cache.Count);
            Assert.AreEqual(4, cache.Count());
            Assert.AreEqual(4, cache.Keys.Count());
            Assert.AreEqual(4, cache.Values.Count());

            // remove first
            cache.Remove("alma");
            Assert.AreEqual(3, cache.Count);
            Assert.AreEqual(3, cache.Count());
            Assert.AreEqual(3, cache.Keys.Count());
            Assert.AreEqual(3, cache.Values.Count());

            // remove last
            cache.Remove("egér");
            Assert.AreEqual(2, cache.Count);
            Assert.AreEqual(2, cache.Count());
            Assert.AreEqual(2, cache.Keys.Count());
            Assert.AreEqual(2, cache.Values.Count());

            // remove first, when there are 2 elements
            cache.Remove("béka");
            Assert.AreEqual(1, cache.Count);
            Assert.AreEqual(1, cache.Count());
            Assert.AreEqual(1, cache.Keys.Count());
            Assert.AreEqual(1, cache.Values.Count());
        }

        [TestMethod]
        public void TouchTest()
        {
            Cache<string, string> cache = new Cache<string, string>(s => s.ToUpperInvariant()) { Behavior = CacheBehavior.RemoveOldestElement };
            Console.WriteLine(cache["alma"]);
            Console.WriteLine(cache["béka"]);
            Console.WriteLine(cache["cica"]);
            Console.WriteLine(cache["dinnye"]);
            Console.WriteLine(cache["egér"]);

            // touch middle
            cache.Touch("cica");
            Assert.AreEqual(5, cache.Count);
            Assert.AreEqual(5, cache.Count());
            Assert.AreEqual(5, cache.Keys.Count());
            Assert.AreEqual(5, cache.Values.Count());
            Assert.AreEqual("alma", cache.First().Key);
            Assert.AreEqual("cica", cache.Last().Key);

            // touch first
            cache.Touch("alma");
            Assert.AreEqual(5, cache.Count);
            Assert.AreEqual(5, cache.Count());
            Assert.AreEqual(5, cache.Keys.Count());
            Assert.AreEqual(5, cache.Values.Count());
            Assert.AreEqual("béka", cache.First().Key);
            Assert.AreEqual("alma", cache.Last().Key);

            // touch last
            cache.Touch("alma");
            Assert.AreEqual(5, cache.Count);
            Assert.AreEqual(5, cache.Count());
            Assert.AreEqual(5, cache.Keys.Count());
            Assert.AreEqual(5, cache.Values.Count());
            Assert.AreEqual("béka", cache.First().Key);
            Assert.AreEqual("alma", cache.Last().Key);

            cache = new Cache<string, string>(s => s.ToUpperInvariant()) { Behavior = CacheBehavior.RemoveLeastRecentUsedElement };
            Console.WriteLine(cache["alma"]);
            Console.WriteLine(cache["béka"]);

            // touch first, when there are 2 elements
            cache.Touch("alma");
            Assert.AreEqual(2, cache.Count);
            Assert.AreEqual(2, cache.Count());
            Assert.AreEqual(2, cache.Keys.Count());
            Assert.AreEqual(2, cache.Values.Count());
            Assert.AreEqual("béka", cache.First().Key);
            Assert.AreEqual("alma", cache.Last().Key);
        }

        [TestMethod]
        public void KeysValuesTest()
        {
            Cache<string, string> cache = new Cache<string, string>(s => s.ToUpperInvariant());
            Console.WriteLine(cache["alma"]);
            Console.WriteLine(cache["béka"]);
            Console.WriteLine(cache["cica"]);
            Console.WriteLine(cache["dinnye"]);
            Console.WriteLine(cache["egér"]);

            var keys = from c in cache select c.Key;
            Assert.IsTrue(keys.SequenceEqual(cache.Keys));

            var values = from c in cache select c.Value;
            Assert.IsTrue(values.SequenceEqual(cache.Values));
        }
    }
}
