#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries
{
    [TestFixture]
    public class StringSegmentTest
    {
        #region Methods

        [Test]
        public void IndexOf()
        {
            Assert.AreEqual(0, new StringSegment(" ").IndexOf(" "));
            Assert.AreEqual(1, new StringSegment(" ,, ").IndexOf(","));
            Assert.AreEqual(2, new StringSegment(" ,, ").IndexOf(", "));
            Assert.AreEqual(1, new StringSegment(" ,., ").IndexOf(","));
            Assert.AreEqual(3, new StringSegment(" ,., ").IndexOf(", "));

            Assert.AreEqual(0, new StringSegment("  ", 1).IndexOf(" "));
            Assert.AreEqual(1, new StringSegment("  ,, ", 1).IndexOf(","));
            Assert.AreEqual(2, new StringSegment("  ,, ", 1).IndexOf(", "));
            Assert.AreEqual(1, new StringSegment("  ,., ", 1).IndexOf(","));
            Assert.AreEqual(3, new StringSegment("  ,., ", 1).IndexOf(", "));
        }

        #endregion
    }
}