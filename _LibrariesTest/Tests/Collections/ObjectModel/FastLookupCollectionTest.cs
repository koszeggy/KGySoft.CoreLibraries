using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KGySoft.Collections;
using KGySoft.Collections.ObjectModel;
using KGySoft.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Tests.Collections.ObjectModel
{
    [TestClass]
    public class FastLookupCollectionTest : TestBase
    {
        [TestMethod]
        public void Construction()
        {
            AssertConsistency(new FastLookupCollection<int>());
            AssertConsistency(new FastLookupCollection<int>(new List<int>{1, 2, 3, 4, 5}));
            AssertConsistency(new FastLookupCollection<int>(new List<int>{1, 1, 2, 2, 1}));
            AssertConsistency(new FastLookupCollection<string>(new List<string>{null, null, "1", "2", "1"}));
        }

        [TestMethod]
        public void AddExplicit()
        {
            var coll = new FastLookupCollection<int>();
            coll.Add(1);
            coll.Add(2);
            coll.Add(1);
            coll.Insert(0, 1);
            AssertConsistency(coll);
        }

        [TestMethod]
        public void SetExplicit()
        {
            var coll = new FastLookupCollection<int>(new[] { 1, 2, 3, 2, 1 }) { CheckConsistency = false };
            coll[1] = 1;
            coll[4] = 2;
            AssertConsistency(coll);
        }

        [TestMethod]
        public void RemoveExplicit()
        {
            var coll = new FastLookupCollection<int>(new List<int> { 1, 2, 3, 2, 1 }) { CheckConsistency = false };
            coll.Remove(1); // first 1
            coll.RemoveAt(1); // 3
            coll.Remove(1); // last 1
            AssertConsistency(coll);
        }

        [TestMethod]
        public void AddInner()
        {
            var inner = new List<string>();
            var coll = new FastLookupCollection<string>(inner) { CheckConsistency = false };
            inner.Add("a");
            Throws<AssertFailedException>(() => AssertConsistency(coll));
            coll.CheckConsistency = true;
            coll.Insert(0, "b");
            AssertConsistency(coll);
        }

        [TestMethod]
        public void SetInner()
        {
            var inner = new List<string> { "1", "2", "3", "2", "1"};
            var coll = new FastLookupCollection<string>(inner) { CheckConsistency = false };
            inner[2] = null;
            Throws<AssertFailedException>(() => AssertConsistency(coll));
            coll.CheckConsistency = true;
            coll[2] = "x";
            AssertConsistency(coll);
        }

        [TestMethod]
        public void RemoveInner()
        {
            var inner = new List<string> { "1", "2", "3", "2", "1" };
            var coll = new FastLookupCollection<string>(inner) { CheckConsistency = false };
            inner.RemoveAt(2);
            Throws<AssertFailedException>(() => AssertConsistency(coll));
            coll.CheckConsistency = true;
            coll.RemoveAt(0);
            AssertConsistency(coll);
        }

        private void AssertConsistency<T>(FastLookupCollection<T> coll)
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

            var actualItemToIndex = (AllowNullDictionary<T, CircularList<int>>)Reflector.GetInstanceFieldByName(coll, "itemToIndex");
            AssertItemsEqual(Sorted(itemToIndex), Sorted(actualItemToIndex));

            IEnumerable Sorted(AllowNullDictionary<T, CircularList<int>> dict) 
                => new AllowNullDictionary<T, CircularList<int>>(dict.OrderBy(item => item.Key));
        }
    }
}
