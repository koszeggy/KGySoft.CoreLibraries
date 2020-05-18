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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class StringSegmentExtensionsTest
    {
        #region Methods

        [Test]
        public void ReadTest()
        {
            StringSegment ss = "123";
            Assert.AreEqual("1", ss.Read(1));
            Assert.AreEqual("23", ss);
            Assert.AreEqual("23", ss.Read(10));
            Assert.IsTrue(ss.IsNull);
        }

        [Test]
        public void ReadLineTest()
        {
            StringSegment ss = "Line1\r\nLine2\rLine3\nLine4";
            Assert.AreEqual("Line1", ss.ReadLine());
            Assert.AreEqual("Line2", ss.ReadLine());
            Assert.AreEqual("Line3", ss.ReadLine());
            Assert.AreEqual("Line4", ss.ReadLine());
            Assert.IsTrue(ss.IsNull);
        }

        #endregion
    }
}