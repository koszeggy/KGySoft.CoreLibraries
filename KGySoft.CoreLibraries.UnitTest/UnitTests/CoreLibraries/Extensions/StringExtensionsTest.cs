#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringExtensionsTest.cs
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
using System.Globalization;
using System.Linq;

#if !NET35
using System.Numerics;
#endif
#if NETCOREAPP3_0_OR_GREATER
using System.Text;
#endif

#if !NETCOREAPP3_0_OR_GREATER
using KGySoft.ComponentModel;
#endif

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class StringExtensionsTest : TestBase
    {
        #region Methods

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
            Assert.AreEqual(expectedResult, s.RemoveQuotes());
        }

        [TestCaseGeneric(null, null, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("x", null, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("Black", ConsoleColor.Black, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("-1", (ConsoleColor)(-1), TypeArguments = new[] { typeof(ConsoleColor) })]
        public void ToEnumTest<TEnum>(string s, TEnum? expectedResult)
            where TEnum : struct, Enum
        {
            Assert.AreEqual(s.ToEnum<TEnum>(), expectedResult);
        }

        [Test]
        public void ParseTest()
        {
            static void Test<TTarget>(string source, TTarget expectedResult)
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
            Throws<ArgumentNullException>(() => Test(null, 1));

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
            typeof(Version).RegisterTypeConverter<VersionConverter>(); 
#endif
            Test("1.2.3.4", new Version(1, 2, 3, 4));
            Test("alpha", "alpha".AsSegment());
        }

        [TestCase("0123456789aAbBcCdDeEfF")]
        public void ParseHexTest(string s)
        {
            byte[] expected = new byte[s.Length / 2];
            for (int i = 0; i < expected.Length; i++)
                expected[i] = Byte.Parse(s.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            CollectionAssert.AreEqual(expected, s.ParseHexBytes());
        }

        [TestCase(" 0, 1, 23 ,45 ,67 , 89,aA,bB,cC,dD, eE, 0fF ", ",")]
        public void ParseHexWithSeparatorTest(string s, string separator)
        {
            byte[] expected = s.Split(new[] { separator }, StringSplitOptions.None).Select(b => Byte.Parse(b, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            CollectionAssert.AreEqual(expected, s.ParseHexBytes(separator));
        }

        [TestCase(" 0, -0, 1, 23 ,45 ,67 , 89, 254 , 100 , 000099 ", ",")]
        public void ParseDecimalBytesTest(string s, string separator)
        {
            byte[] expected = s.Split(new[] { separator }, StringSplitOptions.None).Select(b => Byte.Parse(b, NumberStyles.Integer, CultureInfo.InvariantCulture)).ToArray();
            CollectionAssert.AreEqual(expected, s.ParseDecimalBytes(separator));
        }

        [TestCase("", 2, "")]
        [TestCase("alpha", 0, "")]
        [TestCase("alpha", 1, "alpha")]
        [TestCase("Alpha", 3, "AlphaAlphaAlpha")]
        public void RepeatTest(string s, int count, string expected)
        {
            Assert.AreEqual(expected, s.Repeat(count));
        }

        [Test]
        public void IndexOfAnyTest()
        {
            const string s = "alpha, beta, gamma";
            Throws<ArgumentException>(() => s.IndexOfAny("delta", null), "Specified argument contains a null element.");
            Assert.AreEqual(0, s.IndexOfAny("delta", ""));
            Assert.AreEqual(-1, s.IndexOfAny("delta", "epsilon"));
            Assert.AreEqual(13, s.IndexOfAny("delta", "gamma"));
        }

        #endregion
    }
}