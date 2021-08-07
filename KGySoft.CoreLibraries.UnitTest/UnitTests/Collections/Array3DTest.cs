#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Array3DTest.cs
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
using System.Linq;
using KGySoft.Collections;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class Array3DTest
    {
        #region Methods

        [Test]
        public void ArrayCompatibilityTest()
        {
            const int width = 2;
            const int height = 3;
            const int depth = 4;
            const int planeSize = width * height;
            var array = new int[depth, height, width];
            var array3D = new Array3D<int>(depth, height, width);
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                        array[z, y, x] = array3D[z, y, x] = z * planeSize + y * width + x;
                } 
            }

            Assert.AreEqual(array, array3D);
        }

        [Test]
        public void ZeroDimensionTest()
        {
            var array3D = new Array3D<int>(0, 2, 1);

            Assert.IsTrue(array3D.IsNullOrEmpty());
            Assert.AreEqual(Reflector.EmptyArray<int>(), array3D.Buffer.ToArray());
            int _;
            Assert.Throws<IndexOutOfRangeException>(() => _ = array3D.Buffer[0]);
        }

        [Test]
        public void SliceTest()
        {
            const int depth = 4;
            const int height = 3;
            const int width = 2;
            const int planeSize = width * height;
            ArraySection<int> section = Enumerable.Range(0, width * height * depth).ToArray();
            Array3D<int> array = new Array3D<int>(section, depth, height, width);

            Assert.AreEqual(1, array[0][0][1]);
            Assert.AreEqual(planeSize, array[0].Length);
            Assert.AreEqual(width, array[0][0].Length);

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Index from = 1;
            Index to = ^1;
            Span<int> span = section.AsSpan;
            Assert.AreEqual(span.Slice(1, 2).ToArray(), array[0][0].Slice(1, 2));
            Assert.AreEqual(span[(from.Value * planeSize)..^(to.Value * planeSize)].ToArray(), array[from..to].AsSpan.ToArray());
#endif
        }

        #endregion
    }
}