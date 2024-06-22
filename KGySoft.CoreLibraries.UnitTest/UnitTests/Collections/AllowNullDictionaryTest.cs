#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AllowNullDictionaryTest.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

#nullable enable

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class AllowNullDictionaryTest
    {
        #region Methods

        [SuppressMessage("ReSharper", "GenericEnumeratorNotDisposed", Justification = "Its Dispose doesn't do anything")]
        [TestCase("key1", "value1")]
        [TestCase(42, "alpha")]
#if NETCOREAPP3_0_OR_GREATER
        [TestCase<string, string>(null, "null")]
        [TestCase<int?, string>(null, "null")]
#else
        [TestCaseGeneric(null, "null", TypeArguments = new[] { typeof(string), typeof(string) })]
        [TestCaseGeneric(null, "null", TypeArguments = new[] { typeof(int?), typeof(string) })]
#endif
        public void UsageTest<TKey, TValue>(TKey key, TValue value)
        {
            var dict = new AllowNullDictionary<TKey, TValue>
            {
                { 1.Convert<TKey>(), 1.Convert<TValue>() },
                { 2.Convert<TKey>(), 2.Convert<TValue>() },
                { key, value }
            };

            IDictionary<TKey, TValue> genDict = dict;
            var array = new KeyValuePair<TKey, TValue>[dict.Count];
            var keys = dict.Keys;
            var keysArray = new TKey[dict.Count];
            var values = dict.Values;
            var valuesArray = new TValue[dict.Count];

            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(3, dict.GetEnumerator().RestToList().Count);
            Assert.AreEqual(3, keys.Count);
            Assert.AreEqual(3, values.Count);

            Assert.AreEqual(value, dict[key]);
            Assert.IsTrue(dict.ContainsKey(key));
            Assert.IsTrue(keys.Contains(key));
            Assert.IsTrue(values.Contains(value));
            Assert.IsTrue(dict.TryGetValue(key, out TValue? actualValue));
            Assert.AreEqual(value, actualValue);
            Assert.IsTrue(dict.Any(i => Equals(i.Key, key)));
            Assert.IsTrue(keys.Any(k => Equals(k, key)));
            Assert.IsTrue(values.Any(v => Equals(v, value)));
            Assert.DoesNotThrow(() => genDict.CopyTo(array, 0));
            Assert.GreaterOrEqual(Array.IndexOf(array, new KeyValuePair<TKey,TValue>(key, value)), 0);
            Assert.DoesNotThrow(() => keys.CopyTo(keysArray, 0));
            Assert.GreaterOrEqual(Array.IndexOf(keysArray, key), 0);
            Assert.DoesNotThrow(() => values.CopyTo(valuesArray, 0));
            Assert.GreaterOrEqual(Array.IndexOf(valuesArray, value), 0);
            Assert.Throws<ArgumentException>(() => dict.Add(key, value));
            Assert.DoesNotThrow(() => dict[key] = value);
            Assert.IsTrue(dict.Remove(key));

            Array.Clear(array, 0, array.Length);
            Array.Clear(keysArray, 0, keysArray.Length);
            Array.Clear(valuesArray, 0, valuesArray.Length);
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(2, dict.GetEnumerator().RestToList().Count);
            Assert.AreEqual(2, keys.Count);
            Assert.AreEqual(2, values.Count);

            Assert.Throws<KeyNotFoundException>(() => actualValue = dict[key]);
            Assert.IsFalse(dict.ContainsKey(key));
            Assert.IsFalse(keys.Contains(key));
            Assert.IsFalse(values.Contains(value));
            Assert.IsFalse(dict.TryGetValue(key, out actualValue));
            Assert.IsNull(actualValue);
            Assert.IsFalse(dict.Any(i => Equals(i.Key, key)));
            Assert.IsFalse(keys.Any(k => Equals(k, key)));
            Assert.IsFalse(values.Any(v => Equals(v, value)));
            Assert.DoesNotThrow(() => genDict.CopyTo(array, 0));
            Assert.AreEqual(-1, Array.IndexOf(array, new KeyValuePair<TKey, TValue>(key, value)));
            Assert.DoesNotThrow(() => keys.CopyTo(keysArray, 0));
            Assert.That(() => Array.IndexOf(keysArray, key), Equals(key, default(TKey)) ? Is.GreaterThanOrEqualTo(0) : Is.EqualTo(-1));
            Assert.DoesNotThrow(() => values.CopyTo(valuesArray, 0));
            Assert.AreEqual(-1, Array.IndexOf(valuesArray, value));
            Assert.IsFalse(dict.Remove(key));
            Assert.DoesNotThrow(() => dict.Add(key, value));

            IDictionary dictNonGen = dict;
            ICollection keysNonGen = dictNonGen.Keys;
            ICollection valuesNonGen = dictNonGen.Values;
            var objArray = new object?[dict.Count];

            Assert.AreEqual(3, dictNonGen.Count);
            Assert.AreEqual(3, dictNonGen.GetEnumerator().RestToList().Count);
            Assert.AreEqual(3, keysNonGen.Count);
            Assert.AreEqual(3, valuesNonGen.Count);

            Assert.AreEqual(value, dictNonGen[key!]);
            Assert.IsTrue(dictNonGen.Contains(key));
            Assert.IsTrue(dictNonGen.GetEnumerator().ToEnumerable().Any(i => Equals(i.Key, key)));
            Assert.IsTrue(keysNonGen.Cast<TKey>().Any(k => Equals(k, key)));
            Assert.IsTrue(valuesNonGen.Cast<TValue>().Any(v => Equals(v, value)));
            Assert.DoesNotThrow(() => dictNonGen.CopyTo(objArray, 0));
            Assert.GreaterOrEqual(Array.IndexOf(objArray, new KeyValuePair<TKey, TValue>(key, value)), 0);
            Assert.DoesNotThrow(() => keysNonGen.CopyTo(objArray, 0));
            Assert.GreaterOrEqual(Array.IndexOf(objArray, key), 0);
            Assert.DoesNotThrow(() => valuesNonGen.CopyTo(objArray, 0));
            Assert.GreaterOrEqual(Array.IndexOf(objArray, value), 0);
            Assert.Throws<ArgumentException>(() => dictNonGen.Add(key, value));
            Assert.DoesNotThrow(() => dictNonGen[key] = value);
            Assert.DoesNotThrow(() => dictNonGen.Remove(key));

            Array.Clear(objArray, 0, objArray.Length);
            Assert.AreEqual(2, dictNonGen.Count);
            Assert.AreEqual(2, dictNonGen.GetEnumerator().RestToList().Count);
            Assert.AreEqual(2, keysNonGen.Count);
            Assert.AreEqual(2, valuesNonGen.Count);

            Assert.IsNull(dictNonGen[key]);
            Assert.IsFalse(dictNonGen.Contains(key));
            Assert.IsFalse(dictNonGen.GetEnumerator().ToEnumerable().Any(i => Equals(i.Key, key)));
            Assert.IsFalse(keysNonGen.Cast<TKey>().Any(k => Equals(k, key)));
            Assert.IsFalse(valuesNonGen.Cast<TValue>().Any(v => Equals(v, value)));
            Assert.DoesNotThrow(() => dictNonGen.CopyTo(objArray, 0));
            Assert.AreEqual(-1, Array.IndexOf(array, new KeyValuePair<TKey, TValue>(key, value)));
            Assert.DoesNotThrow(() => keysNonGen.CopyTo(objArray, 0));
            Assert.That(() => Array.IndexOf(keysArray, key), key == null! ? Is.GreaterThanOrEqualTo(0) : Is.EqualTo(-1));
            Assert.DoesNotThrow(() => valuesNonGen.CopyTo(objArray, 0));
            Assert.AreEqual(-1, Array.IndexOf(valuesArray, value));
            Assert.DoesNotThrow(() => dictNonGen.Remove(key!));
            Assert.DoesNotThrow(() => dictNonGen.Add(key!, value));
        }

        #endregion
    }
}