#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringKeyedDictionaryTest.cs
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

using System;
using System.Linq;

using KGySoft.Collections;
using KGySoft.Reflection;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class StringKeyedDictionaryTest
    {
        #region Methods

        [TestCase(null)]
        [TestCase(StringComparison.Ordinal)]
        [TestCase(StringComparison.OrdinalIgnoreCase)]
        [TestCase(StringComparison.InvariantCulture)]
        [TestCase(StringComparison.InvariantCultureIgnoreCase)]
        [TestCase(StringComparison.CurrentCulture)]
        [TestCase(StringComparison.CurrentCultureIgnoreCase)]
        public void UsageTest(StringComparison? comparison)
        {
            var dict = comparison == null ? new StringKeyedDictionary<int>() : new StringKeyedDictionary<int>(StringSegmentComparer.FromComparison(comparison.Value));
            dict.Add("alpha", 1);
            dict.Add("beta", 2);
            dict.Add("gamma", 3);
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);
            // ... TODO: by others

            // remove and re-add by string
            Assert.IsTrue(dict.Remove("alpha"));
            Assert.AreEqual(2, dict.Count);
            Assert.IsFalse(dict.Remove("alpha"));
            Assert.IsTrue(dict.Remove("beta"));
            dict.Add("alpha", -1);
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(-1, dict["alpha"]);
        }

// TODO:
//        [Test]
//        public void KeysValuesTest()
//        {
//            var cache = new Cache<string, string>(s => s.ToUpperInvariant());
//            Console.WriteLine(cache["alpha"]);
//            Console.WriteLine(cache["beta"]);
//            Console.WriteLine(cache["gamma"]);
//            Console.WriteLine(cache["delta"]);
//            Console.WriteLine(cache["epsilon"]);

//            var keys = cache.Select(c => c.Key);
//            Assert.IsTrue(keys.SequenceEqual(cache.Keys));
//            var values = cache.Select(c => c.Value);
//            Assert.IsTrue(values.SequenceEqual(cache.Values));

//            Assert.IsTrue(cache.Remove("beta"));
//            keys = cache.Select(c => c.Key);
//            Assert.IsTrue(keys.SequenceEqual(cache.Keys));
//            values = cache.Select(c => c.Value);
//            Assert.IsTrue(values.SequenceEqual(cache.Values));
//        }

//        [Test]
//        public void SerializationTest()
//        {
//#if NETCOREAPP2_0 || NETCOREAPP3_0
//            // .NET Core 2.0/3.0 does not support delegate serialization
//            var cache = new Cache<string, int>(StringComparer.OrdinalIgnoreCase)
//            {
//                EnsureCapacity = true,
//                Behavior = CacheBehavior.RemoveOldestElement,
//                DisposeDroppedValues = true
//            };
//            cache["One"] = 1;
//            cache["Two"] = 2;
//            cache["Three"] = 3;

//            var cacheCopy = cache.DeepClone();
//            Assert.AreNotSame(cache, cacheCopy);
//            Assert.AreEqual(cache.Count, cacheCopy.Count);
//            Assert.AreEqual(cache.Capacity, cacheCopy.Capacity);
//            Assert.AreEqual(cache.Behavior, cacheCopy.Behavior);
//            Assert.AreEqual(cache.DisposeDroppedValues, cacheCopy.DisposeDroppedValues);
//            Assert.AreEqual(cache.EnsureCapacity, cacheCopy.EnsureCapacity);

//            Assert.IsTrue(cache.SequenceEqual(cacheCopy));
//            Assert.IsTrue(cache.Keys.SequenceEqual(cacheCopy.Keys));
//            Assert.IsTrue(cache.Values.SequenceEqual(cacheCopy.Values));
//#else
//            var cache = new Cache<string, string>(s => s.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
//            {
//                EnsureCapacity = true,
//                Behavior = CacheBehavior.RemoveOldestElement,
//                DisposeDroppedValues = true
//            };
//            Console.WriteLine(cache["alpha"]);
//            Console.WriteLine(cache["beta"]);
//            Console.WriteLine(cache["gamma"]);
//            Assert.IsTrue(cache.Remove("beta"));

//            var cacheCopy = cache.DeepClone();
//            Assert.AreNotSame(cache, cacheCopy);
//            Assert.AreEqual(cache.Count, cacheCopy.Count);
//            Assert.AreEqual(cache.Capacity, cacheCopy.Capacity);
//            Assert.AreEqual(cache.Behavior, cacheCopy.Behavior);
//            Assert.AreEqual(cache.DisposeDroppedValues, cacheCopy.DisposeDroppedValues);
//            Assert.AreEqual(cache.EnsureCapacity, cacheCopy.EnsureCapacity);

//            Assert.IsTrue(cache.SequenceEqual(cacheCopy));
//            Assert.IsTrue(cache.Keys.SequenceEqual(cacheCopy.Keys));
//            Assert.IsTrue(cache.Values.SequenceEqual(cacheCopy.Values));
//#endif
//        }

        #endregion
    }
}
