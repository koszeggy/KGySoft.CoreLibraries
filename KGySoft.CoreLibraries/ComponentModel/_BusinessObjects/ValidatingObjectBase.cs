#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ValidatingObjectBase.cs
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
using System.ComponentModel;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with validating capabilities by adding <see cref="IValidatingObject"/> implementation to the <see cref="PersistableObjectBase"/> class.
    /// <br/>See the <strong>Remarks</strong> section for details and examples.
    /// </summary>
    /// <seealso cref="IValidatingObject" />
    /// <seealso cref="IDataErrorInfo" />
    /// <seealso cref="ObservableObjectBase"/>
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="UndoableObjectBase" />
    /// <seealso cref="EditableObjectBase" />
    /// <seealso cref="ModelBase" />
    /// <remarks>
    /// <para>In a derived class the <see cref="DoValidation">DoValidation</see> method must be overridden.</para>
    /// <para>Validation is automatically performed when the <see cref="IsValid"/> or <see cref="ValidationResults"/> property is accessed or when the object is accessed by the standard <see cref="IDataErrorInfo"/> interface.</para>
    /// <para><see cref="IsValid"/> returns <see langword="true"/>&#160;if <see cref="ValidationResults"/> does not contain any entries with <see cref="ValidationSeverity.Error"/> severity.</para>
    /// <para><strong>Differences between <see cref="ObservableObjectBase.CanGetProperty">CanGetProperty</see>/<see cref="ObservableObjectBase.CanSetProperty">CanSetProperty</see> methods and <see cref="IsValid"/>/<see cref="ValidationResults"/> properties</strong>:
    /// <list type="bullet">
    /// <item>When <see cref="ObservableObjectBase.CanGetProperty">CanGetProperty</see> and <see cref="ObservableObjectBase.CanSetProperty">CanSetProperty</see> methods return <see langword="false"/>, then an exception will be thrown
    /// when the property is get or set via the <see cref="ObservableObjectBase"/> and <see cref="IPersistableObject"/> members. Do not use these methods for business validation. Instead, they can be used to prevent accessing an unknown property
    /// or when a property is tried to be set by a value of invalid type.</item>
    /// <item>On the other hand, <see cref="IsValid"/> and <see cref="ValidationResults"/> properties can be used to indicate whether an object contains problematic values. For every issue a severity level (see <see cref="ValidationSeverity"/>) and
    /// a corresponding message can be assigned. These can be displayed by a UI, for example.</item>
    /// </list>
    /// </para>
    /// <para><strong><see cref="IDataErrorInfo"/> support</strong>:
    /// <br/><see cref="ValidatingObjectBase"/> implements also the <see cref="IDataErrorInfo">System.ComponentModel.IDataErrorInfo</see> interface, which is the oldest standard way in .NET to support validation, therefore it is
    /// supported by most frameworks. <see cref="IDataErrorInfo"/> is able to report errors only, so if warnings and validation infos should also be displayed by a UI, then the object should be accessed via the <see cref="IValidatingObject"/> interface.</para>
    /// <example>
    /// The following example shows how to implement property validation:
    /// <code lang="C#"><![CDATA[
    /// public class MyModel : ValidatingObjectBase
    /// {
    ///     public int Id { get => Get<int>(); set => Set(value); }
    ///     
    ///     public string Name { get => Get<string>(); set => Set(value); }
    ///     
    ///     protected override ValidationResultsCollection DoValidation()
    ///     {
    ///         var result = new ValidationResultsCollection();
    /// 
    ///         // info
    ///         if (Id == 0)
    ///             result.AddInfo(nameof(Id), "This will be considered as a new object when saved");
    ///             // or: result.Add(new ValidationResult(nameof(Id), "This will be considered as a new object when saved", ValidationSeverity.Information));
    /// 
    ///         // warning
    ///         if (Id < 0)
    ///             result.AddWarning(nameof(Id), $"{nameof(Id)} is recommended to be greater or equal to 0.");
    /// 
    ///         // error
    ///         if (String.IsNullOrEmpty(Name))
    ///             result.AddError(nameof(Name), $"{nameof(Name)} must not be null or empty.");
    /// 
    ///         return result;
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// <note type="implement">For another example see the <strong>Remarks</strong> section of the <see cref="ObservableObjectBase"/> class.
    /// The same applies also for the <see cref="ValidatingObjectBase"/> class regarding the ways of defining properties in a derived class.</note>
    /// </remarks>
    [Serializable]
    public abstract class ValidatingObjectBase : PersistableObjectBase, IValidatingObject, IDataErrorInfo
    {
        #region Fields

        private ValidationResultsCollection cachedValidationResults = ValidationResultsCollection.Empty;
        private bool cachedIsValid = true;
        private bool isValidationUpToDate;

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets whether this instance is valid. That is, if <see cref="ValidationResults"/> property does not return any entries where
        /// the value of <see cref="ValidationResult.Severity"/> is <see cref="ValidationSeverity.Error"/>.
        /// </summary>
        /// <value><see langword="true"/>&#160;if this instance is valid; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsValid
        {
            get
            {
                if (isValidationUpToDate)
                    return cachedIsValid;

                Validate();
                Debug.Assert(isValidationUpToDate);
                return cachedIsValid;
            }
        }

        /// <summary>
        /// Gets the validation results for this instance.
        /// </summary>
        public ValidationResultsCollection ValidationResults => isValidationUpToDate ? cachedValidationResults! : Validate();

        #endregion

        #region Explicitly Implemented Interface Properties

        string IDataErrorInfo.Error => ValidationResults.Errors.Message;

        #endregion

        #endregion

        #region Indexers

        string IDataErrorInfo.this[string propertyName] => ValidationResults[propertyName].Errors.Message;

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Forces calling the <see cref="DoValidation">DoValidation</see> method and updates the <see cref="ValidationResults"/> and <see cref="IsValid"/> properties.
        /// </summary>
        /// <returns>A <see cref="ValidationResultsCollection" /> instance containing the validation results.</returns>
        /// <remarks>
        /// <note>Normally you don't need to call this method because the <see cref="IsValid"/> and <see cref="ValidationResults"/>
        /// properties are automatically re-evaluated when they are accessed and the object has been changed since last validation.
        /// Explicit validation can be useful when not every change that affects validation triggers the <see cref="ObservableObjectBase.PropertyChanged"/> event.</note>
        /// </remarks>
        public ValidationResultsCollection Validate()
        {
            ValidationResultsCollection result = DoValidation();
            if (result == null!)
                Throw.InvalidOperationException(Res.ComponentModelDoValidationNull);

            if (result.Count == 0)
                result = ValidationResultsCollection.Empty;
            ValidationResultsCollection lastResult = cachedValidationResults;
            bool raiseValidationResultsChanged = lastResult.Count != result.Count;
            if (!raiseValidationResultsChanged)
            {
                // SequenceEqual would be simpler but also slower
                int count = result.Count;
                for (int i = 0; !raiseValidationResultsChanged && i < count; i++)
                    raiseValidationResultsChanged = !result[i].Equals(lastResult[i]);
            }

            // if there was no change we assign back lastResult so cached values will be preserved
            result = raiseValidationResultsChanged ? result.ToReadOnly() : lastResult;
            bool newIsValid = !result.HasErrors;
            bool raiseIsValidChanged = newIsValid != cachedIsValid;

            cachedValidationResults = result;
            cachedIsValid = newIsValid;
            isValidationUpToDate = true;

            if (raiseIsValidChanged)
                OnPropertyChanged(new PropertyChangedExtendedEventArgs(!newIsValid, newIsValid, nameof(IsValid)));
            if (raiseValidationResultsChanged)
                OnPropertyChanged(new PropertyChangedExtendedEventArgs(lastResult, result, nameof(ValidationResults)));

            return result;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Performs the validation on this instance and returns the validation results. Must not return <see langword="null"/>.
        /// </summary>
        /// <returns>A <see cref="ValidationResultsCollection" /> instance containing the validation results.</returns>
        /// <remarks>
        /// <note>See the <strong>Remarks</strong> section of the <see cref="ValidatingObjectBase"/> class for an example.</note>
        /// </remarks>
        protected abstract ValidationResultsCollection DoValidation();

        /// <summary>
        /// Gets whether the change of the specified <paramref name="propertyName" /> affects the <see cref="ObservableObjectBase.IsModified" /> property.
        /// <br />The <see cref="EditableObjectBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/>, <see cref="IsValid"/> and <see cref="ValidationResults"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true"/>&#160;if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) => base.AffectsModifiedState(propertyName) && !propertyName.In(nameof(IsValid), nameof(ValidationResults));

        #endregion

        #region Protected Internal Methods

        /// <inheritdoc />
        protected internal override void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
        {
            if (e == null!)
                Throw.ArgumentNullException(Argument.e);

            // Invalidating cached validation results if an affected property has changed.
            if (isValidationUpToDate && AffectsModifiedState(e.PropertyName!))
                isValidationUpToDate = false;

            base.OnPropertyChanged(e);
        }

        #endregion

        #endregion
    }
}
