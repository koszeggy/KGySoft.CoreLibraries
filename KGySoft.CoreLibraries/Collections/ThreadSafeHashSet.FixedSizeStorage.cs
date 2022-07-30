#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeHashSet.FixedSizeStorage.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    partial class ThreadSafeHashSet<T>
    {
        /// <summary>
        /// Represents a lock-free fixed size thread safe collection that supports updating values in a thread-safe manner,
        /// though adding new items is not supported. Removing and re-setting items are supported though, which is also thread-safe.
        /// </summary>
        internal sealed class FixedSizeStorage
        {
            #region Nested structs

            #region Entry struct

            [DebuggerDisplay("{" + nameof(DebugValue) + "}")]
            private struct Entry
            {
                #region Fields

                internal uint Hash;

                /// <summary>
                /// Zero-based index of a chained item in the current bucket or -1 if last.
                /// In this collection this field is practically read-only. Deleted items are indicated by the <see cref="IsDeleted"/> field.
                /// </summary>
                internal int Next;

                internal T Value;

                /// <summary>
                /// Cannot be a bool because there is no Interlocked.Exchange for bool but the size is the same anyway due to the alignment.
                /// Not a volatile field because from constructor no volatile write is needed so using Volatile.Read/Write when needed.
                /// </summary>
                internal int IsDeleted;

                #endregion

                #region Properties

                private object? DebugValue => IsDeleted != 0 ? "<Deleted>" : Value;

                #endregion
            }

            #endregion

            #region InternalEnumerator struct

            internal struct InternalEnumerator
            {
                #region Fields

                #region Internal Fields

                internal T Current;

                #endregion

                #region Private Fields

                private readonly Entry[] entries;

                private int pos;

                #endregion

                #endregion

                #region Constructors

                internal InternalEnumerator(FixedSizeStorage owner)
                {
                    entries = owner.entries;
                    pos = 0;
                    Current = default!;
                }

                #endregion

                #region Methods

                internal bool MoveNext()
                {
                    while (pos < entries.Length)
                    {
                        ref var entryRef = ref entries[pos];
                        pos += 1;

                        // skipping deleted items
                        if (Volatile.Read(ref entryRef.IsDeleted) != 0)
                            continue;

                        Current = entryRef.Value;
                        return true;
                    }

                    return false;
                }

                #endregion
            }

            #endregion

            #endregion

            #region Fields

            #region Static Fields

            internal static readonly FixedSizeStorage Empty = new FixedSizeStorage();

            #endregion

            #region Instance Fields

            private readonly IEqualityComparer<T>? comparer;
            private readonly bool isAndHash;

            private Entry[] entries = default!;
            private int[] buckets = default!; // 1-based indices for entries. 0 if unused.
            private uint hashingOperand; // buckets.Length - 1 for AND hashing, buckets.Length for MOD hashing
            private int deletedCount;

            #endregion

            #endregion

            #region Properties

            #region Public Properties

            public int Count => entries.Length - Volatile.Read(ref deletedCount);

            #endregion

            #region Internal Properties

            internal bool IsCleanupLimitReached => deletedCount > 16 && deletedCount > (entries.Length >> 1);
            internal int DeletedCount => deletedCount;
            internal int Capacity => entries.Length;

            #endregion

            #endregion

            #region Constructors

            #region Internal Constructors

            internal FixedSizeStorage(FixedSizeStorage other, TempStorage mergeWith, bool removeDeletedKeys)
            {
                // isAndHash and comparer are not taken from other because it can be Empty
                isAndHash = mergeWith.IsAndHash;
                comparer = mergeWith.Comparer;

                if (removeDeletedKeys)
                {
                    // Possible endless retries are prevented by WaitWhileMerging (this method is called with isMerging=true)
                    // Data loss is prevented by the retry mechanisms in ThreadSafeHashSet.AddInternal/Remove.
                    while (!TryInitialize(other, mergeWith))
                    {
                    }

                    return;
                }

                Initialize(other, mergeWith);
            }

            internal FixedSizeStorage(FixedSizeStorage other)
            {
                Debug.Assert(other.deletedCount > 0);

                // As other is never Empty here isAndHash and comparer can be taken from it
                isAndHash = other.isAndHash;
                comparer = other.comparer;

                // Possible endless retries are prevented by WaitWhileMerging (this method is called with isMerging=true)
                while (true)
                {
                    int count = other.Count;
                    buckets = new int[GetBucketSize(count)];
                    entries = new Entry[count];
                    if (TryCopyFrom(other, count))
                        return;
                }
            }

            #endregion

            #region Private Constructors

            private FixedSizeStorage()
            {
                // this ctor is for the Empty instance
                buckets = new int[1];
                isAndHash = true;
                entries = Reflector.EmptyArray<Entry>();
            }

            private FixedSizeStorage(int capacity, bool isAndHash, IEqualityComparer<T>? comparer)
            {
                this.isAndHash = isAndHash;
                this.comparer = comparer;
                buckets = new int[GetBucketSize(capacity)];
                entries = new Entry[capacity];
            }

            #endregion

            #endregion

            #region Methods

            #region Static Methods

            internal static bool TryInitialize(ICollection<T> collection, bool isAndHash, IEqualityComparer<T>? comparer, out FixedSizeStorage result)
            {
                // initialization may fail if collection.Count changes during the process
                result = new FixedSizeStorage(collection.Count, isAndHash, comparer);
                return result.TryInitialize(collection);
            }

            #endregion

            #region Instance Methods

            #region Public Methods

            public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
                => TryGetValueInternal(equalValue, GetHashCode(equalValue), out actualValue) == true;

            public bool Contains(T item) => ContainsInternal(item, GetHashCode(item)) == true;
            public bool Add(T item) => TryAddInternal(item, GetHashCode(item)) == true;
            public bool Remove(T item) => TryRemoveInternal(item, GetHashCode(item)) == true;

            public void Clear()
            {
                int count = entries.Length;
                for (int i = 0; i < count; i++)
                {
                    if (Interlocked.Exchange(ref entries[i].IsDeleted, 1) == 0)
                        Interlocked.Increment(ref deletedCount);
                }
            }

            #endregion

            #region Internal Methods

            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal bool? ContainsInternal(T item, uint hashCode)
            {
                Entry[] items = entries;
                IEqualityComparer<T> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];

                    // item found: returning whether is not deleted.
                    if (entryRef.Hash == hashCode && comp.Equals(entryRef.Value, item))
                        return Volatile.Read(ref entryRef.IsDeleted) != 0;

                    i = entryRef.Next;
                }

                // not found
                return null;
            }

            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal bool? TryGetValueInternal(T equalValue, uint hashCode, out T? actualValue)
            {
                Entry[] items = entries;
                IEqualityComparer<T> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Value, equalValue))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // item found: returning actual value if not deleted
                    if (Volatile.Read(ref entryRef.IsDeleted) == 0)
                    {
                        // works without locking because the value never changes
                        actualValue = entryRef.Value;
                        return true;
                    }

                    // deleted
                    actualValue = default;
                    return false;
                }

                // not found
                actualValue = default;
                return null;
            }

            /// <summary>
            /// Tries to insert an item. Returns null if no entry found for item. Adding succeeds only if the item was deleted previously.
            /// </summary>
            internal bool? TryAddInternal(T item, uint hashCode)
            {
                Entry[] items = entries;
                IEqualityComparer<T> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Value, item))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // already added
                    if (Volatile.Read(ref entryRef.IsDeleted) == 0)
                        return false;

                    Volatile.Write(ref entryRef.IsDeleted, 1);
                    Interlocked.Decrement(ref deletedCount);
                    return true;

                }

                // not found
                return null;
            }

            internal bool? TryRemoveInternal(T item, uint hashCode)
            {
                Entry[] items = entries;
                IEqualityComparer<T> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Value, item))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    if (Interlocked.Exchange(ref entryRef.IsDeleted, 1) != 0)
                        return false;

                    Interlocked.Increment(ref deletedCount);
                    return true;
                }

                // not found
                return null;
            }

            internal InternalEnumerator GetInternalEnumerator() => new InternalEnumerator(this);

            internal T[] ToArray()
            {
                // we are optimistic and allocating an array for the current count
                int len = Count;
                var result = new T[len];
                T[]? rest = null;

                InternalEnumerator enumerator = GetInternalEnumerator();
                int index = 0;
                while (enumerator.MoveNext())
                {
                    if (index < len)
                    {
                        result[index] = enumerator.Current;
                        index += 1;
                        continue;
                    }

                    // If new elements were added, allocating array for the rest of the elements.
                    // Now we are pessimistic and allocating the maximum possible capacity
                    rest ??= new T[entries.Length - len];
                    rest[index - len] = enumerator.Current;
                    index += 1;
                }

                // the optimistic allocation won: there was no count change during the enumeration
                if (index == len)
                    return result;

                // elements were added or deleted: resizing result
                Array.Resize(ref result, index);
                if (index < len)
                    return result;

                // elements were added: copying rest into the enlarged result
                Array.Copy(rest!, 0, result, len, index - len);
                return result;
            }

            #endregion

            #region Private Methods

            private void Initialize(FixedSizeStorage other, TempStorage mergeWith)
            {
                int otherCount = other.entries.Length;
                int count = otherCount + mergeWith.Count;
                buckets = new int[GetBucketSize(count)];
                entries = new Entry[count];
                CopyFrom(other);
                CopyFrom(mergeWith, otherCount);
            }

            /// <summary>
            /// Similar to <see cref="Initialize"/> but tries to skip deleted entries.
            /// Might fail if elements have been added/removed to other during the process.
            /// </summary>
            private bool TryInitialize(FixedSizeStorage other, TempStorage mergeWith)
            {
                int otherCount = other.Count;
                int count = otherCount + mergeWith.Count;
                buckets = new int[GetBucketSize(count)];
                entries = new Entry[count];
                if (!TryCopyFrom(other, otherCount))
                    return false;
                CopyFrom(mergeWith, otherCount);
                return true;
            }

            private bool TryInitialize(IEnumerable<T> collection)
            {
                IEqualityComparer<T> comp = comparer ?? defaultComparer;
                int index = 0;
                int[] localBuckets = buckets;
                Entry[] items = entries;
                int capacity = items.Length;

                foreach (T item in collection)
                {
                    if (index == capacity)
                        return false;

                    uint hashCode = GetHashCode(item);
                    ref int bucketRef = ref localBuckets[GetBucketIndex(hashCode)];

                    for (int i = bucketRef - 1; i >= 0; i = items[i].Next)
                    {
                        // duplicates by the used comparer are skipped to be compatible with HashSet<T>
                        if (items[i].Hash == hashCode && comp.Equals(items[i].Value, item))
                            goto continueOuter;
                    }

                    ref Entry itemRef = ref items[index];
                    itemRef.Hash = hashCode;
                    itemRef.Next = bucketRef - 1; // Next is zero-based
                    itemRef.Value = item;
                    bucketRef = ++index; // bucket indices are 1-based

                continueOuter:;
                }

                return index == capacity;
            }

            private void CopyFrom(FixedSizeStorage other)
            {
                // Writing this instance is non-volatile because we are coming from constructor.
                // If items are overwritten/deleted in other during this initialization it should be handled by the caller.
                int[] localBuckets = buckets;
                Entry[] items = entries;

                Entry[] otherItems = other.entries;
                int len = otherItems.Length;
                int index = 0;
                while (index < len)
                {
                    ref Entry oldItemRef = ref otherItems[index];
                    ref Entry newItemRef = ref items[index];
                    newItemRef.Hash = oldItemRef.Hash;
                    newItemRef.Value = oldItemRef.Value;
#if NET35 || NET40
                    newItemRef.IsDeleted = oldItemRef.IsDeleted;
#else
                    newItemRef.IsDeleted = Volatile.Read(ref oldItemRef.IsDeleted);
#endif


                    // assuming other was already consistent so not checking for duplicates by the comparer
                    ref int bucketRef = ref localBuckets[GetBucketIndex(newItemRef.Hash)];
                    newItemRef.Next = bucketRef - 1; // Next is zero-based
                    bucketRef = ++index; // bucket indices are 1-based

                    // as we check the copied value deletedCount will not be invalid even if the original instance is modified
                    if (newItemRef.IsDeleted != 0)
                        deletedCount += 1;
                }
            }

            private bool TryCopyFrom(FixedSizeStorage other, int referenceCount)
            {
                int[] localBuckets = buckets;
                Entry[] items = entries;

                Entry[] otherItems = other.entries;
                int len = otherItems.Length;
                int count = 0;

                for (int i = 0; i < len; i++)
                {
                    ref Entry oldItemRef = ref otherItems[i];

                    // skipping deleted items
                    if (Volatile.Read(ref oldItemRef.IsDeleted) != 0)
                        continue;

                    // elements have been added
                    if (count == referenceCount)
                        return false;

                    ref Entry newItemRef = ref items[count];
                    ref int bucketRef = ref localBuckets[GetBucketIndex(oldItemRef.Hash)];
                    newItemRef.Hash = oldItemRef.Hash;
                    newItemRef.Value = oldItemRef.Value;
                    newItemRef.Next = bucketRef - 1; // Next is zero-based
                    bucketRef = ++count; // bucket indices are 1-based
                }

                return count == referenceCount;
            }

            private void CopyFrom(TempStorage other, int index)
            {
                int[] localBuckets = buckets;
                Entry[] items = entries;

                TempStorage.InternalEnumerator enumerator = other.GetInternalEnumerator();
                while (enumerator.MoveNext())
                {
                    // assuming other was already consistent so not checking for duplicates by the comparer
                    ref int bucketRef = ref localBuckets[GetBucketIndex(enumerator.Current.Hash)];
                    ref Entry itemRef = ref items[index];
                    itemRef.Hash = enumerator.Current.Hash;
                    itemRef.Next = bucketRef - 1; // Next is zero-based
                    itemRef.Value = enumerator.Current.Value;
                    bucketRef = ++index; // bucket indices are 1-based
                }
            }

            [MethodImpl(MethodImpl.AggressiveInlining)]
            private uint GetHashCode(T item)
            {
                if (item == null)
                    return 0U;
                IEqualityComparer<T>? comp = comparer;
                return (uint)(comp == null ? item.GetHashCode() : comp.GetHashCode(item));
            }

            private uint GetBucketSize(int capacity)
            {
                if (isAndHash)
                {
                    uint bucketSize = (uint)HashHelper.GetPowerOfTwo(Math.Max(2, capacity));
                    hashingOperand = bucketSize - 1;
                    return bucketSize;
                }

                return hashingOperand = (uint)HashHelper.GetPrime(capacity);
            }

            /// <summary>
            /// An if in a non-virtual method is still faster than calling an abstract method, a delegate or even a C# 9 function pointer
            /// </summary>
            [MethodImpl(MethodImpl.AggressiveInlining)]
            private uint GetBucketIndex(uint hashCode) => isAndHash
                ? hashCode & hashingOperand
                : hashCode % hashingOperand;

            #endregion

            #endregion

            #endregion
        }
    }
}
