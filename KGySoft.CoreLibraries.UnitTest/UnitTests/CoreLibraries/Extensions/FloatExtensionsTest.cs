#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FloatExtensionsTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class FloatExtensionsTest
    {
        #region Methods

        [Test]
        public void TolerantZeroTest()
        {
            Assert.IsTrue(Single.Epsilon.TolerantIsZero());
            Assert.IsTrue((-Single.Epsilon).TolerantIsZero());
            Assert.IsFalse(Single.Epsilon.TolerantIsZero(0f));
            Assert.IsFalse((-Single.Epsilon).TolerantIsZero(0f));
            Assert.IsTrue(Single.MaxValue.TolerantIsZero(Single.PositiveInfinity));
        }

        [Test]
        public void TolerantEqualsTest()
        {
            Assert.IsTrue(Single.Epsilon.TolerantEquals(0f));
            Assert.IsTrue((-Single.Epsilon).TolerantEquals(0f));
            Assert.IsFalse(Single.Epsilon.TolerantEquals(0f, 0f));
            Assert.IsFalse((-Single.Epsilon).TolerantEquals(0f, 0f));
            Assert.IsTrue(Single.MinValue.TolerantEquals(Single.MaxValue, Single.PositiveInfinity));
        }

        [Test]
        public void TolerantCeilingFloorTest()
        {
            Assert.AreEqual(0f, Single.Epsilon.TolerantCeiling());
            Assert.AreEqual(0f, Single.Epsilon.TolerantFloor());
            Assert.AreEqual(0f, (-Single.Epsilon).TolerantCeiling());
            Assert.AreEqual(0f, (-Single.Epsilon).TolerantFloor());
            Assert.AreNotEqual(0f, Single.Epsilon.TolerantCeiling(0f));
            Assert.AreNotEqual(0f, (-Single.Epsilon).TolerantFloor(0f));
        }

        #endregion
    }
}
