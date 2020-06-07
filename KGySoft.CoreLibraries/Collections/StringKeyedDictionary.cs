#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringKeyedDictionary.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
using System.Runtime.CompilerServices;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a string keyed dictionary that can be queried also by <see cref="StringSegment"/>
    /// and <see cref="ReadOnlySpan{T}"/> (in .NET Core 3.0/.NET Standard 2.1 and above).
    /// Uses a custom dictionary implementation.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    internal sealed class StringKeyedDictionary<TValue>
    {
        #region Nested structs

        #region Enumerator struct

        public struct Enumerator
        {
            #region Fields

            private readonly StringKeyedDictionary<TValue> dictionary;
            private readonly int version;
            private readonly bool isGeneric;

            private int index;
            private KeyValuePair<string, TValue> current;

            #endregion

            #region Properties

            public KeyValuePair<string, TValue> Current => current;

            #endregion

            #region Constructors

            internal Enumerator(StringKeyedDictionary<TValue> dictionary, bool isGeneric)
            {
                this.dictionary = dictionary;
                version = dictionary.version;
                this.isGeneric = isGeneric;
                index = 0;
                current = default;
            }

            #endregion

            #region Methods

            public bool MoveNext()
            {
                if (version != dictionary.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                // Unlike in Cache, index goes from 0 to usedCount so we can use a single uint comparison
                while ((uint)index < (uint)dictionary.usedCount)
                {
                    ref Entry entry = ref dictionary.entries[index];
                    index += 1;

                    // skipping deleted items
                    if (entry.Next < -1)
                        continue;

                    current = new KeyValuePair<string, TValue>(entry.Key, entry.Value);
                    return true;
                }

                index = dictionary.usedCount + 1;
                current = default;
                return false;
            }

            #endregion
        }

        #endregion

        #region Entry struct

        [DebuggerDisplay("[{" + nameof(Key) + "}; {" + nameof(Value) + "}]")]
        private struct Entry
        {
            #region Fields

            internal string Key;
            internal TValue Value;
            internal int Hash;
            /// <summary>
            /// Zero-based index of a chained item in the current bucket or -1 if last.
            /// Deleted items use negative indices below -1. Last deleted item has index -2.
            /// </summary>
            internal int Next;

            #endregion
        }

        #endregion

        #endregion

        #region Constants

        private const int deletedNextBase = -3;

        #endregion

        #region Fields

        #region Static Fields

        private static readonly Type typeValue = typeof(TValue);
        private static readonly bool isValueManaged = !typeValue.IsUnmanaged();

        #endregion

        #region Instance Fields

        private readonly StringSegmentComparer comparer;

        private Entry[] entries;
        private int[] buckets; // 1-based indices for entries. 0 if unused.
        private int mask; // same as bucket.Length - 1 but is cached for better performance
        private int usedCount; // used elements in items including deleted ones
        private int deletedCount;
        private int deletedItemsBucket = -1; // First deleted entry among used elements. -1 if there are no deleted elements.
        private int version;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets number of elements currently stored in this <see cref="StringKeyedDictionary{TValue}"/> instance.
        /// </summary>
        public int Count => usedCount - deletedCount;

        #endregion

        #region Indexers

        public TValue this[string key]
        {
            get
            {
                int index = GetItemIndex(key);
                if (index < 0)
                    Throw.KeyNotFoundException();
                return entries[index].Value;
            }
            set
            {
                if (key == null)
                    Throw.ArgumentNullException(Argument.key);

                Insert(key, value, false);
            }
        }

        public TValue this[StringSegment key]
        {
            get
            {
                int index = GetItemIndex(key);
                if (index < 0)
                    Throw.KeyNotFoundException();
                return entries[index].Value;
            }
        }

        public TValue this[ReadOnlySpan<char> key]
        {
            get
            {
                int index = GetItemIndex(key);
                if (index < 0)
                    Throw.KeyNotFoundException();
                return entries[index].Value;
            }
        }

        #endregion

        #endregion

        #region Constructors

        public StringKeyedDictionary() : this(0)
        {
        }

        public StringKeyedDictionary(StringSegmentComparer comparer) : this(0, comparer)
        {
        }

        public StringKeyedDictionary(int capacity, StringSegmentComparer comparer = null)
        {
            this.comparer = comparer;
            if (capacity > 0)
                Initialize(capacity);
        }

        #endregion

        #region Methods

        #region Static Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static bool IsValueManaged() =>
#if !(NETFRAMEWORK || NETSTANDARD2_0)
            // not "caching" the result of this call because that would make the JIT-ed code slower
            RuntimeHelpers.IsReferenceOrContainsReferences<TValue>();
#else
        isValueManaged;
#endif


        private const int minCapacity = 4;

        #endregion

        #region Instance Methods

        #region Public Methods

        public void Add(string key, TValue value)
        {
            if (key == null)
                Throw.ArgumentNullException(Argument.key);

            Insert(key, value, true);
        }

        public bool Remove(string key)
        {
            if (key == null)
                Throw.ArgumentNullException(Argument.key);

            return InternalRemove(key, default, false);
        }

        public bool TryGetValue(string key, out TValue value)
        {
            int i = GetItemIndex(key);
            if (i >= 0)
            {
                value = entries[i].Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetValue(StringSegment key, out TValue value)
        {
            int i = GetItemIndex(key);
            if (i >= 0)
            {
                value = entries[i].Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetValue(ReadOnlySpan<char> key, out TValue value)
        {
            int i = GetItemIndex(key);
            if (i >= 0)
            {
                value = entries[i].Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool ContainsKey(string key) => GetItemIndex(key) >= 0;

        public bool ContainsKey(StringSegment key) => GetItemIndex(key) >= 0;

        public bool ContainsKey(ReadOnlySpan<char> key) => GetItemIndex(key) >= 0;

        public Enumerator GetEnumerator()
            => new Enumerator(this, true);

        #endregion

        #region Internal Methods

        internal bool TryGetValue(MutableStringSegment key, out TValue value)
        {
            int i = GetItemIndex(key);
            if (i >= 0)
            {
                value = entries[i].Value;
                return true;
            }

            value = default;
            return false;
        }

        #endregion

        #region Private Methods

        private void Initialize(int capacity)
        {
            int bucketSize = Math.Max(minCapacity, capacity).GetNextPowerOfTwo();
            mask = bucketSize - 1;
            deletedItemsBucket = -1;
            buckets = new int[bucketSize];
            entries = new Entry[Math.Max(minCapacity, capacity)];
        }

        private int GetItemIndex(string key)
        {
            if (key == null)
                Throw.ArgumentNullException(Argument.key);

            if (buckets == null)
                return -1;

            if (comparer == null)
            {
                int hashCode = StringSegmentComparer.GetHashCodeOrdinal(key);
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && entries[i].Key == key)
                        return i;
                }
            }
            else
            {
                int hashCode = comparer.GetHashCode(key);
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && comparer.Equals(entries[i].Key, key))
                        return i;
                }
            }

            return -1;
        }

        private int GetItemIndex(StringSegment key)
        {
            if (key.IsNull)
                Throw.ArgumentNullException(Argument.key);

            if (buckets == null)
                return -1;

            if (comparer == null)
            {
                int hashCode = key.GetHashCode();
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && key.Equals(entries[i].Key))
                        return i;
                }
            }
            else
            {
                int hashCode = comparer.GetHashCode(key);
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && comparer.Equals(key, entries[i].Key))
                        return i;
                }
            }

            return -1;
        }

        private int GetItemIndex(MutableStringSegment key)
        {
            Debug.Assert(key.Length != 0);
            if (buckets == null)
                return -1;

            if (comparer == null)
            {
                int hashCode = key.GetHashCode();
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && key.Equals(entries[i].Key))
                        return i;
                }
            }
            else
            {
                int hashCode = comparer.GetHashCode(key);
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && comparer.Equals(key, entries[i].Key))
                        return i;
                }
            }

            return -1;
        }

        private int GetItemIndex(ReadOnlySpan<char> key)
        {
            if (buckets == null)
                return -1;

            if (comparer == null)
            {
                int hashCode = String.GetHashCode(key);
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && key.Equals(entries[i].Key))
                        return i;
                }
            }
            else
            {
                int hashCode = comparer.GetHashCode(key);
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && comparer.Equals(key, entries[i].Key))
                        return i;
                }
            }

            return -1;
        }

        private bool InternalRemove(string key, TValue value, bool checkValue)
        {
            if (buckets == null)
                return false;

            int hashCode = comparer?.GetHashCode(key) ?? StringSegmentComparer.GetHashCodeOrdinal(key);
            int bucket = hashCode & mask;
            int previous = -1;
            for (int i = buckets[bucket] - 1; i >= 0; previous = i, i = entries[i].Next)
            {
                ref Entry entry = ref entries[i];
                if (entry.Hash != hashCode || (!comparer?.Equals(entry.Key, key) ?? key != entry.Key))
                    continue;

                if (checkValue && !ComparerHelper<TValue>.EqualityComparer.Equals(entries[i].Value, value))
                    return false;

                // removing entry from the original bucket
                if (previous < 0)
                    buckets[bucket] = entry.Next + 1;
                else
                    entries[previous].Next = entry.Next;

                // Moving entry to a special bucket of removed entries were indices have negative value less than -1
                entry.Next = deletedNextBase - deletedItemsBucket;
                deletedItemsBucket = i;
                deletedCount += 1;

                // cleanup
                entry.Key = null;
                if (IsValueManaged())
                    entry.Value = default;

                version += 1;
                return true;
            }

            return false;
        }

        private void Insert(string key, TValue value, bool throwIfExists)
        {
            if (buckets == null)
                Initialize(0);

            int hashCode = comparer?.GetHashCode(key) ?? StringSegmentComparer.GetHashCodeOrdinal(key);
            ref int bucketRef = ref buckets[hashCode & mask];
            if (comparer == null)
            {
                // searching for an existing key
                for (int i = bucketRef - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash != hashCode || entries[i].Key == key)
                        continue;

                    if (throwIfExists)
                        Throw.ArgumentException(Argument.key, Res.IDictionaryDuplicateKey);

                    // overwriting existing element
                    entries[i].Value = value;
                    version += 1;
                    return;
                }
            }
            else
            {
                // searching for an existing key
                for (int i = bucketRef - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash != hashCode || !comparer.Equals(entries[i].Key, key))
                        continue;

                    if (throwIfExists)
                        Throw.ArgumentException(Argument.key, Res.IDictionaryDuplicateKey);

                    // overwriting existing element
                    entries[i].Value = value;
                    version += 1;
                    return;
                }
            }

            int index;
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
                if (usedCount == entries.Length)
                {
                    Resize(entries.Length << 1);
                    bucketRef = ref buckets[hashCode & mask];
                }

                index = usedCount;
                usedCount += 1;
            }

            ref Entry entryRef = ref entries[index];
            if (fromDeleted)
                deletedItemsBucket = deletedNextBase - entryRef.Next;
            entryRef.Hash = hashCode;
            entryRef.Next = bucketRef - 1; // Next is zero-based
            entryRef.Key = key;
            entryRef.Value = value;
            bucketRef = index + 1; // bucket indices are 1-based
            version += 1;
        }

        private void Resize(int newCapacity)
        {
            int newBucketSize = newCapacity.GetNextPowerOfTwo();
            var newBuckets = new int[newBucketSize];
            var newEntries = new Entry[newBucketSize];
            mask = newBucketSize - 1;
            Array.Copy(entries, 0, newEntries, 0, usedCount);

            // re-applying buckets for the new size
            for (int i = 0; i < usedCount; i++)
            {
                int bucket = newEntries[i].Hash & mask;
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
