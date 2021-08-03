#if NET35 || NET40
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadLocal.cs
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
using System.Diagnostics.CodeAnalysis;

using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// This class is based on the .NET Framework ThreadLocal type. This custom version is used even in .NET 4.0 because
    /// the Framework version supports the <see cref="Values"/> property only from .NET 4.5
    /// Original source: https://source.dot.net/#System.Private.CoreLib/ThreadLocal.cs
    /// </summary>
    internal class ThreadLocal<T> : IDisposable
    {
#region Nested types

#region LinkedSlot class

        /// <summary>
        /// A node in the doubly-linked list stored in the ThreadLocal instance.
        /// </summary>
        private sealed class LinkedSlot
        {
#region Fields

            /// <summary>
            /// The next LinkedSlot for this ThreadLocal instance
            /// </summary>
            internal volatile LinkedSlot? Next;

            /// <summary>
            /// The previous LinkedSlot for this ThreadLocal instance
            /// </summary>
            internal volatile LinkedSlot? Previous;

            /// <summary>
            /// The SlotArray that stores this LinkedSlot
            /// </summary>
            internal volatile LinkedSlotVolatile[]? SlotArray;

            /// <summary>
            /// The value for this slot.
            /// </summary>
            [AllowNull]internal T Value = default!;

#endregion

#region Constructors

            internal LinkedSlot(LinkedSlotVolatile[]? slotArray)
            {
                SlotArray = slotArray;
            }

#endregion
        }

#endregion

#region IdManager class

        /// <summary>
        /// A manager class that assigns IDs to ThreadLocal instances
        /// </summary>
        private class IdManager
        {
#region Fields

            private readonly List<bool> freeIds = new List<bool>();

            private int nextIdToTry;

#endregion

#region Methods

            internal int GetId()
            {
                lock (freeIds)
                {
                    int availableId = nextIdToTry;
                    while (availableId < freeIds.Count)
                    {
                        if (freeIds[availableId])
                            break;
                        availableId++;
                    }

                    if (availableId == freeIds.Count)
                        freeIds.Add(false);
                    else
                        freeIds[availableId] = false;

                    nextIdToTry = availableId + 1;
                    return availableId;
                }
            }

            /// <summary>
            ///  Return an ID to the pool
            /// </summary>
            internal void ReturnId(int id)
            {
                lock (freeIds)
                {
                    freeIds[id] = true;
                    if (id < nextIdToTry) nextIdToTry = id;
                }
            }

#endregion
        }

#endregion

#region FinalizationHelper class

        /// <summary>
        /// A class that facilitates ThreadLocal cleanup after a thread exits.
        ///
        /// After a thread with an associated thread-local table has exited, the FinalizationHelper
        /// is responsible for removing back-references to the table. Since an instance of FinalizationHelper
        /// is only referenced from a single thread-local slot, the FinalizationHelper will be GC'd once
        /// the thread has exited.
        ///
        /// The FinalizationHelper then locates all LinkedSlot instances with back-references to the table
        /// (all those LinkedSlot instances can be found by following references from the table slots) and
        /// releases the table so that it can get GC'd.
        /// </summary>
        private class FinalizationHelper
        {
#region Fields

            internal LinkedSlotVolatile[] SlotArray;
            private readonly bool trackAllValues;

#endregion

#region Construction and Destruction

#region Constructors

            internal FinalizationHelper(LinkedSlotVolatile[] slotArray, bool trackAllValues)
            {
                SlotArray = slotArray;
                this.trackAllValues = trackAllValues;
            }

#endregion

#region Destructor

            ~FinalizationHelper()
            {
                LinkedSlotVolatile[] slotArray = SlotArray;
                for (int i = 0; i < slotArray.Length; i++)
                {
                    LinkedSlot? linkedSlot = slotArray[i].Value;

                    // This slot in the table is empty
                    if (linkedSlot == null)
                        continue;

                    if (trackAllValues)
                    {
                        // Set the SlotArray field to null to release the slot array.
                        linkedSlot.SlotArray = null;
                        continue;
                    }

                    // Remove the LinkedSlot from the linked list. Once the FinalizationHelper is done, all back-references to
                    // the table will be have been removed, and so the table can get GC'd.
                    lock (idManager)
                    {
                        if (linkedSlot.Next != null)
                            linkedSlot.Next.Previous = linkedSlot.Previous;

                        // Since the list uses a dummy head node, the Previous reference should never be null.
                        linkedSlot.Previous!.Next = linkedSlot.Next;
                    }
                }
            }

#endregion

#endregion
        }

#endregion

#region LinkedSlotVolatile struct

        /// <summary>
        /// A wrapper struct used as LinkedSlotVolatile[] - an array of LinkedSlot instances, but with volatile semantics
        /// on array accesses.
        /// </summary>
        private struct LinkedSlotVolatile
        {
#region Fields

            internal volatile LinkedSlot? Value;

#endregion
        }

#endregion

#endregion

#region Fields

#region Static Fields

        /// <summary>
        /// IdManager assigns and reuses slot IDs. Additionally, the object is also used as a global lock.
        /// </summary>
        private static readonly IdManager idManager = new IdManager();

        /// <summary>
        /// A table of thread-local values for all ThreadLocal instances
        ///
        /// So, when a thread reads slotArray, it gets back an array of *all* ThreadLocal values for this thread and this T.
        /// The slot relevant to this particular ThreadLocal instance is determined by the idComplement instance field stored in
        /// the ThreadLocal instance.
        /// </summary>
        [ThreadStatic]private static LinkedSlotVolatile[]? slotArray;

        [ThreadStatic]private static FinalizationHelper? finalizationHelper;

#endregion

#region Instance Fields

        /// <summary>
        /// Specifies whether <see cref="Values"/> property is supported.
        /// </summary>
        private readonly bool trackAllValues;

        /// <summary>
        /// A delegate that returns the created value, if null the created value will be default(T)
        /// </summary>
        private Func<T>? valueFactory;

        /// <summary>
        /// Slot ID of this ThreadLocal instance. We store a bitwise complement of the ID (that is ~ID), which allows us to distinguish
        /// between the case when ID is 0 and an incompletely initialized object, either due to a thread abort in the constructor, or
        /// possibly due to a memory model issue in user code.
        /// </summary>
        private int idComplement;

        /// <summary>
        /// This field is set to true when the constructor completes. That is helpful for recognizing whether a constructor
        /// threw an exception - either due to invalid argument or due to a thread abort. Finally, the field is set to false
        /// when the instance is disposed.
        /// </summary>
        private volatile bool initialized;

        /// <summary>
        /// A linked list of all values associated with this ThreadLocal instance.
        /// We create a dummy head node. That allows us to remove any (non-dummy)  node without having to locate the linkedSlot field.
        /// </summary>
        private LinkedSlot? linkedSlot = new LinkedSlot(null);

#endregion

#endregion

#region Properties

        internal T? Value
        {
            get
            {
                LinkedSlotVolatile[]? slots = slotArray;
                LinkedSlot? slot;
                int id = ~idComplement;

                // Attempt to get the value using the fast path
                if (slots != null   // Has the slot array been initialized?
                    && id >= 0   // Is the ID non-negative (i.e., instance is not disposed)?
                    && id < slots.Length   // Is the table large enough?
                    && (slot = slots[id].Value) != null   // Has a LinkedSlot object has been allocated for this ID?
                    && initialized // Has the instance *still* not been disposed (important for a race condition with Dispose)?
                    )
                {
                    // We verified that the instance has not been disposed *after* we got a reference to the slot.
                    // This guarantees that we have a reference to the right slot.
                    //
                    // Volatile read of the LinkedSlotVolatile.Value property ensures that the initialized read
                    // will not be reordered before the read of slotArray[id].
                    return slot.Value;
                }

                return GetValueSlow()!;
            }
            private set
            {
                LinkedSlotVolatile[]? slots = slotArray;
                LinkedSlot? slot;
                int id = ~idComplement;

                // Attempt to set the value using the fast path
                if (slots != null   // Has the slot array been initialized?
                    && id >= 0   // Is the ID non-negative (i.e., instance is not disposed)?
                    && id < slots.Length   // Is the table large enough?
                    && (slot = slots[id].Value) != null   // Has a LinkedSlot object has been allocated for this ID?
                    && initialized // Has the instance *still* not been disposed (important for a race condition with Dispose)?
                    )
                {
                    // We verified that the instance has not been disposed *after* we got a reference to the slot.
                    // This guarantees that we have a reference to the right slot.
                    //
                    // Volatile read of the LinkedSlotVolatile.Value property ensures that the initialized read
                    // will not be reordered before the read of slotArray[id].
                    slot.Value = value;
                }
                else
                {
                    SetValueSlow(value, slots);
                }
            }
        }

        /// <summary>
        /// Gets a list for all of the values currently stored by all of the threads that have accessed this instance.
        /// </summary>
        internal List<T> Values
        {
            get
            {
                if (!trackAllValues)
                    Throw.InvalidOperationException(Res.InternalError("Values should not be accesses when initialized without tracking values"));
                LinkedSlot? slot = linkedSlot;
                int id = ~idComplement;
                if (id == -1 || slot == null)
                    throw new ObjectDisposedException(Res.ObjectDisposed);

                // Walk over the linked list of slots and gather the values associated with this ThreadLocal instance.
                var valueList = new List<T>();
                for (slot = slot.Next; slot != null; slot = slot.Next)
                {
                    // We can safely read slot.Value. Even if this ThreadLocal has been disposed in the meantime, the LinkedSlot
                    // objects will never be assigned to another ThreadLocal instance.
                    valueList.Add(slot.Value);
                }

                return valueList;
            }
        }

#endregion

#region Constructors

        internal ThreadLocal(Func<T> valueFactory, bool trackAllValues = false)
        {
            this.valueFactory = valueFactory;
            this.trackAllValues = trackAllValues;

            // Assign the ID and mark the instance as initialized. To avoid leaking IDs, we assign the ID and set _initialized
            // in a finally block, to avoid a thread abort in between the two statements.
            try { }
            finally
            {
                idComplement = ~idManager.GetId();

                // As the last step, mark the instance as fully initialized. (Otherwise, if _initialized=false, we know that an exception
                // occurred in the constructor.)
                initialized = true;
            }
        }

#endregion

#region Methods

#region Static Methods

        /// <summary>
        /// Resizes a table to a certain length (or larger).
        /// </summary>
        private static void GrowTable(ref LinkedSlotVolatile[] table, int minLength)
        {
            Debug.Assert(table.Length < minLength);

            // Determine the size of the new table and allocate it.
            int newLen = HashHelper.GetNextPowerOfTwo(minLength);
            LinkedSlotVolatile[] newTable = new LinkedSlotVolatile[newLen];

            // The lock is necessary to avoid a race with ThreadLocal.Dispose. GrowTable has to point all
            // LinkedSlot instances referenced in the old table to reference the new table. Without locking,
            // Dispose could use a stale SlotArray reference and clear out a slot in the old array only, while
            // the value continues to be referenced from the new (larger) array.
            lock (idManager)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    LinkedSlot? linkedSlot = table[i].Value;
                    if (linkedSlot?.SlotArray != null)
                    {
                        linkedSlot.SlotArray = newTable;
                        newTable[i] = table[i];
                    }
                }
            }

            table = newTable;
        }

#endregion

#region Instance Methods

#region Public Methods

        public void Dispose()
        {
            int id;
            lock (idManager)
            {
                id = ~idComplement;
                idComplement = 0;

                // Handle double Dispose calls or disposal of an instance whose constructor threw an exception.
                if (id < 0 || !initialized)
                    return;

                initialized = false;

                Debug.Assert(linkedSlot != null, "Should be non-null if not yet disposed");
                for (LinkedSlot? slot = linkedSlot!.Next; slot != null; slot = slot.Next)
                {
                    LinkedSlotVolatile[]? slots = slot.SlotArray;

                    // The thread that owns this slot has already finished.
                    if (slots == null)
                        continue;

                    // Remove the reference from the LinkedSlot to the slot table.
                    slot.SlotArray = null;

                    // And clear the references from the slot table to the linked slot and the value so that
                    // both can get garbage collected.
                    slots[id].Value!.Value = default;
                    slots[id].Value = null;
                }
            }

            idManager.ReturnId(id);
            linkedSlot = null;
            valueFactory = null;
        }

        public override string? ToString() => Value?.ToString();

#endregion

#region Private Methods

        private T? GetValueSlow()
        {
            // If the object has been disposed, the id will be -1.
            int id = ~idComplement;
            if (id < 0)
                throw new ObjectDisposedException(Res.ObjectDisposed);

            // Determine the initial value
            T? value = valueFactory == null ? default : valueFactory.Invoke();

            // Since the value has been previously uninitialized, we also need to set it (according to the ThreadLocal semantics).
            Value = value;
            return value;
        }

        private void SetValueSlow(T? value, LinkedSlotVolatile[]? slots)
        {
            int id = ~idComplement;

            // If the object has been disposed, id will be -1.
            if (id < 0)
                throw new ObjectDisposedException(Res.ObjectDisposed);

            // If a slot array has not been created on this thread yet, create it.
            if (slots == null)
            {
                slots = new LinkedSlotVolatile[HashHelper.GetNextPowerOfTwo(id + 1)];
                finalizationHelper = new FinalizationHelper(slots, trackAllValues);
                slotArray = slots;
            }

            // If the slot array is not big enough to hold this ID, increase the table size.
            if (id >= slots.Length)
            {
                GrowTable(ref slots, id + 1);
                Debug.Assert(finalizationHelper != null, "Should have been initialized when this thread's slot array was created.");
                finalizationHelper!.SlotArray = slots;
                slotArray = slots;
            }

            // If we are using the slot in this table for the first time, create a new LinkedSlot and add it into
            // the linked list for this ThreadLocal instance.
            if (slots[id].Value == null)
            {
                CreateLinkedSlot(slots, id, value);
            }
            else
            {
                // Volatile read of the LinkedSlotVolatile.Value property ensures that the m_initialized read
                // that follows will not be reordered before the read of slotArray[id].
                LinkedSlot? slot = slots[id].Value;

                // It is important to verify that the ThreadLocal instance has not been disposed. The check must come
                // after capturing slotArray[id], but before assigning the value into the slot. This ensures that
                // if this ThreadLocal instance was disposed on another thread and another ThreadLocal instance was
                // created, we definitely won't assign the value into the wrong instance.
                if (!initialized)
                    throw new ObjectDisposedException(Res.ObjectDisposed);

                slot!.Value = value;
            }
        }

        /// <summary>
        /// Creates a LinkedSlot and inserts it into the linked list for this ThreadLocal instance.
        /// </summary>
        private void CreateLinkedSlot(LinkedSlotVolatile[] slots, int id, T? value)
        {
            var slot = new LinkedSlot(slots);

            // Insert the LinkedSlot into the linked list maintained by this ThreadLocal<> instance and into the slot array
            lock (idManager)
            {
                // Check that the instance has not been disposed. It is important to check this under a lock, since
                // Dispose also executes under a lock.
                if (!initialized)
                    throw new ObjectDisposedException(Res.ObjectDisposed);

                LinkedSlot? firstRealNode = slot.Next;

                // Insert linkedSlot between nodes m_linkedSlot and firstRealNode.
                // (_linkedSlot is the dummy head node that should always be in the front.)
                slot.Next = firstRealNode;
                slot.Previous = slot;
                slot.Value = value;

                if (firstRealNode != null)
                    firstRealNode.Previous = slot;

                slot.Next = slot;

                // Assigning the slot under a lock prevents a race condition with Dispose (dispose also acquires the lock).
                // Otherwise, it would be possible that the ThreadLocal instance is disposed, another one gets created
                // with the same ID, and the write would go to the wrong instance.
                slots[id].Value = slot;
            }
        }

#endregion

#endregion

#endregion
    }
}
#endif