using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using KGySoft.Libraries;
using KGySoft.Libraries.Reflection;
using KGySoft.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest
{
    [TestClass]
    public class UnitTest1 : TestBase
    {
        [TestMethod]
        public void TestMethod1()
        {
            //CheckTestingFramework();
            //new TestOperation
            //{
            //    RefOpName = "Cast with boxing",
            //    ReferenceOperation = () => CastNormal<int>(1),
            //    TestOpName = "Cast with typed ref",
            //    TestOperation = () => CastTyperef<int>(1),
            //    Iterations = 10000000,
            //    Repeat = 5
            //}.DoTest();

            byte[] test = new Random(0).NextBytes(1024);

            Console.WriteLine(Crc32.CalculateHash(test));
            Console.WriteLine(Crc32.CalculateHash(test));

            var crc1 = new Crc32(Crc32.StandardPolynomial, 13);
            var crc2 = new Crc32(Crc32.StandardPolynomial, 13);

            Console.WriteLine(BitConverter.ToUInt32(crc1.ComputeHash(test).Reverse().ToArray(), 0));
            Console.WriteLine(BitConverter.ToUInt32(crc2.ComputeHash(test).Reverse().ToArray(), 0));
           
            Console.WriteLine(BitConverter.ToUInt32(crc1.ComputeHash(test).Reverse().ToArray(), 0));
            Console.WriteLine(BitConverter.ToUInt32(crc2.ComputeHash(test).Reverse().ToArray(), 0));

            crc1.TransformBlock(test, 0, 500, null, 0);
            crc1.TransformFinalBlock(test, 500, 524);
            Console.WriteLine(BitConverter.ToUInt32(crc1.Hash.Reverse().ToArray(), 0));
            crc2.TransformBlock(test, 0, 512, null, 0);
            crc2.TransformFinalBlock(test, 512, 512);
            Console.WriteLine(BitConverter.ToUInt32(crc2.Hash.Reverse().ToArray(), 0));

            crc1.Initialize();
            crc2.Initialize();
            crc1.TransformBlock(test, 0, 500, null, 0);
            crc1.TransformFinalBlock(test, 500, 524);
            Console.WriteLine(BitConverter.ToUInt32(crc1.Hash.Reverse().ToArray(), 0));
            crc2.TransformBlock(test, 0, 512, null, 0);
            crc2.TransformFinalBlock(test, 512, 512);
            Console.WriteLine(BitConverter.ToUInt32(crc2.Hash.Reverse().ToArray(), 0));

            new TestOperation
            {
                RefOpName = "A",
                ReferenceOperation = () => Crc32.CalculateHash(test),
                TestOpName = "B",
                TestOperation = () => Crc32.CalculateHash(test),
                Iterations = 10000,
                Repeat = 5,
            }.DoTest();

        }

        //private static T CastNormal<T>(int i)
        //{
        //    return (T)(object)i;
        //}

        //private static T CastTyperef<T>(int i)
        //{
        //    return __refvalue(__makeref(i), T);
        //}
    }
}
