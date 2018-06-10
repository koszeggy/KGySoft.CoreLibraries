using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KGySoft.Libraries
{
    using KGySoft.Libraries.Resources;

    /// <summary>
    /// <see cref="StringComparer"/> extension methods
    /// </summary>
    public static class StringComparerTools
    {
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
    }
}
