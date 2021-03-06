﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ValidationResult.cs
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

using System.Diagnostics;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a validation entry in a <see cref="ValidationResultsCollection"/>.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Severity) + "}: {" + nameof(PropertyName) + "} - {" + nameof(Message) + "}")]
    public class ValidationResult
    {
        #region Properties

        /// <summary>
        /// Gets the name of the property for this <see cref="ValidationResult"/>.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the message for this <see cref="ValidationResult"/>.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the severity of this <see cref="ValidationResult"/>.
        /// </summary>
        public ValidationSeverity Severity { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property to which this <see cref="ValidationResult"/> belongs.</param>
        /// <param name="message">The message of this <see cref="ValidationResult"/>.</param>
        /// <param name="severity">The severity of the <see cref="ValidationResult"/>. This parameter is optional.
        /// <br/>Default value: <see cref="ValidationSeverity.Error"/>.</param>
        public ValidationResult(string propertyName, string message, ValidationSeverity severity = ValidationSeverity.Error)
        {
            if (propertyName == null!)
                Throw.ArgumentNullException(Argument.propertyName);
            if (message == null!)
                Throw.ArgumentNullException(Argument.message);
            PropertyName = propertyName;
            Message = message;
            if (!Enum<ValidationSeverity>.IsDefined(severity))
                Throw.EnumArgumentOutOfRangeWithValues(Argument.severity, severity);
            Severity = severity;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => Message;

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
            => obj is ValidationResult other &&
            PropertyName == other.PropertyName &&
            Message == other.Message &&
            Severity == other.Severity;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode() => ((int)Severity, PropertyName, Message).GetHashCode();

        #endregion
    }
}
