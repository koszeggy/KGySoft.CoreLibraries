using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KGySoft.CoreLibraries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Tests.CoreLibraries.Extensions
{
    [TestClass]
    public class ObjectExtensionsTest : TestBase
    {
        [TestMethod]
        public void ConvertTest()
        {
            void Test<TTarget>(object source, TTarget expectedResult)
            {
                TTarget actualResult = source.Convert<TTarget>();
                AssertMembersAndItemsEqual(expectedResult, actualResult);
            }

            // IConvertible
            Test("1", 1);
            Test(1, "1");
            Test(1.0, 1);
            Test("1", true); // Parse

            // Registered conversions
            Throws<ArgumentException>(() => Test(1L, new IntPtr(1)));
            Throws<ArgumentException>(() => Test(1, new IntPtr(1)));
            typeof(long).RegisterConversion(typeof(IntPtr), (obj, type, culture) => new IntPtr((long)obj));
            Test(1L, new IntPtr(1));
            Test(1, new IntPtr(1)); // int -> long -> IntPtr

            // to array
            Test(new int[] { 1, 2, 3 }, new long[] { 1, 2, 3 }); // between arrays
            Test(new int[,] { { 1, 2 }, { 3, 4 } }, new long[,] { { 1, 2 }, { 3, 4 } }); // multidimensional array
            Test(new List<int> { 1, 2, 3 }, new long[] { 1, 2, 3 }); // from known length to array
            Test(new List<int> { 1, 2, 3 }.Where(_ => true), new long[] { 1, 2, 3 }); // from unknown length to array

            // to collection
            Test(new int[] { 1, 2, 3 }, new List<string>{"1", "2", "3"}); // populatable
            Test(new int[] { 1, 2, 3 }, new ReadOnlyCollection<string>(new List<string>{ "1", "2", "3" })); // by ctor, accepts list
            Test(new int[] { 1, 2, 3 }, new ArraySegment<string>(new []{ "1", "2", "3" })); // by ctor, accepts array
            Test(new List<int> { 1, 2, 3 }, new ArrayList { 1, 2, 3 }); // gen -> non-gen
            Test(new ArrayList { 1, 2, 3 }, new List<int> { 1, 2, 3 }); // non-gen -> gen

            Test(new Dictionary<int, string>{{1, "2"}, {3, "4"}}, new Dictionary<string, int> { { "1", 2 }, { "3", 4 } }); // dictionary, populatable
#if !(NET35 || NET40)
            Test(new Dictionary<int, string>{{1, "2"}, {3, "4"}}, new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { { "1", 2 }, { "3", 4 } })); // dictionary, by another dictionary
#endif
            Test(new Dictionary<int, string> { { 1, "2" }, { 3, "4" } }, new Hashtable { { 1, "2" }, { 3, "4" } }); // gen -> non-gen
            Test(new Hashtable { { 1, "2" }, { 3, "4" } }, new Dictionary<int, string> { { 1, "2" }, { 3, "4" } }); // non-gen -> gen

            // enumerable to string
            Test(new List<char> { 'a', 'b', 'c' }, "abc");
        }
    }
}
