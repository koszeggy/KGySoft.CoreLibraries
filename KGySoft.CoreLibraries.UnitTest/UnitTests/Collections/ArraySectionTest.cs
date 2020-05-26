#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySectionTest.cs
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

using KGySoft.Collections;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class ArraySectionTest
    {
        #region Methods

        [Test]
        public void ConversionsNullAndEmptyTest()
        {
            ArraySection<int> section = null;
            Assert.IsTrue(section == null, "Compare with null works due to implicit operator and string comparison");
            Assert.IsNotNull(section, "ArraySection is actually a value type");
            Assert.IsTrue(section.IsNull);
            Assert.IsNull(section.ToArray());

            section = Reflector.EmptyArray<int>();
            Assert.IsTrue(section == Reflector.EmptyArray<int>());
            Assert.IsFalse(section.IsNull);
            Assert.IsTrue(section.IsNullOrEmpty);
            Assert.IsTrue(section.Length == 0);
            Assert.AreEqual(Reflector.EmptyArray<int>(), section.ToArray());
        }

        [Test]
        public void EqualsTest()
        {
            ArraySection<int> section = null;
            Assert.IsTrue(section.Equals(null));
            Assert.IsTrue(section.Equals((object)null));

            section = Reflector.EmptyArray<int>();
            Assert.IsTrue(section.Equals(Reflector.EmptyArray<int>()));
            Assert.IsTrue(section.Equals((object)Reflector.EmptyArray<int>()));

            Assert.AreNotEqual(ArraySection<_>.Null, ArraySection<_>.Empty);
        }

        [Test]
        public void GetHashCodeTest()
        {
            Assert.AreNotEqual(ArraySection<_>.Null.GetHashCode(), ArraySection<_>.Empty.GetHashCode());
        }

        #endregion
    }
}
