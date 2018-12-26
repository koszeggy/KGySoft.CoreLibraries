using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using KGySoft.Serialization;
using NUnit.Framework;

namespace _PerformanceTest.Tests.Serialization
{
    /// <summary>
    /// Summary description for BinarySerializerTest
    /// </summary>
    [TestFixture]
    public class BinarySerializerPerformanceTest
    {
        private static void DoTestBinarySerialize(object x)
        {
            const int iterations = 100000;
            Console.WriteLine("=========={0} (iterations: {1:N0})===========", x.GetType(), iterations);

            byte[] raw = new byte[0];
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                raw = BinarySerializer.Serialize(x, BinarySerializationOptions.RecursiveSerializationAsFallback);
                var y = BinarySerializer.Deserialize(raw);
            }
            watch.Stop();
            decimal time1 = watch.ElapsedMilliseconds;
            decimal size1 = raw.Length;
            Console.WriteLine("BinarySerializer time: " + time1);
            Console.WriteLine("BinarySerializer size: " + size1);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, x);
                raw = ms.ToArray();

                ms = new MemoryStream(raw);
                var y = bf.Deserialize(ms);

            }
            watch.Stop();
            Console.WriteLine("BinaryFormatter time: " + watch.ElapsedMilliseconds);
            Console.WriteLine("BinaryFormatter size: " + raw.Length);
            Console.WriteLine("Time performance: {0:P2}", time1 / watch.ElapsedMilliseconds);
            Console.WriteLine("Size performance: {0:P2}", size1 / raw.Length);
        }

        [Test]
        public void SerializerTest()
        {
            //var x = new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }; // multidimensional byte array
            //var x = new byte[][] { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23, 24, 25 }, null }; // jagged byte array
            //var x = Array.CreateInstance(typeof(byte), new int[] {3}, new int[]{-1}); // non-zero based array
            //var x = new byte[][,] { new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, new byte[,] { { 11, 12, 13, 14 }, { 21, 22, 23, 24 }, { 31, 32, 33, 34 } } }; // jagged crazy 1
            //var x = new byte[,][] { { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23 } }, { new byte[] { 11, 12, 13, 14 }, new byte[] { 21, 22, 23, 24 } } }; // jagged crazy 2
            //var x = 1;
            //var x = new List<int>(new int[10]);
            //var x = new HashSet<int> {1, 2, 3};
            //var x = new HashSet<string>(StringComparer.CurrentCulture) { "kerek", "kerék", "kérek" };
            //var x = new HashSet<TestEnum>(EnumComparer<TestEnum>.Comparer) { TestEnum.One, TestEnum.Two };
            //var x = new Queue<int[]>(new int[][] { new int[] { 1, 2, 3 }, null });
            //var x = new Stack<int>(new int[] { 1, 2, 3 });
            //var x = new BitArray(new[] {true, false, true});
            //var x = new BitArray[]{ new BitArray(new[] {true, false, true}), null };
            //var x = new Collection<int>(new int[10]);
            //var x = new DictionaryEntry(new object(), "alma");
            var x = new Dictionary<int, string> { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } };
            //var x = new Cache<int, string>(Cache<int,string>.NullLoader) { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } };
            DoTestBinarySerialize(x);
        }
    }
}
