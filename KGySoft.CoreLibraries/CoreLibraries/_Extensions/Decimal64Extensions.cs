#if NET11_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Decimal64Extensions.cs
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

using System;
using System.Numerics;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class Decimal64Extensions
    {
        #region Constants

        private const long maxSignificand = 9_999__999_999__999_999; // IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MaxSignificand

        #endregion

        #region Fields

        private static readonly Decimal64 normalizationFactor = Decimal64.One / (Decimal64)maxSignificand;
    
        private static readonly Decimal64 negativeFive = -5;
        private static readonly Decimal64 four = 4;
        private static readonly Decimal64 ten = 10;
        private static readonly Decimal64 maxUInt16 = UInt16.MaxValue;
        private static readonly Decimal64 half = (Decimal64)0.5f;
        private static readonly Decimal64 sixteenth = (Decimal64)(1 / 16f);

        #endregion

        #region Properties

        extension(Decimal64)
        {
            internal static long MaxSignificand => maxSignificand;
            internal static Decimal64 NormalizationFactor => normalizationFactor;
            internal static Decimal64 NegativeFive => negativeFive;
            internal static Decimal64 Four => four;
            internal static Decimal64 Ten => ten;
            internal static Decimal64 MaxUInt16 => maxUInt16;
            internal static Decimal64 Half => half;
            internal static Decimal64 Sixteenth => sixteenth;
        }

        #endregion
    }
}
#endif