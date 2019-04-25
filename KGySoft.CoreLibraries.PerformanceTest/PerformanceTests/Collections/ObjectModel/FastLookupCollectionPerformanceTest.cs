using System;
using System.Collections.ObjectModel;
using System.Linq;
using KGySoft.Collections.ObjectModel;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.PerformanceTests.Collections.ObjectModel
{
    [TestFixture]
    public class FastLookupCollectionPerformanceTest
    {
        [Test]
        public void TestIndexOf()
        {
            var list = Enumerable.Range(0, 1000).ToList();
            var collReference =  new Collection<int>(list);
            var collTest = new FastLookupCollection<int>(list);
            var collTestNoCheck = new FastLookupCollection<int>(list) { CheckConsistency = false };
            var rnd = new Random();

            new PerformanceTest
                {
                    TestTime = 200,
                    Repeat = 5,
                }
                .AddCase(() => collReference.IndexOf(rnd.Next(collReference.Count)), "Collection.IndexOf")
                .AddCase(() => collTest.IndexOf(rnd.Next(collTest.Count)), "FastLookupCollection.IndexOf, Consistency check ON")
                .AddCase(() => collTest.IndexOf(rnd.Next(collTestNoCheck.Count)), "FastLookupCollection.IndexOf, Consistency check OFF")
                .DoTest()
                .DumpResults(Console.Out);
        }
    }
}
