#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CircularSortedListTest.cs
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

using System.Collections.Generic;
using System.Linq;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class CircularSortedListTest
    {
        #region Methods

        [Test]
        public void Construction()
        {
            // construction from ICollection<int> (Count is known)
            Dictionary<int, int> dict = new Dictionary<int, int> { { 1, 2 }, { 13, 14 }, { 5, 6 } };
            Assert.IsTrue(new CircularSortedList<int, int>(dict).SequenceEqual(new SortedList<int, int>(dict)));
        }

        [Test]
        public void Populate()
        {
            CircularSortedList<int, int> cslist = new CircularSortedList<int, int>();

            // first element
            Assert.AreEqual(0, cslist.Add(1, 1));

            // 1 element, add to last
            Assert.AreEqual(1, cslist.Add(6, 6));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 1, 6 }));

            // 1 element, add before last
            cslist.Remove(1);
            Assert.AreEqual(0, cslist.Add(1, 1));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 1, 6 }));

            // 2 elements, add to head
            Assert.AreEqual(0, cslist.Add(0, 0));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 0, 1, 6 }));

            // 2 elements, add to middle
            cslist.Remove(1);
            Assert.AreEqual(1, cslist.Add(3, 3));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 0, 3, 6 }));

            // 3 elements, add after middle
            Assert.AreEqual(2, cslist.Add(4, 4));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 0, 3, 4, 6 }));

            // 3 elements, add before middle
            cslist.Remove(4);
            Assert.AreEqual(1, cslist.Add(2, 2));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 0, 2, 3, 6 }));

            // > 3 elements, add after the middle
            Assert.AreEqual(3, cslist.Add(5, 5));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 0, 2, 3, 5, 6 }));

            // > 3 elements, add before the middle
            Assert.AreEqual(1, cslist.Add(1, 1));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 0, 1, 2, 3, 5, 6 }));

            // > 3 elements, add into the middle
            Assert.AreEqual(4, cslist.Add(4, 4));
            Assert.IsTrue(cslist.Keys.SequenceEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }));
        }

        #endregion
    }
}
