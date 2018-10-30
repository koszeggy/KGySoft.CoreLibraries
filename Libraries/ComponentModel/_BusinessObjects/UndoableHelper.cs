using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using KGySoft.Collections;

namespace KGySoft.ComponentModel
{
    internal class UndoableHelper : ICanUndoRedo, ICanUndoInternal
    {
        private struct UndoEntry
        {
            internal object From;
            internal object To;
        }

        private const int defaultUndoCapacity = 20;

        private readonly CircularList<KeyValuePair<string, UndoEntry>> undoSteps = new CircularList<KeyValuePair<string, UndoEntry>>();
        private readonly CircularList<KeyValuePair<string, UndoEntry>> redoSteps = new CircularList<KeyValuePair<string, UndoEntry>>();

        private int undoCapacity = defaultUndoCapacity;
        private int suspendCounter;

        private readonly ObservableObjectBase owner;

        internal UndoableHelper(ObservableObjectBase owner) => this.owner = owner;

        public bool CanUndo => undoSteps.Count > 0;
        public bool CanRedo => redoSteps.Count > 0;

        internal int UndoCapacity
        {
            get => undoCapacity;
            set
            {
                int oldCapacity = undoCapacity;
                if (oldCapacity < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), Res.ArgumentOutOfRange);
                if (oldCapacity == value)
                    return;

                bool raiseUndoChange = value == 0 && undoSteps.Count > 0;
                bool raiseRedoChange = value == 0 && redoSteps.Count > 0;

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

        public void SuspendUndo() => Interlocked.Increment(ref suspendCounter);
        public void ResumeUndo() => Interlocked.Decrement(ref suspendCounter);

        internal void HandlePropertyChanged(PropertyChangedExtendedEventArgs e)
        {
            // CanUndo/CanRedo/etc. properties are excluded by the caller, who checks if the property exists in the internal storage.
            // (Not here because this class is created only if really needed).
            if (undoCapacity == 0 || suspendCounter > 0)
                return;

            // These calls are already locked inside because steps can be accessed from other methods in this class, too
            ClearSteps(redoSteps, nameof(CanRedo));
            AddUndoStep(new KeyValuePair<string, UndoEntry>(e.PropertyName, new UndoEntry { From = e.NewValue, To = e.OldValue }));
        }

        private void ClearSteps(CircularList<KeyValuePair<string, UndoEntry>> storage, string canUndoRedoName)
        {
            if (storage.Count == 0)
                return;

            lock (undoSteps)
                storage.Reset();
            owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(true, false, canUndoRedoName));
        }

        private void AddUndoStep(KeyValuePair<string, UndoEntry> newStep)
        {
            CircularList<KeyValuePair<string, UndoEntry>> storage = undoSteps;
            bool raiseChangedEvent = storage.Count == 0;
            lock (undoSteps)
            {
                if (storage.Count > 0 && storage.Count + 1 == undoCapacity)
                    storage.RemoveFirst();
                storage.AddLast(newStep);
            }

            if (raiseChangedEvent)
                owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(false, true, nameof(CanUndo)));
        }

        /// <summary>
        /// Applying 1 undo/redo step and raising Can... events if necessary.
        /// </summary>
        private bool ApplyStep(CircularList<KeyValuePair<string, UndoEntry>> source, string sourceName, CircularList<KeyValuePair<string, UndoEntry>> target, string targetName)
        {
            if (source.Count == 0)
                return false;

            bool raiseSource = source.Count == 1;
            bool success, raiseTarget;
            lock (undoSteps)
            {
                var step = source[source.Count - 1];
                source.RemoveLast();

                SuspendUndo();
                try
                {
                    // This will be false if actual value was not equal to From, which means an inconsistency between actual and tracked values.
                    success = owner.TryReplaceProperty(step.Key, step.Value.From, step.Value.To, true);
                }
                finally
                {
                    ResumeUndo();
                }

                if (raiseSource)
                    owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(true, false, sourceName));

                raiseTarget = target.Count == 0 && success;
                if (success)
                    target.AddLast(new KeyValuePair<string, UndoEntry>(step.Key, new UndoEntry { From = step.Value.To, To = step.Value.From }));
            }

            if (raiseTarget)
                owner.OnPropertyChanged(new PropertyChangedExtendedEventArgs(false, true, targetName));

            return success;
        }

        /// <summary>
        /// Applying all undo/redo steps.
        /// </summary>
        private void ApplyAll(CircularList<KeyValuePair<string, UndoEntry>> source, string sourceName, CircularList<KeyValuePair<string, UndoEntry>> target, string targetName)
        {
            if (source.Count == 0)
                return;

            SuspendUndo();
            try
            {
                lock (undoSteps)
                {
                    while (source.Count > 0)
                        ApplyStep(source, sourceName, target, targetName);
                }
            }
            finally
            {
                ResumeUndo();
            }
        }

        public void ClearUndoHistory()
        {
            ClearSteps(redoSteps, nameof(ICanUndoRedo.CanRedo));
            ClearSteps(undoSteps, nameof(ICanUndo.CanUndo));
        }

        public bool TryUndo() => ApplyStep(undoSteps, nameof(ICanUndo.CanUndo), redoSteps, nameof(ICanUndoRedo.CanRedo));
        public bool TryRedo() => ApplyStep(redoSteps, nameof(ICanUndoRedo.CanRedo), undoSteps, nameof(ICanUndo.CanUndo));
        public void UndoAll() => ApplyAll(undoSteps, nameof(ICanUndo.CanUndo), redoSteps, nameof(ICanUndoRedo.CanRedo));
        public void RedoAll() => ApplyAll(redoSteps, nameof(ICanUndoRedo.CanRedo), undoSteps, nameof(ICanUndo.CanUndo));
    }
}
