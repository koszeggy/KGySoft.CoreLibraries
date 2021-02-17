﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeDictionary.Locking.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
    partial class ThreadSafeDictionary<TKey, TValue>
    {
        /// <summary>
        /// When instantiated, always has a preallocated storage.
        /// Not thread-safe so the consumer must do the locking for the Try... members.
        /// </summary>
        internal sealed class LockingStorage
        {
            #region Nested structs

            #region Entry struct

            [DebuggerDisplay("[{" + nameof(Key) + "}; {" + nameof(Value) + "}]")]
            private struct Entry
            {
                #region Fields

                internal uint Hash;
                internal TKey Key;
                [AllowNull]internal TValue Value;

                /// <summary>
                /// Zero-based index of a chained item in the current bucket or -1 if last.
                /// Deleted items use negative indices below -1. Last deleted item has index -2.
                /// </summary>
                internal int Next;

                #endregion
            }

            #endregion

            #region CustomEnumerator struct

            internal struct CustomEnumerator
            {
                #region Fields

                #region Internal Fields

                internal (uint Hash, TKey Key, TValue Value) Current;

                #endregion

                #region Private Fields

                private readonly int usedCount;
                private readonly Entry[] entries;

                private int pos;

                #endregion

                #endregion

                #region Constructors

                internal CustomEnumerator(LockingStorage owner)
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
                        Current.Key = entry.Key;
                        Current.Value = entry.Value;
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

            #region Static Fields

            private static readonly IEqualityComparer<TKey> defaultComparer = ComparerHelper<TKey>.EqualityComparer;
            private static readonly IEqualityComparer<TValue> valueComparer = ComparerHelper<TValue>.EqualityComparer;

#if NETFRAMEWORK || NETSTANDARD2_0
            // ReSharper disable StaticMemberInGenericType - they depend on type arguments
            private static readonly bool isKeyManaged = !typeof(TKey).IsUnmanaged();
            private static readonly bool isValueManaged = !typeof(TValue).IsUnmanaged();
            // ReSharper restore StaticMemberInGenericType
#endif

            #endregion

            #region Instance Fields

            private readonly IEqualityComparer<TKey>? comparer;
            private readonly bool isAndHash;

            private int usedCount; // used elements in items including deleted ones
            private Entry[] entries;
            private int[] buckets; // 1-based indices for entries. 0 if unused.
            private uint hashingOperand; // buckets.Length - 1 for AND hashing, buckets.Length for MOD hashing
            private int deletedCount;
            private int deletedItemsBucket; // First deleted entry among used elements. -1 if there are no deleted elements.

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            #region Public Properties

            public int Count => usedCount - deletedCount;

            #endregion

            #region Internal Properties

            internal bool IsAndHash => isAndHash;
            internal IEqualityComparer<TKey>? Comparer => comparer;

            #endregion

            #endregion

            #region Indexers

            public TValue this[TKey key]
            {
                get => TryGetValue(key, out TValue value) ? value : Throw.KeyNotFoundException<TValue>(Res.IDictionaryKeyNotFound);
                set
                {
                    if (key == null!)
                        Throw.ArgumentNullException(Argument.key);
                    TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.OverwriteIfExists);
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal LockingStorage(int capacity, IEqualityComparer<TKey>? comparer, bool isAndHash)
            {
                Debug.Assert(capacity > 0, "Nonzero initial capacity is expected in CustomDictionary");
                this.isAndHash = isAndHash;
                this.comparer = comparer;
                Initialize(capacity);
            }

            #endregion

            #region Methods

            #region Static Methods

            [MethodImpl(MethodImpl.AggressiveInlining)]
            private static bool IsKeyManaged() =>
#if !(NETFRAMEWORK || NETSTANDARD2_0)
                // not "caching" the result of this call because that would make the JIT-ed code slower
                RuntimeHelpers.IsReferenceOrContainsReferences<TKey>();
#else
                isKeyManaged;
#endif

            [MethodImpl(MethodImpl.AggressiveInlining)]
            private static bool IsValueManaged() =>
#if !(NETFRAMEWORK || NETSTANDARD2_0)
                // not "caching" the result of this call because that would make the JIT-ed code slower
                RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();
#else
                isValueManaged;
#endif

            #endregion

            #region Instance Methods

            #region Public Methods

            public bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryGetValueInternal(key, GetHashCode(key), out value);
                // TODO: delete
                //ref TValue resultRef = ref TryGetValueInternal(key, GetHashCode(key));
                //if (!Unsafe.IsNullRef(ref resultRef))
                //{
                //    value = resultRef;
                //    return true;
                //}

                //value = default;
                //return false;
            }

            public void Add(TKey key, TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);

                TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.ThrowIfExists);
            }

            public bool TryAdd(TKey key, TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);

                return TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.DoNotOverwrite);
            }

            public bool TryReplace(TKey key, TValue newValue, TValue originalValue)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryReplaceInternal(key, newValue, originalValue, GetHashCode(key));
            }

            public bool TryRemove(TKey key, [MaybeNullWhen(false)]out TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryRemoveInternal(key, GetHashCode(key), out value);
            }

            public bool Remove(TKey key)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryRemoveInternal(key, GetHashCode(key));
            }

            public void Clear() => Initialize(0);

            #endregion

            #region Internal Methods

            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal bool TryGetValueInternal(TKey key, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                int[] bucketsLocal = buckets;
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = bucketsLocal[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash == hashCode && comp.Equals(entryRef.Key, key))
                    {
                        value = entryRef.Value;
                        return true;
                    }

                    i = entryRef.Next;
                }

                value = default;
                return false;
            }

            internal bool TryInsertInternal(TKey key, TValue value, uint hashCode, DictionaryInsertion behavior)
            {
                int[] bucketsLocal = buckets;
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;
                int index;
                ref int bucketRef = ref bucketsLocal[GetBucketIndex(hashCode)];

                // searching for an existing key
                for (index = bucketRef - 1; index >= 0; index = items[index].Next)
                {
                    if (items[index].Hash != hashCode || !comp.Equals(items[index].Key, key))
                        continue;

                    // existing key found
                    if (behavior == DictionaryInsertion.DoNotOverwrite)
                        return false;
                    if (behavior == DictionaryInsertion.ThrowIfExists)
                        Throw.ArgumentException(Argument.key, Res.IDictionaryDuplicateKey);

                    // overwriting existing element
                    items[index].Value = value;
                    return true;
                }

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
                        Resize(items.Length << 1, out bucketsLocal, out items);
                        bucketRef = ref bucketsLocal[GetBucketIndex(hashCode)];
                    }

                    index = usedCount;
                    usedCount += 1;
                }

                ref Entry entryRef = ref items[index];
                if (fromDeleted)
                    deletedItemsBucket = deletedNextBase - entryRef.Next;
                entryRef.Hash = hashCode;
                entryRef.Next = bucketRef - 1; // Next is zero-based
                entryRef.Key = key;
                entryRef.Value = value;
                bucketRef = index + 1; // bucket indices are 1-based
                return true;
            }

            internal bool TryReplaceInternal(TKey key, TValue newValue, TValue originalValue, uint hashCode)
            {
                int[] bucketsLocal = buckets;
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                // searching for an existing key
                int i = bucketsLocal[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // existing key found: checking value
                    if (!valueComparer.Equals(entryRef.Value, originalValue))
                        return false;

                    // overwriting existing element
                    entryRef.Value = newValue;
                    return true;
                }

                // Existing key was not found
                return false;
            }

            internal bool TryRemoveInternal(TKey key, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                int[] bucketsLocal = buckets;
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;
                int previous = -1;
                ref int bucketRef = ref bucketsLocal[GetBucketIndex(hashCode)];
                int i = bucketRef - 1;

                // searching for an existing key
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
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

                    value = entryRef.Value;

                    // cleanup
                    if (IsKeyManaged())
                        entryRef.Key = default;
                    if (IsValueManaged())
                        entryRef.Value = default;

                    return true;
                }

                // Not found
                value = default;
                return false;
            }

            internal bool TryRemoveInternal(TKey key, uint hashCode)
            {
                int[] bucketsLocal = buckets;
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;
                int previous = -1;
                ref int bucketRef = ref bucketsLocal[GetBucketIndex(hashCode)];
                int i = bucketRef - 1;

                // searching for an existing key
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
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
                    if (IsKeyManaged())
                        entryRef.Key = default;
                    if (IsValueManaged())
                        entryRef.Value = default;

                    return true;
                }

                // Not found
                return false;
            }

            internal CustomEnumerator GetCustomEnumerator() => new CustomEnumerator(this);

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
                    uint bucketSize = (uint)Math.Max(2, capacity).GetNextPowerOfTwo();
                    hashingOperand = bucketSize - 1;
                    return bucketSize;
                }

                return hashingOperand = (uint)PrimeHelper.GetPrime(capacity);
            }

            /// <summary>
            /// An if in a non-virtual method is still faster than calling an abstract method, a delegate or even a C# 9 function pointer
            /// </summary>
            [MethodImpl(MethodImpl.AggressiveInlining)]
            private uint GetBucketIndex(uint hashCode) => isAndHash
                ? hashCode & hashingOperand
                : hashCode % hashingOperand;

            [MethodImpl(MethodImpl.AggressiveInlining)]
            private uint GetHashCode(TKey key)
            {
                IEqualityComparer<TKey>? comp = comparer;
                return (uint)(comp == null ? key.GetHashCode() : comp.GetHashCode(key));
            }

            private void Resize(int newCapacity, out int[] newBuckets, out Entry[] newEntries)
            {
                uint newBucketSize = GetBucketSize(newCapacity);
                newBuckets = new int[newBucketSize];
                newEntries = new Entry[newBucketSize];
                Array.Copy(entries!, 0, newEntries, 0, usedCount);

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

            #endregion
        }
    }
}