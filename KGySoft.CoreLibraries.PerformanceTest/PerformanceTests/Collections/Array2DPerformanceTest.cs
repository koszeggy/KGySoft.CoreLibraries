#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Array2DPerformanceTest.cs
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
    public class Array2DPerformanceTest
    {
        #region Methods

        [Test]
        public void AllocateTest()
        {
            const int width = 320;
            const int height = 200;
            new PerformanceTest
                {
                    TestName = nameof(AllocateTest),
                    Iterations = 10_000
                }
                .AddCase(() =>
                {
                    var _ = new int[height, width];
                }, "new int[height, width]")
                .AddCase(() =>
                {
                    var jagged = new int[height][];
                    for (int y = 0; y < height; y++)
                        jagged[y] = new int[width];
                }, "new int[height][] + new int[width]")
                .AddCase(() =>
                {
                    using var section = new Array2D<int>(height, width);
                }, "new Array2D<int>(height, width) + Dispose")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void AccessTest()
        {
            const int width = 320;
            const int height = 200;
            var array = new int[height, width];
            var arrayJagged = new int[height][];
            for (int y = 0; y < height; y++)
                arrayJagged[y] = new int[width];
            var array2d = new Array2D<int>(height, width);

            new PerformanceTest
                {
                    TestName = nameof(AccessTest),
                    Iterations = 10_000,
                    Repeat = 5
                }
                .AddCase(() =>
                {
                    int i = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                            array[y, x] = ++i;
                    }
                }, "int[y, x] = value")
                .AddCase(() =>
                {
                    int i = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                            arrayJagged[y][x] = ++i;
                    }
                }, "int[y][x] = value")
                .AddCase(() =>
                {
                    int i = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                            array2d[y, x] = ++i;
                    }
                }, "Array2D<int>[y, x] = value")
                .DoTest()
                .DumpResults(Console.Out);

            array2d.Dispose();
        }

        [Test]
        public void EnumerationTest()
        {
            const int width = 320;
            const int height = 200;
            var array = new int[height, width];
            var arrayJagged = new int[height][];
            for (int y = 0; y < height; y++)
                arrayJagged[y] = new int[width];
            var array2d = new Array2D<int>(height, width);

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
                }, "foreach on int[,]")
                .AddCase(() =>
                {
                    int sum = 0;
                    foreach (int[] inner in arrayJagged)
                    {
                        foreach (int i in inner)
                            sum += i;
                    }

                    return sum;
                }, "foreach on int[][]")
                .AddCase(() =>
                {
                    int sum = 0;
                    foreach (int i in array2d)
                        sum += i;
                    return sum;
                }, "foreach on Array2D<int>")
                .AddCase(() => array.Cast<int>().Sum(), "LINQ on int[,] (with Cast)")
                .AddCase(() => arrayJagged.SelectMany(inner => inner).Sum(), "LINQ on int[][]")
                .AddCase(() => array2d.Sum(), "LINQ on Array2D<int>")
                .DoTest()
                .DumpResults(Console.Out);

            array2d.Dispose();
        }

        #endregion
    }
}
