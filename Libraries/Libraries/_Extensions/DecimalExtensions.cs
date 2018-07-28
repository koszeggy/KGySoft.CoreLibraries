#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DecimalExtensions.cs
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

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="decimal">decimal</see> type.
    /// </summary>
    public static class DecimalExtensions
    {
        // TODO: https://github.com/raminrahimzada/CSharp-Helper-Classes/blob/master/Math/DecimalMath/DecimalMath.cs


        #region Constants

        /// <summary>
        /// Represents one possible negative zero value of the <see cref="decimal"/> type. This value is constant.
        /// </summary>
        public const decimal NegativeZero = -0.0m;

        #endregion

        #region Methods

        /// <summary>
        /// Returns a culture-invariant <see cref="string"/> representation of the given <see cref="decimal"/> <paramref name="value"/>,
        /// from which the original value can be parsed without losing any information.
        /// </summary>
        /// <param name="value">A <see cref="decimal"/> value to be converted to <see cref="string"/>.</param>
        /// <returns>A <see cref="decimal"/> value, from which the original value can be parsed without losing any information.</returns>
        public static string ToRoundtripString(this decimal value)
        {
            string result = value.ToString(null, NumberFormatInfo.InvariantInfo);
            return IsNegativeZero(value) ? "-" + result : result;
        }

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> is negative zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c>, if <paramref name="value"/> represents a negative zero value; otherwise, <c>false</c>.</returns>
        public static bool IsNegativeZero(this decimal value)
        {
            return value == 0m && (Decimal.GetBits(value)[3] & 0x80000000) != 0;
        }

        private const decimal e = 2.7182818284590452353602874713526624977572470936999595749m;
        private const decimal eReciprocal = 0.3678794411714423215955237701614608674458111310317678m;

        /// <summary>
        /// log(10, e)
        /// </summary>
        private const decimal log10E = 0.434294481903251827651128918916605082294397005803666566114m;

        private const int maxTaylorIteration = 100;

        internal static decimal Log10(decimal x) => Log(x) * log10E;

        private static decimal Log(decimal x)
        {
            if (x <= 0)
                throw new ArgumentOutOfRangeException(nameof(x), Res.Get(Res.ArgumentOutOfRange));

            int count = 0;
            while (x >= 1m)
            {
                x *= eReciprocal;
                count++;
            }

            while (x <= eReciprocal)
            {
                x *= e;
                count--;
            }

            x--;
            if (x == 0m)
                return count;

            // going on with Taylor series
            decimal result = 0m;
            decimal acc = 1m;
            for (int i = 1; i <= maxTaylorIteration; i++)
            {
                decimal prevResult = result;
                acc *= -x;
                result += acc / i;
                if (prevResult == result)
                    break;
            }

            return count - result;
        }

        internal static decimal Pow(decimal x, decimal power) => Exp(power * Log(x));

        private static decimal Exp(decimal x)
        {
            int count = 0;
            while (x > 1m)
            {
                x--;
                count++;
            }

            while (x < 0m)
            {
                x++;
                count--;
            }

            decimal result = 1m;
            decimal acc = 1m;
            for (int i = 1; ; i++)
            {
                decimal prevResult = result;
                acc *= x / i;
                result += acc;
                if (prevResult == result)
                    break;
            }

            if (count != 0)
                result = result * PowerInt(e, count);

            return result;
        }

        private static decimal PowerInt(decimal x, int power)
        {
            if (power == 0)
                return 1m;
            if (power < 0)
            {
                power = -power;
                x = 1m / x;
            }

            int q = power;
            decimal prod = 1m;
            decimal current = x;
            while (q > 0)
            {
                if ((q & 1) == 1)
                {
                    prod = current * prod;
                    q--;
                }
                current *= current; // value^i -> value^(2*i)
                q /= 2;
            }

            return prod;
        }

        #endregion
    }
}
