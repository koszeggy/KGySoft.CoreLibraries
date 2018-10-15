using System;
using System.ComponentModel;
using System.Threading;
using KGySoft.Libraries;

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
        private readonly UndoableHelper undoable;
        private static readonly string[] ignoreModifiedProperties = { nameof(EditLevel), nameof(UndoCapacity), nameof(CanRedo), nameof(CanUndo) };

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBase"/> class.
        /// </summary>
        protected ModelBase() => undoable = new UndoableHelper(this);

        /// <inheritdoc />
        public bool CanUndo => undoable.CanUndo;

        /// <inheritdoc />
        public bool TryUndo() => undoable.TryUndo();

        /// <summary>
        /// Gets or sets the undo capacity.
        /// <br/>Default value: <c>20</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> must be greater or equal to 0.</exception>
        public int UndoCapacity
        {
            get => undoable.UndoCapacity;
            set => undoable.UndoCapacity = value;
        }

        /// <inheritdoc />
        public void UndoAll() => undoable.UndoAll();

        void ICanUndoInternal.SuspendUndo() => undoable.SuspendUndo();
        void ICanUndoInternal.ResumeUndo() => undoable.ResumeUndo();

        /// <summary>
        /// Clears the undo/redo history without performing any undo.
        /// </summary>
        public void ClearUndoHistory() => undoable.ClearUndoHistory();

        /// <inheritdoc />
        public bool CanRedo => undoable.CanRedo;

        /// <inheritdoc />
        public bool TryRedo() => undoable.TryRedo();

        /// <inheritdoc />
        public void RedoAll() => undoable.RedoAll();

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

        void IEditableObject.BeginEdit() => Editable.BeginEdit(EditableObjectBehavior);
        void IEditableObject.EndEdit() => Editable.EndEdit(EditableObjectBehavior);
        void IEditableObject.CancelEdit() => Editable.CancelEdit(EditableObjectBehavior);
    }
}
