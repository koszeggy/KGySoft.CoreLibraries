#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringExtensions.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.RegularExpressions;

using KGySoft.Reflection;

#endregion

#region Suppressions

#if NET5_0_OR_GREATER
#pragma warning disable CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf' - there is no String.Contains(string, StringComparison) method in some targeted platforms  
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="string">string</see> type.
    /// </summary>
    public static partial class StringExtensions
    {
        #region Methods

        #region Misc Tools

        /// <summary>
        /// Extracts content of a single or double quoted string.
        /// </summary>
        /// <param name="s">The string to be extracted from quotes.</param>
        /// <returns>If <paramref name="s"/> was surrounded by single or double quotes, returns a new string without the quotes; otherwise, returns <paramref name="s"/>.</returns>
        [return:NotNullIfNotNull("s")]public static string? RemoveQuotes(this string? s)
            => (s?.Length ?? 0) < 2
                ? s
                : s!.Length > 1 && (s[0] == '"' && s[s.Length - 1] == '"' || s[0] == '\'' && s[s.Length - 1] == '\'')
                    ? s.Substring(1, s.Length - 2)
                    : s;

        /// <summary>
        /// Converts the passed string to a <see cref="Regex"/> that matches wildcard characters (? and *).
        /// </summary>
        /// <param name="s">The string containing possible wildcard characters.</param>
        /// <returns>A <see cref="Regex"/> instance that matches the pattern of the given string in <paramref name="s"/>.</returns>
        public static Regex ToWildcardsRegex(this string s)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);
            return new Regex("^" + Regex.Escape(s).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Repeats a <see cref="string"/>&#160;<paramref name="count"/> times.
        /// </summary>
        /// <param name="s">The string to repeat <paramref name="count"/> times.</param>
        /// <param name="count">The count of repeating <paramref name="s"/>. If 0, an empty string is returned. If 1 the original <paramref name="s"/> is returned.</param>
        /// <returns><paramref name="s"/> repeated <paramref name="count"/> times.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0.</exception>
        [SecuritySafeCritical]
        public static unsafe string Repeat(this string s, int count)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (s.Length == 0 || count == 1)
                return s;
            if (count == 0)
                return String.Empty;

            string result = new String('\0', count * s.Length);
            fixed (char* pResult = result)
            {
                var sb = new MutableStringBuilder(pResult, result.Length);
                for (int i = 0; i < count; i++)
                    sb.Append(s);
            }

            return result;
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parses delimited hex values from a string into an array of bytes.
        /// </summary>
        /// <param name="s">A string containing delimited hex values.</param>
        /// <param name="separator">A separator delimiting the hex values. If <see langword="null"/>, then <paramref name="s"/> is parsed as a continuous hex stream.</param>
        /// <returns>A byte array containing the hex values as bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="separator"/> is <see langword="null"/>&#160;or empty, and <paramref name="s"/> does not consist of event number of hex digits,
        /// or parsing failed.</exception>
        public static byte[] ParseHexBytes(this string s, string? separator)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);

            if (String.IsNullOrEmpty(separator))
                return ParseHexBytes(s);

            List<StringSegmentInternal> values = new StringSegmentInternal(s).Split(separator!);
            int len = values.Count;
            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                StringSegmentInternal segment = values[i];
                segment.Trim();
                if (!Parser.TryParseHexByte(segment, out result[i]))
                    Throw.ArgumentException(Argument.s, Res.StringExtensionsCannotParseAsType(segment.ToString(), Reflector.ByteType));
            }

            return result;
        }

        /// <summary>
        /// Parses a continuous hex stream from a string.
        /// </summary>
        /// <param name="s">A string containing continuous hex values without delimiters.</param>
        /// <returns>A byte array containing the hex values as bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="s"/> does not consist of event amount of hex digits, or parsing failed.</exception>
        public static byte[] ParseHexBytes(this string s)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);

            if (s.Length == 0)
                return Reflector.EmptyArray<byte>();

            if ((s.Length & 1) != 0)
                Throw.ArgumentException(Argument.s, Res.StringExtensionsSourceLengthNotEven);

            byte[] result = new byte[s.Length >> 1];
            for (int i = 0; i < result.Length; i++)
            {
                if (!Parser.TryParseHexByte(s, i << 1, out result[i]))
                    Throw.ArgumentException(Argument.s, Res.StringExtensionsCannotParseAsType(s.Substring(i << 1, 2), Reflector.ByteType));
            }

            return result;
        }

        /// <summary>
        /// Parses separated decimal bytes from a string.
        /// </summary>
        /// <param name="s">A string containing delimited decimal integer numbers.</param>
        /// <param name="separator">A separator delimiting the values.</param>
        /// <returns>A byte array containing the decimal values as bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="separator"/> is <see langword="null"/>&#160;or empty.</exception>
        /// <exception cref="FormatException"><paramref name="s"/> is not of the correct format.</exception>
        /// <exception cref="OverflowException">A value in <paramref name="s"/> does not fit in the range of a <see cref="byte">byte</see> value.</exception>
        public static byte[] ParseDecimalBytes(this string s, string separator)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);

            if (String.IsNullOrEmpty(separator))
                Throw.ArgumentException(Argument.separator, Res.StringExtensionsSeparatorNullOrEmpty);

            List<StringSegmentInternal> values = new StringSegmentInternal(s).Split(separator);
            int len = values.Count;
            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                StringSegmentInternal segment = values[i];
                segment.Trim();
                if (!segment.TryParseIntQuick(false, Byte.MaxValue, out ulong value))
                    Throw.ArgumentException(Argument.s, Res.StringExtensionsCannotParseAsType(segment.ToString(), Reflector.ByteType));
                result[i] = (byte)value;
            }

            return result;
        }

        /// <summary>
        /// Tries to convert the specified <see cref="string">string</see> to an <see cref="Enum"/> value of <typeparamref name="TEnum"/> type.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see cref="Enum"/>.</typeparam>
        /// <param name="s">The <see cref="string">string</see> to convert.</param>
        /// <param name="definedOnly">If <see langword="true"/>, the result can only be a defined value in the specified <typeparamref name="TEnum"/> type.
        /// If <see langword="false"/>, the result can be a non-defined value, too.</param>
        /// <returns>A non-<see langword="null"/>&#160;value if the conversion was successful; otherwise, <see langword="null"/>.</returns>
        public static TEnum? ToEnum<TEnum>(this string? s, bool definedOnly = false)
            where TEnum : struct, Enum
        {
            if (s == null)
                return null;

            if (!Enum<TEnum>.TryParse(s, out TEnum value))
                return null;

            return !definedOnly || Enum<TEnum>.IsDefined(value) ? value : null;
        }

        /// <summary>
        /// Parses an object of type <typeparamref name="T"/> from a <see cref="string">string</see> value. Firstly, it tries to parse the type natively.
        /// If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the return value.</typeparam>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then the method returns <see langword="null"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An instance of <typeparamref name="T"/>, which is the result of the parsing. A <see langword="null"/>&#160;reference can be returned if <paramref name="s"/> is <see langword="null"/>, and <typeparamref name="T"/> is a reference or nullable type.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;extension methods.</para>
        /// <para>A <see cref="TypeConverter"/> can be registered by the <see cref="TypeExtensions.RegisterTypeConverter{TConverter}">RegisterTypeConverter</see>&#160;extension method.</para>
        /// <para>Natively parsed types:
        /// <list type="bullet">
        /// <item><description><see cref="System.Enum"/> based types</description></item>
        /// <item><description><see cref="string"/></description></item>
        /// <item><description><see cref="char"/></description></item>
        /// <item><description><see cref="byte"/></description></item>
        /// <item><description><see cref="sbyte"/></description></item>
        /// <item><description><see cref="short"/></description></item>
        /// <item><description><see cref="ushort"/></description></item>
        /// <item><description><see cref="int"/></description></item>
        /// <item><description><see cref="uint"/></description></item>
        /// <item><description><see cref="long"/></description></item>
        /// <item><description><see cref="ulong"/></description></item>
        /// <item><description><see cref="float"/></description></item>
        /// <item><description><see cref="double"/></description></item>
        /// <item><description><see cref="decimal"/></description></item>
        /// <item><description><see cref="bool"/></description></item>
        /// <item><description><see cref="IntPtr"/></description></item>
        /// <item><description><see cref="UIntPtr"/></description></item>
        /// <item><description><see cref="Type"/></description></item>
        /// <item><description><see cref="DateTime"/></description></item>
        /// <item><description><see cref="DateTimeOffset"/></description></item>
        /// <item><description><see cref="TimeSpan"/></description></item>
        /// <item><description><see cref="Nullable{T}"/> of types above: <see langword="null"/>&#160;or empty value returns <see langword="null"/>; otherwise, <paramref name="s"/> is parsed as the underlying type</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><typeparamref name="T"/> is not nullable and <paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Parameter <paramref name="s"/> cannot be parsed as <typeparamref name="T"/>.</exception>
        [return:NotNullIfNotNull("s")]public static T? Parse<T>(this string? s, CultureInfo? culture = null)
        {
            if (!Parser.TryParse(s, culture, out T? value, out Exception? error))
                Throw.ArgumentException(Argument.obj, Res.StringExtensionsCannotParseAsType(s!, typeof(T)), error);
            return value;
        }

        /// <summary>
        /// Parses an object from a <see cref="string">string</see> value. Firstly, it tries to parse the type natively.
        /// If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> overload for details.
        /// </summary>
        /// <returns>An object of <paramref name="type"/>, which is the result of the parsing.</returns>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then the method returns <see langword="null"/>.</param>
        /// <param name="type">The desired type of the return value.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>The parsed value. A <see langword="null"/>&#160;reference can be returned if <paramref name="s"/> is <see langword="null"/>, and <paramref name="type"/> is a reference or nullable type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>, or <paramref name="type"/> is not nullable and <paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Parameter <paramref name="s"/> cannot be parsed as <paramref name="type"/>.</exception>
        [return:NotNullIfNotNull("s")]public static object? Parse(this string? s, Type type, CultureInfo? culture = null)
        {
            if (!Parser.TryParse(s, type, culture, true, false, out object? value, out Exception? error) || !type.CanAcceptValue(value))
                Throw.ArgumentException(Argument.obj, Res.StringExtensionsCannotParseAsType(s!, type), error);
            return value;
        }

        /// <summary>
        /// Tries to parse an object of type <typeparamref name="T"/> from a <see cref="string">string</see> value. Firstly, it tries to parse the type natively.
        /// If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.
        /// It can be <see langword="null"/>&#160;even if <paramref name="s"/> is <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <typeparamref name="T"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><typeparamref name="T"/> is not nullable and <paramref name="s"/> is <see langword="null"/>.</exception>
        public static bool TryParse<T>(this string? s, CultureInfo? culture, out T? value)
            => Parser.TryParse(s, culture, out value, out var _);

        /// <summary>
        /// Tries to parse an object of type <typeparamref name="T"/> from a <see cref="string">string</see> value. Firstly, it tries to parse the type natively.
        /// If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.
        /// It can be <see langword="null"/>&#160;even if <paramref name="s"/> is <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <typeparamref name="T"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><typeparamref name="T"/> is not nullable and <paramref name="s"/> is <see langword="null"/>.</exception>
        public static bool TryParse<T>(this string? s, out T? value) => TryParse(s, null, out value);

        /// <summary>
        /// Tries to parse an object of type <paramref name="type"/> from a <see cref="string">string</see> value. Firstly, it tries to parse the type natively.
        /// If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="type">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.
        /// It can be <see langword="null"/>&#160;even if <paramref name="s"/> is <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <paramref name="type"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is null, or <paramref name="type"/> is not nullable and <paramref name="s"/> is <see langword="null"/>.</exception>
        public static bool TryParse(this string? s, Type type, CultureInfo? culture, out object? value)
            => Parser.TryParse(s, type, culture, true, false, out value, out var _);

        /// <summary>
        /// Tries to parse an object of type <paramref name="type"/> from a <see cref="string">string</see> value. Firstly, it tries to parse the type natively.
        /// If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="type">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.
        /// It can be <see langword="null"/>&#160;even if <paramref name="s"/> is <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <paramref name="type"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is null, or <paramref name="type"/> is not nullable and <paramref name="s"/> is <see langword="null"/>.</exception>
        public static bool TryParse(this string? s, Type type, out object? value)
            => Parser.TryParse(s, type, null, true, false, out value, out var _);

        [return: NotNullIfNotNull("s")]internal static object? Parse(this string? s, Type type, bool safeMode)
        {
            if (!Parser.TryParse(s, type, null, true, safeMode, out object? value, out Exception? error) || !type.CanAcceptValue(value))
                Throw.ArgumentException(Argument.obj, Res.StringExtensionsCannotParseAsType(s!, type), error);
            return value;
        }

        #endregion

        #region Comparison

        /// <summary>
        /// Gets whether the specified string <paramref name="s"/> contains the specified <paramref name="value"/> using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance in which <paramref name="value"/> is searched.</param>
        /// <param name="value">The <see cref="string"/> to seek.</param>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <returns><see langword="true"/>&#160;if string <paramref name="s"/> contains <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined <see cref="StringComparison"/> value.</exception>
        public static bool Contains(this string s, string value, StringComparison comparison)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);
            if (!Enum<StringComparison>.IsDefined(comparison))
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

            return s.IndexOf(value, comparison) >= 0;
        }

        /// <summary>
        /// Gets whether the specified string <paramref name="s"/> equals any of the strings in the specified <paramref name="set"/> set by case sensitive ordinal comparison.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns><see langword="true"/>&#160;if string <paramref name="s"/> equals any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        public static bool EqualsAny(this string? s, params string?[]? set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            for (int i = 0; i < length; i++)
            {
                if (s == set[i])
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
        /// <returns><see langword="true"/>&#160;if string <paramref name="s"/> equals any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        public static bool EqualsAny(this string? s, StringComparer comparer, params string?[]? set)
        {
            if (comparer == null!)
                Throw.ArgumentNullException(Argument.comparer);

            return set != null && set.Any(t => comparer.Equals(s, t));
        }

        /// <summary>
        /// Gets whether the specified <see cref="string"/>&#160;<paramref name="s"/> equals any of the strings in the specified <paramref name="set"/> set using a specific <paramref name="comparison"/>.
        /// </summary>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns><see langword="true"/>&#160;if string <paramref name="s"/> equals any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined <see cref="StringComparison"/> value.</exception>
        public static bool EqualsAny(this string? s, StringComparison comparison, params string?[]? set)
        {
            if (!Enum<StringComparison>.IsDefined(comparison))
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

            return set != null && set.Any(str => String.Equals(s, str, comparison));
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence in the specified <see cref="string"/>&#160;<paramref name="s"/> of any of the strings in the specified <paramref name="set"/> by case sensitive ordinal comparison.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns>The zero-based index of the first occurrence in the specified <see cref="string"/>&#160;<paramref name="s"/> of any of the strings in the specified <paramref name="set"/>,
        /// or -1 if none of the strings of <paramref name="set"/> are found in <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="set"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="set"/>contains a <see langword="null"/>&#160;element.</exception>
        public static int IndexOfAny(this string s, params string[] set)
            => IndexOfAny(s, StringComparison.Ordinal, set);

        /// <summary>
        /// Gets the zero-based index of the first occurrence in the specified <see cref="string"/>&#160;<paramref name="s"/> of any of the strings in the specified <paramref name="set"/> using a specific <paramref name="comparison"/>.
        /// </summary>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns>The zero-based index of the first occurrence in the specified <see cref="string"/>&#160;<paramref name="s"/> of any of the strings in the specified <paramref name="set"/>,
        /// or -1 if none of the strings of <paramref name="set"/> are found in <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="set"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined <see cref="StringComparison"/> value.</exception>
        /// <exception cref="ArgumentException"><paramref name="set"/>contains a <see langword="null"/>&#160;element.</exception>
        public static int IndexOfAny(this string s, StringComparison comparison, params string[] set)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);
            if (set == null!)
                Throw.ArgumentNullException(Argument.set);
            if (!Enum<StringComparison>.IsDefined(comparison))
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

            int len = s.Length;
            if (len == 0)
            {
                foreach (string str in set)
                {
                    if (str == null!)
                        Throw.ArgumentException(Argument.set, Res.ArgumentContainsNull);
                    if (str.Length == 0)
                        return 0;
                }

                return -1;
            }

            var index = -1;
            for (int i = 0; i < len; i++)
            {
                foreach (string str in set)
                {
                    if (str == null!)
                        Throw.ArgumentException(Argument.set, Res.ArgumentContainsNull);
                    if (str.Length == 0)
                        return 0;

                    int strLen = str.Length;
                    if (s[i] != str[0] || strLen > len - i)
                        continue;
                    if (strLen == 1 || String.Compare(s, i, str, 0, strLen, comparison) == 0)
                        return i;
                }
            }

            return index;
        }

        /// <summary>
        /// Gets whether the specified <see cref="string"/>&#160;<paramref name="s"/> contains any of the strings in the specified <paramref name="set"/> by case sensitive ordinal comparison.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">A string array</param>
        /// <returns><see langword="true"/>&#160;if string <paramref name="s"/> contains any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="set"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="set"/>contains a <see langword="null"/>&#160;element.</exception>
        public static bool ContainsAny(this string s, params string[] set)
            => ContainsAny(s, StringComparison.Ordinal, set);

        /// <summary>
        /// Gets whether the specified <see cref="string"/>&#160;<paramref name="s"/> contains any of the strings in the specified <paramref name="set"/> set using a specific <paramref name="comparison"/>.
        /// </summary>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">A string array</param>
        /// <returns><see langword="true"/>&#160;if string <paramref name="s"/> contains any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="set"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined <see cref="StringComparison"/> value.</exception>
        /// <exception cref="ArgumentException"><paramref name="set"/>contains a <see langword="null"/>&#160;element.</exception>
        public static bool ContainsAny(this string s, StringComparison comparison, params string[] set)
        {
            if (!Enum<StringComparison>.IsDefined(comparison))
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);
            if (set == null!)
                Throw.ArgumentNullException(Argument.set);

            foreach (var str in set)
            {
                if (str == null!)
                    Throw.ArgumentException(Argument.set, Res.ArgumentContainsNull);
                if (s.IndexOf(str, comparison) >= 0)
                    return true;
            }

            return false;
        }

        #endregion

        #region StringSegment

        /// <summary>
        /// Gets a <see cref="StringSegment"/> instance, which represents a segment of the specified <see cref="string">string</see>.
        /// No new string allocation occurs when using this method.
        /// </summary>
        /// <param name="s">The string to create the <see cref="StringSegment"/> from.</param>
        /// <param name="offset">The offset that points to the first character of the returned segment.</param>
        /// <param name="length">The desired length of the returned segment.</param>
        /// <returns>A <see cref="StringSegment"/> instance, which represents a segment of the specified <see cref="string">string</see>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment AsSegment(this string s, int offset, int length)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);
            if ((uint)offset > (uint)s.Length)
                Throw.ArgumentOutOfRangeException(Argument.offset);
            if ((uint)length > (uint)s.Length - offset)
                Throw.ArgumentOutOfRangeException(Argument.length);
            return new StringSegment(s, offset, length);
        }

        /// <summary>
        /// Gets a <see cref="StringSegment"/> instance, which represents a segment of the specified <see cref="string">string</see>.
        /// No new string allocation occurs when using this method.
        /// </summary>
        /// <param name="s">The string to create the <see cref="StringSegment"/> from.</param>
        /// <param name="offset">The offset that points to the first character of the returned segment.</param>
        /// <returns>A <see cref="StringSegment"/> instance, which represents a segment of the specified <see cref="string">string</see>.</returns>
        public static StringSegment AsSegment(this string s, int offset)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);
            if ((uint)offset > (uint)s.Length)
                Throw.ArgumentOutOfRangeException(Argument.offset);
            return new StringSegment(s, offset, s.Length - offset);
        }

        /// <summary>
        /// Gets the specified string as a <see cref="StringSegment"/> instance.
        /// </summary>
        /// <param name="s">The string to create the <see cref="StringSegment"/> from.</param>
        /// <returns>A <see cref="StringSegment"/> instance for the specified string.</returns>
        public static StringSegment AsSegment(this string? s) => s == null ? default : new StringSegment(s);

        #endregion

        #endregion
    }
}
