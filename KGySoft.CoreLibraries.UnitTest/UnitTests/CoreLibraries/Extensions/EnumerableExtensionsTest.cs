#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumerableExtensionsTest.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using KGySoft.Collections;

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

        [Test]
        public void IsNullOrEmptyTest()
        {
            Assert.IsTrue("".IsNullOrEmpty());
            Assert.IsFalse("alpha".IsNullOrEmpty());
            Assert.IsTrue("alpha".Where(Char.IsDigit).IsNullOrEmpty());
            Assert.IsFalse("alpha".Where(Char.IsLetter).IsNullOrEmpty());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(",")]
        [TestCase(", ")]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration", Justification = "Intended")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "ReSharper")]
        public void JoinTest(string separator)
        {
            IEnumerable<string> values = Enumerable.Range(1, 10).Select(i => i.ToString());
            string expected = String.Join(separator, values.ToArray());

            Assert.AreEqual(expected, values.Join(separator));
            Assert.AreEqual(expected, values.ToArray().Join(separator));
            Assert.AreEqual(expected, values.ToList().Join(separator));

            if (separator?.Length == 1)
            {
                Assert.AreEqual(expected, values.Join(separator[0]));
                Assert.AreEqual(expected, values.ToArray().Join(separator[0]));
                Assert.AreEqual(expected, values.ToList().Join(separator[0]));
            }
        }

        [Test]
        public void ShuffleTest()
        {
            int[] numbers = Enumerable.Range(1, 100).ToArray();
            int[] shuffled = numbers.Shuffle().ToArray();
            CollectionAssert.AreNotEqual(numbers, shuffled);
            CollectionAssert.AreEquivalent(numbers, shuffled);
            CollectionAssert.AreEqual(numbers, shuffled.OrderBy(i => i));
        }

        [Test]
        public void TryGetCountTest()
        {
            // ICollection<T>
            Assert.IsTrue(new int[1].TryGetCount(out int count));
            Assert.AreEqual(1, count);

            // IReadOnlyCollection<T>
            Assert.IsTrue(new Queue<int>(new int[2]).TryGetCount(out count));
            Assert.AreEqual(2, count);

            // ICollection
            Assert.IsTrue(new ArrayList { 1, 2, 3 }.TryGetCount(out count));
            Assert.AreEqual(3, count);

#if !NETFRAMEWORK
            // IIListProvider<T>
            Assert.IsTrue(new int[5].Select(c => (byte)c).TryGetCount(out count));
            Assert.AreEqual(5, count);
#endif

            // ICollection<T> that is not ICollection via non-generic access
            Assert.IsTrue(((IEnumerable)new LockingCollection<int>(new int[8])).TryGetCount(out count));
            Assert.AreEqual(8, count);

#if !NETFRAMEWORK
            // IIListProvider<T> via non-generic access
            Assert.IsTrue(((IEnumerable)new int[13].Select(c => (byte)c)).TryGetCount(out count));
            Assert.AreEqual(13, count);
#endif

            // Cannot be determined without counting
            Assert.IsFalse(Enumerable.Range(0, 10).Where(n => n > 1).TryGetCount(out count));
            Assert.AreEqual(0, count);
        }

        #endregion
    }
}
