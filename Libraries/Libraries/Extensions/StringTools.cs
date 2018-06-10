using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KGySoft.Libraries
{
    using KGySoft.Libraries.Resources;

    /// <summary>
    /// String extension methods
    /// </summary>
    public static class StringTools
    {
        /// <summary>
        /// Gets a token value from a string.
        /// </summary>
        /// <param name="s">The string that contains tokens.</param>
        /// <param name="token">Token key.</param>
        /// <param name="tokenSeparator">A substring that separates tokens. To escape token separator it can be doubled.</param>
        /// <param name="nameValueSeparator">A substring that separates the token key and value in a token.</param>
        /// <param name="ignoreCase">When <c>true</c>, casing is ignored by invariant culture.</param>
        /// <returns>Value of the token or empty string if token is not found. Returned value
        /// is trimmed. To avoid trimming, value should be embedded into single or double quotes.</returns>
        /// <example>
        /// <code lang="C#">
        /// string s = "top: 20; left: 30; padding: 5".GetTokenValue("left", ";", ":", false); // s == "30"
        /// </code>
        /// </example>
        public static string GetTokenValue(this string s, string token, string tokenSeparator, string nameValueSeparator, bool ignoreCase)
        {
            if (s == null)
                return null;
            StringComparison comp = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            if (s.IndexOf(token + nameValueSeparator, comp) < 0)
                return String.Empty;
            const string escapedToken = "\"<!TokenEscape!>\"";
            s = s.Replace(tokenSeparator + tokenSeparator, escapedToken);
            int tokenPos = 0;
            while (tokenPos >= 0)
            {
                tokenPos = s.IndexOf(token, tokenPos, comp);

                // checking whether the found token is only a substring in another token or a full word
                if (tokenPos > 0 && (Char.IsLetterOrDigit(s[tokenPos - 1]) || s[tokenPos - 1] == '_'))
                {
                    tokenPos++;
                    continue;
                }

                if (tokenPos >= 0)
                {
                    string result = s.Substring(tokenPos + token.Length + nameValueSeparator.Length,
                            (s + tokenSeparator).IndexOf(tokenSeparator, tokenPos, comp) - tokenPos - token.Length - nameValueSeparator.Length).Trim()
                            .Replace(escapedToken, tokenSeparator).RemoveQuotes();
                    return result;
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// Gets a token value from a string.
        /// </summary>
        /// <param name="s">The string that contains tokens.</param>
        /// <param name="token">Token key.</param>
        /// <param name="tokenSeparator">A substring that separates tokens.
        /// Doubled token separator is considered as inline content.</param>
        /// <param name="nameValueSeparator">A substring that separates the token key and value in a token.</param>
        /// <returns>Value of the token or empty string if token is not found. Returned value
        /// is trimmed. To avoid trimming, value should be embedded into single or double quotes.</returns>
        /// <example>
        /// <code lang="C#">
        /// string s = "top: 20; left: 30; padding: 5".GetTokenValue("left", ";", ":"); // s == "30"
        /// </code>
        /// </example>
        public static string GetTokenValue(this string s, string token, string tokenSeparator, string nameValueSeparator)
        {
            return GetTokenValue(s, token, tokenSeparator, nameValueSeparator, false);
        }

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
    }
}
