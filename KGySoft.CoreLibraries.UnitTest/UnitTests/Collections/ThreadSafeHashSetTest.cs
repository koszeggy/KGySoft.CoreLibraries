#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeHashSetTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
using System.Linq;
using System.Threading;

using KGySoft.Collections;
using KGySoft.Reflection;
using KGySoft.Threading;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    internal class ThreadSafeHashSetTest
    {
        #region Fields

        private static readonly TimeSpan defaultTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan infiniteTimeout =
#if NET35 || NET40
            TimeSpan.FromMilliseconds(Timeout.Infinite);
#else
            Timeout.InfiniteTimeSpan;
#endif

        private static readonly object[] usageTestSource =
        {
            new object[] { "Default settings", null, HashingStrategy.Auto, defaultTimeout },
            new object[] { "Explicit comparer", ComparerHelper<string>.EqualityComparer, HashingStrategy.Auto, defaultTimeout },
            new object[] { "MOD hashing", null, HashingStrategy.Modulo, defaultTimeout },
            new object[] { "AND hashing", null, HashingStrategy.And, defaultTimeout },
            new object[] { "No merge", null, HashingStrategy.And, infiniteTimeout },
            new object[] { "Immediate merge", null, HashingStrategy.And, TimeSpan.Zero },
            new object[] { "1 ms merge", null, HashingStrategy.And, TimeSpan.FromMilliseconds(1) },
        };

        #endregion

        #region Methods

        [TestCase(false)]
        [TestCase(true)]
        public void FixedSizeStorageUsageTest(bool ignoreCase)
        {
            Assert.IsTrue(ThreadSafeHashSet<string>.FixedSizeStorage.TryInitialize(
                    new HashSet<string> { "alpha", "beta", "gamma" },
                    default, ignoreCase ? StringComparer.OrdinalIgnoreCase : ComparerHelper<string>.EqualityComparer, out var set));

            Assert.AreEqual(3, set.Count);

            // Contains
            Assert.IsTrue(set.Contains("alpha"));
            Assert.IsFalse(set.Contains("delta"));

            // Add
            Assert.IsFalse(set.Add("alpha"));
            Assert.IsFalse(set.Add("delta"));
            Assert.AreEqual(3, set.Count);

            // TryGetValue
            string equalValue = new String("alpha".ToCharArray());
            Assert.AreNotSame("alpha", equalValue);
            Assert.IsTrue(set.TryGetValue(equalValue, out string actualValue));
            Assert.AreEqual(equalValue, actualValue);
            Assert.AreNotSame(equalValue, actualValue);
            Assert.IsFalse(set.TryGetValue("delta", out actualValue));
            Assert.IsNull(actualValue);

            // Remove
            Assert.IsTrue(set.Remove("alpha"));
            Assert.AreEqual(2, set.Count);
            Assert.IsFalse(set.Remove("alpha"));
            Assert.IsTrue(set.Remove("beta"));
            Assert.AreEqual(1, set.Count);

            // Re-add
            Assert.IsTrue(set.Add("alpha"));
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains("alpha"));

            // Clear
            set.Clear();
            Assert.AreEqual(0, set.Count);
            Assert.IsTrue(set.Add("alpha"));
            Assert.AreEqual(1, set.Count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TempStorageUsageTest(bool ignoreCase)
        {
            var set = new ThreadSafeHashSet<string>.TempStorage(2, ignoreCase ? StringComparer.OrdinalIgnoreCase : ComparerHelper<string>.EqualityComparer, default);

            // Add
            Assert.IsTrue(set.Add("alpha"));
            Assert.IsTrue(set.Add("beta"));
            Assert.IsTrue(set.Add("gamma")); // here resize is needed
            Assert.AreEqual(3, set.Count);
            Assert.IsFalse(set.Add("alpha"));
            Assert.AreEqual(3, set.Count);

            // Contains
            Assert.IsTrue(set.Contains("alpha"));
            Assert.IsFalse(set.Contains("delta"));

            // TryGetValue
            string equalValue = new String("alpha".ToCharArray());
            Assert.AreNotSame("alpha", equalValue);
            Assert.IsTrue(set.TryGetValue(equalValue, out string actualValue));
            Assert.AreEqual(equalValue, actualValue);
            Assert.AreNotSame(equalValue, actualValue);
            Assert.IsFalse(set.TryGetValue("delta", out actualValue));
            Assert.IsNull(actualValue);

            // Remove
            Assert.IsTrue(set.Remove("alpha"));
            Assert.AreEqual(2, set.Count);
            Assert.IsFalse(set.Remove("alpha"));
            Assert.IsTrue(set.Remove("beta"));
            Assert.AreEqual(1, set.Count);

            // Re-add
            Assert.IsTrue(set.Add("alpha"));
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains("alpha"));
        }

        [TestCaseSource(nameof(usageTestSource))]
        public void UsageTest(string testName, IEqualityComparer<string> comparer, HashingStrategy strategy, TimeSpan mergeInterval)
        {
            Console.WriteLine(testName);

            string[] initialValues = { "alpha", "beta", "gamma" };

            var set = new ThreadSafeHashSet<string>(initialValues, comparer, strategy) { MergeInterval = mergeInterval };

            Assert.AreEqual(3, set.Count);

            // Contains
            Assert.IsTrue(set.Contains("alpha"));
            Assert.IsFalse(set.Contains("delta"));

            // Add
            Assert.IsFalse(set.Add("alpha"));
            Assert.IsTrue(set.Add("delta")); // to locking
            Assert.AreEqual(4, set.Count);

            // TryGetValue
            string equalValue = new String("alpha".ToCharArray());
            Assert.AreNotSame("alpha", equalValue);
            Assert.IsTrue(set.TryGetValue(equalValue, out string actualValue)); // in non-locking
            Assert.AreEqual(equalValue, actualValue);
            Assert.AreNotSame(equalValue, actualValue);
            Assert.IsTrue(set.TryGetValue("delta", out actualValue)); // maybe in locking
            Assert.AreSame("delta", actualValue);
            Assert.IsFalse(set.TryGetValue("epsilon", out actualValue));
            Assert.IsNull(actualValue);

            // Remove
            Assert.IsTrue(set.Remove("alpha"));
            Assert.AreEqual(3, set.Count);
            Assert.IsFalse(set.Remove("alpha"));
            Assert.IsTrue(set.Remove("delta")); // maybe from locking
            Assert.AreEqual(2, set.Count);

            // Re-add
            Assert.IsTrue(set.Add("alpha")); // in non-locking
            Assert.AreEqual(3, set.Count);
            Assert.IsTrue(set.Contains("alpha"));
            Assert.IsTrue(set.Add("delta")); // maybe in locking unless already merged
            Assert.AreEqual(4, set.Count);
            Assert.IsTrue(set.Contains("delta"));

            // Clear
            set.Clear();
            Assert.AreEqual(0, set.Count);
            Assert.IsTrue(set.Add("alpha")); // in non-locking, even after clear
            Assert.IsTrue(set.Contains("alpha"));
            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Add("delta")); // maybe in locking unless already merged
            Assert.IsTrue(set.Contains("delta"));
            Assert.AreEqual(2, set.Count);

            // Forcing merge
            set.EnsureMerged();
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains("alpha"));
            Assert.IsTrue(set.Contains("delta"));
        }

        [Test]
        public void InitFromCollectionTest()
        {
            string[] array = { "alpha", "beta", "gamma", null };

            // initializing as collection
            Assert.IsTrue(array.SequenceEqual(new ThreadSafeHashSet<string>(array)));

            // initializing as enumerable
            Assert.IsTrue(array.SequenceEqual(new ThreadSafeHashSet<string>(array.Where(_ => true))));
        }

        [Test]
        public void CopyToTest()
        {
            var set = new ThreadSafeHashSet<string> { "alpha", "beta", "gamma", null };

            var arr = new string[set.Count];
            ((ICollection<string>)set).CopyTo(arr, 0);
            Assert.IsTrue(arr.SequenceEqual(set));
        }

        [Test]
        public void ToArrayTest()
        {
            var set = new ThreadSafeHashSet<string> { "alpha", "beta", "gamma", null };
            var tSet = new ThreadSafeHashSet<string>(set);

            Assert.IsTrue(set.SequenceEqual(tSet));
            Assert.IsTrue(set.ToArray().SequenceEqual(tSet.ToArray()));

            tSet.Remove("alpha");
            Assert.AreEqual(3, tSet.Count);
            Assert.AreEqual(3, tSet.ToArray().Length);
        }

#if !NETFRAMEWORK
        [Obsolete]
#endif
        [Test]
        public void SerializationTest()
        {
            var set = new ThreadSafeHashSet<string> { "alpha", "beta", "gamma", null };

            ThreadSafeHashSet<string> clone = set.DeepClone(false);
            Assert.IsTrue(set.SequenceEqual(clone));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreserveMergedItemsTest(bool preserveMergedKeys)
        {
            const int count = 100;
            var set = new ThreadSafeHashSet<int>(Enumerable.Range(0, count))
            {
                MergeInterval = infiniteTimeout,
                PreserveMergedItems = preserveMergedKeys
            };

            set.EnsureMerged();

            // removing half of the elements + 1 from the lock free storage
            for (int i = 0; i < count / 2 + 1; i++)
                Assert.IsTrue(set.Remove(i));

            // adding an element to the locking storage so there will be something to merge
            set.Add(-1);

            // performing a new merge with 51 deleted items
            set.EnsureMerged();

            Assert.AreEqual(count / 2, set.Count);
            Assert.AreEqual(preserveMergedKeys ? count / 2 + 1 : 0, ((ThreadSafeHashSet<int>.FixedSizeStorage)Reflector.GetField(set, "fixedSizeStorage"))!.DeletedCount);
            set.TrimExcess();
            Assert.AreEqual(0, ((ThreadSafeHashSet<int>.FixedSizeStorage)Reflector.GetField(set, "fixedSizeStorage"))!.DeletedCount);
            Assert.AreEqual(count / 2, set.Count);
        }

        [Test]
        public void RaceConditionTest()
        {
            const int key = 1;

            // Initializing without passing a collection to the constructor and then adding one element: it will be in the temp locking storage
            var set = new ThreadSafeHashSet<int> { key };

            // Waiting for the merge timeout
            Thread.Sleep(set.MergeInterval);

            bool[] results = new bool[100];
            ParallelHelper.For(0, results.Length,
                y => results[y] = set.TryRemove(key));

            Assert.AreEqual(1, results.Count(r => r));
        }

        #endregion
    }
}
