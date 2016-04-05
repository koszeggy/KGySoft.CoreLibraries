using System;
using System.Globalization;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Extensions for the <see cref="decimal"/> type.
    /// </summary>
    public static class DecimalTools
    {
        /// <summary>
        /// Represents one possible negative zero value of the <see cref="decimal"/> type. This value is constant.
        /// </summary>
        public const decimal NegativeZero = -0.0m;

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
    }
}
