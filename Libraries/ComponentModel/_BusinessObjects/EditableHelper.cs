using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace KGySoft.ComponentModel
{
    internal class EditableHelper : ICanEdit
    {
        #region Fields

        private readonly PersistableObjectBase owner;
        private int editLevel;

#if NET35
        private readonly Stack<KeyValuePair<string, object>[]> snapshots = new Stack<KeyValuePair<string, object>[]>();
#else
        private readonly ConcurrentStack<KeyValuePair<string, object>[]> snapshots = new ConcurrentStack<KeyValuePair<string, object>[]>();
#endif

        #endregion

        #region Properties

        public int EditLevel => editLevel;

        #endregion

        #region Constructors

        internal EditableHelper(PersistableObjectBase owner) => this.owner = owner;

        #endregion

        #region Methods

        public void BeginEdit()
        {
            Interlocked.Increment(ref editLevel);
#if NET35
            lock (snapshots)
                snapshots.Push(owner.PropertiesInternal.ToArray());
#else
            snapshots.Push(owner.PropertiesInternal.ToArray());
#endif

        }

        public void EndEdit()
        {
            // TODO: if (editLevel == 0) invalidopex
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
            Interlocked.Decrement(ref editLevel);
        }

        public void CancelEdit()
        {
            // TODO: if (editLevel == 0) invalidopex
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

            Interlocked.Decrement(ref editLevel);
        }

        #endregion
    }
}
