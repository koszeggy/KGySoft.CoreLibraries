#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CharExtensions.cs
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
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="char">char</see> type.
    /// </summary>
    public static class CharExtensions
    {
        #region Methods

        /// <summary>
        /// Gets whether <paramref name="c"/> is a non-character code point in Unicode.
        /// </summary>
        /// <param name="c">The <see cref="char"/> code point to check.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="c"/> is a non-character code point in Unicode; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool IsNonCharacter(this char c)
            => (c >= 0xFDD0 && c <= 0xFDEF)
            || c >= 0xFFFE;

        /// <summary>
        /// Gets whether <paramref name="c"/> is a valid standalone character code point in Unicode.
        /// That is, if <paramref name="c"/> is not a half-surrogate and is not defined as a non-character code point.
        /// </summary>
        /// <param name="c">The <see cref="char"/> code point to check.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="c"/> is a valid standalone character code point in Unicode; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool IsValidCharacter(this char c)
            => !Char.IsSurrogate(c) && !IsNonCharacter(c);

        #endregion
    }
}
