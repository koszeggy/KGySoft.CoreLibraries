#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializerPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using KGySoft.Collections;
using KGySoft.Serialization.Binary;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Serialization
{
    [TestFixture]
    public class BinarySerializerPerformanceTest
    {
        #region Enumerations

        private enum TestEnum
        {
            One, Two
        }

        #endregion

        #region Fields

        private static readonly object[] serializerTestSource =
        {
            1,
            new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, // multidimensional byte array
            new byte[][] { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23, 24, 25 }, null }, // jagged byte array
            new byte[][,] { new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, new byte[,] { { 11, 12, 13, 14 }, { 21, 22, 23, 24 }, { 31, 32, 33, 34 } } }, // jagged crazy 1
            new byte[,][] { { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23 } }, { new byte[] { 11, 12, 13, 14 }, new byte[] { 21, 22, 23, 24 } } }, // jagged crazy 2
            new List<int>(new int[10]),
            new HashSet<int> { 1, 2, 3 },
            new HashSet<string>(StringComparer.CurrentCulture) { "alpha", "beta", "gamma" },
            new HashSet<TestEnum>(EnumComparer<TestEnum>.Comparer) { TestEnum.One, TestEnum.Two },
            new Queue<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),
            new Stack<int>(new int[] { 1, 2, 3 }),
            new BitArray(new[] { true, false, true }),
            new BitArray[] { new BitArray(new[] { true, false, true }), null },
            new Collection<int>(new int[10]),
            new DictionaryEntry(new object(), "alpha"),
            new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
            new Cache<int, string>() { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
        };

        #endregion

        #region Methods

        [TestCaseSource(nameof(serializerTestSource))]
        public void SerializerTest(object testObj)
        {
            byte[] Serialize(IFormatter formatter, object o)
            {
                using (var ms = new MemoryStream())
                {
#pragma warning disable SYSLIB0011 // Type or member is obsolete - justification: formatter is not necessarily a BinaryFormatter
                    formatter.Serialize(ms, o);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                    return ms.ToArray();
                }
            }

            object Deserialize(IFormatter formatter, byte[] data)
            {
                using (var ms = new MemoryStream(data))
#pragma warning disable SYSLIB0011 // Type or member is obsolete - justification: formatter is not necessarily a BinaryFormatter
                    return formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }

            var bf = new BinaryFormatter();
            var bsf = new BinarySerializationFormatter();

            new PerformanceTest<object> { TestName = $"Binary Serialization/Deserialization Speed Test - {testObj.GetType()}", Iterations = 10000 }
                .AddCase(() => Deserialize(bf, Serialize(bf, testObj)), "BinaryFormatter")
                .AddCase(() => Deserialize(bsf, Serialize(bsf, testObj)), "BinarySerializationFormatter")
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<byte[]> { TestName = $"Binary Serialization Size Test - {testObj.GetType()}", Iterations = 1, SortBySize = true }
                .AddCase(() => Serialize(bf, testObj), "BinaryFormatter")
                .AddCase(() => Serialize(bsf, testObj), "BinarySerializationFormatter")
                .DoTest()
                .DumpResults(Console.Out, dumpReturnValue: true);
        }

        #endregion
    }
}
