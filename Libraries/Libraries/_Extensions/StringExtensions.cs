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

        /// <summary>
        /// Tries to convert the specified <see cref="string">string</see> to an <see cref="Enum">enum</see> value.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see cref="Enum">enum</see>.</typeparam>
        /// <param name="s">The <see cref="string">string</see> to convert.</param>
        /// <param name="definedOnly">If <c>true</c>, the result can only be a defined value in the specified <typeparamref name="TEnum"/> type.
        /// If <c>false</c>, the result can be a non-defined value, too.</param>
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
    }
}
