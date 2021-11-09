#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DoubleExtensions.cs
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
using System.Globalization;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="double">double</see> type.
    /// </summary>
    public static class DoubleExtensions
    {
        #region Constants

        #region Public Constants
        
        /// <summary>
        /// Represents the negative zero value. This value is constant.
        /// </summary>
        /// <remarks>The value of this constant is <c>-0.0</c>.</remarks>
        public const double NegativeZero = -0d;

        #endregion

        #region Private Constants

        private const double defaultTolerance = 1e-6d;

        #endregion

        #endregion

        #region Fields

        private static readonly long negativeZeroBits = BitConverter.DoubleToInt64Bits(NegativeZero);

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Returns a culture-invariant <see cref="string"/> representation of the given <see cref="double"/>&#160;<paramref name="value"/>,
        /// from which the original value can be parsed without losing any information.
        /// </summary>
        /// <param name="value">A <see cref="double"/> value to be converted to <see cref="string"/>.</param>
        /// <returns>A <see cref="double"/> value, from which the original value can be parsed without losing any information.</returns>
        /// <remarks>
        /// The result of this method can be parsed by <see cref="double.Parse(string,IFormatProvider)">Double.Parse</see>; however, to retrieve exactly the
        /// original value, including a negative zero value, use the <see cref="StringExtensions.Parse{T}(string,CultureInfo)">Parse</see>&#160;<see cref="string"/> extension method instead.
        /// </remarks>
        public static string ToRoundtripString(this double value) => value.ToRoundtripString(NumberFormatInfo.InvariantInfo);

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> is negative zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> represents a negative zero value; otherwise, <see langword="false"/>.</returns>
        public static bool IsNegativeZero(this double value) => BitConverter.DoubleToInt64Bits(value) == negativeZeroBits;

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> can be considered zero using a specific <paramref name="tolerance"/>.
        /// </summary>
        /// <param name="value">The value to be check.</param>
        /// <param name="tolerance">The tolerance to be used. For the best performance its value is not checked but the reasonable value is between 0 and 0.5. This parameter is optional.
        /// <br/>Default value: <c>0.000001</c> (10<sup>-6</sup>).</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="value"/> can be considered zero using the specified <paramref name="tolerance"/>; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool TolerantIsZero(this double value, double tolerance = defaultTolerance) => Math.Abs(value) <= tolerance;

        /// <summary>
        /// Gets whether two <see cref="double">double</see> values are equal considering the specified <paramref name="tolerance"/>.
        /// </summary>
        /// <param name="value">The value to be compared to another one.</param>
        /// <param name="other">The other value compared to the self <paramref name="value"/>.</param>
        /// <param name="tolerance">The tolerance to be used. For the best performance its value is not checked but it should be some low positive value to get a reasonable result. This parameter is optional.
        /// <br/>Default value: <c>0.000001</c> (10<sup>-6</sup>).</param>
        /// <returns><see langword="true"/>, if the values are equal considering the specified <paramref name="tolerance"/>; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool TolerantEquals(this double value, double other, double tolerance = defaultTolerance)
            => TolerantIsZero(value - other, tolerance);

        /// <summary>
        /// Gets the ceiling of the specified <paramref name="value"/> using a specific <paramref name="tolerance"/>.
        /// That is the closest integral number to <paramref name="value"/> if the difference from that is not larger than <paramref name="tolerance"/>;
        /// otherwise, the smallest integral value that is greater than <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value, whose ceiling is abut to be retrieved.</param>
        /// <param name="tolerance">The tolerance to be used. For the best performance its value is not checked but the reasonable value is between 0 and 0.5. This parameter is optional.
        /// <br/>Default value: <c>0.000001</c> (10<sup>-6</sup>).</param>
        /// <returns>The ceiling of the specified <paramref name="value"/> using the specified <paramref name="tolerance"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static double TolerantCeiling(this double value, double tolerance = defaultTolerance)
            => TolerantIsZero(Math.IEEERemainder(value, 1), tolerance) ? Math.Round(value) : Math.Ceiling(value);

        /// <summary>
        /// Gets the floor of the specified <paramref name="value"/> using a specific <paramref name="tolerance"/>.
        /// That is the closest integral number to <paramref name="value"/> if the difference from that is not larger than <paramref name="tolerance"/>;
        /// otherwise, the largest integral value that is less than <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value, whose floor is abut to be retrieved.</param>
        /// <param name="tolerance">The tolerance to be used. For the best performance its value is not checked but the reasonable value is between 0 and 0.5. This parameter is optional.
        /// <br/>Default value: <c>0.000001</c> (10<sup>-6</sup>).</param>
        /// <returns>The floor of the specified <paramref name="value"/> using the specified <paramref name="tolerance"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static double TolerantFloor(this double value, double tolerance = defaultTolerance)
            => TolerantIsZero(Math.IEEERemainder(value, 1), tolerance) ? Math.Round(value) : Math.Floor(value);

        #endregion

        #region Internal Methods

        internal static string ToRoundtripString(this double value, IFormatProvider provider) =>
#if NETCOREAPP3_0_OR_GREATER
            value.ToString("R", provider);
#else
            IsNegativeZero(value) ? "-0" : value.ToString("R", provider);
#endif


        #endregion

        #endregion
    }
}
