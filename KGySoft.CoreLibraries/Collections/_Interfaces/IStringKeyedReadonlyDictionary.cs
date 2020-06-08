#if !(NET35 || NET40)
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IStringKeyedReadOnlyDictionary.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents an <see cref="IReadOnlyDictionary{TKey,TValue}"/> with <see cref="string">string</see> key
    /// that can be queried also by <see cref="StringSegment"/> and <see cref="ReadOnlySpan{T}"/>
    /// (in .NET Core 3.0/.NET Standard 2.1 and above) instances.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <remarks>
    /// <note>This type is not available in .NET 4.0 and lover versions.</note>
    /// </remarks>
    public interface IStringKeyedReadOnlyDictionary<TValue> : IReadOnlyDictionary<string, TValue>
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

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        /// <summary>
        /// Gets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// The element with the specified <paramref name="key"/>.
        /// </returns>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="KeyNotFoundException"><paramref name="key"/> is not found.</exception>
        /// <remarks><note>This member is available only in .NET Core 3.0/.NET Standard 2.1 and above.</note></remarks>
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

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        /// <summary>
        /// Determines whether this instance contains an element with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>, if the dictionary contains an element with the <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key to locate.</param>
        /// <remarks><note>This member is available only in .NET Core 3.0/.NET Standard 2.1 and above.</note></remarks>
        bool ContainsKey(ReadOnlySpan<char> key);
#endif

        /// <summary>
        /// Gets the <paramref name="value"/> associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if the dictionary contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see cref="StringSegment.Null">StringSegment.Null</see>.</exception>
        bool TryGetValue(StringSegment key, out TValue value);

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        /// <summary>
        /// Gets the <paramref name="value"/> associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if the dictionary contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <remarks><note>This member is available only in .NET Core 3.0/.NET Standard 2.1 and above.</note></remarks>
        bool TryGetValue(ReadOnlySpan<char> key, out TValue value);
#endif

        #endregion
    }
}
#endif