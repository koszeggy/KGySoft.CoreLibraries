#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Array2DTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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

using KGySoft.Collections;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class Array2DTest
    {
        #region Methods

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

        [Test]
        public void ZeroDimensionTest()
        {
            var array2d = new Array2D<int>(2, 0);

            Assert.IsTrue(array2d.IsNullOrEmpty());
            Assert.AreEqual(Reflector.EmptyArray<int>(), array2d.Buffer.ToArray());
            int _;
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = array2d.Buffer[0]);
        }

        #endregion
    }
}