#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FloatExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
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

using KGySoft.Libraries.Reflection;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="float">float</see> type.
    /// </summary>
    public static class FloatExtensions
    {
        #region Constants

        /// <summary>
        /// Represents the negative zero value. This value is constant.
        /// </summary>
        public const float NegativeZero = -0f;

        #endregion

        #region Fields

        private static long negativeZeroBits = BitConverter.DoubleToInt64Bits(NegativeZero);

        #endregion

        #region Methods

        /// <summary>
        /// Returns a culture-invariant <see cref="string"/> representation of the given <see cref="float"/> <paramref name="value"/>,
        /// from which the original value can be parsed without losing any information.
        /// </summary>
        /// <param name="value">A <see cref="float"/> value to be converted to <see cref="string"/>.</param>
        /// <returns>A <see cref="float"/> value, from which the original value can be parsed without losing any information.</returns>
        /// <remarks>
        /// The result of this method can be parsed by <see cref="float.Parse(string,IFormatProvider)">Float.Parse</see>; however, to retrieve exactly the
        /// original value, including a negative zero value, use <see cref="Reflector.Parse(Type,string)">Reflector.Parse</see> instead.
        /// </remarks>
        public static string ToRoundtripString(this float value)
        {
            if (IsNegativeZero(value))
                return "-0";
            return value.ToString("R", NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> is negative zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> represents a negative zero value; otherwise, <see langword="false"/>.</returns>
        public static bool IsNegativeZero(this float value)
        {
            return BitConverter.DoubleToInt64Bits(value) == negativeZeroBits;
        }

        #endregion
    }
}
