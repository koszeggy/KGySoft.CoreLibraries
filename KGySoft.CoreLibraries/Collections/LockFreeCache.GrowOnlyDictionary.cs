#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockFreeCache.GrowOnlyDictionary.cs
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

#endregion

namespace KGySoft.Collections
{
    partial class LockFreeCache<TKey, TValue>
    {
        /// <summary>
        /// Represents a lock-free thread safe dictionary where items can only be added and queried but not replaced or deleted.
        /// </summary>
        [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
        internal class GrowOnlyDictionary
        {
            #region Nested types

            #region Entry class

            [DebuggerDisplay("[{" + nameof(Key) + "}; {" + nameof(Value) + "}]")]
            private sealed class Entry
            {
                #region Fields

                internal readonly uint Hash;
                internal readonly TKey Key;
                internal readonly TValue Value;
                internal volatile Entry? Next;

                #endregion

                #region Constructors

                internal Entry(uint hashCode, TKey key, TValue value)
                {
                    Hash = hashCode;
                    Key = key;
                    Value = value;
                }

                #endregion
            }

            #endregion

            #region Bucket struct

            /// <summary>
            /// Just a volatile reference holder so array items can be treated volatile.
            /// This is needed because in older targeted platforms there is no generic Volatile.Read/Write
            /// </summary>
            [DebuggerDisplay("{" + nameof(First) + "}")]
            private struct Bucket
            {
                #region Fields

                internal volatile Entry? First;

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

                private readonly Bucket[] buckets;

                private int pos;
                private Entry? currentEntry;


                #endregion

                #endregion

                #region Constructors

                internal CustomEnumerator(GrowOnlyDictionary owner) : this()
                {
                    buckets = owner.buckets;
                }

                #endregion

                #region Methods

                internal bool MoveNext()
                {
                    // next entry in the current bucket
                    currentEntry = currentEntry?.Next;

                    // next non-empty bucket
                    while (currentEntry == null)
                    {
                        if (pos == buckets.Length)
                            return false;
                        currentEntry = buckets[pos].First;
                        pos += 1;
                    }

                    Current = (currentEntry.Hash, currentEntry.Key, currentEntry.Value);
                    return true;
                }

                #endregion
            }

            #endregion

            #endregion

            #region Fields

            #region Static Fields

            private static readonly IEqualityComparer<TKey> defaultComparer = ComparerHelper<TKey>.EqualityComparer;

            #endregion

            #region Instance Fields

            private readonly Bucket[] buckets;

            private uint hashingOperand; // buckets.Length - 1 for AND hashing, buckets.Length for MOD hashing
            private int count;

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            internal IEqualityComparer<TKey>? Comparer { get; }
            internal int Count => Volatile.Read(ref count);
            internal bool IsAndHash { get; }

            #endregion

            #region Indexers

            internal TValue this[TKey key]
            {
                get => TryGetValue(key, out TValue? value) ? value : Throw.KeyNotFoundException<TValue>(Res.IDictionaryKeyNotFound);
                set
                {
                    if (key == null!)
                        Throw.ArgumentNullException(Argument.key);
                    TryAddInternal(key, value, GetHashCode(key));
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal GrowOnlyDictionary(int capacity, IEqualityComparer<TKey>? comparer, bool isAndHash)
            {
                this.Comparer = comparer;
                IsAndHash = isAndHash;
                buckets = new Bucket[GetBucketSize(capacity)];
            }

            #endregion

            #region Methods

            #region Internal Methods

            internal bool TryAdd(TKey key, TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryAddInternal(key, value, GetHashCode(key));
            }

            internal bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                return TryGetValueInternal(key, GetHashCode(key), out value);
            }

            internal bool TryAddInternal(TKey key, TValue value, uint hashCode)
            {
                ref Bucket bucketRef = ref buckets[GetBucketIndex(hashCode)];

                // we are optimistic
                var newEntry = new Entry(hashCode, key, value);

                // bucket is empty: trying to add the first item
                if (bucketRef.First == null)
                {
                    if (Interlocked.CompareExchange(ref bucketRef.First, newEntry, null) == null)
                    {
                        Interlocked.Increment(ref count);
                        return true;
                    }

                    // Here we have a lost race. Though Bucket is a struct, detecting change works because we stored a ref to the bucket.
                    Debug.Assert(bucketRef.First != null);
                }

                // iterating through the entries until we find key or the end of the list
                IEqualityComparer<TKey> comp = Comparer ?? defaultComparer;
                Entry entry = bucketRef.First!;
                while (true)
                {
                    // key already exists
                    if (entry.Hash == hashCode && comp.Equals(key, entry.Key))
                        return false;

                    // last item in the bucket: trying to chain the new one
                    if (entry.Next == null)
                    {
                        if (Interlocked.CompareExchange(ref entry.Next, newEntry, null) == null)
                        {
                            Interlocked.Increment(ref count);
                            return true;
                        }
                    }

                    entry = entry.Next;
                }
            }

            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal bool TryGetValueInternal(TKey key, uint hashCode, [MaybeNullWhen(false)]out TValue value)
            {
                IEqualityComparer<TKey> comp = Comparer ?? defaultComparer;
                for (Entry? entry = buckets[GetBucketIndex(hashCode)].First; entry != null; entry = entry.Next)
                {
                    if (entry.Hash == hashCode && comp.Equals(key, entry.Key))
                    {
                        value = entry.Value;
                        return true;
                    }
                }

                value = default;
                return false;
            }

            internal TValue GetOrAddInternal(TKey key, Func<TKey, TValue> itemLoader, uint hashCode)
            {
                ref Bucket bucketRef = ref buckets[GetBucketIndex(hashCode)];
                Entry? newEntry = null;

                // bucket is empty: trying to add the first item
                if (bucketRef.First == null)
                {
                    newEntry = new Entry(hashCode, key, itemLoader.Invoke(key));
                    if (Interlocked.CompareExchange(ref bucketRef.First, newEntry, null) == null)
                    {
                        Interlocked.Increment(ref count);
                        return newEntry.Value;
                    }

                    // Here we have a lost race. Though Bucket is a struct, detecting change works because we stored a ref to the bucket.
                    Debug.Assert(bucketRef.First != null);
                }

                // iterating through the entries until we find key or the end of the list
                IEqualityComparer<TKey> comp = Comparer ?? defaultComparer;
                Entry entry = bucketRef.First!;
                while (true)
                {
                    // item found
                    if (entry.Hash == hashCode && comp.Equals(key, entry.Key))
                        return entry.Value;

                    // last item in the bucket: trying to chain the new one
                    if (entry.Next == null)
                    {
                        if (Interlocked.CompareExchange(ref entry.Next, newEntry ??= new Entry(hashCode, key, itemLoader.Invoke(key)), null) == null)
                        {
                            Interlocked.Increment(ref count);
                            return newEntry.Value;
                        }
                    }

                    entry = entry.Next;
                }
            }

            internal TValue GetOrAddInternal(TKey key, TValue value, uint hashCode)
            {
                ref Bucket bucketRef = ref buckets[GetBucketIndex(hashCode)];
                Entry? newEntry = null;

                // bucket is empty: trying to add the first item
                if (bucketRef.First == null)
                {
                    newEntry = new Entry(hashCode, key, value);
                    if (Interlocked.CompareExchange(ref bucketRef.First, newEntry, null) == null)
                    {
                        Interlocked.Increment(ref count);
                        return newEntry.Value;
                    }

                    // Here we have a lost race. Though Bucket is a struct, detecting change works because we stored a ref to the bucket.
                    Debug.Assert(bucketRef.First != null);
                }

                // iterating through the entries until we find key or the end of the list
                IEqualityComparer<TKey> comp = Comparer ?? defaultComparer;
                Entry entry = bucketRef.First!;
                while (true)
                {
                    // item found
                    if (entry.Hash == hashCode && comp.Equals(key, entry.Key))
                        return entry.Value;

                    // last item in the bucket: trying to chain the new one
                    if (entry.Next == null)
                    {
                        if (Interlocked.CompareExchange(ref entry.Next, newEntry ??= new Entry(hashCode, key, value), null) == null)
                        {
                            Interlocked.Increment(ref count);
                            return newEntry.Value;
                        }
                    }

                    entry = entry.Next;
                }
            }

            internal CustomEnumerator GetCustomEnumerator() => new CustomEnumerator(this);

            #endregion

            #region Private Methods

            private uint GetBucketSize(int capacity)
            {
                if (IsAndHash)
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
            private uint GetBucketIndex(uint hashCode) => IsAndHash
                ? hashCode & hashingOperand
                : hashCode % hashingOperand;

            [MethodImpl(MethodImpl.AggressiveInlining)]
            private uint GetHashCode(TKey key)
            {
                IEqualityComparer<TKey>? comp = Comparer;
                return (uint)(comp == null ? key.GetHashCode() : comp.GetHashCode(key));
            }

            #endregion

            #endregion
        }
    }
}
