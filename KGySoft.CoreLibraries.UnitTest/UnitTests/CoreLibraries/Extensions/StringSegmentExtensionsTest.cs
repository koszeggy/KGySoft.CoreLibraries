#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentExtensionsTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;

using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class StringSegmentExtensionsTest : TestBase
    {
        #region Methods

#if AOT
        [OneTimeSetUp]
        public void EnsureAotGenericTests()
        {
            Reflector.MemberOf(() => ToEnumTest<ConsoleColor>(default, default));
        }
#endif

        [Test]
        public void ReadToWhiteSpaceTest()
        {
            StringSegment ss = null;
            AssertAreEqual(StringSegment.Null, ss.ReadToWhiteSpace());

            ss = StringSegment.Empty;
            AssertAreEqual(StringSegment.Empty, ss.ReadToWhiteSpace());
            Assert.IsTrue(ss.IsNull);

            ss = "alpha beta\tgamma\r\ndelta ";
            AssertAreEqual("alpha", ss.ReadToWhiteSpace());
            AssertAreEqual("beta", ss.ReadToWhiteSpace());
            AssertAreEqual("gamma", ss.ReadToWhiteSpace());
            AssertAreEqual(StringSegment.Empty, ss.ReadToWhiteSpace());
            AssertAreEqual("delta", ss.ReadToWhiteSpace());
            AssertAreEqual(StringSegment.Empty, ss.ReadToWhiteSpace());
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorCharTest()
        {
            var sep = ' ';
            StringSegment ss = null;
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(sep));

            ss = StringSegment.Empty;
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma  delta ";
            AssertAreEqual("alpha,", ss.ReadToSeparator(sep));
            AssertAreEqual("beta", ss.ReadToSeparator(sep));
            AssertAreEqual("gamma", ss.ReadToSeparator(sep));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            AssertAreEqual("delta", ss.ReadToSeparator(sep));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorStringSegmentTest()
        {
            StringSegment ss = null;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator(StringSegment.Null));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(StringSegment.Empty));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(" ".AsSegment()));
            Assert.IsTrue(ss.IsNull);
          
            ss = StringSegment.Empty;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator(StringSegment.Null));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(StringSegment.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator(StringSegment.Null));
            AssertAreEqual(" ", ss.ReadToSeparator(StringSegment.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma  delta ";
            StringSegment sep = ", ";
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator(StringSegment.Null));
            AssertAreEqual("alpha", ss.ReadToSeparator(sep));
            AssertAreEqual("beta gamma  delta ", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorStringTest()
        {
            StringSegment ss = null;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((string)null));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(String.Empty));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(" "));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((string)null));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(String.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((string)null));
            AssertAreEqual(" ", ss.ReadToSeparator(String.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma  delta ";
            string sep = ", ";
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((string)null));
            AssertAreEqual("alpha", ss.ReadToSeparator(sep));
            AssertAreEqual("beta gamma  delta ", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorCharArrayTest()
        {
            char[] sep = { ' ', ',' };

            StringSegment ss = null;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((char[])null));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(Reflector.EmptyArray<char>()));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((char[])null));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((char[])null));
            AssertAreEqual(" ", ss.ReadToSeparator(Reflector.EmptyArray<char>()));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta ";
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((char[])null));
            AssertAreEqual("alpha", ss.ReadToSeparator(sep));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            AssertAreEqual("beta", ss.ReadToSeparator(sep));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorStringArrayTest()
        {
            string[] sep = { ", ", " " };

            StringSegment ss = null;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((string[])null));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(Reflector.EmptyArray<string>()));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((string[])null));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((string[])null));
            AssertAreEqual(" ", ss.ReadToSeparator(Reflector.EmptyArray<string>()));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            AssertAreEqual(" ", ss.ReadToSeparator(new string[] { null }));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma,";
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((string[])null));
            AssertAreEqual("alpha", ss.ReadToSeparator(sep));
            AssertAreEqual("beta", ss.ReadToSeparator(sep));
            AssertAreEqual("gamma,", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorStringSegmentArrayTest()
        {
            StringSegment[] sep = { ", ", " " };

            StringSegment ss = null;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((StringSegment[])null));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(Reflector.EmptyArray<StringSegment>()));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((StringSegment[])null));
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((StringSegment[])null));
            AssertAreEqual(" ", ss.ReadToSeparator(Reflector.EmptyArray<StringSegment>()));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            AssertAreEqual(" ", ss.ReadToSeparator(new StringSegment[] { null }));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma,";
            AssertThrows<ArgumentNullException>(() => ss.ReadToSeparator((StringSegment[])null));
            AssertAreEqual("alpha", ss.ReadToSeparator(sep));
            AssertAreEqual("beta", ss.ReadToSeparator(sep));
            AssertAreEqual("gamma,", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

#if NETCOREAPP2_1_OR_GREATER
        [Test]
        public void ReadToSeparatorSpanTest()
        {
            StringSegment ss = null;
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(ReadOnlySpan<char>.Empty));
            AssertAreEqual(StringSegment.Null, ss.ReadToSeparator(" ".AsSpan()));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            AssertAreEqual(StringSegment.Empty, ss.ReadToSeparator(ReadOnlySpan<char>.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            AssertAreEqual(" ", ss.ReadToSeparator(ReadOnlySpan<char>.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma  delta ";
            ReadOnlySpan<char> sep = ", ";
            AssertAreEqual("alpha", ss.ReadToSeparator(sep));
            AssertAreEqual("beta gamma  delta ", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        } 
#endif

        [Test]
        public void ReadLineTest()
        {
            StringSegment ss = null;
            AssertAreEqual(StringSegment.Null, ss.ReadLine());

            ss = StringSegment.Empty;
            AssertAreEqual(StringSegment.Empty, ss.ReadLine());
            Assert.IsTrue(ss.IsNull);

            ss = "Line1\r\nLine2\rLine3\nLine4";
            AssertAreEqual("Line1", ss.ReadLine());
            AssertAreEqual("Line2", ss.ReadLine());
            AssertAreEqual("Line3", ss.ReadLine());
            AssertAreEqual("Line4", ss.ReadLine());
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadTest()
        {
            StringSegment ss = null;
            AssertAreEqual(StringSegment.Null, ss.Read(1));

            ss = StringSegment.Empty;
            AssertAreEqual(StringSegment.Empty, ss.Read(1));
            Assert.IsTrue(ss.IsNull);

            ss = "123";
            AssertAreEqual("1", ss.Read(1));
            AssertAreEqual("23", ss);
            AssertAreEqual("23", ss.Read(10));
            Assert.IsTrue(ss.IsNull);
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
            AssertAreEqual(expectedResult.AsSegment(), s.AsSegment().RemoveQuotes());
        }

#if NETCOREAPP3_0_OR_GREATER
        [TestCase<ConsoleColor>(null, null)]
        [TestCase<ConsoleColor>("x", null)]
        [TestCase<ConsoleColor>("Black", ConsoleColor.Black)]
        [TestCase<ConsoleColor>("-1", (ConsoleColor)(-1))]
#else
        [TestCaseGeneric(null, null, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("x", null, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("Black", ConsoleColor.Black, TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric("-1", (ConsoleColor)(-1), TypeArguments = new[] { typeof(ConsoleColor) })]
#endif
        public void ToEnumTest<TEnum>(string s, TEnum? expectedResult)
            where TEnum : struct, Enum
        {
            AssertAreEqual(s.AsSegment().ToEnum<TEnum>(), expectedResult);
        }

        #endregion
    }
}