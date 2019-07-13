#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DoubleExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.Globalization;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="double">double</see> type.
    /// </summary>
    public static class DoubleExtensions
    {
        #region Constants

        /// <summary>
        /// Represents the negative zero value. This value is constant.
        /// </summary>
        /// <remarks>The value of this constant is <c>-0.0</c>.</remarks>
        public const double NegativeZero = -0d;

        #endregion

        #region Fields

        private static long negativeZeroBits = BitConverter.DoubleToInt64Bits(NegativeZero);

        #endregion

        #region Methods

        /// <summary>
        /// Returns a culture-invariant <see cref="string"/> representation of the given <see cref="double"/>&#160;<paramref name="value"/>,
        /// from which the original value can be parsed without losing any information.
        /// </summary>
        /// <param name="value">A <see cref="double"/> value to be converted to <see cref="string"/>.</param>
        /// <returns>A <see cref="double"/> value, from which the original value can be parsed without losing any information.</returns>
        /// <remarks>
        /// The result of this method can be parsed by <see cref="double.Parse(string,IFormatProvider)">Double.Parse</see>; however, to retrieve exactly the
        /// original value, including a negative zero value, use the <see cref="StringExtensions.Parse">Parse</see>&#160;<see cref="string"/> extension method instead.
        /// </remarks>
        public static string ToRoundtripString(this double value) 
            => IsNegativeZero(value) ? "-0" : value.ToString("R", NumberFormatInfo.InvariantInfo);

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> is negative zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> represents a negative zero value; otherwise, <see langword="false"/>.</returns>
        public static bool IsNegativeZero(this double value) => BitConverter.DoubleToInt64Bits(value) == negativeZeroBits;

        #endregion
    }
}
