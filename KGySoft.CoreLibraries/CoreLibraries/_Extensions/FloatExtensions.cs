#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FloatExtensions.cs
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
    /// Provides extension methods for the <see cref="float">float</see> type.
    /// </summary>
    public static class FloatExtensions
    {
        #region Constants

        #region Public Constants

        /// <summary>
        /// Represents the negative zero value. This value is constant.
        /// </summary>
        /// <remarks>The value of this constant is <c>-0.0</c>.</remarks>
        public const float NegativeZero = -0f;

        #endregion

        #region Private Constants

        private const float defaultTolerance = 1e-6f;

        #endregion

        #endregion

        #region Fields

#if NETFRAMEWORK || NETSTANDARD2_0
        private static readonly long negativeZeroBits = BitConverter.DoubleToInt64Bits(NegativeZero);
#else
        private static readonly int negativeZeroBits = BitConverter.SingleToInt32Bits(NegativeZero);
#endif

        #endregion

        #region Methods
        
        #region Public Methods

        /// <summary>
        /// Returns a culture-invariant <see cref="string"/> representation of the given <see cref="float"/>&#160;<paramref name="value"/>,
        /// from which the original value can be parsed without losing any information.
        /// </summary>
        /// <param name="value">A <see cref="float"/> value to be converted to <see cref="string"/>.</param>
        /// <returns>A <see cref="float"/> value, from which the original value can be parsed without losing any information.</returns>
        /// <remarks>
        /// The result of this method can be parsed by <see cref="float.Parse(string,IFormatProvider)">Float.Parse</see>; however, to retrieve exactly the
        /// original value, including a negative zero value, use the <see cref="StringExtensions.Parse{T}(string,CultureInfo)">Parse</see>&#160;<see cref="string"/> extension method instead.
        /// </remarks>
        public static string ToRoundtripString(this float value) => value.ToRoundtripString(NumberFormatInfo.InvariantInfo);

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> is negative zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> represents a negative zero value; otherwise, <see langword="false"/>.</returns>
        public static bool IsNegativeZero(this float value) =>
#if NETFRAMEWORK || NETSTANDARD2_0
            BitConverter.DoubleToInt64Bits(value) == negativeZeroBits;
#else
            BitConverter.SingleToInt32Bits(value) == negativeZeroBits;
#endif

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> can be considered zero using a specific <paramref name="tolerance"/>.
        /// </summary>
        /// <param name="value">The value to be check.</param>
        /// <param name="tolerance">The tolerance to be used. For the best performance its value is not checked but the reasonable value is between 0 and 0.5. This parameter is optional.
        /// <br/>Default value: <c>0.000001</c> (10<sup>-6</sup>).</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="value"/> can be considered zero using the specified <paramref name="tolerance"/>; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool TolerantIsZero(this float value, float tolerance = defaultTolerance) =>
#if NETFRAMEWORK || NETSTANDARD2_0
            Math.Abs(value) <= tolerance;
#else
            MathF.Abs(value) <= tolerance;
#endif

        /// <summary>
        /// Gets whether two <see cref="float">float</see> values are equal considering the specified <paramref name="tolerance"/>.
        /// </summary>
        /// <param name="value">The value to be compared to another one.</param>
        /// <param name="other">The other value compared to the self <paramref name="value"/>.</param>
        /// <param name="tolerance">The tolerance to be used. For the best performance its value is not checked but it should be some low positive value to get a reasonable result. This parameter is optional.
        /// <br/>Default value: <c>0.000001</c> (10<sup>-6</sup>).</param>
        /// <returns><see langword="true"/>, if the values are equal considering the specified <paramref name="tolerance"/>; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool TolerantEquals(this float value, float other, float tolerance = defaultTolerance)
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
        public static float TolerantCeiling(this float value, float tolerance = defaultTolerance) =>
#if NETFRAMEWORK || NETSTANDARD2_0
            TolerantIsZero((float)Math.IEEERemainder(value, 1), tolerance) ? (float)Math.Round(value) : (float)Math.Ceiling(value);
#else
            TolerantIsZero(MathF.IEEERemainder(value, 1), tolerance) ? MathF.Round(value) : MathF.Ceiling(value);
#endif

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
        public static float TolerantFloor(this float value, float tolerance = defaultTolerance) =>
#if NETFRAMEWORK || NETSTANDARD2_0
            TolerantIsZero((float)Math.IEEERemainder(value, 1), tolerance) ? (float)Math.Round(value) : (float)Math.Floor(value);
#else
            TolerantIsZero(MathF.IEEERemainder(value, 1), tolerance) ? MathF.Round(value) : MathF.Floor(value);
#endif

        #endregion

        #region Internal Methods

        internal static string ToRoundtripString(this float value, IFormatProvider provider) =>
#if NETCOREAPP3_0_OR_GREATER
            value.ToString("R", provider);
#else
            IsNegativeZero(value) ? "-0" : value.ToString("R", provider);
#endif


        #endregion

        #endregion
    }
}
