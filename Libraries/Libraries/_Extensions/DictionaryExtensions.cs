#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DictionaryExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
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
using KGySoft.Libraries.Resources;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="IDictionary{TKey,TValue}"/> type.
    /// </summary>
    public static class DictionaryExtensions
    {
        #region Methods

        /// <summary>
        /// Tries to get the typed value from a <see cref="string"/>-<see cref="object"/> dictionary for the given key.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The dictionary key.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> not found or its actual type is not compatible with <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TActualValue">The actual type of the stored value.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> not found or its type is wrong.</returns>
        public static TActualValue GetValueOrDefault<TActualValue>(this IDictionary<string, object> dictionary, string key, TActualValue defaultValue = default)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), Res.Get(Res.ArgumentNull));

            return dictionary.TryGetValue(key, out var objValue) && objValue is TActualValue value ? value : defaultValue;
        }

        /// <summary>
        /// Tries to get the typed value from a dictionary for the given key.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The dictionary key.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> not found or its actual type is not compatible with <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TKey">The type of value</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TActualValue">The actual type of the stored value.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> not found or its type is wrong.</returns>
        public static TActualValue GetValueOrDefault<TKey, TValue, TActualValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TActualValue defaultValue = default)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), Res.Get(Res.ArgumentNull));

            return dictionary.TryGetValue(key, out var objValue) && objValue is TActualValue value ? value : defaultValue;
        }

        #endregion
    }
}
