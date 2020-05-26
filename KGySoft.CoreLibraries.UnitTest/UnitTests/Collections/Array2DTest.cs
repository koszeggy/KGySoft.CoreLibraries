using System;
using System.Collections.Generic;
using System.Text;
using KGySoft.Collections;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class Array2DTest
    {
        [Test]
        public void ArrayCompatibilityTest()
        {
            const int width = 2;
            const int height = 3;
            var array = new int[height, width];
            var array2d = new Array2D<int>(height, width);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    array[y, x] = array2d[y, x] = y * width + x;
            }

            Assert.AreEqual(array, array2d);
        }
    }
}
