#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ValidationSeverity.cs
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
