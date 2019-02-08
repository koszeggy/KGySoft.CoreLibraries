#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DictionaryExtensions.cs
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
using System.Collections.Generic;
using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="IDictionary{TKey,TValue}"/> type.
    /// </summary>
    public static class DictionaryExtensions
    {
        #region Methods

        /// <summary>
        /// Tries to get a value from a <paramref name="dictionary"/> for the given key.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or the default value of <typeparamref name="TValue"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), Res.ArgumentNull);

            return dictionary.TryGetValue(key, out TValue value) ? value : default;
        }

        /// <summary>
        /// Tries to get the typed value from a <paramref name="dictionary"/> for the given key.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type is not compatible with <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TActualValue">The type of the value of the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        public static TActualValue GetValueOrDefault<TKey, TValue, TActualValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TActualValue defaultValue)
            where TActualValue : TValue
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), Res.ArgumentNull);

            return dictionary.TryGetValue(key, out TValue value) && value is TActualValue actualValue ? actualValue : defaultValue;
        }

        /// <summary>
        /// Tries to get the typed value from a <see cref="string"/>-<see cref="object"/>&#160;<paramref name="dictionary"/> for the given key.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type is not compatible with <typeparamref name="TActualValue"/> This parameter is optional.
        /// <br/>Default value: <see langword="null"/>&#160;if <typeparamref name="TActualValue"/> is a reference type; otherwise, the bitwise zero value of <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TActualValue">The type of the value of the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        public static TActualValue GetValueOrDefault<TActualValue>(this IDictionary<string, object> dictionary, string key, TActualValue defaultValue = default)
            => dictionary.GetValueOrDefault<string, object, TActualValue>(key, defaultValue);

        /// <summary>
        /// Returns a <see cref="LockingDictionary{TKey,TValue}"/>, which provides a thread-safe wrapper for the specified <paramref name="dictionary"/>.
        /// This only means that if the members are accessed through the returned <see cref="LockingDictionary{TKey,TValue}"/>, then the inner state of the wrapped dictionary remains always consistent and not that all of the multi-threading concerns can be ignored.
        /// For a <see cref="Cache{TKey,TValue}"/> instance consider to use the <see cref="Cache{TKey,TValue}.GetThreadSafeAccessor">GetThreadSafeAccessor</see> method instead, which does not necessarily lock the item loader delegate.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="LockingDictionary{TKey,TValue}"/> class for details and some examples.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The dictionary to create a thread-safe wrapper for.</param>
        /// <returns>A <see cref="LockingDictionary{TKey,TValue}"/>, which provides a thread-safe wrapper for the specified <paramref name="dictionary"/>.</returns>
        public static LockingDictionary<TKey, TValue> AsThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => new LockingDictionary<TKey, TValue>(dictionary);

        #endregion
    }
}
