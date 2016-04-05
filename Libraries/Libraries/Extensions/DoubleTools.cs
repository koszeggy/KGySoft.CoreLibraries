using System;
using System.Globalization;

using KGySoft.Libraries.Reflection;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Extensions for the <see cref="double"/> type.
    /// </summary>
    public static class DoubleTools
    {
        /// <summary>
        /// Represents the negative zero value. This value is constant.
        /// </summary>
        public const double NegativeZero = -0d;

        private static long negativeZeroBits = BitConverter.DoubleToInt64Bits(NegativeZero);

        /// <summary>
        /// Returns a culture-invariant <see cref="string"/> representation of the given <see cref="double"/> <paramref name="value"/>,
        /// from which the original value can be parsed without losing any information.
        /// </summary>
        /// <param name="value">A <see cref="double"/> value to be converted to <see cref="string"/>.</param>
        /// <returns>A <see cref="double"/> value, from which the original value can be parsed without losing any information.</returns>
        /// <remarks>
        /// The result of this method can be parsed by <see cref="double.Parse(string,IFormatProvider)">Double.Parse</see>; however, to retrieve exactly the
        /// original value, including a negative zero value, use <see cref="Reflector.Parse(Type,string)">Reflector.Parse</see> instead.
        /// </remarks>
        public static string ToRoundtripString(this double value)
        {
            return IsNegativeZero(value) ? "-0" : value.ToString("R", NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Gets whether the specified <paramref name="value"/> is negative zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c>, if <paramref name="value"/> represents a negative zero value; otherwise, <c>false</c>.</returns>
        public static bool IsNegativeZero(this double value)
        {
            return BitConverter.DoubleToInt64Bits(value) == negativeZeroBits;
        }
    }
}
