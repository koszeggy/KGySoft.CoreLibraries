using System;
using System.ComponentModel;
using System.Threading;
using KGySoft.CoreLibraries;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a base object for model classes or business objects, which can validate their state, have undo/redo capability and can support committable/revertible editing.
    /// </summary>
    /// <remarks>
    /// TODO: This class unifies the capabilities of ValidatingObjectBase, EditableObjectBase and UndoableObjectBase
    /// - All comments from those classes
    /// </remarks>
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ICanUndoRedo" />
    /// <seealso cref="ICanUndoInternal" />
    /// <seealso cref="IRevertibleChangeTracking" />
    /// <seealso cref="ICanEdit" />
    /// <seealso cref="IEditableObject" />
    public abstract class ModelBase : ValidatingObjectBase,
        ICanUndoRedo, ICanUndoInternal, IRevertibleChangeTracking, // Undoable
        ICanEdit, IEditableObject // Editable
    {
        private UndoableHelper undoable;
        private static readonly string[] ignoreModifiedProperties = { nameof(EditLevel), nameof(UndoCapacity), nameof(CanRedo), nameof(CanUndo) };

        internal UndoableHelper Undoable
        {
            get
            {
                if (undoable == null)
                    Interlocked.CompareExchange(ref undoable, new UndoableHelper(this), null);
                return undoable;
            }
        }

        /// <inheritdoc />
        public bool CanUndo => Undoable.CanUndo;

        /// <inheritdoc />
        public bool TryUndo() => Undoable.TryUndo();

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

        /// <inheritdoc />
        public void UndoAll() => Undoable.UndoAll();

        void ICanUndoInternal.SuspendUndo() => Undoable.SuspendUndo();
        void ICanUndoInternal.ResumeUndo() => Undoable.ResumeUndo();

        /// <summary>
        /// Clears the undo/redo history without performing any undo.
        /// </summary>
        public void ClearUndoHistory() => Undoable.ClearUndoHistory();

        /// <inheritdoc />
        public bool CanRedo => Undoable.CanRedo;

        /// <inheritdoc />
        public bool TryRedo() => Undoable.TryRedo();

        /// <inheritdoc />
        public void RedoAll() => Undoable.RedoAll();

        bool IChangeTracking.IsChanged => CanUndo;
        void IChangeTracking.AcceptChanges() => ClearUndoHistory();
        void IRevertibleChangeTracking.RejectChanges() => UndoAll();

        /// <summary>
        /// Gets whether the change of the specified <paramref name="propertyName" /> affects the <see cref="ObservableObjectBase.IsModified" /> property.
        /// <br />The <see cref="ModelBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/>, <see cref="EditLevel"/>, <see cref="ValidatingObjectBase.IsValid"/>,
        /// <see cref="UndoCapacity"/>, <see cref="CanUndo"/> and <see cref="CanRedo"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true" /> if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) =>
            base.AffectsModifiedState(propertyName) && !propertyName.In(ignoreModifiedProperties);

        /// <summary>
        /// Performs the validation on this instance and returns the validation results. The <see cref="ModelBase"/> class returns an empty <see cref="ValidationResultsCollection"/>.
        /// </summary>
        /// <returns>A <see cref="ValidationResultsCollection" /> instance containing the validation results. The <see cref="ModelBase"/> class returns an empty <see cref="ValidationResultsCollection"/>.</returns>
        protected override ValidationResultsCollection DoValidation() => new ValidationResultsCollection();

        private EditableHelper editable;

        internal EditableHelper Editable
        {
            get
            {
                if (editable == null)
                    Interlocked.CompareExchange(ref editable, new EditableHelper(this), null);
                return editable;
            }
        }

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

        /// <inheritdoc />
        public int EditLevel => Editable.EditLevel;

        /// <summary>
        /// Gets how the object should behave if treated as an <see cref="IEditableObject"/>.
        /// <br/>The base implementation returns <see cref="ComponentModel.EditableObjectBehavior.NestingDisabled"/>.
        /// </summary>
        /// <seealso cref="ComponentModel.EditableObjectBehavior"/>
        /// <seealso cref="IEditableObject"/>
        protected virtual EditableObjectBehavior EditableObjectBehavior => EditableObjectBehavior.NestingDisabled;

        /// <summary>
        /// Raises the <see cref="ObservableObjectBase.PropertyChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedExtendedEventArgs" /> instance containing the event data.</param>
        protected internal override void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
        {
            if (PropertiesInternal.ContainsKey(e.PropertyName))
                Undoable.HandlePropertyChanged(e);

            base.OnPropertyChanged(e);
        }

        void IEditableObject.BeginEdit() => Editable.BeginEdit(EditableObjectBehavior);
        void IEditableObject.EndEdit() => Editable.EndEdit(EditableObjectBehavior);
        void IEditableObject.CancelEdit() => Editable.CancelEdit(EditableObjectBehavior);
    }
}
