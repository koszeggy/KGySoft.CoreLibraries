#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringExtensions.cs
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using KGySoft.Libraries.Resources;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="string">string</see> type.
    /// </summary>
    public static class StringExtensions
    {
        #region Methods

        #region Misc Tools
        
        /// <summary>
        /// Extracts content of a single or double quoted string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveQuotes(this string s)
        {
            if (String.IsNullOrEmpty(s))
                return s;
            string result = s;
            if (result.Length > 1 && ((result[0] == '"' && result[result.Length - 1] == '"') ||
                    result[0] == '\'' && result[result.Length - 1] == '\''))
                result = result.Substring(1, result.Length - 2);
            return result;
        }

        /// <summary>
        /// Converts the passed string to a <see cref="Regex"/> that match wildcard characters (? and *).
        /// </summary>
        public static Regex ToWildcardsRegex(this string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.Get(Res.ArgumentNull));
            return new Regex("^" + Regex.Escape(s).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Repeats a <see cref="string"/> <paramref name="count"/> times.
        /// </summary>
        public static string Repeat(this string s, int count)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.Get(Res.ArgumentNull));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));

            if (s.Length == 0 || count == 1)
                return s;
            if (count == 0)
                return String.Empty;

            StringBuilder result = new StringBuilder(s);
            for (int i = 0; i < count; i++)
            {
                result.Append(s);
            }

            return result.ToString();
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parses separated hex values from a string.
        /// </summary>
        public static byte[] ParseHexBytes(this string s, string separator)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.Get(Res.ArgumentNull));

            if (string.IsNullOrEmpty(separator))
                return ParseHexBytes(s);

            string[] values = s.Split(new string[] { separator }, StringSplitOptions.None);
            byte[] result = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = Byte.Parse(values[i].Trim(), NumberStyles.HexNumber);
            }
            return result;
        }

        /// <summary>
        /// Parses a continuous hex stream from a string.
        /// </summary>
        public static byte[] ParseHexBytes(this string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.Get(Res.ArgumentNull));

            if (s.Length == 0)
                return new byte[0];

            if (s.Length % 2 != 0)
                throw new ArgumentException("Source length error", nameof(s));

            byte[] result = new byte[s.Length >> 1];
            for (int i = 0; i < (s.Length >> 1); i++)
            {
                result[i] = Byte.Parse(s.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return result;
        }

        /// <summary>
        /// Parses separated decimal bytes from a string.
        /// </summary>
        public static byte[] ParseDecimalBytes(this string s, string separator)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.Get(Res.ArgumentNull));

            if (String.IsNullOrEmpty(separator))
                throw new ArgumentException(Res.Get(Res.SeparatorNullOrEmpty), nameof(separator));

            string[] values = s.Split(new string[] { separator }, StringSplitOptions.None);
            byte[] result = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = Byte.Parse(values[i].Trim());
            }
            return result;
        }

        /// <summary>
        /// Tries to convert the specified <see cref="string">string</see> to an <see cref="Enum">enum</see> value.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see cref="Enum">enum</see>.</typeparam>
        /// <param name="s">The <see cref="string">string</see> to convert.</param>
        /// <param name="definedOnly">If <see langword="true"/>, the result can only be a defined value in the specified <typeparamref name="TEnum"/> type.
        /// If <see langword="false"/>, the result can be a non-defined value, too.</param>
        /// <returns>A non-<see langword="null"/> value if the conversion was successful; otherwise, <see langword="null"/>.</returns>
        public static TEnum? ToEnum<TEnum>(this string s, bool definedOnly = false)
            where TEnum : struct, IConvertible // replaced to System.Enum by RecompILer
        {
            if (s == null)
                return null;

            if (!Enum<TEnum>.TryParse(s, out TEnum value))
                return null;

            return !definedOnly || Enum<TEnum>.IsDefined(value) ? value : (TEnum?)null;
        }

        #endregion

        #region Comparison

        /// <summary>
        /// Gets whether the specified string <paramref name="s"/> contains the specified <paramref name="value"/> using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance in which <paramref name="value"/> is searched.</param>
        /// <param name="value">The <see cref="string"/> to seek.</param>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <returns><see langword="true"/> if string <paramref name="s"/> contains <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined <see cref="StringComparison"/> value.</exception>
        public static bool Contains(this string s, string value, StringComparison comparison)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.Get(Res.ArgumentNull));
            if (value == null)
                throw new ArgumentNullException(nameof(value), Res.Get(Res.ArgumentNull));
            if (!Enum<StringComparison>.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison), Res.Get(Res.ArgumentOutOfRange));

            return s.IndexOf(value, comparison) >= 0;
        }

        /// <summary>
        /// Gets whether the specified string <paramref name="s"/> equals any of the strings in the specified <paramref name="set"/> set by case sensitive ordinal comparison.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns><see langword="true"/> if string <paramref name="s"/> equals any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        public static bool EqualsAny(this string s, params string[] set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            for (int i = 0; i < length; i++)
            {
                if (String.Equals(s, set[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets whether the specified string <paramref name="s"/> equals any of the strings in the specified <paramref name="set"/> set using a specific <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">A <see cref="StringComparer"/> that checks the equality.</param>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns><see langword="true"/> if string <paramref name="s"/> equals any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        public static bool EqualsAny(this string s, StringComparer comparer, params string[] set)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer), Res.Get(Res.ArgumentNull));

            return set != null && set.Any(t => comparer.Equals(s, t));
        }

        /// <summary>
        /// Gets whether the specified <see cref="string"/> <paramref name="s"/> equals any of the strings in the specified <paramref name="set"/> set using a specific <paramref name="comparison"/>.
        /// </summary>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns><see langword="true"/> if string <paramref name="s"/> equals any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined <see cref="StringComparison"/> value.</exception>
        public static bool EqualsAny(this string s, StringComparison comparison, params string[] set)
        {
            if (!Enum<StringComparison>.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison), Res.Get(Res.ArgumentOutOfRange));

            return set != null && set.Any(str => String.Equals(s, str, comparison));
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence in the specified <see cref="string"/> <paramref name="s"/> of any of the strings in the specified <paramref name="set"/> by case sensitive ordinal comparison.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns>The zero-based index of the first occurrence in the specified <see cref="string"/> <paramref name="s"/> of any of the strings in the specified <paramref name="set"/>,
        /// or -1 if none of the strings of <paramref name="set"/> are found in <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="set"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="set"/>contains a <see langword="null"/> element.</exception>
        public static int IndexOfAny(this string s, params string[] set)
            => IndexOfAny(s, StringComparison.Ordinal, set);

        /// <summary>
        /// Gets the zero-based index of the first occurrence in the specified <see cref="string"/> <paramref name="s"/> of any of the strings in the specified <paramref name="set"/> using a specific <paramref name="comparison"/>.
        /// </summary>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns>The zero-based index of the first occurrence in the specified <see cref="string"/> <paramref name="s"/> of any of the strings in the specified <paramref name="set"/>,
        /// or -1 if none of the strings of <paramref name="set"/> are found in <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="set"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined <see cref="StringComparison"/> value.</exception>
        /// <exception cref="ArgumentException"><paramref name="set"/>contains a <see langword="null"/> element.</exception>
        public static int IndexOfAny(this string s, StringComparison comparison, params string[] set)
        {
            if (!Enum<StringComparison>.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison), Res.Get(Res.ArgumentOutOfRange));
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.Get(Res.ArgumentNull));
            if (set == null)
                throw new ArgumentNullException(nameof(set), Res.Get(Res.ArgumentNull));

            var index = -1;
            foreach (var str in set)
            {
                int pos = s.IndexOf(str ?? throw new ArgumentException(Res.Get(Res.ArgumentContainsNull), nameof(set)), comparison);
                if (pos == 0)
                    return 0;
                if (pos >= 0 && pos < index)
                    index = pos;
            }

            return index;
        }

        /// <summary>
        /// Gets whether the specified <see cref="string"/> <paramref name="s"/> contains any of the strings in the specified <paramref name="set"/> by case sensitive ordinal comparison.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">A string array</param>
        /// <returns><see langword="true"/> if string <paramref name="s"/> contains any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="set"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="set"/>contains a <see langword="null"/> element.</exception>
        public static bool ContainsAny(this string s, params string[] set)
            => ContainsAny(s, StringComparison.Ordinal, set);

        /// <summary>
        /// Gets whether the specified <see cref="string"/> <paramref name="s"/> contains any of the strings in the specified <paramref name="set"/> set using a specific <paramref name="comparison"/>.
        /// </summary>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">A string array</param>
        /// <returns><see langword="true"/> if string <paramref name="s"/> contains any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="set"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined <see cref="StringComparison"/> value.</exception>
        /// <exception cref="ArgumentException"><paramref name="set"/>contains a <see langword="null"/> element.</exception>
        public static bool ContainsAny(this string s, StringComparison comparison, params string[] set)
        {
            if (!Enum<StringComparison>.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison), Res.Get(Res.ArgumentOutOfRange));
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.Get(Res.ArgumentNull));
            if (set == null)
                throw new ArgumentNullException(nameof(set), Res.Get(Res.ArgumentNull));

            foreach (var str in set)
            {
                if (s.IndexOf(str ?? throw new ArgumentException(Res.Get(Res.ArgumentContainsNull), nameof(set)), comparison) >= 0)
                    return true;
            }

            return false;
        }

        #endregion

        #endregion
    }
}
