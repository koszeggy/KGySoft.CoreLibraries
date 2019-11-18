#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: UInt64Extensions.cs
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

using System;
using System.Diagnostics;
#if NETCOREAPP3_0
using System.Numerics;
#endif
using System.Runtime.CompilerServices; 

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class UInt64Extensions
    {
        #region Methods

#if !(NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static bool IsSingleFlag(this ulong value) => value != 0 && (value & (value - 1UL)) == 0UL;

#if !(NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static int GetFlagsCount(this ulong value)
        {
#if NET35 || NET40 || NET45 || NET472 || NETSTANDARD2_0 || NETCOREAPP2_0
            // There are actually better general solutions than this but for enums we usually expect
            // only a few flags set. Up to 3-4 flags this solution is faster than the optimal Hamming weight solution.
            int result = 0;
            while (value != 0)
            {
                result++;
                value &= value - 1;
            }

            return result;
#else
            return BitOperations.PopCount(value);
#endif
        }

        internal static unsafe string QuickToString(this ulong value, bool isNegative)
        {
            if (value == 0)
                return "0";

            char* buf = stackalloc char[20];
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

        internal static unsafe void QuickToString(this ulong value, bool isNegative, int size, char* target)
        {
            if (value == 0)
            {
                *target = '0';
                return;
            }

            while (value > 0)
            {
                target[--size] = (char)(value % 10 + '0');
                value /= 10;
            }

            if (isNegative)
                target[--size] = '-';

            Debug.Assert(size == 0, "Invalid size was passed to QuickToString");
        }

        #endregion
    }
}
