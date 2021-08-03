#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastLookupCollectionPerformanceTest.cs
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
using System.Collections.ObjectModel;
using System.Linq;

using KGySoft.Collections.ObjectModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections.ObjectModel
{
    [TestFixture]
    public class FastLookupCollectionPerformanceTest
    {
        #region Methods

        [Test]
        public void IndexOfTest()
        {
            const int count = 1000;
            var list = Enumerable.Range(0, count).ToList();
            var collReference = new Collection<int>(list);
            var collTest = new FastLookupCollection<int>(list);
            var collTestNoCheck = new FastLookupCollection<int>(list) { CheckConsistency = false };

            new RandomizedPerformanceTest { Iterations = 1_000_000 }
                .AddCase(rnd => collReference.IndexOf(rnd.Next(count)), "Collection.IndexOf")
                .AddCase(rnd => collTest.IndexOf(rnd.Next(count)), "FastLookupCollection.IndexOf, Consistency check ON")
                .AddCase(rnd => collTestNoCheck.IndexOf(rnd.Next(count)), "FastLookupCollection.IndexOf, Consistency check OFF")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void AddRemoveLastTest()
        {
            const int count = 1000;
            var list = Enumerable.Range(0, count).ToList();
            var collReference = new Collection<int>(list);
            var collTest = new FastLookupCollection<int>(list);
            var collTestNoCheck = new FastLookupCollection<int>(list) { CheckConsistency = false };
            const int value = -1;

            new PerformanceTest { TestName = "Add+IndexOf+RemoveAt", TestTime = 1000 }
                .AddCase(() =>
                {
                    collReference.Add(value);
                    collReference.RemoveAt(collReference.IndexOf(value));
                }, "Collection")
                .AddCase(() =>
                {
                    collTest.Add(value);
                    collTest.RemoveAt(collTest.IndexOf(value));
                }, "FastLookupCollection, Consistency check ON")
                .AddCase(() =>
                {
                    collTestNoCheck.Add(value);
                    collTestNoCheck.RemoveAt(collTestNoCheck.IndexOf(value));
                }, "FastLookupCollection, Consistency check OFF")
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest { TestName = "Add+Remove", TestTime = 1000 }
                .AddCase(() =>
                {
                    collReference.Add(value);
                    collReference.Remove(value);
                }, "Collection")
                .AddCase(() =>
                {
                    collTest.Add(value);
                    collTest.Remove(value);
                }, "FastLookupCollection, Consistency check ON")
                .AddCase(() =>
                {
                    collTestNoCheck.Add(value);
                    collTestNoCheck.Remove(value);
                }, "FastLookupCollection, Consistency check OFF")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void AddRemoveRandomTest()
        {
            const int count = 1000;
            var list = Enumerable.Range(0, count).ToList();
            var collReference = new Collection<int>(list);
            var collTest = new FastLookupCollection<int>(list);
            var collTestNoCheck = new FastLookupCollection<int>(list) { CheckConsistency = false };
            const int value = -1;

            new RandomizedPerformanceTest { TestName = "Insert+RemoveAt", TestTime = 1000 }
                .AddCase(rnd =>
                {
                    int pos = rnd.Next(count);
                    collReference.Insert(pos, value);
                    collReference.RemoveAt(pos);
                }, "Collection")
                .AddCase(rnd =>
                {
                    int pos = rnd.Next(count);
                    collTest.Insert(pos, value);
                    collTest.RemoveAt(pos);
                }, "FastLookupCollection, Consistency check ON")
                .AddCase(rnd =>
                {
                    int pos = rnd.Next(count);
                    collTestNoCheck.Insert(pos, value);
                    collTestNoCheck.RemoveAt(pos);
                }, "FastLookupCollection, Consistency check OFF")
                .DoTest()
                .DumpResults(Console.Out);

            new RandomizedPerformanceTest { TestName = "Insert+Remove", TestTime = 1000 }
                .AddCase(rnd =>
                {
                    collReference.Insert(rnd.Next(count), value);
                    collReference.Remove(value);
                }, "Collection")
                .AddCase(rnd =>
                {
                    collTest.Insert(rnd.Next(count), value);
                    collTest.Remove(value);
                }, "FastLookupCollection, Consistency check ON")
                .AddCase(rnd =>
                {
                    collTestNoCheck.Insert(rnd.Next(count), value);
                    collTestNoCheck.Remove(value);
                }, "FastLookupCollection, Consistency check OFF")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void SetGetTest()
        {
            const int count = 1000;
            var list = Enumerable.Range(0, count).ToList();
            var collReference = new Collection<int>(list);
            var collTest = new FastLookupCollection<int>(list);
            var collTestNoCheck = new FastLookupCollection<int>(list) { CheckConsistency = false };
            const int value = -1;

            new RandomizedPerformanceTest<int> { TestTime = 1000 }
                .AddCase(rnd =>
                {
                    int pos = rnd.Next(count);
                    collReference[pos] = value;
                    return collReference[pos];
                }, "Collection")
                .AddCase(rnd =>
                {
                    int pos = rnd.Next(count);
                    collTest[pos] = value;
                    return collTest[pos];
                }, "FastLookupCollection, Consistency check ON")
                .AddCase(rnd =>
                {
                    int pos = rnd.Next(count);
                    collTestNoCheck[pos] = value;
                    return collTestNoCheck[pos];
                }, "FastLookupCollection, Consistency check OFF")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}
