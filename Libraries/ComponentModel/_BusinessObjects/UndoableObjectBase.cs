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
    /// - IRevertibleChangeTracking
    /// </remarks>
    /// <seealso cref="ICanUndoRedo" />
    /// <seealso cref="IRevertibleChangeTracking" />
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="EditableObjectBase" />
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ModelBase" />
    public abstract class UndoableObjectBase : PersistableObjectBase, ICanUndoRedo, ICanUndoInternal, IRevertibleChangeTracking
    {
        private UndoableHelper undoable;

        internal ICanUndoRedo AsUndoable
        {
            get
            {
                if (undoable == null)
                    Interlocked.CompareExchange(ref undoable, new UndoableHelper(this), null);
                return undoable;
            }
        }

        /// <inheritdoc />
        public bool CanUndo => AsUndoable.CanUndo;

        /// <inheritdoc />
        public bool TryUndo() => AsUndoable.TryUndo();

        /// <summary>
        /// Gets or sets the undo capacity.
        /// <br/>Default value: <c>20</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> must be greater or equal to 0.</exception>
        public int UndoCapacity
        {
            get => AsUndoable.UndoCapacity;
            set => AsUndoable.UndoCapacity = value;
        }

        /// <inheritdoc />
        public void UndoAll() => AsUndoable.UndoAll();

        void ICanUndoInternal.SuspendUndo() => undoable.SuspendUndo();
        void ICanUndoInternal.ResumeUndo() => undoable.ResumeUndo();

        /// <summary>
        /// Clears the undo/redo history without performing any undo.
        /// </summary>
        public void ClearUndoHistory() => AsUndoable.ClearUndoHistory();

        /// <inheritdoc />
        public bool CanRedo => AsUndoable.CanRedo;

        /// <inheritdoc />
        public bool TryRedo() => AsUndoable.TryRedo();

        /// <inheritdoc />
        public void RedoAll() => AsUndoable.RedoAll();

        bool IChangeTracking.IsChanged => CanUndo;
        void IChangeTracking.AcceptChanges() => ClearUndoHistory();
        void IRevertibleChangeTracking.RejectChanges() => UndoAll();
    }
}
