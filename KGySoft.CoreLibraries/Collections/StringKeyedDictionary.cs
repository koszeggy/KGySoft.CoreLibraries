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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a string keyed dictionary that can be queried also by <see cref="StringSegment"/>
    /// and <see cref="ReadOnlySpan{T}"/> (in .NET Core 3.0/.NET Standard 2.1 and above) instances.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    ///  TODO: docs
    /// - Special implementation with custom hashing (does not reorganize keys on too many collisions)
    /// - Non-generic IDictionary.Contains and indexer also allows StringSegment
    /// - Value type enumerator, when foreach-ed directly, but reference enumerator when used in LINQ to avoid boxing
    public sealed class StringKeyedDictionary<TValue> : IStringKeyedDictionary<TValue>, IDictionary
#if !(NET35 || NET40)
        , IStringKeyedReadOnlyDictionary<TValue>
#endif
    {
        #region Nested Types
        
        #region Nested Classes

        [Serializable]
        private sealed class ReferenceEnumerator : IEnumerator<KeyValuePair<string, TValue>>, IDictionaryEnumerator
        {
            #region Fields

            private readonly StringKeyedDictionary<TValue> dictionary;
            private readonly int version;
            private readonly bool isGeneric;

            private int index;
            private KeyValuePair<string, TValue> current;

            #endregion

            #region Properties

            #region Public Properties

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public KeyValuePair<string, TValue> Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == dictionary.usedCount + 1)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return isGeneric ? (object)current : new DictionaryEntry(current.Key, current.Value);
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index == 0 || index == dictionary.usedCount + 1)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index == 0 || index == dictionary.usedCount + 1)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current.Key;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (index == 0 || index == dictionary.usedCount + 1)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current.Value;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal ReferenceEnumerator(StringKeyedDictionary<TValue> dictionary, bool isGeneric)
            {
                this.dictionary = dictionary;
                version = dictionary.version;
                this.isGeneric = isGeneric;
                index = 0;
                current = default;
            }

            #endregion

            #region Methods

            #region Public Methods

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/>&#160;if the enumerator was successfully advanced to the next element; <see langword="false"/>&#160;if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
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

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                if (version != dictionary.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = 0;
                current = default;
            }

            #endregion

            #region Explicitly Implemented Interface Properties

            void IDisposable.Dispose() { }

            #endregion

            #endregion
        }

        #region KeysCollection class

        [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
        [Serializable]
        private sealed class KeysCollection : ICollection<string>, ICollection
        {
            #region Fields

            private readonly StringKeyedDictionary<TValue> owner;
            [NonSerialized] private object syncRoot;

            #endregion

            #region Properties

            #region Public Properties

            public int Count => owner.Count;

            public bool IsReadOnly => true;

            #endregion

            #region Explicitly Implemented Interface Properties

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot
            {
                get
                {
                    if (syncRoot == null)
                        Interlocked.CompareExchange(ref syncRoot, new object(), null);
                    return syncRoot;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal KeysCollection(StringKeyedDictionary<TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            #region Public Methods

            public bool Contains(string item)
            {
                if (item == null)
                    Throw.ArgumentNullException(Argument.item);
                return owner.ContainsKey(item);
            }

            [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, array CAN be null so it must be checked")]
            [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, array CAN be null so the Throw is reachable")]
            public void CopyTo(string[] array, int arrayIndex)
            {
                if (array == null)
                    Throw.ArgumentNullException(Argument.array);
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
                if (array.Length - arrayIndex < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

                int len = owner.usedCount;
                Entry[] entries = owner.entries;
                for (int i = 0; i < len; i++)
                {
                    if (entries[i].Next >= -1)
                        array[arrayIndex++] = entries[i].Key;
                }
            }

            public IEnumerator<string> GetEnumerator()
            {
                Entry[] entries = owner.entries;
                if (entries == null)
                    yield break;

                int version = owner.version;
                int len = owner.usedCount;
                for (int i = 0; i < len; i++)
                {
                    if (version != owner.version)
                        Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);
                    if (entries[i].Next >= -1)
                        yield return entries[i].Key;
                }
            }

            #endregion

            #region Explicitly Implemented Interface Methods

            void ICollection<string>.Add(string item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            void ICollection<string>.Clear() => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            bool ICollection<string>.Remove(string item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, array CAN be null so it must be checked")]
            [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, array CAN be null so the Throw is reachable")]
            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    Throw.ArgumentNullException(Argument.array);

                if (array is string[] keys)
                {
                    CopyTo(keys, index);
                    return;
                }

                if (index < 0 || index > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.index);
                if (array.Length - index < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                if (array.Rank != 1)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

                if (array is object[] objectArray)
                {
                    int len = owner.usedCount;
                    Entry[] entries = owner.entries;
                    for (int i = 0; i < len; i++)
                    {
                        if (entries[i].Next >= -1)
                            objectArray[index++] = entries[i].Key;
                    }
                }

                Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
            }

            #endregion

            #endregion
        }

        #endregion

        #region KeysCollection class

        [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}; TValue = {typeof(" + nameof(TValue) + ")}")]
        [Serializable]
        private sealed class ValuesCollection : ICollection<TValue>, ICollection
        {
            #region Fields

            private readonly StringKeyedDictionary<TValue> owner;
            [NonSerialized] private object syncRoot;

            #endregion

            #region Properties

            #region Public Properties

            public int Count => owner.Count;

            public bool IsReadOnly => true;

            #endregion

            #region Explicitly Implemented Interface Properties

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot
            {
                get
                {
                    if (syncRoot == null)
                        Interlocked.CompareExchange(ref syncRoot, new object(), null);
                    return syncRoot;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal ValuesCollection(StringKeyedDictionary<TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            #region Public Methods

            public bool Contains(TValue item) => owner.ContainsValue(item);

            [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, array CAN be null so it must be checked")]
            [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, array CAN be null so the Throw is reachable")]
            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null)
                    Throw.ArgumentNullException(Argument.array);
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
                if (array.Length - arrayIndex < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

                int len = owner.usedCount;
                Entry[] entries = owner.entries;
                for (int i = 0; i < len; i++)
                {
                    if (entries[i].Next >= -1)
                        array[arrayIndex++] = entries[i].Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                Entry[] entries = owner.entries;
                if (entries == null)
                    yield break;

                int version = owner.version;
                int len = owner.usedCount;
                for (int i = 0; i < len; i++)
                {
                    if (version != owner.version)
                        Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);
                    if (entries[i].Next >= -1)
                        yield return entries[i].Value;
                }
            }

            #endregion

            #region Explicitly Implemented Interface Methods

            void ICollection<TValue>.Add(TValue item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            void ICollection<TValue>.Clear() => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            bool ICollection<TValue>.Remove(TValue item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, array CAN be null so it must be checked")]
            [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, array CAN be null so the Throw is reachable")]
            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    Throw.ArgumentNullException(Argument.array);

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                    return;
                }

                if (index < 0 || index > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.index);
                if (array.Length - index < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                if (array.Rank != 1)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

                if (array is object[] objectArray)
                {
                    int len = owner.usedCount;
                    Entry[] entries = owner.entries;
                    for (int i = 0; i < len; i++)
                    {
                        if (entries[i].Next >= -1)
                            objectArray[index++] = entries[i].Value;
                    }
                }

                Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Nested structs

        #region Enumerator struct

        /// <summary>
        /// Enumerates the elements of a <see cref="StringKeyedDictionary{TValue}"/>.
        /// </summary>
        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>
        {
            #region Fields

            private readonly StringKeyedDictionary<TValue> dictionary;
            private readonly int version;

            private int index;
            private KeyValuePair<string, TValue> current;

            #endregion

            #region Properties

            #region Public Properties

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public KeyValuePair<string, TValue> Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == dictionary.usedCount + 1)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(StringKeyedDictionary<TValue> dictionary)
            {
                this.dictionary = dictionary;
                version = dictionary.version;
                index = 0;
                current = default;
            }

            #endregion

            #region Methods

            #region Public Methods

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/>&#160;if the enumerator was successfully advanced to the next element; <see langword="false"/>&#160;if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
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

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                if (version != dictionary.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = 0;
                current = default;
            }

            #endregion

            #region Explicitly Implemented Interface Properties

            void IDisposable.Dispose() { }

            #endregion

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

        #endregion

        #region Constants

        private const int deletedNextBase = -3;

        #endregion

        #region Fields

        #region Static Fields

        private static readonly Type typeValue = typeof(TValue);
#if (NETFRAMEWORK || NETSTANDARD2_0)
        private static readonly bool isValueManaged = !typeValue.IsUnmanaged();
#endif

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

        private object syncRoot;
        private KeysCollection keysCollection;
        private ValuesCollection valuesCollection;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties
        
        /// <summary>
        /// Gets number of elements currently stored in this <see cref="StringKeyedDictionary{TValue}"/> instance.
        /// </summary>
        public int Count => usedCount - deletedCount;

        /// <summary>
        /// Gets the keys stored in the dictionary.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the keys in the original <see cref="StringKeyedDictionary{TValue}"/>.
        /// Therefore, changes to the <see cref="StringKeyedDictionary{TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<string> Keys => keysCollection ??= new KeysCollection(this);

        /// <summary>
        /// Gets the values stored in the dictionary.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the values in the original <see cref="StringKeyedDictionary{TValue}"/>.
        /// Therefore, changes to the <see cref="StringKeyedDictionary{TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TValue> Values => valuesCollection ??= new ValuesCollection(this);

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly => false;

        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;

        ICollection IDictionary.Keys => (ICollection)Keys;
        ICollection IDictionary.Values => (ICollection)Values;
        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

#if !(NET35 || NET40)

        IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values => Values;
#endif

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers
        
        /// <summary>
        /// Gets or sets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// The element with the specified <paramref name="key"/>.
        /// </returns>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
        /// <remarks>
        /// <para>A key cannot be <see langword="null"/>, but a value can be, if the type of values in the list, <typeparamref name="TValue"/>, is a reference or <see cref="Nullable{T}"/> type.</para>
        /// <para>If the <paramref name="key"/> is not found when a value is being retrieved, <see cref="KeyNotFoundException"/> is thrown.
        /// If the key is not found when a value is being set, the key and value are added.</para>
        /// <para>You can also use this property to add new elements by setting the value of a key that does not exist in the <see cref="StringKeyedDictionary{TValue}"/>, for example:
        /// <code lang="C#">myDictionary["myNonexistentKey"] = myValue;</code>
        /// However, if the specified key already exists in the <see cref="StringKeyedDictionary{TValue}"/>, setting this property
        /// overwrites the old value. In contrast, the <see cref="Add">Add</see> method throws an <see cref="ArgumentException"/>, when <paramref name="key"/> already exists in the collection.</para>
        /// <para>Getting or setting this property approaches an O(1) operation.</para>
        /// </remarks>
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, key CAN be null so it must be checked")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, key CAN be null so the Throw is reachable")]
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

        // Bug: could be cref="IStringKeyedDictionary{TValue}.this[StringSegment]" but that kills ReSharper
        /// <inheritdoc cref="P:KGySoft.Collections.IStringKeyedDictionary`1.Item(KGySoft.CoreLibraries.StringSegment)"/>
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

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        // Bug: could be cref="IStringKeyedDictionary{TValue}.this[ReadOnlySpan{char}]" but that kills ReSharper
        /// <inheritdoc cref="P:KGySoft.Collections.IStringKeyedDictionary`1.Item(System.ReadOnlySpan{System.Char})"/>
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
#endif

        #endregion

        #region Explicitly Implemented Interface Indexers

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, key CAN be null so it must be checked")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, key CAN be null so the Throw is reachable")]
        object IDictionary.this[object key]
        {
            get => key switch
            {
                string stringKey => TryGetValue(stringKey, out TValue value) ? (object)value : null,
                StringSegment stringSegmentKey => TryGetValue(stringSegmentKey, out TValue value) ? (object)value : null,
                _ => null
            };
            set
            {
                if (key == null)
                    Throw.ArgumentNullException(Argument.key);
                Throw.ThrowIfNullIsInvalid<TValue>(value);

                var stringKey = key as string;
                if (stringKey == null)
                    Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, Reflector.StringType));
                try
                {
                    this[stringKey] = (TValue)value;
                }
                catch (InvalidCastException)
                {
                    Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeValue));
                }
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringKeyedDictionary{TValue}"/> class
        /// using ordinal comparison.
        /// </summary>
        public StringKeyedDictionary() : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringKeyedDictionary{TValue}"/> class
        /// using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">A <see cref="StringSegmentComparer"/> instance to use when comparing keys.
        /// When <see langword="null"/>, ordinal comparison will be used.</param>
        public StringKeyedDictionary(StringSegmentComparer comparer) : this(0, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringKeyedDictionary{TValue}"/> class
        /// using the specified <paramref name="capacity"/> and <paramref name="comparer"/>.
        /// </summary>
        /// <param name="capacity">The initial capacity that the <see cref="StringSegmentComparer"/> can contain.</param>
        /// <param name="comparer">A <see cref="StringSegmentComparer"/> instance to use when comparing keys.
        /// When <see langword="null"/>, ordinal comparison will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public StringKeyedDictionary(int capacity, StringSegmentComparer comparer = null)
        {
            if (capacity < 0)
                Throw.ArgumentOutOfRangeException(Argument.capacity, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            this.comparer = comparer;
            if (capacity > 0)
                Initialize(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringKeyedDictionary{TValue}"/> class from the specified <paramref name="dictionary"/>
        /// using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary whose elements are added to the <see cref="StringKeyedDictionary{TValue}"/>.</param>
        /// <param name="comparer">A <see cref="StringSegmentComparer"/> instance to use when comparing keys.
        /// When <see langword="null"/>, ordinal comparison will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public StringKeyedDictionary(IDictionary<string, TValue> dictionary, StringSegmentComparer comparer = null)
            : this(dictionary?.Count ?? 0, comparer)
        {
            if (dictionary == null)
                Throw.ArgumentNullException(Argument.dictionary);
            foreach (KeyValuePair<string, TValue> item in dictionary)
                Add(item.Key, item.Value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringKeyedDictionary{TValue}"/> class from the specified <paramref name="collection"/>
        /// using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements are added to the <see cref="StringKeyedDictionary{TValue}"/>.</param>
        /// <param name="comparer">A <see cref="StringSegmentComparer"/> instance to use when comparing keys.
        /// When <see langword="null"/>, ordinal comparison will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public StringKeyedDictionary(IEnumerable<KeyValuePair<string, TValue>> collection, StringSegmentComparer comparer = null)
            : this(comparer)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);
            foreach (KeyValuePair<string, TValue> item in collection)
                Add(item.Key, item.Value);
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

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="StringKeyedDictionary{TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be <see langword="null"/>&#160;for reference and <see cref="Nullable{T}"/> types.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="StringKeyedDictionary{TValue}"/>.</exception>
        /// <remarks>
        /// <para>A key cannot be <see langword="null"/>, but a value can be, if the type of values in the sorted list, <typeparamref name="TValue"/>, is a reference or <see cref="Nullable{T}"/> type.</para>
        /// <para>You can also use the <see cref="this[string]">indexer</see> to add new elements by setting the value of a
        /// key that does not exist in the <see cref="StringKeyedDictionary{TValue}"/>. for example:
        /// <code lang="C#"><![CDATA[myCollection["myNonexistentKey"] = myValue;]]></code>
        /// However, if the specified key already exists in the <see cref="StringKeyedDictionary{TValue}"/>, setting the <see cref="this[string]">indexer</see>
        /// overwrites the old value. In contrast, the <see cref="Add">Add</see> method does not modify existing elements.</para>
        /// <para>This method approaches an O(1) operation unless if insertion causes a resize, in which case the operation is O(n).</para>
        /// </remarks>
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, key CAN be null so it must be checked")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, key CAN be null so the Throw is reachable")]
        public void Add(string key, TValue value)
        {
            if (key == null)
                Throw.ArgumentNullException(Argument.key);

            Insert(key, value, true);
        }

        /// <summary>
        /// Removes the value with the specified <paramref name="key"/> from the <see cref="StringKeyedDictionary{TValue}"/>.
        /// </summary>
        /// <param name="key">Key of the item to remove.</param>
        /// <returns><see langword="true"/>&#160;if the element is successfully removed; otherwise, <see langword="false"/>. This method also returns <see langword="false"/>&#160;if key was not found in the dictionary.</returns>
        /// <remarks><para>If the <see cref="StringKeyedDictionary{TValue}"/> does not contain an element with the specified key, the dictionary remains unchanged. No exception is thrown.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, key CAN be null so it must be checked")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, key CAN be null so the Throw is reachable")]
        public bool Remove(string key)
        {
            if (key == null)
                Throw.ArgumentNullException(Argument.key);

            return InternalRemove(key, default, false);
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="StringKeyedDictionary{TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="Count"/> property is set to 0, and references to other objects from elements of the collection are also released.</para>
        /// <para>This method is an O(1) operation.</para>
        /// </remarks>
        public void Clear()
        {
            if (Count == 0)
                return;

            buckets = null;
            entries = null;
            usedCount = 0;
            deletedCount = 0;
            deletedItemsBucket = -1;
            version += 1;
        }

        /// <summary>
        /// Tries to gets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>, if this dictionary contains an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <remarks>
        /// <para>If the <paramref name="key"/> is not found, then the <paramref name="value"/> parameter gets the appropriate default value
        /// for the type <typeparamref name="TValue"/>; for example, 0 (zero) for integer types, <see langword="false"/>&#160;for Boolean types, and <see langword="null"/>&#160;for reference types.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
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

        // Bug: could be cref="IStringKeyedDictionary{TValue}.TryGetValue(StringSegment,out TValue)" but that kills ReSharper
        /// <inheritdoc cref="M:KGySoft.Collections.IStringKeyedDictionary`1.TryGetValue(KGySoft.CoreLibraries.StringSegment,`0@)"/>
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

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        // Bug: could be cref="IStringKeyedDictionary{TValue}.TryGetValue(ReadOnlySpan{char},out TValue)" but that kills ReSharper
        /// <inheritdoc cref="M:KGySoft.Collections.IStringKeyedDictionary`1.TryGetValue(System.ReadOnlySpan{System.Char},`0@)"/>
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
#endif

        /// <summary>
        /// Determines whether the <see cref="StringKeyedDictionary{TValue}"/> contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="StringKeyedDictionary{TValue}"/>.</param>
        /// <returns><see langword="true"/>&#160;if the <see cref="StringKeyedDictionary{TValue}"/> contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks><para>This method approaches an O(1) operation.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(string key) => GetItemIndex(key) >= 0;

        // Bug: could be cref="IStringKeyedDictionary{TValue}.ContainsKey(StringSegment)" but that kills ReSharper
        /// <inheritdoc cref="M:KGySoft.Collections.IStringKeyedDictionary`1.ContainsKey(KGySoft.CoreLibraries.StringSegment)"/>
        public bool ContainsKey(StringSegment key) => GetItemIndex(key) >= 0;

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        // Bug: could be cref="IStringKeyedDictionary{TValue}.ContainsKey(ReadOnlySpan{char})" but that kills ReSharper
        /// <inheritdoc cref="M:KGySoft.Collections.IStringKeyedDictionary`1.ContainsKey(System.ReadOnlySpan{System.Char})"/>
        public bool ContainsKey(ReadOnlySpan<char> key) => GetItemIndex(key) >= 0;
#endif

        /// <summary>
        /// Determines whether the <see cref="StringKeyedDictionary{TValue}"/> contains a specific value.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="StringKeyedDictionary{TValue}"/>.
        /// The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns><see langword="true"/>&#160;if the <see cref="StringKeyedDictionary{TValue}"/> contains an element with the specified <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="TValue"/> is an <see langword="enum"/>&#160;type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="TValue"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        /// </remarks>
        public bool ContainsValue(TValue value)
        {
            if (entries == null)
                return false;

            if (value == null)
            {
                for (int i = 0; i < usedCount; i++)
                {
                    if (entries[i].Next >= -1 && entries[i].Value == null)
                        return true;
                }

                return false;
            }

            IEqualityComparer<TValue> valueComparer = ComparerHelper<TValue>.EqualityComparer;
            for (int i = 0; i < usedCount; i++)
            {
                if (entries[i].Next >= -1 && valueComparer.Equals(entries[i].Value, value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="StringKeyedDictionary{TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="Enumerator"/> that can be used to iterate through the <see cref="StringKeyedDictionary{TValue}"/>.
        /// </returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public Enumerator GetEnumerator()
            => new Enumerator(this);

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
            entries = new Entry[Math.Max(1, capacity)];
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

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        private int GetItemIndex(ReadOnlySpan<char> key)
        {
            if (buckets == null)
                return -1;

            if (comparer == null)
            {
                int hashCode = String.GetHashCode(key);
                for (int i = buckets[hashCode & mask] - 1; i >= 0; i = entries[i].Next)
                {
                    if (entries[i].Hash == hashCode && key.Equals(entries[i].Key.AsSpan(), StringComparison.Ordinal))
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
#endif

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
                Initialize(minCapacity);

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
            int newBucketSize = Math.Max(newCapacity, minCapacity).GetNextPowerOfTwo();
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

        #region Explicitly Implemented Interface Methods

        void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
        {
            int i = GetItemIndex(item.Key);
            return i >= 0 && ComparerHelper<TValue>.EqualityComparer.Equals(item.Value, entries[i].Value);
        }

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, array CAN be null so it must be checked")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, array CAN be null so the Throw is reachable")]
        void ICollection<KeyValuePair<string, TValue>>.CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                Throw.ArgumentNullException(Argument.array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
            if (array.Length - arrayIndex < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

            for (int i = 0; i < usedCount; i++)
            {
                if (entries[i].Next >= -1)
                    array[arrayIndex++] = new KeyValuePair<string, TValue>(entries[i].Key, entries[i].Value);
            }
        }

        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
        {
            if (item.Key == null)
                Throw.ArgumentNullException(Argument.key);
            return InternalRemove(item.Key, item.Value, true);
        }

        IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => new ReferenceEnumerator(this, true);
        IEnumerator IEnumerable.GetEnumerator() => new ReferenceEnumerator(this, true);

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, key CAN be null so it must be checked")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, key CAN be null so the Throw is reachable")]
        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                Throw.ArgumentNullException(Argument.key);
            Throw.ThrowIfNullIsInvalid<TValue>(value);

            var stringKey = key as string;
            if (stringKey == null)
                Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, Reflector.StringType));
            try
            {
                Add(stringKey, (TValue)value);
            }
            catch (InvalidCastException)
            {
                Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeValue));
            }
        }

        bool IDictionary.Contains(object key) => key switch
        {
            string stringKey => ContainsKey(stringKey),
            StringSegment stringSegmentKey => ContainsKey(stringSegmentKey),
            _ => false
        };

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        IDictionaryEnumerator IDictionary.GetEnumerator() => new ReferenceEnumerator(this, false);

        void IDictionary.Remove(object key)
        {
            if (key is string stringKey)
                Remove(stringKey);
        }

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, array CAN be null so it must be checked")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, array CAN be null so the Throw is reachable")]
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                Throw.ArgumentNullException(Argument.array);
            if (index < 0 || index > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (array.Length - index < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (array.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            switch (array)
            {
                case KeyValuePair<string, TValue>[] keyValuePairs:
                    ((ICollection<KeyValuePair<string, TValue>>)this).CopyTo(keyValuePairs, index);
                    return;

                case DictionaryEntry[] dictionaryEntries:
                    for (int i = 0; i < usedCount; i++)
                    {
                        if (entries[i].Next >= -1)
                            dictionaryEntries[index++] = new DictionaryEntry(entries[i].Key, entries[i].Value);
                    }

                    return;

                case object[] objectArray:
                    for (int i = 0; i < usedCount; i++)
                    {
                        objectArray[index] = new KeyValuePair<string, TValue>(entries[i].Key, entries[i].Value);
                        index += 1;
                    }

                    return;

                default:
                    Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
                    return;
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
