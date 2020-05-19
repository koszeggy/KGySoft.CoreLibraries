#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentExtensionsTest.cs
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
using KGySoft.Diagnostics;
using KGySoft.Reflection;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class StringSegmentExtensionsTest : TestBase
    {
        #region Methods

        [Test]
        public void ReadToWhiteSpaceTest()
        {
            StringSegment ss = null;
            Assert.AreEqual(StringSegment.Null, ss.ReadToWhiteSpace());

            ss = StringSegment.Empty;
            Assert.AreEqual(StringSegment.Empty, ss.ReadToWhiteSpace());
            Assert.IsTrue(ss.IsNull);

            ss = "alpha beta\tgamma\r\ndelta ";
            Assert.AreEqual("alpha", ss.ReadToWhiteSpace());
            Assert.AreEqual("beta", ss.ReadToWhiteSpace());
            Assert.AreEqual("gamma", ss.ReadToWhiteSpace());
            Assert.AreEqual(StringSegment.Empty, ss.ReadToWhiteSpace());
            Assert.AreEqual("delta", ss.ReadToWhiteSpace());
            Assert.AreEqual(StringSegment.Empty, ss.ReadToWhiteSpace());
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorCharTest()
        {
            var sep = ' ';
            StringSegment ss = null;
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(sep));

            ss = StringSegment.Empty;
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma  delta ";
            Assert.AreEqual("alpha,", ss.ReadToSeparator(sep));
            Assert.AreEqual("beta", ss.ReadToSeparator(sep));
            Assert.AreEqual("gamma", ss.ReadToSeparator(sep));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.AreEqual("delta", ss.ReadToSeparator(sep));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorStringSegmentTest()
        {
            StringSegment ss = null;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator(StringSegment.Null));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(StringSegment.Empty));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(" ".AsSegment()));
            Assert.IsTrue(ss.IsNull);
          
            ss = StringSegment.Empty;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator(StringSegment.Null));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(StringSegment.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            Throws<ArgumentNullException>(() => ss.ReadToSeparator(StringSegment.Null));
            Assert.AreEqual(" ", ss.ReadToSeparator(StringSegment.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma  delta ";
            StringSegment sep = ", ";
            Throws<ArgumentNullException>(() => ss.ReadToSeparator(StringSegment.Null));
            Assert.AreEqual("alpha", ss.ReadToSeparator(sep));
            Assert.AreEqual("beta gamma  delta ", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorStringTest()
        {
            StringSegment ss = null;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((string)null));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(String.Empty));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(" "));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((string)null));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(String.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((string)null));
            Assert.AreEqual(" ", ss.ReadToSeparator(String.Empty));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma  delta ";
            string sep = ", ";
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((string)null));
            Assert.AreEqual("alpha", ss.ReadToSeparator(sep));
            Assert.AreEqual("beta gamma  delta ", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorCharArrayTest()
        {
            char[] sep = { ' ', ',' };

            StringSegment ss = null;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((char[])null));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(Reflector.EmptyArray<char>()));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((char[])null));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((char[])null));
            Assert.AreEqual(" ", ss.ReadToSeparator(Reflector.EmptyArray<char>()));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta ";
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((char[])null));
            Assert.AreEqual("alpha", ss.ReadToSeparator(sep));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.AreEqual("beta", ss.ReadToSeparator(sep));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorStringArrayTest()
        {
            string[] sep = { ", ", " " };

            StringSegment ss = null;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((string[])null));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(Reflector.EmptyArray<string>()));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((string[])null));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((string[])null));
            Assert.AreEqual(" ", ss.ReadToSeparator(Reflector.EmptyArray<string>()));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            Assert.AreEqual(" ", ss.ReadToSeparator(new string[] { null }));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma,";
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((string[])null));
            Assert.AreEqual("alpha", ss.ReadToSeparator(sep));
            Assert.AreEqual("beta", ss.ReadToSeparator(sep));
            Assert.AreEqual("gamma,", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadToSeparatorStringSegmentArrayTest()
        {
            StringSegment[] sep = { ", ", " " };

            StringSegment ss = null;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((StringSegment[])null));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(Reflector.EmptyArray<StringSegment>()));
            Assert.AreEqual(StringSegment.Null, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = StringSegment.Empty;
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((StringSegment[])null));
            Assert.AreEqual(StringSegment.Empty, ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((StringSegment[])null));
            Assert.AreEqual(" ", ss.ReadToSeparator(Reflector.EmptyArray<StringSegment>()));
            Assert.IsTrue(ss.IsNull);

            ss = " ".AsSegment();
            Assert.AreEqual(" ", ss.ReadToSeparator(new StringSegment[] { null }));
            Assert.IsTrue(ss.IsNull);

            ss = "alpha, beta gamma,";
            Throws<ArgumentNullException>(() => ss.ReadToSeparator((StringSegment[])null));
            Assert.AreEqual("alpha", ss.ReadToSeparator(sep));
            Assert.AreEqual("beta", ss.ReadToSeparator(sep));
            Assert.AreEqual("gamma,", ss.ReadToSeparator(sep));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadLineTest()
        {
            StringSegment ss = null;
            Assert.AreEqual(StringSegment.Null, ss.ReadLine());

            ss = StringSegment.Empty;
            Assert.AreEqual(StringSegment.Empty, ss.ReadLine());
            Assert.IsTrue(ss.IsNull);

            ss = "Line1\r\nLine2\rLine3\nLine4";
            Assert.AreEqual("Line1", ss.ReadLine());
            Assert.AreEqual("Line2", ss.ReadLine());
            Assert.AreEqual("Line3", ss.ReadLine());
            Assert.AreEqual("Line4", ss.ReadLine());
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadTest()
        {
            StringSegment ss = null;
            Assert.AreEqual(StringSegment.Null, ss.Read(1));

            ss = StringSegment.Empty;
            Assert.AreEqual(StringSegment.Empty, ss.Read(1));
            Assert.IsTrue(ss.IsNull);

            ss = "123";
            Assert.AreEqual("1", ss.Read(1));
            Assert.AreEqual("23", ss);
            Assert.AreEqual("23", ss.Read(10));
            Assert.IsTrue(ss.IsNull);
        }

        #endregion
    }
}