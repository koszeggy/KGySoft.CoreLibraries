using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KGySoft.Libraries
{
	/// <summary>
	/// General routines.
	/// </summary>
    [Obsolete("This class is obsolete. Use extension classes instead.")]
	public static class Utils
	{
		#region String utils

        ///// <summary>
        ///// Removes the control characters from a text.
        ///// </summary>
        //public static string RemoveControlCharacters(string text)
        //{
        //    text = text.Replace("\t", " ");
        //    text = text.Replace(Environment.NewLine, " ");
        //    text = text.Replace("\t", " ");
        //    text = text.Replace("\n\r", " ");

        //    text = text.Replace('\0', ' ');
        //    text = text.Replace('\a', ' ');
        //    text = text.Replace('\b', ' ');
        //    text = text.Replace('\f', ' ');
        //    text = text.Replace('\n', ' ');
        //    text = text.Replace('\r', ' ');
        //    text = text.Replace('\t', ' ');
        //    text = text.Replace('\v', ' ');

        //    return text;
        //}

		/// <summary>
		/// Gets a token value from a string.
		/// <example>
		/// <para>
		/// Example: GetTokenValue("top: 20; left: 30; padding: 5", "left", ";", ":") == "30"
		/// </para>
		/// </example>
		/// </summary>
		/// <param name="s">The string that contains tokens.</param>
		/// <param name="token">Token key.</param>
		/// <param name="tokenSeparator">A substring that separates tokens.
		/// Doubled token separator is considered as inline content.</param>
		/// <param name="nameValueSeparator">A substring that separates the token key and value in a token.</param>
		/// <returns>Value of the token or empty string if token is not found. Returned value
		/// is trimmed. To avoid trimming, value should be embedded into single or double quotes.</returns>
		public static string GetTokenValue(string s, string token, string tokenSeparator, string nameValueSeparator)
		{
			if (s == null || !s.ToLower().Contains(token.ToLower() + nameValueSeparator))
				return String.Empty;
			const string inlineToken = "\"<!inlineToken!>\"";
			s = s.Replace(tokenSeparator + tokenSeparator, inlineToken);
			int tokenPos = 0;
			while (tokenPos >= 0)
			{
				tokenPos = s.ToLower().IndexOf(token.ToLower() + nameValueSeparator, tokenPos);
				// checking whether the found token is only a substring in another token or a full word
				if (tokenPos > 0 && (Char.IsLetterOrDigit(s[tokenPos - 1]) || s[tokenPos - 1] == '_'))
				{
					tokenPos++;
					continue;
				}

				if (tokenPos >= 0)
				{
					string result = s.Substring(tokenPos + token.Length + nameValueSeparator.Length,
							(s + tokenSeparator).IndexOf(tokenSeparator, tokenPos) - tokenPos - token.Length - nameValueSeparator.Length).Trim()
							.Replace(inlineToken, tokenSeparator);
					result = RemoveQuotes(result);
					return result;
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Extracts content of a single or double quoted string.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string RemoveQuotes(string s)
		{
			if (String.IsNullOrEmpty(s))
				return s;
			string result = s;
			if (result.Length > 1 && ((result[0] == '"' && result[result.Length - 1] == '"') ||
				result[0] == '\'' && result[result.Length - 1] == '\''))
				result = result.Substring(1, result.Length - 2);
			return s;
		}

		#endregion

		#region Object Utils

		/// <summary>
		/// Returns true when the object is among the elements of <paramref name="set"/>.
		/// Similar to the in operator in SQL and Pascal
		/// </summary>
		[Obsolete("Use Object extension method 'In' instead")]
        public static bool InSet(object item, params object[] set)
		{
            if (item == null)
                throw new ArgumentNullException("item");

            return item.In(set);
		}

		#endregion

		#region Type conversion utils

		/// <summary>
		/// Converts an object to bool. An object can be converted to bool if its string representation
        /// contains "true", "false", "1" or "0". Otherwise, an exception is thrown.
		/// </summary>
        /// <param name="value">The object that contains the bool value.</param>
		/// <param name="defaultValue">Return value if <paramref name="value"/> is null or empty.</param>
        [Obsolete("Use Object extension method 'ToBool' instead")]
        public static bool ToBool(object value, bool defaultValue)
		{
            string s;
			if (String.IsNullOrEmpty(s = value.ToString()))
				return defaultValue;
			if (s.ToLower() == "true" || s == "1")
				return true;
			else if (s.ToLower() == "false" || s == "0")
				return false;
			else
				throw new InvalidOperationException(String.Format("'{0}' cannot be converted to a bool value", value));
		}

		/// <summary>
		/// Retrieves the number from an object. Uses the current culture.
		/// </summary>
        [Obsolete("Use Object extension method 'ToDouble' instead")]
        public static double ToDouble(out bool isNumber, object value)
		{
			isNumber = false;
			if (value == null)
				return 0;

			string s = value.ToString().Trim();

			string thousandSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
			double ret = 0;

			s = s.Replace(thousandSeparator, string.Empty);

			if (!isNumber)
				isNumber = double.TryParse(s, out ret);

			return ret;
		}

		#endregion
	}
}
