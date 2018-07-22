#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringComparerExtensions.cs
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

using KGySoft.Libraries.Resources;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="StringComparer"/> type.
    /// </summary>
    public static class StringComparerExtensions
    {
        #region Methods

        /// <summary>
        /// Gets whether a string equals any of the strings in a set using a specific <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">A <see cref="StringComparer"/> that checks the equality.</param>
        /// <param name="s">A string instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">A string array</param>
        /// <returns></returns>
        public static bool EqualsAny(this StringComparer comparer, string s, params string[] set)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer), Res.Get(Res.ArgumentNull));

            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            for (int i = 0; i < length; i++)
            {
                if (comparer.Equals(s, set[i]))
                    return true;
            }

            return false;
        }

        #endregion
    }
}
