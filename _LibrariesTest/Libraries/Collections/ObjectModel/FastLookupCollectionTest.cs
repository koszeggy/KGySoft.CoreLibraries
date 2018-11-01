using System;
using System.Collections.Generic;
using KGySoft.Collections;
using KGySoft.Collections.ObjectModel;
using KGySoft.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Collections.ObjectModel
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
            var coll = new FastLookupCollection<int> { CheckConsistency = false };
            coll.Add(1);
            coll.Add(2);
            coll.Add(1);
            coll.Insert(0, 1);
            AssertConsistency(coll);
        }

        private void AssertConsistency<T>(FastLookupCollection<T> coll)
        {
            var nullToIndex = new CircularList<int>();
            var itemToIndex = new Dictionary<T, CircularList<int>>();
            for (int i = 0; i < coll.Count; i++)
            {
                T item = coll[i];
                if (item == null)
                    nullToIndex.Add(i);
                else
                {
                    if (!itemToIndex.TryGetValue(item, out var indices))
                    {
                        indices = new CircularList<int>();
                        itemToIndex[item] = indices;
                    }

                    indices.Add(i);
                }
            }

            var actualNullToIndex = (CircularList<int>)Reflector.GetInstanceFieldByName(coll, "nullToIndex") ?? new CircularList<int>();
            AssertItemsEqual(nullToIndex, actualNullToIndex);
            var actualItemToIndex = (Dictionary<T, CircularList<int>>)Reflector.GetInstanceFieldByName(coll, "itemToIndex");
            AssertItemsEqual(itemToIndex, actualItemToIndex);
        }
    }
}
