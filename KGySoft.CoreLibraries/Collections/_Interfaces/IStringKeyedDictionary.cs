#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IStringKeyedDictionary.cs
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
#if NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

    /// <summary>
    /// Represents an <see cref="IDictionary{TKey,TValue}"/> with <see cref="string">string</see> key
    /// that can be queried also by <see cref="StringSegment"/> and <see cref="ReadOnlySpan{T}"/>
    /// (in .NET Core 2.1/.NET Standard 2.1 and above) instances.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <seealso cref="string" />
    public interface IStringKeyedDictionary<TValue> : IDictionary<string, TValue>
    {
        #region Indexers

        /// <summary>
        /// Gets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// The element with the specified <paramref name="key"/>.
        /// </returns>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see cref="StringSegment.Null">StringSegment.Null</see>.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="key"/> is not found.</exception>
        TValue this[StringSegment key] { get; }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Gets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// The element with the specified <paramref name="key"/>.
        /// </returns>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="KeyNotFoundException"><paramref name="key"/> is not found.</exception>
        /// <remarks><note>This member is available only in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        TValue this[ReadOnlySpan<char> key] { get; }
#endif

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether this instance contains an element with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>, if the dictionary contains an element with the <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key to locate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see cref="StringSegment.Null">StringSegment.Null</see>.</exception>
        bool ContainsKey(StringSegment key);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Determines whether this instance contains an element with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>, if the dictionary contains an element with the <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key to locate.</param>
        /// <remarks><note>This member is available only in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        bool ContainsKey(ReadOnlySpan<char> key);
#endif

        /// <summary>
        /// Tries to get the <paramref name="value"/> associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if the dictionary contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see cref="StringSegment.Null">StringSegment.Null</see>.</exception>
        bool TryGetValue(StringSegment key, [MaybeNullWhen(false)]out TValue value);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Tries to get the <paramref name="value"/> associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if the dictionary contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <remarks><note>This member is available only in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value);
#endif

        /// <summary>
        /// Tries to get the value from the dictionary for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <returns>The found value or the default value of <typeparamref name="TValue"/> if <paramref name="key"/> was not found in the dictionary.</returns>
        TValue? GetValueOrDefault(string key);

        /// <summary>
        /// Tries to get the typed value from the dictionary for the given <paramref name="key"/>.
        /// The <paramref name="defaultValue"/> parameter can have a more specific type than <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type
        /// is not compatible with <typeparamref name="TActualValue"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>&#160;if <typeparamref name="TActualValue"/> is a reference type; otherwise, the bitwise zero value of <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        TActualValue GetValueOrDefault<TActualValue>(string key, TActualValue defaultValue = default!) where TActualValue : TValue;

        /// <summary>
        /// Tries to get the typed value from the dictionary for the given <paramref name="key"/>.
        /// The <paramref name="defaultValueFactory"/> can return an instance of a more specific type than <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.</param>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the dictionary.</returns>
        TActualValue GetValueOrDefault<TActualValue>(string key, Func<TActualValue> defaultValueFactory) where TActualValue : TValue;

        /// <summary>
        /// Tries to get the value from the dictionary for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <returns>The found value or the default value of <typeparamref name="TValue"/> if <paramref name="key"/> was not found in the dictionary.</returns>
        TValue? GetValueOrDefault(StringSegment key);

        /// <summary>
        /// Tries to get the typed value from the dictionary for the given <paramref name="key"/>.
        /// The <paramref name="defaultValue"/> parameter can have a more specific type than <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type
        /// is not compatible with <typeparamref name="TActualValue"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>&#160;if <typeparamref name="TActualValue"/> is a reference type; otherwise, the bitwise zero value of <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        TActualValue GetValueOrDefault<TActualValue>(StringSegment key, TActualValue defaultValue = default!) where TActualValue : TValue;

        /// <summary>
        /// Tries to get the typed value from the dictionary for the given <paramref name="key"/>.
        /// The <paramref name="defaultValueFactory"/> can return an instance of a more specific type than <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.</param>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the dictionary.</returns>
        TActualValue GetValueOrDefault<TActualValue>(StringSegment key, Func<TActualValue> defaultValueFactory) where TActualValue : TValue;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Tries to get the value from the dictionary for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <returns>The found value or the default value of <typeparamref name="TValue"/> if <paramref name="key"/> was not found in the dictionary.</returns>
        /// <remarks><note>This member is available only in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        TValue? GetValueOrDefault(ReadOnlySpan<char> key);

        /// <summary>
        /// Tries to get the typed value from the dictionary for the given <paramref name="key"/>.
        /// The <paramref name="defaultValue"/> parameter can have a more specific type than <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="key"/> was not found or its actual type
        /// is not compatible with <typeparamref name="TActualValue"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>&#160;if <typeparamref name="TActualValue"/> is a reference type; otherwise, the bitwise zero value of <typeparamref name="TActualValue"/>.</param>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="key"/> was not found or its value cannot be cast to <typeparamref name="TActualValue"/>.</returns>
        /// <remarks><note>This member is available only in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        TActualValue GetValueOrDefault<TActualValue>(ReadOnlySpan<char> key, TActualValue defaultValue = default!) where TActualValue : TValue;

        /// <summary>
        /// Tries to get the typed value from the dictionary for the given <paramref name="key"/>.
        /// The <paramref name="defaultValueFactory"/> can return an instance of a more specific type than <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="defaultValueFactory">A delegate that can be invoked to return a default value if <paramref name="key"/> was not found.</param>
        /// <typeparam name="TActualValue">The type of the value with the corresponding <paramref name="key"/> to get.</typeparam>
        /// <returns>The found value or the result of <paramref name="defaultValueFactory"/> if <paramref name="key"/> was not found in the dictionary.</returns>
        /// <remarks><note>This member is available only in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        TActualValue GetValueOrDefault<TActualValue>(ReadOnlySpan<char> key, Func<TActualValue> defaultValueFactory) where TActualValue : TValue;
#endif

        #endregion
    }
}