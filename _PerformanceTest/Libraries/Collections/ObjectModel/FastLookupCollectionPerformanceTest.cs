using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KGySoft.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest.Libraries.Collections.ObjectModel
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
            const int interations = 10000;
            const int repeat = 5;

            new TestOperation
            {
                TestOpName = "Collection.IndexOf",
                TestOperation = () => collReference.IndexOf(rnd.Next(collReference.Count)),
                Iterations = interations,
                Repeat = repeat
            }.DoTest();

            rnd = new Random(0);
            new TestOperation
            {
                TestOpName = "FastLookupCollection.IndexOf, Consistency check ON",
                TestOperation = () => collTest.IndexOf(rnd.Next(collTest.Count)),
                Iterations = interations,
                Repeat = repeat
            }.DoTest();

            rnd = new Random(0);
            collTest.CheckConsistency = false;
            new TestOperation
            {
                TestOpName = "FastLookupCollection.IndexOf, Consistency check OFF",
                TestOperation = () => collTest.IndexOf(rnd.Next(collTest.Count)),
                Iterations = interations,
                Repeat = repeat
            }.DoTest();
        }
    }
}
