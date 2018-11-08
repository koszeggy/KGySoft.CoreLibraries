﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AllowNullDictionary.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using KGySoft.Annotations;
using KGySoft.Diagnostics;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a dictionary, which allows <see langword="null"/> as a key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <seealso cref="IDictionary{TKey, TValue}" />
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    internal class AllowNullDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        #region Fields

        private readonly Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();

        private bool hasNullKey;
        private TValue nullValue;

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
                keys[0] = default(TKey);
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
            get => key == null ? (hasNullKey ? nullValue : throw new KeyNotFoundException(Res.Get(Res.KeyNotFound))) : dict[key];
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

        public AllowNullDictionary()
        {
        }

        public AllowNullDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection), Res.ArgumentNull);
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
                throw new ArgumentException(Res.Get(Res.DuplicateKey), nameof(key));
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

        public bool TryGetValue([CanBeNull]TKey key, out TValue value)
        {
            if (key != null)
                return dict.TryGetValue(key, out value);

            value = hasNullKey ? nullValue : default(TValue);
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
            ((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(array, arrayIndex);
            if (hasNullKey)
                array[arrayIndex + dict.Count] = new KeyValuePair<TKey, TValue>(default(TKey), nullValue);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => !hasNullKey ? dict.GetEnumerator() : GetEnumeratorWithNull();

        #endregion

        #region Private Methods

        private IEnumerator<KeyValuePair<TKey, TValue>> GetEnumeratorWithNull()
        {
            yield return new KeyValuePair<TKey, TValue>(default(TKey), nullValue);
            foreach (var item in dict)
                yield return item;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => TryGetValue(item.Key, out TValue value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
            => TryGetValue(item.Key, out TValue value) && EqualityComparer<TValue>.Default.Equals(value, item.Value) && Remove(item.Key);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}
