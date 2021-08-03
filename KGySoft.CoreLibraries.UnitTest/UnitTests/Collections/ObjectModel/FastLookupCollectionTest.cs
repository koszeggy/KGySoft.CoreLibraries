#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastLookupCollectionTest.cs
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

using System.Collections;
using System.Linq;

using KGySoft.Collections;
using KGySoft.Collections.ObjectModel;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections.ObjectModel
{
    [TestFixture]
    public class FastLookupCollectionTest : TestBase
    {
        #region Methods

        #region Static Methods

        private static void AssertConsistency<T>(FastLookupCollection<T> coll, bool expectNull = false)
        {
            var actualItemToIndex = (AllowNullDictionary<T, int>)Reflector.GetField(coll, "itemToIndex");
            if (expectNull)
            {
                Assert.IsNull(actualItemToIndex);
                return;
            }

            Assert.IsNotNull(actualItemToIndex);
            var itemToIndex = new AllowNullDictionary<T, int>();
            for (int i = 0; i < coll.Count; i++)
            {
                T item = coll[i];
                if (!itemToIndex.TryGetValue(item, out int index))
                    itemToIndex[item] = i;
                else if (index >= 0)
                    itemToIndex[item] = ~index;
            }

            AssertItemsEqual(Sorted(itemToIndex), Sorted(actualItemToIndex));

            IEnumerable Sorted(AllowNullDictionary<T, int> dict)
                => new AllowNullDictionary<T, int>(dict.OrderBy(item => item.Key));
        }

        #endregion

        #region Instance Methods

        [Test]
        public void UsageTest()
        {
            var coll = new FastLookupCollection<int> { CheckConsistency = true };
            coll.Add(1);
            Assert.AreEqual(0, coll.IndexOf(1));

            coll.Add(2);
            Assert.AreEqual(1, coll.IndexOf(2));

            // adding a duplicate: still the first index is returned
            coll.Add(1);
            Assert.AreEqual(0, coll.IndexOf(1));

            // inserting at the first position
            coll.Insert(0, 1);
            Assert.AreEqual(0, coll.IndexOf(1));
            Assert.AreEqual(2, coll.IndexOf(2));
            AssertConsistency(coll);

            // non existing
            Assert.AreEqual(-1, coll.IndexOf(0));

            // adding and removing unique at the end
            coll.Add(100);
            AssertConsistency(coll);
            Assert.AreEqual(coll.Count - 1, coll.IndexOf(100));
            coll.Remove(100);
            AssertConsistency(coll);
            Assert.AreEqual(-1, coll.IndexOf(100));

            // inserting and removing clears the mapping, IndexOf rebuilds it
            coll.Insert(0, -100);
            AssertConsistency(coll, true);
            Assert.AreEqual(0, coll.IndexOf(-100));
            AssertConsistency(coll);
            coll.Remove(-100);
            AssertConsistency(coll, true);
            Assert.AreEqual(-1, coll.IndexOf(-100));
            AssertConsistency(coll);

            // setting a duplicate clears the mapping, IndexOf rebuilds it
            coll[0] = -1;
            AssertConsistency(coll, true);
            Assert.AreEqual(0, coll.IndexOf(-1));
            AssertConsistency(coll);

            // removing a duplicate at the end clears the mapping
            coll.RemoveAt(coll.Count - 1);
            AssertConsistency(coll, true);
        }

        #endregion

        #endregion
    }
}
