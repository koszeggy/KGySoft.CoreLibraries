#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Enum.Parse.cs
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

using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    public static partial class Enum<TEnum>
    {
        #region Methods

        #region String

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The <see cref="string">string</see> representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specifies the separator among the values. If <see langword="null"/>&#160;or is empty, then comma (<c>,</c>) separator is used.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="string">string</see> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool TryParse(string value, string? separator, bool ignoreCase, out TEnum result)
        {
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);

            // simple name match test (always case-sensitive)
            if (NameValuePairs.TryGetValue(value, out result))
                return true;

            var s = new StringSegmentInternal(value);
            s.Trim();
            result = default(TEnum);
            if (s.Length == 0)
                return false;

            // simple numeric value
            char c = s[0];
            if (((c >= '0' && c <= '9') || c == '-' || c == '+') && s.TryParseIntQuick(underlyingInfo.IsSigned, underlyingInfo.MaxValue, out ulong numericValue))
            {
                result = converter.ToEnum(numericValue);
                return true;
            }

            // rest: flags enum or ignored case
            if (String.IsNullOrEmpty(separator))
                separator = EnumExtensions.DefaultParseSeparator;

            ulong acc = 0UL;
            StringKeyedDictionary<ulong> dict = ignoreCase ? NameRawValuePairsIgnoreCase : NameRawValuePairs;
            while (s.TryGetNextSegment(separator!, out StringSegmentInternal token))
            {
                token.Trim();
                if (token.Length == 0)
                    return false;

                // literal token found in dictionary
                if (dict.TryGetValue(token, out ulong tokens))
                {
                    acc |= tokens;
                    continue;
                }

                // checking if is numeric token
                c = token[0];
                if (((c >= '0' && c <= '9') || c == '-' || c == '+') && token.TryParseIntQuick(underlyingInfo.IsSigned, underlyingInfo.MaxValue, out numericValue))
                {
                    acc |= numericValue;
                    continue;
                }

                // none of above
                return false;
            }

            result = converter.ToEnum(acc);
            return true;
        }

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The <see cref="string">string</see> representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="string">string</see> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool TryParse(string value, bool ignoreCase, out TEnum result) => TryParse(value, EnumExtensions.DefaultParseSeparator, ignoreCase, out result);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The <see cref="string">string</see> representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specifies the separator among the values. If <see langword="null"/>&#160;or is empty, then comma (<c>,</c>) separator is used.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="string">string</see> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool TryParse(string value, string? separator, out TEnum result) => TryParse(value, separator, false, out result);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The <see cref="string">string</see> representation of the enumerated value or values to parse.</param>
        /// <param name="result"><see langword="null"/>&#160;if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="string">string</see> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool TryParse(string value, out TEnum result) => TryParse(value, EnumExtensions.DefaultParseSeparator, false, out result);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The <see cref="string">string</see> representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specified the separator among the values. If <see langword="null"/>&#160;or is empty, then comma (<c>,</c>) separator is used. This parameter is optional.
        /// <br/>Default value: <c>,</c></param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(string value, string? separator = EnumExtensions.DefaultParseSeparator, bool ignoreCase = false)
        {
            if (!TryParse(value, separator, ignoreCase, out TEnum result))
                Throw.ArgumentException(Argument.value, Res.EnumValueCannotBeParsedAsEnum(value, typeof(TEnum)));
            return result;
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The <see cref="string">string</see> representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(string value, bool ignoreCase)
        {
            if (!TryParse(value, EnumExtensions.DefaultParseSeparator, ignoreCase, out TEnum result))
                Throw.ArgumentException(Argument.value, Res.EnumValueCannotBeParsedAsEnum(value, typeof(TEnum)));
            return result;
        }

        #endregion

        #region StringSegment

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specifies the separator among the values. If <see cref="StringSegment.Null"/> or <see cref="StringSegment.Empty"/>, then comma (<c>,</c>) separator is used.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="StringSegment"/> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        public static bool TryParse(StringSegment value, StringSegment separator, bool ignoreCase, out TEnum result)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);

            // simple name match test (always case-sensitive)
            if (NameValuePairs.TryGetValue(value, out result))
                return true;

            var s = new StringSegmentInternal(value.UnderlyingString!, value.Offset, value.Length);
            s.Trim();
            result = default(TEnum);
            if (s.Length == 0)
                return false;

            // simple numeric value
            char c = s[0];
            if (((c >= '0' && c <= '9') || c == '-' || c == '+') && s.TryParseIntQuick(underlyingInfo.IsSigned, underlyingInfo.MaxValue, out ulong numericValue))
            {
                result = converter.ToEnum(numericValue);
                return true;
            }

            // rest: flags enum or ignored case
            if (separator.IsNullOrEmpty)
                separator = EnumExtensions.DefaultParseSeparator;

            ulong acc = 0UL;
            StringKeyedDictionary<ulong> dict = ignoreCase ? NameRawValuePairsIgnoreCase : NameRawValuePairs;
            while (s.TryGetNextSegment(separator, out StringSegmentInternal token))
            {
                token.Trim();
                if (token.Length == 0)
                    return false;

                // literal token found in dictionary
                if (dict.TryGetValue(token, out ulong tokens))
                {
                    acc |= tokens;
                    continue;
                }

                // checking if is numeric token
                c = token[0];
                if (((c >= '0' && c <= '9') || c == '-' || c == '+') && token.TryParseIntQuick(underlyingInfo.IsSigned, underlyingInfo.MaxValue, out numericValue))
                {
                    acc |= numericValue;
                    continue;
                }

                // none of above
                return false;
            }

            result = converter.ToEnum(acc);
            return true;
        }

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="StringSegment"/> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        public static bool TryParse(StringSegment value, bool ignoreCase, out TEnum result) => TryParse(value, EnumExtensions.DefaultParseSeparator, ignoreCase, out result);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specifies the separator among the values. If <see cref="StringSegment.Null"/> or <see cref="StringSegment.Empty"/>, then comma (<c>,</c>) separator is used.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="StringSegment"/> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        public static bool TryParse(StringSegment value, StringSegment separator, out TEnum result) => TryParse(value, separator, false, out result);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="StringSegment"/> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        public static bool TryParse(StringSegment value, out TEnum result) => TryParse(value, EnumExtensions.DefaultParseSeparator, false, out result);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specified the separator among the values. If <see cref="StringSegment.Null"/> or <see cref="StringSegment.Empty"/>, then comma (<c>,</c>) separator is used. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegment.Null"/>.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(StringSegment value, StringSegment separator = default, bool ignoreCase = false)
        {
            if (!TryParse(value, separator, ignoreCase, out TEnum result))
                Throw.ArgumentException(Argument.value, Res.EnumValueCannotBeParsedAsEnum(value.ToString()!, typeof(TEnum)));
            return result;
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(StringSegment value, bool ignoreCase)
        {
            if (!TryParse(value, default, ignoreCase, out TEnum result))
                Throw.ArgumentException(Argument.value, Res.EnumValueCannotBeParsedAsEnum(value.ToString()!, typeof(TEnum)));
            return result;
        }

        #endregion

        #region Span
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specifies the separator among the values. If empty, then comma (<c>,</c>) separator is used.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, ReadOnlySpan<char> separator, bool ignoreCase, out TEnum result)
        {
            // simple name match test (always case-sensitive)
            if (NameValuePairs.TryGetValue(value, out result))
                return true;

            ReadOnlySpan<char> s = value.Trim();
            result = default(TEnum);
            if (s.Length == 0)
                return false;

            // simple numeric value
            char c = s[0];
            if (((c >= '0' && c <= '9') || c == '-' || c == '+') && s.TryParseIntQuick(underlyingInfo.IsSigned, underlyingInfo.MaxValue, out ulong numericValue))
            {
                result = converter.ToEnum(numericValue);
                return true;
            }

            // rest: flags enum or ignored case
            if (separator.IsEmpty)
                separator = EnumExtensions.DefaultParseSeparator;

            ulong acc = 0UL;
            StringKeyedDictionary<ulong> dict = ignoreCase ? NameRawValuePairsIgnoreCase : NameRawValuePairs;
            while (s.TryGetNextSegment(separator, out ReadOnlySpan<char> token))
            {
                token = token.Trim();
                if (token.Length == 0)
                    return false;

                // literal token found in dictionary
                if (dict.TryGetValue(token, out ulong tokens))
                {
                    acc |= tokens;
                    continue;
                }

                // checking if is numeric token
                c = token[0];
                if (((c >= '0' && c <= '9') || c == '-' || c == '+') && token.TryParseIntQuick(underlyingInfo.IsSigned, underlyingInfo.MaxValue, out numericValue))
                {
                    acc |= numericValue;
                    continue;
                }

                // none of above
                return false;
            }

            result = converter.ToEnum(acc);
            return true;
        }

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, bool ignoreCase, out TEnum result) => TryParse(value, EnumExtensions.DefaultParseSeparator, ignoreCase, out result);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specifies the separator among the values. If empty, then comma (<c>,</c>) separator is used.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, ReadOnlySpan<char> separator, out TEnum result) => TryParse(value, separator, false, out result);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="result">Returns the default value of <typeparamref name="TEnum"/>, if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if the <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, out TEnum result) => TryParse(value, EnumExtensions.DefaultParseSeparator, false, out result);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specified the separator among the values. If empty, then comma (<c>,</c>) separator is used. This parameter is optional.
        /// <br/>Default value: <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see></param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(ReadOnlySpan<char> value, ReadOnlySpan<char> separator = default, bool ignoreCase = false)
        {
            if (!TryParse(value, separator, ignoreCase, out TEnum result))
                Throw.ArgumentException(Argument.value, Res.EnumValueCannotBeParsedAsEnum(value.ToString(), typeof(TEnum)));
            return result;
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The string representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(ReadOnlySpan<char> value, bool ignoreCase)
        {
            if (!TryParse(value, default, ignoreCase, out TEnum result))
                Throw.ArgumentException(Argument.value, Res.EnumValueCannotBeParsedAsEnum(value.ToString(), typeof(TEnum)));
            return result;
        }

#endif
        #endregion

        #endregion
    }
}
