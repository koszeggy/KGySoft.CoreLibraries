#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringKeyedDictionaryTest.cs
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

#region Suppressions

#if NET
#if NET5_0 || NET6_0
#pragma warning disable IDE0079 // Remove unnecessary suppression - CS0618 is emitted by ReSharper
#pragma warning disable CS0618 // Use of obsolete symbol - as above  
#else
#error Check whether IFormatter is still available in this .NET version
#endif
#endif

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class StringKeyedDictionaryTest
    {
        #region Fields

        private static readonly StringSegmentComparer[] comparers =
        {
            null,
            StringSegmentComparer.Ordinal,
            StringSegmentComparer.OrdinalIgnoreCase,
            StringSegmentComparer.InvariantCulture,
            StringSegmentComparer.InvariantCultureIgnoreCase,
            StringSegmentComparer.CurrentCulture,
            StringSegmentComparer.CurrentCultureIgnoreCase,
            StringSegmentComparer.OrdinalRandomized,
            StringSegmentComparer.OrdinalIgnoreCaseRandomized,
        };

        #endregion

        #region Methods

        [TestCaseSource(nameof(comparers))]
        public void UsageTest(StringSegmentComparer comparer)
        {
            var dict = new StringKeyedDictionary<int>(comparer) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 } };
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);
            Assert.AreEqual(1, dict["alpha".AsSegment()]);
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.AreEqual(1, dict["alpha".AsSpan()]);
#endif

            // remove and re-add by string
            Assert.IsTrue(dict.Remove("alpha"));
            Assert.AreEqual(2, dict.Count);
            Assert.IsFalse(dict.Remove("alpha"));
            Assert.IsTrue(dict.Remove("beta"));
            dict.Add("alpha", -1);
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(-1, dict["alpha"]);
            Assert.AreEqual(-1, dict["alpha".AsSegment()]);
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Assert.AreEqual(-1, dict["alpha".AsSpan()]);
#endif

            // Clear
            dict.Clear();
            Assert.AreEqual(0, dict.Count);
            dict.Add("alpha", 42);
            Assert.AreEqual(42, dict["alpha"]);
        }

        [Test]
        public void KeysValuesTest()
        {
            var dict = new StringKeyedDictionary<int>
            {
                { "alpha", 1 },
                { "beta", 2 },
                { "gamma", 3 },
                { "delta", 4 },
                { "epsilon", 5 }
            };

            ICollection<string> keys = dict.Keys;
            Assert.AreEqual(dict.Count, keys.Count);
            CollectionAssert.AreEqual(dict.Select(c => c.Key), dict.Keys);


            ICollection<int> values = dict.Values;
            Assert.AreEqual(dict.Count, values.Count);
            CollectionAssert.AreEqual(dict.Select(c => c.Value), dict.Values);

            Assert.IsTrue(dict.Remove("beta"));
            Assert.AreEqual(dict.Count, keys.Count);
            Assert.AreEqual(dict.Count, values.Count);
        }

#if !NETFRAMEWORK
        [Obsolete]
#endif
        [TestCase(StringComparison.Ordinal)]
        [TestCase(StringComparison.OrdinalIgnoreCase)]
        [TestCase(StringComparison.InvariantCulture)]
        [TestCase(StringComparison.InvariantCultureIgnoreCase)]
        [TestCase(StringComparison.CurrentCulture)]
        [TestCase(StringComparison.CurrentCultureIgnoreCase)]
        public void SerializationTest(StringComparison comparison)
        {
            var dict = new StringKeyedDictionary<int>(StringSegmentComparer.FromComparison(comparison))
            {
                { "alpha", 1 },
                { "beta", 2 },
                { "gamma", 3 },
                { "delta", 4 },
                { "epsilon", 5 }
            };

            // By BinarySerializationFormatter
            StringKeyedDictionary<int> clone = dict.DeepClone();
            Assert.AreNotSame(dict, clone);
            Assert.AreEqual(dict.Count, clone.Count);
            CollectionAssert.AreEqual(dict, clone);

            // By BinaryFormatter
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, dict);
            ms.Position = 0;
            clone = (StringKeyedDictionary<int>)formatter.Deserialize(ms);
            CollectionAssert.AreEqual(dict, clone);
        }

        #endregion
    }
}
