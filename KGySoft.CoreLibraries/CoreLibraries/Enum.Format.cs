﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Enum.Format.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
using System.Globalization;
#endif
using System.Security;
using System.Text;

#endregion

#region Suppressions

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER)
#pragma warning disable CS8602 // Dereference of a possibly null reference
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    public static partial class Enum<TEnum>
    {
        #region Methods

        #region Public Methods

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given <see langword="enum"/> value specified in the <paramref name="value"/> parameter.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="format">The formatting options.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/> or is empty, then comma-space (<c>, </c>) separator is used. This parameter is optional.
        /// <br/>Default value: <c>, </c>.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="format"/>.</exception>
        public static string ToString(TEnum value, EnumFormattingOptions format, string? separator = EnumExtensions.DefaultFormatSeparator)
        {
            if ((uint)format > (uint)EnumFormattingOptions.Number)
                Throw.EnumArgumentOutOfRange(Argument.format, value);

            if (format == EnumFormattingOptions.DistinctFlags)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                if (EnvironmentHelper.IsPartiallyTrustedDomain)
                    return FormatDistinctFlagsPartiallyTrusted(value, separator);
#endif
                return FormatDistinctFlags(value, separator);
            }

            if (format == EnumFormattingOptions.Number)
                return ToNumericString(converter.ToUInt64(value));

            // returning as flags
            if ((format == EnumFormattingOptions.Auto && isFlags) || format == EnumFormattingOptions.CompoundFlagsOrNumber || format == EnumFormattingOptions.CompoundFlagsAndNumber)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                if (EnvironmentHelper.IsPartiallyTrustedDomain)
                    return FormatCompoundFlagsPartiallyTrusted(value, separator, format == EnumFormattingOptions.CompoundFlagsAndNumber);
#endif
                return FormatCompoundFlags(value, separator, format == EnumFormattingOptions.CompoundFlagsAndNumber);
            }

            // defined value exists
            if (ValueNamePairs.TryGetValue(value, out string? name))
                return name;

            // if single value is requested returning a number
            return ToNumericString(converter.ToUInt64(value));
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given <see langword="enum"/> value specified in the <paramref name="value"/> parameter.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        public static string ToString(TEnum value)
        {
            // returning as flags
            if (isFlags)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                if (EnvironmentHelper.IsPartiallyTrustedDomain)
                    return FormatCompoundFlagsPartiallyTrusted(value, EnumExtensions.DefaultFormatSeparator, false);
#endif

                return FormatCompoundFlags(value, EnumExtensions.DefaultFormatSeparator, false);
            }

            // defined value exists
            if (ValueNamePairs.TryGetValue(value, out string? name))
                return name;

            // defined value does not exist: returning a number
            return ToNumericString(converter.ToUInt64(value));
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given <see langword="enum"/> value specified in the <paramref name="value"/> parameter.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/> or is empty, then comma-space (<c>, </c>) separator is used.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        public static string ToString(TEnum value, string? separator) => ToString(value, EnumFormattingOptions.Auto, separator);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Tries to format the <paramref name="value"/> of the current <typeparamref name="TEnum"/> instance into the provided span of characters.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value to be formatted.</param>
        /// <param name="destination">The target span of characters of the formatted value.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">The formatting options. This parameter is optional.
        /// <br/>Default value: <see cref="EnumFormattingOptions.Auto"/>.</param>
        /// <param name="separator">A span containing the separator in case of flags formatting. If empty, then comma-space (<c>, </c>) separator is used. This parameter is optional.
        /// <br/>Default value: <see cref="Span{T}.Empty"><![CDATA[Span<char>.Empty]]></see>.</param>
        /// <returns><see langword="true"/>, if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public static bool TryFormat(TEnum value, Span<char> destination, out int charsWritten, EnumFormattingOptions format = EnumFormattingOptions.Auto, ReadOnlySpan<char> separator = default)
        {
            if ((uint)format > (uint)EnumFormattingOptions.Number)
                Throw.EnumArgumentOutOfRange(Argument.format, value);

            if (format == EnumFormattingOptions.DistinctFlags)
                return TryFormatDistinctFlags(value, destination, out charsWritten, separator);

            if (format == EnumFormattingOptions.Number)
                return TryFormatNumericString(converter.ToUInt64(value), destination, out charsWritten);

            // returning as flags
            if ((format == EnumFormattingOptions.Auto && isFlags) || format == EnumFormattingOptions.CompoundFlagsOrNumber || format == EnumFormattingOptions.CompoundFlagsAndNumber)
                return TryFormatCompoundFlags(value, destination, out charsWritten, separator, format == EnumFormattingOptions.CompoundFlagsAndNumber);

            // defined value exists
            if (ValueNamePairs.TryGetValue(value, out string? name))
                return destination.TryWrite(name, out charsWritten);

            // if single value is requested returning a number
            return TryFormatNumericString(converter.ToUInt64(value), destination, out charsWritten);
        }

        /// <summary>
        /// Tries to format the <paramref name="value"/> of the current <typeparamref name="TEnum"/> instance
        /// into the provided span of characters using <see cref="EnumFormattingOptions.Auto"/> formatting options.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value to be formatted.</param>
        /// <param name="destination">The target span of characters of the formatted value.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="separator">A span containing the separator in case of flags formatting. If empty, then comma-space (<c>, </c>) separator is used.</param>
        /// <returns><see langword="true"/>, if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public static bool TryFormat(TEnum value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> separator)
            => TryFormat(value, destination, out charsWritten, EnumFormattingOptions.Auto, separator);

        /// <summary>
        /// Tries to format the <paramref name="value"/> of the current <typeparamref name="TEnum"/> instance into the provided span of UTF-8 bytes.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value to be formatted.</param>
        /// <param name="utf8Destination">The target span of UTF-8 bytes of the formatted value.</param>
        /// <param name="bytesWritten">When this method returns, the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <param name="format">The formatting options. This parameter is optional.
        /// <br/>Default value: <see cref="EnumFormattingOptions.Auto"/>.</param>
        /// <param name="separator">A span containing the separator in case of flags formatting. If empty, then comma-space (<c>, </c>) separator is used. This parameter is optional.
        /// <br/>Default value: <see cref="Span{T}.Empty"><![CDATA[Span<byte>.Empty]]></see>.</param>
        /// <returns><see langword="true"/>, if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public static bool TryFormat(TEnum value, Span<byte> utf8Destination, out int bytesWritten, EnumFormattingOptions format = EnumFormattingOptions.Auto, ReadOnlySpan<byte> separator = default)
        {
            if ((uint)format > (uint)EnumFormattingOptions.Number)
                Throw.EnumArgumentOutOfRange(Argument.format, value);

            if (format == EnumFormattingOptions.DistinctFlags)
                return TryFormatDistinctFlags(value, utf8Destination, out bytesWritten, separator);

            if (format == EnumFormattingOptions.Number)
                return TryFormatNumericString(converter.ToUInt64(value), utf8Destination, out bytesWritten);

            // returning as flags
            if ((format == EnumFormattingOptions.Auto && isFlags) || format == EnumFormattingOptions.CompoundFlagsOrNumber || format == EnumFormattingOptions.CompoundFlagsAndNumber)
                return TryFormatCompoundFlags(value, utf8Destination, out bytesWritten, separator, format == EnumFormattingOptions.CompoundFlagsAndNumber);

            // defined value exists
            if (ValueUtf8NamePairs.TryGetValue(value, out byte[]? name))
                return utf8Destination.TryWrite(name, out bytesWritten);

            // if single value is requested returning a number
            return TryFormatNumericString(converter.ToUInt64(value), utf8Destination, out bytesWritten);
        }

        /// <summary>
        /// Tries to format the <paramref name="value"/> of the current <typeparamref name="TEnum"/> instance
        /// into the provided span of UTF-8 bytes using <see cref="EnumFormattingOptions.Auto"/> formatting options.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value to be formatted.</param>
        /// <param name="utf8Destination">The target span of UTF-8 bytes of the formatted value.</param>
        /// <param name="bytesWritten">When this method returns, the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <param name="separator">A span containing the separator in case of flags formatting. If empty, then comma-space (<c>, </c>) separator is used.</param>
        /// <returns><see langword="true"/>, if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        public static bool TryFormat(TEnum value, Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<byte> separator)
            => TryFormat(value, utf8Destination, out bytesWritten, EnumFormattingOptions.Auto, separator);

#endif

        #endregion

        #region Private Methods

        [SecuritySafeCritical]
        private static unsafe string FormatDistinctFlags(TEnum e, string? separator)
        {
            EnsureRawValueNamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return Zero;

            ulong value = origRawValue;

            // Unlike in FormatCompoundFlags we use it as a queue and we may use every position:
            // MinValue: Flag is unset; <0: Flag has no name (digits size are stored); >=0: Name index
            int* resultsQueue = stackalloc int[underlyingInfo.BitSize];

            int maxFlag = 0; // Indicates the valuable length of resultsQueue
            int resultLength = 0; // Indicates the length of the string to be allocated

            for (int i = 0; i < underlyingInfo.BitSize; i++)
            {
                ulong flagValue = 1UL << i;

                // unset flag
                if ((value & flagValue) == 0UL)
                {
                    resultsQueue[i] = Int32.MinValue;
                    continue;
                }

                maxFlag = i;
                int nameIndex = FindIndex(flagValue);

                // flag with name
                if (nameIndex >= 0)
                {
                    // The value can be covered by a single name
                    if (origRawValue == flagValue)
                        return rawValueNamePairs.Names![nameIndex];

                    resultsQueue[i] = nameIndex;
                    resultLength += rawValueNamePairs.Names![nameIndex].Length;
                }
                // flag without name
                else
                {
                    // The numeric value of the single flag can be returned
                    if (origRawValue == flagValue)
                        return ToNumericString(flagValue);

                    int size = GetStringLength(flagValue);
                    resultsQueue[i] = -size;
                    resultLength += size;
                }

                value &= ~flagValue;
                if (value == 0UL)
                    break;
            }

            if (String.IsNullOrEmpty(separator))
                separator = EnumExtensions.DefaultFormatSeparator;

            // Building result. Mutating a preallocated string is much faster than StringBuilder.
            string result = new String('\0', resultLength + separator.Length * (origRawValue.GetFlagsCount() - 1));

            fixed (char* pinnedResult = result)
            {
                var sb = new MutableStringBuilder(pinnedResult, result.Length);

                // Applying the names/numbers
                for (int i = 0; i <= maxFlag; i++)
                {
                    if (resultsQueue[i] >= 0)
                        sb.Append(rawValueNamePairs.Names![resultsQueue[i]]);
                    else if (resultsQueue[i] == Int32.MinValue)
                        continue;
                    else
                        ToNumericString(1UL << i, -resultsQueue[i], ref sb);

                    if (i < maxFlag)
                        sb.Append(separator);
                }
            }

            return result;
        }

#if NETFRAMEWORK || NETSTANDARD2_0
        private static string FormatDistinctFlagsPartiallyTrusted(TEnum e, string? separator)
        {
            EnsureRawValueNamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return Zero;

            ulong value = origRawValue;

            // Unlike in FormatCompoundFlags we use it as a queue and we may use every position:
            // MinValue: Flag is unset; <0: Flag has no name (digits size are stored); >=0: Name index
            int[] resultsQueue = new int[underlyingInfo.BitSize];

            int maxFlag = 0; // Indicates the valuable length of resultsQueue
            int resultLength = 0; // Indicates the length of the string to be allocated

            for (int i = 0; i < underlyingInfo.BitSize; i++)
            {
                ulong flagValue = 1UL << i;

                // unset flag
                if ((value & flagValue) == 0UL)
                {
                    resultsQueue[i] = Int32.MinValue;
                    continue;
                }

                maxFlag = i;
                int nameIndex = FindIndex(flagValue);

                // flag with name
                if (nameIndex >= 0)
                {
                    // The value can be covered by a single name
                    if (origRawValue == flagValue)
                        return rawValueNamePairs.Names![nameIndex];

                    resultsQueue[i] = nameIndex;
                    resultLength += rawValueNamePairs.Names![nameIndex].Length;
                }
                // flag without name
                else
                {
                    // The numeric value of the single flag can be returned
                    if (origRawValue == flagValue)
                        return ToNumericString(flagValue);

                    int size = GetStringLength(flagValue);
                    resultsQueue[i] = -size;
                    resultLength += size;
                }

                value &= ~flagValue;
                if (value == 0UL)
                    break;
            }

            if (String.IsNullOrEmpty(separator))
                separator = EnumExtensions.DefaultFormatSeparator;

            var sb = new StringBuilder(resultLength + separator!.Length * (origRawValue.GetFlagsCount() - 1));

            // Applying the names/numbers
            for (int i = 0; i <= maxFlag; i++)
            {
                if (resultsQueue[i] >= 0)
                    sb.Append(rawValueNamePairs.Names![resultsQueue[i]]);
                else if (resultsQueue[i] == Int32.MinValue)
                    continue;
                else
                    sb.Append(ToNumericString(1UL << i));

                if (i < maxFlag)
                    sb.Append(separator);
            }

            return sb.ToString();
        }
#endif

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static bool TryFormatDistinctFlags(TEnum e, Span<char> destination, out int charsWritten, ReadOnlySpan<char> separator)
        {
            EnsureRawValueNamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return destination.TryWrite(Zero, out charsWritten);

            ulong value = origRawValue;

            // Unlike in TryFormatCompoundFlags we use it as a queue and we may use every position:
            // MinValue: Flag is unset; -1: Flag has no name; >=0: Name index
            Span<int> resultsQueue = stackalloc int[underlyingInfo.BitSize];

            int maxFlag = 0; // Indicates the valuable length of resultsQueue
            int resultLength = 0; // Indicates the length of the string to be allocated

            for (int i = 0; i < underlyingInfo.BitSize; i++)
            {
                ulong flagValue = 1UL << i;

                // unset flag
                if ((value & flagValue) == 0UL)
                {
                    resultsQueue[i] = Int32.MinValue;
                    continue;
                }

                maxFlag = i;
                int nameIndex = FindIndex(flagValue);

                // flag with name
                if (nameIndex >= 0)
                {
                    // The value can be covered by a single name
                    if (origRawValue == flagValue)
                        return destination.TryWrite(rawValueNamePairs.Names![nameIndex], out charsWritten);

                    resultsQueue[i] = nameIndex;
                    resultLength += rawValueNamePairs.Names![nameIndex].Length;
                }
                // flag without name
                else
                {
                    // The numeric value of the single flag can be returned
                    if (origRawValue == flagValue)
                        return TryFormatNumericString(flagValue, destination, out charsWritten);

                    resultsQueue[i] = -1;
                    resultLength += GetStringLength(flagValue);
                }

                value &= ~flagValue;
                if (value == 0UL)
                    break;
            }

            if (separator.IsEmpty)
                separator = EnumExtensions.DefaultFormatSeparator;

            int totalLength = resultLength + separator.Length * (origRawValue.GetFlagsCount() - 1);
            if (destination.Length < totalLength)
            {
                charsWritten = 0;
                return false;
            }

            // Applying the names/numbers
            for (int i = 0; i <= maxFlag; i++)
            {
                if (resultsQueue[i] >= 0)
                    destination.Append(rawValueNamePairs.Names![resultsQueue[i]]);
                else if (resultsQueue[i] == Int32.MinValue)
                    continue;
                else
                    ToNumericString(1UL << i, ref destination);

                if (i < maxFlag)
                    destination.Append(separator);
            }

            charsWritten = totalLength;
            return true;
        }

        private static bool TryFormatDistinctFlags(TEnum e, Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<byte> separator)
        {
            EnsureRawValueUtf8NamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return utf8Destination.TryWrite(ZeroUtf8, out bytesWritten);

            ulong value = origRawValue;

            // Unlike in TryFormatCompoundFlags we use it as a queue and we may use every position:
            // MinValue: Flag is unset; -1: Flag has no name; >=0: Name index
            Span<int> resultsQueue = stackalloc int[underlyingInfo.BitSize];

            int maxFlag = 0; // Indicates the valuable length of resultsQueue
            int resultLength = 0; // Indicates the length of the string to be allocated

            for (int i = 0; i < underlyingInfo.BitSize; i++)
            {
                ulong flagValue = 1UL << i;

                // unset flag
                if ((value & flagValue) == 0UL)
                {
                    resultsQueue[i] = Int32.MinValue;
                    continue;
                }

                maxFlag = i;
                int nameIndex = FindIndex(flagValue);

                // flag with name
                if (nameIndex >= 0)
                {
                    // The value can be covered by a single name
                    if (origRawValue == flagValue)
                        return utf8Destination.TryWrite(utf8Names.Names![nameIndex], out bytesWritten);

                    resultsQueue[i] = nameIndex;
                    resultLength += utf8Names.Names![nameIndex].Length;
                }
                // flag without name
                else
                {
                    // The numeric value of the single flag can be returned
                    if (origRawValue == flagValue)
                        return TryFormatNumericString(flagValue, utf8Destination, out bytesWritten);

                    resultsQueue[i] = -1;
                    resultLength += GetStringLength(flagValue);
                }

                value &= ~flagValue;
                if (value == 0UL)
                    break;
            }

            if (separator.IsEmpty)
                separator = EnumExtensions.DefaultFormatSeparatorUtf8;

            int totalLength = resultLength + separator.Length * (origRawValue.GetFlagsCount() - 1);
            if (utf8Destination.Length < totalLength)
            {
                bytesWritten = 0;
                return false;
            }

            // Applying the names/numbers
            for (int i = 0; i <= maxFlag; i++)
            {
                if (resultsQueue[i] >= 0)
                    utf8Destination.Append(utf8Names.Names![resultsQueue[i]]);
                else if (resultsQueue[i] == Int32.MinValue)
                    continue;
                else
                    ToNumericString(1UL << i, ref utf8Destination);

                if (i < maxFlag)
                    utf8Destination.Append(separator);
            }

            bytesWritten = totalLength;
            return true;
        }
#endif

        [SecuritySafeCritical]
        private static unsafe string FormatCompoundFlags(TEnum e, string? separator, bool allowNumberWithNames)
        {
            EnsureRawValueNamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return Zero;

            // Finally, thanks to the changes in .NET Core 3.0 (see https://github.com/dotnet/coreclr/pull/21254/files)
            // the System.Enum.ToString performance is not terrible anymore. This is also a similar solution (apart
            // from the feature differences). We can't use Span here because that is not available for all targets.
            ulong[] rawValues = rawValueNamePairs.RawValues!;
            ulong value = origRawValue;

            // Unlike in FormatDistinctFlags it is used as a stack because the largest value is added first.
            int* resultsStack = stackalloc int[underlyingInfo.BitSize];

            int resultsCount = 0; // Indicates the top of resultsStack
            int resultLength = 0; // Indicates the length of the string to be allocated

            // Processing existing values from largest to smallest
            for (int i = rawValues.Length - 1; value > 0 && i >= 0; i--)
            {
                ulong biggestUnprocessedValue = rawValues[i];
                if (biggestUnprocessedValue == 0UL)
                    break;

                if ((value & biggestUnprocessedValue) == biggestUnprocessedValue)
                {
                    // The value can be covered by a single name
                    if (origRawValue == biggestUnprocessedValue)
                        return rawValueNamePairs.Names![i];

                    resultsStack[resultsCount] = i;
                    resultLength += rawValueNamePairs.Names![i].Length;
                    resultsCount += 1;
                    value &= ~biggestUnprocessedValue;
                }
            }

            // There is a rest value but numbers cannot be mixed with names: returning a standalone number
            if (value != 0UL && !allowNumberWithNames)
                return ToNumericString(origRawValue);

            if (String.IsNullOrEmpty(separator))
                separator = EnumExtensions.DefaultFormatSeparator;

            int numericValueLen = 0;
            if (value != 0UL)
            {
                numericValueLen = GetStringLength(value);
                resultLength += numericValueLen;
                resultsCount += 1;
            }

            // Building result. Mutating a preallocated string is much faster than StringBuilder.
            string result = new String('\0', resultLength + separator.Length * (resultsCount - 1));
            fixed (char* pinnedResult = result)
            {
                var sb = new MutableStringBuilder(pinnedResult, result.Length);

                // Applying the number (if any)
                if (numericValueLen != 0)
                {
                    ToNumericString(value, numericValueLen, ref sb);
                    resultsCount -= 1;
                    if (resultsCount > 1)
                        sb.Append(separator);
                }

                // Applying the names
                for (int i = resultsCount - 1; i >= 0; i--)
                {
                    sb.Append(rawValueNamePairs.Names![resultsStack[i]]);

                    if (i > 0)
                        sb.Append(separator);
                }
            }

            return result;
        }

#if NETFRAMEWORK || NETSTANDARD2_0
        private static string FormatCompoundFlagsPartiallyTrusted(TEnum e, string? separator, bool allowNumberWithNames)
        {
            EnsureRawValueNamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return Zero;

            // Finally, thanks to the changes in .NET Core 3.0 (see https://github.com/dotnet/coreclr/pull/21254/files)
            // the System.Enum.ToString performance is not terrible anymore. This is also a similar solution (apart
            // from the feature differences). We can't use Span here because that is not available for all targets.
            ulong[] rawValues = rawValueNamePairs.RawValues!;
            ulong value = origRawValue;

            // Unlike in FormatDistinctFlags it is used as a stack because the largest value is added first.
            int[] resultsStack = new int[underlyingInfo.BitSize];

            int resultsCount = 0; // Indicates the top of resultsStack
            int resultLength = 0; // Indicates the length of the string to be allocated

            // Processing existing values from largest to smallest
            for (int i = rawValues.Length - 1; value > 0 && i >= 0; i--)
            {
                ulong biggestUnprocessedValue = rawValues[i];
                if (biggestUnprocessedValue == 0UL)
                    break;

                if ((value & biggestUnprocessedValue) == biggestUnprocessedValue)
                {
                    // The value can be covered by a single name
                    if (origRawValue == biggestUnprocessedValue)
                        return rawValueNamePairs.Names![i];

                    resultsStack[resultsCount] = i;
                    resultLength += rawValueNamePairs.Names![i].Length;
                    resultsCount += 1;
                    value &= ~biggestUnprocessedValue;
                }
            }

            // There is a rest value but numbers cannot be mixed with names: returning a standalone number
            if (value != 0UL && !allowNumberWithNames)
                return ToNumericString(origRawValue);

            if (String.IsNullOrEmpty(separator))
                separator = EnumExtensions.DefaultFormatSeparator;

            int numericValueLen = 0;
            if (value != 0UL)
            {
                numericValueLen = GetStringLength(value);
                resultLength += numericValueLen;
                resultsCount += 1;
            }

            var sb = new StringBuilder(resultLength + separator!.Length * (resultsCount - 1));

            // Applying the number (if any)
            if (numericValueLen != 0)
            {
                sb.Append(ToNumericString(value));
                resultsCount -= 1;
                if (resultsCount > 1)
                    sb.Append(separator);
            }

            // Applying the names
            for (int i = resultsCount - 1; i >= 0; i--)
            {
                sb.Append(rawValueNamePairs.Names![resultsStack[i]]);

                if (i > 0)
                    sb.Append(separator);
            }

            return sb.ToString();
        }
#endif

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static bool TryFormatCompoundFlags(TEnum e, Span<char> destination, out int charsWritten, ReadOnlySpan<char> separator, bool allowNumberWithNames)
        {
            EnsureRawValueNamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return destination.TryWrite(Zero, out charsWritten);

            ulong[] rawValues = rawValueNamePairs.RawValues!;
            ulong value = origRawValue;

            // Unlike in TryFormatDistinctFlags it is used as a stack because the largest value is added first.
            Span<int> resultsStack = stackalloc int[underlyingInfo.BitSize];

            int resultsCount = 0; // Indicates the top of resultsStack
            int resultLength = 0; // Indicates the length of the string to be written

            // Processing existing values from largest to smallest
            for (int i = rawValues.Length - 1; value > 0 && i >= 0; i--)
            {
                ulong biggestUnprocessedValue = rawValues[i];
                if (biggestUnprocessedValue == 0UL)
                    break;

                if ((value & biggestUnprocessedValue) == biggestUnprocessedValue)
                {
                    // The value can be covered by a single name
                    if (origRawValue == biggestUnprocessedValue)
                        return destination.TryWrite(rawValueNamePairs.Names![i], out charsWritten);

                    resultsStack[resultsCount] = i;
                    resultLength += rawValueNamePairs.Names![i].Length;
                    resultsCount += 1;
                    value &= ~biggestUnprocessedValue;
                }
            }

            // There is a rest value but numbers cannot be mixed with names: returning a standalone number
            if (value != 0UL && !allowNumberWithNames)
                return TryFormatNumericString(origRawValue, destination, out charsWritten);

            if (separator.IsEmpty)
                separator = EnumExtensions.DefaultFormatSeparator;

            if (value != 0UL)
            {
                resultLength += GetStringLength(value);
                resultsCount += 1;
            }

            int totalLength = resultLength + separator.Length * (resultsCount - 1);
            if (destination.Length < totalLength)
            {
                charsWritten = 0;
                return false;
            }

            // Applying the number (if any)
            if (value != 0UL)
            {
                ToNumericString(value, ref destination);
                resultsCount -= 1;
                if (resultsCount > 1)
                    destination.Append(separator);
            }

            // Applying the names
            for (int i = resultsCount - 1; i >= 0; i--)
            {
                destination.Append(rawValueNamePairs.Names![resultsStack[i]]);

                if (i > 0)
                    destination.Append(separator);
            }

            charsWritten = totalLength;
            return true;
        }

        private static bool TryFormatCompoundFlags(TEnum e, Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<byte> separator, bool allowNumberWithNames)
        {
            EnsureRawValueUtf8NamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return utf8Destination.TryWrite(ZeroUtf8, out bytesWritten);

            ulong[] rawValues = rawValueNamePairs.RawValues!;
            ulong value = origRawValue;

            // Unlike in TryFormatDistinctFlags it is used as a stack because the largest value is added first.
            Span<int> resultsStack = stackalloc int[underlyingInfo.BitSize];

            int resultsCount = 0; // Indicates the top of resultsStack
            int resultLength = 0; // Indicates the length of the string to be written

            // Processing existing values from largest to smallest
            for (int i = rawValues.Length - 1; value > 0 && i >= 0; i--)
            {
                ulong biggestUnprocessedValue = rawValues[i];
                if (biggestUnprocessedValue == 0UL)
                    break;

                if ((value & biggestUnprocessedValue) == biggestUnprocessedValue && (allowNumberWithNames || utf8Names.IsValidName![i]))
                {
                    // The value can be covered by a single name
                    if (origRawValue == biggestUnprocessedValue)
                        return utf8Destination.TryWrite(utf8Names.Names![i], out bytesWritten);

                    resultsStack[resultsCount] = i;
                    resultLength += utf8Names.Names![i].Length;
                    resultsCount += 1;
                    value &= ~biggestUnprocessedValue;
                }
            }

            // There is a rest value but numbers cannot be mixed with names: returning a standalone number
            if (value != 0UL && !allowNumberWithNames)
                return TryFormatNumericString(origRawValue, utf8Destination, out bytesWritten);

            if (separator.IsEmpty)
                separator = EnumExtensions.DefaultFormatSeparatorUtf8;

            if (value != 0UL)
            {
                resultLength += GetStringLength(value);
                resultsCount += 1;
            }

            int totalLength = resultLength + separator.Length * (resultsCount - 1);
            if (utf8Destination.Length < totalLength)
            {
                bytesWritten = 0;
                return false;
            }

            // Applying the number (if any)
            if (value != 0UL)
            {
                ToNumericString(value, ref utf8Destination);
                resultsCount -= 1;
                if (resultsCount > 1)
                    utf8Destination.Append(separator);
            }

            // Applying the names
            for (int i = resultsCount - 1; i >= 0; i--)
            {
                utf8Destination.Append(utf8Names.Names![resultsStack[i]]);

                if (i > 0)
                    utf8Destination.Append(separator);
            }

            bytesWritten = totalLength;
            return true;
        }
#endif

        private static string ToNumericString(ulong value)
        {
            if (!underlyingInfo.IsSigned)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                if (EnvironmentHelper.IsPartiallyTrustedDomain)
                    return value.ToString(NumberFormatInfo.InvariantInfo);
#endif
                return value.QuickToString(false);
            }

            long signedValue = ToSigned(value);
#if NETFRAMEWORK || NETSTANDARD2_0
            if (EnvironmentHelper.IsPartiallyTrustedDomain)
                return signedValue.ToString(NumberFormatInfo.InvariantInfo);
#endif
            bool isNeg = signedValue < 0;
            return (isNeg ? (ulong)-signedValue : (ulong)signedValue).QuickToString(isNeg);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static byte[] ToNumericStringUtf8(ulong value)
        {
            // no need for some QuickToStringUtf8 because this is called only from one time initializations
            return Encoding.ASCII.GetBytes(underlyingInfo.IsSigned ? ToSigned(value).ToString(NumberFormatInfo.InvariantInfo) : value.ToString(NumberFormatInfo.InvariantInfo));
        }
#endif

        [SecurityCritical]
        private static void ToNumericString(ulong value, int numLen, ref MutableStringBuilder sb)
        {
            if (!underlyingInfo.IsSigned)
                sb.Append(value, false, numLen);
            else
            {
                long signedValue = ToSigned(value);
                bool isNeg = signedValue < 0;
                sb.Append((isNeg ? (ulong)-signedValue : (ulong)signedValue), isNeg, numLen);
            }
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static void ToNumericString(ulong value, ref Span<char> destination)
        {
            if (TryFormatNumericString(value, destination, out int charsWritten))
            {
                destination = destination.Slice(charsWritten);
                return;
            }

            Debug.Fail("Could not write value");
        }

        private static void ToNumericString(ulong value, ref Span<byte> utf8Destination)
        {
            if (TryFormatNumericString(value, utf8Destination, out int bytesWritten))
            {
                utf8Destination = utf8Destination.Slice(bytesWritten);
                return;
            }

            Debug.Fail("Could not write value");
        }

        private static bool TryFormatNumericString(ulong value, Span<char> destination, out int charsWritten) => underlyingInfo.IsSigned
            ? ToSigned(value).TryFormat(destination, out charsWritten, provider: NumberFormatInfo.InvariantInfo)
            : value.TryFormat(destination, out charsWritten, provider: NumberFormatInfo.InvariantInfo);

        private static bool TryFormatNumericString(ulong value, Span<byte> utf8Destination, out int bytesWritten)
        {
#if NET8_0_OR_GREATER
            return underlyingInfo.IsSigned
                ? ToSigned(value).TryFormat(utf8Destination, out bytesWritten, provider: NumberFormatInfo.InvariantInfo)
                : value.TryFormat(utf8Destination, out bytesWritten, provider: NumberFormatInfo.InvariantInfo);
#else
            if (!underlyingInfo.IsSigned)
                return utf8Destination.TryWrite(value, false, out bytesWritten);

            long signedValue = ToSigned(value);
            bool isNeg = signedValue < 0;
            return utf8Destination.TryWrite(isNeg ? (ulong)-signedValue : (ulong)signedValue, isNeg, out bytesWritten);
#endif
        }
#endif

        private static int GetStringLength(ulong value)
        {
            if (!underlyingInfo.IsSigned)
                return value.DecimalDigitsCount();
            long signed = ToSigned(value);
            int sign;
            if (signed < 0)
            {
                signed = -signed;
                sign = 1;
            }
            else
                sign = 0;

            return ((ulong)signed).DecimalDigitsCount() + sign;
        }

        #endregion

        #endregion
    }
}