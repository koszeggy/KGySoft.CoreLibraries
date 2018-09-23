using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using KGySoft.Libraries;
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
