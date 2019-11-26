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
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            => GetValueOrDefault(dictionary, key, default(TValue));

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
            if (dictionary == null)
                Throw.ArgumentNullException(Argument.dictionary);

            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        /// <summary>
        /// Tries to get a value from a <paramref name="dictionary"/> for the given key.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="GetValueOrDefault{TActualValue}(IDictionary{string,object},string,TActualValue)"/> method for some examples.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.
        /// If <see langword="null"/>, then the default value of the <typeparamref name="TValue"/> type will be returned for a non-existing <paramref name="key"/>.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValueFactory)
        {
            if (defaultValueFactory == null)
                return dictionary.GetValueOrDefault(key, default(TValue));

            if (dictionary == null)
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
        public static TValue GetValueOrDefault<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key)
            => GetValueOrDefault(dictionary, key, default(TValue));

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
            if (dictionary == null)
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
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.
        /// If <see langword="null"/>, then the default value of the <typeparamref name="TValue"/> type will be returned for a non-existing <paramref name="key"/>.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        /// <remarks><note>If <paramref name="dictionary"/> is neither an <see cref="IDictionary{TKey,TValue}"/>, nor an <see cref="IReadOnlyDictionary{TKey,TValue}"/> instance,
        /// then a sequential lookup is performed using a default equality comparer on the keys.</note></remarks>
        public static TValue GetValueOrDefault<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, Func<TValue> defaultValueFactory)
        {
            if (defaultValueFactory == null)
                return dictionary.GetValueOrDefault(key, default(TValue));

            if (dictionary == null)
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
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TActualValue defaultValue)
            where TActualValue : TValue
        {
            if (dictionary == null)
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
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.
        /// If <see langword="null"/>, then the default value of the <typeparamref name="TActualValue"/> type will be returned for a non-existing <paramref name="key"/>.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TActualValue> defaultValueFactory)
            where TActualValue : TValue
        {
            if (defaultValueFactory == null)
                return dictionary.GetActualValueOrDefault(key, default(TActualValue));

            if (dictionary == null)
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
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, TActualValue defaultValue)
            where TActualValue : TValue
        {
            if (dictionary == null)
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
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.
        /// If <see langword="null"/>, then the default value of the <typeparamref name="TActualValue"/> type will be returned for a non-existing <paramref name="key"/>.</param>
        /// <typeparam name="TKey">The type of the stored keys in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TValue">Type of the stored values in the <paramref name="dictionary"/>.</typeparam>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the <paramref name="dictionary"/>.</returns>
        /// <remarks><note>If <paramref name="dictionary"/> is neither an <see cref="IDictionary{TKey,TValue}"/>, nor an <see cref="IReadOnlyDictionary{TKey,TValue}"/> instance,
        /// then a sequential lookup is performed using a default equality comparer on the keys.</note></remarks>
        public static TActualValue GetActualValueOrDefault<TKey, TValue, TActualValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, Func<TActualValue> defaultValueFactory)
            where TActualValue : TValue
        {
            if (defaultValueFactory == null)
                return dictionary.GetActualValueOrDefault(key, default(TActualValue));

            if (dictionary == null)
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
        /// Tries to get the typed value from a <see cref="string"/>-<see cref="object"/>&#160;<paramref name="dictionary"/> for the given key.
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
        /// <note type="tip">Try also <a href="https://dotnetfiddle.net/XDjrOB" target="_blank">online</a>.</note>
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
        ///         intValue = dict.GetValueOrDefault("Unknown", () =>
        ///         {
        ///             Console.WriteLine("Default value factory was invoked");
        ///             return -1;
        ///         });
        /// 
        ///         Console.WriteLine($"{nameof(intValue)}: {intValue}; {nameof(intOrNull)}: {intOrNull}");
        ///     }
        /// }]]></code>
        /// </example>
        public static TActualValue GetValueOrDefault<TActualValue>(this IDictionary<string, object> dictionary, string key, TActualValue defaultValue = default)
            => dictionary.GetActualValueOrDefault(key, defaultValue);

#if !(NET35 || NET40)
        /// <summary>
        /// Tries to get the typed value from a <see cref="string"/>-<see cref="object"/>&#160;<paramref name="dictionary"/> for the given key.
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
        public static TActualValue GetValueOrDefault<TActualValue>(this IEnumerable<KeyValuePair<string, object>> dictionary, string key, TActualValue defaultValue = default)
            => dictionary.GetActualValueOrDefault(key, defaultValue);
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
        public static LockingDictionary<TKey, TValue> AsThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => new LockingDictionary<TKey, TValue>(dictionary);

        #endregion
    }
}
