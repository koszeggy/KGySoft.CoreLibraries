#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectExtensionsTest.cs
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
using System.Linq;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class ObjectExtensionsTest : TestBase
    {
        #region Methods

        [Test]
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
            Test(100.12, 'x'); // double -> long -> char
            Test(13.45m, ConsoleColor.Magenta); // decimal -> long -> enum

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
            Test(new int[] { 1, 2, 3 }, new List<string> { "1", "2", "3" }); // populatable
            Test(new int[] { 1, 2, 3 }, new ReadOnlyCollection<string>(new List<string> { "1", "2", "3" })); // by ctor, accepts list
#if !NET35
            Test(new int[] { 1, 2, 3 }, new ArraySegment<string>(new[] { "1", "2", "3" })); // by ctor, accepts array
#endif
            Test(new List<int> { 1, 2, 3 }, new ArrayList { 1, 2, 3 }); // gen -> non-gen
            Test(new ArrayList { 1, 2, 3 }, new List<int> { 1, 2, 3 }); // non-gen -> gen

            Test(new Dictionary<int, string> { { 1, "2" }, { 3, "4" } }, new Dictionary<string, int> { { "1", 2 }, { "3", 4 } }); // dictionary, populatable
#if !(NET35 || NET40)
            Test(new Dictionary<int, string> { { 1, "2" }, { 3, "4" } }, new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { { "1", 2 }, { "3", 4 } })); // dictionary, by another dictionary
#endif

            Test(new Dictionary<int, string> { { 1, "2" }, { 3, "4" } }, new Hashtable { { 1, "2" }, { 3, "4" } }); // gen -> non-gen
            Test(new SortedList { { 1, "2" }, { 3, "4" } }, new SortedList<int, string> { { 1, "2" }, { 3, "4" } }); // non-gen -> gen

            // enumerable to string
            Test(new List<char> { 'a', 'b', 'c' }, "abc");
        }

#endregion
    }
}
