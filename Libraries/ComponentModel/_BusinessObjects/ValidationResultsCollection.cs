using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a collection of <see cref="ValidationResult"/> entries.
    /// </summary>
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public class ValidationResultsCollection : Collection<ValidationResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultsCollection"/> class.
        /// </summary>
        public ValidationResultsCollection()
        {
        }

        private ValidationResultsCollection(IList<ValidationResult> list) : base(list)
        {
        }

        internal ValidationResultsCollection ToReadOnly() => new ValidationResultsCollection(new ReadOnlyCollection<ValidationResult>(Items));

        private ValidationResult[] errors, warnings, infos;

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
                throw new ArgumentNullException(nameof(item), Res.ArgumentNull);
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
                throw new ArgumentNullException(nameof(item), Res.ArgumentNull);
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

        ///// <summary>
        ///// Gets the validation results for the specified property.
        ///// </summary>
        ///// <param name="propertyName">Name of the property to get the validation results.</param>
        ///// <param name="severity">The severity of the validation results to get. Specify <see langword="null"/> to get results of any severities. This parameter is optional.
        ///// <br/>Default value: <see langword="null"/>.</param>
        ///// <returns></returns>
        //public ValidationResult[] ForProperty(string propertyName, ValidationSeverity? severity = null) 

        public ValidationResult[] this[string propertyName, ValidationSeverity? severity = null] => this.Where(r => r.PropertyName == propertyName && (severity == null || severity == r.Severity)).ToArray();

        /// <summary>
        /// Gets the validation results denoting an error.
        /// </summary>
        public ValidationResult[] Errors => errors ?? (errors = this.Where(r => r.Severity == ValidationSeverity.Error).ToArray());

        /// <summary>
        /// Gets the validation results denoting a warning.
        /// </summary>
        public ValidationResult[] Warnings => warnings ?? (warnings = this.Where(r => r.Severity == ValidationSeverity.Warning).ToArray());

        /// <summary>
        /// Gets the validation results denoting an information.
        /// </summary>
        public ValidationResult[] Infos => infos ?? (infos = this.Where(r => r.Severity == ValidationSeverity.Information).ToArray());

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has errors.
        /// </summary>
        /// <value><see langword="true"/> if this instance has errors; otherwise, <see langword="false"/>.</value>
        public bool HasErrors => Errors.Length > 0;

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has warnings.
        /// </summary>
        /// <value><see langword="true"/> if this instance has warnings; otherwise, <see langword="false"/>.</value>
        public bool HasWarnings => Warnings.Length > 0;

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has information entries.
        /// </summary>
        /// <value><see langword="true"/> if this instance has information entries; otherwise, <see langword="false"/>.</value>
        public bool HasInfos => Infos.Length > 0;

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
}