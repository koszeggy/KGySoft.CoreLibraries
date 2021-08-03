#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: UndoableObjectBase.cs
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
    /// Represents an object with step-by-step undo/redo capabilities by adding <see cref="ICanUndoRedo"/> implementation to the <see cref="PersistableObjectBase"/> class.
    /// Undoing and redoing works for properties set through the <see cref="IPersistableObject"/> implementation and the <see cref="ObservableObjectBase.Set">ObservableObjectBase.Set</see> method.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>An object derived from <see cref="UndoableObjectBase"/> continuously tracks the property changes of properties, which are set through the <see cref="IPersistableObject"/> implementation
    /// and the <see cref="ObservableObjectBase.Set">ObservableObjectBase.Set</see> method.
    /// <note>When it is needed to be determined whether a type has undo/redo capability use the <see cref="ICanUndo"/> or <see cref="ICanUndoRedo"/> interfaces instead the <see cref="UndoableObjectBase"/> type
    /// because other undoable types, such as the <see cref="ModelBase"/> class are not necessarily derived from the <see cref="UndoableObjectBase"/> class.
    /// See also the class diagram of the business object base classes of the <c>KGySoft.CoreLibraries</c> assembly:</note>
    /// <img src="../Help/Images/ComponentModel_BusinessObjects.png" alt="Class diagram of business object base classes"/></para>
    /// <para><strong>Differences from <see cref="EditableObjectBase"/></strong>:
    /// <br/>Both <see cref="UndoableObjectBase"/> and <see cref="EditableObjectBase"/> can revert changes; however, the aspects of these classes are different.
    /// <list type="bullet">
    /// <item>An undoable class (which implements <see cref="ICanUndo"/> or <see cref="ICanUndoRedo"/> interfaces such as <see cref="UndoableObjectBase"/>) is able to undo (or redo) any changes made so far either step-by-step or in a single step.</item>
    /// <item>On the other hand, an editable class (which implements <see cref="ICanEdit"/> such as <see cref="EditableObjectBase"/>) is able to start editing sessions by saving a snapshot of its current state, which states are committable and revertible.</item>
    /// <item>Undo and editing features are independent from each other and a class is allowed to implement both (like the <see cref="ModelBase"/> class).</item>
    /// </list>
    /// </para>
    /// <para><strong><see cref="IRevertibleChangeTracking"/> support</strong>:
    /// <br/><see cref="UndoableObjectBase"/> implements also the <see cref="IRevertibleChangeTracking">System.ComponentModel.IRevertibleChangeTracking</see> interface, which is the standard way in .NET to support undoing.
    /// <br/>This is how the <see cref="IRevertibleChangeTracking"/> members are mapped in the <see cref="UndoableObjectBase"/> class:
    /// <list type="bullet">
    /// <item><see cref="IChangeTracking.IsChanged">IChangeTracking.IsChanged</see>: Returns <see cref="CanUndo"/>.</item>
    /// <item><see cref="IChangeTracking.AcceptChanges">IChangeTracking.AcceptChanges</see>: Calls <see cref="ClearUndoHistory">ClearUndoHistory</see>.</item>
    /// <item><see cref="IRevertibleChangeTracking.RejectChanges">IRevertibleChangeTracking.RejectChanges</see>: Calls <see cref="UndoAll">UndoAll</see>.</item>
    /// </list>
    /// </para>
    /// <para><strong><see cref="ObservableObjectBase.IsModified"/> vs. <see cref="CanUndo">CanUndo</see></strong>:
    /// <list type="bullet">
    /// <item>The <see cref="ObservableObjectBase.IsModified"/> property reflects the object's "dirty" state, meaning, it has been changed since the initialization or last save.
    /// The modified state can be cleared by the <see cref="ObservableObjectBase.SetModified">SetModified</see> method. Clearing the modified state (eg. on saving the object) does not affect the undo capabilities, though.</item>
    /// <item>The <see cref="CanUndo"/> property tells whether there are any steps to undo. On saving an object the modified state can be cleared and still there can be undoable steps. And vice versa, undoing all steps will not clear the modified state.</item>
    /// </list>
    /// </para>
    /// <note type="implement">For an example see the <strong>Remarks</strong> section of the <see cref="ObservableObjectBase"/> class.
    /// The same applies also for the <see cref="UndoableObjectBase"/> class in terms of implementation.</note>
    /// </remarks>
    /// <seealso cref="ICanUndo" />
    /// <seealso cref="ICanUndoRedo" />
    /// <seealso cref="IRevertibleChangeTracking" />
    /// <seealso cref="ObservableObjectBase"/>
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="EditableObjectBase" />
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ModelBase" />
    [Serializable]
    public abstract class UndoableObjectBase : PersistableObjectBase, ICanUndoRedo, ICanUndoInternal, IRevertibleChangeTracking
    {
        #region Fields

        #region Static Fields

        private static readonly string[] ignoreModifiedProperties = { nameof(UndoCapacity), nameof(CanRedo), nameof(CanUndo) };

        #endregion

        #region Instance Fields

        private UndoableHelper? undoable;

        #endregion

        #endregion

        #region Properties

        #region Public Properties

        /// <inheritdoc />
        public bool CanUndo => Undoable.CanUndo;

        /// <inheritdoc />
        public bool CanRedo => Undoable.CanRedo;

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

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets whether the change of the specified <paramref name="propertyName" /> affects the <see cref="ObservableObjectBase.IsModified" /> property.
        /// <br />The <see cref="UndoableObjectBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/>, <see cref="UndoCapacity"/>,
        /// <see cref="CanUndo"/> and <see cref="CanRedo"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true"/>&#160;if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) =>
            base.AffectsModifiedState(propertyName) && !propertyName.In(ignoreModifiedProperties);

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

        #endregion

        #endregion
    }
}
