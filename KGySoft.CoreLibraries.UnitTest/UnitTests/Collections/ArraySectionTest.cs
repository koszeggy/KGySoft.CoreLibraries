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

using System;
using System.Linq;
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

        [Test]
        public void SliceTest()
        {
            ArraySection<int> section = Enumerable.Range(0, 10).ToArray();
            ArraySection<int> subsection = section.Slice(1, 2);

            Assert.AreEqual(1, subsection[0]);
            Assert.AreEqual(2, subsection.Length);

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            Span<int> span = section.AsSpan;
            Assert.AreEqual(span.Slice(1, 2).ToArray(), subsection);
            Assert.AreEqual(span[1..^1].ToArray(), section[1..^1]);
#endif
        }

        [Test]
        public void SerializationTest()
        {
            var section = ArraySection<int>.Null;
            Assert.AreEqual(section, section.DeepClone());

            section = new[] { 1, 2, 3, 4, 5 }.AsSection(1, 3);
            Assert.IsTrue(section.SequenceEqual(section.DeepClone()));
        }

        #endregion
    }
}
