using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KGySoft.Threading;

using NUnit.Framework;

namespace KGySoft.CoreLibraries.UnitTests.Threading
{
    [TestFixture]
    public class ParallelHelperTest
    {
        [Test]
        public void ForTest()
        {
            var bools = new bool[1000];
            ParallelHelper.For(0, bools.Length, i => bools[i] = true);
            Assert.IsTrue(bools.All(b => b));
        }
    }
}
