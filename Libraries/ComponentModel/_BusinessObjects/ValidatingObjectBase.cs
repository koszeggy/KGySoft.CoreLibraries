using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using KGySoft.Annotations;
using KGySoft.CoreLibraries;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object validating capability. The <see cref="DoValidation">DoValidation</see> method must be overridden to implement validation logic.
    /// </summary>
    /// <seealso cref="IValidatingObject" />
    /// <seealso cref="IDataErrorInfo" />
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="UndoableObjectBase" />
    /// <seealso cref="EditableObjectBase" />
    /// <seealso cref="ModelBase" />
    /// TODO
    /// - note: components - akár kép is! - Cast to IValidatingObject if needed
    /// - Example
    /// - CanSetProperty vs validation
    /// - IDataErrorInfo, possible implementation for Annotations (Entity/MVC), FluentValidation
    /// - This implementation caches last IsValid result and causes re-evaluation if a property is changed, which affects the IsModified property (AffectsModifiedState returns true)
    public abstract class ValidatingObjectBase : PersistableObjectBase, IValidatingObject, IDataErrorInfo
    {
        private ValidationResultsCollection cachedValidationResults;
        private bool lastIsValid = true;
        private bool? isValid;

        /// <summary>
        /// Gets whether this instance is valid. That is, if <see cref="ValidationResults"/> property does not return any entries where
        /// the value of <see cref="ValidationResult.Severity"/> is <see cref="ValidationSeverity.Error"/>.
        /// </summary>
        /// <value><see langword="true"/> if this instance is valid; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsValid => isValid ?? (bool)(isValid = !ValidationResults.HasErrors);

        /// <summary>
        /// Gets the validation results for this instance.
        /// </summary>
        public ValidationResultsCollection ValidationResults => cachedValidationResults ?? Validate();

        private ValidationResultsCollection Validate()
        {
            ValidationResultsCollection result = (DoValidation() ?? throw new InvalidOperationException(Res.ComponentModelDoValidationNull)).ToReadOnly();

            bool newIsValid = !result.HasErrors;
            bool raiseIsValidChanged = newIsValid != lastIsValid;
            isValid = lastIsValid = newIsValid;

            ValidationResultsCollection lastResult = cachedValidationResults;
            bool raiseValidationResultsChanged = lastResult?.SequenceEqual(result) != true;
            cachedValidationResults = result;

            if (raiseIsValidChanged)
                OnPropertyChanged(new PropertyChangedExtendedEventArgs(!newIsValid, newIsValid, nameof(IsValid)));
            if (raiseValidationResultsChanged)
                OnPropertyChanged(new PropertyChangedExtendedEventArgs(lastResult, result, nameof(ValidationResults)));

            return result;
        }

        /// <summary>
        /// Performs the validation on this instance and returns the validation results. Must not return <see langword="null"/>.
        /// </summary>
        /// <returns> A <see cref="ValidationResultsCollection" /> instance containing the validation results.</returns>
        protected abstract ValidationResultsCollection DoValidation();

        /// <summary>
        /// Raises the <see cref="ObservableObjectBase.PropertyChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedExtendedEventArgs" /> instance containing the event data.</param>
        protected internal override void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
        {
            // Invalidating cached validation results if an affected property has changed.
            if (isValid != null && AffectsModifiedState(e.PropertyName))
            {
                isValid = null;
                cachedValidationResults = null;
            }

            base.OnPropertyChanged(e);
        }

        /// <summary>
        /// Gets whether the change of the specified <paramref name="propertyName" /> affects the <see cref="ObservableObjectBase.IsModified" /> property.
        /// <br />The <see cref="EditableObjectBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/>, <see cref="IsValid"/> and <see cref="ValidationResults"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true" /> if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) => base.AffectsModifiedState(propertyName) && !propertyName.In(nameof(IsValid), nameof(ValidationResults));

        //string IDataErrorInfo.this[string propertyName] => String.Join(Environment.NewLine, ValidationResults.Errors.Where(e => e.PropertyName == propertyName).Select(e => e.Message));
        string IDataErrorInfo.this[string propertyName] => String.Join(Environment.NewLine, ValidationResults[propertyName, ValidationSeverity.Error].Select(e => e.Message)
#if NET35
            .ToArray()
#endif
        );

        string IDataErrorInfo.Error => String.Join(Environment.NewLine, ValidationResults.Errors.Select(e => e.Message)
#if NET35
                .ToArray()
#endif
        );
    }
}
