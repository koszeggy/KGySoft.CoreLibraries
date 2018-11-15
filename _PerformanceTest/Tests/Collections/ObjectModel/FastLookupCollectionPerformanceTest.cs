using System;
using System.Collections.ObjectModel;
using System.Linq;
using KGySoft.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest.Tests.Collections.ObjectModel
{
    [TestClass]
    public class FastLookupCollectionPerformanceTest : TestBase
    {
        [TestMethod]
        public void TestIndexOf()
        {
            var list = Enumerable.Range(0, 1000).ToList();
            var collReference =  new Collection<int>(list);
            var collTest = new FastLookupCollection<int>(list);
            var rnd = new Random(0);
            const int iterations = 10000;
            const int repeat = 5;

            new TestOperation
            {
                TestOpName = "Collection.IndexOf",
                TestOperation = () => collReference.IndexOf(rnd.Next(collReference.Count)),
                Iterations = iterations,
                Repeat = repeat
            }.DoTest();

            rnd = new Random(0);
            new TestOperation
            {
                TestOpName = "FastLookupCollection.IndexOf, Consistency check ON",
                TestOperation = () => collTest.IndexOf(rnd.Next(collTest.Count)),
                Iterations = iterations,
                Repeat = repeat
            }.DoTest();

            rnd = new Random(0);
            collTest.CheckConsistency = false;
            new TestOperation
            {
                TestOpName = "FastLookupCollection.IndexOf, Consistency check OFF",
                TestOperation = () => collTest.IndexOf(rnd.Next(collTest.Count)),
                Iterations = iterations,
                Repeat = repeat
            }.DoTest();
        }
    }
}
