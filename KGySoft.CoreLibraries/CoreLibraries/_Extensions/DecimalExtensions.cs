#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DecimalExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
using System.Security;

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

        /// <summary>
        /// The base e logarithm of 10 = log(10) = 1 / log10E
        /// </summary>
        private const decimal logE10 = 2.3025850929940456840179914547m;

        #endregion

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
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static decimal Log(this decimal value)
        {
            if (value <= 0m)
                Throw.ArgumentOutOfRangeException(Argument.value);

            // We could just return LogE(value), but it gets very inaccurate for very small values.
            // For big values the accuracy would not be a problem, but dividing by 10 is much faster (and converges faster) than dividing by e in LogE.
            // So normalizing the value between (0.1 and 1], and utilising that Log(123.456) = Log(0.123456 * 10^3) = Log(0.123456) + 3 * Log(10)
            int exp = 0;
            if (value > 1m)
            {
                do
                {
                    value = value.ShiftRight();
                    exp += 1;
                } while (value > 1m);
            }
            else
            {
                while (value <= 0.1m)
                {
                    value = value.ShiftLeft();
                    exp -= 1;
                }
            }

            decimal result = value == 1m ? 0m : LogE(value);
            if (exp != 0)
                result += exp * logE10;
            return result.Normalize();
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

            // We could just return LogE(value) * log10E, but it gets very inaccurate for very small values.
            // For big values the accuracy would not be a problem, but dividing by 10 is much faster (and converges faster) than dividing by e in LogE,
            // and also it provides very accurate results for powers of 10.
            // So normalizing the value between (0.1 and 1], and utilising that Log(123.456) = Log(0.123456 * 10^3) = Log(0.123456) + 3 * Log(10)
            int exp = 0;
            if (value > 1m)
            {
                do
                {
                    value = value.ShiftRight();
                    exp += 1;
                } while (value > 1m);
            }
            else
            {
                while (value <= 0.1m)
                {
                    value = value.ShiftLeft();
                    exp -= 1;
                }
            }

            if (value == 1m)
                return exp;

            return (LogE(value) * log10E + exp).Normalize();
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
            decimal result = Log(value) / Log(@base);
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
            int integerPart;
            if (power >= 1m)
            {
                decimal floor = Math.Floor(power);
                power -= floor;
                integerPart = (int)floor; // > Int32.MaxInt is not a problem, an OverflowException would be thrown later anyway
            }
            else if (power < 0m)
            {
                decimal floor = Math.Floor(-power);
                if (floor >= 66) // e^-66 < epsilon (~ 2.17e-29)
                    return 0m;

                power += floor;
                integerPart = -(int)floor;
            }
            else
                integerPart = 0;

            // Doing the power series for the fractional part of power: exp(p) = 1 + p + (p^2)/2! + (p^3)/3! + ...
            // see also https://en.wikipedia.org/wiki/Exponential_function#Power_series
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
                result *= E.Pow(integerPart);
            return result.Normalize();
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
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static decimal Pow(this decimal value, decimal power)
        {
            #region Local Methods

            [MethodImpl(MethodImplOptions.NoInlining)]
            static decimal PowLarge(decimal value, decimal power)
            {
                Debug.Assert(power is < Int32.MinValue or > Int32.MaxValue);
                return value switch
                {
                    > 0m => Exp(power * Log(value)),
                    < 0m => (power % 2m) switch
                    {
                        0m => Exp(power * Log(-value)),
                        1m or -1m => -Exp(power * Log(-value)),
                        // Fractional power for a negative value: the result would be a complex number.
                        // NOTE: we throw the exception for the value parameter to be conform with the exception in smaller range (in which case it comes from the Log() method).
                        _ => Throw.ArgumentOutOfRangeException<decimal>(Argument.value)
                    },
                    0m => power switch
                    {
                        > 0m => 0m,
                        < 0m => Throw.OverflowException<decimal>(),
                        0m => 1m
                    },
                };
            }

            #endregion

            if (power is > Int32.MaxValue or < Int32.MinValue)
                return PowLarge(value, power);

            // It's faster if we calculate the result for the integer part first, and then for the fractional
            decimal integerPart = Math.Truncate(power);
            decimal fracPart = power - integerPart;

            decimal result = Pow(value, (int)integerPart);
            if (fracPart == 0m)
                return result;

            result *= Exp(fracPart * Log(value));
            return Math.Abs(power) > 1e-10m
                ? RoundInternal(result)
                : result;
        }

        /// <summary>
        /// Returns the specified <paramref name="value"/> raised to the specified <paramref name="power"/>.
        /// </summary>
        /// <param name="value">The value to be raised to a power.</param>
        /// <param name="power">The specified power.</param>
        /// <returns>The specified <paramref name="value"/> raised to the specified <paramref name="power"/>.</returns>
        /// <exception cref="OverflowException"><paramref name="power"/> is too large for the result to fit in a <see cref="decimal"/> value.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static decimal Pow(this decimal value, int power)
        {
            if (power <= 0)
            {
                if (power == 0)
                    return 1m;
                if (value == 0m) // 0^-p would be negative infinity
                    Throw.OverflowException();
                value = 1m / value;
                power = -power; // for MinValue it remains the same, but it's handled correctly below
            }

            decimal current = value;
            decimal result = 1m;
            while (true)
            {
                if ((power & 1) == 1)
                {
                    result = current * result;
                    if (power == 1)
                        return result.Normalize();
                }

                power >>>= 1;
                if (power != 0)
                    current *= current;
            }
        }

        #endregion

        #region Internal Methods

        internal static string ToRoundtripString(this decimal value, IFormatProvider provider)
        {
            string result = value.ToString(null, provider);
            return IsNegativeZero(value) ? "-" + result : result;
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static unsafe decimal ShiftRight(this decimal value)
        {
#if NETFRAMEWORK || NETSTANDARD2_0
            if (EnvironmentHelper.IsPartiallyTrustedDomain)
                return value * 0.1m;
#endif
            ref byte scale = ref ((byte*)&value)[BitConverter.IsLittleEndian ? 2 : 1];
            if (scale == 28)
                return value * 0.1m;
            scale += 1;
            return value;
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static unsafe decimal ShiftLeft(this decimal value)
        {
#if NETFRAMEWORK || NETSTANDARD2_0
            if (EnvironmentHelper.IsPartiallyTrustedDomain)
                return value * 10m;
#endif
            ref byte scale = ref ((byte*)&value)[BitConverter.IsLittleEndian ? 2 : 1];
            if (scale == 0)
                return value * 10m;
            scale -= 1;
            return value;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates the natural base logarithm.
        /// </summary>
        private static decimal LogE(decimal value)
        {
            int count = 0;

            // Constants are chosen so the Taylor-series always start with |value| < 0.4625 for faster converging (up to 79 iterations)
            // 1.462 / E - 1 = -0.462160257007; 1.462 - 1 = 0.462
            if (value >= 1.462m)
            {
                do
                {
                    value *= eReciprocal;
                    count += 1;
                } while (value >= 1.462m);
            }
            else
            {
                // 0.538 * E - 1 = 0.462435623711; 0.538 - 1 = 0.462
                while (value <= 0.538m)
                {
                    value *= E;
                    count -= 1;
                }
            }

            value -= 1;
            if (value == 0m)
                return count;

            Debug.Assert(Math.Abs(value) < 0.5m, $"Suboptimal start value for Taylor-series: {value}");

            // going on with Taylor series
            decimal result = 0m;
            decimal acc = 1m;
            for (int i = 1; ; i++)
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
        /// If the decimal value rounded to 25 places are the same as the rounded value to 5 decimals, then returns the rounded value.
        /// This helps to correct the results of the Log/Pow methods.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static decimal RoundInternal(decimal value)
        {
            decimal round25 = Math.Round(value, 25);
            if (Math.Round(value, 5) == round25)
                return Normalize(round25);
            return value;
        }

        #endregion

        #endregion
    }
}
