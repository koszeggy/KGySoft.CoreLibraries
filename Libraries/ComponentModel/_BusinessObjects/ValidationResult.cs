using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using KGySoft.Annotations;
using KGySoft.Libraries;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a collection of <see cref="ValidationResult"/> entries.
    /// </summary>
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public class ValidationResultsCollection : Collection<ValidationResult>
    {
        private ValidationResultEntries errors, warnings, infos;

        public sealed class ValidationResultEntries : ReadOnlyCollection<ValidationResult>, IDataErrorInfo
        {
            public ValidationResultEntries([NotNull] IList<ValidationResult> list) : base(list)
            {
            }

            string IDataErrorInfo.this[string propertyName] => String.Join(Environment.NewLine, this.Where(e => e.PropertyName == propertyName).Select(e => e.Message));

            string IDataErrorInfo.Error => String.Join(Environment.NewLine, this.Select(e => e.Message));
        }

        private void InvalidateCaches() => errors = warnings = infos = null;

        /// <summary>
        /// Inserts an element into the <see cref="ValidationResultsCollection" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> cannot be <see langword="null"/>.</exception>
        protected override void InsertItem(int index, ValidationResult item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), Res.Get(Res.ArgumentNull));
            base.InsertItem(index, item);
            InvalidateCaches();
        }

        /// <summary>
        /// Replaces the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> cannot be <see langword="null"/>.</exception>
        protected override void SetItem(int index, ValidationResult item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), Res.Get(Res.ArgumentNull));
            base.SetItem(index, item);
            InvalidateCaches();
        }

        /// <summary>
        /// Removes all elements from the <see cref="ValidationResultsCollection" />.
        /// </summary>
        protected override void ClearItems()
        {
            base.ClearItems();
            InvalidateCaches();
        }

        /// <summary>
        /// Gets the validation results for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of the property to get the validation results.</param>
        /// <param name="severity">The severity of the validation results to get. Specify <see langword="null"/> to get results of any severities. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns></returns>
        public IReadOnlyList<ValidationResult> ForProperty(string propertyName, ValidationSeverity? severity = null) => this.Where(r => r.PropertyName == propertyName && (severity == null || severity == r.Severity)).ToArray();

        /// <summary>
        /// Gets the validation results denoting an error.
        /// </summary>
        public IReadOnlyList<ValidationResult> Errors => errors ?? (errors = new ValidationResultEntries(this.Where(r => r.Severity == ValidationSeverity.Error).ToArray()));

        /// <summary>
        /// Gets the validation results denoting a warning.
        /// </summary>
        public IReadOnlyList<ValidationResult> Warnings => warnings ?? (warnings = new ValidationResultEntries(this.Where(r => r.Severity == ValidationSeverity.Warning).ToArray()));

        /// <summary>
        /// Gets the validation results denoting an information.
        /// </summary>
        public IReadOnlyList<ValidationResult> Infos => infos ?? (infos = new ValidationResultEntries(this.Where(r => r.Severity == ValidationSeverity.Information).ToArray()));

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has errors.
        /// </summary>
        /// <value><see langword="true"/> if this instance has errors; otherwise, <see langword="false"/>.</value>
        public bool HasErrors => this.Any(r => r.Severity == ValidationSeverity.Error);

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has warnings.
        /// </summary>
        /// <value><see langword="true"/> if this instance has warnings; otherwise, <see langword="false"/>.</value>
        public bool HasWarnings => this.Any(r => r.Severity == ValidationSeverity.Warning);

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has information entries.
        /// </summary>
        /// <value><see langword="true"/> if this instance has information entries; otherwise, <see langword="false"/>.</value>
        public bool HasInfos => this.Any(r => r.Severity == ValidationSeverity.Information);

        /// <summary>
        /// Adds an error to this <see cref="ValidationResultsCollection"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="message">The error message.</param>
        public void AddError(string propertyName, string message) => Add(new ValidationResult(propertyName, message));

        /// <summary>
        /// Adds a warning to this <see cref="ValidationResultsCollection"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="message">The warning message.</param>
        public void AddWarning(string propertyName, string message) => Add(new ValidationResult(propertyName, message, ValidationSeverity.Warning));

        /// <summary>
        /// Adds an information to this <see cref="ValidationResultsCollection"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="message">The information message.</param>
        public void AddInfo(string propertyName, string message) => Add(new ValidationResult(propertyName, message, ValidationSeverity.Information));
    }

    /// <summary>
    /// Represents a validation entry in a <see cref="ValidationResultsCollection"/>.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Severity) + "}: " + nameof(PropertyName) + " - " + nameof(Message) + "}")]
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
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName), Res.Get(Res.ArgumentNull));
            Message = message ?? throw new ArgumentNullException(nameof(message), Res.Get(Res.ArgumentNull));
            if (!Enum<ValidationSeverity>.IsDefined(severity))
                throw new ArgumentOutOfRangeException(nameof(severity), Res.Get(Res.ArgumentOutOfRange));
            Severity = severity;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => Message ?? base.ToString();
    }

    /// <summary>
    /// Represents a severity level for a <see cref="ValidationResult"/>.
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
