#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Conversion.cs
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
using System.Globalization;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a delegate for a type conversion. A conversion can be registered by the <see cref="TypeExtensions.RegisterConversion(Type,Type,Conversion)">RegisterConversion</see> extension method.
    /// </summary>
    /// <param name="obj">The source object to convert.</param>
    /// <param name="targetType">The desired type of the result.</param>
    /// <param name="culture">The used culture for the conversion. If <see langword="null"/>, then the conversion must use culture invariant conversion.</param>
    /// <returns>The result instance of the conversion.</returns>
    public delegate object? Conversion(object obj, Type targetType, CultureInfo? culture);
}
