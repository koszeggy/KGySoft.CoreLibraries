﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Enum.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Text;
#endif

using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Generic helper class for the <see cref="Enum"/> class. Provides high performance solutions
    /// for already existing functionality in the <see cref="Enum"/> class along with some additional features.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration. Must be an <see cref="Enum"/> type.</typeparam>
    /// <remarks>
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/xNTnLE" target="_blank">online</a>.</note>
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "It is not a suffix but the name of the type")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Enum", Justification = "Naming it Enum is intended")]
    [SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "All fields in this class depend on TEnum")]
    public static partial class Enum<TEnum> where TEnum : struct, Enum
    {
        #region Fields

        // For the best performance, locks are used only on initialization. This may lead to concurrent initializations
        // but that is alright. Once a field is set no more locks will be requested for it again.
        // Note: it is important that this is the first field
        private static readonly object syncRoot = new object();

        private static readonly bool isFlags = typeof(TEnum).IsFlagsEnum();

        // These fields share the same data per underlying type
        private static readonly EnumComparer<TEnum> converter = EnumComparer<TEnum>.Comparer; // The comparer contains also some internal converter methods.

#if NETFRAMEWORK || NETSTANDARD2_0
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Must not be readonly because it may cause a VerificationException from a partially trusted domain")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local", Justification = "Must not be readonly because it may cause a VerificationException from a partially trusted domain")]
        private static RangeInfo underlyingInfo = RangeInfo.GetRangeInfo(Enum.GetUnderlyingType(typeof(TEnum)));
#else
        private static readonly RangeInfo underlyingInfo = RangeInfo.GetRangeInfo(Enum.GetUnderlyingType(typeof(TEnum)));
#endif

        // These members can vary per TEnum and are initialized only on demand
        private static TEnum[]? values;
        private static string[]? names;
        private static Array? underlyingValues;
        private static Dictionary<TEnum, string>? valueNamePairs;
        private static StringKeyedDictionary<TEnum>? nameValuePairs;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static Dictionary<TEnum, byte[]>? valueUtf8NamePairs;
#endif
        private static (ulong[]? RawValues, string[]? Names) rawValueNamePairs;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static (byte[][]? Names, bool[]? IsValidName) utf8Names; // acts as additional items in rawValueNamePairs tuple, requires EnsureRawValueNamePairs first
#endif
        private static StringKeyedDictionary<ulong>? nameRawValuePairs;
        private static StringKeyedDictionary<ulong>? nameRawValuePairsIgnoreCase;
        private static ulong? flagsMask;

        #endregion

        #region Properties

        private static string[] Names => names ?? InitNames();
        private static TEnum[] Values => values ?? InitValues();
        private static Array UnderlyingValues => underlyingValues ?? InitUnderlyingValues();
        private static Dictionary<TEnum, string> ValueNamePairs => valueNamePairs ?? InitValueNamePairs();
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static Dictionary<TEnum, byte[]> ValueUtf8NamePairs => valueUtf8NamePairs ?? InitValueUtf8NamePairs(); 
#endif
        private static StringKeyedDictionary<TEnum> NameValuePairs => nameValuePairs ?? InitNameValuePairs();
        private static StringKeyedDictionary<ulong> NameRawValuePairs => nameRawValuePairs ?? InitNameRawValuePairs();
        private static StringKeyedDictionary<ulong> NameRawValuePairsIgnoreCase => nameRawValuePairsIgnoreCase ?? InitNameRawValuePairsIgnoreCase();

        private static string Zero
        {
            get
            {
                Debug.Assert(rawValueNamePairs.RawValues != null, $"{nameof(EnsureRawValueNamePairs)} was not called");
                return rawValueNamePairs.RawValues!.Length > 0 && rawValueNamePairs.RawValues![0] == 0UL
                    ? rawValueNamePairs.Names![0]
                    : "0";
            }
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static ReadOnlySpan<byte> ZeroUtf8
        {
            get
            {
                Debug.Assert(utf8Names.Names != null, $"{nameof(EnsureRawValueUtf8NamePairs)} was not called");
                return rawValueNamePairs.RawValues!.Length > 0 && rawValueNamePairs.RawValues[0] == 0UL
                    ? utf8Names.Names![0]
                    : "0"u8;
            }
        }
#endif

        private static ulong FlagsMask
            => flagsMask ??= Values.Select(converter.ToUInt64).Where(UInt64Extensions.IsSingleFlag).Aggregate(0UL, (acc, value) => acc | value);

        #endregion

        #region Methods

        #region Public Methods

        #region Values and Name(s)

        /// <summary>
        /// Retrieves the array of the values of the constants in enumeration <typeparamref name="TEnum"/>.
        /// </summary>
        /// <returns>An array of the values of the constants in <typeparamref name="TEnum"/>.
        /// The elements of the array are sorted by the binary values of the enumeration constants.</returns>
        public static TEnum[] GetValues()
        {
            TEnum[] result = new TEnum[Values.Length];
            Array.Copy(values!, result, values!.Length);
            return result;
        }

        /// <summary>
        /// Retrieves the array of the values of the constants in enumeration <typeparamref name="TEnum"/>
        /// where the element type of the result array is the underlying type of <typeparamref name="TEnum"/>.
        /// </summary>
        /// <returns>An array of the values of the constants in <typeparamref name="TEnum"/>
        /// where the element type of the result array is the underlying type of <typeparamref name="TEnum"/>.
        /// The elements of the array are sorted by the binary values of the enumeration constants.</returns>
        public static Array GetUnderlyingValues() => (Array)UnderlyingValues.Clone();

        /// <summary>
        /// Retrieves the array of the values of the constants in enumeration <typeparamref name="TEnum"/>.
        /// </summary>
        /// <returns>An array of the values of the constants in <typeparamref name="TEnum"/>.
        /// The elements of the array are in the same order as in case of the result of the <see cref="GetValues">GetValues</see> method.</returns>
        public static string[] GetNames()
        {
            string[] result = new string[Names.Length];
            Array.Copy(names!, result, names!.Length);
            return result;
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The <see langword="enum"/> value whose name is required.</param>
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/> if no such constant is found.</returns>
        public static string? GetName(TEnum value)
        {
            ValueNamePairs.TryGetValue(value, out string? result);
            return result;
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value of the required field.</param>
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/> if no such constant is found.</returns>
        public static string? GetName(long value)
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
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/> if no such constant is found.</returns>
        [CLSCompliant(false)]
        public static string? GetName(ulong value) => value > underlyingInfo.MaxValue ? null : TryGetNameByValue(value);

        #endregion

        #region IsDefined-like Methods

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value.</param>
        /// <returns><see langword="true"/> if <typeparamref name="TEnum"/> has a defined field that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsDefined(TEnum value) => ValueNamePairs.ContainsKey(value);

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">A <see cref="string">string</see> value representing a field name in the enumeration.</param>
        /// <returns><see langword="true"/> if <typeparamref name="TEnum"/> has a defined field whose name equals <paramref name="value"/> (search is case-sensitive); otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsDefined(string value)
        {
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);
            return NameValuePairs.ContainsKey(value);
        }

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">A <see cref="StringSegment"/> value representing a field name in the enumeration.</param>
        /// <returns><see langword="true"/> if <typeparamref name="TEnum"/> has a defined field whose name equals <paramref name="value"/> (search is case-sensitive); otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        public static bool IsDefined(StringSegment value)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            return NameValuePairs.ContainsKey(value);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value representing a field name in the enumeration.</param>
        /// <returns><see langword="true"/> if <typeparamref name="TEnum"/> has a defined field whose name equals <paramref name="value"/> (search is case-sensitive); otherwise, <see langword="false"/>.</returns>
        public static bool IsDefined(ReadOnlySpan<char> value) => NameValuePairs.ContainsKey(value);
#endif

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/> as a field value.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <returns><see langword="true"/> if <typeparamref name="TEnum"/> has a field whose value that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/> if <typeparamref name="TEnum"/> has a field whose value that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        [CLSCompliant(false)]
        public static bool IsDefined(ulong value)
        {
            if (value > underlyingInfo.MaxValue)
                return false;
            EnsureRawValueNamePairs();
            return FindIndex(value) >= 0;
        }

        /// <summary>
        /// Returns <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <paramref name="defaultValue"/>, even if it is undefined.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value.</param>
        /// <param name="defaultValue">A <typeparamref name="TEnum"/> value to return if <paramref name="value"/>
        /// is not defined in <typeparamref name="TEnum"/>. It does not needed to be a defined value. This parameter is optional.
        /// <br/>Default value: The bitwise zero value of <typeparamref name="TEnum"/>.</param>
        /// <returns><paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <paramref name="defaultValue"/>, even if it is undefined.</returns>
        public static TEnum GetDefinedOrDefault(TEnum value, TEnum defaultValue = default) => IsDefined(value) ? value : defaultValue;

        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <paramref name="defaultValue"/>, even if it is undefined. The search is case-sensitive.
        /// </summary>
        /// <param name="value">A <see cref="string">string</see> value representing a field name in the enumeration.</param>
        /// <param name="defaultValue">A <typeparamref name="TEnum"/> value to return if <paramref name="value"/>
        /// is not defined in <typeparamref name="TEnum"/>. It does not needed to be a defined value. This parameter is optional.
        /// <br/>Default value: The bitwise zero value of <typeparamref name="TEnum"/>.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <paramref name="defaultValue"/>, even if it is undefined.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static TEnum GetDefinedOrDefault(string value, TEnum defaultValue = default)
        {
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);
            return NameValuePairs.GetValueOrDefault(value, defaultValue);
        }

        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <paramref name="defaultValue"/>, even if it is undefined. The search is case-sensitive.
        /// </summary>
        /// <param name="value">A <see cref="StringSegment"/> value representing a field name in the enumeration.</param>
        /// <param name="defaultValue">A <typeparamref name="TEnum"/> value to return if <paramref name="value"/>
        /// is not defined in <typeparamref name="TEnum"/>. It does not needed to be a defined value. This parameter is optional.
        /// <br/>Default value: The bitwise zero value of <typeparamref name="TEnum"/>.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <paramref name="defaultValue"/>, even if it is undefined.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        public static TEnum GetDefinedOrDefault(StringSegment value, TEnum defaultValue = default)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            return NameValuePairs.GetValueOrDefault(value, defaultValue);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <paramref name="defaultValue"/>, even if it is undefined. The search is case-sensitive.
        /// </summary>
        /// <param name="value">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value representing a field name in the enumeration.</param>
        /// <param name="defaultValue">A <typeparamref name="TEnum"/> value to return if <paramref name="value"/>
        /// is not defined in <typeparamref name="TEnum"/>. It does not needed to be a defined value. This parameter is optional.
        /// <br/>Default value: The bitwise zero value of <typeparamref name="TEnum"/>.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <paramref name="defaultValue"/>, even if it is undefined.</returns>
        public static TEnum GetDefinedOrDefault(ReadOnlySpan<char> value, TEnum defaultValue = default) => NameValuePairs.GetValueOrDefault(value, defaultValue);
#endif

        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in the enumeration;
        /// otherwise, returns <paramref name="defaultValue"/>, even if it is undefined.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <param name="defaultValue">A <typeparamref name="TEnum"/> value to return if <paramref name="value"/>
        /// is not defined in <typeparamref name="TEnum"/>. It does not needed to be a defined value. This parameter is optional.
        /// <br/>Default value: The bitwise zero value of <typeparamref name="TEnum"/>.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <paramref name="defaultValue"/>, even if it is undefined.</returns>
        public static TEnum GetDefinedOrDefault(long value, TEnum defaultValue = default) => IsDefined(value) ? converter.ToEnum((ulong)value) : defaultValue;

        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in the enumeration;
        /// otherwise, returns <paramref name="defaultValue"/>, even if it is undefined.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <param name="defaultValue">A <typeparamref name="TEnum"/> value to return if <paramref name="value"/>
        /// is not defined in <typeparamref name="TEnum"/>. It does not needed to be a defined value. This parameter is optional.
        /// <br/>Default value: The bitwise zero value of <typeparamref name="TEnum"/>.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <paramref name="defaultValue"/>, even if it is undefined.</returns>
        [CLSCompliant(false)]
        public static TEnum GetDefinedOrDefault(ulong value, TEnum defaultValue = default) => IsDefined(value) ? converter.ToEnum(value) : defaultValue;

        /// <summary>
        /// Returns <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>; otherwise, returns <see langword="null"/>.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value.</param>
        /// <returns><paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>; otherwise, <see langword="null"/>.</returns>
        public static TEnum? GetDefinedOrNull(TEnum value) => IsDefined(value) ? value : null;

        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <see langword="null"/>. The search is case-sensitive.
        /// </summary>
        /// <param name="value">A <see cref="string">string</see> value representing a field name in the enumeration.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static TEnum? GetDefinedOrNull(string value)
        {
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);
            return NameValuePairs.TryGetValue(value, out TEnum result) ? result : null;
        }

        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <see langword="null"/>. The search is case-sensitive.
        /// </summary>
        /// <param name="value">A <see cref="StringSegment"/> value representing a field name in the enumeration.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see cref="StringSegment.Null"/>.</exception>
        public static TEnum? GetDefinedOrNull(StringSegment value)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            return NameValuePairs.TryGetValue(value, out TEnum result) ? result : null;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <see langword="null"/>. The search is case-sensitive.
        /// </summary>
        /// <param name="value">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> value representing a field name in the enumeration.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <see langword="null"/>.</returns>
        public static TEnum? GetDefinedOrNull(ReadOnlySpan<char> value) => NameValuePairs.TryGetValue(value, out TEnum result) ? result : null;
#endif

        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <see langword="null"/>.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <see langword="null"/>.</returns>
        public static TEnum? GetDefinedOrNull(long value) => IsDefined(value) ? converter.ToEnum((ulong)value) : null;

        /// <summary>
        /// Returns the <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, returns <see langword="null"/>.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <returns>The <typeparamref name="TEnum"/> value associated with <paramref name="value"/> if it is defined in <typeparamref name="TEnum"/>;
        /// otherwise, <see langword="null"/>.</returns>
        [CLSCompliant(false)]
        public static TEnum? GetDefinedOrNull(ulong value) => IsDefined(value) ? converter.ToEnum(value) : null;

        #endregion

        #region Flags

        /// <summary>
        /// Gets whether the bits that are set in the <paramref name="flags"/> parameter are set in the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An enumeration value of <typeparamref name="TEnum"/> type.</param>
        /// <param name="flags">A flags <see langword="enum"/> value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
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
        /// <param name="flags">A flags <see langword="enum"/> value, whose bits should be checked. It is not checked whether <typeparamref name="TEnum"/>
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
            return rawValueNamePairs.RawValues!.Where(UInt64Extensions.IsSingleFlag).Select(converter.ToEnum);
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{TEnum}"/> enumeration of <paramref name="flags"/>,
        /// where each flags are returned as distinct values.
        /// </summary>
        /// <param name="flags">A flags <see langword="enum"/> value, whose flags should be returned. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <param name="onlyDefinedValues"><see langword="true"/> to return only flags that are defined in <typeparamref name="TEnum"/>;
        /// <see langword="false"/> to return also undefined flags. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A lazy-enumerated <see cref="IEnumerable{TEnum}"/> instance containing each flags of <paramref name="flags"/> as distinct values.</returns>
        /// <remarks>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public static IEnumerable<TEnum> GetFlags(TEnum flags, bool onlyDefinedValues = false)
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

        #endregion

        #region Clear

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
                ulong value = converter.ToUInt64(values![i]);

                // avoiding duplicated keys (multiple names for the same value)
                if (!result.ContainsKey(value))
                    result.Add(value, names![i]);
            }

            // initializing names first, because Ensure checks values, and the initialization does not use locking
            rawValueNamePairs.Names = result.Values.ToArray();
            rawValueNamePairs.RawValues = result.Keys.ToArray();
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static void EnsureRawValueUtf8NamePairs()
        {
            EnsureRawValueNamePairs();
            if (utf8Names.Names == null)
                InitUtf8Names();
        }

        private static void InitUtf8Names()
        {
            Debug.Assert(rawValueNamePairs.Names != null);
            var result = new byte[rawValueNamePairs.Names!.Length][];
            var isValid = new bool[result.Length];
            for (int i = 0; i < rawValueNamePairs.Names.Length; i++)
            {
                isValid[i] = rawValueNamePairs.Names[i].IsValidUnicode();
                result[i] = isValid[i] ? Encoding.UTF8.GetBytes(rawValueNamePairs.Names[i]) : ToNumericStringUtf8(rawValueNamePairs.RawValues![i]);
            }

            utf8Names.IsValidName = isValid;
            utf8Names.Names = result;
        }
#endif

        private static string[] InitNames()
        {
            lock (syncRoot)
                return names = Enum.GetNames(typeof(TEnum));
        }

        private static TEnum[] InitValues()
        {
            lock (syncRoot)
                return values = (TEnum[])Enum.GetValues(typeof(TEnum));
        }

        private static Array InitUnderlyingValues()
        {
            lock (syncRoot)
            {
#if NET7_0_OR_GREATER
                return underlyingValues = Enum.GetValuesAsUnderlyingType(typeof(TEnum));
#else
                EnsureRawValueNamePairs();
                ulong[] rawValues = rawValueNamePairs.RawValues!;
                switch (underlyingInfo.TypeCode)
                {
                    case TypeCode.SByte:
                        {
                            var result = new sbyte[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = (sbyte)rawValues[i];
                            return underlyingValues = result;
                        }

                    case TypeCode.Byte:
                        {
                            var result = new byte[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = (byte)rawValues[i];
                            return underlyingValues = result;
                        }

                    case TypeCode.Int16:
                        {
                            var result = new short[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = (short)rawValues[i];
                            return underlyingValues = result;
                        }

                    case TypeCode.UInt16:
                        {
                            var result = new ushort[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = (ushort)rawValues[i];
                            return underlyingValues = result;
                        }

                    case TypeCode.Int32:
                        {
                            var result = new int[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = (int)rawValues[i];
                            return underlyingValues = result;
                        }

                    case TypeCode.UInt32:
                        {
                            var result = new uint[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = (uint)rawValues[i];
                            return underlyingValues = result;
                        }

                    case TypeCode.Int64:
                        {
                            var result = new long[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = (long)rawValues[i];
                            return underlyingValues = result;
                        }

                    case TypeCode.UInt64:
                        return underlyingValues = rawValues;

                    case TypeCode.Boolean:
                        {
                            var result = new bool[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = rawValues[i] == 1UL;
                            return underlyingValues = result;
                        }

                    case TypeCode.Char:
                        {
                            var result = new char[rawValues.Length];
                            for (int i = 0; i < result.Length; i++)
                                result[i] = (char)rawValues[i];
                            return underlyingValues = result;
                        }

                    default:
                        return Throw.InternalError<Array>($"Not an enum type: {typeof(TEnum)}");
                }
#endif
            }
        }

        private static StringKeyedDictionary<TEnum> InitNameValuePairs()
        {
            lock (syncRoot)
            {
                StringKeyedDictionary<TEnum>? result = nameValuePairs;

                // lost race
                if (result != null)
                    return result;

                result = new StringKeyedDictionary<TEnum>(Names.Length);
                for (int i = 0; i < Values.Length; i++)
                    result.Add(names![i], values![i]);
                return nameValuePairs = result;
            }
        }

        private static Dictionary<TEnum, string> InitValueNamePairs()
        {
            lock (syncRoot)
            {
                Dictionary<TEnum, string>? result = valueNamePairs;

                // lost race
                if (result != null)
                    return result;

                result = new Dictionary<TEnum, string>(Names.Length, ComparerHelper<TEnum>.EqualityComparer);
                for (int i = 0; i < Values.Length; i++)
                {
                    // avoiding duplicated keys (multiple names for the same value)
                    if (!result.ContainsKey(values![i]))
                        result.Add(values[i], names![i]);
                }

                return valueNamePairs = result;
            }
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static Dictionary<TEnum, byte[]> InitValueUtf8NamePairs()
        {
            lock (syncRoot)
            {
                Dictionary<TEnum, byte[]>? result = valueUtf8NamePairs;

                // lost race
                if (result != null)
                    return result;

                result = new Dictionary<TEnum, byte[]>(Names.Length, ComparerHelper<TEnum>.EqualityComparer);
                for (int i = 0; i < Values.Length; i++)
                {
                    // avoiding duplicated keys (multiple names for the same value)
                    if (!result.ContainsKey(values![i]))
                        // is a name contains invalid sequence in UTF16, then its numeric representation is used because that can be parsed back
                        result.Add(values[i], names![i].IsValidUnicode() ? Encoding.UTF8.GetBytes(names[i]) : ToNumericStringUtf8(converter.ToUInt64(values[i])));
                }

                return valueUtf8NamePairs = result;
            }
        }
#endif

        private static StringKeyedDictionary<ulong> InitNameRawValuePairs()
        {
            lock (syncRoot)
            {
                StringKeyedDictionary<ulong>? result = nameRawValuePairs;

                // lost race
                if (result != null)
                    return result;

                result = new StringKeyedDictionary<ulong>(Names.Length);
                for (int i = 0; i < Values.Length; i++)
                    result.Add(names![i], converter.ToUInt64(values![i]));

                return nameRawValuePairs = result;
            }
        }

        private static StringKeyedDictionary<ulong> InitNameRawValuePairsIgnoreCase()
        {
            lock (syncRoot)
            {
                StringKeyedDictionary<ulong>? result = nameRawValuePairsIgnoreCase;

                // lost race
                if (result != null)
                    return result;

                result = new StringKeyedDictionary<ulong>(Names.Length, StringSegmentComparer.OrdinalIgnoreCase);
                StringKeyedDictionary<ulong> refDict = NameRawValuePairs;
                foreach (KeyValuePair<string, ulong> pair in refDict)
                    result[pair.Key] = pair.Value;

                return nameRawValuePairsIgnoreCase = result;
            }
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static int FindIndex(ulong value)
        {
            Debug.Assert(rawValueNamePairs.RawValues != null, $"{nameof(EnsureRawValueNamePairs)} was not called");
            return Array.BinarySearch(rawValueNamePairs.RawValues!, 0, rawValueNamePairs.RawValues!.Length, value);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static string? TryGetNameByValue(ulong value)
        {
            EnsureRawValueNamePairs();
            int index = FindIndex(value);
            return index >= 0 ? rawValueNamePairs.Names![index] : null;
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

        private static long ToSigned(ulong value)
            => underlyingInfo.TypeCode switch
            {
                TypeCode.Int32 => (int)value,
                TypeCode.Int64 => (long)value,
                TypeCode.Int16 => (short)value,
                _ => (sbyte)value
            };

        #endregion

        #endregion
    }
}
