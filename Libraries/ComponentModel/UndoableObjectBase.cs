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
    /// To access the undo/redo features call the <see cref="AsUndoable"/> property, which returns an <see cref="ICanUndoRedo"/> instance.
    /// </summary>
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="ICanUndoRedo" />
    /// <seealso cref="IRevertibleChangeTracking" />
    public abstract class UndoableObjectBase : PersistableObjectBase, ICanUndoRedo, IRevertibleChangeTracking
    {
        #region Constants

        private const int defaultUndoCapacity = 20;

        #endregion

        #region Fields

        private readonly CircularList<KeyValuePair<string, object>> undoSteps = new CircularList<KeyValuePair<string, object>>();
        private readonly CircularList<KeyValuePair<string, object>> redoSteps = new CircularList<KeyValuePair<string, object>>();

        private int undoCapacity = defaultUndoCapacity;
        private int suspendCounter;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the current <see cref="UndoableObjectBase"/> as an <see cref="ICanUndoRedo"/> instance.
        /// </summary>
        /// <returns></returns>
        public ICanUndoRedo AsUndoable => this;

        #endregion

        #region Explicitly Implemented Interface Properties

        bool IChangeTracking.IsChanged => AsUndoable.CanUndo;

        bool ICanUndo.CanUndo => undoSteps.Count > 0;
        bool ICanUndoRedo.CanRedo => redoSteps.Count > 0;

        int ICanUndo.UndoCapacity
        {
            get => undoCapacity;
            set
            {
                if (undoCapacity < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
                if (undoCapacity == value)
                    return;
                lock (undoSteps)
                {
                    OnPropertyChanging();
                    if (undoSteps.Count > value)
                        undoSteps.RemoveRange(0, undoSteps.Count - value);
                    if (redoSteps.Count > value)
                        redoSteps.RemoveRange(0, redoSteps.Count - value);
                    undoCapacity = value;
                }

                OnPropertyChanged();
            }
        }

        #endregion

        #endregion

        #region Methods

        #region Internal Methods

        internal void SuspendUndo() => Interlocked.Increment(ref suspendCounter); // actually private protected
        internal void ResumeUndo() => Interlocked.Decrement(ref suspendCounter); // actually private protected

        #endregion

        #region Protected Methods

        /// <inheritdoc />
        protected override void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanging(propertyName);

            // No locking needed here because we are in base write lock in SetProperty and we ignore other properties anyway
            if (undoCapacity == 0 || suspendCounter > 0 || propertyName == null || !PropertiesInternal.TryGetValue(propertyName, out object value))
                return;

            // These are already locked inside because steps can be accessed from other methods in this class, too
            Clear(redoSteps, nameof(ICanUndoRedo.CanRedo));
            AddStep(undoSteps, nameof(ICanUndo.CanUndo), new KeyValuePair<string, object>(propertyName, value));
        }

        #endregion

        #region Private Methods

        private void Clear(CircularList<KeyValuePair<string, object>> storage, string canUndoRedoName)
        {
            if (storage.Count == 0)
                return;

            lock (undoSteps)
            {
                OnPropertyChanging(canUndoRedoName);
                storage.Reset();
            }

            OnPropertyChanged(canUndoRedoName);
        }

        private void AddStep(CircularList<KeyValuePair<string, object>> storage, string canUndoRedoName, KeyValuePair<string, object> newStep)
        {
            bool raiseEvents;
            lock (undoSteps)
            {
                raiseEvents = storage.Count == 0;
                if (raiseEvents)
                    base.OnPropertyChanging(canUndoRedoName);
                if (storage.Count > 0 && storage.Count + 1 == undoCapacity)
                    storage.RemoveFirst();
                storage.AddLast(newStep);
            }

            if (raiseEvents)
                OnPropertyChanged(canUndoRedoName);
        }

        /// <summary>
        /// Applying 1 undo/redo step and raising Can... events if necessary.
        /// </summary>
        private bool ApplyStep(CircularList<KeyValuePair<string, object>> source, string sourceName, CircularList<KeyValuePair<string, object>> target, string targetName)
        {
            lock (undoSteps)
            {
                if (source.Count == 0)
                    return false;

                SuspendUndo();
                try
                {
                    bool raiseSource = source.Count == 1;
                    bool raiseTarget = source.Count == 0;

                    var step = source[source.Count - 1];
                    if (raiseSource)
                        OnPropertyChanging(sourceName);
                    source.RemoveLast();
                    if (raiseSource)
                        OnPropertyChanged(sourceName);

                    AsPersistable.SetProperty(step.Key, step.Value);

                    if (raiseTarget)
                        OnPropertyChanging(targetName);
                    target.AddLast(step);
                    if (raiseTarget)
                        OnPropertyChanged(targetName);
                }
                finally
                {
                    ResumeUndo();
                }

                return true;
            }
        }

        /// <summary>
        /// Applying all undo/redo step and raising Can... events if necessary. Optimized to call Set only once per property.
        /// </summary>
        private bool ApplyAll(CircularList<KeyValuePair<string, object>> source, string sourceName, CircularList<KeyValuePair<string, object>> target, string targetName)
        {
            lock (undoSteps)
            {
                if (source.Count == 0)
                    return false;

                SuspendUndo();
                try
                {
                    bool raiseTarget = source.Count == 0;

                    // Reversing so it can be added at once to the target. Now the last entry is the oldest change.
                    source.Reverse();
                    Dictionary<string, object> changes = new Dictionary<string, object>();

                    // collecting all changes in a dictionary where every property has the oldest state
                    foreach (var step in source)
                        changes[step.Key] = step.Value;

                    OnPropertyChanging(sourceName);
                    if (raiseTarget)
                        OnPropertyChanging(targetName);

                    foreach (var change in changes)
                        AsPersistable.SetProperty(change.Key, change.Value);

                    target.AddRange(source);
                    source.Reset();
                    OnPropertyChanged(sourceName);
                    if (raiseTarget)
                        OnPropertyChanged(targetName);
                }
                finally
                {
                    ResumeUndo();
                }

                return true;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void IChangeTracking.AcceptChanges() => AsUndoable.ClearHistory();
        void IRevertibleChangeTracking.RejectChanges() => AsUndoable.UndoAll();

        void ICanUndo.ClearHistory()
        {
            Clear(redoSteps, nameof(ICanUndoRedo.CanRedo));
            Clear(undoSteps, nameof(ICanUndo.CanUndo));
        }

        bool ICanUndo.TryUndo() => ApplyStep(undoSteps, nameof(ICanUndo.CanUndo), redoSteps, nameof(ICanUndoRedo.CanRedo));
        bool ICanUndoRedo.TryRedo() => ApplyStep(redoSteps, nameof(ICanUndoRedo.CanRedo), undoSteps, nameof(ICanUndo.CanUndo));

        void ICanUndo.UndoAll() => ApplyAll(undoSteps, nameof(ICanUndo.CanUndo), redoSteps, nameof(ICanUndoRedo.CanRedo));
        void ICanUndoRedo.RedoAll() => ApplyAll(redoSteps, nameof(ICanUndoRedo.CanRedo), undoSteps, nameof(ICanUndo.CanUndo));

        #endregion

        #endregion
    }
}
