using System;
using System.Globalization;

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a delegate for a type conversion attempt. A conversion can be registered by the <see cref="TypeExtensions.RegisterConversion(Type,Type,ConversionAttempt)">RegisterConversion</see> method.
    /// </summary>
    /// <param name="obj">The source object to convert.</param>
    /// <param name="targetType">The desired type of the <paramref name="result"/> parameter if the return value is <see langword="true"/>.</param>
    /// <param name="culture">The used culture for the conversion. If <see langword="null"/>, then the conversion must use culture invariant conversion.</param>
    /// <param name="result">The result if the return value is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    public delegate bool ConversionAttempt(object obj, Type targetType, CultureInfo culture, out object result);
}
