#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Array3DPerformanceTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    [TestFixture]
    public class Array3DPerformanceTest
    {
        #region Methods

        [Test]
        public void AllocateTest()
        {
            const int width = 120;
            const int height = 100;
            const int depth = 5;
            new PerformanceTest
                {
                    TestName = nameof(AllocateTest),
                    Iterations = 10_000
                }
                .AddCase(() =>
                {
                    var _ = new int[depth, height, width];
                }, "new int[depth, height, width]")
                .AddCase(() =>
                {
                    var jagged = new int[depth][][];
                    for (int z = 0; z < depth; z++)
                    {
                        jagged[z] = new int[height][];
                        for (int y = 0; y < height; y++)
                            jagged[z][y] = new int[width];
                    }
                }, "new int[depth][][] + int[height][] + new int[width]")
                .AddCase(() =>
                {
                    using var section = new Array3D<int>(depth, height, width);
                }, "new Array3D<int>(height, width) + Dispose")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void AccessTest()
        {
            const int width = 120;
            const int height = 100;
            const int depth = 5;
            var array = new int[depth, height, width];
            var jagged = new int[depth][][];
            for (int z = 0; z < depth; z++)
            {
                jagged[z] = new int[height][];
                for (int y = 0; y < height; y++)
                    jagged[z][y] = new int[width];
            }
            var array3D = new Array3D<int>(depth, height, width);

            new PerformanceTest
                {
                    TestName = nameof(AccessTest),
                    Iterations = 10_000
                }
                .AddCase(() =>
                {
                    int i = 0;
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                                array[z, y, x] = ++i;
                        } 
                    }
                }, "int[z, y, x] = value")
                .AddCase(() =>
                {
                    int i = 0;
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                                jagged[z][y][x] = ++i;
                        } 
                    }
                }, "int[z][y][x] = value")
                .AddCase(() =>
                {
                    int i = 0;
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                                array3D[z, y, x] = ++i;
                        } 
                    }
                }, "Array3D<int>[z, y, x] = value")
                .DoTest()
                .DumpResults(Console.Out);

            array3D.Dispose();
        }

        [Test]
        public void EnumerationTest()
        {
            const int width = 120;
            const int height = 100;
            const int depth = 5;
            var array = new int[depth, height, width];
            var jagged = new int[depth][][];
            for (int z = 0; z < depth; z++)
            {
                jagged[z] = new int[height][];
                for (int y = 0; y < height; y++)
                    jagged[z][y] = new int[width];
            }
            var array3D = new Array3D<int>(depth, height, width);

            new PerformanceTest<int>
                {
                    TestName = nameof(EnumerationTest),
                    Iterations = 100
                }
                .AddCase(() =>
                {
                    int sum = 0;
                    foreach (int i in array)
                        sum += i;
                    return sum;
                }, "foreach on int[,,]")
                .AddCase(() =>
                {
                    int sum = 0;
                    foreach (int[][] inner2 in jagged)
                    {
                        foreach (int[] inner in inner2)
                        {
                            foreach (int i in inner)
                                sum += i;
                        }
                    }
                    
                    return sum;
                }, "foreach on int[][][]")
                .AddCase(() =>
                {
                    int sum = 0;
                    foreach (int i in array3D)
                        sum += i;
                    return sum;
                }, "foreach on Array3D<int>")
                .AddCase(() => array.Cast<int>().Sum(), "LINQ on int[,,] (with Cast)")
                .AddCase(() => (from inner2 in jagged from inner in inner2 from i in inner select i).Sum(), "LINQ on int[][][]")
                .AddCase(() => array3D.Sum(), "LINQ on Array3D<int>")
                .DoTest()
                .DumpResults(Console.Out);

            array3D.Dispose();
        }

        #endregion
    }
}
