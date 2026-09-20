#if NET11_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Decimal128Extensions.cs
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
    internal static class Decimal128Extensions
    {
        #region Constants

        private const long maxSignificandUpper = 0x0001_ED09_BEAD_87C0;
        private const long maxSignificandLower = 0x378D_8E63_FFFF_FFFF;

        #endregion

        #region Fields

        // IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MaxSignificand: 9_999__999_999__999_999__999_999__999_999__999_999
        private static readonly UInt128 maxSignificand = new(maxSignificandUpper, maxSignificandLower);
        private static readonly UInt128 maxSignificandP1 = maxSignificand + 1;
        private static readonly Decimal128 normalizationFactor = Decimal128.One / (Decimal128)maxSignificand;
    
        private static readonly Decimal128 negativeFive = -5;
        private static readonly Decimal128 four = 4;
        private static readonly Decimal128 ten = 10;
        private static readonly Decimal128 maxUInt16 = UInt16.MaxValue;
        private static readonly Decimal128 half = 0.5m;
        private static readonly Decimal128 sixteenth = (Decimal128)(1 / 16f);

        #endregion

        #region Properties

        extension(Decimal128)
        {
            internal static UInt128 MaxSignificandP1 => maxSignificandP1;
            internal static Decimal128 NormalizationFactor => normalizationFactor;
            internal static Decimal128 NegativeFive => negativeFive;
            internal static Decimal128 Four => four;
            internal static Decimal128 Ten => ten;
            internal static Decimal128 MaxUInt16 => maxUInt16;
            internal static Decimal128 Half => half;
            internal static Decimal128 Sixteenth => sixteenth;
        }

        #endregion
    }
}
#endif