#if NET11_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Decimal32Extensions.cs
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
    internal static class Decimal32Extensions
    {
        #region Constants

        private const int maxSignificand = 9_999_999; // IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MaxSignificand

        #endregion

        #region Fields

        private static readonly Decimal32 normalizationFactor = Decimal32.One / (Decimal32)maxSignificand;
    
        private static readonly Decimal32 negativeFive = (Decimal32)(-5);
        private static readonly Decimal32 four = (Decimal32)4;
        private static readonly Decimal32 ten = (Decimal32)10;
        private static readonly Decimal32 maxUInt16 = UInt16.MaxValue;
        private static readonly Decimal32 half = (Decimal32)0.5f;
        private static readonly Decimal32 sixteenth = (Decimal32)(1 / 16f);

        #endregion

        #region Properties

        extension(Decimal32)
        {
            internal static int MaxSignificand => maxSignificand;
            internal static Decimal32 NormalizationFactor => normalizationFactor;
            internal static Decimal32 NegativeFive => negativeFive;
            internal static Decimal32 Four => four;
            internal static Decimal32 Ten => ten;
            internal static Decimal32 MaxUInt16 => maxUInt16;
            internal static Decimal32 Half => half;
            internal static Decimal32 Sixteenth => sixteenth;
        }

        #endregion
    }
}
#endif