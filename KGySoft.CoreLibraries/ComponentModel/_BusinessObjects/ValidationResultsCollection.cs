#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ValidationResultsCollection.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a collection of <see cref="ValidationResult"/> entries.
    /// </summary>
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    [Serializable]
    public sealed class ValidationResultsCollection : Collection<ValidationResult>
    {
        #region Fields

        #region Static Fields

        /// <summary>
        /// Gets an empty, immutable <see cref="ValidationResultsCollection"/>. This field is read-only.
        /// </summary>
        public static readonly ValidationResultsCollection Empty = new ValidationResultsCollection(Reflector.EmptyArray<ValidationResult>());

        #endregion

        #region Instance Fields

        private ValidationResultsCollection? errors, warnings, infos;
        private IThreadSafeCacheAccessor<string, ValidationResultsCollection>? resultsByNameCache;
        private string? combinedMessage;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets a read-only <see cref="ValidationResultsCollection"/> containing <see cref="ValidationResult"/> entries with <see cref="ValidationSeverity.Error"/> severities.
        /// </summary>
        public ValidationResultsCollection Errors => errors ??= FilterBySeverity(ValidationSeverity.Error);

        /// <summary>
        /// Gets the validation results denoting a warning.= as a read-only <see cref="ValidationResultsCollection"/>.
        /// </summary>
        public ValidationResultsCollection Warnings => warnings ??= FilterBySeverity(ValidationSeverity.Warning);

        /// <summary>
        /// Gets the validation results denoting an information.
        /// </summary>
        public ValidationResultsCollection Infos => infos ??= FilterBySeverity(ValidationSeverity.Information);

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

        /// <summary>
        /// Gets a single combined <see cref="string"/> that contains all messages in this <see cref="ValidationResultsCollection"/>.
        /// </summary>
        public string Message
        {
            get
            {
                if (combinedMessage != null)
                    return combinedMessage;

                int len = Count;
                switch (len)
                {
                    case 0:
                        return combinedMessage = String.Empty;
                    case 1:
                        return combinedMessage = this[0].Message;
                    default:
                        // not using Select because Join would be much slower then
                        var result = new List<string>(Count);
                        
                        // ReSharper disable once ForCanBeConvertedToForeach - performance
                        // ReSharper disable once LoopCanBeConvertedToQuery - performance
                        for (int i = 0; i < len; i++)
                            result.Add(this[i].Message);
                        return combinedMessage = result.Join(Environment.NewLine);
                }
            }
        }

        #endregion

        #region Indexers

        /// <summary>
        /// Gets the validation results for the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property to get the validation results.</param>
        public ValidationResultsCollection this[string propertyName]
        {
            get
            {
                if (propertyName == null!)
                    Throw.ArgumentNullException(Argument.propertyName);
                if (Count == 0)
                    return Empty;

                if (resultsByNameCache == null)
                    Interlocked.CompareExchange(ref resultsByNameCache, ThreadSafeCacheFactory.Create<string, ValidationResultsCollection>(FilterByName, LockFreeCacheOptions.Profile128), null);

                // ReSharper disable once ConstantConditionalAccessQualifier - resultsByNameCache can be nullified again in InvalidateCaches, in which rare case we just ignore the cache.
                // Once a ValidationResultsCollection has been built and returned publicly, it is read-only so can never be invalidated
                return resultsByNameCache?[propertyName] ?? FilterByName(propertyName);
            }
        }

        /// <summary>
        /// Gets the validation results for the specified <paramref name="propertyName"/> and <paramref name="severity"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property to get the validation results.</param>
        /// <param name="severity">The severity of the validation results to get. Specify <see langword="null"/>&#160;to get results of any severities.</param>
        public ValidationResultsCollection this[string propertyName, ValidationSeverity? severity]
        {
            get
            {
                if (severity.HasValue && (uint)severity.Value > (uint)ValidationSeverity.Error)
                    Throw.EnumArgumentOutOfRange(Argument.severity, severity.Value);
                ValidationResultsCollection byName = this[propertyName];
                return severity switch
                {
                    null => byName,
                    ValidationSeverity.Error => byName.Errors,
                    ValidationSeverity.Warning => byName.Warnings,
                    _ => byName.Infos,
                };
            }
        }

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
        /// <exception cref="NotSupportedException">This <see cref="ValidationResultsCollection"/> instance is read-only and cannot be modified.</exception>
        public void AddError(string propertyName, string message) => Add(new ValidationResult(propertyName, message));

        /// <summary>
        /// Adds a warning to this <see cref="ValidationResultsCollection"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="message">The warning message.</param>
        /// <exception cref="NotSupportedException">This <see cref="ValidationResultsCollection"/> instance is read-only and cannot be modified.</exception>
        public void AddWarning(string propertyName, string message) => Add(new ValidationResult(propertyName, message, ValidationSeverity.Warning));

        /// <summary>
        /// Adds an information to this <see cref="ValidationResultsCollection"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="message">The information message.</param>
        /// <exception cref="NotSupportedException">This <see cref="ValidationResultsCollection"/> instance is read-only and cannot be modified.</exception>
        public void AddInfo(string propertyName, string message) => Add(new ValidationResult(propertyName, message, ValidationSeverity.Information));

        /// <summary>
        /// Gets the first <see cref="ValidationResult"/> with highest severity, optionally using the specified <paramref name="propertyName"/>,
        /// or <see langword="null"/>, if no such <see cref="ValidationResult"/> exists.
        /// </summary>
        /// <param name="propertyName">An optional property name to get the result for a specific property, or <see langword="null"/>&#160;to get the
        /// highest severity <see cref="ValidationResult"/> for any property.</param>
        /// <returns>The first <see cref="ValidationResult"/> with highest severity using the specified <paramref name="propertyName"/>, or <see langword="null"/>,
        /// if no such <see cref="ValidationResult"/> exists.</returns>
        public ValidationResult? TryGetFirstWithHighestSeverity(string? propertyName = null)
        {
            ValidationResultsCollection results = propertyName == null ? this : this[propertyName];
            return results.HasErrors ? results.Errors[0]
                : results.HasWarnings ? results.Warnings[0]
                : results.HasInfos ? results.Infos[0]
                : null;
        }

        /// <summary>
        /// Gets the string representation of this <see cref="ValidationResultsCollection"/> instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this <see cref="ValidationResultsCollection"/> instance.
        /// </returns>
        public override string ToString() => Message;

        #endregion

        #region Internal Methods

        internal ValidationResultsCollection ToReadOnly() => Items.IsReadOnly ? this : new ValidationResultsCollection(new ReadOnlyCollection<ValidationResult>(Items));

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

        private void InvalidateCaches()
        {
            errors = warnings = infos = null;
            resultsByNameCache = null;
            combinedMessage = null;
        }

        private ValidationResultsCollection FilterBySeverity(ValidationSeverity severity)
        {
            int len = Count;
            if (len == 0)
                return Empty;
            var result = new List<ValidationResult>(len);
            for (int i = 0; i < len; i++)
            {
                ValidationResult item = this[i];
                if (item.Severity == severity)
                    result.Add(item);
            }

            // ToArray is practical because that makes the result read-only without wrapping into a ReadOnlyCollection
            return result.Count == 0 ? Empty
                : result.Count == Count ? ToReadOnly()
                : new ValidationResultsCollection(result.ToArray());
        }

        private ValidationResultsCollection FilterByName(string propertyName)
        {
            int len = Count;
            if (len == 0)
                return Empty;
            var result = new List<ValidationResult>(len);
            for (int i = 0; i < len; i++)
            {
                ValidationResult item = this[i];
                if (item.PropertyName == propertyName)
                    result.Add(item);
            }

            // ToArray is practical because that makes the result read-only without wrapping into a ReadOnlyCollection
            return result.Count == 0 ? Empty
                : result.Count == Count ? ToReadOnly()
                : new ValidationResultsCollection(result.ToArray());
        }

        #endregion

        #endregion
    }
}
