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
    /// Represents an object with step-by-step undo/redo capabilities.
    /// Undoing and redoing works for properties set through the <see cref="IPersistableObject"/> implementation and the <see cref="PersistableObjectBase.Set">PersistableObjectBase.Set</see> method.
    /// </summary>
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="ICanUndoRedo" />
    /// <seealso cref="IRevertibleChangeTracking" />
    public abstract class UndoableObjectBase : PersistableObjectBase, ICanUndoRedo, IRevertibleChangeTracking
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
    }
}
