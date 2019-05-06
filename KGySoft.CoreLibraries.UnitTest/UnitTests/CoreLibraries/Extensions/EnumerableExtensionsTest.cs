#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumerableExtensionsTest.cs
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class EnumerableExtensionsTest
    {
        #region Methods

        [Test]
        public void TryAddRangeTest()
        {
            var list = new List<int?>();
            IEnumerable e = list;

            // can call CollectionExtension.AddRange
            Assert.IsTrue(e.TryAddRange(new int?[] { 1, 2, null }));
            Assert.IsTrue(list.SequenceEqual(new int?[] { 1, 2, null }));

            // can only add one by one (different coll type)
            list.Clear();
            Assert.IsTrue(e.TryAddRange(new[] { 1, 2, 3 }));
            Assert.IsTrue(list.SequenceEqual(new int?[] { 1, 2, 3 }));
        }

        [Test]
        public void TryInsertRangeTest()
        {
            var list = new List<int?>() { 0, 100 };
            IEnumerable e = new List<int?>(list);

            // can call CollectionExtension.InsertRange
            Assert.IsTrue(e.TryInsertRange(1, new int?[] { 1, 2, null }));
            Assert.IsTrue(e.Cast<int?>().SequenceEqual(new int?[] { 0, 1, 2, null, 100 }));

            // can only insert one by one (different coll type)
            e = new List<int?>(list);
            Assert.IsTrue(e.TryInsertRange(1, new[] { 1, 2, 3 }));
            Assert.IsTrue(e.Cast<int?>().SequenceEqual(new int?[] { 0, 1, 2, 3, 100 }));
        }

        [Test]
        public void TryRemoveRangeTest()
        {
            var list = new List<int?>() { null, 1, 2, 3, 4, 5 };
            IEnumerable e = new List<int?>(list);

            // can call CollectionExtension.RemoveRange
            Assert.IsTrue(e.TryRemoveRange(1, 2));
            Assert.IsTrue(e.Cast<int?>().SequenceEqual(new int?[] { null, 3, 4, 5 }));

            // can only remove one by one (non-generic)
            e = new ArrayList(list);
            Assert.IsTrue(e.TryRemoveRange(1, 2));
            Assert.IsTrue(e.Cast<int?>().SequenceEqual(new int?[] { null, 3, 4, 5 }));
        }

        [Test]
        public void TryReplaceRangeTest()
        {
            var list = new List<int?>() { null, 1, 2, 3, 4, 5 };
            IEnumerable e = new List<int?>(list);

            // can call CollectionExtension.RemoveRange
            Assert.IsTrue(e.TryReplaceRange(1, 2, new int?[] { -1, -2, -3 }));
            Assert.IsTrue(e.Cast<int?>().SequenceEqual(new int?[] { null, -1, -2, -3, 3, 4, 5 }));

            // can only replace one by one (different coll type) - same amount
            e = new List<int?>(list);
            Assert.IsTrue(e.TryReplaceRange(1, 2, new[] { -1, -2 }));
            Assert.IsTrue(e.Cast<int?>().SequenceEqual(new int?[] { null, -1, -2, 3, 4, 5 }));

            // can only replace one by one (different coll type) - more to remove
            e = new List<int?>(list);
            Assert.IsTrue(e.TryReplaceRange(1, 2, new[] { -1 }));
            Assert.IsTrue(e.Cast<int?>().SequenceEqual(new int?[] { null, -1, 3, 4, 5 }));

            // can only replace one by one (different coll type) - more to add
            e = new List<int?>(list);
            Assert.IsTrue(e.TryReplaceRange(1, 2, new[] { -1, -2, -3 }));
            Assert.IsTrue(e.Cast<int?>().SequenceEqual(new int?[] { null, -1, -2, -3, 3, 4, 5 }));
        }

        #endregion
    }
}
