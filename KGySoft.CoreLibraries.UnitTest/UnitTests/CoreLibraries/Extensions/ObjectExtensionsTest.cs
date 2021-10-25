#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectExtensionsTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
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
#if !NET35
using System.Numerics;
#endif
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
                Console.Write($"{source?.GetType().GetName(TypeNameKind.ShortName) ?? "<null>"} ({AsString(source)}) -> {typeof(TTarget).GetName(TypeNameKind.ShortName)} ");
                TTarget actualResult = source.Convert<TTarget>();
                AssertDeepEquals(expectedResult, actualResult);
                Console.WriteLine($"({AsString(actualResult)})");
            }

            static string AsString(object obj)
            {
                if (obj == null)
                    return "<null>";

                // KeyValuePair has a similar ToString to this one
                if (obj is DictionaryEntry de)
                    return $"[{de.Key}, {de.Value}]";

                if (obj is not IEnumerable enumerable || enumerable is string)
                    return obj.ToStringInternal(CultureInfo.InvariantCulture);

                return enumerable.Cast<object>().Select(AsString).Join(", ");
            }

            // IConvertible or natively supported
            Test("1", 1);
            Test(1, "1");
            Test(1.0, 1);
            Test(-0f, -0d);
            Test("1", true); // Parse
            Test(100, 'd');
            Test(13m, ConsoleColor.Magenta); // decimal -> string -> ConsoleColor
            Test("12.34", 12.34);
            Test("12.34", 12);
#if !NET35
            Test("12.34", (BigInteger)12);
#endif
#if NET5_0_OR_GREATER
            Test("12.34", (Half)12.34);
#endif

            // TypeConverter
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

            // between enumerable and string
            Test(new List<char> { 'a', 'b', 'c' }, "abc");
            Test(new List<int> { 'a', 'b', 'c' }, "abc");
            Test("abc", new List<char> { 'a', 'b', 'c' });
            Test("abc", new int[] { 'a', 'b', 'c' });

            // between non-convertibles via string
            Test(1L, new IntPtr(1));
            Test(1, new IntPtr(1));
            DateTime now = DateTime.Now;
            Test(now, new DateTimeOffset(now));
            Test(new DateTimeOffset(now), now);
#if !NET35
            Test(1, new BigInteger(1));
            Test(12.34, new BigInteger(12.34));
            Test(new BigInteger(1), 1);
            Test(new BigInteger(12.34), 12d);
            Test(new BigInteger(Double.MaxValue), Double.MaxValue);
#endif
#if NETCOREAPP3_0_OR_GREATER && !NETSTANDARD_TEST
            Test('a', new Rune('a'));
            Test(new Rune('a'), 'a');
            Test("abc", new Rune[] { new Rune('a'), new Rune('b'), new Rune('c') });
            Test(new Rune[] { new Rune('a'), new Rune('b'), new Rune('c') }, "abc");
            Test("🙂x🏯", new Rune[] { Rune.GetRuneAt("🙂", 0), new Rune('x'), Rune.GetRuneAt("🏯", 0) });
            Test(new Rune[] { Rune.GetRuneAt("🙂", 0), new Rune('x'), Rune.GetRuneAt("🏯", 0) }, "🙂x🏯");
            Test(new Rune[] { Rune.GetRuneAt("🙂", 0), new Rune('x'), Rune.GetRuneAt("🏯", 0) }, "🙂x🏯".ToCharArray());
#endif
#if NET5_0_OR_GREATER
            Test(1f, (Half)1f);
            Test((Half)(-0f), -0f);
#endif
#if NET6_0_OR_GREATER
            Test(now, DateOnly.FromDateTime(now));
            Test(now.TimeOfDay, TimeOnly.FromTimeSpan(now.TimeOfDay));
            Test(DateOnly.FromDateTime(now), now.Date);
            Test(TimeOnly.FromDateTime(now), now.TimeOfDay);
#endif

            // Registered conversions
            Throws<ArgumentException>(() => Test(now, now.Ticks));
            Throws<ArgumentException>(() => Test(now, (double)now.Ticks));
            typeof(DateTime).RegisterConversion(typeof(long), (obj, _, _) => ((DateTime)obj).Ticks);
            Test(now, now.Ticks);
            Test(now, (double)now.Ticks); // DateTime -> long -> double
        }

        [Test]
        public void ToInvariantStringRoundtripTest()
        {
            static void Test(object source)
            {
                Type type = source.GetType();
                Assert.IsTrue(type.CanBeParsedNatively(), $"Type {type.GetName(TypeNameKind.ShortName)} cannot be parsed natively");
                Console.Write($"{type.GetName(TypeNameKind.ShortName)} ({source}) -> ");
                string str = source.ToStringInternal(CultureInfo.InvariantCulture);
                Console.WriteLine(str);
                Assert.AreEqual(str, source.Convert<string>());
                object parsed = str.Parse(type);
                Assert.AreEqual(source, parsed);
                Assert.AreEqual(source, str.Convert(type));
                AssertDeepEquals(source, parsed); // bitwise equality such as negative zero
            }

            Test("alpha");
            Test('a');
            Test((byte)1);
            Test((sbyte)1);
            Test((short)1);
            Test((ushort)1);
            Test(1);
            Test(1u);
            Test(1L);
            Test(1UL);
            Test(1d);
            Test(-0d);
            Test(1f);
            Test(-0f);
            Test(1m);
            Test(-0.0m);
            Test(true);
            Test(DateTime.Now);
            Test(DateTime.UtcNow);
            Test(DateTimeOffset.Now);
            Test(DateTimeOffset.UtcNow);
            Test(TimeSpan.FromHours(1));
            Test((IntPtr)1);
            Test((UIntPtr)1);
            Test(ConsoleColor.Blue);
            Test(typeof(ObjectExtensionsTest));
#if !NET35
            Test((BigInteger)1);
#endif
#if NETCOREAPP3_0_OR_GREATER && !NETSTANDARD_TEST
            Test(new Rune('a'));
            Test(new Rune("🏯"[0], "🏯"[1]));
#endif
#if NET5_0_OR_GREATER
            Test((Half)1);
            Test((Half)(-0f));
#endif
#if NET6_0_OR_GREATER
            Test(DateOnly.FromDateTime(DateTime.Today));
            Test(TimeOnly.FromDateTime(DateTime.Now));
#endif
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
