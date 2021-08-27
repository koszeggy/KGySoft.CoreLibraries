#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumExtensions
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
using System.Collections;
using System.Collections.Generic;

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="Enum"/> type.
    /// </summary>
    public static class EnumExtensions
    {
        #region Constants

        // These constants are defined here instead of Enum<TEnum> to avoid being defined multiple times
        internal const string DefaultFormatSeparator = ", ";
        internal const string DefaultParseSeparator = ",";

        #endregion

        #region Methods

        #region Public methods

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given <see langword="enum"/>&#160;value specified in the <paramref name="value"/> parameter.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="value"/>.</typeparam>
        /// <param name="value">An <see name="Enum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="format">Formatting option. This parameter is optional.
        /// <br/>Default value: <see cref="EnumFormattingOptions.Auto"/>.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/>&#160;or is empty, then comma-space (<c>, </c>) separator is used. This parameter is optional.
        /// <br/>Default value: <c>, </c>.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="format"/>.</exception>
        public static string ToString<TEnum>(this TEnum value, EnumFormattingOptions format = EnumFormattingOptions.Auto, string? separator = DefaultFormatSeparator)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.ToString(value, format, separator);
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given enum <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="value"/>.</typeparam>
        /// <param name="value">An <see name="Enum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/>&#160;or is empty, then comma-space (", ") separator is used.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        public static string ToString<TEnum>(this TEnum value, string? separator)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.ToString(value, EnumFormattingOptions.Auto, separator);
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="value"/>.</typeparam>
        /// <param name="value">The enum value whose name is required.</param>
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/>&#160;if no such constant is found.</returns>
        public static string? GetName<TEnum>(this TEnum value)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.GetName(value);
        }

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="value"/>.</typeparam>
        /// <param name="value">A <typeparamref name="TEnum"/> value.</param>
        /// <returns><see langword="true"/>&#160;if <typeparamref name="TEnum"/> has a defined field that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsDefined<TEnum>(this TEnum value)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.IsDefined(value);
        }

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the <typeparamref name="TEnum"/> type,
        /// or, when <paramref name="flags"/> is zero, it is checked whether zero is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="flags"/>.</typeparam>
        /// <param name="flags">A flags enum value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        public static bool AllFlagsDefined<TEnum>(this TEnum flags)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.AllFlagsDefined(flags);
        }

        /// <summary>
        /// Gets whether the bits that are set in the <paramref name="flags"/> parameter are set in the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="value"/>.</typeparam>
        /// <param name="value">An enumeration value of <typeparamref name="TEnum"/> type.</param>
        /// <param name="flags">An unsigned integer value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/> and whether all bits that are set are defined in the <typeparamref name="TEnum"/> type.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is zero, or when the bits that are set in <paramref name="flags"/> are set in <paramref name="value"/>;
        /// otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <note>Always specify the <typeparamref name="TEnum"/> generic argument when use this method; otherwise, in .NET 4.0 and above the less performant <see cref="Enum.HasFlag"/> method will be used.</note>
        /// </remarks>
        public static bool HasFlag<TEnum>(this TEnum value, TEnum flags)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.HasFlag(value, flags);
        }

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>. It is not checked, whether this flag is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="value"/>.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if only a single bit is set in <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsSingleFlag<TEnum>(this TEnum value)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.IsSingleFlag(value);
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{TEnum}"/> enumeration of <paramref name="flags"/>,
        /// where each flags are returned as distinct values.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="flags"/>.</typeparam>
        /// <param name="flags">A flags enum value, whose flags should be returned. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <param name="onlyDefinedValues"><see langword="true"/>&#160;to return only flags that are defined in <typeparamref name="TEnum"/>;
        /// <see langword="false"/>&#160;to return also undefined flags. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A lazy-enumerated <see cref="IEnumerable{TEnum}"/> instance containing each flags of <paramref name="flags"/> as distinct values.</returns>
        /// <remarks>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public static IEnumerable<TEnum> GetFlags<TEnum>(this TEnum flags, bool onlyDefinedValues = false)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.GetFlags(flags, onlyDefinedValues);
        }

        /// <summary>
        /// Gets the number of bits set in <paramref name="value"/>.
        /// It is not checked, whether all flags are defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>&#160;<paramref name="value"/>.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <returns>The number of bits set in <paramref name="value"/>.</returns>
        public static int GetFlagsCount<TEnum>(this TEnum value)
            where TEnum : struct, Enum
        {
            return Enum<TEnum>.GetFlagsCount(value);
        }

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the <see langword="enum"/> type of <paramref name="flags"/>,
        /// or when <paramref name="flags"/> is zero, it is checked whether zero is defined in the <see langword="enum"/> type of <paramref name="flags"/>.
        /// </summary>
        /// <param name="flags">The <see langword="enum"/>&#160;value.</param>
        /// <returns><c>true</c>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        /// <remarks><note>For better performance use the generic <see cref="AllFlagsDefined{TEnum}">AllFlagsDefined</see> overload whenever it is possible.</note></remarks>
        public static bool AllFlagsDefined(this Enum? flags)
        {
            if (flags == null)
                return false;
            string enumText = flags.ToString("F");
            return !(char.IsDigit(enumText[0]) || enumText[0] == '-');
        }

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c>, if only a single bit is set in <paramref name="value"/>; otherwise, <c>false</c>.</returns>
        public static bool IsSingleFlag(this Enum? value)
        {
            if (value == null)
                return false;
            ulong rawValue = ToUInt64(value);
            return rawValue != 0UL && (rawValue & (rawValue - 1)) == 0UL;
        }

        #endregion

        #region Internal Methods

        internal static ulong ToUInt64(this Enum value)
        {
            IConvertible convertible = value;
            return value.GetTypeCode() switch
            {
                TypeCode.SByte => (byte)convertible.ToSByte(null),
                TypeCode.Int16 => (ushort)convertible.ToInt16(null),
                TypeCode.Int32 => (uint)convertible.ToInt32(null),
                TypeCode.Int64 => (ulong)convertible.ToInt64(null),
                _ => convertible.ToUInt64(null)
            };
        }

        #endregion

        #endregion
    }
}
