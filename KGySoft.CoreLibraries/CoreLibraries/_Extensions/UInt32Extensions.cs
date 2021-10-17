#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: UInt32Extensions.cs
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

using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class UInt32Extensions
    {
        #region Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static uint GetBitMask(this uint value)
        {
            Debug.Assert(value != 0U);
            // In .NET Core 3.0 and above we could use this:
            // return value.IsSingleFlag() ? value - 1 : BitOperations.RoundUpToPowerOf2(value) - 1;
            // But it contains an extra condition and it cannot be always HW accelerated (eg. 32 bit process/CPU),
            // so it is almost always slower than the following software solution (even when HW acceleration is available).

            // This is the SW version of RoundUpToPowerOf2, except the last step,
            // which would add +1 to the result, which we would need to undo anyway.
            // It returns value - 1 for powers of two or the minimal covering bit mask for other values.
            --value;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value;
        }

        #endregion
    }
}