using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using KGySoft.Libraries;
using KGySoft.Libraries.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest
{
    [TestClass]
    public class UnitTest1 : TestBase
    {
        [TestMethod]
        public void TestMethod1()
        {
            CheckTestingFramework();
            //new TestOperation
            //{
            //    RefOpName = "Cast with boxing",
            //    ReferenceOperation = () => CastNormal<int>(1),
            //    TestOpName = "Cast with typed ref",
            //    TestOperation = () => CastTyperef<int>(1),
            //    Iterations = 10000000,
            //    Repeat = 5
            //}.DoTest();

            //Array array = Array.CreateInstance(typeof(int), new[] { 10 }, new[] { 1 });
            //for (int i = 1; i <= 10; i++)
            //    array.SetValue(i, i);
            //var dest = new int[10];

            //new TestOperation
            //{
            //    RefOpName = "Primitive copy",
            //    TestOpName = "Regular copy",
            //    ReferenceOperation = () => Buffer.BlockCopy(array, 0, dest, 0, 40),
            //    TestOperation = () => Array.Copy(array, 1, dest, 0, 10),
            //    Iterations = 100000,
            //    Repeat = 5
            //}.DoTest();

        }

        private class MyType
        {
            public int X, Y;

            public MyType()
            {
                X = 1;
                Y = 2;
            }
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
