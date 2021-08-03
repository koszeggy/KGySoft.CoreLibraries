#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DoubleExtensionsTest.cs
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
    public class DoubleExtensionsTest
    {
        #region Methods

        [Test]
        public void TolerantZeroTest()
        {
            Assert.IsTrue(Double.Epsilon.TolerantIsZero());
            Assert.IsTrue((-Double.Epsilon).TolerantIsZero());
            Assert.IsFalse(Double.Epsilon.TolerantIsZero(0d));
            Assert.IsFalse((-Double.Epsilon).TolerantIsZero(0d));
            Assert.IsTrue(Double.MaxValue.TolerantIsZero(Double.PositiveInfinity));
        }

        [Test]
        public void TolerantEqualsTest()
        {
            Assert.IsTrue(Double.Epsilon.TolerantEquals(0d));
            Assert.IsTrue((-Double.Epsilon).TolerantEquals(0d));
            Assert.IsFalse(Double.Epsilon.TolerantEquals(0d, 0d));
            Assert.IsFalse((-Double.Epsilon).TolerantEquals(0d, 0d));
            Assert.IsTrue(Double.MinValue.TolerantEquals(Double.MaxValue, Double.PositiveInfinity));
        }

        [Test]
        public void TolerantCeilingFloorTest()
        {
            Assert.AreEqual(0d, Double.Epsilon.TolerantCeiling());
            Assert.AreEqual(0d, Double.Epsilon.TolerantFloor());
            Assert.AreEqual(0d, (-Double.Epsilon).TolerantCeiling());
            Assert.AreEqual(0d, (-Double.Epsilon).TolerantFloor());
            Assert.AreNotEqual(0d, Double.Epsilon.TolerantCeiling(0d));
            Assert.AreNotEqual(0d, (-Double.Epsilon).TolerantFloor(0d));
        }

        #endregion
    }
}
