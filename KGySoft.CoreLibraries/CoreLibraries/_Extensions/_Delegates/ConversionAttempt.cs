#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ConversionAttempt.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a delegate for a type conversion attempt. A conversion can be registered by the <see cref="TypeExtensions.RegisterConversion(Type,Type,ConversionAttempt)">RegisterConversion</see> extension method.
    /// </summary>
    /// <param name="obj">The source object to convert.</param>
    /// <param name="targetType">The desired type of the <paramref name="result"/> parameter if the return value is <see langword="true"/>.</param>
    /// <param name="culture">The used culture for the conversion. If <see langword="null"/>, then the conversion must use culture invariant conversion.</param>
    /// <param name="result">The result if the return value is <see langword="true"/>.</param>
    /// <returns><see langword="true"/>&#160;if the conversion was successful; otherwise, <see langword="false"/>.</returns>
    [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "Try... pattern")]
    [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate", Justification = "Impossible, not called from generic context and all conversions are stored together.")]
    public delegate bool ConversionAttempt(object obj, Type targetType, CultureInfo? culture, [MaybeNullWhen(false)]out object result);
}
