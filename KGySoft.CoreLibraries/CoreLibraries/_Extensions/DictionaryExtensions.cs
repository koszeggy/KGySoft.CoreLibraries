﻿#region Copyright

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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using KGySoft.Collections;

#endregion

#region Suppressions

#if NETCOREAPP3_0 // Only in .NET Core 3 the IDictionary<TKey, TValue> has the TKey : notnull constraint. In .NET 5 this has already been removed
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="IDictionary{TKey,TValue}"/> type.
    /// </summary>
    public static class DictionaryExtensions
    {
        #region Methods

#if NETFRAMEWORK || NETSTANDARD2_0 // These methods are not included into .NET Core/Standard versions to prevent conflict with CollectionExtensions
        /// <summary>
        /// Tries to get a value from a <paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or the default value of <typeparamref name="TValue"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        [return:MaybeNull]public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            => GetValueOrDefault(dictionary, key, default(TValue)!);

        /// <summary>
        /// Tries to get a value from a <paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        /// <summary>
        /// Tries to get a value from a <paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found..</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValueFactory)
        {
            // null is actually tolerated but defaultValueFactory is not marked as nullable to avoid the confusing MaybeNull return value
            if (defaultValueFactory == null!)
                return dictionary.GetValueOrDefault(key, default(TValue)!);

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValueFactory.Invoke();
        }
#endif

#if !(NET35 || NET40)
        /// <summary>
        /// Tries to get a value from the provided <paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or the default value of <typeparamref name="TValue"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        /// <remarks><note>If <paramref name="dictionary"/> is neither an <see cref="IDictionary{TKey,TValue}"/>, nor an <see cref="IReadOnlyDictionary{TKey,TValue}"/> instance,
        /// then a sequential lookup is performed using a default equality comparer on the keys.</note></remarks>
        [return:MaybeNull]public static TValue GetValueOrDefault<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key)
            => GetValueOrDefault(dictionary, key, default(TValue)!);

        /// <summary>
        /// Tries to get a value from the provided <paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        /// <remarks><note>If <paramref name="dictionary"/> is neither an <see cref="IDictionary{TKey,TValue}"/>, nor an <see cref="IReadOnlyDictionary{TKey,TValue}"/> instance,
        /// then a sequential lookup is performed using a default equality comparer on the keys.</note></remarks>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "CS8600", Justification = "ReSharper does not tolerate 'out TValue? value'")]
        public static TValue GetValueOrDefault<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            switch (dictionary)
            {
                case IDictionary<TKey, TValue> dict:
                    return dict.TryGetValue(key, out TValue value) ? value : defaultValue;
                case IReadOnlyDictionary<TKey, TValue> dict:
                    return dict.TryGetValue(key, out value) ? value : defaultValue;
                default:
                {
                    IEqualityComparer<TKey> comparer = ComparerHelper<TKey>.EqualityComparer;
                    foreach (KeyValuePair<TKey, TValue> item in dictionary)
                    {
                        if (comparer.Equals(item.Key, key))
                            return item.Value;
                    }

                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// Tries to get a value from the provided <paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        /// <remarks><note>If <paramref name="dictionary"/> is neither an <see cref="IDictionary{TKey,TValue}"/>, nor an <see cref="IReadOnlyDictionary{TKey,TValue}"/> instance,
        /// then a sequential lookup is performed using a default equality comparer on the keys.</note></remarks>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "CS8600", Justification = "ReSharper does not tolerate 'out TValue? value'")]
        public static TValue GetValueOrDefault<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, Func<TValue> defaultValueFactory)
        {
            // null is actually tolerated but defaultValueFactory is not marked as nullable to avoid the confusing MaybeNull return value
            if (defaultValueFactory == null!)
                return dictionary.GetValueOrDefault(key, default(TValue)!);

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            switch (dictionary)
            {
                case IDictionary<TKey, TValue> dict:
                    return dict.TryGetValue(key, out TValue value) ? value : defaultValueFactory.Invoke();
                case IReadOnlyDictionary<TKey, TValue> dict:
                    return dict.TryGetValue(key, out value) ? value : defaultValueFactory.Invoke();
                default:
                    {
                        IEqualityComparer<TKey> comparer = ComparerHelper<TKey>.EqualityComparer;
                        foreach (KeyValuePair<TKey, TValue> item in dictionary)
                        {
                            if (comparer.Equals(item.Key, key))
                                return item.Value;
                        }

                        return defaultValueFactory.Invoke();
                    }
            }
        }
#endif

        /// <summary>
        /// Tries to get the typed value from a <paramref name="dictionary"/> for the given key.
        /// In this method <paramref name="defaultValue"/> can have a different type than <typeparamref name="TValue"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type is not compatible with <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "CS8600", Justification = "ReSharper does not tolerate 'out TValue? value'")]
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TActualValue defaultValue)
            where TActualValue : TValue
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            return dictionary.TryGetValue(key, out TValue value) && value is TActualValue actualValue ? actualValue : defaultValue;
        }

        /// <summary>
        /// Tries to get the typed value from a <paramref name="dictionary"/> for the given key.
        /// In this method <paramref name="defaultValueFactory"/> can return an instance of a different type than <typeparamref name="TValue"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "CS8600", Justification = "ReSharper does not tolerate 'out TValue? value'")]
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TActualValue> defaultValueFactory)
            where TActualValue : TValue
        {
            // null is actually tolerated but defaultValueFactory is not marked as nullable to avoid the confusing MaybeNull return value
            if (defaultValueFactory == null!)
                return dictionary.GetActualValueOrDefault(key, default(TActualValue)!);

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            return dictionary.TryGetValue(key, out TValue value) && value is TActualValue actualValue ? actualValue : defaultValueFactory.Invoke();
        }

#if !(NET35 || NET40)
        /// <summary>
        /// Tries to get the typed value from a <paramref name="dictionary"/> for the given key. Unlike in the <see cref="GetValueOrDefault{TKey,TValue}(IEnumerable{KeyValuePair{TKey,TValue}},TKey,TValue)"/> overload,
        /// here <paramref name="defaultValue"/> can have a different type than <typeparamref name="TValue"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type is not compatible with <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        /// <remarks><note>If <paramref name="dictionary"/> is neither an <see cref="IDictionary{TKey,TValue}"/>, nor an <see cref="IReadOnlyDictionary{TKey,TValue}"/> instance,
        /// then a sequential lookup is performed using a default equality comparer on the keys.</note></remarks>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "CS8600", Justification = "ReSharper does not tolerate 'out TValue? value'")]
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, TActualValue defaultValue)
            where TActualValue : TValue
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            switch (dictionary)
            {
                case IDictionary<TKey, TValue> dict:
                    {
                        return dict.TryGetValue(key, out TValue value) && value is TActualValue actualValue ? actualValue : defaultValue;
                    }
                case IReadOnlyDictionary<TKey, TValue> dict:
                    {
                        return dict.TryGetValue(key, out TValue value) && value is TActualValue actualValue ? actualValue : defaultValue;
                    }
                default:
                    {
                        IEqualityComparer<TKey> comparer = ComparerHelper<TKey>.EqualityComparer;
                        foreach (KeyValuePair<TKey, TValue> item in dictionary)
                        {
                            // allowing multiple keys with different type of values
                            if (comparer.Equals(item.Key, key) && item.Value is TActualValue actualValue)
                                return actualValue;
                        }

                        return defaultValue;
                    }
            }
        }

        /// <summary>
        /// Tries to get the typed value from a <paramref name="dictionary"/> for the given key. Unlike in the <see cref="GetValueOrDefault{TKey,TValue}(IEnumerable{KeyValuePair{TKey,TValue}},TKey,Func{TValue})"/> overload,
        /// here <paramref name="defaultValueFactory"/> can return an instance of a different type than <typeparamref name="TValue"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        /// <remarks><note>If <paramref name="dictionary"/> is neither an <see cref="IDictionary{TKey,TValue}"/>, nor an <see cref="IReadOnlyDictionary{TKey,TValue}"/> instance,
        /// then a sequential lookup is performed using a default equality comparer on the keys.</note></remarks>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "CS8600", Justification = "ReSharper does not tolerate 'out TValue? value'")]
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, Func<TActualValue> defaultValueFactory)
            where TActualValue : TValue
        {
            // null is actually tolerated but defaultValueFactory is not marked as nullable to avoid the confusing MaybeNull return value
            if (defaultValueFactory == null!)
                return dictionary.GetActualValueOrDefault(key, default(TActualValue)!);

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            switch (dictionary)
            {
                case IDictionary<TKey, TValue> dict:
                    {
                        return dict.TryGetValue(key, out TValue value) && value is TActualValue actualValue ? actualValue : defaultValueFactory.Invoke();
                    }
                case IReadOnlyDictionary<TKey, TValue> dict:
                    {
                        return dict.TryGetValue(key, out TValue value) && value is TActualValue actualValue ? actualValue : defaultValueFactory.Invoke();
                    }
                default:
                    {
                        IEqualityComparer<TKey> comparer = ComparerHelper<TKey>.EqualityComparer;
                        foreach (KeyValuePair<TKey, TValue> item in dictionary)
                        {
                            // allowing multiple keys with different type of values
                            if (comparer.Equals(item.Key, key) && item.Value is TActualValue actualValue)
                                return actualValue;
                        }

                        return defaultValueFactory.Invoke();
                    }
            }
        }

#endif

        /// <summary>
        /// Tries to get the typed value from a <see cref="string">string</see>-<see cref="object">object</see>&#160;<paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type is not compatible with <typeparamref name="TActualValue"/> This parameter is optional.
        /// <br/>Default value: <see langword="null"/>&#160;if <typeparamref name="TActualValue"/> is a reference type; otherwise, the bitwise zero value of <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        /// <example>
        /// The following example demonstrates how to use the <see cref="O:KGySoft.CoreLibraries.DictionaryExtensions.GetValueOrDefault">GetValueOrDefault</see> overloads.
        /// <note type="tip">Try also <a href="https://dotnetfiddle.net/GKSif4" target="_blank">online</a>.</note>
        /// <code lang="C#"><![CDATA[
        /// using System;
        /// using System.Collections.Generic;
        /// using KGySoft.CoreLibraries;
        /// 
        /// public class Example
        /// {
        ///     public static void Main()
        ///     {
        ///         var dict = new Dictionary<string, object>
        ///         {
        ///             { "Int", 42 },
        ///             { "String", "Blah" },
        ///         };
        /// 
        ///         // old way:
        ///         object obj;
        ///         int intValue;
        ///         if (dict.TryGetValue("Int", out obj) && obj is int)
        ///             intValue = (int)obj;
        /// 
        ///         // C# 7.0 way:
        ///         if (dict.TryGetValue("Int", out object o) && o is int i)
        ///             intValue = i;
        /// 
        ///         // GetValueOrDefault ways:
        /// 
        ///         // TValue return type (which is object now)
        ///         intValue = (int)dict.GetValueOrDefault("Int");
        /// 
        ///         // by defining a default value the actual type (and return type) can be specified
        ///         intValue = dict.GetValueOrDefault("Int", 0);
        ///
        ///         // an alternative syntax for string-object dictionaries (a default value still can be specified)
        ///         intValue = dict.GetValueOrDefault<int>("Int");
        ///
        ///         // to use different actual (and return) type for non string-object dictionaries use the GetActualValueOrDefault method:
        ///         intValue = dict.GetActualValueOrDefault("Int", 0);
        /// 
        ///         // using nullable int actual type to get null if "Unknown" does not exist or is not an int
        ///         int? intOrNull = dict.GetValueOrDefault<int?>("Unknown");
        ///
        ///         // If obtaining a default value is an expensive operation you can use the delegate overloads.
        ///         // The delegate is invoked only when the key was not found in the dictionary:
        ///         intValue = (int)dict.GetValueOrDefault("Unknown", () =>
        ///         {
        ///             Console.WriteLine("Default value factory was invoked from GetValueOrDefault");
        ///             return -1;
        ///         });
        ///
        ///		   // Which is the same as:
        ///        intValue = dict.GetActualValueOrDefault("Unknown", () =>
        ///        {
        ///            Console.WriteLine("Default value factory was invoked from GetActualValueOrDefault");
        ///            return -1;
        ///        });
        /// 
        ///        Console.WriteLine($"{nameof(intValue)}: {intValue}; {nameof(intOrNull)}: {intOrNull}");
        ///     }
        /// }]]></code>
        /// </example>
        public static TActualValue GetValueOrDefault<TActualValue>(
#nullable disable // workaround for accepting both IDictionary<string, object?> and IDictionary<string, object>
            [DisallowNull]this IDictionary<string, object> dictionary,
#nullable restore
            string key, TActualValue defaultValue = default)
        {
#nullable disable // just for ReSharper to suppress "Nullability of type argument 'TActualValue' must match constraint type 'object'" - TODO: delete when possible
            return dictionary.GetActualValueOrDefault(key, defaultValue!);
#nullable restore
        }

#if !(NET35 || NET40)
        /// <summary>
        /// Tries to get the typed value from a <see cref="string">string</see>-<see cref="object">object</see>&#160;<paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type is not compatible with <typeparamref name="TActualValue"/> This parameter is optional.
        /// <br/>Default value: <see langword="null"/>&#160;if <typeparamref name="TActualValue"/> is a reference type; otherwise, the bitwise zero value of <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        /// <remarks><note>If <paramref name="dictionary"/> is neither an <see cref="IDictionary{TKey,TValue}"/>, nor an <see cref="IReadOnlyDictionary{TKey,TValue}"/> instance,
        /// then a sequential lookup is performed using a default equality comparer on the keys.</note></remarks>
        public static TActualValue GetValueOrDefault<TActualValue>(
#nullable disable // workaround for accepting both IEnumerable<KeyValuePair<string, object?>> and IEnumerable<KeyValuePair<string, object>>
            [DisallowNull]this IEnumerable<KeyValuePair<string, object>> dictionary,
#nullable restore
            string key, TActualValue defaultValue = default)
        {
#nullable disable // just for ReSharper to suppress "Nullability of type argument 'TActualValue' must match constraint type 'object'" - TODO: delete when possible
            return dictionary.GetActualValueOrDefault(key, defaultValue!);
#nullable restore
        }
#endif

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
        public static LockingDictionary<TKey, TValue> AsThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TKey : notnull
            => new LockingDictionary<TKey, TValue>(dictionary);

        public static ThreadSafeDictionary<TKey, TValue> ToThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TKey : notnull
            => new ThreadSafeDictionary<TKey, TValue>(dictionary);

        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
            {
                if (dictionary.TryGetValue(key, out TValue? oldValue))
                {
                    TValue newValue = updateValueFactory.Invoke(key, oldValue);
                    dictionary[key] = newValue;
                    return newValue;
                }

                dictionary[key] = addValue;
                return addValue;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (updateValueFactory == null!)
                Throw.ArgumentNullException(nameof(updateValueFactory));

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.AddOrUpdate(key, addValue, updateValueFactory);
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.AddOrUpdate(key, addValue, updateValueFactory);
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, addValue, updateValueFactory);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, addValue, updateValueFactory);
            }
        }

        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
            {
                if (dictionary.TryGetValue(key, out TValue? oldValue))
                {
                    TValue newValue = updateValueFactory.Invoke(key, oldValue);
                    dictionary[key] = newValue;
                    return newValue;
                }

                TValue result = addValueFactory.Invoke(key);
                dictionary[key] = result;
                return result;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (addValueFactory == null!)
                Throw.ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory == null!)
                Throw.ArgumentNullException(nameof(updateValueFactory));

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.AddOrUpdate(key, addValueFactory, updateValueFactory);
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.AddOrUpdate(key, addValueFactory, updateValueFactory);
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, addValueFactory, updateValueFactory);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, addValueFactory, updateValueFactory);
            }
        }

        public static TValue AddOrUpdate<TKey, TValue, TArg>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key,
                Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
            {
                if (dictionary.TryGetValue(key, out TValue? oldValue))
                {
                    TValue newValue = updateValueFactory.Invoke(key, oldValue, factoryArgument);
                    dictionary[key] = newValue;
                    return newValue;
                }

                TValue result = addValueFactory.Invoke(key, factoryArgument);
                dictionary[key] = result;
                return result;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (addValueFactory == null!)
                Throw.ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory == null!)
                Throw.ArgumentNullException(nameof(updateValueFactory));

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.AddOrUpdate(key, addValueFactory, updateValueFactory, factoryArgument);
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.AddOrUpdate(key, addValueFactory, updateValueFactory, factoryArgument);
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, addValueFactory, updateValueFactory, factoryArgument);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, addValueFactory, updateValueFactory, factoryArgument);
            }
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue)
            {
                if (dictionary.TryGetValue(key, out TValue? value))
                    return value;

                dictionary[key] = addValue;
                return addValue;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.GetOrAdd(key, addValue);
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.GetOrAdd(key, addValue);
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, addValue);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, addValue);
            }
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> addValueFactory)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> addValueFactory)
            {
                if (dictionary.TryGetValue(key, out TValue? value))
                    return value;

                TValue result = addValueFactory.Invoke(key);
                dictionary[key] = result;
                return result;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (addValueFactory == null!)
                Throw.ArgumentNullException(nameof(addValueFactory));

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.GetOrAdd(key, addValueFactory);
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.GetOrAdd(key, addValueFactory);
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, addValueFactory);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, addValueFactory);
            }
        }

        public static TValue GetOrAdd<TKey, TValue, TArg>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TArg, TValue> addValueFactory, TArg factoryArgument)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key,
                Func<TKey, TArg, TValue> addValueFactory, TArg factoryArgument)
            {
                if (dictionary.TryGetValue(key, out TValue? value))
                    return value;

                TValue result = addValueFactory.Invoke(key, factoryArgument);
                dictionary[key] = result;
                return result;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (addValueFactory == null!)
                Throw.ArgumentNullException(nameof(addValueFactory));

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.GetOrAdd(key, addValueFactory, factoryArgument);
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.GetOrAdd(key, addValueFactory, factoryArgument);
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, addValueFactory, factoryArgument);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, addValueFactory, factoryArgument);
            }
        }

        #endregion
    }
}
