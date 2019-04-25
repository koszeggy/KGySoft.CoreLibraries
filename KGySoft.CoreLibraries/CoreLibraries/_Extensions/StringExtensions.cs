#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringExtensions.cs
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
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
        /// <param name="s">The string to be extracted from quotes.</param>
        /// <returns>If <paramref name="s"/> was surrounded by single or double quotes, returns a new string without the quotes; otherwise, returns <paramref name="s"/>.</returns>
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
        /// Converts the passed string to a <see cref="Regex"/> that matches wildcard characters (? and *).
        /// </summary>
        /// <param name="s">The string containing possible wildcard characters.</param>
        /// <returns>A <see cref="Regex"/> instance that matches the pattern of the given string in <paramref name="s"/>.</returns>
        public static Regex ToWildcardsRegex(this string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);
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
        public static string Repeat(this string s, int count)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.ArgumentOutOfRange);

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
        /// Parses delimited hex values from a string into an array of bytes.
        /// </summary>
        /// <param name="s">A string containing delimited hex values.</param>
        /// <param name="separator">A separator delimiting the hex values. If <see langword="null"/>, then <paramref name="s"/> is parsed as a continuous hex stream.</param>
        /// <returns>A byte array containing the hex values as bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="separator"/> is <see langword="null"/>&#160;and <paramref name="s"/> does not consist of event amount of hex digits.</exception>
        /// <exception cref="FormatException"><paramref name="s"/> is not of the correct format.</exception>
        /// <exception cref="OverflowException">A value in <paramref name="s"/> does not fit in the range of a <see cref="byte">byte</see> value.</exception>
        public static byte[] ParseHexBytes(this string s, string separator)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);

            if (string.IsNullOrEmpty(separator))
                return ParseHexBytes(s);

            string[] values = s.Split(new string[] { separator }, StringSplitOptions.None);
            byte[] result = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
                result[i] = Byte.Parse(values[i].Trim(), NumberStyles.HexNumber);

            return result;
        }

        /// <summary>
        /// Parses a continuous hex stream from a string.
        /// </summary>
        /// <param name="s">A string containing continuous hex values without delimiters.</param>
        /// <returns>A byte array containing the hex values as bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="s"/> does not consist of event amount of hex digits.</exception>
        /// <exception cref="FormatException"><paramref name="s"/> is not of the correct format.</exception>
        /// <exception cref="OverflowException">A value in <paramref name="s"/> does not fit in the range of a <see cref="byte">byte</see> value.</exception>
        public static byte[] ParseHexBytes(this string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);

            if (s.Length == 0)
                return new byte[0];

            if (s.Length % 2 != 0)
                throw new ArgumentException(Res.StringExtensionsSourceLengthNotEven, nameof(s));

            byte[] result = new byte[s.Length >> 1];
            for (int i = 0; i < (s.Length >> 1); i++)
                result[i] = Byte.Parse(s.Substring(i << 1, 2), NumberStyles.HexNumber);

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
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);

            if (String.IsNullOrEmpty(separator))
                throw new ArgumentException(Res.StringExtensionsSeparatorNullOrEmpty, nameof(separator));

            string[] values = s.Split(new string[] { separator }, StringSplitOptions.None);
            byte[] result = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
                result[i] = Byte.Parse(values[i].Trim());
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
        public static TEnum? ToEnum<TEnum>(this string s, bool definedOnly = false)
            where TEnum : struct, IConvertible // replaced to System.Enum by RecompILer
        {
            if (s == null)
                return null;

            if (!Enum<TEnum>.TryParse(s, out TEnum value))
                return null;

            return !definedOnly || Enum<TEnum>.IsDefined(value) ? value : (TEnum?)null;
        }

        /// <summary>
        /// Parses an object of type <typeparamref name="T"/> from a <see cref="string"/> value. Firstly, it tries to parse the type natively.
        /// If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the return value.</typeparam>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then returns <see langword="null"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An object of <typeparamref name="T"/>, which is the result of the parsing.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <para>A <see cref="TypeConverter"/> can be registered by the <see cref="TypeExtensions.RegisterTypeConverter{TConverter}">RegisterTypeConverter</see>&#160;<see cref="Type"/> extension method.</para>
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
        /// <exception cref="ArgumentNullException"><typeparamref name="T"/> not nullable and <paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Parameter <paramref name="s"/> cannot be parsed as <typeparamref name="T"/>.</exception>
        public static T Parse<T>(this string s, CultureInfo culture = null)
            => TryParse(s, typeof(T), culture, out object value, out Exception error) && typeof(T).CanAcceptValue(value)
                ? (T)value
                : throw new ArgumentException(Res.StringExtensionsCannotParseAsType(s, typeof(T)), nameof(s), error);

        /// <summary>
        /// Parses an object from a <see cref="string"/> value. Firstly, it tries to parse the type natively.
        /// If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> overload for details.
        /// </summary>
        /// <returns>An object of <paramref name="type"/>, which is the result of the parsing.</returns>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then returns <see langword="null"/>.</param>
        /// <param name="type">The desired type of the return value.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>, or <paramref name="type"/> is not nullable and <paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Parameter <paramref name="s"/> cannot be parsed as <paramref name="type"/>.</exception>
        public static object Parse(this string s, Type type, CultureInfo culture = null)
            => TryParse(s, type, culture, out object value, out Exception error) && type.CanAcceptValue(value)
                ? value
                : throw new ArgumentException(Res.StringExtensionsCannotParseAsType(s, type), nameof(s), error);

        /// <summary>
        /// Tries to parse an object of type <typeparamref name="T"/> from a <see cref="string"/> value. Firstly, it tries to parse the type natively.
        /// If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <typeparamref name="T"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse<T>(this string s, CultureInfo culture, out T value)
        {
            value = default;
            if (!TryParse(s, typeof(T), culture, out object result, out var _) || !typeof(T).CanAcceptValue(result))
                return false;

            value = (T)result;
            return true;
        }

        /// <summary>
        /// Tries to parse an object of type <typeparamref name="T"/> from a <see cref="string"/> value. Firstly, it tries to parse the type natively.
        /// If <typeparamref name="T"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <typeparam name="T">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <typeparamref name="T"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <typeparamref name="T"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse<T>(this string s, out T value) => TryParse(s, null, out value);

        /// <summary>
        /// Tries to parse an object of type <paramref name="type"/> from a <see cref="string"/> value. Firstly, it tries to parse the type natively.
        /// If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="type">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="culture">The culture to use for the parsing. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <paramref name="type"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(this string s, Type type, CultureInfo culture, out object value) => TryParse(s, type, culture, out value, out var _);

        /// <summary>
        /// Tries to parse an object of type <paramref name="type"/> from a <see cref="string"/> value. Firstly, it tries to parse the type natively.
        /// If <paramref name="type"/> cannot be parsed natively but the type has a <see cref="TypeConverter"/> or a registered conversion that can convert from string,
        /// then the type converter or conversion will be used.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Parse{T}"/> method for details.
        /// </summary>
        /// <param name="s">The string value to parse. If <see langword="null"/>&#160;and <paramref name="type"/> is a reference or nullable type, then <paramref name="value"/> will be <see langword="null"/>.</param>
        /// <param name="type">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the parsing.</param>
        /// <returns><see langword="true"/>, if <paramref name="s"/> could be parsed as <paramref name="type"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(this string s, Type type, out object value) => TryParse(s, type, null, out value, out var _);

        private static bool TryParse(string s, Type type, CultureInfo culture, out object value, out Exception error)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            error = null;
            value = null;
            if (s == null)
            {
                if (type.CanAcceptValue(null))
                    return true;

                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);
            }

            if (type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            // ReSharper disable once PossibleNullReferenceException
            if (type.IsByRef)
                type = type.GetElementType();

            if (culture == null)
                culture = CultureInfo.InvariantCulture;

            try
            {
                // ReSharper disable once PossibleNullReferenceException
                if (type.IsEnum)
                {
#if NET35 || NET40 || NET45
                    value = Enum.Parse(type, s);
                    return true;
#else
#error .NET version is not supported. Use non-generic TryParse (available in .NET Core)
#endif
                }
                if (type == Reflector.StringType)
                {
                    value = s;
                    return true;
                }
                if (type == Reflector.CharType)
                {
                    if (!Char.TryParse(s, out char result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.ByteType)
                {
                    if (!Byte.TryParse(s, out byte result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.SByteType)
                {
                    if (!SByte.TryParse(s, out sbyte result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.ShortType)
                {
                    if (!Int16.TryParse(s, out short result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.UShortType)
                {
                    if (!UInt16.TryParse(s, out ushort result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.IntType)
                {
                    if (!Int32.TryParse(s, out int result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.UIntType)
                {
                    if (!UInt32.TryParse(s, out uint result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.LongType)
                {
                    if (!Int64.TryParse(s, out long result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.ULongType)
                {
                    if (!UInt64.TryParse(s, out ulong result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.IntPtrType)
                {
                    if (!Int64.TryParse(s, out long result))
                        return false;
                    value = new IntPtr(result);
                    return true;
                }
                if (type == Reflector.UIntPtrType)
                {
                    if (!UInt64.TryParse(s, out ulong result))
                        return false;
                    value = new UIntPtr(result);
                    return true;
                }
                if (type == Reflector.FloatType)
                {
                    if (!Single.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture, out float result))
                        return false;
                    if (result.Equals(0f) && s.Trim().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                        result = -0f;
                    value = result;
                    return true;
                }
                if (type == Reflector.DoubleType)
                {
                    if (!Double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture, out double result))
                        return false;
                    if (result.Equals(0d) && s.Trim().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                        result = -0d;
                    value = result;
                    return true;
                }
                if (type == Reflector.DecimalType)
                {
                    if (!Decimal.TryParse(s, NumberStyles.Number, culture, out decimal result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.TimeSpanType)
                {
                    if (!TimeSpan.TryParse(s, out TimeSpan result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.BoolType)
                {
                    if (s.EqualsAny(StringComparison.OrdinalIgnoreCase, "true", "1"))
                    {
                        value = true;
                        return true;
                    }
                    if (s.EqualsAny(StringComparison.OrdinalIgnoreCase, "false", "0"))
                    {
                        value = false;
                        return true;
                    }

                    return false;
                }
                if (type.In(Reflector.Type, Reflector.RuntimeType
#if !NET35 && !NET40
                    , Reflector.TypeInfo
#endif
                ))
                {
                    value = Reflector.ResolveType(s);
                    return value != null;
                }
                if (type == Reflector.DateTimeType)
                {
                    DateTimeStyles style = s.EndsWith("Z", StringComparison.Ordinal) ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                    if (!DateTime.TryParse(s, culture, style, out DateTime result))
                        return false;
                    value = result;
                    return true;
                }
                if (type == Reflector.DateTimeOffsetType)
                {
                    DateTimeStyles style = s.EndsWith("Z", StringComparison.Ordinal) ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                    if (!DateTimeOffset.TryParse(s, culture, style, out DateTimeOffset result))
                        return false;
                    value = result;
                    return true;
                }

                // a registered converter from string
                switch (Reflector.StringType.GetConversions(type, true).ElementAtOrDefault(0))
                {
                    case ConversionAttempt conversionAttempt:
                        if (conversionAttempt.Invoke(s, type, culture, out value) && type.CanAcceptValue(value))
                            return true;
                        break;
                    case Conversion conversion:
                        value = conversion.Invoke(s, type, culture);
                        if (type.CanAcceptValue(value))
                            return true;
                        break;
                }

                // Trying type converter as a fallback
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter.CanConvertFrom(Reflector.StringType))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute - false alarm, context can be null
                    value = converter.ConvertFrom(null, culture, s);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                error = e;
                value = null;
                return false;
            }
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
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);
            if (value == null)
                throw new ArgumentNullException(nameof(value), Res.ArgumentNull);
            if (!Enum<StringComparison>.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison), Res.EnumOutOfRange(comparison));

            return s.IndexOf(value, comparison) >= 0;
        }

        /// <summary>
        /// Gets whether the specified string <paramref name="s"/> equals any of the strings in the specified <paramref name="set"/> set by case sensitive ordinal comparison.
        /// </summary>
        /// <param name="s">A <see cref="string"/> instance that is to be compared to each element of the <paramref name="set"/>.</param>
        /// <param name="set">An <see cref="Array"/> of strings.</param>
        /// <returns><see langword="true"/>&#160;if string <paramref name="s"/> equals any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/>&#160;if string <paramref name="s"/> equals any of the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        public static bool EqualsAny(this string s, StringComparer comparer, params string[] set)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer), Res.ArgumentNull);

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
        public static bool EqualsAny(this string s, StringComparison comparison, params string[] set)
        {
            if (!Enum<StringComparison>.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison), Res.EnumOutOfRange(comparison));

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
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);
            if (set == null)
                throw new ArgumentNullException(nameof(set), Res.ArgumentNull);
            if (!Enum<StringComparison>.IsDefined(comparison))
                throw new ArgumentOutOfRangeException(nameof(comparison), Res.EnumOutOfRange(comparison));

            var index = -1;
            foreach (var str in set)
            {
                int pos = s.IndexOf(str ?? throw new ArgumentException(Res.ArgumentContainsNull, nameof(set)), comparison);
                if (pos == 0)
                    return 0;
                if (pos >= 0 && pos < index)
                    index = pos;
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
                throw new ArgumentOutOfRangeException(nameof(comparison), Res.EnumOutOfRange(comparison));
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);
            if (set == null)
                throw new ArgumentNullException(nameof(set), Res.ArgumentNull);

            foreach (var str in set)
            {
                if (s.IndexOf(str ?? throw new ArgumentException(Res.ArgumentContainsNull, nameof(set)), comparison) >= 0)
                    return true;
            }

            return false;
        }

        #endregion

        #endregion
    }
}
