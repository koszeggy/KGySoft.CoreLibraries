#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SpanExtensions.cs
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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> types.
    /// </summary>
    /// <remarks><note>This class is available only in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
    public static partial class SpanExtensions
    {
        #region Fields

        private static readonly char[] newLineSeparators = { '\r', '\n' };

        #endregion

        #region Methods

        #region Public Methods

        #region Read with Advance

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next whitespace character and returns
        /// the consumed part without the whitespace. If the first character of <paramref name="rest"/> was a whitespace
        /// before the call, then an empty span is returned. If the whole <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> has been processed,
        /// then <paramref name="rest"/> will be <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> if the whole span has been processed.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by whitespace characters,
        /// or the complete original value of <paramref name="rest"/> if it contained no more whitespace characters.</returns>
        public static ReadOnlySpan<char> ReadToWhiteSpace(ref this ReadOnlySpan<char> rest)
        {
            if (rest.Length == 0)
            {
                rest = default;
                return default;
            }

            int pos = -1;
            for (int i = 0; i < rest.Length; i++)
            {
                if (Char.IsWhiteSpace(rest[i]))
                {
                    pos = i;
                    break;
                }
            }

            // last segment
            if (pos == -1)
            {
                ReadOnlySpan<char> result = rest;
                rest = default;
                return result;
            }
            // returning next segment and advance
            else
            {
                ReadOnlySpan<char> result = rest.Slice(0, pos);
                rest = rest.Slice(pos + 1);
                return result;
            }
        }

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next <paramref name="separator"/> character and returns
        /// the consumed part without the <paramref name="separator"/>. If the first character of <paramref name="rest"/> was a <paramref name="separator"/>
        /// before the call, then an empty span is returned. If the whole <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> has been processed,
        /// then <paramref name="rest"/> will be <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> if the whole span has been processed.</param>
        /// <param name="separator">The separator character to search in the specified span.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by the specified <paramref name="separator"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
        public static ReadOnlySpan<char> ReadToSeparator(ref this ReadOnlySpan<char> rest, char separator)
        {
            if (rest.Length == 0)
            {
                rest = default;
                return default;
            }

            int pos = rest.IndexOf(separator);

            // last segment
            if (pos == -1)
            {
                ReadOnlySpan<char> result = rest;
                rest = default;
                return result;
            }
            // returning next segment and advance
            else
            {
                ReadOnlySpan<char> result = rest.Slice(0, pos);
                rest = rest.Slice(pos + 1);
                return result;
            }
        }

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next <paramref name="separator"/> and returns
        /// the consumed part without the <paramref name="separator"/>. If <paramref name="rest"/> started with <paramref name="separator"/>
        /// before the call, then an empty span is returned. If the whole <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> has been processed,
        /// then <paramref name="rest"/> will be <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> if the whole span has been processed.</param>
        /// <param name="separator">The separator to search in the specified span.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by the specified <paramref name="separator"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
        public static ReadOnlySpan<char> ReadToSeparator(ref this ReadOnlySpan<char> rest, ReadOnlySpan<char> separator)
        {
            if (separator.Length == 0)
            {
                ReadOnlySpan<char> result = rest;
                rest = default;
                return result;
            }

            if (rest.Length == 0)
            {
                rest = default;
                return default;
            }

            int pos = rest.IndexOf(separator);

            // last segment
            if (pos == -1)
            {
                ReadOnlySpan<char> result = rest;
                rest = default;
                return result;
            }
            // returning next segment and advance
            else
            {
                ReadOnlySpan<char> result = rest.Slice(0, pos);
                rest = rest.Slice(pos + separator.Length);
                return result;
            }
        }

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next separator and returns
        /// the consumed part without the separator. If <paramref name="rest"/> started with one of the <paramref name="separators"/>
        /// before the call, then an empty span is returned. If the whole <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> has been processed,
        /// then <paramref name="rest"/> will be <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> if the whole span has been processed.</param>
        /// <param name="separators">The separators to search in the specified span.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> that contains the first segment of the original value of the <paramref name="rest"/> parameter delimited by any of the specified <paramref name="separators"/>,
        /// or the complete original value of <paramref name="rest"/> if it contained no more separators.</returns>
        public static ReadOnlySpan<char> ReadToSeparator(ref this ReadOnlySpan<char> rest, params char[] separators)
        {
            if (separators == null!)
                Throw.ArgumentNullException(Argument.separators);
            if (separators.Length <= 1)
            {
                if (separators.Length == 1)
                    return ReadToSeparator(ref rest, separators[0]);

                ReadOnlySpan<char> result = rest;
                rest = default;
                return result;
            }

            if (rest.Length == 0)
            {
                rest = default;
                return default;
            }

            int pos = rest.IndexOfAny(separators);

            // last segment
            if (pos == -1)
            {
                ReadOnlySpan<char> result = rest;
                rest = default;
                return result;
            }
            // returning next segment and advance
            else
            {
                ReadOnlySpan<char> result = rest.Slice(0, pos);
                rest = rest.Slice(pos + 1);
                return result;
            }
        }

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the current line and returns
        /// the consumed part without the newline character(s). If <paramref name="rest"/> started with a new line
        /// before the call, then an empty span is returned. If the whole <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> has been processed,
        /// then <paramref name="rest"/> will be <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> if the whole span has been processed.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> that contains the first line of the original value of the <paramref name="rest"/> parameter,
        /// or the complete original value of <paramref name="rest"/> if it contained no more lines.</returns>
        public static ReadOnlySpan<char> ReadLine(ref this ReadOnlySpan<char> rest)
        {
            if (rest.Length == 0)
            {
                rest = default;
                return default;
            }

            int pos = rest.IndexOfAny(newLineSeparators);

            // last segment
            if (pos == -1)
            {
                ReadOnlySpan<char> result = rest;
                rest = default;
                return result;
            }
            // returning next segment and advance
            else
            {
                ReadOnlySpan<char> result = rest.Slice(0, pos);
                rest = rest.Length > result.Length + 1 && rest[result.Length] == '\r' && rest[result.Length + 1] == '\n'
                    ? rest.Slice(pos + 2)
                    : rest.Slice(pos + 1);
                return result;
            }
        }

        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter consuming up to <paramref name="maxLength"/> characters and returns
        /// the consumed part. If <paramref name="rest"/> started with a new line
        /// before the call, then an empty span is returned. If the whole <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> has been processed,
        /// then <paramref name="rest"/> will be <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> after returning.
        /// </summary>
        /// <param name="rest">Represents the rest of the string to process. When this method returns, the value of this
        /// parameter will be the remaining unprocessed part, or <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see> if the whole span has been processed.</param>
        /// <param name="maxLength">The maximum number of characters to read.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> that contains the first line of the original value of the <paramref name="rest"/> parameter,
        /// or the complete original value of <paramref name="rest"/> if it contained no more than <paramref name="maxLength"/> characters.</returns>
        public static ReadOnlySpan<char> Read(ref this ReadOnlySpan<char> rest, int maxLength)
        {
            if (maxLength <= 0)
                Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.ArgumentMustBeLessThanOrEqualTo(0));

            ReadOnlySpan<char> result;
            if (maxLength >= rest.Length)
            {
                result = rest;
                rest = default;
                return result;
            }

            result = rest.Slice(0, maxLength);
            rest = rest.Slice(maxLength);
            return result;
        }

        #endregion

        #region Misc Tools

        /// <summary>
        /// Extracts content of a single or double quoted string.
        /// </summary>
        /// <param name="span">The span to be extracted from quotes.</param>
        /// <returns>If <paramref name="span"/> was surrounded by single or double quotes, returns a new string without the quotes; otherwise, returns <paramref name="span"/>.</returns>
        public static ReadOnlySpan<char> RemoveQuotes(this ReadOnlySpan<char> span)
            => span.Length < 2
                ? span
                : span.Length > 1 && (span[0] == '"' && span[span.Length - 1] == '"' || span[0] == '\'' && span[span.Length - 1] == '\'')
                    ? span.Slice(1, span.Length - 2)
                    : span;

        /// <summary>
        /// Extracts content of a single or double quoted string.
        /// </summary>
        /// <param name="span">The span to be extracted from quotes.</param>
        /// <returns>If <paramref name="span"/> was surrounded by single or double quotes, returns a new string without the quotes; otherwise, returns <paramref name="span"/>.</returns>
        public static Span<char> RemoveQuotes(this Span<char> span)
            => span.Length < 2
                ? span
                : span.Length > 1 && (span[0] == '"' && span[span.Length - 1] == '"' || span[0] == '\'' && span[span.Length - 1] == '\'')
                    ? span.Slice(1, span.Length - 2)
                    : span;

        #endregion

        #region Parsing

        /// <summary>
        /// Tries to convert the specified <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to an <see cref="Enum"/> value of <typeparamref name="TEnum"/> type.
        /// No string allocation occurs when using this method.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see cref="Enum"/>.</typeparam>
        /// <param name="s">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to convert.</param>
        /// <param name="definedOnly">If <see langword="true"/>, the result can only be a defined value in the specified <typeparamref name="TEnum"/> type.
        /// If <see langword="false"/>, the result can be a non-defined value, too.</param>
        /// <returns>A non-<see langword="null"/>&#160;value if the conversion was successful; otherwise, <see langword="null"/>.</returns>
        public static TEnum? ToEnum<TEnum>(this ReadOnlySpan<char> s, bool definedOnly = false)
            where TEnum : struct, Enum
        {
            if (s.IsEmpty)
                return null;

            if (!Enum<TEnum>.TryParse(s, out TEnum value))
                return null;

            return !definedOnly || Enum<TEnum>.IsDefined(value) ? value : (TEnum?)null;
        }

        /// <summary>
        /// Parses an object of type <typeparamref name="T"/> from a <see cref="string"/> represented by a <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value.
        /// Firstly, it tries to parse the type natively. If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/>
        /// or a registered conversion that can convert from string, then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the return value.</typeparam>
        /// <param name="s">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value to parse. If represents <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then the method returns <see langword="null"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An instance of <typeparamref name="T"/>, which is the result of the parsing. A <see langword="null"/>&#160;reference can be returned if <paramref name="s"/> represents <see langword="null"/>, and <typeparamref name="T"/> is a reference or nullable type.</returns>
        /// <remarks>
        /// <para>The following types are parsed natively:
        /// <list type="bullet">
        /// <item><description><see cref="Enum"/> based types</description></item>
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
        /// <note>Apart from <see cref="Enum"/> and <see cref="Type"/> types, no string allocation occurs when parsing any of the types above.</note>
        /// </para>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;extension methods.
        /// If a registered conversion can convert from <see cref="string">string</see>, then it can be used, though in that case a string allocation will occur.</para>
        /// <para>A <see cref="TypeConverter"/> can be registered by the <see cref="TypeExtensions.RegisterTypeConverter{TConverter}">RegisterTypeConverter</see>&#160;extension method.
        /// If a type converter can convert from <see cref="string">string</see>, then it can be used, though in that case a string allocation will occur.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">Parameter <paramref name="s"/> cannot be parsed as <typeparamref name="T"/>.</exception>
        [return:MaybeNull]public static T Parse<T>(this ReadOnlySpan<char> s, CultureInfo? culture = null)
        {
            if (!Parser.TryParse(s, culture, out T? value, out Exception? error))
                Throw.ArgumentException(Argument.obj, Res.SpanExtensionsCannotParseAsType(s, typeof(T)), error);
            return value;
        }

        /// <summary>
        /// Parses an object from a <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value.
        /// Firstly, it tries to parse the type natively. If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/>
        /// or a registered conversion that can convert from string, then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> overload for details.
        /// </summary>
        /// <returns>An object of <paramref name="type"/>, which is the result of the parsing.</returns>
        /// <param name="s">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value to parse. If represents <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then the method returns <see langword="null"/>.</param>
        /// <param name="type">The desired type of the return value.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>The parsed value. A <see langword="null"/>&#160;reference can be returned if <paramref name="s"/> represents <see langword="null"/>, and <paramref name="type"/> is a reference or nullable type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Parameter <paramref name="s"/> cannot be parsed as <paramref name="type"/>.</exception>
        public static object? Parse(this ReadOnlySpan<char> s, Type type, CultureInfo? culture = null)
        {
            if (!Parser.TryParse(s, type, culture, true, false, out object? value, out Exception? error) || !type.CanAcceptValue(value))
                Throw.ArgumentException(Argument.obj, Res.SpanExtensionsCannotParseAsType(s, type), error);
            return value;
        }

        /// <summary>
        /// Tries to parse an object of type <typeparamref name="T"/> from a <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value.
        /// Firstly, it tries to parse the type natively. If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/>
        /// or a registered conversion that can convert from string, then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="s">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value to parse. If represents <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.
        /// It will be <see langword="null"/>&#160;if <paramref name="s"/> represents <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <typeparamref name="T"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse<T>(this ReadOnlySpan<char> s, CultureInfo? culture, [MaybeNull]out T value)
            => Parser.TryParse(s, culture, out value, out var _);

        /// <summary>
        /// Tries to parse an object of type <typeparamref name="T"/> from a <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value.
        /// Firstly, it tries to parse the type natively. If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/>
        /// or a registered conversion that can convert from string, then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="s">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value to parse. If represents <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.
        /// It will be <see langword="null"/>&#160;if <paramref name="s"/> represents <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <typeparamref name="T"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse<T>(this ReadOnlySpan<char> s, [MaybeNull]out T value) => TryParse(s, null, out value);

        /// <summary>
        /// Tries to parse an object of type <paramref name="type"/> from a <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value.
        /// Firstly, it tries to parse the type natively. If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/>
        /// or a registered conversion that can convert from string, then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <param name="s">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value to parse. If <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="type">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.
        /// It will be <see langword="null"/>, if <paramref name="s"/> represents <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <paramref name="type"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static bool TryParse(this ReadOnlySpan<char> s, Type type, CultureInfo culture, out object? value)
            => Parser.TryParse(s, type, culture, true, false, out value, out var _);

        /// <summary>
        /// Tries to parse an object of type <paramref name="type"/> from a <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value.
        /// Firstly, it tries to parse the type natively. If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/>
        /// or a registered conversion that can convert from string, then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <param name="s">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value to parse. If <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="type">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.
        /// It will be <see langword="null"/>, if <paramref name="s"/> represents <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <paramref name="type"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static bool TryParse(this ReadOnlySpan<char> s, Type type, out object? value)
            => Parser.TryParse(s, type, null, true, false, out value, out var _);

        #endregion

        #endregion

        #region Internal Methods

        internal static bool TryParseIntQuick(this ReadOnlySpan<char> s, bool allowNegative, ulong max, out ulong result)
        {
            Debug.Assert(s.Length > 0, $"Nonzero length is expected in {nameof(TryParseIntQuick)}");
            Debug.Assert(!allowNegative || max < UInt64.MaxValue, "If negative values are allowed max should be less than UInt64.MaxValue");

            result = 0UL;
            bool isNegative = false;
            int i = 0;

            switch (s[0])
            {
                case '+':
                    i += 1;
                    break;
                case '-':
                    isNegative = true;
                    i += 1;
                    break;
            }

            ulong value = 0UL;
            while (i < s.Length)
            {
                uint digit = s[i] - (uint)'0';
                if (digit > 9)
                    return false;

                ulong newValue = value * 10 + digit;

                // overflow
                if (newValue < value)
                    return false;

                value = newValue;
                i += 1;
            }

            if (isNegative)
            {
                if (value == 0)
                    return true;

                // for negative values the MaxValue of the appropriate range is expected (eg 127 for SByte)
                if (!allowNegative || value > max + 1)
                    return false;

                result = (ulong)-(long)value;
                return true;
            }

            if (value > max)
                return false;

            result = value;
            return true;
        }

        internal static bool TryGetNextSegment(this ref ReadOnlySpan<char> s, ReadOnlySpan<char> separator, out ReadOnlySpan<char> result)
        {
            if (s.Length == 0)
            {
                result = default;
                return false;
            }

            int pos = s.IndexOf(separator);

            // last segment
            if (pos == -1)
            {
                result = s;
                s = default;
                return true;
            }

            // returning next segment and advance
            result = s.Slice(0, pos);
            s = s.Slice(pos + separator.Length);
            return true;
        }

        internal static bool TryWrite(this Span<char> destination, string value, out int charsWritten)
        {
            if (value.AsSpan().TryCopyTo(destination))
            {
                charsWritten = value.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        internal static void Append(this ref Span<char> destination, ReadOnlySpan<char> value)
        {
            Debug.Assert(destination.Length >= value.Length);
            value.CopyTo(destination);
            destination = destination.Slice(value.Length);
        }

        #endregion

        #endregion
    }
}
#endif