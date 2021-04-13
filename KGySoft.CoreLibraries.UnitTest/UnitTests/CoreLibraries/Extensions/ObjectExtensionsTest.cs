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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class ObjectExtensionsTest : TestBase
    {
        #region Nested Types

        #region UnsafeStruct struct

        [Serializable]
        private unsafe struct UnsafeStruct
        {
            #region Fields

            public void* VoidPointer;
            public int* IntPointer;
            public int*[] PointerArray;
            public void** PointerOfPointer;

            #endregion
        }

        #endregion


        #endregion

        #region Fields

        private static readonly unsafe object[] deepCloneBySerializerTestSource =
        {
            // natively supported types
            null,
            1,
            typeof(int),
            new List<int> { 1 },

            // custom serializable type
            new Exception("message"),

            // non serializable
            new BitVector32(13),

            // not serializable in .NET Core
            CultureInfo.GetCultureInfo("en-US"),
            new Collection<Encoding> { Encoding.ASCII, Encoding.Unicode },
            new MemoryStream(new byte[] { 1, 2, 3 }),

            // pointer fields
            new UnsafeStruct
            {
                VoidPointer = (void*)new IntPtr(1),
                IntPointer = (int*)new IntPtr(1),
                PointerArray = null, // new int*[] { (int*)new IntPtr(1), null }, // - not supported
                PointerOfPointer = (void**)new IntPtr(1)
            },
        };

        private static readonly unsafe object[] deepCloneByObjectClonerTestSource =
        {
            // not cloned types
            null,
            1,
            "string",
            ConsoleColor.Blue,
            typeof(int),
            new Func<int, string>(i => i.ToString()),
            new BitVector32(13),

            // contains array
            new List<int> { 1 },
            new object[] { 1, "alpha", ConsoleColor.Blue, null },
            new object[,] { { 1, "alpha" }, { ConsoleColor.Blue, null } },

            // complex types
            new Exception("message"),
            CultureInfo.GetCultureInfo("en-US"),
            new Collection<Encoding> { Encoding.ASCII, Encoding.Unicode },
            new MemoryStream(new byte[] { 1, 2, 3 }),

            // contains delegate
            new Cache<int, string>(i => i.ToString()),

            // pointer fields
            new UnsafeStruct
            {
                VoidPointer = (void*)new IntPtr(1),
                IntPointer = (int*)new IntPtr(1),
                PointerArray = null, // new int*[] { (int*)new IntPtr(1), null }, // - clone by ObjectCloner works but equality check fails because enumeration is not supported
                PointerOfPointer = (void**)new IntPtr(1)
            },
        };

        #endregion

        #region Methods

        [Test]
        public void ConvertTest()
        {
            static void Test<TTarget>(object source, TTarget expectedResult)
            {
                Console.Write($"{source?.GetType().GetName(TypeNameKind.ShortName) ?? "<null>"} ({source}) -> {typeof(TTarget).GetName(TypeNameKind.ShortName)} ");
                TTarget actualResult = source.Convert<TTarget>();
                AssertDeepEquals(expectedResult, actualResult);
                Console.WriteLine($"({actualResult?.ToString() ?? "<null>"})");
            }

            // IConvertible
            Test("1", 1);
            Test(1, "1");
            Test(1.0, 1);
            Test("1", true); // Parse
            Test(100.12, 'd'); // double -> long -> char
            Test(13.45m, ConsoleColor.Magenta); // decimal -> long -> enum

            // Registered conversions
            Throws<ArgumentException>(() => Test(1L, new IntPtr(1)));
            Throws<ArgumentException>(() => Test(1, new IntPtr(1)));
            typeof(long).RegisterConversion(typeof(IntPtr), (obj, type, culture) => new IntPtr((long)obj));
            Test(1L, new IntPtr(1));
            Test(1, new IntPtr(1)); // int -> long -> IntPtr
            Test("1", "1".AsSegment());
            Test("1".AsSegment(), "1");

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
            Test(new int[] { 1, 2, 3 }, new ArraySection<string>(new[] { "1", "2", "3" })); // by ctor, accepts array
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

#if !NETFRAMEWORK
        [Obsolete] 
#endif
        [TestCaseSource(nameof(deepCloneBySerializerTestSource))]
        public void DeepCloneBySerializerTest(object obj)
        {
            AssertDeepEquals(obj, obj.DeepClone());
            AssertDeepEquals(obj, obj.DeepClone(true));
        }

        [TestCaseSource(nameof(deepCloneByObjectClonerTestSource))]
        public void DeepCloneByObjectClonerTest(object obj)
        {
            AssertDeepEquals(obj, obj.DeepClone(null));
        }

        [Test]
        public void DeepCloneByObjectClonerCircularReferenceTest()
        {
            var obj = new object[1];
            obj[0] = obj;

            object[] clone = obj.DeepClone(null);
            AssertDeepEquals(obj, clone);
            Assert.AreSame(clone, clone[0]);
        }

        [Test]
        public void CustomCloneTest()
        {
            static object CustomClone(object o) => o switch
            {
                int i => i + 1,
                string s => s + "_clone",
                _ => null
            };

            object[] obj = { 1, "alpha", null, new object() };
            var clone = obj.DeepClone(CustomClone);

            Assert.AreEqual(2,  clone[0]);
            Assert.AreEqual("alpha_clone",  clone[1]);
            Assert.IsNull(clone[2]);
            Assert.IsTrue(clone[3].GetType() == typeof(object));
        }

        #endregion
    }
}
