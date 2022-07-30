#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeHashSet.TempStorage.cs
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

using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    partial class ThreadSafeHashSet<T>
    {
        /// <summary>
        /// When instantiated, always has a preallocated storage.
        /// Not thread-safe so the consumer must do the locking when calling the internal members.
        /// </summary>
        internal sealed class TempStorage
        {
            #region Nested structs

            #region Entry struct

            [DebuggerDisplay("{" + nameof(Value) + "}")]
            private struct Entry
            {
                #region Fields

                internal uint Hash;
                /// <summary>
                /// Zero-based index of a chained item in the current bucket or -1 if last.
                /// Deleted items use negative indices below -1. Last deleted item has index -2.
                /// </summary>
                internal int Next;
                [AllowNull] internal T Value;

                #endregion
            }

            #endregion

            #region InternalEnumerator struct

            internal struct InternalEnumerator
            {
                #region Fields

                #region Internal Fields

                internal (uint Hash, T Value) Current;

                #endregion

                #region Private Fields

                private readonly int usedCount;
                private readonly Entry[] entries;

                private int pos;

                #endregion

                #endregion

                #region Constructors

                internal InternalEnumerator(TempStorage owner)
                {
                    usedCount = owner.usedCount;
                    entries = owner.entries;
                    pos = 0;
                    Current = default;
                }

                #endregion

                #region Methods

                internal bool MoveNext()
                {
                    while (pos < usedCount)
                    {
                        ref var entry = ref entries[pos];
                        pos += 1;

                        // skipping deleted items
                        if (entry.Next < -1)
                            continue;

                        Current.Hash = entry.Hash;
                        Current.Value = entry.Value!;
                        return true;
                    }

                    return false;
                }

                #endregion
            }

            #endregion

            #endregion

            #region Constants

            private const int deletedNextBase = -3;

            #endregion

            #region Fields

            private readonly IEqualityComparer<T>? comparer;
            private readonly bool isAndHash;

            private int usedCount; // used elements in items including deleted ones
            private Entry[] entries = default!;
            private int[] buckets = default!; // 1-based indices for entries. 0 if unused.
            private uint hashingOperand; // buckets.Length - 1 for AND hashing, buckets.Length for MOD hashing
            private int deletedCount;
            private int deletedItemsBucket; // First deleted entry among used elements. -1 if there are no deleted elements.

            #endregion

            #region Properties

            #region Public Properties

            public int Count => usedCount - deletedCount;

            #endregion

            #region Internal Properties

            internal bool IsAndHash => isAndHash;
            internal IEqualityComparer<T>? Comparer => comparer;

            #endregion

            #endregion

            #region Constructors

            internal TempStorage(int capacity, IEqualityComparer<T>? comparer, bool isAndHash)
            {
                Debug.Assert(capacity > 0, "Nonzero initial capacity is expected in CustomDictionary");
                this.isAndHash = isAndHash;
                this.comparer = comparer;
                Initialize(capacity);
            }

            internal TempStorage(IEnumerable<T> collection, IEqualityComparer<T>? comparer, bool isAndHash)
                : this((collection as ICollection<T>)?.Count ?? defaultCapacity, comparer, isAndHash)
            {
                foreach (T item in collection)
                    Add(item);
            }

            #endregion

            #region Methods

            #region Public Methods

            public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
                => TryGetValueInternal(equalValue, GetHashCode(equalValue), out actualValue);

            public bool Contains(T item) => ContainsInternal(item, GetHashCode(item));
            public bool Add(T item) => AddInternal(item, GetHashCode(item));
            public bool Remove(T item) => TryRemoveInternal(item, GetHashCode(item));

            #endregion

            #region Internal Methods

            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal bool ContainsInternal(T item, uint hashCode)
            {
                Entry[] items = entries;
                IEqualityComparer<T> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash == hashCode && comp.Equals(entryRef.Value, item))
                        return true;

                    i = entryRef.Next;
                }

                return false;
            }

            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal bool TryGetValueInternal(T equalValue, uint hashCode, [MaybeNullWhen(false)] out T actualValue)
            {
                Entry[] items = entries;
                IEqualityComparer<T> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash == hashCode && comp.Equals(entryRef.Value, equalValue))
                    {
                        actualValue = entryRef.Value;
                        return true;
                    }

                    i = entryRef.Next;
                }

                actualValue = default;
                return false;
            }

            internal bool AddInternal(T item, uint hashCode)
            {
                Entry[] items = entries;
                IEqualityComparer<T> comp = comparer ?? defaultComparer;
                ref int bucketRef = ref buckets[GetBucketIndex(hashCode)];
                int index = bucketRef - 1;

                // searching for an existing item
                while (index >= 0)
                {
                    ref Entry entryRef = ref items[index];

                    // existing item found
                    if (entryRef.Hash == hashCode && comp.Equals(entryRef.Value, item))
                        return false;

                    index = entryRef.Next;
                }

                AddNew(item, hashCode, ref bucketRef);
                return true;
            }

            internal bool TryRemoveInternal(T item, uint hashCode)
            {
                int[] bucketsLocal = buckets;
                Entry[] items = entries;
                IEqualityComparer<T> comp = comparer ?? defaultComparer;
                int previous = -1;
                ref int bucketRef = ref bucketsLocal[GetBucketIndex(hashCode)];
                int i = bucketRef - 1;

                // searching for an existing key
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Value, item))
                    {
                        previous = i;
                        i = items[i].Next;
                        continue;
                    }

                    // removing entry from the original bucket
                    if (previous < 0)
                        bucketRef = entryRef.Next + 1;
                    else
                        items[previous].Next = entryRef.Next;

                    // Moving entry to a special bucket of removed entries were indices have negative value less than -1
                    entryRef.Next = deletedNextBase - deletedItemsBucket;
                    deletedItemsBucket = i;
                    deletedCount += 1;

                    // cleanup
                    if (Reflector<T>.IsManaged)
                        entryRef.Value = default;

                    return true;
                }

                // Not found
                return false;
            }

            internal InternalEnumerator GetInternalEnumerator() => new InternalEnumerator(this);

            #endregion

            #region Private Methods

            private void Initialize(int capacity)
            {
                // unlike many other dictionaries in this project even the initial length of entries can be longer than
                // capacity because this type is meant to be used as a short-living dynamically growing storage rather than a fixed size long-term cache
                uint size = GetBucketSize(capacity);
                buckets = new int[size];
                entries = new Entry[size];
                deletedItemsBucket = -1;
                usedCount = 0;
                deletedCount = 0;
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

            [MethodImpl(MethodImpl.AggressiveInlining)]
            private uint GetHashCode(T item)
            {
                if (item == null)
                    return 0U;
                IEqualityComparer<T>? comp = comparer;
                return (uint)(comp == null ? item.GetHashCode() : comp.GetHashCode(item));
            }

            private void AddNew(T item, uint hashCode, ref int bucketRef)
            {
                Entry[] items = entries;
                int index;
                // Here existing key was not found
                bool fromDeleted = deletedCount > 0;

                // re-using the removed entries if possible
                if (fromDeleted)
                {
                    index = deletedItemsBucket;
                    deletedCount -= 1;
                }
                // otherwise, adding a new entry
                else
                {
                    // storage expansion is needed
                    if (usedCount == items.Length)
                    {
                        Resize(items.Length << 1, out int[] resizedBuckets, out items);
                        bucketRef = ref resizedBuckets[GetBucketIndex(hashCode)];
                    }

                    index = usedCount;
                    usedCount += 1;
                }

                ref Entry entryRef = ref items[index];
                if (fromDeleted)
                    deletedItemsBucket = deletedNextBase - entryRef.Next;
                entryRef.Hash = hashCode;
                entryRef.Next = bucketRef - 1; // Next is zero-based
                entryRef.Value = item;
                bucketRef = index + 1; // bucket indices are 1-based
            }

            private void Resize(int newCapacity, out int[] newBuckets, out Entry[] newEntries)
            {
                uint newBucketSize = GetBucketSize(newCapacity);
                newBuckets = new int[newBucketSize];
                newEntries = new Entry[newBucketSize];
                Array.Copy(entries, 0, newEntries, 0, usedCount);

                // re-applying buckets for the new size
                for (int i = 0; i < usedCount; i++)
                {
                    uint bucket = GetBucketIndex(newEntries[i].Hash);
                    newEntries[i].Next = newBuckets[bucket] - 1;
                    newBuckets[bucket] = i + 1;
                }

                buckets = newBuckets;
                entries = newEntries;
            }

            #endregion

            #endregion
        }
    }
}
