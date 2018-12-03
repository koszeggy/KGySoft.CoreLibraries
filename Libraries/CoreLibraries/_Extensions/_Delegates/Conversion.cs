using System;
using System.Globalization;

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a delegate for a type conversion. A conversion can be registered by the <see cref="TypeExtensions.RegisterConversion(Type,Type,Conversion)">RegisterConversion</see> method.
    /// </summary>
    /// <param name="obj">The source object to convert.</param>
    /// <param name="targetType">The desired type of the result.</param>
    /// <param name="culture">The used culture for the conversion. If <see langword="null"/>, then the conversion must use culture invariant conversion.</param>
    /// <returns>The result instance of the conversion.</returns>
    public delegate object Conversion(object obj, Type targetType, CultureInfo culture);
}
