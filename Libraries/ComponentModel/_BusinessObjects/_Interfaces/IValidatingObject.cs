#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IValidatingObject.cs
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
    /// Represents an object that can be validated.
    /// </summary>
    public interface IValidatingObject
    {
        #region Properties

        /// <summary>
        /// Gets whether this instance is valid. That is, if the <see cref="ValidationResults"/> property does not return any entry where
        /// the <see cref="ValidationResult.Severity"/> property is <see cref="ValidationSeverity.Error"/>.
        /// </summary>
        /// <value><see langword="true"/>&#160;if this instance is valid; otherwise, <see langword="false"/>.
        /// </value>
        bool IsValid { get; }

        /// <summary>
        /// Gets the validation results for this instance.
        /// </summary>
        ValidationResultsCollection ValidationResults { get; }

        #endregion
    }
}
