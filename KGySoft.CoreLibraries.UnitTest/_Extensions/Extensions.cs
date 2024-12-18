﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Extensions.cs
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
#if NETCOREAPP && !NETSTANDARD_TEST
using System.Collections.Immutable;
#endif
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Moved to the tests from Libraries (it was internal anyway) because not recommended to use in general as it is not repeatable.
    /// </summary>
    internal static class Extensions
    {
        #region Methods

        /// <summary>
        /// Creates an <see cref="IEnumerable{T}"/> of <see cref="DictionaryEntry"/> elements from an <see cref="IDictionaryEnumerator"/>.
        /// </summary>
        /// <param name="enumerator">The <see cref="IDictionaryEnumerator"/> to create an <see cref="IEnumerable{T}"/> from.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that enumerates the elements of the input <paramref name="enumerator"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumerator"/> is <see langword="null"/>.</exception>
        /// <remarks><note type="caution">Unlike the usual <see cref="IEnumerable{T}"/> implementations, the result of this method cannot be enumerated more than once.</note></remarks>
        internal static IEnumerable<DictionaryEntry> ToEnumerable(this IDictionaryEnumerator enumerator)
        {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator), Res.ArgumentNull);

            while (enumerator.MoveNext())
                yield return enumerator.Entry;
        }

        /// <summary>
        /// Creates an <see cref="IEnumerable{T}"/>&#160;<see cref="KeyValuePair{TKey,TValue}"/> elements from an <see cref="IDictionaryEnumerator"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the key elements of the <paramref name="enumerator"/>.</typeparam>
        /// <typeparam name="TValue">The type of the value elements of the <paramref name="enumerator"/>.</typeparam>
        /// <param name="enumerator">The <see cref="IDictionaryEnumerator"/> to create an <see cref="IEnumerable{DictionaryEntry}"/> from.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that enumerates the elements of the input <paramref name="enumerator"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumerator"/> is <see langword="null"/>.</exception>
        /// <remarks><note type="caution">Unlike the usual <see cref="IEnumerable{T}"/> implementations, the result of this method cannot be enumerated more than once.</note></remarks>
        internal static IEnumerable<KeyValuePair<TKey, TValue>> ToEnumerable<TKey, TValue>(this IDictionaryEnumerator enumerator)
        {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator), Res.ArgumentNull);

            while (enumerator.MoveNext())
                yield return new KeyValuePair<TKey, TValue>((TKey)enumerator.Key, (TValue)enumerator.Value);
        }

        internal static IEnumerable<string> GetKeysEnumerator(this IDictionaryEnumerator enumerator)
        {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator), Res.ArgumentNull);

            while (enumerator.MoveNext())
                yield return enumerator.Key as string;
        }

        /// <summary>
        /// Converts the byte array (deemed as extended 8-bit ASCII characters) to its raw string representation.
        /// </summary>
        internal static string ToRawString(this byte[] bytes)
        {
            string s = Encoding.Default.GetString(bytes);
            var chars = new char[s.Length];
            var whitespaceControls = new[] { '\t', '\r', '\n' };
            for (int i = 0; i < s.Length; i++)
                chars[i] = s[i] < 32 && !s[i].In(whitespaceControls) ? '□' : s[i];
            return new String(chars);
        }

        internal static string Dump(this object o, bool dumpProperties = true)
        {
            if (o == null)
                return "<null>";

            if (o is IConvertible convertible)
                return convertible.ToString(CultureInfo.InvariantCulture);

            if (o is IEnumerable enumerable)
                return $"[{enumerable.Cast<object>().Select(e => e.Dump(dumpProperties)).Join(", ")}]";

            if (!dumpProperties)
                return o.ToString();

            return $"{{{o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Select(p => $"{p.Name} = {Dump(p.GetValue(o, null), true)}").Join(", ")}}}";
        }

        internal static bool AreDefaultOrSequenceEqual<T>(this IEnumerable<T> source, IEnumerable<T> target)
        {
            // including null
            if (ReferenceEquals(source, target))
                return true;

#if NETCOREAPP && !NETSTANDARD_TEST
            if ((source, target) is (ImmutableArray<T> { IsDefault: true }, ImmutableArray<T> { IsDefault: true }))
                return true;
#endif

            return source.SequenceEqual(target);
        }

        #endregion
    }
}
