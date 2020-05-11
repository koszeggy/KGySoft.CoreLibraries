#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Enum.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices; 
using System.Security; 

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Generic helper class for the <see cref="Enum"/> class.
    /// Provides high performance solutions for already existing functionality in the <see cref="Enum"/> class along with
    /// some additional features.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration. Must be an <see cref="Enum"/> type.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "It is not a suffix but the name of the type")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Enum", Justification = "Naming it Enum is intended")]
    public static class Enum<TEnum> where TEnum : struct, Enum
    {
        #region Fields

        // ReSharper disable StaticMemberInGenericType - values are specific for TEnum
        private static readonly bool isFlags = (typeof(TEnum)).IsFlagsEnum();

        // For the best performance, locks are used only on initialization. This may lead to concurrent initializations
        // but that is alright. Once a field is set no more locks will be requested for it again.
        private static readonly object syncRoot = new object();

        // These fields share the same data per underlying type
        private static readonly EnumComparer<TEnum> converter = EnumComparer<TEnum>.Comparer; // The comparer contains also some internal converter methods.
        private static readonly EnumUnderlyingInfo underlyingInfo = EnumUnderlyingInfo.GetUnderlyingInfo(Enum.GetUnderlyingType(typeof(TEnum)));

        // These members van vary per TEnum and are initialized only on demand
        private static TEnum[] values;
        private static string[] names;
        private static Dictionary<TEnum, string> valueNamePairs;
        private static Dictionary<string, TEnum> nameValuePairs;
        private static (ulong[] RawValues, string[] Names) rawValueNamePairs;
        private static Dictionary<StringSegment, ulong> nameRawValuePairs;
        private static Dictionary<StringSegment, ulong> nameRawValuePairsIgnoreCase;
        private static ulong? flagsMask;
        // ReSharper restore StaticMemberInGenericType

        #endregion

        #region Properties

        private static string[] Names
        {
            get
            {
                string[] result = names;

                if (result != null)
                    return result;

                lock (syncRoot)
                    return names = Enum.GetNames(typeof(TEnum));
            }
        }

        private static TEnum[] Values
        {
            get
            {
                TEnum[] result = values;

                if (result != null)
                    return result;

                lock (syncRoot)
                    return values = (TEnum[])Enum.GetValues(typeof(TEnum));
            }
        }

        private static Dictionary<TEnum, string> ValueNamePairs
        {
            get
            {
                Dictionary<TEnum, string> result = valueNamePairs;

                if (result != null)
                    return result;

                lock (syncRoot)
                {
                    result = new Dictionary<TEnum, string>(Names.Length, ComparerHelper<TEnum>.EqualityComparer);
                    for (int i = 0; i < Values.Length; i++)
                    {
                        // avoiding duplicated keys (multiple names for the same value)
                        if (!result.ContainsKey(values[i]))
                            result.Add(values[i], names[i]);
                    }

                    return valueNamePairs = result;
                }
            }
        }

        private static Dictionary<string, TEnum> NameValuePairs
        {
            get
            {
                Dictionary<string, TEnum> result = nameValuePairs;

                if (result != null)
                    return result;

                lock (syncRoot)
                {
                    result = new Dictionary<string, TEnum>(Names.Length);
                    for (int i = 0; i < Values.Length; i++)
                        result.Add(names[i], values[i]);
                    return nameValuePairs = result;
                }
            }
        }

        private static Dictionary<StringSegment, ulong> NameRawValuePairs
        {
            get
            {
                Dictionary<StringSegment, ulong> result = nameRawValuePairs;
                if (result != null)
                    return result;

                lock (syncRoot)
                {
                    result = new Dictionary<StringSegment, ulong>(Names.Length);
                    for (int i = 0; i < Values.Length; i++)
                        result.Add(new StringSegment(names[i]), converter.ToUInt64(values[i]));

                    return nameRawValuePairs = result;
                }
            }
        }

        private static Dictionary<StringSegment, ulong> NameRawValuePairsIgnoreCase
        {
            get
            {
                Dictionary<StringSegment, ulong> result = nameRawValuePairsIgnoreCase;
                if (result != null)
                    return result;

                result = new Dictionary<StringSegment, ulong>(Names.Length, StringSegment.IgnoreCaseComparer);
                Dictionary<StringSegment, ulong> refDict = NameRawValuePairs;
                foreach (KeyValuePair<StringSegment, ulong> pair in refDict)
                    result[pair.Key] = pair.Value;

                lock (syncRoot)
                    return nameRawValuePairsIgnoreCase = result;
            }
        }

        private static string Zero =>
            rawValueNamePairs.RawValues.Length > 0 && rawValueNamePairs.RawValues[0] == 0UL
                ? rawValueNamePairs.Names[0]
                : "0";

        private static ulong FlagsMask
            => flagsMask ??= Values.Select(converter.ToUInt64).Where(UInt64Extensions.IsSingleFlag).Aggregate(0UL, (acc, value) => acc | value);

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Retrieves the array of the values of the constants in enumeration <typeparamref name="TEnum"/>.
        /// </summary>
        /// <returns>An array of the values of the constants in <typeparamref name="TEnum"/>.
        /// The elements of the array are sorted by the binary values of the enumeration constants.</returns>
        public static TEnum[] GetValues()
        {
            TEnum[] result = new TEnum[Values.Length];
            Array.Copy(values, result, values.Length);
            return result;
        }

        /// <summary>
        /// Retrieves the array of the values of the constants in enumeration <typeparamref name="TEnum"/>.
        /// </summary>
        /// <returns>An array of the values of the constants in <typeparamref name="TEnum"/>.
        /// The elements of the array are in the same order as in case of the result of the <see cref="GetValues">GetValues</see> method.</returns>
        public static string[] GetNames()
        {
            string[] result = new string[Names.Length];
            Array.Copy(names, result, names.Length);
            return result;
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The <see langword="enum"/>&#160;value whose name is required.</param>
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/>&#160;if no such constant is found.</returns>
        public static string GetName(TEnum value)
        {
            ValueNamePairs.TryGetValue(value, out string result);
            return result;
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value of the required field.</param>
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/>&#160;if no such constant is found.</returns>
        public static string GetName(long value)
        {
            if (value < underlyingInfo.MinValue
                || underlyingInfo.IsSigned && value > (long)underlyingInfo.MaxValue
                || !underlyingInfo.IsSigned && (ulong)value > underlyingInfo.MaxValue)
                return null;

            return TryGetNameByValue((ulong)value & underlyingInfo.SizeMask);
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value of the required field.</param>
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/>&#160;if no such constant is found.</returns>
        [CLSCompliant(false)]
        public static string GetName(ulong value) => value > underlyingInfo.MaxValue ? null : TryGetNameByValue(value);

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value.</param>
        /// <returns><see langword="true"/>&#160;if <typeparamref name="TEnum"/> has a defined field that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsDefined(TEnum value) => ValueNamePairs.ContainsKey(value);

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">A <see cref="string"/> value representing a field name in the enumeration.</param>
        /// <returns><see langword="true"/>&#160;if <typeparamref name="TEnum"/> has a defined field whose name equals <paramref name="value"/> (search is case-sensitive); otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsDefined(string value)
        {
            if (value == null)
                Throw.ArgumentNullException(Argument.value);
            return NameValuePairs.ContainsKey(value);
        }

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/> as a field value.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <returns><see langword="true"/>&#160;if <typeparamref name="TEnum"/> has a field whose value that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsDefined(long value)
        {
            if (value < underlyingInfo.MinValue
                || underlyingInfo.IsSigned && value > (long)underlyingInfo.MaxValue
                || !underlyingInfo.IsSigned && (ulong)value > underlyingInfo.MaxValue)
                return false;
            EnsureRawValueNamePairs();
            return FindIndex((ulong)value & underlyingInfo.SizeMask) >= 0;
        }

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/> as a field value.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <returns><see langword="true"/>&#160;if <typeparamref name="TEnum"/> has a field whose value that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        [CLSCompliant(false)]
        public static bool IsDefined(ulong value)
        {
            if (value > underlyingInfo.MaxValue)
                return false;
            EnsureRawValueNamePairs();
            return FindIndex(value) >= 0;
        }

        /// <summary>
        /// Gets whether the bits that are set in the <paramref name="flags"/> parameter are set in the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An enumeration value of <typeparamref name="TEnum"/> type.</param>
        /// <param name="flags">A flags <see langword="enum"/>&#160;value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/> and whether all bits that are set are defined in the <typeparamref name="TEnum"/> type.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is zero, or when the bits that are set in <paramref name="flags"/> are set in <paramref name="value"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool HasFlag(TEnum value, TEnum flags) => HasFlagCore(value, converter.ToUInt64(flags));

        /// <summary>
        /// Gets whether the bits that are set in the <paramref name="flags"/> parameter are set in the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An enumeration value of <typeparamref name="TEnum"/> type.</param>
        /// <param name="flags">An integer value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/> and whether all bits that are set are defined in the <typeparamref name="TEnum"/> type.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is zero, or when the bits that are set in <paramref name="flags"/> are set in <paramref name="value"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool HasFlag(TEnum value, long flags)
        {
            if (flags < underlyingInfo.MinValue
                || underlyingInfo.IsSigned && flags > (long)underlyingInfo.MaxValue
                || !underlyingInfo.IsSigned && (ulong)flags > underlyingInfo.MaxValue)
                return false;
            return HasFlagCore(value, (ulong)flags & underlyingInfo.SizeMask);
        }

        /// <summary>
        /// Gets whether the bits that are set in the <paramref name="flags"/> parameter are set in the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An enumeration value of <typeparamref name="TEnum"/> type.</param>
        /// <param name="flags">An unsigned integer value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/> and whether all bits that are set are defined in the <typeparamref name="TEnum"/> type.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is zero, or when the bits that are set in <paramref name="flags"/> are set in <paramref name="value"/>;
        /// otherwise, <see langword="false"/>.</returns>
        [CLSCompliant(false)]
        public static bool HasFlag(TEnum value, ulong flags) => flags <= underlyingInfo.MaxValue && HasFlagCore(value, flags);

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>. It is not checked, whether this flag is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if only a single bit is set in <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsSingleFlag(TEnum value) => converter.ToUInt64(value).IsSingleFlag();

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>. It is not checked, whether this flag is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> falls into the range of <typeparamref name="TEnum"/> range
        /// and only a single bit is set in <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsSingleFlag(long value)
        {
            if (value == 0L
                || value < underlyingInfo.MinValue
                || underlyingInfo.IsSigned && value > (long)underlyingInfo.MaxValue
                || !underlyingInfo.IsSigned && (ulong)value > underlyingInfo.MaxValue)
                return false;
            return ((ulong)value).IsSingleFlag();
        }

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>. It is not checked, whether this flag is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> falls into the range of <typeparamref name="TEnum"/> range
        /// and only a single bit is set in <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        [CLSCompliant(false)]
        public static bool IsSingleFlag(ulong value)
        {
            if (value == 0UL || value > underlyingInfo.MaxValue)
                return false;
            return value.IsSingleFlag();
        }

        /// <summary>
        /// Gets the number of bits set in <paramref name="value"/>.
        /// It is not checked, whether all flags are defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>The number of bits set in <paramref name="value"/>.</returns>
        public static int GetFlagsCount(TEnum value) => converter.ToUInt64(value).GetFlagsCount();

        /// <summary>
        /// Gets the number of bits set in <paramref name="value"/> or <c>-1</c> if <paramref name="value"/> does not fall into the range of <typeparamref name="TEnum"/>.
        /// It is not checked, whether all flags are defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>The number of bits set in <paramref name="value"/>, or <c>-1</c> if <paramref name="value"/> does not fall into the range of <typeparamref name="TEnum"/>.</returns>
        public static int GetFlagsCount(long value)
        {
            if (value == 0L)
                return 0;
            if (value < underlyingInfo.MinValue
                || underlyingInfo.IsSigned && value > (long)underlyingInfo.MaxValue
                || !underlyingInfo.IsSigned && (ulong)value > underlyingInfo.MaxValue)
                return -1;

            return ((ulong)value & underlyingInfo.SizeMask).GetFlagsCount();
        }

        /// <summary>
        /// Gets the number of bits set in <paramref name="value"/> or <c>-1</c> if <paramref name="value"/> does not fall into the range of <typeparamref name="TEnum"/>.
        /// It is not checked, whether all flags are defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>The number of bits set in <paramref name="value"/>, or <c>-1</c> if <paramref name="value"/> does not fall into the range of <typeparamref name="TEnum"/>.</returns>
        [CLSCompliant(false)]
        public static int GetFlagsCount(ulong value)
        {
            if (value == 0UL)
                return 0;
            if (value > underlyingInfo.MaxValue)
                return -1;

            return value.GetFlagsCount();
        }

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the <typeparamref name="TEnum"/> type,
        /// or, when <paramref name="flags"/> is zero, it is checked whether zero is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="flags">A flags <see langword="enum"/>&#160;value, whose bits should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        public static bool AllFlagsDefined(TEnum flags) => AllFlagsDefinedCore(converter.ToUInt64(flags));

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the <typeparamref name="TEnum"/> type,
        /// or, when <paramref name="flags"/> is zero, it is checked whether zero is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="flags">An integer value, whose bits should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        public static bool AllFlagsDefined(long flags)
        {
            if (flags < underlyingInfo.MinValue
                || underlyingInfo.IsSigned && flags > (long)underlyingInfo.MaxValue
                || !underlyingInfo.IsSigned && (ulong)flags > underlyingInfo.MaxValue)
                return false;

            return AllFlagsDefinedCore((ulong)flags & underlyingInfo.SizeMask);
        }

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the <typeparamref name="TEnum"/> type,
        /// or, when <paramref name="flags"/> is zero, it is checked whether zero is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="flags">An unsigned integer value, whose bits should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        [CLSCompliant(false)]
        public static bool AllFlagsDefined(ulong flags) => flags <= underlyingInfo.MaxValue && AllFlagsDefinedCore(flags);

        /// <summary>
        /// Gets a <typeparamref name="TEnum"/> value where all defined single flag values are set. 
        /// </summary>
        /// <returns>A <typeparamref name="TEnum"/> value where all defined single flag values are set. </returns>
        /// <remarks>
        /// <para>Flag values are the ones whose binary representation contains only a single bit.</para>
        /// <para>It is not checked whether <typeparamref name="TEnum"/> is really marked by <see cref="FlagsAttribute"/>.</para>
        /// </remarks>
        public static TEnum GetFlagsMask() => converter.ToEnum(FlagsMask);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The <see cref="string"/> representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values the separator among the values. If <see langword="null"/>&#160;or is empty, then comma (<c>,</c>) separator is used.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <param name="result"><see langword="null"/>&#160;if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if <see cref="string"/> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool TryParse(string value, string separator, bool ignoreCase, out TEnum result)
        {
            if (value == null)
                Throw.ArgumentNullException(Argument.value);

            // simple name match test
            if (NameValuePairs.TryGetValue(value, out result))
                return true;

            var s = new StringSegment(value);
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
            while (s.TryGetNextSegment(separator, out StringSegment token))
            {
                token.Trim();
                if (token.Length == 0)
                    return false;

                // literal token found in dictionary
                if (NameRawValuePairs.TryGetValue(token, out ulong tokens))
                {
                    acc |= tokens;
                    continue;
                }

                // checking for case-insensitive match
                if (ignoreCase && NameRawValuePairsIgnoreCase.TryGetValue(token, out tokens))
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
        /// <param name="value">The <see cref="string"/> representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <param name="result"><see langword="null"/>&#160;if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if <see cref="string"/> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be <see langword="null"/>.</exception>
        public static bool TryParse(string value, bool ignoreCase, out TEnum result) => TryParse(value, EnumExtensions.DefaultParseSeparator, ignoreCase, out result);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The <see cref="string"/> representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values the separator among the values. If <see langword="null"/>&#160;or is empty, then comma (<c>,</c>) separator is used.</param>
        /// <param name="result"><see langword="null"/>&#160;if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if <see cref="string"/> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="value"/> is not a simple field or numeric value</exception>
        public static bool TryParse(string value, string separator, out TEnum result) => TryParse(value, separator, false, out result);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// In case of success the return value is <see langword="true"/>&#160;and parsed <see langword="enum"/>&#160;is returned in <paramref name="result"/> parameter.
        /// </summary>
        /// <param name="value">The <see cref="string"/> representation of the enumerated value or values to parse.</param>
        /// <param name="result"><see langword="null"/>&#160;if return value is <see langword="false"/>; otherwise, the parsed <see langword="enum"/>&#160;value.</param>
        /// <returns><see langword="false"/>&#160;if <see cref="string"/> in <paramref name="value"/> parameter cannot be parsed as <typeparamref name="TEnum"/>; otherwise, <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be <see langword="null"/>.</exception>
        public static bool TryParse(string value, out TEnum result) => TryParse(value, EnumExtensions.DefaultParseSeparator, false, out result);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The <see cref="string"/> representation of the enumerated value or values to parse.</param>
        /// <param name="separator">In case of more values specified the separator among the values. If <see langword="null"/>&#160;or is empty, then comma (<c>,</c>) separator is used. This parameter is optional.
        /// <br/>Default value: <c>,</c></param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> and <paramref name="separator"/> cannot be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(string value, string separator = EnumExtensions.DefaultParseSeparator, bool ignoreCase = false)
        {
            if (!TryParse(value, separator, ignoreCase, out TEnum result))
                Throw.ArgumentException(Argument.value, Res.EnumValueCannotBeParsedAsEnum(value, typeof(TEnum)));
            return result;
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The <see cref="string"/> representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(string value, bool ignoreCase)
        {
            if (!TryParse(value, EnumExtensions.DefaultParseSeparator, ignoreCase, out TEnum result))
                Throw.ArgumentException(Argument.value, Res.EnumValueCannotBeParsedAsEnum(value, typeof(TEnum)));
            return result;
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given <see langword="enum"/>&#160;value specified in the <paramref name="value"/> parameter.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="format">Formatting options.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/>&#160;or is empty, then comma-space (<c>, </c>) separator is used. This parameter is optional.
        /// <br/>Default value: <c>, </c>.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="format"/>.</exception>
        public static string ToString(TEnum value, EnumFormattingOptions format, string separator = EnumExtensions.DefaultFormatSeparator)
        {
            if ((uint)format > (uint)EnumFormattingOptions.CompoundFlagsAndNumber)
                Throw.EnumArgumentOutOfRange(Argument.format, value);

            if (format == EnumFormattingOptions.DistinctFlags)
                return FormatDistinctFlags(value, separator);

            // returning as flags
            if ((format == EnumFormattingOptions.Auto && isFlags) || format == EnumFormattingOptions.CompoundFlagsOrNumber || format == EnumFormattingOptions.CompoundFlagsAndNumber)
                return FormatCompoundFlags(value, separator, format == EnumFormattingOptions.CompoundFlagsAndNumber);
            
            // defined value exists
            if (ValueNamePairs.TryGetValue(value, out string name))
                return name;

            // if single value is requested returning a number
            return ToNumericString(converter.ToUInt64(value));
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given <see langword="enum"/>&#160;value specified in the <paramref name="value"/> parameter.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        public static string ToString(TEnum value)
        {
            // returning as flags
            if (isFlags)
                return FormatCompoundFlags(value, EnumExtensions.DefaultFormatSeparator, false);
            
            // defined value exists
            if (ValueNamePairs.TryGetValue(value, out string name))
                return name;

            // defined value does not exist: returning a number
            return ToNumericString(converter.ToUInt64(value));
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given <see langword="enum"/>&#160;value specified in the <paramref name="value"/> parameter.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/>&#160;or is empty, then comma-space (<c>, </c>) separator is used.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        public static string ToString(TEnum value, string separator) => ToString(value, EnumFormattingOptions.Auto, separator);

        /// <summary>
        /// Gets the defined flags in <typeparamref name="TEnum"/>, where each flags are returned as distinct values.
        /// </summary>
        /// <returns>A lazy-enumerated <see cref="IEnumerable{TEnum}"/> instance containing each flags of <typeparamref name="TEnum"/> as distinct values.</returns>
        /// <remarks>
        /// <para>Flag values are the ones whose binary representation contains only a single bit.</para>
        /// <para>It is not checked whether <typeparamref name="TEnum"/> is really marked by <see cref="FlagsAttribute"/>.</para>
        /// <para>Flags with the same values but different names are returned only once.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public static IEnumerable<TEnum> GetFlags()
        {
            EnsureRawValueNamePairs();
            return rawValueNamePairs.RawValues.Where(UInt64Extensions.IsSingleFlag).Select(converter.ToEnum);
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{TEnum}"/> enumeration of <paramref name="flags"/>,
        /// where each flags are returned as distinct values.
        /// </summary>
        /// <param name="flags">A flags <see langword="enum"/>&#160;value, whose flags should be returned. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <param name="onlyDefinedValues">When <see langword="true"/>, returns only flags, which are defined in <typeparamref name="TEnum"/>.
        /// When <see langword="false"/>, returns also undefined flags in <paramref name="flags"/>.</param>
        /// <returns>A lazy-enumerated <see cref="IEnumerable{TEnum}"/> instance containing each flags of <paramref name="flags"/> as distinct values.</returns>
        /// <remarks>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public static IEnumerable<TEnum> GetFlags(TEnum flags, bool onlyDefinedValues)
        {
            ulong value = converter.ToUInt64(flags);
            if (value == 0UL)
                yield break;

            for (int i = 0; i < underlyingInfo.BitSize; i++)
            {
                ulong flag = 1UL << i;
                if ((value & flag) != 0)
                {
                    if (!onlyDefinedValues || (FlagsMask & flag) == flag)
                        yield return converter.ToEnum(flag);

                    value &= ~flag;
                    if (value == 0UL)
                        break;
                }
            }
        }

        /// <summary>
        /// Clears caches associated with <typeparamref name="TEnum"/> enumeration.
        /// </summary>
        public static void ClearCaches()
        {
            lock (syncRoot)
            {
                values = null;
                names = null;
                valueNamePairs = null;
                nameValuePairs = null;
                nameRawValuePairs = null;
                rawValueNamePairs.RawValues = null;
                rawValueNamePairs.Names = null;
            }
        }

        #endregion

        #region Private Methods

        private static void EnsureRawValueNamePairs()
        {
            if (rawValueNamePairs.RawValues == null)
                InitRawValueNamePairs();
        }

        private static void InitRawValueNamePairs()
        {
            var result = new SortedList<ulong, string>(Names.Length);
            int length = Values.Length;
            for (int i = 0; i < length; i++)
            {
                ulong value = converter.ToUInt64(values[i]);

                // avoiding duplicated keys (multiple names for the same value)
                if (!result.ContainsKey(value))
                    result.Add(value, names[i]);
            }

            rawValueNamePairs.RawValues = result.Keys.ToArray();
            rawValueNamePairs.Names = result.Values.ToArray();
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static int FindIndex(ulong value) => Array.BinarySearch(rawValueNamePairs.RawValues, 0, rawValueNamePairs.RawValues.Length, value);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static string TryGetNameByValue(ulong value)
        {
            EnsureRawValueNamePairs();
            int index = FindIndex(value);
            return index >= 0 ? rawValueNamePairs.Names[index] : null;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static bool AllFlagsDefinedCore(ulong flags)
        {
            if (flags == 0UL)
            {
                EnsureRawValueNamePairs();
                return FindIndex(0UL) == 0;
            }

            return (FlagsMask & flags) == flags;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static bool HasFlagCore(TEnum value, ulong flags) => flags == 0UL || (converter.ToUInt64(value) & flags) == flags;

        [SecuritySafeCritical]
        private static unsafe string FormatDistinctFlags(TEnum e, string separator)
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
                        return rawValueNamePairs.Names[nameIndex];

                    resultsQueue[i] = nameIndex;
                    resultLength += rawValueNamePairs.Names[nameIndex].Length;
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
                        sb.Append(rawValueNamePairs.Names[resultsQueue[i]]);
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

        [SecuritySafeCritical]
        private static unsafe string FormatCompoundFlags(TEnum e, string separator, bool allowNumberWithNames)
        {
            EnsureRawValueNamePairs();
            ulong origRawValue = converter.ToUInt64(e);
            if (origRawValue == 0UL)
                return Zero;

            // Finally, thanks to the changes in .NET Core 3.0 (see https://github.com/dotnet/coreclr/pull/21254/files)
            // the System.Enum.ToString performance is not terrible anymore. This is also a similar solution (apart
            // from the feature differences). We can't use Span here because that is not available for all targets.
            ulong[] rawValues = rawValueNamePairs.RawValues;
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
                        return rawValueNamePairs.Names[i];

                    resultsStack[resultsCount] = i;
                    resultLength += rawValueNamePairs.Names[i].Length;
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
                    sb.Append(rawValueNamePairs.Names[resultsStack[i]]);

                    if (i > 0)
                        sb.Append(separator);
                }
            }

            return result;
        }

        private static long ToSigned(ulong value)
            => underlyingInfo.TypeCode switch
            {
                TypeCode.Int32 => (int)value,
                TypeCode.Int64 => (long)value,
                TypeCode.Int16 => (short)value,
                _ => (sbyte)value
            };

        private static string ToNumericString(ulong value)
        {
            if (!underlyingInfo.IsSigned)
                return value.QuickToString(false);

            long signedValue = ToSigned(value);
            bool isNeg = signedValue < 0;
            return (isNeg ? (ulong)-signedValue : (ulong)signedValue).QuickToString(isNeg);
        }

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

        private static int GetStringLength(ulong value)
        {
            if (!underlyingInfo.IsSigned)
                return (int)Math.Log10(value) + 1;
            long signed = ToSigned(value);
            int sign;
            if (signed < 0)
            {
                signed = -signed;
                sign = 1;
            }
            else
                sign = 0;

            return (int)Math.Log10(signed) + 1 + sign;
        }

        #endregion

        #endregion
    }
}
