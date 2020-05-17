#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CharExtensions.cs
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
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="char">char</see> type.
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

        /// <summary>
        /// Gets whether <paramref name="c"/> is a whitespace character.
        /// This method is faster than <see cref="Char.IsWhiteSpace(char)">Char.IsWhiteSpace</see>
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="c"/> is a whitespace character;
        /// otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool IsWhiteSpace(this char c)
        {
            // credit to https://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin
            switch (c)
            {
                case '\u0009':
                case '\u000A':
                case '\u000B':
                case '\u000C':
                case '\u000D':
                case '\u0020':
                case '\u0085':
                case '\u00A0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u2028':
                case '\u2029':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}
