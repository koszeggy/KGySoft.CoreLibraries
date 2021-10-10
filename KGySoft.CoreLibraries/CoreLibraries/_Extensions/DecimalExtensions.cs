#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DecimalExtensions.cs
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
using System.Collections.Generic;
using System.Globalization;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="decimal">decimal</see> type.
    /// </summary>
    public static class DecimalExtensions
    {
        #region Constants

        #region Public Constants

        /// <summary>
        /// Represents one possible negative zero value of the <see cref="decimal"/> type.
        /// </summary>
        /// <remarks>The value of this constant is <c>-0.0</c>.</remarks>
        public const decimal NegativeZero = -0.0m;

        /// <summary>
        /// Represents the natural logarithmic base, specified by the constant, <em>e</em>.
        /// </summary>
        /// <remarks>
        /// <para>This member is similar to <see cref="Math.E">Math.E</see> but has <see cref="decimal"/> type instead of <see cref="double"/>.</para>
        /// <para>The value of this constant is <c>2.7182818284590452353602874714</c>.</para>
        /// </remarks>
        public const decimal E = 2.7182818284590452353602874714m;

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Represents the ratio of the circumference of a circle to its diameter, specified by the constant, <em>π</em>.
        /// </summary>
        /// <remarks>
        /// <para>This member is similar to <see cref="Math.PI">Math.PI</see> but has <see cref="decimal"/> type instead of <see cref="double"/>.</para>
        /// <para>The value of this constant is <c>3.1415926535897932384626433833</c>.</para>
        /// </remarks>
        public const decimal PI = 3.1415926535897932384626433833m;

        /// <summary>
        /// Represents the smallest positive <see cref="decimal"/> value that is greater than zero.
        /// </summary>
        /// <remarks>The value of this constant is <c>0.0000000000000000000000000001</c>.</remarks>
        public const decimal Epsilon = 0.0000000000000000000000000001m;

        #endregion

        #region Private Constants

        /// <summary>
        /// 1 / e = 0.3678794411714423215955237702
        /// </summary>
        private const decimal eReciprocal = 1m / E;

        /// <summary>
        /// Logarithm of e in base 10 = log(e, 10)
        /// </summary>
        private const decimal log10E = 0.4342944819032518276511289189m;

        private const int maxTaylorIteration = 100;

        #endregion

        #endregion

        #region Fields

        private static readonly Dictionary<decimal, int> powerOf10 = InitPowerOf10();

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Returns a culture-invariant <see cref="string"/> representation of the given <see cref="decimal"/>&#160;<paramref name="value"/>,
        /// from which the original value can be parsed without losing any information.
        /// </summary>
        /// <param name="value">A <see cref="decimal"/> value to be converted to <see cref="string"/>.</param>
        /// <returns>A <see cref="decimal"/> value, from which the original value can be parsed without losing any information.</returns>
        public static string ToRoundtripString(this decimal value) => value.ToRoundtripString(NumberFormatInfo.InvariantInfo);

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> is negative zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> represents a negative zero value; otherwise, <see langword="false"/>.</returns>
        public static bool IsNegativeZero(this decimal value) => value == 0m && (Decimal.GetBits(value)[3] & 0x80000000) != 0;

        /// <summary>
        /// Removes the trailing zeros after the decimal sign of the specified <see cref="decimal"/>&#160;<paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <returns>The normalized value of the specified <see cref="decimal"/>&#160;<paramref name="value"/> containing no trailing zeros after the decimal sign.</returns>
        public static decimal Normalize(this decimal value) => value / 1.0000000000000000000000000000m;

        /// <summary>
        /// Returns the natural (base <em>e</em>) logarithm of a <see cref="decimal"/>&#160;<paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value whose logarithm is to be found. Must be greater than zero.</param>
        /// <returns>The natural logarithm of <paramref name="value"/>.</returns>
        /// <remarks>
        /// <para>This member is similar to <see cref="Math.Log(double)">Math.Log(double)</see> but uses <see cref="decimal"/> type instead of <see cref="double"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than or equal to 0.</exception>
        public static decimal Log(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            int resultLog10;
            if (value >= 1m)
            {
                if (powerOf10.TryGetValue(value, out resultLog10))
                    return resultLog10 / log10E;
            }
            else
            {
                decimal reciprocal = 1m / value;
                if (reciprocal != 0m && powerOf10.TryGetValue(reciprocal, out resultLog10))
                    return -resultLog10 / log10E;
            }

            return RoundInternal(LogE(value));
        }

        /// <summary>
        /// Returns the base 10 logarithm of a <see cref="decimal"/>&#160;<paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value whose logarithm is to be found. Must be greater than zero.</param>
        /// <returns>The base 10 logarithm of <paramref name="value"/>.</returns>
        /// <remarks>
        /// <para>This member is similar to <see cref="Math.Log10">Math.Log10</see> but uses <see cref="decimal"/> type instead of <see cref="double"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than or equal to 0.</exception>
        public static decimal Log10(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            int result;
            if (value >= 1m)
            {
                if (powerOf10.TryGetValue(value, out result))
                    return result;
            }
            else
            {
                decimal reciprocal = 1m / value;
                if (reciprocal != 0m && powerOf10.TryGetValue(reciprocal, out result))
                    return -result;
            }

            return LogE(value) * log10E;
        }

        /// <summary>
        /// Returns the logarithm of a specified <paramref name="value"/> in a specified <paramref name="base"/>.
        /// </summary>
        /// <param name="value">The value whose logarithm is to be found. Must be greater than zero.</param>
        /// <param name="base">The base of the logarithm.</param>
        /// <returns>The logarithm of the specified <paramref name="value"/> in the specified <paramref name="base"/>.</returns>
        /// <remarks>
        /// <para>This member is similar to <see cref="Math.Log(double,double)">Math.Log(double, double)</see> but uses <see cref="decimal"/> type instead of <see cref="double"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than or equal to 0.
        /// <br/>-or-
        /// <br/><paramref name="base"/> equals to 1 or is less or equal to 0.</exception>
        public static decimal Log(this decimal value, decimal @base)
        {
            if (@base == 1m)
                Throw.ArgumentOutOfRangeException(Argument.value);
            if (value == 1m && @base == 0m)
                return 0m;
            var result = Log(value) / Log(@base);
            return RoundInternal(result);
        }

        /// <summary>
        /// Returns <em>e</em> raised to the specified <paramref name="power"/>.
        /// </summary>
        /// <param name="power">The specified power.</param>
        /// <returns>The number <em>e</em> raised to the specified <paramref name="power"/>.</returns>
        /// <exception cref="OverflowException"><paramref name="power"/> is too large for the result to fit in a <see cref="decimal"/> value.</exception>
        /// <remarks>
        /// <para>This member is similar to <see cref="Math.Exp">Math.Exp</see> but uses <see cref="decimal"/> type instead of <see cref="double"/>.</para>
        /// </remarks>
        public static decimal Exp(this decimal power)
        {
            int integerPart = 0;
            if (power > 1m)
            {
                decimal diff = Math.Floor(power);
                power -= diff;
                integerPart += (int)diff;
            }
            else if (power < 0m)
            {
                decimal diff = Math.Floor(Math.Abs(power));
                if (diff > Int32.MaxValue)
                {
                    diff = Int32.MaxValue;
                    power = 0m;
                }
                else
                    power += diff;
                integerPart -= (int)diff;
            }

            decimal result = 1m;
            decimal acc = 1m;
            for (int i = 1; ; i++)
            {
                decimal prevResult = result;
                acc *= power / i;
                result += acc;
                if (prevResult == result)
                    break;
            }

            if (integerPart != 0)
                result *= Pow(E, integerPart);

            return result;
        }

        /// <summary>
        /// Returns the specified <paramref name="value"/> raised to the specified <paramref name="power"/>.
        /// </summary>
        /// <param name="value">The value to be raised to a power.</param>
        /// <param name="power">The specified power.</param>
        /// <returns>The specified <paramref name="value"/> raised to the specified <paramref name="power"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative and <paramref name="power"/> is not an integer.</exception>
        /// <exception cref="OverflowException"><paramref name="power"/> is too large for the result to fit in a <see cref="decimal"/> value.</exception>
        /// <remarks>
        /// <para>This member is similar to <see cref="Math.Pow">Math.Pow</see> but uses <see cref="decimal"/> type instead of <see cref="double"/>.</para>
        /// </remarks>
        public static decimal Pow(this decimal value, decimal power)
        {
            if (power <= Int32.MaxValue && Math.Truncate(power) == power)
                return Pow(value, (int)power);

            return RoundInternal(Exp(power * Log(value)));
        }

        /// <summary>
        /// Returns the specified <paramref name="value"/> raised to the specified <paramref name="power"/>.
        /// </summary>
        /// <param name="value">The value to be raised to a power.</param>
        /// <param name="power">The specified power.</param>
        /// <returns>The specified <paramref name="value"/> raised to the specified <paramref name="power"/>.</returns>
        /// <exception cref="OverflowException"><paramref name="power"/> is too large for the result to fit in a <see cref="decimal"/> value.</exception>
        public static decimal Pow(decimal value, int power)
        {
            if (power == 0)
                return 1m;

            decimal current = value;
            if (power < 0)
            {
                power = power > Int32.MinValue ? -power : Int32.MaxValue;
                current = 1m / current;
            }

            decimal result = 1m;
            while (power > 0)
            {
                if ((power & 1) == 1)
                {
                    result = current * result;
                    power -= 1;
                }

                power >>= 1;
                if (power > 0)
                    current *= current;
            }

            return result;
        }

        #endregion

        #region Internal Methods

        internal static string ToRoundtripString(this decimal value, IFormatProvider provider)
        {
            string result = value.ToString(null, provider);
            return IsNegativeZero(value) ? "-" + result : result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialized the power of 10 cache.
        /// Could be in static constructor but moved here for performance reasons (CA1810)
        /// </summary>
        private static Dictionary<decimal, int> InitPowerOf10()
        {
            var result = new Dictionary<decimal, int> { [0m] = 1 };
            decimal value = 1m;
            for (int i = 0; i <= 28; i++)
            {
                result[value] = i;
                if (i < 28)
                    value *= 10m;
            }

            return result;
        }

        /// <summary>
        /// Calculates the natural base logarithm.
        /// </summary>
        private static decimal LogE(decimal value)
        {
            int count = 0;
            while (value >= 1m)
            {
                value *= eReciprocal;
                count += 1;
            }

            while (value <= eReciprocal)
            {
                value *= E;
                count -= 1;
            }

            value -= 1;
            if (value == 0m)
                return count;

            // going on with Taylor series
            decimal result = 0m;
            decimal acc = 1m;
            for (int i = 1; i <= maxTaylorIteration; i++)
            {
                decimal prevResult = result;
                acc *= -value;
                result += acc / i;
                if (prevResult == result)
                    break;
            }

            return count - result;
        }

        /// <summary>
        /// If the decimal value rounded to 23 places are the same as the rounded value to 4 decimals, then returns the rounded value.
        /// This helps to correct the results of the Log methods.
        /// </summary>
        private static decimal RoundInternal(decimal value)
        {
            decimal round23 = Math.Round(value, 23);
            if (round23 == 0m)
                return value;
            if (Math.Round(value, 5) == round23)
                return Normalize(round23);
            return value;
        }

        #endregion

        #endregion
    }
}
