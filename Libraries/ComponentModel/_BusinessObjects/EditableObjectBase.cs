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
using System.Threading;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with editing capabilities. Starting an edit session saves a snapshot about the stored properties, which can be either applied or reverted.
    /// Works for properties set through the <see cref="IPersistableObject"/> implementation and the <see cref="PersistableObjectBase.Set">PersistableObjectBase.Set</see> method.
    /// </summary>
    /// <seealso cref="KGySoft.ComponentModel.UndoableObjectBase" />
    /// <seealso cref="System.ComponentModel.IEditableObject" />
    public abstract class EditableObjectBase : PersistableObjectBase, ICanEdit
    {
        private EditableHelper editable;

        internal ICanEdit AsEditable
        {
            get
            {
                if (editable == null)
                    Interlocked.CompareExchange(ref editable, new EditableHelper(this), null);
                return editable;
            }
        }

        public void BeginEdit() => AsEditable.BeginEdit();
        public void EndEdit() => AsEditable.EndEdit();
        public void CancelEdit() => AsEditable.CancelEdit();
        public int EditLevel => AsEditable.EditLevel;
    }
}
