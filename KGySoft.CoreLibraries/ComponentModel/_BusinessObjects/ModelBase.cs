#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ModelBase.cs
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
using System.Threading;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a base object for model classes or business objects, which can validate their state, have undo/redo capability and can support committable/revertible editing.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <note>This class unifies the capabilities of <see cref="ValidatingObjectBase"/>, <see cref="EditableObjectBase"/> and <see cref="UndoableObjectBase"/> classes.
    /// If you don't need all of these features you can pick one of these classes as a base class for your business objects. If you need none of these features but only raising events about property changes
    /// you can use the <see cref="ObservableObjectBase"/> or <see cref="PersistableObjectBase"/> classes.
    /// See also the class diagram of the business object base classes of the <c>KGySoft.CoreLibraries</c> assembly:</note>
    /// <img src="../Help/Images/ComponentModel_BusinessObjects.png" alt="Class diagram of business object base classes"/>
    /// <para><strong>Differences from <see cref="ValidatingObjectBase"/></strong>:
    /// <list type="bullet">
    /// <item>When deriving from <see cref="ModelBase"/>, overriding <see cref="DoValidation">DoValidation</see> method is not mandatory.
    /// By default, <see cref="DoValidation">DoValidation</see> returns an empty <see cref="ValidationResultsCollection"/> instance.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ValidatingObjectBase"/> class for an example about how to validate properties by overriding the <see cref="DoValidation">DoValidation</see> method.</item>
    /// <item>The <see cref="ModelBase"/> implements the <see cref="ICanUndo"/> and <see cref="IRevertibleChangeTracking"/> interfaces to support undo/redo functionality.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="UndoableObjectBase"/> class for details about the undo/redo feature. The same applies also for the <see cref="ModelBase"/> class.</item>
    /// <item>The <see cref="ModelBase"/> implements the <see cref="ICanEdit"/> and <see cref="IEditableObject"/> interfaces to support object editing.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="EditableObjectBase"/> class for details about object editing. The same applies also for the <see cref="ModelBase"/> class.</item>
    /// </list>
    /// </para>
    /// <para>Though undo/redo and object editing are independent features, the undo history is cleared when an editing session is reverted by the <see cref="RevertLastEdit">RevertLastEdit</see> or <see cref="TryRevertAllEdits">TryRevertAllEdits</see>
    /// methods to avoid confusion.</para>
    /// <note type="implement">
    /// <para>See the <strong>Remarks</strong> section of the <see cref="ObservableObjectBase"/> class for an example about how to define properties in a derived class.</para>
    /// <para>See the <strong>Remarks</strong> section of the <see cref="ValidatingObjectBase"/> class for an example about how to validate properties in a derived class.</para>
    /// </note>
    /// </remarks>
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ICanUndo" />
    /// <seealso cref="ICanUndoRedo" />
    /// <seealso cref="IRevertibleChangeTracking" />
    /// <seealso cref="ICanEdit" />
    /// <seealso cref="IEditableObject" />
    [Serializable]
    public abstract class ModelBase : ValidatingObjectBase,
        ICanUndoRedo, ICanUndoInternal, IRevertibleChangeTracking, // Undoable
        ICanEdit, IEditableObject // Editable
    {
        #region Fields

        #region Static Fields

        private static readonly string[] ignoreModifiedProperties = { nameof(EditLevel), nameof(UndoCapacity), nameof(CanRedo), nameof(CanUndo) };

        #endregion

        #region Instance Fields

        private UndoableHelper? undoable;
        private EditableHelper? editable;

        #endregion

        #endregion

        #region Properties

        #region Public Properties

        /// <inheritdoc />
        public bool CanUndo => Undoable.CanUndo;

        /// <inheritdoc />
        public bool CanRedo => Undoable.CanRedo;

        /// <inheritdoc />
        public int EditLevel => Editable.EditLevel;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets or sets the undo capacity.
        /// <br/>Default value: <c>20</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> must be greater or equal to 0.</exception>
        protected int UndoCapacity
        {
            get => Undoable.UndoCapacity;
            set => Undoable.UndoCapacity = value;
        }

        /// <summary>
        /// Gets how the object should behave if treated as an <see cref="IEditableObject"/>.
        /// <br/>The base implementation returns <see cref="ComponentModel.EditableObjectBehavior.DisableNesting"/>.
        /// </summary>
        /// <remarks>
        /// <note>To see how this affects the editing behavior see the <strong>Remarks</strong> section of the <see cref="EditableObjectBase"/> class, which applies also for the <see cref="ModelBase"/> class.</note>
        /// </remarks>
        /// <seealso cref="ComponentModel.EditableObjectBehavior"/>
        /// <seealso cref="IEditableObject"/>
        protected virtual EditableObjectBehavior EditableObjectBehavior => EditableObjectBehavior.DisableNesting;

        #endregion

        #region Private Properties

        private UndoableHelper Undoable
        {
            get
            {
                if (undoable == null)
                    Interlocked.CompareExchange(ref undoable, new UndoableHelper(this), null);
                return undoable;
            }
        }

        private EditableHelper Editable
        {
            get
            {
                if (editable == null)
                    Interlocked.CompareExchange(ref editable, new EditableHelper(this), null);
                return editable;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        bool IChangeTracking.IsChanged => CanUndo;

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        /// <inheritdoc />
        public bool TryUndo() => Undoable.TryUndo();

        /// <inheritdoc />
        public void UndoAll() => Undoable.UndoAll();

        /// <summary>
        /// Clears the undo/redo history without performing any undo.
        /// </summary>
        public void ClearUndoHistory() => Undoable.ClearUndoHistory();

        /// <inheritdoc />
        public bool TryRedo() => Undoable.TryRedo();

        /// <inheritdoc />
        public void RedoAll() => Undoable.RedoAll();

        /// <inheritdoc />
        public void BeginNewEdit() => Editable.BeginNewEdit();

        /// <inheritdoc />
        public void CommitLastEdit() => Editable.CommitLastEdit();

        /// <inheritdoc />
        public void RevertLastEdit() => Editable.RevertLastEdit();

        /// <inheritdoc />
        public bool TryCommitAllEdits() => Editable.TryCommitAllEdits();

        /// <inheritdoc />
        public bool TryRevertAllEdits() => Editable.TryRevertAllEdits();

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets whether the change of the specified <paramref name="propertyName" /> affects the <see cref="ObservableObjectBase.IsModified" /> property.
        /// <br />The <see cref="ModelBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/>, <see cref="EditLevel"/>, <see cref="ValidatingObjectBase.IsValid"/>,
        /// <see cref="UndoCapacity"/>, <see cref="CanUndo"/> and <see cref="CanRedo"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true"/>&#160;if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) =>
            base.AffectsModifiedState(propertyName) && !propertyName.In(ignoreModifiedProperties);

        /// <inheritdoc />
        protected override ValidationResultsCollection DoValidation() => new ValidationResultsCollection();

        #endregion

        #region Protected Internal Methods

        /// <inheritdoc />
        protected internal override void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
        {
            if (e == null!)
                Throw.ArgumentNullException(Argument.e);
            if (Properties.ContainsKey(e.PropertyName!))
                Undoable.HandlePropertyChanged(e);

            base.OnPropertyChanged(e);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void ICanUndoInternal.SuspendUndo() => Undoable.SuspendUndo();
        void ICanUndoInternal.ResumeUndo() => Undoable.ResumeUndo();
        void IChangeTracking.AcceptChanges() => ClearUndoHistory();
        void IRevertibleChangeTracking.RejectChanges() => UndoAll();
        void IEditableObject.BeginEdit() => Editable.BeginEdit(EditableObjectBehavior);
        void IEditableObject.EndEdit() => Editable.EndEdit(EditableObjectBehavior);
        void IEditableObject.CancelEdit() => Editable.CancelEdit(EditableObjectBehavior);

        #endregion

        #endregion
    }
}
