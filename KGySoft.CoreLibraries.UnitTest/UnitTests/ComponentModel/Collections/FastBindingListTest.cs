#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastBindingListTest.cs
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
using System.Collections.ObjectModel;

using KGySoft.ComponentModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel.Collections
{
    [TestFixture]
    public class FastBindingListTest : TestBase
    {
        #region Methods

        [Test]
        public void AllowNewDefault()
        {
            Assert.IsTrue(new FastBindingList<int>().AllowNew);
            Assert.IsFalse(new FastBindingList<int>(new ReadOnlyCollection<int>(new int[0])).AllowNew);
            Assert.IsFalse(new FastBindingList<string>().AllowNew);
        }

        [Test]
        public void Find()
        {
            var coll = new FastBindingList<KeyValuePair<int, string>> { new KeyValuePair<int, string>(1, "1"), new KeyValuePair<int, string>(2, "2") };

            Throws<ArgumentException>(() => coll.Find("X", null), "No property descriptor found for property name 'X' in type 'System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]'.");
            Assert.AreEqual(-1, coll.Find(nameof(KeyValuePair<_, _>.Key), 0));
            Assert.AreEqual(0, coll.Find(nameof(KeyValuePair<_, _>.Key), 1));
        }

        #endregion
    }
}
