#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeDictionary.FixedSizeStorage.cs
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    partial class ThreadSafeDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents a lock-free fixed size thread safe dictionary that supports updating values in a thread-safe manner,
        /// though adding new values is not supported. Removing and re-setting keys are supported though, which is also thread-safe.
        /// </summary>
        internal sealed class FixedSizeStorage
        {
            #region Nested structs

            #region Entry struct

            [DebuggerDisplay("[{" + nameof(Key) + "}; {" + nameof(DebugValue) + "}]")]
            private struct Entry
            {
                #region Fields

                internal uint Hash;

                internal TKey Key;

                /// <summary>
                /// A reference to the actual value that can be accessed by volatile read. Value is null if the item is deleted.
                /// If the inner value can be overwritten atomically, then the reference is replaced only on delete/restore.
                /// </summary>
#if NET35 || NET40
                volatile // because there is no generic Volatile.Read in .NET 3.5/4.0
#endif
                internal StrongBox<TValue>? Value;

                /// <summary>
                /// Zero-based index of a chained item in the current bucket or -1 if last.
                /// In this collection this field is practically read-only. Deleted items are indicated by a null Value.
                /// </summary>
                internal int Next;

                #endregion

                #region Properties

                private object? DebugValue => Value == null ? "<Deleted>" : Value.Value;

                #endregion
            }

            #endregion

            #region InternalEnumerator struct

            internal struct InternalEnumerator
            {
                #region Fields

                #region Internal Fields

                internal (TKey Key, TValue Value) Current;

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
                    Current = default;
                }

                #endregion

                #region Methods

                internal bool MoveNext()
                {
                    while (pos < entries.Length)
                    {
                        ref var entryRef = ref entries[pos];
                        pos += 1;

#if NET35 || NET40
                        var box = entryRef.Value;
#else
                        var box = Volatile.Read(ref entryRef.Value);
#endif

                        // skipping deleted items
                        if (box == null)
                            continue;

                        Current = (entryRef.Key, box.Value!);
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

            #region Internal Fields

            internal static readonly FixedSizeStorage Empty = new FixedSizeStorage();

            #endregion

            #region Private Fields

            private static readonly bool isAtomic = Reflector<TValue>.SizeOf <= IntPtr.Size;

            #endregion

            #endregion

            #region Instance Fields

            private readonly IEqualityComparer<TKey>? comparer;
            private readonly bool isAndHash;

            private Entry[] entries = default!;
            private int[] buckets = default!; // 1-based indices for entries. 0 if unused.
            private uint hashingOperand; // buckets.Length - 1 for AND hashing, buckets.Length for MOD hashing
            private int deletedCount;

            #endregion

            #endregion

            #region Properties and Indexers

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

            #region Indexers

            public TValue this[TKey key]
            {
                get => TryGetValue(key, out TValue? value) ? value : Throw.KeyNotFoundException<TValue>(Res.IDictionaryKeyNotFound);
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

            #region Internal Constructors

            internal FixedSizeStorage(FixedSizeStorage other, TempStorage mergeWith, bool removeDeletedKeys)
            {
                // isAndHash and comparer are not taken from other because it can be Empty
                isAndHash = mergeWith.IsAndHash;
                comparer = mergeWith.Comparer;

                if (removeDeletedKeys)
                {
                    // Possible endless retries are prevented by WaitWhileMerging (this method is called with isMerging=true)
                    // Data loss is prevented by the retry mechanisms in ThreadSafeDictionary.TryInsertInternal/TryRemove.
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

            private FixedSizeStorage(int capacity, bool isAndHash, IEqualityComparer<TKey>? comparer)
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

            internal static bool TryInitialize(ICollection<KeyValuePair<TKey, TValue>> collection, bool isAndHash, IEqualityComparer<TKey>? comparer, out FixedSizeStorage result)
            {
                // initialization may fail if collection.Count changes during the process
                result = new FixedSizeStorage(collection.Count, isAndHash, comparer);
                return result.TryInitialize(collection);
            }

            #endregion
            
            #region Instance Methods

            #region Public Methods

            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);

                return TryGetValueInternal(key, GetHashCode(key), out value) == true;
            }

            public bool TryAdd(TKey key, TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.DoNotOverwrite) == true;
            }

            public bool TryReplace(TKey key, TValue newValue, TValue originalValue)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryReplaceInternal(key, newValue, originalValue, GetHashCode(key)) == true;
            }

            public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryRemoveInternal(key, GetHashCode(key), out value) == true;
            }

            public bool TryRemove(TKey key, TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryRemoveInternal(key, value, GetHashCode(key)) == true;
            }

            public bool Remove(TKey key)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryRemoveInternal(key, GetHashCode(key)) == true;
            }

            public void Clear()
            {
                int count = entries.Length;
                for (int i = 0; i < count; i++)
                {
                    if (Interlocked.Exchange(ref entries[i].Value, null) != null)
                        Interlocked.Increment(ref deletedCount);
                }
            }

            public bool TryGetOrAdd(TKey key, TValue addValue, [MaybeNullWhen(false)]out TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryGetOrAdd(key, addValue, GetHashCode(key), out value);
            }

            public bool TryAddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory, [MaybeNullWhen(false)]out TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                if (updateValueFactory == null!)
                    Throw.ArgumentNullException(nameof(updateValueFactory));
                return TryAddOrUpdate(key, addValue, updateValueFactory, GetHashCode(key), out value);
            }

            #endregion

            #region Internal Methods

            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal bool? TryGetValueInternal(TKey key, uint hashCode, out TValue? value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // key found: lock-free reading.
#if NET35 || NET40
                    StrongBox<TValue>? box = entryRef.Value;
#else
                    StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                    if (box != null)
                    {
                        // works without locking because the box is always replaced if TValue cannot be copied atomically
                        value = box.Value;
                        return true;
                    }

                    // deleted
                    value = default;
                    return false;
                }

                // not found
                value = default;
                return null;
            }

            /// <summary>
            /// Tries to insert a value. Returns null if key not found. Adding succeeds only if a key was deleted previously.
            /// </summary>
            internal bool? TryInsertInternal(TKey key, TValue value, uint hashCode, DictionaryInsertion behavior)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        // entry was deleted, trying to add
                        if (box == null)
                        {
                            if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(value), null) != null)
                                continue; // lost race

                            Interlocked.Decrement(ref deletedCount);
                            return true;
                        }

                        // value can be overwritten
                        if (behavior == DictionaryInsertion.OverwriteIfExists)
                        {
                            if (isAtomic)
                                box.Value = value;
                            else
                            {
                                if (Interlocked.Exchange(ref entryRef.Value, new StrongBox<TValue>(value)) == null)
                                    Interlocked.Decrement(ref deletedCount); // a deletion occurred after a lost race
                            }

                            return true;
                        }

                        // could not add
                        if (behavior == DictionaryInsertion.ThrowIfExists)
                            Throw.ArgumentException(Argument.key, Res.IDictionaryDuplicateKey);
                        return false;
                    }
                }

                // not found
                return null;
            }

            /// <summary>
            /// Tries to replace a value. Returns null if key not found.
            /// </summary>
            internal bool? TryReplaceInternal(TKey key, TValue newValue, TValue originalValue, uint hashCode)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        // deleted or original value does not match
                        if (box == null || !valueComparer.Equals(box.Value, originalValue))
                            return false;

                        if (isAtomic)
                        {
                            box.Value = newValue;
                            return true;
                        }

                        if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(newValue), box) == box)
                            return true;
                    }
                }

                // not found
                return null;
            }

            internal bool? TryRemoveInternal(TKey key, uint hashCode)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    if (Interlocked.Exchange(ref entryRef.Value, null) == null)
                        return false;

                    Interlocked.Increment(ref deletedCount);
                    return true;
                }

                // not found
                return null;
            }

            internal bool? TryRemoveInternal(TKey key, uint hashCode, out TValue? value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        // already deleted
                        if (box == null)
                        {
                            value = default;
                            return false;
                        }

                        if (Interlocked.CompareExchange(ref entryRef.Value, null, box) != box)
                            continue;

                        Interlocked.Increment(ref deletedCount);
                        value = box.Value;
                        return true;
                    }
                }

                // not found
                value = default;
                return null;
            }

            internal bool? TryRemoveInternal(TKey key, TValue value, uint hashCode)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        // already deleted or value does not match
                        if (box == null || !valueComparer.Equals(value, box.Value))
                            return false;

                        if (Interlocked.CompareExchange(ref entryRef.Value, null, box) != box)
                            continue;

                        Interlocked.Increment(ref deletedCount);
                        return true;
                    }
                }

                // not found
                return null;
            }

            internal bool TryAddOrUpdate(TKey key, TValue addValue, TValue updateValue, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // entry found, adding or updating will succeed now
                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        // deleted, trying to add
                        if (box == null)
                        {
                            if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(addValue), null) != null)
                                continue;
                            Interlocked.Decrement(ref deletedCount);
                            value = addValue;
                            return true;
                        }

                        // existing value found, trying to update
                        if (isAtomic)
                        {
                            box.Value = updateValue;

                            // as factoring may last a longer time we check whether box is still up-to-date
#if NET35 || NET40
                            if (box != entryRef.Value)
#else
                            if (box != Volatile.Read(ref entryRef.Value))
#endif
                            {
                                continue;
                            }
                        }
                        // no atomic update is possible, trying to replace the box
                        else if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(updateValue), box) != box)
                            continue;

                        value = updateValue;
                        return true;
                    }
                }

                // not found
                value = default;
                return false;
            }

            internal bool TryAddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // entry found, adding or updating will succeed now
                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        // deleted, trying to add
                        if (box == null)
                        {
                            if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(addValue), null) != null)
                                continue;
                            Interlocked.Decrement(ref deletedCount);
                            value = addValue;
                            return true;
                        }

                        // existing value found, trying to update
                        TValue newValue = updateValueFactory.Invoke(key, box.Value!);
                        if (isAtomic)
                        {
                            box.Value = newValue;

                            // as factoring may last a longer time we check whether box is still up-to-date
#if NET35 || NET40
                            if (box != entryRef.Value)
#else
                            if (box != Volatile.Read(ref entryRef.Value))
#endif
                            {
                                continue;
                            }
                        }
                        // no atomic update is possible, trying to replace the box
                        else if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(newValue), box) != box)
                            continue;

                        value = newValue;
                        return true;
                    }
                }

                // not found
                value = default;
                return false;
            }

            internal bool TryAddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory,
                uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // entry found, adding or updating will succeed now
                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        // deleted, trying to add
                        if (box == null)
                        {
                            value = addValueFactory.Invoke(key);
                            if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(value), null) != null)
                                continue;
                            Interlocked.Decrement(ref deletedCount);
                            return true;
                        }

                        // existing value found, trying to update
                        TValue newValue = updateValueFactory.Invoke(key, box.Value!);
                        if (isAtomic)
                        {
                            box.Value = newValue;

                            // as factoring may last a longer time we check whether box is still up-to-date
#if NET35 || NET40
                            if (box != entryRef.Value)
#else
                            if (box != Volatile.Read(ref entryRef.Value))
#endif
                            {
                                continue;
                            }
                        }
                        // no atomic update is possible, trying to replace the box
                        else if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(newValue), box) != box)
                            continue;

                        value = newValue;
                        return true;
                    }
                }

                // not found
                value = default;
                return false;
            }

            internal bool TryAddOrUpdate<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory,
                TArg factoryArgument, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // entry found, adding or updating will succeed now
                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        // deleted, trying to add
                        if (box == null)
                        {
                            value = addValueFactory.Invoke(key, factoryArgument);
                            if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(value), null) != null)
                                continue;
                            Interlocked.Decrement(ref deletedCount);
                            return true;
                        }

                        // existing value found, trying to update
                        TValue newValue = updateValueFactory.Invoke(key, box.Value!, factoryArgument);
                        if (isAtomic)
                        {
                            box.Value = newValue;

                            // as factoring may last a longer time we check whether box is still up-to-date
#if NET35 || NET40
                            if (box != entryRef.Value)
#else
                            if (box != Volatile.Read(ref entryRef.Value))
#endif
                            {
                                continue;
                            }
                        }
                        // no atomic update is possible, trying to replace the box
                        else if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(newValue), box) != box)
                            continue;

                        value = newValue;
                        return true;
                    }
                }

                // not found
                value = default;
                return false;
            }

            internal bool TryGetOrAdd(TKey key, TValue addValue, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // entry found, getting or adding will succeed now
                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        if (box != null)
                        {
                            // works without locking because the box is always replaced if TValue cannot be copied atomically
                            value = box.Value!;
                            return true;
                        }

                        // deleted, trying to add
                        if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(addValue), null) != null)
                            continue;

                        Interlocked.Decrement(ref deletedCount);
                        value = addValue;
                        return true;
                    }
                }

                // not found
                value = default;
                return false;
            }

            internal bool TryGetOrAdd(TKey key, Func<TKey, TValue> addValueFactory, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // entry found, getting or adding will succeed now
                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        if (box != null)
                        {
                            // works without locking because the box is always replaced if TValue cannot be copied atomically
                            value = box.Value!;
                            return true;
                        }

                        // deleted, trying to add
                        value = addValueFactory.Invoke(key);
                        if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(value), null) != null)
                            continue;
                        Interlocked.Decrement(ref deletedCount);
                        return true;
                    }
                }

                // not found
                value = default;
                return false;
            }

            internal bool TryGetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, TArg factoryArgument, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                Entry[] items = entries;
                IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

                int i = buckets[GetBucketIndex(hashCode)] - 1;
                while (i >= 0)
                {
                    ref Entry entryRef = ref items[i];
                    if (entryRef.Hash != hashCode || !comp.Equals(entryRef.Key, key))
                    {
                        i = entryRef.Next;
                        continue;
                    }

                    // entry found, getting or adding will succeed now
                    while (true)
                    {
#if NET35 || NET40
                        StrongBox<TValue>? box = entryRef.Value;
#else
                        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value);
#endif

                        if (box != null)
                        {
                            // works without locking because the box is always replaced if TValue cannot be copied atomically
                            value = box.Value!;
                            return true;
                        }

                        // deleted, trying to add
                        value = addValueFactory.Invoke(key, factoryArgument);
                        if (Interlocked.CompareExchange(ref entryRef.Value, new StrongBox<TValue>(value), null) != null)
                            continue;
                        Interlocked.Decrement(ref deletedCount);
                        return true;
                    }
                }

                // not found
                value = default;
                return false;
            }

            internal InternalEnumerator GetInternalEnumerator() => new InternalEnumerator(this);

            internal KeyValuePair<TKey, TValue>[] ToArray()
            {
                // we are optimistic and allocating an array for the current count
                int len = Count;
                var result = new KeyValuePair<TKey, TValue>[len];
                KeyValuePair<TKey, TValue>[]? rest = null;

                InternalEnumerator enumerator = GetInternalEnumerator();
                int index = 0;
                while (enumerator.MoveNext())
                {
                    if (index < len)
                    {
                        result[index] = new KeyValuePair<TKey, TValue>(enumerator.Current.Key, enumerator.Current.Value);
                        index += 1;
                        continue;
                    }

                    // If new elements were added, allocating array for the rest of the elements.
                    // Now we are pessimistic and allocating the maximum possible capacity
                    rest ??= new KeyValuePair<TKey, TValue>[entries.Length - len];
                    rest[index - len] = new KeyValuePair<TKey, TValue>(enumerator.Current.Key, enumerator.Current.Value);
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

            private bool TryInitialize(ICollection<KeyValuePair<TKey, TValue>> collection)
                => comparer == null ? TryInitializeDefault(collection) : TryInitializeByComparer(collection);

            private bool TryInitializeDefault(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            {
                int index = 0;
                int[] localBuckets = buckets;
                Entry[] items = entries;
                int capacity = items.Length;

                foreach (KeyValuePair<TKey, TValue> item in collection)
                {
                    if (item.Key == null!)
                        Throw.ArgumentNullException(Argument.key);
                    if (index == capacity)
                        return false;

                    uint hashCode = (uint)item.Key.GetHashCode();
                    ref int bucketRef = ref localBuckets[GetBucketIndex(hashCode)];

                    // avoiding duplicate keys by the used comparer
                    for (int i = bucketRef - 1; i >= 0; i = items[i].Next)
                    {
                        if (items![i].Hash != hashCode || !defaultComparer.Equals(items[i].Key, item.Key))
                            continue;

                        Throw.ArgumentException(Argument.key, Res.IDictionaryDuplicateKey);
                    }

                    // as this is the initialization from constructor, no volatile access/locking is needed here
                    ref Entry itemRef = ref items[index];
                    itemRef.Hash = hashCode;
                    itemRef.Next = bucketRef - 1; // Next is zero-based
                    itemRef.Key = item.Key;
                    itemRef.Value = new StrongBox<TValue>(item.Value);
                    bucketRef = ++index; // bucket indices are 1-based
                }

                return index == capacity;
            }

            private bool TryInitializeByComparer(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            {
                int index = 0;
                IEqualityComparer<TKey> customComparer = comparer!;
                int[] localBuckets = buckets;
                Entry[] items = entries;
                int capacity = items.Length;

                foreach (KeyValuePair<TKey, TValue> item in collection)
                {
                    if (item.Key == null!)
                        Throw.ArgumentNullException(Argument.key);
                    if (index == capacity)
                        return false;

                    uint hashCode = (uint)customComparer.GetHashCode(item.Key);
                    ref int bucketRef = ref localBuckets[GetBucketIndex(hashCode)];

                    // avoiding duplicate keys by the used comparer
                    for (int i = bucketRef - 1; i >= 0; i = items[i].Next)
                    {
                        if (items![i].Hash != hashCode || !customComparer.Equals(items[i].Key, item.Key))
                            continue;

                        Throw.ArgumentException(Argument.key, Res.IDictionaryDuplicateKey);
                    }

                    // as this is the initialization from constructor, no volatile access is needed here
                    ref Entry itemRef = ref items[index];
                    itemRef.Hash = hashCode;
                    itemRef.Next = bucketRef - 1; // Next is zero-based
                    itemRef.Key = item.Key;
                    itemRef.Value = new StrongBox<TValue>(item.Value);
                    bucketRef = ++index; // bucket indices are 1-based
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
                    newItemRef.Key = oldItemRef.Key;
#if NET35 || NET40
                    newItemRef.Value = oldItemRef.Value;
#else
                    newItemRef.Value = Volatile.Read(ref oldItemRef.Value);
#endif

                    // assuming other was already consistent so not checking for duplicate keys
                    ref int bucketRef = ref localBuckets[GetBucketIndex(newItemRef.Hash)];
                    newItemRef.Next = bucketRef - 1; // Next is zero-based
                    bucketRef = ++index; // bucket indices are 1-based

                    // as the inner box is a copy deletedCount will not be invalid even if the original instance is modified
                    if (newItemRef.Value == null)
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
#if NET35 || NET40
                    StrongBox<TValue>? box = oldItemRef.Value;
#else
                    StrongBox<TValue>? box = Volatile.Read(ref oldItemRef.Value);
#endif
                    // skipping deleted items
                    if (box == null)
                        continue;

                    // elements have been added
                    if (count == referenceCount)
                        return false;

                    ref Entry newItemRef = ref items[count];
                    ref int bucketRef = ref localBuckets[GetBucketIndex(oldItemRef.Hash)];
                    newItemRef.Hash = oldItemRef.Hash;
                    newItemRef.Key = oldItemRef.Key;
                    newItemRef.Value = box; // non-volatile write because we are coming from constructor
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
                    // assuming other was already consistent so not checking for duplicate keys
                    ref int bucketRef = ref localBuckets[GetBucketIndex(enumerator.Current.Hash)];
                    ref Entry itemRef = ref items[index];
                    itemRef.Hash = enumerator.Current.Hash;
                    itemRef.Next = bucketRef - 1; // Next is zero-based
                    itemRef.Key = enumerator.Current.Key;
                    itemRef.Value = new StrongBox<TValue>(enumerator.Current.Value);
                    bucketRef = ++index; // bucket indices are 1-based
                }
            }

            [MethodImpl(MethodImpl.AggressiveInlining)]
            private uint GetHashCode(TKey key)
            {
                IEqualityComparer<TKey>? comp = comparer;
                return (uint)(comp == null ? key.GetHashCode() : comp.GetHashCode(key));
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
