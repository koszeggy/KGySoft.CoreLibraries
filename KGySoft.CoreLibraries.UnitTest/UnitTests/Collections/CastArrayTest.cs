#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CastArrayTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Diagnostics.CodeAnalysis;

using NUnit.Framework;
using NUnit.Framework.Constraints;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class CastArrayTest
    {
        #region Methods

        [Test]
        public void CastTest()
        {
            // same size
            var boolAsByte = new[] { false, true }.Cast<bool, byte>();
            Assert.AreEqual(2, boolAsByte.Length);
            Assert.AreEqual((byte)0, boolAsByte[0]);
            Assert.IsFalse(boolAsByte.Buffer[0]);
            boolAsByte[0] = 1;
            Assert.AreEqual((byte)1, boolAsByte[0]);
            Assert.IsTrue(boolAsByte.Buffer[0]);
            var boolAsSbyte = boolAsByte.Cast<sbyte>();
            Assert.AreEqual(2, boolAsSbyte.Length);
            Assert.AreEqual((sbyte)1, boolAsSbyte[0]);
            Assert.IsTrue(boolAsSbyte.Buffer[0]);

            // byte-size source
            var byteAsInt = new byte[5].Cast<byte, int>();
            Assert.AreEqual(1, byteAsInt.Length);
            Assert.AreEqual(5, byteAsInt.Buffer.Length);

            // 2 to 4 bytes
            var wordAsInt = new short[5].Cast<short, int>();
            Assert.AreEqual(2, wordAsInt.Length);

            // 4 to 2 bytes
            var intAsWord = new int[5].Cast<int, short>();
            Assert.AreEqual(10, intAsWord.Length);
        }

        [Test]
        public void SliceTest()
        {
            // same size: always works
            var charAsWord = new char[5].Cast<char, ushort>();
            for (int i = 0; i < charAsWord.Length; i++)
            {
                var slice = charAsWord.Slice(i, charAsWord.Length - i);
                Assert.AreEqual(charAsWord.Length - i, slice.Length);
            }

            // byte-size source: always works
            var byteAsDword = new byte[10].Cast<byte, uint>();
            for (int i = 0; i < byteAsDword.Length; i++)
            {
                var slice = byteAsDword.Slice(i, byteAsDword.Length - i);
                Assert.AreEqual(byteAsDword.Length - i, slice.Length);
            }

            // TTo can be divided by TFrom: always works
            var wordAsDword = new ushort[10].Cast<ushort, uint>();
            for (int i = 0; i < wordAsDword.Length; i++)
            {
                var slice = wordAsDword.Slice(i, wordAsDword.Length - i);
                Assert.AreEqual(wordAsDword.Length - i, slice.Length);
            }

            // TTo cannot be divided by TFrom: works only if startIndex is aligned with TFrom
            var wordAsByte = new ushort[10].Cast<ushort, byte>();
            for (int i = 0; i < wordAsByte.Length; i++)
                wordAsByte[i] = (byte)i;

            for (int i = 0; i < wordAsByte.Length; i++)
            {
                [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Not accessed after returning from the call")]
                int Func() => wordAsByte.Slice(i, wordAsByte.Length - i).Length;
                Assert.That((ActualValueDelegate<int>)Func, i % 2 == 0 ? Is.EqualTo(wordAsByte.Length - i) : Throws.ArgumentException);

                // but Span/Memory always works
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                var mem = wordAsByte.AsMemory;
                var span = wordAsByte.AsSpan;
                var sliceMem = mem.Slice(i, mem.Length - i);
                var sliceSpan = span.Slice(i, span.Length - i);
                Assert.IsTrue(sliceSpan.SequenceEqual(sliceMem.Span));
#endif
            }
        }

        #endregion
    }
}
