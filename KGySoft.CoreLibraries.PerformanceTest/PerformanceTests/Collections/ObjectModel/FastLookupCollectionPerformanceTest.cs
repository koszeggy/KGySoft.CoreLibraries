#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastLookupCollectionPerformanceTest.cs
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
        public void TestIndexOf()
        {
            var list = Enumerable.Range(0, 1000).ToList();
            var collReference = new Collection<int>(list);
            var collTest = new FastLookupCollection<int>(list);
            var collTestNoCheck = new FastLookupCollection<int>(list) { CheckConsistency = false };

            new RandomizedPerformanceTest
                {
                    TestTime = 200,
                    //Repeat = 5,
                }
                .AddCase(rnd => collReference.IndexOf(rnd.Next(collReference.Count)), "Collection.IndexOf")
                .AddCase(rnd => collTest.IndexOf(rnd.Next(collTest.Count)), "FastLookupCollection.IndexOf, Consistency check ON")
                .AddCase(rnd => collTest.IndexOf(rnd.Next(collTestNoCheck.Count)), "FastLookupCollection.IndexOf, Consistency check OFF")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}
