#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumExtensions
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
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
using System.Collections.Generic;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="Enum"/> type (<see langword="enum"/>s).
    /// </summary>
    public static class EnumExtensions
    {
        #region Constants

        // These constants are defined here instead of Enum<TEnum> to avoid being defined multiple times
        internal const string DefaultFormatSeparator = ", ";
        internal const string DefaultParseSeparator = ",";

        #endregion

        #region Methods

        #region Generic methods

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given enum <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An <see name="Enum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="format">Formatting option.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/> or is empty, then comma-space (", ") separator is used.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="format"/>.</exception>
        public static string ToString<TEnum>(this TEnum value, EnumFormattingOptions format, string separator)
            where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.ToString(value, format, separator);
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given enum <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An <see name="Enum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="format">Formatting option.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="format"/>.</exception>
        public static string ToString<TEnum>(this TEnum value, EnumFormattingOptions format)
            where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.ToString(value, format, DefaultFormatSeparator);
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given enum <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An <see name="Enum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/> or is empty, then comma-space (", ") separator is used.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        public static string ToString<TEnum>(this TEnum value, string separator)
            where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.ToString(value, EnumFormattingOptions.Auto, separator);
        }

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given enum <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An <see name="Enum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        /// <remarks>
        /// <note>Always specify the <typeparamref name="TEnum"/> generic argument when use this method; otherwise, the less performant <see cref="Enum.ToString()"/> method will be used.</note>
        /// </remarks>
        public static string ToString<TEnum>(this TEnum value)
            where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.ToString(value, EnumFormattingOptions.Auto, DefaultFormatSeparator);
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The enum value whose name is required.</param>
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/> if no such constant is found.</returns>
        public static string GetName<TEnum>(this TEnum value)
            where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.GetName(value);
        }

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value.</param>
        /// <returns><see langword="true"/> if <typeparamref name="TEnum"/> has a defined field that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsDefined<TEnum>(this TEnum value)
            where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.IsDefined(value);
        }

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the <typeparamref name="TEnum"/> type,
        /// or when <paramref name="flags"/> is zero, it is checked whether zero is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="flags">A flags enum value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        public static bool AllFlagsDefined<TEnum>(this TEnum flags)
            where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.AllFlagsDefined(flags);
        }

        /// <summary>
        /// Gets whether the bits that are set in the <paramref name="flags"/> parameter are set in the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An enumeration value of <typeparamref name="TEnum"/> type.</param>
        /// <param name="flags">An unsigned integer value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/> and whether all bits that are set are defined in the <typeparamref name="TEnum"/> type.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is zero, or when the bits that are set in <paramref name="flags"/> are set in <paramref name="value"/>;
        /// otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <note>Always specify the <typeparamref name="TEnum"/> generic argument when use this method; otherwise, in .NET 4.0 and above the less performant <see cref="Enum.HasFlag"/> method will be used.</note>
        /// </remarks>
        public static bool HasFlag<TEnum>(this TEnum value, TEnum flags)
            where TEnum : struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.HasFlag(value, flags);
        }

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>. It is not checked, whether this flag is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if only a single bit is set in <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsSingleFlag<TEnum>(TEnum value)
            where TEnum : struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.IsSingleFlag(value);
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{TEnum}"/> enumeration of <paramref name="flags"/>,
        /// where each flags are returned as distinct values.
        /// </summary>
        /// <param name="flags">A flags enum value, whose flags should be returned. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <param name="onlyDefinedValues">When <see langword="true"/>, returns only flags, which are defined in <typeparamref name="TEnum"/>.</param>
        /// <returns>A lazy-enumerated <see cref="IEnumerable{TEnum}"/> instance containing each flags of <paramref name="flags"/> as distinct values.</returns>
        public static IEnumerable<TEnum> GetFlags<TEnum>(TEnum flags, bool onlyDefinedValues)
            where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
        {
            return Enum<TEnum>.GetFlags(flags, onlyDefinedValues);
        }

        #endregion

#region Non-generic Methods

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the type of the <see langword="enum"/>,
        /// or when <paramref name="flags"/> is zero, it is checked whether zero is defined in the type of the <see langword="enum"/>.
        /// </summary>
        /// <param name="flags">The <see langword="enum"/> value.</param>
        /// <returns><c>true</c>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        /// <remarks><note>For better performance use the generic overload (<see cref="AllFlagsDefined{TEnum}">AllFlagsDefined&lt;TEnum&gt;(TEnum)</see>) whenever it is possible.</note></remarks>
        public static bool AllFlagsDefined(this Enum flags)
        {
            var enumText = flags?.ToString("F") ?? "0";
            return !(char.IsDigit(enumText[0]) || enumText[0] == '-');
        }

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c>, if only a single bit is set in <paramref name="value"/>; otherwise, <c>false</c>.</returns>
        /// <remarks><note>For better performance use the generic overload (<see cref="IsSingleFlag{TEnum}">IsSingleFlag&lt;TEnum&gt;(TEnum)</see>) whenever it is possible.</note></remarks>
        public static bool IsSingleFlag(this Enum value)
        {
            if (value == null)
                return false;

            ulong rawValue = Enum.GetUnderlyingType(value.GetType()) == typeof(ulong) ? Convert.ToUInt64(value) : (ulong)Convert.ToInt64(value) & GetMask();
            return rawValue != 0UL && (rawValue & (rawValue - 1)) == 0UL;
        }

#endregion

#endregion
    }
}
