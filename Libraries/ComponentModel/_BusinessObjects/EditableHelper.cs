using System;
using System.Collections.Generic;
using System.Linq;
using KGySoft.Collections;
using KGySoft.CoreLibraries;

namespace KGySoft.ComponentModel
{
    internal class EditableHelper : ICanEdit
    {
        #region Fields

        private readonly ObservableObjectBase owner;
        private readonly LockingList<IDictionary<string, object>> snapshots = new List<IDictionary<string, object>>().AsThreadSafe();

        #endregion

        #region Properties

        public int EditLevel => snapshots.Count;

        #endregion

        #region Constructors

        internal EditableHelper(ObservableObjectBase owner) => this.owner = owner;

        #endregion

        #region Methods

        public void BeginNewEdit()
        {
            int oldLevel = EditLevel;
            snapshots.Add(owner.CloneProperties());
            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(oldLevel, oldLevel + 1, nameof(EditLevel)));
        }

        public void CommitLastEdit()
        {
            int currentLevel;
            snapshots.Lock();
            try
            {
                currentLevel = EditLevel;
                if (currentLevel == 0)
                    throw new InvalidOperationException(Res.Get(Res.NotEditing));
                snapshots.RemoveAt(currentLevel - 1);
            }
            finally
            {
                snapshots.Unlock();
            }

            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(currentLevel, currentLevel - 1, nameof(EditLevel)));
        }

        public void RevertLastEdit()
        {
            snapshots.Lock();
            int currentLevel;
            try
            {
                currentLevel = EditLevel;
                if (currentLevel == 0)
                    throw new InvalidOperationException(Res.Get(Res.NotEditing));
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
            finally
            {
                snapshots.Unlock();
            }

            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(currentLevel, currentLevel - 1, nameof(EditLevel)));
        }

        public bool TryCommitAllEdits()
        {
            int currentLevel;
            snapshots.Lock();
            try
            {
                currentLevel = EditLevel;
                if (currentLevel == 0)
                    return false;
                snapshots.Clear();
            }
            finally
            {
                snapshots.Unlock();
            }
            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(currentLevel, 0, nameof(EditLevel)));

            return true;
        }

        public bool TryRevertAllEdits()
        {
            snapshots.Lock();
            int currentLevel;
            try
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
            finally
            {
                snapshots.Unlock();
            }

            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(currentLevel, 0, nameof(EditLevel)));
            return true;
        }

        #endregion

        internal void BeginEdit(EditableObjectBehavior behavior)
        {
            switch (behavior)
            {
                case EditableObjectBehavior.NestingDisabled:
                    if (EditLevel == 0)
                        BeginNewEdit();
                    break;
                case EditableObjectBehavior.NestingAllowed:
                    BeginNewEdit();
                    break;
                case EditableObjectBehavior.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(behavior), Res.ArgumentOutOfRange);
            }
        }

        internal void EndEdit(EditableObjectBehavior behavior)
        {
            switch (behavior)
            {
                case EditableObjectBehavior.NestingDisabled:
                    TryCommitAllEdits();
                    break;
                case EditableObjectBehavior.NestingAllowed:
                    CommitLastEdit();
                    break;
                case EditableObjectBehavior.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(behavior), Res.ArgumentOutOfRange);
            }
        }

        internal void CancelEdit(EditableObjectBehavior behavior)
        {
            switch (behavior)
            {
                case EditableObjectBehavior.NestingDisabled:
                    TryRevertAllEdits();
                    break;
                case EditableObjectBehavior.NestingAllowed:
                    RevertLastEdit();
                    break;
                case EditableObjectBehavior.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(behavior), Res.ArgumentOutOfRange);
            }
        }
    }
}
