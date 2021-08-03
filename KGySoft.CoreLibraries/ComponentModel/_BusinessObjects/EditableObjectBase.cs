#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EditableObjectBase.cs
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

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with editing capabilities by adding <see cref="ICanEdit"/> implementation to the <see cref="PersistableObjectBase"/> class.
    /// Starting an edit session saves a snapshot of the stored properties, which can be either applied or reverted.
    /// Saving and restoring properties works for properties set through the <see cref="IPersistableObject"/> implementation and the <see cref="ObservableObjectBase.Set">ObservableObjectBase.Set</see> method.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <seealso cref="ICanEdit" />
    /// <seealso cref="IEditableObject" />
    /// <seealso cref="ObservableObjectBase"/>
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="UndoableObjectBase" />
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ModelBase" />
    /// <remarks>
    /// <para>An object derived from <see cref="EditableObjectBase"/> is able to save a snapshot of its properties (the ones, which are set through the <see cref="IPersistableObject"/> implementation
    /// or the <see cref="ObservableObjectBase.Set">ObservableObjectBase.Set</see> method).</para>
    /// <para>A new snapshot can be saved by the <see cref="BeginNewEdit">BeginNewEdit</see> method. This can be called by a UI before starting the editing.</para>
    /// <para>Call the <see cref="CommitLastEdit">CommitLastEdit</see> method to apply the changes since the last <see cref="BeginNewEdit">BeginNewEdit</see> call.</para>
    /// <para>Call the <see cref="RevertLastEdit">RevertLastEdit</see> method to discard the changes since the last <see cref="BeginNewEdit">BeginNewEdit</see> call.</para>
    /// <para>The editing sessions can be nested by calling <see cref="BeginNewEdit">BeginNewEdit</see> method multiple times. The number of the <see cref="BeginNewEdit">BeginNewEdit</see> calls without
    /// a corresponding <see cref="CommitLastEdit">CommitLastEdit</see> or <see cref="RevertLastEdit">RevertLastEdit</see> call is indicated by the <see cref="EditLevel"/> property.</para>
    /// <para>By calling the <see cref="TryCommitAllEdits">TryCommitAllEdits</see> and <see cref="TryRevertAllEdits">TryRevertAllEdits</see> methods all of the previous <see cref="BeginNewEdit">BeginNewEdit</see>
    /// calls can be applied or discarded, respectively. These methods return a <see cref="bool">bool</see> value indicating whether any action occurred.</para>
    /// <para><note>When it is needed to be determined whether a type has editing capabilities use the <see cref="ICanEdit"/> interface instead the <see cref="EditableObjectBase"/> type
    /// because other editable types, such as the <see cref="ModelBase"/> class are not necessarily derived from the <see cref="EditableObjectBase"/> class.
    /// See also the class diagram of the business object base classes of the <c>KGySoft.CoreLibraries</c> assembly:</note>
    /// <img src="../Help/Images/ComponentModel_BusinessObjects.png" alt="Class diagram of business object base classes"/></para>
    /// <para><strong>Differences from <see cref="UndoableObjectBase"/></strong>:
    /// <br/>Both <see cref="UndoableObjectBase"/> and <see cref="EditableObjectBase"/> can revert changes; however, the aspects of these classes are different.
    /// <list type="bullet">
    /// <item>An undoable class (which implements <see cref="ICanUndo"/> or <see cref="ICanUndoRedo"/> interfaces such as <see cref="UndoableObjectBase"/>) is able to undo (or redo) any changes made so far either step-by-step or in a single step.</item>
    /// <item>On the other hand, an editable class (which implements <see cref="ICanEdit"/> such as <see cref="EditableObjectBase"/>) is able to start editing sessions by saving a snapshot of its current state, which states are committable and revertible.</item>
    /// <item>Undo and editing features are independent from each other and a class is allowed to implement both (like the <see cref="ModelBase"/> class).</item>
    /// </list>
    /// </para>
    /// <para><strong><see cref="IEditableObject"/> support</strong>:
    /// <br/><see cref="EditableObjectBase"/> implements also the <see cref="IEditableObject">System.ComponentModel.IEditableObject</see> interface, which is the standard way in .NET to support editing.
    /// Several controls of different UI frameworks automatically call its members if an object, which implements it, is bound to a grid control, for example. However, some frameworks (such as Windows Forms) do not always
    /// call the begin/end operations the same times, which can cause problems. To handle this, you can override the <see cref="EditableObjectBehavior"/> property in your class to adjust the editing behavior via the <see cref="IEditableObject"/> interface.
    /// If not overridden, the <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.DisableNesting"/>, which works in most cases.
    /// <br/>This is how the <see cref="IEditableObject"/> members are mapped in the <see cref="EditableObjectBase"/> class:
    /// <list type="bullet">
    /// <item><see cref="IEditableObject.BeginEdit">IEditableObject.BeginEdit</see>:
    /// <list type="bullet">
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.Disabled"/>, then the call is ignored.</item>
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.DisableNesting"/>, then <see cref="BeginNewEdit">BeginNewEdit</see> is called only if <see cref="EditLevel"/> returns 0; otherwise, the call is ignored.</item>
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.AllowNesting"/>, then <see cref="BeginNewEdit">BeginNewEdit</see> is called.</item>
    /// </list>
    /// </item>
    /// <item><see cref="IEditableObject.EndEdit">IEditableObject.EndEdit</see>:
    /// <list type="bullet">
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.Disabled"/>, then the call is ignored.</item>
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.DisableNesting"/>, then <see cref="TryCommitAllEdits">TryCommitAllEdits</see> is called.</item>
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.AllowNesting"/>, then <see cref="CommitLastEdit">CommitLastEdit</see> is called.</item>
    /// </list>
    /// </item>
    /// <item><see cref="IEditableObject.CancelEdit">IEditableObject.CancelEdit</see>:
    /// <list type="bullet">
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.Disabled"/>, then the call is ignored.</item>
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.DisableNesting"/>, then <see cref="TryRevertAllEdits">TryRevertAllEdits</see> is called.</item>
    /// <item>If <see cref="EditableObjectBehavior"/> returns <see cref="ComponentModel.EditableObjectBehavior.AllowNesting"/>, then <see cref="RevertLastEdit">RevertLastEdit</see> is called.</item>
    /// </list>
    /// </item>
    /// </list>
    /// </para>
    /// <note type="implement">For an example see the <strong>Remarks</strong> section of the <see cref="ObservableObjectBase"/> class.
    /// The same applies also for the <see cref="EditableObjectBase"/> class in terms of implementation.</note>
    /// </remarks>
    [Serializable]
    public abstract class EditableObjectBase : PersistableObjectBase, ICanEdit, IEditableObject
    {
        #region Fields

        private EditableHelper? editable;

        #endregion

        #region Properties

        #region Public Properties

        /// <inheritdoc />
        public int EditLevel => Editable.EditLevel;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets how the object should behave if treated as an <see cref="IEditableObject"/>.
        /// <br/>The base implementation returns <see cref="ComponentModel.EditableObjectBehavior.DisableNesting"/>.
        /// </summary>
        /// <remarks>
        /// <note>To see how this affects the editing behavior see the <strong>Remarks</strong> section of the <see cref="EditableObjectBase"/> class.</note>
        /// </remarks>
        /// <seealso cref="ComponentModel.EditableObjectBehavior"/>
        /// <seealso cref="IEditableObject"/>
        protected virtual EditableObjectBehavior EditableObjectBehavior => EditableObjectBehavior.DisableNesting;

        #endregion

        #region Private Properties

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

        #endregion

        #region Methods

        #region Public Methods

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
        /// <br />The <see cref="EditableObjectBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/> and <see cref="EditLevel"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true"/>&#160;if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) => base.AffectsModifiedState(propertyName) && propertyName != nameof(EditLevel);

        #endregion

        #region Explicitly Implemented Interface Methods

        void IEditableObject.BeginEdit() => Editable.BeginEdit(EditableObjectBehavior);
        void IEditableObject.EndEdit() => Editable.EndEdit(EditableObjectBehavior);
        void IEditableObject.CancelEdit() => Editable.CancelEdit(EditableObjectBehavior);

        #endregion

        #endregion
    }
}
