using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object that can be validated.
    /// </summary>
    public interface IValidatingObject
    {
        /// <summary>
        /// Gets whether this instance is valid. That is, if <see cref="Validate">Validate</see> method does not return any entries where
        /// the value of <see cref="ValidationResult.Severity"/> is <see cref="ValidationSeverity.Error"/>.
        /// </summary>
        /// <value><see langword="true"/> if this instance is valid; otherwise, <see langword="false"/>.
        /// </value>
        bool IsValid { get; }

        /// <summary>
        /// Validates this instance and returns the validation results.
        /// </summary>
        /// <returns>A <see cref="ValidationResultsCollection"/> instance containing the validation results.</returns>
        ValidationResultsCollection Validate();
    }
}
