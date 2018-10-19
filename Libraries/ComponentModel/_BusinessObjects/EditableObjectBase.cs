#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EditableObjectBase.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.ComponentModel;
using System.Threading;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with editing capabilities. Starting an edit session saves a snapshot about the stored properties, which can be either applied or reverted.
    /// Works for properties set through the <see cref="IPersistableObject"/> implementation and the <see cref="ObservableObjectBase.Set">ObservableObjectBase.Set</see> method.
    /// </summary>
    /// <seealso cref="ICanEdit" />
    /// <seealso cref="IEditableObject" />
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="UndoableObjectBase" />
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ModelBase" />
    /// TODO
    /// - note: differences to UndoableObjectBase
    /// - note: Other editable classes are not derived from EditableObjectBase - akár kép is! - Avoid casting to EditableObjectBase because for example ModelBase does not implement it. Cast to ICanEdit instead
    /// - note: IsModified vs EditLevel (vs CanUndo)
    /// - Example (or just mention the one in the base, which also applies here)
    /// - IEditableObject: see EditableObjectBehavior: If used in WinForms/WPF environment NestingDisabled is recommended (default behavior) because neither WinForms nor WPF calls their methods symmetrically.
    public abstract class EditableObjectBase : PersistableObjectBase, ICanEdit, IEditableObject
    {
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

        /// <summary>
        /// Gets whether the change of the specified <paramref name="propertyName" /> affects the <see cref="ObservableObjectBase.IsModified" /> property.
        /// <br />The <see cref="EditableObjectBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/> and <see cref="EditLevel"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true" /> if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) => base.AffectsModifiedState(propertyName) && propertyName != nameof(EditLevel);
    }
}
