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

            //new TestOperation
            //{
            //    RefOpName = "Without name cache",
            //    TestOpName = "With name cache",
            //    ReferenceOperation = () => typeof(IList<int>).IsImplementationOfGenericType(typeof(List<>)),
            //    TestOperation = () => typeof(IList<int>).IsImplementationOfGenericType2(typeof(List<>)),
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
