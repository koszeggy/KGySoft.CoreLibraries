#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GrowOnlyDictionary.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a lock-free thread safe dictionary where items can only be added and queried but not replaced or deleted.
    /// </summary>
    internal class GrowOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        #region Nested types

        #region Entry class

        private sealed class Entry
        {
            #region Fields

            internal uint Hash;
            internal TKey Key;
            internal TValue Value;
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
        private struct Bucket
        {
            #region Fields

            internal volatile Entry? First;

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        private static readonly IEqualityComparer<TKey> defaultComparer = ComparerHelper<TKey>.EqualityComparer;

        #endregion

        #region Instance Fields

        private readonly IEqualityComparer<TKey>? comparer;
        private readonly bool isAndHash;
        private readonly Bucket[] buckets;

        private uint hashingOperand; // buckets.Length - 1 for AND hashing, buckets.Length for MOD hashing
        private int count;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        public int Count => Volatile.Read(ref count);

        #endregion

        #region Indexers

        public TValue this[TKey key]
        {
            get => TryGetValue(key, out TValue value) ? value : Throw.KeyNotFoundException<TValue>(Res.IDictionaryKeyNotFound);
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
            this.comparer = comparer;
            this.isAndHash = isAndHash;
            buckets = new Bucket[GetBucketSize(capacity)];
        }

        #endregion

        #region Methods

        #region Public Methods

        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return TryAddInternal(key, value, GetHashCode(key));
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return TryGetValueInternal(key, GetHashCode(key), out value);
        }

        #endregion

        #region Internal Methods

        internal bool TryAddInternal(TKey key, TValue value, uint hashCode)
        {
            Debug.Assert(key != null!);
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
            }

            // iterating through the entries until we find key or the end of the list
            IEqualityComparer<TKey> comp = comparer ?? defaultComparer;
            Entry entry = bucketRef.First;
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
        internal bool TryGetValueInternal(TKey key, uint hashCode, [MaybeNullWhen(false)] out TValue value)
        {
            Debug.Assert(key != null!);

            IEqualityComparer<TKey> comp = comparer ?? defaultComparer;
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

        #endregion

        #region Private Methods

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

        #endregion

        #endregion
    }
}
