#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ConversionAttempt.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
    public delegate bool ConversionAttempt(object obj, Type targetType, CultureInfo culture, out object result);
}
