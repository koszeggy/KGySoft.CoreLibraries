#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ValidationResultsCollection.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a collection of <see cref="ValidationResult"/> entries.
    /// </summary>
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    [Serializable]
    public class ValidationResultsCollection : Collection<ValidationResult>
    {
        #region Fields

        private ReadOnlyCollection<ValidationResult>? errors, warnings, infos;

        #endregion

        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets the validation results denoting an error.
        /// </summary>
#if NET35 || NET40
        public IList<ValidationResult>
#else
        public IReadOnlyList<ValidationResult>
#endif
            Errors => errors ??= new ReadOnlyCollection<ValidationResult>(this.Where(r => r.Severity == ValidationSeverity.Error).ToArray());

        /// <summary>
        /// Gets the validation results denoting a warning.
        /// </summary>
#if NET35 || NET40
        public IList<ValidationResult>
#else
        public IReadOnlyList<ValidationResult>
#endif
            Warnings => warnings ??= new ReadOnlyCollection<ValidationResult>(this.Where(r => r.Severity == ValidationSeverity.Warning).ToArray());

        /// <summary>
        /// Gets the validation results denoting an information.
        /// </summary>
#if NET35 || NET40
        public IList<ValidationResult>
#else
        public IReadOnlyList<ValidationResult>
#endif
            Infos => infos ??= new ReadOnlyCollection<ValidationResult>(this.Where(r => r.Severity == ValidationSeverity.Information).ToArray());

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has errors.
        /// </summary>
        /// <value><see langword="true"/>&#160;if this instance has errors; otherwise, <see langword="false"/>.</value>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has warnings.
        /// </summary>
        /// <value><see langword="true"/>&#160;if this instance has warnings; otherwise, <see langword="false"/>.</value>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Gets whether this <see cref="ValidationResultsCollection"/> has information entries.
        /// </summary>
        /// <value><see langword="true"/>&#160;if this instance has information entries; otherwise, <see langword="false"/>.</value>
        public bool HasInfos => Infos.Count > 0;

        #endregion

        #region Indexers

        /// <summary>
        /// Gets the validation results for the specified <paramref name="propertyName"/> and <paramref name="severity"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property to get the validation results.</param>
        /// <param name="severity">The severity of the validation results to get. Specify <see langword="null"/>&#160;to get results of any severities. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
#if NET35 || NET40
        public IList<ValidationResult>
#else
        public IReadOnlyList<ValidationResult>
#endif
            this[string propertyName, ValidationSeverity? severity = null] => this.Where(r => r.PropertyName == propertyName && (severity == null || severity == r.Severity)).ToArray();

        #endregion

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultsCollection"/> class.
        /// </summary>
        public ValidationResultsCollection()
        {
        }

        #endregion

        #region Private Constructors

        private ValidationResultsCollection(IList<ValidationResult> list) : base(list)
        {
        }

        #endregion

        #endregion

        #region Methods

        #region Public Methods

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

        #endregion

        #region Internal Methods

        internal ValidationResultsCollection ToReadOnly() => new ValidationResultsCollection(new ReadOnlyCollection<ValidationResult>(Items));

        #endregion

        #region Protected Methods

        /// <summary>
        /// Inserts an element into the <see cref="ValidationResultsCollection" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> cannot be <see langword="null"/>.</exception>
        protected override void InsertItem(int index, ValidationResult item)
        {
            if (item == null!)
                Throw.ArgumentNullException(Argument.item);
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
            if (item == null!)
                Throw.ArgumentNullException(Argument.item);
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

        #endregion

        #region Private Methods

        private void InvalidateCaches() => errors = warnings = infos = null;

        #endregion

        #endregion
    }
}
