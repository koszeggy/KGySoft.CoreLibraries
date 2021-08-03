#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EditableHelper.cs
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
using System.Collections.Generic;

using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.ComponentModel
{
    [Serializable]
    internal sealed class EditableHelper : ICanEdit
    {
        #region Fields

        private readonly ObservableObjectBase owner;
        private readonly List<IDictionary<string, object?>> snapshots = new List<IDictionary<string, object?>>();

        #endregion

        #region Properties

        public int EditLevel => snapshots.Count;

        #endregion

        #region Constructors

        internal EditableHelper(ObservableObjectBase owner) => this.owner = owner;

        #endregion

        #region Methods

        #region Public Methods

        public void BeginNewEdit()
        {
            int oldLevel = EditLevel;
            ThreadSafeDictionary<string, object?> clone = owner.CloneProperties();
            lock (snapshots)
                snapshots.Add(clone);
            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(oldLevel, oldLevel + 1, nameof(EditLevel)));
        }

        public void CommitLastEdit()
        {
            int currentLevel;
            lock (snapshots)
            {
                currentLevel = EditLevel;
                if (currentLevel == 0)
                    Throw.InvalidOperationException(Res.ComponentModelNotEditing);
                snapshots.RemoveAt(currentLevel - 1);
            }

            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(currentLevel, currentLevel - 1, nameof(EditLevel)));
        }

        public void RevertLastEdit()
        {
            int currentLevel;
            lock (snapshots)
            {
                currentLevel = EditLevel;
                if (currentLevel == 0)
                    Throw.InvalidOperationException(Res.ComponentModelNotEditing);
                var undoable = owner as ICanUndoInternal;
                undoable?.SuspendUndo();
                try
                {
                    owner.ReplaceProperties(snapshots[currentLevel - 1], true);
                }
                finally
                {
                    undoable?.ResumeUndo();
                }

                undoable?.ClearUndoHistory();
                snapshots.RemoveAt(currentLevel - 1);
            }

            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(currentLevel, currentLevel - 1, nameof(EditLevel)));
        }

        public bool TryCommitAllEdits()
        {
            int currentLevel;
            lock (snapshots)
            {
                currentLevel = EditLevel;
                if (currentLevel == 0)
                    return false;
                snapshots.Clear();
            }

            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(currentLevel, 0, nameof(EditLevel)));
            return true;
        }

        public bool TryRevertAllEdits()
        {
            int currentLevel;
            lock (snapshots)
            {
                currentLevel = EditLevel;
                if (currentLevel == 0)
                    return false;
                var undoable = owner as ICanUndoInternal;
                undoable?.SuspendUndo();
                try
                {
                    owner.ReplaceProperties(snapshots[0], true);
                }
                finally
                {
                    undoable?.ResumeUndo();
                }

                undoable?.ClearUndoHistory();
                snapshots.Clear();
            }

            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(currentLevel, 0, nameof(EditLevel)));
            return true;
        }

        #endregion

        #region Internal Methods

        internal void BeginEdit(EditableObjectBehavior behavior)
        {
            switch (behavior)
            {
                case EditableObjectBehavior.DisableNesting:
                    if (EditLevel == 0)
                        BeginNewEdit();
                    break;
                case EditableObjectBehavior.AllowNesting:
                    BeginNewEdit();
                    break;
                case EditableObjectBehavior.Disabled:
                    break;
                default:
                    Throw.EnumArgumentOutOfRangeWithValues(Argument.behavior, behavior);
                    break;
            }
        }

        internal void EndEdit(EditableObjectBehavior behavior)
        {
            switch (behavior)
            {
                case EditableObjectBehavior.DisableNesting:
                    TryCommitAllEdits();
                    break;
                case EditableObjectBehavior.AllowNesting:
                    CommitLastEdit();
                    break;
                case EditableObjectBehavior.Disabled:
                    break;
                default:
                    Throw.EnumArgumentOutOfRangeWithValues(Argument.behavior, behavior);
                    break;
            }
        }

        internal void CancelEdit(EditableObjectBehavior behavior)
        {
            switch (behavior)
            {
                case EditableObjectBehavior.DisableNesting:
                    TryRevertAllEdits();
                    break;
                case EditableObjectBehavior.AllowNesting:
                    RevertLastEdit();
                    break;
                case EditableObjectBehavior.Disabled:
                    break;
                default:
                    Throw.EnumArgumentOutOfRangeWithValues(Argument.behavior, behavior);
                    break;
            }
        }

        #endregion

        #endregion
    }
}
