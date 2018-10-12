using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace KGySoft.ComponentModel
{
    internal class EditableHelper : ICanEdit
    {
        #region Fields

        private readonly PersistableObjectBase owner;

#if NET35
        private readonly Stack<KeyValuePair<string, object>[]> snapshots = new Stack<KeyValuePair<string, object>[]>();
#else
        private readonly ConcurrentStack<KeyValuePair<string, object>[]> snapshots = new ConcurrentStack<KeyValuePair<string, object>[]>();
#endif

        #endregion

        internal EditableHelper(PersistableObjectBase owner) => this.owner = owner;

        #region Methods

        public void BeginEdit()
        {
#if NET35
            lock (snapshots)
                snapshots.Push(owner.PropertiesInternal.ToArray());
#else
            snapshots.Push(owner.PropertiesInternal.ToArray());
#endif

        }

        public void EndEdit()
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

        public void CancelEdit()
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
                var undoable = owner as ICanUndoInternal;
                undoable?.SuspendUndo();
                try
                {
                    owner.ReplaceProperties(previousState);
                }
                finally
                {
                    undoable?.ResumeUndo();
                }

                undoable?.ClearUndoHistory();
            }
        }

        #endregion

        public int EditLevel 
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
