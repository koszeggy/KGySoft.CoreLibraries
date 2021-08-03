#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockFreeCache.ReadOnlyDictionary.cs
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

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    partial class LockFreeCache<TKey, TValue>
    {
        [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
        protected sealed class ReadOnlyDictionary
        {
            #region Nested structs

            #region Entry struct

            [DebuggerDisplay("[{" + nameof(Key) + "}; {" + nameof(Value) + "}]")]
            private struct Entry
            {
                #region Fields

                internal uint Hash;
                internal TKey Key;
                internal TValue Value;

                /// <summary>
                /// Zero-based index of a chained item in the current bucket or -1 if last.
                /// In this collection there are no deleted items.
                /// </summary>
                internal int Next;

                #endregion
            }

            #endregion

            #endregion

            #region Fields

            #region Static Fields

            #region Internal Fields

            internal static readonly ReadOnlyDictionary Empty = new ReadOnlyDictionary();

            #endregion

            #region Private Fields

            private static readonly IEqualityComparer<TKey> defaultComparer = ComparerHelper<TKey>.EqualityComparer;

            #endregion

            #endregion

            #region Instance Fields

            private readonly IEqualityComparer<TKey>? comparer;
            private readonly Entry[] entries;
            private readonly int[] buckets; // 1-based indices for entries. 0 if unused.
            private readonly uint hashingOperand; // buckets.Length - 1 for AND hashing, buckets.Length for MOD hashing
            private readonly bool isAndHash;

            #endregion

            #endregion

            #region Properties

            internal int Count => entries.Length;

            #endregion

            #region Constructors

            #region Internal Constructors

            internal ReadOnlyDictionary(int maxCapacity, GrowOnlyDictionary primaryValues, ReadOnlyDictionary additionalValues)
                : this(primaryValues.IsAndHash, primaryValues.Comparer, Math.Min(maxCapacity, primaryValues.Count + additionalValues.Count))
            {
                int[] localBuckets = buckets;
                Entry[] items = entries;
                int capacity = items.Length;
                int index = 0;

                // Taking primary values first. This initialization works even if growing dictionary grows during the initialization
                GrowOnlyDictionary.CustomEnumerator enumerator = primaryValues.GetCustomEnumerator();
                while (index < capacity && enumerator.MoveNext())
                {
                    // using the same comparer and assuming other was already consistent so not checking for duplicate keys
                    ref int bucketRef = ref localBuckets[GetBucketIndex(enumerator.Current.Hash)];
                    ref Entry itemRef = ref items[index];
                    itemRef.Hash = enumerator.Current.Hash;
                    itemRef.Next = bucketRef - 1; // Next is zero-based
                    itemRef.Key = enumerator.Current.Key;
                    itemRef.Value = enumerator.Current.Value;
                    bucketRef = ++index; // bucket indices are 1-based
                }

                if (index == capacity)
                    return;

                // Taking as many additional values as allowed by remaining capacity
                Entry[] otherEntries = additionalValues.entries;
                int count = capacity - index;
                for (int i = 0; i < count; i++)
                {
                    ref Entry itemRef = ref items[index];
                    itemRef = otherEntries[i];
                    ref int bucketRef = ref localBuckets[GetBucketIndex(itemRef.Hash)];
                    itemRef.Next = bucketRef - 1;
                    bucketRef = ++index;
                }

                Debug.Assert(index == capacity);
            }

            #endregion

            #region Private Constructors

            private ReadOnlyDictionary()
            {
                // this ctor is for the Empty instance
                buckets = new int[1];
                isAndHash = true;
                entries = Reflector.EmptyArray<Entry>();
            }

            private ReadOnlyDictionary(bool isAndHash, IEqualityComparer<TKey>? comparer, int capacity)
            {
                this.isAndHash = isAndHash;
                this.comparer = comparer;

                uint bucketSize;
                if (isAndHash)
                {
                    bucketSize = (uint)HashHelper.GetPowerOfTwo(Math.Max(2, capacity));
                    hashingOperand = bucketSize - 1;
                }
                else
                    hashingOperand = bucketSize = (uint)HashHelper.GetPrime(capacity);

                buckets = new int[bucketSize];
                entries = new Entry[capacity];
            }

            #endregion

            #endregion

            #region Methods

            #region Internal Methods

            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal bool TryGetValueInternal(TKey key, uint hashCode, [MaybeNullWhen(false)] out TValue value)
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

            #endregion

            #region Private Methods

            /// <summary>
            /// An if in a non-virtual method is still faster than calling an abstract method, a delegate or even a C# 9 function pointer
            /// </summary>
            [MethodImpl(MethodImpl.AggressiveInlining)]
            private uint GetBucketIndex(uint hashCode) => isAndHash
                ? hashCode & hashingOperand
                : hashCode % hashingOperand;

            #endregion

            #endregion
        }
    }
}
