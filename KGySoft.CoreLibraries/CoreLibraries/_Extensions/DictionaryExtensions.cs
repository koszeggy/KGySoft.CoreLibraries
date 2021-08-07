#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DictionaryExtensions.cs
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
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using KGySoft.Collections;

#endregion

#region Suppressions

#if NETFRAMEWORK || NETSTANDARD2_0
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif
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
        public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
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
        public static TValue? GetValueOrDefault<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key)
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
        public static TValue GetValueOrDefault<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            switch (dictionary)
            {
                case IDictionary<TKey, TValue> dict:
                    return dict.TryGetValue(key, out TValue? value) ? value : defaultValue;
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
                    return dict.TryGetValue(key, out TValue? value) ? value : defaultValueFactory.Invoke();
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
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TActualValue defaultValue)
            where TActualValue : TValue
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            return dictionary.TryGetValue(key, out TValue? value) && value is TActualValue actualValue ? actualValue : defaultValue;
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
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TActualValue> defaultValueFactory)
            where TActualValue : TValue
        {
            // null is actually tolerated but defaultValueFactory is not marked as nullable to avoid the confusing MaybeNull return value
            if (defaultValueFactory == null!)
                return dictionary.GetActualValueOrDefault(key, default(TActualValue)!);

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            return dictionary.TryGetValue(key, out TValue? value) && value is TActualValue actualValue ? actualValue : defaultValueFactory.Invoke();
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
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, TActualValue defaultValue)
            where TActualValue : TValue
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            switch (dictionary)
            {
                case IDictionary<TKey, TValue> dict:
                    {
                        return dict.TryGetValue(key, out TValue? value) && value is TActualValue actualValue ? actualValue : defaultValue;
                    }
                case IReadOnlyDictionary<TKey, TValue> dict:
                    {
                        return dict.TryGetValue(key, out TValue? value) && value is TActualValue actualValue ? actualValue : defaultValue;
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
                        return dict.TryGetValue(key, out TValue? value) && value is TActualValue actualValue ? actualValue : defaultValueFactory.Invoke();
                    }
                case IReadOnlyDictionary<TKey, TValue> dict:
                    {
                        return dict.TryGetValue(key, out TValue? value) && value is TActualValue actualValue ? actualValue : defaultValueFactory.Invoke();
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
            string key, TActualValue defaultValue = default!)
        {
#nullable disable // just for ReSharper to suppress "Nullability of type argument 'TActualValue' must match constraint type 'object'"
            return dictionary.GetActualValueOrDefault(key, defaultValue);
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
            string key, TActualValue defaultValue = default!)
        {
#nullable disable // just for ReSharper to suppress "Nullability of type argument 'TActualValue' must match constraint type 'object'"
            return dictionary.GetActualValueOrDefault(key, defaultValue);
#nullable restore
        }
#endif

        /// <summary>
        /// Returns a <see cref="LockingDictionary{TKey,TValue}"/>, which provides a thread-safe wrapper for the specified <paramref name="dictionary"/>.
        /// This only means that if the members are accessed through the returned <see cref="LockingDictionary{TKey,TValue}"/>, then the inner state of the wrapped dictionary remains always consistent and not that all of the multi-threading concerns can be ignored.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="LockingDictionary{TKey,TValue}"/> class for details and some examples.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The dictionary to create a thread-safe wrapper for.</param>
        /// <returns>A <see cref="LockingDictionary{TKey,TValue}"/>, which provides a thread-safe wrapper for the specified <paramref name="dictionary"/>.</returns>
        /// <remarks>
        /// <para>To use a thread-safe dictionary without wrapping any <see cref="IDictionary{TKey,TValue}"/> instance consider to use the <see cref="ThreadSafeDictionary{TKey,TValue}"/> class instead.</para>
        /// <para>For a <see cref="Cache{TKey,TValue}"/> instance consider to use the <see cref="Cache{TKey,TValue}.GetThreadSafeAccessor">GetThreadSafeAccessor</see> method instead, which does not necessarily lock the item loader delegate.</para>
        /// </remarks>
        public static LockingDictionary<TKey, TValue> AsThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TKey : notnull
            => new LockingDictionary<TKey, TValue>(dictionary);

        /// <summary>
        /// Tries to add a pair of key and value the specified <paramref name="dictionary"/>. The operation is thread safe if <paramref name="dictionary"/>
        /// is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/> or <see cref="LockingDictionary{TKey,TValue}"/> instance.
        /// For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="item">The <see cref="KeyValuePair{TKey,TValue}"/> to add to the <paramref name="dictionary"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> was added to the <paramref name="dictionary"/> successfully;
        /// <see langword="false"/>&#160;if the key already exists or when <see cref="ICollection{T}.IsReadOnly"/> returns <see langword="true"/>&#160;for <paramref name="dictionary"/>.</returns>
        /// <remarks>
        /// <para>The <see cref="System.Collections.Generic.CollectionExtensions"/> class in .NET Core 2.0 and above also has
        /// a <see cref="System.Collections.Generic.CollectionExtensions.TryAdd{TKey,TValue}">TryAdd</see> method that behaves somewhat differently.
        /// To avoid ambiguity with that method this one has a <see cref="KeyValuePair{TKey,TValue}"/> parameter.</para>
        /// <para>Unlike the <see cref="System.Collections.Generic.CollectionExtensions.TryAdd{TKey,TValue}">CollectionExtensions.TryAdd</see> method, this one
        /// is thread safe when used with <see cref="ConcurrentDictionary{TKey,TValue}"/>, <see cref="ThreadSafeDictionary{TKey,TValue}"/> and <see cref="LockingDictionary{TKey,TValue}"/> instances.
        /// Additionally, this one returns <see langword="false"/>&#160;if <paramref name="dictionary"/> is read-only instead of throwing an exception.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="item"/> has a <see langword="null"/> key.</exception>
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> item)
            where TKey : notnull
        {
            #region Local Methods

            static bool Fallback(IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
            {
                if (dictionary.IsReadOnly || dictionary.ContainsKey(key))
                    return false;
                dictionary[key] = value;
                return true;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (item.Key == null!)
                Throw.ArgumentException(Argument.item, Res.PropertyNull(nameof(item.Key)));

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.TryAdd(item.Key, item.Value);
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.TryAdd(item.Key, item.Value);
#endif
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
                case Dictionary<TKey, TValue> dict:
                    return dict.TryAdd(item.Key, item.Value);
#endif
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, item.Key, item.Value);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, item.Key, item.Value);
            }
        }

        /// <summary>
        /// Tries to update the value associated with <paramref name="key"/> to <paramref name="newValue"/> if the existing value with <paramref name="key"/>
        /// is equal to <paramref name="originalValue"/>. The operation is thread safe if <paramref name="dictionary"/>
        /// is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/> or <see cref="LockingDictionary{TKey,TValue}"/> instance.
        /// For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key of the item to replace.</param>
        /// <param name="newValue">The replacement value of <paramref name="key"/> if its value equals to <paramref name="originalValue"/>.</param>
        /// <param name="originalValue">The expected original value of the stored item with the associated <paramref name="key"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="dictionary"/> is not read-only and the value with <paramref name="key"/>
        /// was equal to <paramref name="originalValue"/> and was replaced with <paramref name="newValue"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
        public static bool TryUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue newValue, TValue originalValue)
            where TKey : notnull
        {
            #region Local Methods

            static bool Fallback(IDictionary<TKey, TValue> dictionary, TKey key, TValue newValue, TValue originalValue)
            {
                if (dictionary.IsReadOnly
                    || !dictionary.TryGetValue(key, out TValue? oldValue)
                    || !ComparerHelper<TValue>.EqualityComparer.Equals(oldValue, originalValue))
                {
                    return false;
                }

                dictionary[key] = newValue;
                return true;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.TryUpdate(key, newValue, originalValue);
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.TryUpdate(key, newValue, originalValue);
#endif
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, newValue, originalValue);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, newValue, originalValue);
            }
        }

        /// <summary>
        /// Adds or updates a key/value pair in the <paramref name="dictionary"/> based on whether the specified <paramref name="key"/> already exists.
        /// The operation is thread safe if <paramref name="dictionary"/> is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or <see cref="LockingDictionary{TKey,TValue}"/> instance. For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key to be added or whose value should be updated.</param>
        /// <param name="addValue">The value to be added for an absent key.</param>
        /// <param name="updateValue">The value to be set for an existing key.</param>
        /// <returns>The new value for the <paramref name="key"/>. This will be either <paramref name="addValue"/> (if the key was absent)
        /// or <paramref name="updateValue"/> (if the key was present).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only.</exception>
        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue, TValue updateValue)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue, TValue updateValue)
            {
                if (dictionary.IsReadOnly)
                    Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = updateValue;
                    return updateValue;
                }

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
                    return tDict.AddOrUpdate(key, addValue, updateValue);
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.AddOrUpdate(key, addValue, (_, _) => updateValue);
#endif
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, addValue, updateValue);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, addValue, updateValue);
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <paramref name="dictionary"/> if the <paramref name="key"/> does not already exist,
        /// or updates a key/value pair in the <paramref name="dictionary"/> by using the specified <paramref name="updateValueFactory"/> if the <paramref name="key"/> already exists.
        /// The operation is thread safe if <paramref name="dictionary"/> is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or <see cref="LockingDictionary{TKey,TValue}"/> instance. For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key to be added or whose value should be updated.</param>
        /// <param name="addValue">The value to be added for an absent key.</param>
        /// <param name="updateValueFactory">A delegate used to generate a new value for an existing key based on the key's existing value.</param>
        /// <returns>The new value for the <paramref name="key"/>. This will be either <paramref name="addValue"/> (if the key was absent)
        /// or the result of <paramref name="updateValueFactory"/> (if the key was present).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/>, <paramref name="key"/> or <paramref name="updateValueFactory"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only.</exception>
        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
            {
                if (dictionary.IsReadOnly)
                    Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
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
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.AddOrUpdate(key, addValue, updateValueFactory);
#endif
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

        /// <summary>
        /// Uses the specified delegates to add a key/value pair to the <paramref name="dictionary"/> if the <paramref name="key"/> does not already exist,
        /// or to update a key/value pair in the <paramref name="dictionary"/> if the <paramref name="key"/> already exists.
        /// The operation is thread safe if <paramref name="dictionary"/> is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or <see cref="LockingDictionary{TKey,TValue}"/> instance. For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key to be added or whose value should be updated.</param>
        /// <param name="addValueFactory">A delegate used to generate a value for an absent key.</param>
        /// <param name="updateValueFactory">A delegate used to generate a new value for an existing key based on the key's existing value.</param>
        /// <returns>The new value for the <paramref name="key"/>. This will be either the result of <paramref name="addValueFactory"/> (if the key was absent)
        /// or the result of <paramref name="updateValueFactory"/> (if the key was present).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/>, <paramref name="key"/>, <paramref name="addValueFactory"/> or <paramref name="updateValueFactory"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only.</exception>
        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
            {
                if (dictionary.IsReadOnly)
                    Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
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
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.AddOrUpdate(key, addValueFactory, updateValueFactory);
#endif
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

        /// <summary>
        /// Uses the specified delegates to add a key/value pair to the <paramref name="dictionary"/> if the <paramref name="key"/> does not already exist,
        /// or to update a key/value pair in the <paramref name="dictionary"/> if the <paramref name="key"/> already exists.
        /// The operation is thread safe if <paramref name="dictionary"/> is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or <see cref="LockingDictionary{TKey,TValue}"/> instance. For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TArg">The type of an argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key to be added or whose value should be updated.</param>
        /// <param name="addValueFactory">A delegate used to generate a value for an absent key.</param>
        /// <param name="updateValueFactory">A delegate used to generate a new value for an existing key based on the key's existing value.</param>
        /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/>.</param>
        /// <returns>The new value for the <paramref name="key"/>. This will be either the result of <paramref name="addValueFactory"/> (if the key was absent)
        /// or the result of <paramref name="updateValueFactory"/> (if the key was present).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/>, <paramref name="key"/>, <paramref name="addValueFactory"/> or <paramref name="updateValueFactory"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only.</exception>
        public static TValue AddOrUpdate<TKey, TValue, TArg>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key,
                Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
            {
                if (dictionary.IsReadOnly)
                    Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
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
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.AddOrUpdate(key, addValueFactory, updateValueFactory, factoryArgument);
#endif
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

        /// <summary>
        /// Adds a key/value pair to the <paramref name="dictionary"/> if the key does not already exist, and returns either the added or the existing value.
        /// The operation is thread safe if <paramref name="dictionary"/> is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or <see cref="LockingDictionary{TKey,TValue}"/> instance. For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key of the element to add or whose value should be returned.</param>
        /// <param name="addValue">The value to be added, if the key does not already exist.</param>
        /// <returns>The value for the key. This will be either the existing value for the <paramref name="key"/> if the key is already in the dictionary,
        /// or the specified <paramref name="addValue"/> if the key was not in the dictionary.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="key"/> was not present in the <paramref name="dictionary"/>, which is read-only.</exception>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, TValue addValue)
            {
                if (dictionary.TryGetValue(key, out TValue? value))
                    return value;

                if (dictionary.IsReadOnly)
                    Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
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
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.GetOrAdd(key, addValue);
#endif
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

        /// <summary>
        /// Adds a key/value pair to the <paramref name="dictionary"/> by using the specified <paramref name="addValueFactory"/>
        /// if the key does not already exist, and returns either the added or the existing value.
        /// The operation is thread safe if <paramref name="dictionary"/> is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or <see cref="LockingDictionary{TKey,TValue}"/> instance. For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key of the element to add or whose value should be returned.</param>
        /// <param name="addValueFactory">The delegate to be used to generate the value, if the key does not already exist.</param>
        /// <returns>The value for the key. This will be either the existing value for the <paramref name="key"/> if the key is already in the dictionary,
        /// or the result of the specified <paramref name="addValueFactory"/> if the key was not in the dictionary.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/>, <paramref name="key"/> or <paramref name="addValueFactory"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="key"/> was not present in the <paramref name="dictionary"/>, which is read-only.</exception>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> addValueFactory)
            where TKey : notnull
        {
            #region Local Methods

            static TValue Fallback(IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> addValueFactory)
            {
                if (dictionary.TryGetValue(key, out TValue? value))
                    return value;

                if (dictionary.IsReadOnly)
                    Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
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
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.GetOrAdd(key, addValueFactory);
#endif
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

        /// <summary>
        /// Adds a key/value pair to the <paramref name="dictionary"/> by using the specified <paramref name="addValueFactory"/>
        /// if the key does not already exist, and returns either the added or the existing value.
        /// The operation is thread safe if <paramref name="dictionary"/> is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or <see cref="LockingDictionary{TKey,TValue}"/> instance. For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TArg">The type of an argument to pass into <paramref name="addValueFactory"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">The key of the element to add or whose value should be returned.</param>
        /// <param name="addValueFactory">The delegate to be used to generate the value, if the key does not already exist.</param>
        /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory"/>.</param>
        /// <returns>The value for the key. This will be either the existing value for the <paramref name="key"/> if the key is already in the dictionary,
        /// or the result of the specified <paramref name="addValueFactory"/> if the key was not in the dictionary.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/>, <paramref name="key"/> or <paramref name="addValueFactory"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="key"/> was not present in the <paramref name="dictionary"/>, which is read-only.</exception>
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
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.GetOrAdd(key, addValueFactory, factoryArgument);
#endif
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

        /// <summary>
        /// Tries to remove the value with the specified <paramref name="key"/> from the specified <paramref name="dictionary"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">Key of the item to remove.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="dictionary"/> is not read-only and the element is successfully removed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return !dictionary.IsReadOnly && dictionary.Remove(key);
        }

        /// <summary>
        /// Tries to remove and return the <paramref name="value"/> with the specified <paramref name="key"/> from the specified <paramref name="dictionary"/>.
        /// The operation is thread safe if <paramref name="dictionary"/> is a <see cref="ThreadSafeDictionary{TKey,TValue}"/>, <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or <see cref="LockingDictionary{TKey,TValue}"/> instance. For other <see cref="IDictionary{TKey,TValue}"/> implementations the caller should care about thread safety if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values in the <paramref name="dictionary"/>.</typeparam>
        /// <param name="dictionary">The target dictionary.</param>
        /// <param name="key">Key of the item to remove.</param>
        /// <param name="value">When this method returns, contains the value removed from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>,
        /// or the default value of the <typeparamref name="TValue"/> type if <paramref name="dictionary"/> is read-only or <paramref name="key"/> does not exist.</param>
        /// <returns><see langword="true"/>&#160;if the element is successfully removed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
        public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)]out TValue value)
            where TKey : notnull
        {
            #region Local Methods

            static bool Fallback(IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)]out TValue value)
            {
                if (!dictionary.IsReadOnly && dictionary.TryGetValue(key, out TValue? removedValue) && dictionary.Remove(key))
                {
                    value = removedValue;
                    return true;
                }

                value = default;
                return false;
            }

            #endregion

            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            switch (dictionary)
            {
                case ThreadSafeDictionary<TKey, TValue> tDict:
                    return tDict.TryRemove(key, out value);
#if !NET35
                case ConcurrentDictionary<TKey, TValue> cDict:
                    return cDict.TryRemove(key, out value);
#endif
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
                case Dictionary<TKey, TValue> dict:
                    return dict.Remove(key, out value);
#endif
                case LockingDictionary<TKey, TValue> lDict:
                    lDict.Lock();
                    try
                    {
                        return Fallback(lDict, key, out value);
                    }
                    finally
                    {
                        lDict.Unlock();
                    }

                default:
                    return Fallback(dictionary, key, out value);
            }
        }

        #endregion
    }
}
