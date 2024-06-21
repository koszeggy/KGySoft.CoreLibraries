#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AllowNullDictionary.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using KGySoft.Annotations;
using KGySoft.Diagnostics;
using KGySoft.Reflection;

#endregion

#region Suppressions

#if NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint. - TKey CAN be null here
#else
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes). - That's the point
#endif
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. - False alarm, it is decorated by [AllowNull]. Declaring as nullable would just raise more false alarm warnings.
#pragma warning disable CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes). - That's the point

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a dictionary that allows <see langword="null"/> as a key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <seealso cref="IDictionary{TKey, TValue}" />
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ").Name}; TValue = {typeof(" + nameof(TValue) + ").Name}")]
    [SuppressMessage("ReSharper", "UseNullableReferenceTypesAnnotationSyntax", Justification = "False alarm, only [NotNull] prevents AssignNullToNotNullAttribute warnings")]
    public class AllowNullDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        #region Nested Types

        #region Enumerations

        private enum EnumerationState : byte
        {
            NotStarted,
            EnumeratingNull,
            EnumeratingDictionary,
            Finished
        }

        #endregion

        #region Nested Classes

        #region ReferenceEnumerator class

        private class ReferenceEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            #region Fields

            private readonly AllowNullDictionary<TKey, TValue> owner;
            private readonly bool isGeneric;

            private Dictionary<TKey, TValue>.Enumerator wrappedEnumerator;
            private KeyValuePair<TKey, TValue> current;
            private EnumerationState state;

            #endregion

            #region Properties

            #region Public Properties

            public KeyValuePair<TKey, TValue> Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    if (state is EnumerationState.NotStarted or EnumerationState.Finished)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return isGeneric ? current : new DictionaryEntry(current.Key!, current.Value);
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (state is not (EnumerationState.EnumeratingNull or EnumerationState.EnumeratingDictionary))
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return new DictionaryEntry(current.Key!, current.Value);
                }
            }

            object? IDictionaryEnumerator.Key
            {
                get
                {
                    if (state is not (EnumerationState.EnumeratingNull or EnumerationState.EnumeratingDictionary))
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current.Key;
                }
            }

            object? IDictionaryEnumerator.Value
            {
                get
                {
                    if (state is not (EnumerationState.EnumeratingNull or EnumerationState.EnumeratingDictionary))
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current.Value;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal ReferenceEnumerator(AllowNullDictionary<TKey, TValue> owner, bool isGeneric)
            {
                this.owner = owner;
                wrappedEnumerator = owner.dict.GetEnumerator();
                current = default;
                state = EnumerationState.NotStarted;
                this.isGeneric = isGeneric;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Releases the enumerator
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                // Known limitation: modifications are not detected for the null key
                switch (state)
                {
                    case EnumerationState.NotStarted:
                        if (!owner.hasNullKey)
                        {
                            state = EnumerationState.EnumeratingDictionary;
                            goto case EnumerationState.EnumeratingDictionary;
                        }

                        state = EnumerationState.EnumeratingNull;
                        current = new KeyValuePair<TKey, TValue>(default!, owner.nullValue);
                        return true;

                    case EnumerationState.EnumeratingNull:
                        state = EnumerationState.EnumeratingDictionary;
                        goto case EnumerationState.EnumeratingDictionary;

                    case EnumerationState.EnumeratingDictionary:
                        bool result = wrappedEnumerator.MoveNext();
                        current = wrappedEnumerator.Current;
                        if (!result)
                            state = EnumerationState.Finished;
                        return result;

                    case EnumerationState.Finished:
                        return false;

                    default:
                        return Throw.InvalidOperationException<bool>(Res.InternalError($"Unexpected status: {state}"));
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                ((IEnumerator)wrappedEnumerator).Reset();
                state = EnumerationState.NotStarted;
            }

            #endregion
        }

        #endregion

        #region KeysCollection class

        [DebuggerTypeProxy(typeof(DictionaryKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
        private sealed class KeysCollection : ICollection<TKey>, ICollection
        {
            #region Fields

            private readonly AllowNullDictionary<TKey, TValue> owner;

            #endregion

            #region Properties

            #region Public Properties

            public int Count => owner.Count;
            public bool IsReadOnly => true;

            #endregion

            #region Explicitly Implemented Interface Properties

            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => ((ICollection)owner.Keys).SyncRoot;

            #endregion

            #endregion

            #region Constructors

            internal KeysCollection(AllowNullDictionary<TKey, TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            #region Public Methods

            public bool Contains(TKey item) => owner.ContainsKey(item);

            public void CopyTo(TKey?[] array, int arrayIndex)
            {
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
                if (array.Length - arrayIndex < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

                if (owner.hasNullKey)
                {
                    array[arrayIndex] = default;
                    arrayIndex += 1;
                }

                owner.dict.Keys.CopyTo(array!, arrayIndex);
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                if (owner.hasNullKey)
                    yield return default!;
                foreach (TKey key in owner.dict.Keys)
                    yield return key;
            }

            #endregion

            #region Explicitly Implemented Interface Methods

            void ICollection<TKey>.Add(TKey item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            void ICollection<TKey>.Clear() => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            bool ICollection<TKey>.Remove(TKey item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);

                if (array is TKey[] keys)
                {
                    CopyTo(keys, index);
                    return;
                }

                if (index < 0 || index > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.index);
                int length = array.Length;
                if (length - index < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                if (array.Rank != 1)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

                if (array is object?[] objectArray)
                {
                    if (owner.hasNullKey)
                    {
                        objectArray[index] = default;
                        index += 1;
                    }

                    ((ICollection)owner.dict.Keys).CopyTo(array, index);
                    return;
                }

                Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
            }

            #endregion

            #endregion
        }

        #endregion

        #region ValuesCollection class

        [DebuggerTypeProxy(typeof(DictionaryValueCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
        private sealed class ValuesCollection : ICollection<TValue>, ICollection
        {
            #region Fields

            private readonly AllowNullDictionary<TKey, TValue> owner;

            #endregion

            #region Properties

            #region Public Properties

            public int Count => owner.Count;
            public bool IsReadOnly => true;

            #endregion

            #region Explicitly Implemented Interface Properties

            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => ((ICollection)owner.Values).SyncRoot;

            #endregion

            #endregion

            #region Constructors

            internal ValuesCollection(AllowNullDictionary<TKey, TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            #region Public Methods

            public bool Contains(TValue item) => owner.ContainsValue(item);

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
                if (array.Length - arrayIndex < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

                if (owner.hasNullKey)
                {
                    array[arrayIndex] = owner.nullValue;
                    arrayIndex += 1;
                }

                owner.dict.Values.CopyTo(array, arrayIndex);
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                if (owner.hasNullKey)
                    yield return owner.nullValue;
                foreach (TValue value in owner.dict.Values)
                    yield return value;
            }

            #endregion

            #region Explicitly Implemented Interface Methods

            void ICollection<TValue>.Add(TValue item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            void ICollection<TValue>.Clear() => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            bool ICollection<TValue>.Remove(TValue item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                    return;
                }

                if (index < 0 || index > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.index);
                int length = array.Length;
                if (length - index < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                if (array.Rank != 1)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

                if (array is object?[] objectArray)
                {
                    if (owner.hasNullKey)
                    {
                        objectArray[index] = owner.nullValue;
                        index += 1;
                    }

                    ((ICollection)owner.dict.Values).CopyTo(array, index);
                    return;
                }

                Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Nested structs

        /// <summary>
        /// Enumerates the elements of a <see cref="AllowNullDictionary{TKey,TValue}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            #region Fields

            private readonly AllowNullDictionary<TKey, TValue> owner;

            private Dictionary<TKey, TValue>.Enumerator wrappedEnumerator;
            private KeyValuePair<TKey, TValue> current;
            private EnumerationState state;

            #endregion

            #region Properties

            #region Public Properties

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public readonly KeyValuePair<TKey, TValue> Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    if (state is EnumerationState.NotStarted or EnumerationState.Finished)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(AllowNullDictionary<TKey, TValue> owner)
            {
                this.owner = owner;
                wrappedEnumerator = owner.dict.GetEnumerator();
                current = default;
                state = EnumerationState.NotStarted;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Releases the enumerator
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                // Known limitation: modifications are not detected for the null key
                switch (state)
                {
                    case EnumerationState.NotStarted:
                        if (!owner.hasNullKey)
                        {
                            state = EnumerationState.EnumeratingDictionary;
                            goto case EnumerationState.EnumeratingDictionary;
                        }

                        current = new KeyValuePair<TKey, TValue>(default!, owner.nullValue);
                        state = EnumerationState.EnumeratingNull;
                        return true;

                    case EnumerationState.EnumeratingNull:
                        state = EnumerationState.EnumeratingDictionary;
                        goto case EnumerationState.EnumeratingDictionary;

                    case EnumerationState.EnumeratingDictionary:
                        bool result = wrappedEnumerator.MoveNext();
                        current = wrappedEnumerator.Current;
                        if (!result)
                            state = EnumerationState.Finished;
                        return result;

                    case EnumerationState.Finished:
                        return false;

                    default:
                        return Throw.InvalidOperationException<bool>(Res.InternalError($"Unexpected status: {state}"));
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                ((IEnumerator)wrappedEnumerator).Reset();
                state = EnumerationState.NotStarted;
            }

            #endregion
        }

        #endregion

        #endregion

        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        private readonly Dictionary<TKey, TValue> dict;

        private bool hasNullKey;
        [AllowNull]private TValue nullValue;

        [NonSerialized]private KeysCollection? keysCollection;
        [NonSerialized]private ValuesCollection? valuesCollection;

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the keys stored in the dictionary.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the keys in the original <see cref="AllowNullDictionary{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="AllowNullDictionary{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TKey> Keys => keysCollection ??= new KeysCollection(this);

        /// <summary>
        /// Gets the values stored in the dictionary.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the values in the original <see cref="AllowNullDictionary{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="AllowNullDictionary{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TValue> Values => valuesCollection ??= new ValuesCollection(this);

        /// <summary>
        /// Gets number of elements currently stored in this <see cref="AllowNullDictionary{TKey,TValue}"/> instance.
        /// </summary>
        public int Count => hasNullKey ? dict.Count + 1 : dict.Count;

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        ICollection IDictionary.Keys => (ICollection)Keys;
        ICollection IDictionary.Values => (ICollection)Values;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => ((ICollection)dict).SyncRoot;

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
        /// <param name="key">The key of the value to get or set. In this dictionary it can be even <see langword="null"/>.</param>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
        /// <remarks>
        /// <para>In this dictionary both the <paramref name="key"/> and the <paramref name="value"/>can be <see langword="null"/>.</para>
        /// <para>If the <paramref name="key"/> is not found when a value is being retrieved, <see cref="KeyNotFoundException"/> is thrown.
        /// If the key is not found when a value is being set, the key and value are added.</para>
        /// <para>You can also use this property to add new elements by setting the value of a key that does not exist in the <see cref="AllowNullDictionary{TKey,TValue}"/>, for example:
        /// <code lang="C#">myDictionary[myNonexistentKey] = myValue;</code>
        /// However, if the specified key already exists in the <see cref="AllowNullDictionary{TKey,TValue}"/>, setting this property
        /// overwrites the old value. In contrast, the <see cref="Add">Add</see> method throws an <see cref="ArgumentException"/>, when <paramref name="key"/> already exists in the collection.</para>
        /// <para>Getting or setting this property approaches an O(1) operation.</para>
        /// </remarks>
        public TValue this[[CanBeNull]TKey key]
        {
            get
            {
                if (key != null)
                    return dict[key];

                if (!hasNullKey)
                    Throw.KeyNotFoundException();
                return nullValue;
            }
            set
            {
                if (key == null)
                {
                    nullValue = value;
                    hasNullKey = true;
                }
                else
                    dict[key] = value;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object? IDictionary.this[object? key]
        {
            get => key switch
            {
                TKey k => dict.TryGetValue(k, out TValue? result) ? result : null,
                null => hasNullKey ? nullValue : null,
                _ => null
            };
            set
            {
                Throw.ThrowIfNullIsInvalid<TKey>(key, Argument.key);
                Throw.ThrowIfNullIsInvalid<TValue>(value);

                try
                {
                    TKey typedKey = (TKey)key!;
                    try
                    {
                        this[typedKey] = (TValue)value!;
                    }
                    catch (InvalidCastException)
                    {
                        Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeof(TValue)));
                    }
                }
                catch (InvalidCastException)
                {
                    Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key!, typeof(TKey)));
                }
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowNullDictionary{TKey,TValue}"/> class.
        /// </summary>
        public AllowNullDictionary() : this(defaultCapacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowNullDictionary{TKey,TValue}"/> class
        /// using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys.</param>
        public AllowNullDictionary(IEqualityComparer<TKey>? comparer) : this(defaultCapacity, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowNullDictionary{TKey,TValue}"/> class
        /// using the specified <paramref name="capacity"/> and <paramref name="comparer"/>.
        /// </summary>
        /// <param name="capacity">The initial capacity that the <see cref="AllowNullDictionary{TKey,TValue}"/> can contain.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public AllowNullDictionary(int capacity, IEqualityComparer<TKey>? comparer = null)
            => dict = new Dictionary<TKey, TValue>(capacity, comparer);

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowNullDictionary{TKey,TValue}"/> class
        /// with the specified <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="collection"/> contains one or more duplicate keys.</exception>
        public AllowNullDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer = null)
            : this(((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count).GetValueOrDefault(defaultCapacity), comparer)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);
            foreach (KeyValuePair<TKey, TValue> item in collection)
                this[item.Key] = item.Value;
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="AllowNullDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add. In this dictionary it can be even <see langword="null"/>.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="AllowNullDictionary{TKey,TValue}"/>.</exception>
        /// <remarks>
        /// <para>In this dictionary both the <paramref name="key"/> and the <paramref name="value"/>can be <see langword="null"/>.</para>
        /// <para>You can also use the <see cref="this[TKey]">indexer</see> to add new elements by setting the value of a
        /// key that does not exist in the <see cref="AllowNullDictionary{TKey,TValue}"/>. for example:
        /// <code lang="C#"><![CDATA[myCollection[myNonexistentKey] = myValue;]]></code>
        /// However, if the specified key already exists in the <see cref="AllowNullDictionary{TKey,TValue}"/>, setting the <see cref="this[TKey]">indexer</see>
        /// overwrites the old value. In contrast, the <see cref="Add">Add</see> method does not modify existing elements.</para>
        /// <para>This method approaches an O(1) operation unless if insertion causes a resize, in which case the operation is O(n).</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void Add([CanBeNull]TKey key, TValue value)
        {
            if (key != null)
            {
                dict.Add(key, value);
                return;
            }

            if (hasNullKey)
                Throw.ArgumentException(Argument.key, Res.IDictionaryDuplicateKey);
            hasNullKey = true;
            nullValue = value;
        }

        /// <summary>
        /// Determines whether the <see cref="AllowNullDictionary{TKey,TValue}"/> contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="AllowNullDictionary{TKey,TValue}"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="AllowNullDictionary{TKey,TValue}"/> contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks><para>This method approaches an O(1) operation.</para></remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public bool ContainsKey([CanBeNull]TKey key) => key == null ? hasNullKey : dict.ContainsKey(key);

        /// <summary>
        /// Determines whether the <see cref="AllowNullDictionary{TKey,TValue}"/> contains a specific value.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="AllowNullDictionary{TKey,TValue}"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="AllowNullDictionary{TKey,TValue}"/> contains an element with the specified <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method determines equality using the <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> comparer.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public bool ContainsValue(TValue value)
            => hasNullKey && EqualityComparer<TValue>.Default.Equals(value, nullValue) || dict.ContainsValue(value);

        /// <summary>
        /// Removes the value with the specified <paramref name="key"/> from the <see cref="AllowNullDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">Key of the item to remove.</param>
        /// <returns><see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if key was not found in the dictionary.</returns>
        /// <remarks><para>If the <see cref="AllowNullDictionary{TKey,TValue}"/> does not contain an element with the specified key, the dictionary remains unchanged. No exception is thrown.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public bool Remove([CanBeNull]TKey key)
        {
            if (key != null)
                return dict.Remove(key);
            bool oldHasNull = hasNullKey;
            hasNullKey = false;
            if (Reflector<TValue>.IsManaged)
                nullValue = default;
            return oldHasNull;
        }

        /// <summary>
        /// Tries to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>, if this dictionary contains an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <remarks>
        /// <para>If the <paramref name="key"/> is not found, then the <paramref name="value"/> parameter gets the appropriate default value
        /// for the type <typeparamref name="TValue"/>; for example, 0 (zero) for integer types, <see langword="false"/> for Boolean types, and <see langword="null"/> for reference types.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public bool TryGetValue([CanBeNull]TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            if (key != null)
                return dict.TryGetValue(key, out value);

            value = hasNullKey ? nullValue : default;
            return hasNullKey;
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="AllowNullDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="Count"/> property is set to 0, and references to other objects from elements of the collection are also released.</para>
        /// <para>This method is an O(1) operation.</para>
        /// </remarks>
        public void Clear()
        {
            hasNullKey = false;
            if (Reflector<TValue>.IsManaged)
                nullValue = default;
            dict.Clear();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="AllowNullDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the <see cref="AllowNullDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public Enumerator GetEnumerator() => new Enumerator(this);

        #endregion

        #region Private Methods

        private IEnumerator<KeyValuePair<TKey, TValue>> GetEnumeratorWithNull()
        {
            yield return new KeyValuePair<TKey, TValue>(default!, nullValue);
            foreach (var item in dict)
                yield return item;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => TryGetValue(item.Key, out TValue? value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);


        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
            => TryGetValue(item.Key, out TValue? value) && EqualityComparer<TValue>.Default.Equals(value, item.Value) && Remove(item.Key);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
            if (array.Length - arrayIndex < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

            if (hasNullKey)
            {
                array[arrayIndex] = new KeyValuePair<TKey, TValue>(default!, nullValue);
                arrayIndex += 1;
            }

            ((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(array, arrayIndex);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => new ReferenceEnumerator(this, true);

        IEnumerator IEnumerable.GetEnumerator() => new ReferenceEnumerator(this, true);

        IDictionaryEnumerator IDictionary.GetEnumerator() => new ReferenceEnumerator(this, false);

        void IDictionary.Add(object? key, object? value)
        {
            Throw.ThrowIfNullIsInvalid<TKey>(key, Argument.key);
            Throw.ThrowIfNullIsInvalid<TValue>(value);

            try
            {
                TKey typedKey = (TKey)key!;
                try
                {
                    Add(typedKey, (TValue)value!);
                }
                catch (InvalidCastException)
                {
                    Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeof(TValue)));
                }
            }
            catch (InvalidCastException)
            {
                Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key!, typeof(TKey)));
            }
        }

        bool IDictionary.Contains(object? key)
            => key switch
            {
                TKey k => ContainsKey(k),
                null => hasNullKey,
                _ => false
            };

        void IDictionary.Remove(object? key)
        {
            if (key == null)
            {
                if (!hasNullKey)
                    return;

                hasNullKey = false;
                if (Reflector<TValue>.IsManaged)
                    nullValue = default;
                return;
            }

            if (key is TKey k)
                Remove(k);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (index < 0 || index > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            int length = array.Length;
            if (length - index < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (array.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            switch (array)
            {
                case KeyValuePair<TKey, TValue>[] keyValuePairs:
                    ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(keyValuePairs, index);
                    return;

                case DictionaryEntry[] dictionaryEntries:
                    if (hasNullKey)
                    {
                        dictionaryEntries[index] = new DictionaryEntry(null!, nullValue);
                        index += 1;
                    }

                    ((ICollection)dict).CopyTo(array, index);
                    return;

                case object[] objectArray:
                    if (hasNullKey)
                    {
                        objectArray[index] = new KeyValuePair<TKey, TValue>(default!, nullValue);
                        index += 1;
                    }

                    ((ICollection)dict).CopyTo(array, index);
                    return;

                default:
                    Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
                    return;
            }
        }

        #endregion

        #endregion
    }
}
