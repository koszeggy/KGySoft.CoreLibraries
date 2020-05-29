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
using System.Linq;
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
            Assert.Throws<IndexOutOfRangeException>(() => _ = array2d.Buffer[0]);
        }

        [Test]
        public void SliceTest()
        {
            const int width = 4;
            const int height = 3;
            ArraySection<int> section = Enumerable.Range(0, width * height).ToArray();
            Array2D<int> array = new Array2D<int>(section, height, width);

            Assert.AreEqual(1, array[0][1]);
            Assert.AreEqual(width, array[0].Length);

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            Index from = 1;
            Index to = ^1;
            Span<int> span = section.AsSpan;
            Assert.AreEqual(span.Slice(1, 2).ToArray(), array[0].Slice(1, 2));
            Assert.AreEqual(span[(from.Value * width)..^(to.Value * width)].ToArray(), array[from..to].AsSpan.ToArray());
#endif
        }

        #endregion
    }
}