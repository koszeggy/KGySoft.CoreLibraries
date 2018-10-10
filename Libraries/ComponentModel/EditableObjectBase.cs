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

using System;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with editing capabilities. Starting an edit session saves a snapshot about the stored properties, which can be either applied or reverted.
    /// Works for properties set through the <see cref="IPersistableObject"/> implementation and the <see cref="PersistableObjectBase.Set">PersistableObjectBase.Set</see> method.
    /// To access the editing features call the <see cref="AsEditable"/> property, which returns an <see cref="IEditableObject"/> instance.
    /// </summary>
    /// <seealso cref="KGySoft.ComponentModel.UndoableObjectBase" />
    /// <seealso cref="System.ComponentModel.IEditableObject" />
    public abstract class EditableObjectBase : UndoableObjectBase, IEditableObject
    {
        #region Fields

#if NET35
        private readonly Stack<KeyValuePair<string, object>[]> snapshots = new Stack<KeyValuePair<string, object>[]>();
#else
        private readonly ConcurrentStack<KeyValuePair<string, object>[]> snapshots = new ConcurrentStack<KeyValuePair<string, object>[]>();
#endif

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current <see cref="EditableObjectBase"/> as an <see cref="IEditableObject"/> instance.
        /// </summary>
        /// <returns></returns>
        public IEditableObject AsEditable => this;

        #endregion

        #region Methods

        void IEditableObject.BeginEdit()
        {
#if NET35
            lock (snapshots)
                snapshots.Push(PropertiesInternal.ToArray());
#else
            snapshots.Push(PropertiesInternal.ToArray());
#endif

        }

        void IEditableObject.EndEdit()
        {
#if NET35
            lock (snapshots)
            {
                if (snapshots.Count == 0)
                    throw new InvalidOperationException(Res.Get(Res.NotEditing));
                snapshots.Pop();
            }
#else
            if (!snapshots.TryPop(out var _))
                throw new InvalidOperationException(Res.Get(Res.NotEditing));
#endif

        }

        void IEditableObject.CancelEdit()
        {
            lock (snapshots)
            {
                // ReSharper disable once JoinDeclarationAndInitializer - .NET 3.5
                // ReSharper disable once InlineOutVariableDeclaration - >= .NET 4.0
                KeyValuePair<string, object>[] previousState;
#if NET35
                if (snapshots.Count == 0)
                    throw new InvalidOperationException(Res.Get(Res.NotEditing));
                previousState = snapshots.Pop();
#else
                if (!snapshots.TryPop(out previousState))
                    throw new InvalidOperationException(Res.Get(Res.NotEditing));
#endif


                // when reverting edit clearing undo history, too because we can't reset a valid state
                // (even with storing the undo history before every BeginEdit it would be a mess if some steps were undone)
                SuspendUndo();
                try
                {
                    ReplaceProperties(previousState);
                }
                finally
                {
                    ResumeUndo();
                }

                AsUndoable.ClearHistory();
            }
        }

        #endregion
    }
}
