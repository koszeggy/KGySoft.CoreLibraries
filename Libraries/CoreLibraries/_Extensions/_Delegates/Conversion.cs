using System;
using System.Globalization;

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a delegate for type conversions.
    /// </summary>
    /// <param name="obj">The source object to convert.</param>
    /// <param name="targetType">The desired target type of the conversion.</param>
    /// <param name="culture">The used culture for the conversion. If <see langword="null"/>, then the conversion must use culture invariant conversion.</param>
    /// <param name="result">The result. If the delegate returns <see langword="true"/>, then <paramref name="targetType"/> is assignable from the value of this parameter.</param>
    /// <returns><see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    public delegate bool Conversion(object obj, Type targetType, CultureInfo culture, out object result);
}
