#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ValidationSeverity.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents the severity level of a <see cref="ValidationResult"/>.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Represents the information severity level.
        /// </summary>
        Information,

        /// <summary>
        /// Represents the warning severity level.
        /// </summary>
        Warning,

        /// <summary>
        /// Represents the error severity level.
        /// </summary>
        Error
    }
}
