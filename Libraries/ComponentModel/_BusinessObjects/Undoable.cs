using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using KGySoft.Collections;

namespace KGySoft.ComponentModel
{
    internal class UndoableHelper : ICanUndoRedo
    {
        private const int defaultUndoCapacity = 20;

#error inkább 2 elem kéne a lépésekbe. Vagy hogy redozunk valamit, ha csak a régi értéket tudjuk?
        private readonly CircularList<KeyValuePair<string, object>> undoSteps = new CircularList<KeyValuePair<string, object>>();
        private readonly CircularList<KeyValuePair<string, object>> redoSteps = new CircularList<KeyValuePair<string, object>>();

        private int undoCapacity = defaultUndoCapacity;
        private int suspendCounter;

        private PersistableObjectBase owner;

        internal UndoableHelper(PersistableObjectBase owner)
        {
            this.owner = owner;
            owner.PropertyChanged += Owner_PropertyChanged;
        }

        public bool CanUndo => undoSteps.Count > 0;
        public bool CanRedo => redoSteps.Count > 0;

        public int UndoCapacity
        {
            get => undoCapacity;
            set
            {
                int oldCapacity = undoCapacity;
                if (oldCapacity < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
                if (oldCapacity == value)
                    return;

                owner.OnPropertyChanging(new PropertyChangingExtendedEventArgs(oldCapacity, nameof(UndoCapacity)));
                bool raiseUndoChange = value == 0 && undoSteps.Count > 0;
                if (raiseUndoChange)
                    owner.OnPropertyChanging(new PropertyChangingExtendedEventArgs(true, nameof(CanUndo)));
                bool raiseRedoChange = value == 0 && redoSteps.Count > 0;
                if (raiseRedoChange)
                    owner.OnPropertyChanging(new PropertyChangingExtendedEventArgs(true, nameof(CanRedo)));

                lock (undoSteps)
                {
                    if (undoSteps.Count > value)
                        undoSteps.RemoveRange(0, undoSteps.Count - value);
                    if (redoSteps.Count > value)
                        redoSteps.RemoveRange(0, redoSteps.Count - value);
                    undoCapacity = value;
                }

                owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(oldCapacity, value, nameof(UndoCapacity)));
                if (raiseUndoChange)
                    owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(true, false, nameof(CanUndo)));
                if (raiseRedoChange)
                    owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(true, false, nameof(CanRedo)));
            }
        }

        internal void SuspendUndo() => Interlocked.Increment(ref suspendCounter);
        internal void ResumeUndo() => Interlocked.Decrement(ref suspendCounter);

        private void Owner_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Ignoring if property does not exist in internal storage and cannot be set via the IPersistableObject interface. This excludes self CanUndo/CanRedo/etc. properties, too.
            // 
            if (undoCapacity == 0 || suspendCounter > 0 || !(e is PropertyChangedExtendedEventArgs extArgs) || !owner.PropertiesInternal.ContainsKey(e.PropertyName))
                return;

            // These calls are already locked inside because steps can be accessed from other methods in this class, too
            ClearSteps(redoSteps, nameof(CanRedo));
            AddUndoStep(new KeyValuePair<string, object>(e.PropertyName, extArgs.OldValue));
        }

        private void ClearSteps(CircularList<KeyValuePair<string, object>> storage, string canUndoRedoName)
        {
            if (storage.Count == 0)
                return;

            owner.OnPropertyChanging(new PropertyChangingExtendedEventArgs(true, canUndoRedoName));
            lock (undoSteps)
                storage.Reset();
            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(true, false, canUndoRedoName));
        }

        private void AddUndoStep(KeyValuePair<string, object> newStep)
        {
            CircularList<KeyValuePair<string, object>> storage = undoSteps;
            bool raiseEvents = storage.Count == 0;
            if (raiseEvents)
                owner.OnPropertyChanging(new PropertyChangingExtendedEventArgs(false, nameof(CanUndo)));
            lock (undoSteps)
            {
                if (storage.Count > 0 && storage.Count + 1 == undoCapacity)
                    storage.RemoveFirst();
                storage.AddLast(newStep);
            }

            if (raiseEvents)
                owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(false, true, nameof(CanUndo)));
        }

        /// <summary>
        /// Applying 1 undo/redo step and raising Can... events if necessary.
        /// </summary>
        private bool ApplyStep(CircularList<KeyValuePair<string, object>> source, string sourceName, CircularList<KeyValuePair<string, object>> target, string targetName)
        {
            if (source.Count == 0)
                return false;

            bool raiseSource = source.Count == 1;
            bool raiseTarget = source.Count == 0;

            var step = source[source.Count - 1];
            if (raiseSource)
                owner.OnPropertyChanging(new PropertyChangingExtendedEventArgs(true, sourceName));
            if (raiseTarget)
                owner.OnPropertyChanging(new PropertyChangingExtendedEventArgs(false, targetName));
            lock (undoSteps)
            {
                source.RemoveLast();

                SuspendUndo();
                try
                {
                    owner.AsPersistable.SetProperty(step.Key, step.Value);
                }
                finally
                {
                    ResumeUndo();
                }

                target.AddLast(step);
            }

            if (raiseSource)
                owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(true, false, sourceName));
            if (raiseTarget)
                owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(false, true, targetName));

            return true;
        }

        /// <summary>
        /// Applying all undo/redo step and raising Can... events if necessary. Optimized to call Set only once per property.
        /// </summary>
        private void ApplyAll(CircularList<KeyValuePair<string, object>> source, string sourceName, CircularList<KeyValuePair<string, object>> target, string targetName)
        {
                if (source.Count == 0)
                    return;

                    bool raiseTarget = source.Count == 0;
                    OnPropertyChanging(sourceName);
                    if (raiseTarget)
                        OnPropertyChanging(targetName);

            lock (undoSteps)
            {
                    // Reversing so it can be added at once to the target. Now the last entry is the oldest change.
                    source.Reverse();
                    Dictionary<string, object> changes = new Dictionary<string, object>();

                    // collecting all changes in a dictionary where every property has the oldest state
                    foreach (var step in source)
                        changes[step.Key] = step.Value;


                SuspendUndo();
                try
                {
                    foreach (var change in changes)
                        AsPersistable.SetProperty(change.Key, change.Value);
                }
                finally
                {
                    ResumeUndo();
                }

                    target.AddRange(source);
                    source.Reset();

            }
                    OnPropertyChanged(sourceName);
                    if (raiseTarget)
                        OnPropertyChanged(targetName);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void IChangeTracking.AcceptChanges() => AsUndoable.ClearHistory();
        void IRevertibleChangeTracking.RejectChanges() => AsUndoable.UndoAll();

        void ICanUndo.ClearHistory()
        {
            ClearSteps(redoSteps, nameof(ICanUndoRedo.CanRedo));
            ClearSteps(undoSteps, nameof(ICanUndo.CanUndo));
        }

        bool ICanUndo.TryUndo() => ApplyStep(undoSteps, nameof(ICanUndo.CanUndo), redoSteps, nameof(ICanUndoRedo.CanRedo));
        bool ICanUndoRedo.TryRedo() => ApplyStep(redoSteps, nameof(ICanUndoRedo.CanRedo), undoSteps, nameof(ICanUndo.CanUndo));

        void ICanUndo.UndoAll() => ApplyAll(undoSteps, nameof(ICanUndo.CanUndo), redoSteps, nameof(ICanUndoRedo.CanRedo));
        void ICanUndoRedo.RedoAll() => ApplyAll(redoSteps, nameof(ICanUndoRedo.CanRedo), undoSteps, nameof(ICanUndo.CanUndo));

        #endregion

        #endregion
    }
}
