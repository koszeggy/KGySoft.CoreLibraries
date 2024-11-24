#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySectionPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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

        [Test]
        public void CastTest()
        {
            const int size = 1 << 8;
            var array = new int[size];
            ArraySection<int> arraySection = array;
            CastArray<byte, int> castArray = new byte[size * 4];
            new PerformanceTest<int>
                {
                    TestName = nameof(CastTest),
                    TestTime = 5000,
                    Repeat = 3
                }
                .AddCase(() =>
                {
                    int sum = 0;
                    for (int i = 0; i < size; i++)
                        sum += array[i];
                    return sum;
                }, "int[]")
                .AddCase(() =>
                {
                    int sum = 0;
                    for (int i = 0; i < size; i++)
                        sum += arraySection[i];
                    return sum;
                }, "ArraySection<int>.this[]")
                .AddCase(() =>
                {
                    int sum = 0;
                    for (int i = 0; i < size; i++)
                        sum += arraySection.GetElementUnchecked(i);
                    return sum;
                }, "ArraySection<int>.GetElementUnchecked()")
                .AddCase(() =>
                {
                    int sum = 0;
                    for (int i = 0; i < size; i++)
                        sum += castArray[i];
                    return sum;
                }, "CastArray<byte, int>.this[]")
                .AddCase(() =>
                {
                    int sum = 0;
                    for (int i = 0; i < size; i++)
                        sum += castArray.GetElementUnsafe(i);
                    return sum;
                }, "CastArray<byte, int>.GetElementUnsafe()")
                .DoTest()
                .DumpResults(Console.Out);

            // When the Unsafe/Unchecked members had validation, the .NET 9 results didn't make any sense. The .NET 8 vs .NET 9 results with validation:

            //   ==[CastTest (.NET Core 8.0.11) Results]================================================
            //   Test Time: 5 000 ms
            //   Warming up: Yes
            //   Test cases: 5
            //   Repeats: 3
            //   Calling GC.Collect: Yes
            //   Forced CPU Affinity: No
            //   Cases are sorted by fulfilled iterations (the most first)
            //   --------------------------------------------------
            //   1. int[]: 76 147 062 iterations in 15 000,01 ms. Adjusted for 5 000 ms: 25 382 332,48
            //     #1  22 612 023 iterations in 5 000,01 ms. Adjusted: 22 611 968,28
            //     #2  21 729 178 iterations in 5 000,00 ms. Adjusted: 21 729 174,52	 <---- Worst
            //     #3  31 805 861 iterations in 5 000,00 ms. Adjusted: 31 805 854,64	 <---- Best
            //     Worst-Best difference: 10 076 680,12 (46,37%)
            //   2. CastArray<byte, int>.GetElementUnsafe(): 61 283 100 iterations in 15 000,00 ms. Adjusted for 5 000 ms: 20 427 695,91 (-4 954 636,57 / 80,48%)
            //     #1  20 384 655 iterations in 5 000,00 ms. Adjusted: 20 384 650,92	 <---- Worst
            //     #2  20 484 621 iterations in 5 000,00 ms. Adjusted: 20 484 617,31	 <---- Best
            //     #3  20 413 824 iterations in 5 000,00 ms. Adjusted: 20 413 819,51
            //     Worst-Best difference: 99 966,39 (0,49%)
            //   3. CastArray<byte, int>.this[]: 50 064 699 iterations in 15 000,00 ms. Adjusted for 5 000 ms: 16 688 229,44 (-8 694 103,04 / 65,75%)
            //     #1  16 765 507 iterations in 5 000,00 ms. Adjusted: 16 765 502,98	 <---- Best
            //     #2  16 545 793 iterations in 5 000,00 ms. Adjusted: 16 545 789,36	 <---- Worst
            //     #3  16 753 399 iterations in 5 000,00 ms. Adjusted: 16 753 395,98
            //     Worst-Best difference: 219 713,62 (1,33%)
            //   4. ArraySection<int>.GetElementUnchecked(): 35 238 766 iterations in 15 000,00 ms. Adjusted for 5 000 ms: 11 746 252,67 (-13 636 079,81 / 46,28%)
            //     #1  11 754 512 iterations in 5 000,00 ms. Adjusted: 11 754 509,88
            //     #2  11 701 179 iterations in 5 000,00 ms. Adjusted: 11 701 176,19	 <---- Worst
            //     #3  11 783 075 iterations in 5 000,00 ms. Adjusted: 11 783 071,94	 <---- Best
            //     Worst-Best difference: 81 895,74 (0,70%)
            //   5. ArraySection<int>.this[]: 35 103 222 iterations in 15 000,00 ms. Adjusted for 5 000 ms: 11 701 072,05 (-13 681 260,43 / 46,10%)
            //     #1  11 798 659 iterations in 5 000,00 ms. Adjusted: 11 798 656,88
            //     #2  11 810 859 iterations in 5 000,00 ms. Adjusted: 11 810 857,11	 <---- Best
            //     #3  11 493 704 iterations in 5 000,00 ms. Adjusted: 11 493 702,16	 <---- Worst
            //     Worst-Best difference: 317 154,95 (2,76%)

            // ==[CastTest (.NET Core 9.0.0) Results]================================================
            // Test Time: 5 000 ms
            // Warming up: Yes
            // Test cases: 5
            // Repeats: 3
            // Calling GC.Collect: Yes
            // Forced CPU Affinity: No
            // Cases are sorted by fulfilled iterations (the most first)
            // --------------------------------------------------
            // 1. int[]: 88 433 450 iterations in 15 010,38 ms. Adjusted for 5 000 ms: 29 459 692,58
            //   #1  26 015 300 iterations in 5 000,00 ms. Adjusted: 26 015 298,96	 <---- Worst
            //   #2  26 252 475 iterations in 5 010,38 ms. Adjusted: 26 198 104,50
            //   #3  36 165 675 iterations in 5 000,00 ms. Adjusted: 36 165 674,28	 <---- Best
            //   Worst-Best difference: 10 150 375,32 (39,02%)
            // 2. ArraySection<int>.this[]: 65 975 972 iterations in 15 000,00 ms. Adjusted for 5 000 ms: 21 991 990,08 (-7 467 702,50 / 74,65%)
            //   #1  21 919 783 iterations in 5 000,00 ms. Adjusted: 21 919 782,56	 <---- Worst
            //   #2  21 959 072 iterations in 5 000,00 ms. Adjusted: 21 959 071,12
            //   #3  22 097 117 iterations in 5 000,00 ms. Adjusted: 22 097 116,56	 <---- Best
            //   Worst-Best difference: 177 334,00 (0,81%)
            // 3. CastArray<byte, int>.this[]: 52 393 558 iterations in 15 000,00 ms. Adjusted for 5 000 ms: 17 464 519,33 (-11 995 173,25 / 59,28%)
            //   #1  17 417 420 iterations in 5 000,00 ms. Adjusted: 17 417 420,00	 <---- Worst
            //   #2  17 468 032 iterations in 5 000,00 ms. Adjusted: 17 468 032,00
            //   #3  17 508 106 iterations in 5 000,00 ms. Adjusted: 17 508 106,00	 <---- Best
            //   Worst-Best difference: 90 686,00 (0,52%)
            // 4. CastArray<byte, int>.GetElementUnsafe(): 42 640 524 iterations in 15 000,00 ms. Adjusted for 5 000 ms: 14 213 507,62 (-15 246 184,96 / 48,25%)
            //   #1  14 216 030 iterations in 5 000,00 ms. Adjusted: 14 216 029,15
            //   #2  14 248 602 iterations in 5 000,00 ms. Adjusted: 14 248 601,72	 <---- Best
            //   #3  14 175 892 iterations in 5 000,00 ms. Adjusted: 14 175 892,00	 <---- Worst
            //   Worst-Best difference: 72 709,72 (0,51%)
            // 5. ArraySection<int>.GetElementUnchecked(): 42 464 831 iterations in 15 000,00 ms. Adjusted for 5 000 ms: 14 154 943,19 (-15 304 749,38 / 48,05%)
            //   #1  14 021 669 iterations in 5 000,00 ms. Adjusted: 14 021 668,44	 <---- Worst
            //   #2  14 205 648 iterations in 5 000,00 ms. Adjusted: 14 205 648,00
            //   #3  14 237 514 iterations in 5 000,00 ms. Adjusted: 14 237 513,15	 <---- Best
            //   Worst-Best difference: 215 844,71 (1,54%)

            // After removing even the null validation (actually a method named Unchecked/Unsafe may do that if documented),
            // the results are similar in .NET 8 and .NET 9 again.
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