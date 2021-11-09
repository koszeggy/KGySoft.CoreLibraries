#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySectionPerformanceTest.cs
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Collections
{
    [TestFixture]
    public class ArraySectionPerformanceTest
    {
        #region Methods

        [Test]
        public void AllocateTest()
        {
            const int size = 1 << 16;
            new PerformanceTest
                {
                    TestName = nameof(AllocateTest),
                    Iterations = 10_000
                }
                .AddCase(() =>
                {
                    var _ = new int[size];
                }, "new int[size]")
                .AddCase(() =>
                {
                    var section = new ArraySection<int>(size);
                    section.Release();
                }, "new ArraySection<int>(size, assureClean: true) + Release")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void AccessTest()
        {
            const int size = 1 << 8;
            var array = new int[size];
            var arraySection = new ArraySection<int>(size);

            new PerformanceTest
                {
                    TestName = nameof(AccessTest),
                    Iterations = 100_000,
                    Repeat = 5
                }
                .AddCase(() =>
                {
                    for (int i = 0; i < size; i++)
                        array[i] = i;
                }, "int[index] = value")
                .AddCase(() =>
                {
                    for (int i = 0; i < size; i++)
                        arraySection[i] = i;
                }, "ArraySection<int>[index] = value")
                .DoTest()
                .DumpResults(Console.Out);

            arraySection.Release();
        }

        [Test]
        public void EnumerationTest()
        {
            const int size = 1 << 8;
            IEnumerable<int> range = Enumerable.Range(0, size);
            int[] array = range.ToArray();
            ArraySection<int> arraySection = new ArraySection<int>(array);

            new PerformanceTest<int>
                {
                    TestName = nameof(EnumerationTest),
                    Iterations = 100_000,
                    Repeat = 5
                }
                //.AddCase(() =>
                //{
                //    int sum = 0;
                //    foreach (int i in array)
                //        sum += i;
                //    return sum;
                //}, "foreach on int[]")
                //.AddCase(() =>
                //{
                //    int sum = 0;
                //    foreach (int i in arraySection)
                //        sum += i;
                //    return sum;
                //}, "foreach on ArraySection<int>")
                .AddCase(() => array.Sum(), "LINQ on int[]")
                .AddCase(() => arraySection.Sum(), "LINQ on ArraySection<int>")
                .DoTest()
                .DumpResults(Console.Out);

            arraySection.Release();
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [Test]
        public void AsSpanTest()
        {
            const int size = 1 << 8;
            var array = new int[size];
            Memory<int> memory = array; 
            ArraySegment<int> arraySegment = array;
            ArraySection<int> arraySection = array;

            new PerformanceTest<int>
                {
                    TestName = nameof(AsSpanTest),
                    Iterations = 1_000_000
                }
                .AddCase(() => array.AsSpan()[0], "int[].AsSpan()")
                .AddCase(() => arraySegment.AsSpan()[0], "ArraySegment<int>.AsSpan()")
                .AddCase(() => arraySection.AsSpan[0], "ArraySection<int>.AsSpan")
                .AddCase(() => memory.Span[0], "Memory<int>.Span")
                .DoTest()
                .DumpResults(Console.Out);
        }
#endif

        #endregion
    }
}