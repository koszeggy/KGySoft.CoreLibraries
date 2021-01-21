#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FixedSizeDictionary.cs
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
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a fixed size thread safe dictionary that supports updating values in a thread-safe manner,
    /// though adding new values is not supported. Removing and re-setting keys are supported though, which is also thread-safe.
    /// </summary>
    internal sealed class FixedSizeDictionary<TKey, TValue>
        where TKey : notnull
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
            /// A reference to the actual value and its living status. As a reference, it can be used as locking object for atomic
            /// modifications. The reference is replaced only on delete/restore. Volatile, because reading is lock-free.
            /// Actually a Volatile.Read would be enough on reading (writing is always performed in a lock) but that is not available in .NET 3.5.
            /// </summary>
#if NET35 || NET40
            volatile // because there is no generic Volatile.Read in .NET 3.5/4.0
#endif
            internal StrongBox<ValueHolder> Value;

            /// <summary>
            /// Zero-based index of a chained item in the current bucket or -1 if last.
            /// In this collection this field is practically read-only. Deleted items are indicated by !ValueHolder.IsLiving.
            /// </summary>
            internal int Next;

            #endregion

            #region Properties

            private object? DebugValue => !Value.Value.IsLiving ? "<Deleted>" : (object?)Value.Value.Value;

            #endregion
        }

        #endregion

        #region ValueHolder struct

        private struct ValueHolder
        {
            #region Fields

            #region Static Fields
            
            internal static readonly ValueHolder Deleted = new ValueHolder();

            #endregion

            #region Instance Fields
            
            internal readonly bool IsLiving;

            internal TValue Value;

            #endregion

            #endregion

            #region Constructors
            
            internal ValueHolder(TValue value)
            {
                IsLiving = true;
                Value = value;
            }

            #endregion
        }

        #endregion

        #region CustomEnumerator struct

        internal struct Enumerator
        {
            #region Fields

            #region Internal Fields

            public KeyValuePair<TKey, TValue> Current { get; private set; }

            #endregion

            #region Private Fields

            private readonly Entry[] entries;

            private int pos;

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(FixedSizeDictionary<TKey, TValue> owner)
            {
                entries = owner.entries;
                pos = 0;
                Current = default;
            }

            #endregion

            #region Methods

            public bool MoveNext()
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
                    if (!box.Value.IsLiving)
                        continue;

                    Current = new KeyValuePair<TKey, TValue>(entryRef.Key, box.Value.Value);
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

        internal static readonly FixedSizeDictionary<TKey, TValue> Empty = new FixedSizeDictionary<TKey, TValue>();

        #endregion

        #region Private Fields

        private static readonly IEqualityComparer<TKey> defaultComparer = ComparerHelper<TKey>.EqualityComparer;
        private static readonly IEqualityComparer<TValue> valueComparer = ComparerHelper<TValue>.EqualityComparer;

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

        public int Count => entries.Length -
#if NET35 || NET40
            Thread.VolatileRead(ref deletedCount);
#else
            Volatile.Read(ref deletedCount);
#endif

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

        #region Internal Constructors

        internal FixedSizeDictionary(bool isAndHash, IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer = null)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            this.isAndHash = isAndHash;
            this.comparer = comparer;
            Initialize(dictionary);
        }

        internal FixedSizeDictionary(FixedSizeDictionary<TKey, TValue> other, CustomDictionary<TKey, TValue> mergeWith)
        {
            isAndHash = mergeWith.IsAndHash;
            comparer = mergeWith.Comparer;
            Initialize(other, mergeWith);
        }

        #endregion

        #region Private Constructors

        private FixedSizeDictionary()
        {
            Debug.Assert(typeof(TValue).SizeOf() <= IntPtr.Size, $"TValue = {typeof(TValue).GetName(TypeNameKind.ShortName)} should not be used because it cannot be written atomically.");

            // this ctor is for the Empty instance
            buckets = new int[1];
            isAndHash = true;
            entries = Reflector.EmptyArray<Entry>();
        }

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            return TryGetValueInternal(key, GetHashCode(key), out value) == true;
            //ref TValue resultRef = ref TryGetValueInternal(key, GetHashCode(key), out var _);
            //if (!Unsafe.IsNullRef(ref resultRef))
            //{
            //    value = resultRef;
            //    return true;
            //}

            //value = default;
            //return false;
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

        public bool TryRemove(TKey key, out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return TryRemoveInternal(key, GetHashCode(key), out value) == true;
            //ref TValue valueRef = ref TryRemoveInternal(key, GetHashCode(key), out var _);
            //if (!Unsafe.IsNullRef(ref valueRef))
            //{
            //    value = valueRef;
            //    return true;
            //}

            //value = default;
            //return false;
        }

        public bool Remove(TKey key)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return TryRemoveInternal(key, GetHashCode(key)) == true;
            //ref TValue valueRef = ref TryRemoveInternal(key, GetHashCode(key), out var _);
            //return !Unsafe.IsNullRef(ref valueRef);
        }

        public void Clear()
        {
            int count = entries.Length;
            for (int i = 0; i < count; i++)
            {
                while (true)
                {
                    StrongBox<ValueHolder> box = entries[i].Value;
                    lock (box)
                    {
                        if (box != entries[i].Value)
                            continue;

                        if (!box.Value.IsLiving)
                            break;

                        entries[i].Value = new StrongBox<ValueHolder>(ValueHolder.Deleted);
                        deletedCount += 1;
                        break;
                    }
                }
            }
        }

        #endregion

        #region Internal Methods

        //[MethodImpl(MethodImpl.AggressiveInlining)]
        //internal ref TValue TryGetValueInternal(TKey key, uint hash, out bool deleted)
        //{
        //    int[] bucketsLocal = buckets;
        //    Entry[] items = entries;
        //    IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

        //    int i = bucketsLocal[GetBucketIndex(hash)] - 1;
        //    while (i >= 0)
        //    {
        //        ref Entry entryRef = ref items[i];
        //        if (entryRef.Hash != hash || !comp.Equals(entryRef.Key, key))
        //        {
        //            i = entryRef.Next;
        //            continue;
        //        }

        //        // key found: lock-free reading
        //        StrongBox<TValue>? box = Volatile.Read(ref entryRef.Value.Value);

        //        // deleted
        //        if (box == null)
        //        {
        //            deleted = true;
        //            return ref Unsafe.NullRef<TValue>();
        //        }

        //        // exists
        //        deleted = false;
        //        return ref box.Value;
        //    }

        //    // not found
        //    deleted = false;
        //    return ref Unsafe.NullRef<TValue>();
        //}

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal bool? TryGetValueInternal(TKey key, uint hash, [MaybeNull] out TValue value)
        {
            int[] bucketsLocal = buckets;
            Entry[] items = entries;
            IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

            int i = bucketsLocal[GetBucketIndex(hash)] - 1;
            while (i >= 0)
            {
                ref Entry entryRef = ref items[i];
                if (entryRef.Hash != hash || !comp.Equals(entryRef.Key, key))
                {
                    i = entryRef.Next;
                    continue;
                }

                // key found: lock-free reading.
#if NET35 || NET40
                var box = entryRef.Value;
#else
                var box = Volatile.Read(ref entryRef.Value);
#endif

                // If IsLiving is true, it will not change so Value can be read safely
                if (box.Value.IsLiving)
                {
                    // works without locking because the private constructor ensures that TValue can be copied atomically
                    value = box.Value.Value;
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
        /// Tries to insert a value. Returns null if key not found. Succeeds only if a key was deleted previously.
        /// </summary>
        internal bool? TryInsertInternal(TKey key, TValue value, uint hash, DictionaryInsertion behavior)
        {
            int[] bucketsLocal = buckets;
            Entry[] items = entries;
            IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

            int i = bucketsLocal[GetBucketIndex(hash)] - 1;
            while (i >= 0)
            {
                ref Entry entryRef = ref items[i];
                if (entryRef.Hash != hash || !comp.Equals(entryRef.Key, key))
                {
                    i = entryRef.Next;
                    continue;
                }

                while (true)
                {
                    StrongBox<ValueHolder> box = entryRef.Value;
                    lock (box)
                    {
                        // lost race (item has been deleted/restored)
                        if (box != entryRef.Value)
                            continue;

                        // entry was deleted
                        if (!box.Value.IsLiving)
                        {
                            entryRef.Value = new StrongBox<ValueHolder>(new ValueHolder(value));
                            deletedCount -= 1;
                            return true;
                        }

                        // the value can be overridden
                        if (behavior == DictionaryInsertion.OverwriteIfExists)
                        {
                            // due to the private ctor this operation is atomic
                            box.Value.Value = value;
                            return true;
                        }
                    }

                    // exists
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
        internal bool? TryReplaceInternal(TKey key, TValue newValue, TValue originalValue, uint hash)
        {
            int[] bucketsLocal = buckets;
            Entry[] items = entries;
            IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

            int i = bucketsLocal[GetBucketIndex(hash)] - 1;
            while (i >= 0)
            {
                ref Entry entryRef = ref items[i];
                if (entryRef.Hash != hash || !comp.Equals(entryRef.Key, key))
                {
                    i = entryRef.Next;
                    continue;
                }

                while (true)
                {
                    StrongBox<ValueHolder> box = entryRef.Value;
                    lock (box)
                    {
                        // lost race (item has been deleted/restored)
                        if (box != entryRef.Value)
                            continue;

                        // deleted or original value does not match
                        if (!box.Value.IsLiving || !valueComparer.Equals(box.Value.Value, originalValue))
                            return false;

                        box.Value.Value = newValue;
                        return true;
                    }
                }
            }

            // not found
            return null;
        }

        //internal ref TValue TryRemoveInternal(TKey key, uint hash, StrongBox<bool> alreadyDeleted)
        //{
        //    Debug.Assert(!alreadyDeleted.Value);

        //    int[] bucketsLocal = buckets;
        //    Entry[] items = entries;
        //    IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

        //    int i = bucketsLocal[GetBucketIndex(hash)] - 1;
        //    while (i >= 0)
        //    {
        //        ref Entry entryRef = ref items[i];
        //        if (entryRef.Hash != hash || !comp.Equals(entryRef.Key, key))
        //        {
        //            i = entryRef.Next;
        //            continue;
        //        }

        //        var box = entryRef.Value;
        //        lock (box)
        //        {
        //            // already deleted
        //            if (box.Value.Deleted)
        //            {
        //                alreadyDeleted.Value = true;
        //                return ref Unsafe.NullRef<TValue>();
        //            }

        //            deletedCount += 1;
        //            return ref box.Value.Value;
        //        }
        //    }

        //    // not found
        //    return ref Unsafe.NullRef<TValue>();
        //}

        internal bool? TryRemoveInternal(TKey key, uint hash, [MaybeNull]out TValue value)
        {
            int[] bucketsLocal = buckets;
            Entry[] items = entries;
            IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

            int i = bucketsLocal[GetBucketIndex(hash)] - 1;
            while (i >= 0)
            {
                ref Entry entryRef = ref items[i];
                if (entryRef.Hash != hash || !comp.Equals(entryRef.Key, key))
                {
                    i = entryRef.Next;
                    continue;
                }

                while (true)
                {
                    StrongBox<ValueHolder> box = entryRef.Value;
                    lock (box)
                    {
                        // lost race (item has been deleted/restored)
                        if (box != entryRef.Value)
                            continue;

                        // already deleted
                        if (!box.Value.IsLiving)
                        {
                            value = default;
                            return false;
                        }

                        // exists, delete
                        entryRef.Value = new StrongBox<ValueHolder>(ValueHolder.Deleted);
                        value = box.Value.Value;
                        deletedCount += 1;
                        return true;
                    }
                }
            }

            // not found
            value = default;
            return null;
        }

        internal bool? TryRemoveInternal(TKey key, uint hash)
        {
            int[] bucketsLocal = buckets;
            Entry[] items = entries;
            IEqualityComparer<TKey> comp = comparer ?? defaultComparer;

            int i = bucketsLocal[GetBucketIndex(hash)] - 1;
            while (i >= 0)
            {
                ref Entry entryRef = ref items[i];
                if (entryRef.Hash != hash || !comp.Equals(entryRef.Key, key))
                {
                    i = entryRef.Next;
                    continue;
                }

                while (true)
                {
                    StrongBox<ValueHolder> box = entryRef.Value;
                    lock (box)
                    {
                        // lost race (item has been deleted/restored)
                        if (box != entryRef.Value)
                            continue;

                        // already deleted
                        if (!box.Value.IsLiving)
                            return false;

                        // exists, delete
                        entryRef.Value = new StrongBox<ValueHolder>(ValueHolder.Deleted);
                        deletedCount += 1;
                        return true;
                    }
                }
            }

            // not found
            return null;
        }

        internal Enumerator GetEnumerator() => new Enumerator(this);

        #endregion

        #region Private Methods

        private void Initialize(IDictionary<TKey, TValue> dictionary)
        {
            int count = dictionary.Count;
            buckets = new int[GetBucketSize(count)];
            entries = new Entry[count];

            if (comparer == null)
                PopulateDefault(dictionary);
            else
                PopulateByComparer(dictionary);
        }

        private void Initialize(FixedSizeDictionary<TKey, TValue> other, CustomDictionary<TKey, TValue> mergeWith)
        {
            int otherCount = other.entries.Length;
            int count = otherCount + mergeWith.Count;
            buckets = new int[GetBucketSize(count)];
            entries = new Entry[count];
            CopyFrom(other);
            CopyFrom(mergeWith, otherCount);
        }

        private void PopulateDefault(IDictionary<TKey, TValue> dictionary)
        {
            int index = 0;
            int[] localBuckets = buckets;
            Entry[] items = entries;

            foreach (KeyValuePair<TKey, TValue> item in dictionary)
            {
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
                itemRef.Value = new StrongBox<ValueHolder>(new ValueHolder(item.Value));
                bucketRef = ++index; // bucket indices are 1-based
            }
        }

        private void PopulateByComparer(IDictionary<TKey, TValue> dictionary)
        {
            int index = 0;
            IEqualityComparer<TKey> customComparer = comparer!;
            int[] localBuckets = buckets;
            Entry[] items = entries;

            foreach (KeyValuePair<TKey, TValue> item in dictionary)
            {
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
                itemRef.Value = new StrongBox<ValueHolder>(new ValueHolder(item.Value));
                bucketRef = ++index; // bucket indices are 1-based
            }
        }

        private void CopyFrom(FixedSizeDictionary<TKey, TValue> other)
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
                newItemRef.Value = new StrongBox<ValueHolder>(oldItemRef.Value.Value);
#else
                newItemRef.Value = new StrongBox<ValueHolder>(Volatile.Read(ref oldItemRef.Value).Value);
#endif

                // assuming other was already consistent so not checking for duplicate keys
                ref int bucketRef = ref localBuckets[GetBucketIndex(newItemRef.Hash)];
                newItemRef.Next = bucketRef - 1; // Next is zero-based
                bucketRef = ++index; // bucket indices are 1-based

                // as the inner box is a copy deletedCount will not be invalid even if the original instance is modified
                if (!newItemRef.Value.Value.IsLiving)
                    deletedCount += 1;
            }
        }

        private void CopyFrom(CustomDictionary<TKey, TValue> other, int index)
        {
            int[] localBuckets = buckets;
            Entry[] items = entries;

            CustomDictionary<TKey, TValue>.CustomEnumerator enumerator = other.GetCustomEnumerator();
            while (enumerator.MoveNext())
            {
                // assuming other was already consistent so not checking for duplicate keys
                ref int bucketRef = ref localBuckets[GetBucketIndex(enumerator.Current.Hash)];
                ref Entry itemRef = ref items[index];
                itemRef.Hash = enumerator.Current.Hash;
                itemRef.Next = bucketRef - 1; // Next is zero-based
                itemRef.Key = enumerator.Current.Key;
                itemRef.Value = new StrongBox<ValueHolder>(new ValueHolder(enumerator.Current.Value));
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

        #endregion

        #endregion
    }
}
