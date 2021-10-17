#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: UInt64Extensions.cs
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
#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
#endif
using System.Runtime.CompilerServices;
using System.Security;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class UInt64Extensions
    {
        #region Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static bool IsSingleFlag(this ulong value) => value != 0 && (value & (value - 1UL)) == 0UL;

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetFlagsCount(this ulong value)
        {
#if NETCOREAPP3_0_OR_GREATER
            return BitOperations.PopCount(value);
#else
            // There are actually better general solutions than this but for enums we usually expect
            // only a few flags set. Up to 3-4 flags this solution is faster than the optimal Hamming weight solution.
            int result = 0;
            while (value != 0)
            {
                result++;
                value &= value - 1;
            }

            return result;
#endif
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static ulong GetBitMask(this ulong value)
        {
            Debug.Assert(value != 0UL);
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
            value |= value >> 32;
            return value;
        }

        internal static int DecimalDigitsCount(this ulong value)
        {
            return value >= 10000000000000000000UL ? 20
                : value >= 1000000000000000000UL ? 19
                : value >= 100000000000000000UL ? 18
                : value >= 10000000000000000UL ? 17
                : value >= 1000000000000000UL ? 16
                : value >= 100000000000000UL ? 15
                : value >= 10000000000000UL ? 14
                : value >= 1000000000000UL ? 13
                : value >= 100000000000UL ? 12
                : value >= 10000000000UL ? 11
                : value >= 1000000000UL ? 10
                : value >= 100000000UL ? 9
                : value >= 10000000UL ? 8
                : value >= 1000000UL ? 7
                : value >= 100000UL ? 6
                : value >= 10000UL ? 5
                : value >= 1000UL ? 4
                : value >= 100UL ? 3
                : value >= 10UL ? 2
                : 1;
        }

        [SecuritySafeCritical]
        internal static unsafe string QuickToString(this ulong value, bool isNegative)
        {
            if (value == 0)
                return "0";

            char* buf = stackalloc char[21];
            int size = 0;
            while (value > 0)
            {
                buf[size] = (char)(value % 10 + '0');
                size += 1;
                value /= 10;
            }

            if (isNegative)
            {
                buf[size] = '-';
                size += 1;
            }

            string result = new String('\0', size);
            fixed (char* s = result)
            {
                for (int i = size - 1; i >= 0; i--)
                    s[size - i - 1] = buf[i];
            }

            return result;
        }

        #endregion
    }
}
