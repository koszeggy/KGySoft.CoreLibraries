#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AllowNullDictionary.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using KGySoft.Annotations;
using KGySoft.Diagnostics;

#endregion

#region Suppressions

#if NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint. - TKey CAN be null here
#else
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#endif

#endregion


namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a dictionary, which allows <see langword="null"/>&#160;as a key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <seealso cref="IDictionary{TKey, TValue}" />
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    internal class AllowNullDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        private readonly Dictionary<TKey, TValue> dict;

        private bool hasNullKey;

        // ReSharper disable once RedundantDefaultMemberInitializer - needed for constructor
        [AllowNull]private TValue nullValue = default!;

        #endregion

        #region Properties and Indexers

        #region Properties

        public ICollection<TKey> Keys
        {
            get
            {
                if (!hasNullKey)
                    return dict.Keys;

                var keys = new TKey[Count];
                keys[0] = default(TKey)!;
                dict.Keys.CopyTo(keys, 1);
                return keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (!hasNullKey)
                    return dict.Values;

                var values = new TValue[Count];
                values[0] = nullValue;
                dict.Values.CopyTo(values, 1);
                return values;
            }
        }

        public int Count => hasNullKey ? dict.Count + 1 : dict.Count;

        public bool IsReadOnly => false;

        #endregion

        #region Indexers

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

        #endregion

        #region Constructors

        public AllowNullDictionary(int capacity) => dict = new Dictionary<TKey, TValue>(capacity);

        public AllowNullDictionary() : this(defaultCapacity)
        {
        }

        public AllowNullDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(((collection as ICollection<KeyValuePair<TKey, TValue>>)?.Count).GetValueOrDefault(defaultCapacity))
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);
            foreach (KeyValuePair<TKey, TValue> item in collection)
                this[item.Key] = item.Value;
        }

        #endregion

        #region Methods

        #region Public Methods

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

        public bool ContainsKey([CanBeNull]TKey key) => key == null ? hasNullKey : dict.ContainsKey(key);

        public bool Remove([CanBeNull]TKey key)
        {
            if (key != null)
                return dict.Remove(key);
            bool oldHasNull = hasNullKey;
            hasNullKey = false;
            return oldHasNull;
        }

        public bool TryGetValue([CanBeNull]TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            if (key != null)
                return dict.TryGetValue(key, out value);

            value = hasNullKey ? nullValue : default(TValue)!;
            return hasNullKey;
        }

        public void Clear()
        {
            hasNullKey = false;
            nullValue = default(TValue);
            dict.Clear();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
            if (array.Length - arrayIndex < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

            ((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(array, arrayIndex);
            if (hasNullKey)
                array[arrayIndex + dict.Count] = new KeyValuePair<TKey, TValue>(default(TKey)!, nullValue);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => !hasNullKey ? dict.GetEnumerator() : GetEnumeratorWithNull();

        #endregion

        #region Private Methods

        private IEnumerator<KeyValuePair<TKey, TValue>> GetEnumeratorWithNull()
        {
            yield return new KeyValuePair<TKey, TValue>(default(TKey)!, nullValue);
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

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}
