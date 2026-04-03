#if !(NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MemoryHelperTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests
{
    [TestFixture]
    public class MemoryHelperTest
    {
        #region Methods

        [TestCase(0, 0)] // no misalignment
        [TestCase(4, 0)]
        [TestCase(4, 4)]
        [TestCase(2, 0)]
        [TestCase(2, 2)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        public unsafe void CopyAndCompareTest(int offsetSrc, int offsetDst)
        {
            int testLength = 1024 + 8 + 4 + 2 + 1;
            var src = ThreadSafeRandom.Instance.NextBytes(testLength + offsetSrc);
            var dest = new byte[testLength + offsetDst];

            fixed (byte* pSrc = src)
            fixed (byte* pDest = dest)
            {
                MemoryHelper.CopyMemory(pSrc + offsetSrc, pDest + offsetDst, testLength);
                CollectionAssert.AreEqual(src.AsSection(offsetSrc, testLength), dest.AsSection(offsetDst, testLength));
            }
        }

        #endregion
    }
}
#endif