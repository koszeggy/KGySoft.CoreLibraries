using System;
using System.ComponentModel;
using System.Linq;
using KGySoft.Annotations;

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
        /// Gets whether this instance is valid. That is, if <see cref="IValidatingObject.Validate">Validate</see> method does not return any entries where
        /// the value of <see cref="ValidationResult.Severity"/> is <see cref="ValidationSeverity.Error"/>.
        /// </summary>
        /// <value><see langword="true"/> if this instance is valid; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsValid => isValid ?? (bool)(isValid = !(cachedValidationResults ?? Validate()).HasErrors);

        /// <summary>
        /// Validates this instance and returns the validation results.
        /// </summary>
        /// <returns>A <see cref="ValidationResultsCollection" /> instance containing the validation results.</returns>
        public ValidationResultsCollection Validate()
        {
            ValidationResultsCollection result = DoValidation();
            if (result == null)
                throw new InvalidOperationException(Res.Get(Res.DoValidationNull));
            bool newIsValid = !result.HasErrors;
            bool raiseChange = newIsValid != lastIsValid;
            isValid = lastIsValid = newIsValid;
            cachedValidationResults = result;

            // not raising before setting cached fields because getting IsValid could cause infinite loop otherwise.
            if (raiseChange)
            {
                OnPropertyChanging(new PropertyChangingExtendedEventArgs(!newIsValid, nameof(IsValid)));
                OnPropertyChanged(new PropertyChangedExtendedEventArgs(!newIsValid, newIsValid, nameof(IsValid)));
            }

            return result;
        }

        /// <summary>
        /// Performs the validation on this instance and returns the validation results. Must not return <see langword="null"/>.
        /// </summary>
        /// <returns> A <see cref="ValidationResultsCollection" /> instance containing the validation results.</returns>
        [NotNull]
        protected abstract ValidationResultsCollection DoValidation();

        /// <summary>
        /// Raises the <see cref="ObservableObjectBase.PropertyChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedExtendedEventArgs" /> instance containing the event data.</param>
        protected internal override void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Invalidating cached validation results if an affected property has changed.
            if (isValid != null && AffectsModifiedState(e.PropertyName))
            {
                isValid = null;
                cachedValidationResults = null;
            }
        }

        /// <summary>
        /// Gets whether the change of the specified <paramref name="propertyName" /> affects the <see cref="ObservableObjectBase.IsModified" /> property.
        /// <br />The <see cref="EditableObjectBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/> and <see cref="IsValid"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true" /> if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) => base.AffectsModifiedState(propertyName) && propertyName != nameof(IsValid);

        string IDataErrorInfo.this[string propertyName] => String.Join(Environment.NewLine, (cachedValidationResults ?? Validate()).Errors.Where(e => e.PropertyName == propertyName).Select(e => e.Message));
        string IDataErrorInfo.Error => (cachedValidationResults ?? Validate()).Error;
    }
}
