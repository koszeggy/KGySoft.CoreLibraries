#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: UInt32Extensions.cs
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

#if NET6_0_OR_GREATER
using System;
using System.Numerics;
#endif
using System.Runtime.CompilerServices;
#if NET5_0_OR_GREATER
using System.Runtime.Intrinsics.Arm;
#endif
#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class UInt32Extensions
    {
        #region Methods

        /// <summary>
        /// Gets a mask for generating random values excluding the upper bound.
        /// Returns value - 1 for powers of two, or the minimal covering bit mask for other values.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static uint GetBitMaskExclusive(this uint value)
        {
            // In .NET Core 3.0 and above we could use BitOperations.RoundUpToPowerOf2(value) - 1,
            // or BitOperations.IsPow2(value) ? value - 1 : UInt64.MaxValue >> BitOperations.LeadingZeroCount(value).
            // But as there is no BitOperations.IsAccelerated, we must check the possible HW supported ways at low level,
            // because their SW fallback contain more unnecessary steps than our fallback below.
            // When HW support is available, we prefer LeadingZeroCount, because RoundUpToPowerOf2 uses that in the background as well.
            Debug.Assert(value != 0UL);
#if NET6_0_OR_GREATER
            if (Lzcnt.IsSupported)
                return BitOperations.IsPow2(value) ? value - 1 : UInt32.MaxValue >> (int)Lzcnt.LeadingZeroCount(value);
#endif
#if NET5_0_OR_GREATER
            if (ArmBase.IsSupported)
                return BitOperations.IsPow2(value) ? value - 1 : UInt32.MaxValue >> ArmBase.LeadingZeroCount(value);
#endif

            // This is the SW version of RoundUpToPowerOf2, except the last step,
            // which would add +1 to the result, which we would need to undo anyway.
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