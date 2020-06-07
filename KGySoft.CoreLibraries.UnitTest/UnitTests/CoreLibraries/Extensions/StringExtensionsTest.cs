#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringExtensionsTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
using System.Drawing;
using KGySoft.Diagnostics;
using KGySoft.Reflection;
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
                actualResult = (TTarget)source.Parse(typeof(TTarget));
                AssertDeepEquals(expectedResult, actualResult);
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
            Test("1", 1);
            Test("1", (int?)1);
            Test("1.0", 1.0d);
            Test("-0", DoubleExtensions.NegativeZero);
            Test("true", true);
            Test("0", false);
            Test("1980-01-13", new DateTime(1980, 01, 13));
            Test("Black", ConsoleColor.Black);
            Test("1", new IntPtr(1));

            // Registered conversions
            Test("1.2.3.4", new Version(1, 2, 3, 4));
            Test("alpha", "alpha".AsSegment());
        }

        #endregion
    }
}