#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeDictionaryTest.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.Reflection;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class ThreadSafeDictionaryTest
    {
        #region Fields

        private static TimeSpan defaultTimeout = TimeSpan.FromMilliseconds(100);
        private static TimeSpan infiniteTimeout =
#if NET35 || NET40
            TimeSpan.FromMilliseconds(Timeout.Infinite);
#else
            Timeout.InfiniteTimeSpan;
#endif

        private static object[] usageTestSource =
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
        public void LockFreeStorageUsageTest(bool ignoreCase)
        {
            var dict = new ThreadSafeDictionary<string, int>.LockFreeStorage(default, new Dictionary<string, int>
            {
                ["alpha"] = 1,
                ["beta"] = 2,
                ["gamma"] = 3,
            }, ignoreCase ? StringComparer.OrdinalIgnoreCase : null);

            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // TryAdd
            Assert.IsFalse(dict.TryAdd("alpha", 11));
            Assert.IsFalse(dict.TryAdd("delta", 4));
            Assert.AreEqual(3, dict.Count);

            // Update
            dict["alpha"] = 11;
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(11, dict["alpha"]);

            // Remove
            Assert.IsTrue(dict.TryRemove("alpha", out int value));
            Assert.AreEqual(11, value);
            Assert.AreEqual(2, dict.Count);
            Assert.IsFalse(dict.TryRemove("alpha", out value));
            Assert.AreEqual(0, value);
            Assert.IsTrue(dict.Remove("beta"));

            // Re-add
            Assert.IsTrue(dict.TryAdd("alpha", -1));
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(-1, dict["alpha"]);

            // Replace
            Assert.IsFalse(dict.TryReplace("alpha", 1, 11));
            Assert.IsTrue(dict.TryReplace("alpha", 1, -1));
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // Clear
            dict.Clear();
            Assert.AreEqual(0, dict.Count);
            Assert.IsTrue(dict.TryAdd("alpha", 42));
            Assert.AreEqual(42, dict["alpha"]);
            Assert.AreEqual(1, dict.Count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LockingStorageUsageTest(bool ignoreCase)
        {
            var dict = new ThreadSafeDictionary<string, int>.LockingStorage(2, ignoreCase ? StringComparer.OrdinalIgnoreCase : null, default);

            // Add
            dict.Add("alpha", 1);
            dict.Add("beta", 2);
            dict.Add("gamma", 3); // here resize is needed
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // TryAdd
            Assert.IsFalse(dict.TryAdd("alpha", 11));
            Assert.IsTrue(dict.TryAdd("delta", 4));
            Assert.AreEqual(4, dict.Count);

            // Update
            dict["alpha"] = 11;
            Assert.AreEqual(4, dict.Count);
            Assert.AreEqual(11, dict["alpha"]);

            // Remove
            Assert.IsTrue(dict.TryRemove("alpha", out int value));
            Assert.AreEqual(11, value);
            Assert.AreEqual(3, dict.Count);
            Assert.IsFalse(dict.TryRemove("alpha", out value));
            Assert.AreEqual(0, value);
            Assert.IsTrue(dict.Remove("beta"));

            // Re-add
            dict.Add("alpha", -1);
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(-1, dict["alpha"]);

            // Replace
            Assert.IsFalse(dict.TryReplace("alpha", 1, 11));
            Assert.IsTrue(dict.TryReplace("alpha", 1, -1));
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // Clear
            dict.Clear();
            Assert.AreEqual(0, dict.Count);
            dict.Add("alpha", 42);
            Assert.AreEqual(42, dict["alpha"]);
            Assert.AreEqual(1, dict.Count);
        }

        [TestCaseSource(nameof(usageTestSource))]
        public void UsageTest(string testName, IEqualityComparer<string> comparer, int strategy, TimeSpan mergeInterval)
        {
            Console.WriteLine(testName);

            var initialValues = new Dictionary<string, int>
            {
                ["alpha"] = 1,
                ["beta"] = 2,
                ["gamma"] = 3,
            };

            var dict = new ThreadSafeDictionary<string, int>(initialValues, comparer, (HashingStrategy)strategy) { MergeInterval = mergeInterval };

            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // TryAdd
            Assert.IsFalse(dict.TryAdd("alpha", 11));
            Assert.IsTrue(dict.TryAdd("delta", 4)); // to locking
            Assert.AreEqual(4, dict.Count);

            // Update
            dict["alpha"] = 11; // in non-locking
            Assert.AreEqual(4, dict.Count);
            Assert.AreEqual(11, dict["alpha"]);
            dict["delta"] = 44; // maybe in locking
            Assert.AreEqual(4, dict.Count);
            Assert.AreEqual(44, dict["delta"]);

            // Remove
            Assert.IsTrue(dict.TryRemove("alpha", out int value));
            Assert.AreEqual(11, value);
            Assert.AreEqual(3, dict.Count);
            Assert.IsFalse(dict.TryRemove("alpha", out value));
            Assert.AreEqual(0, value);
            Assert.IsTrue(dict.Remove("delta")); // maybe from locking
            Assert.AreEqual(2, dict.Count);

            // Re-add
            Assert.IsTrue(dict.TryAdd("alpha", -1)); // in non-locking
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(-1, dict["alpha"]);
            Assert.IsTrue(dict.TryAdd("delta", -4)); // maybe in locking unless already merged
            Assert.AreEqual(4, dict.Count);
            Assert.AreEqual(-4, dict["delta"]);

            // Replace
            Assert.IsFalse(dict.TryUpdate("alpha", 1, 11));
            Assert.IsTrue(dict.TryUpdate("alpha", 1, -1)); // in non-locking
            Assert.AreEqual(4, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);
            Assert.IsTrue(dict.TryUpdate("delta", 4, -4)); // maybe in locking
            Assert.AreEqual(4, dict.Count);
            Assert.AreEqual(4, dict["delta"]);

            // Clear
            dict.Clear();
            Assert.AreEqual(0, dict.Count);
            Assert.IsTrue(dict.TryAdd("alpha", 42)); // in non-locking, even after clear
            Assert.AreEqual(42, dict["alpha"]);
            Assert.AreEqual(1, dict.Count);
            Assert.IsTrue(dict.TryAdd("delta", 13)); // maybe in locking unless already merged
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(13, dict["delta"]);

            // Forcing merge
            dict.EnsureMerged();
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(42, dict["alpha"]);
            Assert.AreEqual(13, dict["delta"]);
        }

        #endregion
    }
}
