#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParallelHelperPerformanceTest.cs
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

using KGySoft.Reflection;
using KGySoft.Threading;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Threading
{
    [TestFixture]
    public class ParallelHelperPerformanceTest
    {
        #region Methods

#if NETCOREAPP3_0_OR_GREATER
        [TestCase<int>]
        //[TestCase<byte>]
#else
        [TestCaseGeneric(TypeArguments = [typeof(int)])]
#endif
        public void SortPerformanceTest<T>()
            where T : unmanaged
        {
            //var array = new int[100_000_000];
            var array = new T[10_000_000];
            var random = new FastRandom(0);
            for (int i = 0; i < array.Length; i++)
                array[i] = random.NextObject<T>();

            var byteArray = new byte[array.Length * Reflector<T>.SizeOf];
            Buffer.BlockCopy(array, 0, byteArray, 0, byteArray.Length);

            int threadCount = 2;
            string typeName = typeof(T).GetName(TypeNameKind.ShortName);

            new PerformanceTest { TestName = $"{nameof(SortPerformanceTest)}<{typeName}>", Repeat = 3, TestTime = 5000 }
                .AddCase(() => ParallelHelper.Sort(AsyncHelper.SingleThreadContext, (T[])array.Clone()), "ParallelHelper.Sort(T[]) (single thread)")
                .AddCase(() => ParallelHelper.Sort((T[])array.Clone()), "ParallelHelper.Sort(T[]) (max threads)")
                .AddCase(() => ParallelHelper.Sort(new List<T>(array)), "ParallelHelper.Sort(List<T>) (max threads)")
                .AddCase(() => ParallelHelper.Sort(new SimpleContext(threadCount), (T[])array.Clone()), $"ParallelHelper.Sort(T[]) ({threadCount} threads)")
                .AddCase(() => ParallelHelper.Sort(new SimpleContext(threadCount), ((T[])array.Clone()).AsSection()), $"ParallelHelper.Sort(ArraySection) ({threadCount} threads)")
                .AddCase(() => ParallelHelper.Sort(((T[])array.Clone()).AsSection()), "ParallelHelper.Sort(ArraySection) (max threads)")
                .AddCase(() => ParallelHelper.Sort(((byte[])byteArray.Clone()).Cast<byte, T>()), $"ParallelHelper.Sort(CastArray<byte, {typeName}>) (max threads)")
                .AddCase(() => ParallelHelper.Sort((IList<T>)((byte[])byteArray.Clone()).Cast<byte, T>()), "ParallelHelper.Sort(CastArray as IList) (max threads)")
                .AddCase(() => ParallelHelper.Sort(((T[])array.Clone()).Cast<T, T>()), $"ParallelHelper.Sort(CastArray<{typeName}, {typeName}>) (max threads)")
                .AddCase(() => Array.Sort((T[])array.Clone()), "Array.Sort")
                .DoTest()
                .DumpResults(Console.Out);
        }

#if NETCOREAPP3_0_OR_GREATER
        [TestCase<byte>]
        [TestCase<int>]
        [TestCase<float>]
#else
        [TestCaseGeneric(TypeArguments = [typeof(byte)])]
        [TestCaseGeneric(TypeArguments = [typeof(int)])]
        [TestCaseGeneric(TypeArguments = [typeof(float)])]
#endif
        public void CastArraySortPerformanceTest<T>()
            where T : unmanaged
        {
            var backingArray = new byte[10_000_000 * Reflector<T>.SizeOf];
            var castArray = backingArray.Cast<byte, T>();
            var random = new FastRandom(0);
            for (int i = 0; i < castArray.Length; i++)
                castArray[i] = random.NextObject<T>();

            T[] array = castArray.ToArray()!;

            string typeName = typeof(T).GetName(TypeNameKind.ShortName);

            new PerformanceTest { TestName = $"{nameof(CastArraySortPerformanceTest)}<{typeName}>", Repeat = 3, TestTime = 5000 }
                .AddCase(() => Array.Sort((T[])array.Clone()), "Array.Sort")
                .AddCase(() => ParallelHelper.Sort(null, ((byte[])backingArray.Clone()).Cast<byte, T>()), $"CastArray<byte, {typeName}>.Sort")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}