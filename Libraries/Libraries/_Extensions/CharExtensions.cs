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

#endregion

namespace KGySoft.Libraries
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
        /// <returns><see langword="true"/> if <paramref name="c"/> is a non-character code point in Unicode; otherwise, <see langword="false"/>.</returns>
        public static bool IsNonCharacter(this char c)
            => (c >= 0xFDD0 && c <= 0xFDEF)
            || c >= 0xFFFE;

        /// <summary>
        /// Gets whether <paramref name="c"/> is a valid standalone character code point in Unicode.
        /// That is, if <paramref name="c"/> is not a half-surrogate and is not defined as a non-character code point.
        /// </summary>
        /// <param name="c">The <see cref="char"/> code point to check.</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is a valid standalone character code point in Unicode.; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidCharacter(this char c)
            => !Char.IsSurrogate(c) && !IsNonCharacter(c);

        #endregion
    }
}
