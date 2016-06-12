//using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace _PerformanceTest
//{
//    [TestClass]
//    public class UnitTest1: TestBase
//    {
//        [TestMethod]
//        public void TestMethod1()
//        {
//            CheckTestingFramework();
//            DoTest(new TestOperation
//            {
//                RefOpName = "Cast with boxing",
//                RefOp = () => CastNormal<int>(1),
//                CheckOpName = "Cast with typed ref",
//                CheckOp = () => CastTyperef<int>(1),
//                Iterations = 10000000,
//                Repeat = 5
//            });
//        }

//        private static T CastNormal<T>(int i)
//        {
//            return (T)(object)i;
//        }

//        private static T CastTyperef<T>(int i)
//        {
//            return __refvalue(__makeref(i), T);
//        }
//    }
//}
