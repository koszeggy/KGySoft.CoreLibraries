#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GrowOnlyDictionaryTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Threading.Tasks;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class GrowOnlyDictionaryTest
    {
        #region Methods

        [Test]
        public void UsageTest()
        {
            var dict = new GrowOnlyDictionary<int, object>(16, null, true);
            Assert.AreEqual(0, dict.Count);

            // Adding new
            Assert.IsTrue(dict.TryAdd(0, 0));
            Assert.IsTrue(dict.TryGetValue(0, out object value));
            Assert.AreEqual(0, value);
            Assert.AreEqual(1, dict.Count);

            // Cannot add it again
            Assert.IsFalse(dict.TryAdd(0, 0));
            Assert.AreEqual(0, dict[0]);
            Assert.AreEqual(1, dict.Count);

            // And cannot overwrite it with another value
            Assert.IsFalse(dict.TryAdd(0, 1));
            Assert.AreEqual(0, dict[0]);
            Assert.AreEqual(1, dict.Count);

            // But can add another value
            dict[1] = 1;
            Assert.AreEqual(1, dict[1]);
            Assert.AreEqual(2, dict.Count);

            // Adding a linked item in an existing bucket
            dict[16] = 16;
            Assert.AreEqual(16, dict[16]);
            Assert.AreEqual(3, dict.Count);
        }

        [Test]
        public void ParallelUsageTest()
        {
            const int capacity = 1024;
            const int count = 10_000;
            var dict = new GrowOnlyDictionary<int, object>(capacity, null, true);
            Parallel.For(0, count, i => Assert.IsTrue(dict.TryAdd(i, i)));

            Assert.AreEqual(count, dict.Count);
            Parallel.For(0, count, i => Assert.AreEqual(i, dict[i]));
        }

        #endregion
    }
}
