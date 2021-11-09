#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SpanExtensionsTest.cs
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
using System.Numerics;
#if NETCOREAPP3_0_OR_GREATER
using System.Text;
#endif

#if !NETCOREAPP3_0_OR_GREATER
using KGySoft.ComponentModel;
#endif
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class SpanExtensionsTest : TestBase
    {
        #region Methods

        [Test]
        public void ReadToWhiteSpaceTest()
        {
            ReadOnlySpan<char> ss = null;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToWhiteSpace().ToString());

            ss = ReadOnlySpan<char>.Empty;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToWhiteSpace().ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = "alpha beta\tgamma\r\ndelta ";
            Assert.AreEqual("alpha", ss.ReadToWhiteSpace().ToString());
            Assert.AreEqual("beta", ss.ReadToWhiteSpace().ToString());
            Assert.AreEqual("gamma", ss.ReadToWhiteSpace().ToString());
            Assert.AreEqual("", ss.ReadToWhiteSpace().ToString());
            Assert.AreEqual("delta", ss.ReadToWhiteSpace().ToString());
            Assert.AreEqual("", ss.ReadToWhiteSpace().ToString());
            Assert.IsTrue(ss.IsEmpty);
        }

        [Test]
        public void ReadToSeparatorCharTest()
        {
            var sep = ' ';
            ReadOnlySpan<char> ss = null;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToSeparator(sep).ToString());

            ss = ReadOnlySpan<char>.Empty;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToSeparator(sep).ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = "alpha, beta gamma  delta ";
            Assert.AreEqual("alpha,", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("beta", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("gamma", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("delta", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("", ss.ReadToSeparator(sep).ToString());
            Assert.IsTrue(ss.IsEmpty);
        }

        [Test]
        public void ReadToSeparatorSpanTest()
        {
            ReadOnlySpan<char> ss = null;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToSeparator(ReadOnlySpan<char>.Empty).ToString());
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToSeparator(" ").ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = ReadOnlySpan<char>.Empty;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToSeparator(ReadOnlySpan<char>.Empty).ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = " ".AsSpan();
            Assert.AreEqual(" ", ss.ReadToSeparator(ReadOnlySpan<char>.Empty).ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = "alpha, beta gamma  delta ";
            ReadOnlySpan<char> sep = ", ";
            Assert.AreEqual("alpha", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("beta gamma  delta ", ss.ReadToSeparator(sep).ToString());
            Assert.IsTrue(ss.IsEmpty);
        }

        [Test]
        public void ReadToSeparatorCharArrayTest()
        {
            char[] sep = { ' ', ',' };

            ReadOnlySpan<char> ss = null;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToSeparator(Reflector.EmptyArray<char>()).ToString());
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToSeparator(sep).ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = ReadOnlySpan<char>.Empty;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadToSeparator(sep).ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = " ".AsSpan();
            Assert.AreEqual(" ", ss.ReadToSeparator(Reflector.EmptyArray<char>()).ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = "alpha, beta ";
            Assert.AreEqual("alpha", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("beta", ss.ReadToSeparator(sep).ToString());
            Assert.AreEqual("", ss.ReadToSeparator(sep).ToString());
            Assert.IsTrue(ss.IsEmpty);
        }

        [Test]
        public void ReadLineTest()
        {
            ReadOnlySpan<char> ss = null;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadLine().ToString());

            ss = ReadOnlySpan<char>.Empty;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.ReadLine().ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = "Line1\r\nLine2\rLine3\nLine4";
            Assert.AreEqual("Line1", ss.ReadLine().ToString());
            Assert.AreEqual("Line2", ss.ReadLine().ToString());
            Assert.AreEqual("Line3", ss.ReadLine().ToString());
            Assert.AreEqual("Line4", ss.ReadLine().ToString());
            Assert.IsTrue(ss.IsEmpty);
        }

        [Test]
        public void ReadTest()
        {
            ReadOnlySpan<char> ss = null;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.Read(1).ToString());

            ss = ReadOnlySpan<char>.Empty;
            Assert.AreEqual(ReadOnlySpan<char>.Empty.ToString(), ss.Read(1).ToString());
            Assert.IsTrue(ss.IsEmpty);

            ss = "123";
            Assert.AreEqual("1", ss.Read(1).ToString());
            Assert.AreEqual("23", ss.ToString());
            Assert.AreEqual("23", ss.Read(10).ToString());
            Assert.IsTrue(ss.IsEmpty);
        }

        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("a", "a")]
        [TestCase("alpha", "alpha")]
        [TestCase("\"", "\"")]
        [TestCase("'", "'")]
        [TestCase("'\"", "'\"")]
        [TestCase("\"\"", "")]
        [TestCase("''", "")]
        [TestCase("'a'", "a")]
        [TestCase("\"a\"", "a")]
        public void RemoveQuotesTest(string s, string expectedResult)
        {
            Assert.AreEqual(expectedResult ?? String.Empty, s.AsSpan().RemoveQuotes().ToString());
            Assert.AreEqual(expectedResult?.ToCharArray() ?? Reflector.EmptyArray<char>(), (s?.ToCharArray() ?? Reflector.EmptyArray<char>()).AsSpan().RemoveQuotes().ToArray());
        }

        [TestCaseGeneric(null, null, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("x", null, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("Black", ConsoleColor.Black, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("-1", (ConsoleColor)(-1), TypeArguments = new[] { typeof(ConsoleColor) })]
        public void ToEnumTest<TEnum>(string s, TEnum? expectedResult)
            where TEnum : struct, Enum
        {
            Assert.AreEqual(s.AsSpan().ToEnum<TEnum>(), expectedResult);
        }

        [Test]
        public void ParseTest()
        {
            static void Test<TTarget>(ReadOnlySpan<char> source, TTarget expectedResult)
            {
                Console.Write($"Parse as {typeof(TTarget).GetName(TypeNameKind.ShortName)} ");
                TTarget actualResult = source.Parse<TTarget>();
                AssertDeepEquals(expectedResult, actualResult);
                AssertDeepEquals(expectedResult, (TTarget)source.Parse(typeof(TTarget)));
                Console.WriteLine($"({actualResult?.ToString() ?? "<null>"})");
            }

            // null
            Test(null, (object)null);
            Test(null, (int?)null);
            Throws<ArgumentException>(() => Test(null, 1));

            // string
            Test("1", "1");
            Test("1", (object)"1");

            // Native types
            Test("1", (byte)1);
            Test("1", (sbyte)1);
            Test("1", (short)1);
            Test("1", (ushort)1);
            Test("1", 1);
            Test("1", 1u);
            Test("1", 1L);
            Test("1", (IntPtr)1);
            Test("1", (UIntPtr)1);
            Test("1.25", (byte)1);
            Test("1.25", (sbyte)1);
            Test("1.25", (short)1);
            Test("1.25", (ushort)1);
            Test("1.25", 1);
            Test("1.25", 1u);
            Test("1.25", 1L);
            Test("1.25", (IntPtr)1);
            Test("1.25", (UIntPtr)1);
            Test("1", (int?)1);

            Test("1.0", 1f);
            Test("1.25", 1.25f);
            Test("-0", FloatExtensions.NegativeZero);
            Test("1.0", 1d);
            Test("1.25", 1.25d);
            Test("-0", DoubleExtensions.NegativeZero);
            Test("1.0", 1.0m);
            Test("1.25", 1.25m);
            Test("-0.0", -0.0m);
#if NET5_0_OR_GREATER
            Test("1.0", (Half)1f);
            Test("1.25", (Half)1.25f);
            Test("-0", (Half)(-0f));
#endif

            Test("true", true);
            Test("0", false);
            Test("-1", true);
            Test("a", 'a');
            Test("1980-01-13", new DateTime(1980, 01, 13));
            Test("Black", ConsoleColor.Black);
            Test("1", new IntPtr(1));
#if !NET35
            Test("1", new BigInteger(1));
            Test("1.25", new BigInteger(1));
#endif
#if NETCOREAPP3_0_OR_GREATER && !NETSTANDARD_TEST
            Test("a", new Rune('a'));
            Test("🏯", new Rune("🏯"[0], "🏯"[1]));
#endif
#if NET6_0_OR_GREATER
            Test("1980-01-01", new DateOnly(1980, 01, 01));
            Test("13:13", new TimeOnly(13, 13));
#endif

            // Registered conversions
#if !NETCOREAPP3_0_OR_GREATER
            Throws<ArgumentException>(() => Test("1.2.3.4", new Version(1, 2, 3, 4)));
            typeof(Version).RegisterTypeConverter<VersionConverter>(); 
#endif
            Test("1.2.3.4", new Version(1, 2, 3, 4));
            Test("alpha", "alpha".AsSegment());
        }

        #endregion
    }
}
#endif