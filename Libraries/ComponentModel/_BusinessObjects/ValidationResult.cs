using System;
using System.Diagnostics;
using KGySoft.CoreLibraries;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a validation entry in a <see cref="ValidationResultsCollection"/>.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Severity) + "}: {" + nameof(PropertyName) + "} - {" + nameof(Message) + "}")]
    public class ValidationResult
    {
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property to which this <see cref="ValidationResult"/> belongs.</param>
        /// <param name="message">The message of this <see cref="ValidationResult"/>.</param>
        /// <param name="severity">The severity of the <see cref="ValidationResult"/>. This parameter is optional.
        /// <br/>Default value: <see cref="ValidationSeverity.Error"/>.</param>
        public ValidationResult(string propertyName, string message, ValidationSeverity severity = ValidationSeverity.Error)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            Message = message ?? throw new ArgumentNullException(nameof(message), Res.ArgumentNull);
            if (!Enum<ValidationSeverity>.IsDefined(severity))
                throw new ArgumentOutOfRangeException(nameof(severity), Res.ArgumentOutOfRange);
            Severity = severity;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => Message ?? base.ToString();

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
            => obj is ValidationResult other &&
                PropertyName == other.PropertyName &&
                Message == other.Message &&
                Severity == other.Severity;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode() => ((int)Severity | (PropertyName.GetHashCode() & 0b11111111_11111100) << 16) | Message.GetHashCode();
    }
}
