#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Int32Extensions.cs
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

namespace KGySoft.CoreLibraries
{
    internal static class Int32Extensions
    {
        #region Methods

        internal static int GetNextPowerOfTwo(this int minValue)
        {
            // if already power of 2:
            if (minValue > 0 && (minValue & (minValue - 1)) == 0)
                return minValue;

            // 0x40000000 (1 << 31) is a negative number
            if (minValue >= 0x40000000)
                return 0x40000000;

            int result = 2;
            while (result < minValue)
                result <<= 1;

            return result;
        }

        #endregion
    }
}