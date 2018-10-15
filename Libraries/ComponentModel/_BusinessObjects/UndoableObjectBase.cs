#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: UndoableObjectBase.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.Collections;
using KGySoft.Libraries;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with step-by-step undo/redo capabilities by adding <see cref="ICanUndoRedo"/> implementation to <see cref="PersistableObjectBase"/>.
    /// Undoing and redoing works for properties set through the <see cref="IPersistableObject"/> implementation and the <see cref="PersistableObjectBase.Set">PersistableObjectBase.Set</see> method.
    /// </summary>
    /// <remarks>
    /// <para>An object derived from <see cref="UndoableObjectBase"/> continuously tracks the property changes of properties, which are set through the <see cref="IPersistableObject"/> implementation
    /// and the <see cref="PersistableObjectBase.Set">PersistableObjectBase.Set</see> method.</para> 
    /// TODO
    /// - note: differences to EditableObjectBase
    /// - note: Other undoable classes are not derived from UndoableObjectBase - akár kép is! - Avoid casting to UndoableObjectBase because for example ModelBase does not implement it. Cast to ICanUndoRedo instead
    /// - note: IsModified vs CanUndo and IRevertibleChangeTracking.IsChanged
    /// - Example (or just mention the one in the base, which also applies here)
    /// - IRevertibleChangeTracking implementation
    /// </remarks>
    /// <seealso cref="ICanUndoRedo" />
    /// <seealso cref="IRevertibleChangeTracking" />
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="EditableObjectBase" />
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ModelBase" />
    public abstract class UndoableObjectBase : PersistableObjectBase, ICanUndoRedo, ICanUndoInternal, IRevertibleChangeTracking
    {
        private readonly UndoableHelper undoable;
        private static readonly string[] ignoreModifiedProperties = { nameof(UndoCapacity), nameof(CanRedo), nameof(CanUndo) };

        protected UndoableObjectBase() => undoable = new UndoableHelper(this);

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
        /// <br />The <see cref="UndoableObjectBase" /> implementation excludes the <see cref="ObservableObjectBase.IsModified"/>, <see cref="UndoCapacity"/>,
        /// <see cref="CanUndo"/> and <see cref="CanRedo"/> properties.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true" /> if changing of the specified <paramref name="propertyName" /> affects the value of the <see cref="ObservableObjectBase.IsModified" /> property; otherwise, <see langword="false" />.</returns>
        protected override bool AffectsModifiedState(string propertyName) => 
            base.AffectsModifiedState(propertyName) && !propertyName.In(ignoreModifiedProperties);
    }
}
