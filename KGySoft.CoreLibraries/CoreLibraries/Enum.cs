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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Generic helper class for the <see cref="Enum"/> class.
    /// Provides faster solutions for already existing functionalities in the <see cref="Enum"/> class along with
    /// some additional functionality.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration. Must be an <see cref="Enum"/> type.</typeparam>
    public static class Enum<TEnum> where TEnum: struct, IConvertible // replaced to System.Enum by RecompILer
    {
        #region Fields

        // ReSharper disable StaticMemberInGenericType - values are specific for TEnum
        private static readonly Type enumType = typeof(TEnum);
        private static readonly Type underlyingType;
        private static readonly bool isFlags;
        private static readonly bool isSigned;
        private static readonly long min;
        private static readonly ulong max;
        private static readonly ulong sizeMask; // masking is needed because binary representation of (ulong)(1L << 31) != (1UL << 31), for example
        private static readonly object syncRoot = new object(); // locks are used so that multiple threads may assign a field multiple times but it is still faster than locking fields even on non-null access
        private static readonly Func<ulong, TEnum> toEnum;

        private static TEnum[] values;
        private static string[] names;
        private static Dictionary<TEnum, string> valueNamePairs;
        private static CircularSortedList<ulong, string> numValueNamePairs;
        private static Dictionary<string, TEnum> nameValuePairs;
        private static Dictionary<string, ulong> nameNumValuePairs;
        private static Dictionary<string, ulong> nameNumValuePairsIgnoreCase;
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
                    return names = Enum.GetNames(enumType);
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
                    return values = (TEnum[])Enum.GetValues(enumType);
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
                    // ReSharper disable once JoinDeclarationAndInitializer - #if branches
                    IEqualityComparer<TEnum> comparer;
#if NET35
                    comparer = EnumComparer<TEnum>.Comparer;
#elif NET40 || NET45
                    comparer = underlyingType == Reflector.IntType
                        ? (IEqualityComparer<TEnum>)EqualityComparer<TEnum>.Default
                        : EnumComparer<TEnum>.Comparer;
#else
#error .NET version is not set or not supported!
#endif
                    result = new Dictionary<TEnum, string>(Names.Length, comparer);
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

        private static CircularSortedList<ulong, string> NumValueNamePairs
        {
            get
            {
                CircularSortedList<ulong, string> result = numValueNamePairs;

                if (result != null)
                    return result;

                lock (syncRoot)
                {
                    result = new CircularSortedList<ulong, string>(Names.Length);
                    for (int i = 0; i < Values.Length; i++)
                    {
                        ulong value = isSigned ? (ulong)values[i].ToInt64(null) & sizeMask : values[i].ToUInt64(null);

                        // avoiding duplicated keys (multiple names for the same value)
                        if (!result.ContainsKey(value))
                            result.Add(value, names[i]);
                    }

                    return numValueNamePairs = result;
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

        private static Dictionary<string, ulong> NameNumValuePairs
        {
            get
            {
                Dictionary<string, ulong> result = nameNumValuePairs;
                if (result != null)
                    return result;

                lock (syncRoot)
                {
                    result = new Dictionary<string, ulong>(Names.Length);
                    for (int i = 0; i < Values.Length; i++)
                    {
                        ulong value = isSigned ? (ulong)values[i].ToInt64(null) & sizeMask : values[i].ToUInt64(null);
                        result.Add(names[i], value);
                    }

                    return nameNumValuePairs = result;
                }
            }
        }

        private static Dictionary<string, ulong> NameNumValuePairsIgnoreCase
        {
            get
            {
                Dictionary<string, ulong> result = nameNumValuePairsIgnoreCase;
                if (result != null)
                    return result;

                result = new Dictionary<string, ulong>(Names.Length, StringComparer.OrdinalIgnoreCase);
                Dictionary<string, ulong> refDict = NameNumValuePairs;
                foreach (KeyValuePair<string, ulong> pair in refDict)
                    result[pair.Key] = pair.Value;

                lock (syncRoot)
                    return nameNumValuePairsIgnoreCase = result;
            }
        }

        private static string Zero
        {
            get
            {
                if (Names.Length == 0)
                    return "0";

                if (NameNumValuePairs[names[0]] == 0UL)
                    return names[0];

                return "0";
            }
        }

        #endregion

        #region Constructors

        static Enum()
        {
            if (!typeof(TEnum).IsEnum)
                throw new InvalidOperationException(Res.EnumTypeParameterInvalid);

            underlyingType = Enum.GetUnderlyingType(enumType);
            isFlags = enumType.IsFlagsEnum();
            isSigned = underlyingType.In(Reflector.SByteType, Reflector.ShortType, Reflector.IntType, Reflector.LongType);
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.SByte:
                    sizeMask = Byte.MaxValue;
                    min = SByte.MinValue;
                    max = (ulong)SByte.MaxValue;
                    break;
                case TypeCode.Byte:
                    sizeMask = Byte.MaxValue;
                    max = Byte.MaxValue;
                    break;
                case TypeCode.Int16:
                    sizeMask = UInt16.MaxValue;
                    min = Int16.MinValue;
                    max = (ulong)Int16.MaxValue;
                    break;
                case TypeCode.UInt16:
                    sizeMask = UInt16.MaxValue;
                    max = UInt16.MaxValue;
                    break;
                case TypeCode.Int32:
                    sizeMask = UInt32.MaxValue;
                    min = Int32.MinValue;
                    max = Int32.MaxValue;
                    break;
                case TypeCode.UInt32:
                    sizeMask = UInt32.MaxValue;
                    max = UInt32.MaxValue;
                    break;
                case TypeCode.Int64:
                    sizeMask = UInt64.MaxValue;
                    min = Int64.MinValue;
                    max = Int64.MaxValue;
                    break;
                case TypeCode.UInt64:
                    sizeMask = UInt64.MaxValue;
                    max = UInt64.MaxValue;
                    break;
            }

            ParameterExpression parameter = Expression.Parameter(Reflector.ULongType, "value");
            LambdaExpression lambda = Expression.Lambda<Func<ulong, TEnum>>(Expression.Convert(parameter, typeof(TEnum)), parameter);
            toEnum = (Func<ulong, TEnum>)lambda.Compile();
        }

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
            if (value < min || isSigned && value > (long)max || !isSigned && (ulong)value > max)
                return null;

            NumValueNamePairs.TryGetValue((ulong)value & sizeMask, out string result);
            return result;
        }

        /// <summary>
        /// Retrieves the name of the constant in the specified enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value of the required field.</param>
        /// <returns>A string containing the name of the enumerated <paramref name="value"/>, or <see langword="null"/>&#160;if no such constant is found.</returns>
        public static string GetName(ulong value)
        {
            if (value > max)
                return null;

            NumValueNamePairs.TryGetValue(value, out string result);
            return result;
        }

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
        public static bool IsDefined(string value) => value == null ? throw new ArgumentNullException(nameof(value), Res.ArgumentNull) : NameValuePairs.ContainsKey(value);

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/> as a field value.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <returns><see langword="true"/>&#160;if <typeparamref name="TEnum"/> has a field whose value that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsDefined(long value)
        {
            if (value < min || isSigned && value > (long)max || !isSigned && (ulong)value > max)
                return false;
            return NumValueNamePairs.ContainsKey((ulong)value & sizeMask);
        }

        /// <summary>
        /// Gets whether <paramref name="value"/> is defined in <typeparamref name="TEnum"/> as a field value.
        /// </summary>
        /// <param name="value">A numeric value representing a field value in the enumeration.</param>
        /// <returns><see langword="true"/>&#160;if <typeparamref name="TEnum"/> has a field whose value that equals <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsDefined(ulong value) => value <= max && NumValueNamePairs.ContainsKey(value);

        /// <summary>
        /// Gets whether the bits that are set in the <paramref name="flags"/> parameter are set in the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An enumeration value of <typeparamref name="TEnum"/> type.</param>
        /// <param name="flags">A flags <see langword="enum"/>&#160;value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/> and whether all bits that are set are defined in the <typeparamref name="TEnum"/> type.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is zero, or when the bits that are set in <paramref name="flags"/> are set in <paramref name="value"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool HasFlag(TEnum value, TEnum flags)
        {
            ulong rawFlags = isSigned ? (ulong)flags.ToInt64(null) & sizeMask : flags.ToUInt64(null);
            return HasFlagCore(value, rawFlags);
        }

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
            if (flags < min || isSigned && flags > (long)max || !isSigned && (ulong)flags > max)
                return false;
            return HasFlagCore(value, (ulong)flags & sizeMask);
        }

        /// <summary>
        /// Gets whether the bits that are set in the <paramref name="flags"/> parameter are set in the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">An enumeration value of <typeparamref name="TEnum"/> type.</param>
        /// <param name="flags">An unsigned integer value, whose flags should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/> and whether all bits that are set are defined in the <typeparamref name="TEnum"/> type.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is zero, or when the bits that are set in <paramref name="flags"/> are set in <paramref name="value"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool HasFlag(TEnum value, ulong flags) => flags <= max && HasFlagCore(value, flags);

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>. It is not checked, whether this flag is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if only a single bit is set in <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsSingleFlag(TEnum value)
        {
            ulong rawValue = isSigned ? (ulong)value.ToInt64(null) & sizeMask : value.ToUInt64(null);
            return rawValue != 0 && (rawValue & (rawValue - 1)) == 0;
        }

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>. It is not checked, whether this flag is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> falls into the range of <typeparamref name="TEnum"/> range
        /// and only a single bit is set in <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsSingleFlag(long value)
        {
            if (value == 0L || value < min || isSigned && value > (long)max || !isSigned && (ulong)value > max)
                return false;
            return (value & (value - 1)) == 0L;
        }

        /// <summary>
        /// Gets whether only a single bit is set in <paramref name="value"/>. It is not checked, whether this flag is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/>, if <paramref name="value"/> falls into the range of <typeparamref name="TEnum"/> range
        /// and only a single bit is set in <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public static bool IsSingleFlag(ulong value)
        {
            if (value == 0UL || value > max)
                return false;
            return (value & (value - 1)) == 0UL;
        }

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the <typeparamref name="TEnum"/> type,
        /// or, when <paramref name="flags"/> is zero, it is checked whether zero is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="flags">A flags <see langword="enum"/>&#160;value, whose bits should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        public static bool AllFlagsDefined(TEnum flags)
        {
            ulong value = isSigned ? (ulong)flags.ToInt64(null) & sizeMask : flags.ToUInt64(null);
            return AllFlagsDefinedCore(value);
        }

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
            if (flags < min || isSigned && flags > (long)max || !isSigned && (ulong)flags > max)
                return false;
            return AllFlagsDefinedCore((ulong)flags & sizeMask);
        }

        /// <summary>
        /// Gets whether every single bit value in <paramref name="flags"/> are defined in the <typeparamref name="TEnum"/> type,
        /// or, when <paramref name="flags"/> is zero, it is checked whether zero is defined in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="flags">An unsigned integer value, whose bits should be checked. It is not checked whether <typeparamref name="TEnum"/>
        /// is really marked by <see cref="FlagsAttribute"/>.</param>
        /// <returns><see langword="true"/>, if <paramref name="flags"/> is a zero value and zero is defined,
        /// or if <paramref name="flags"/> is nonzero and its every bit has a defined name.</returns>
        public static bool AllFlagsDefined(ulong flags) => flags <= max && AllFlagsDefinedCore(flags);

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
                throw new ArgumentNullException(nameof(value), Res.ArgumentNull);
            
            // simple name match test
            if (NameValuePairs.TryGetValue(value, out result))
                return true;

            value = value.Trim();
            result = default(TEnum);
            if (value.Length == 0)
                return false;

            // simple numeric value
            if ((value[0] >= '0' && value[0] <= '9') || value[0] == '-' || value[0] == '+')
            {
                if (isSigned)
                {
                    if (Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long numericValue))
                    {
                        if (numericValue < min || numericValue > (long)max)
                            return false;
                        result = toEnum.Invoke((ulong)numericValue);
                        return true;
                    }
                }
                else
                {
                    if (UInt64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong numericValue))
                    {
                        if (numericValue > max)
                            return false;
                        result = toEnum.Invoke(numericValue);
                        return true;
                    }
                }

                result = default(TEnum);
            }

            // rest: flags enum or ignored case
            if (String.IsNullOrEmpty(separator))
                separator = EnumExtensions.DefaultParseSeparator;

            string[] tokens = separator.Length == 1 ? value.Split(separator[0]) : value.Split(new[] { separator }, StringSplitOptions.None);
            ulong acc = 0UL;
            foreach (string t in tokens)
            {
                string token = t.Trim();
                if (token.Length == 0)
                    return false;

                // literal token found in dictionary
                if (NameNumValuePairs.TryGetValue(token, out ulong tokenValue))
                {
                    acc |= tokenValue;
                    continue;
                }

                // checking for case-insensitive match
                if (ignoreCase && NameNumValuePairsIgnoreCase.TryGetValue(token, out tokenValue))
                {
                    acc |= tokenValue;
                    continue;
                }

                // checking if is numeric token
                if ((token[0] >= '0' && token[0] <= '9') || token[0] == '-' || token[0] == '+')
                {
                    if (isSigned)
                    {
                        if (Int64.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long numericValue))
                        {
                            if (numericValue < min || numericValue > (long)max)
                                return false;
                            acc |= (ulong)numericValue;
                            continue;
                        }
                    }
                    else
                    {
                        if (UInt64.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong numericValue))
                        {
                            if (numericValue > max)
                                return false;
                            acc |= numericValue;
                            continue;
                        }
                    }

                    return false;
                }

                // none of above
                return false;
            }

            result = toEnum.Invoke(acc);
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
            => !TryParse(value, separator, ignoreCase, out TEnum result) 
                ? throw new ArgumentException(Res.EnumValueCannotBeParsedAsEnum(value, enumType), nameof(value)) 
                : result;

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated values to an equivalent enumerated object.
        /// </summary>
        /// <param name="value">The <see cref="string"/> representation of the enumerated value or values to parse.</param>
        /// <param name="ignoreCase">If <see langword="true"/>, ignores case; otherwise, regards case.</param>
        /// <returns>The parsed <see langword="enum"/>&#160;value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> cannot be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> cannot be parsed as <typeparamref name="TEnum"/>.</exception>
        public static TEnum Parse(string value, bool ignoreCase) 
            => !TryParse(value, EnumExtensions.DefaultParseSeparator, ignoreCase, out TEnum result) 
                ? throw new ArgumentException(Res.EnumValueCannotBeParsedAsEnum(value, enumType), nameof(value)) 
                : result;

        /// <summary>
        /// Returns the <see cref="string"/> representation of the given <see langword="enum"/>&#160;value specified in the <paramref name="value"/> parameter.
        /// </summary>
        /// <param name="value">A <typeparamref name="TEnum"/> value that has to be converted to <see cref="string"/>.</param>
        /// <param name="format">Formatting option. This parameter is optional.
        /// <br/>Default value: <see cref="EnumFormattingOptions.Auto"/>.</param>
        /// <param name="separator">Separator in case of flags formatting. If <see langword="null"/>&#160;or is empty, then comma-space (<c>, </c>) separator is used. This parameter is optional.
        /// <br/>Default value: <c>, </c>.</param>
        /// <returns>The string representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid <paramref name="format"/>.</exception>
        public static string ToString(TEnum value, EnumFormattingOptions format = EnumFormattingOptions.Auto, string separator = EnumExtensions.DefaultFormatSeparator)
        {
            if ((uint)format > (uint)EnumFormattingOptions.CompoundFlagsAndNumber)
                throw new ArgumentOutOfRangeException(nameof(format), Res.EnumOutOfRange(format));

            if (format == EnumFormattingOptions.DistinctFlags)
                return FormatDistinctFlags(value, separator);

            // defined value exists
            if (ValueNamePairs.TryGetValue(value, out string name))
                return name;

            // if single value is requested returning a number
            if ((format == EnumFormattingOptions.Auto && !isFlags) || format == EnumFormattingOptions.NonFlags)
                return isSigned ? value.ToInt64(null).ToString(CultureInfo.InvariantCulture) : value.ToUInt64(null).ToString(CultureInfo.InvariantCulture);

            // returning as flags
            return FormatFlags(value, separator, format == EnumFormattingOptions.CompoundFlagsAndNumber);
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
        public static IEnumerable<TEnum> GetFlags() => Values.Where(IsSingleFlag).Distinct();

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
            ulong value = isSigned ? (ulong)flags.ToInt64(null) & sizeMask : flags.ToUInt64(null);
            if (value == 0UL)
                yield break;

            for (int i = 0; i <= 63; i++)
            {
                ulong flag = 1UL << i;
                if ((value & flag) != 0)
                {
                    if (!onlyDefinedValues || NumValueNamePairs.ContainsKey(flag))
                        yield return toEnum.Invoke(flag);

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
                nameNumValuePairs = null;
                numValueNamePairs = null;
            }
        }

        #endregion

        #region Private Methods

        private static bool AllFlagsDefinedCore(ulong flags)
        {
            if (flags == 0UL)
                return NumValueNamePairs.ContainsKey(0UL);

            for (int i = 0; i <= 63; i++)
            {
                ulong flag = 1UL << i;
                if ((flags & flag) != 0UL)
                {
                    if (!NumValueNamePairs.ContainsKey(flag))
                        return false;

                    flags &= ~flag;
                    if (flags == 0UL)
                        break;
                }
            }

            return true;
        }

        private static bool HasFlagCore(TEnum value, ulong flags)
        {
            if (flags == 0UL)
                return true;

            ulong rawValue = isSigned ? (ulong)value.ToInt64(null) & sizeMask : value.ToUInt64(null);
            return (rawValue & flags) == flags;
        }

        private static string FormatDistinctFlags(TEnum e, string separator)
        {
            ulong value = isSigned ? (ulong)e.ToInt64(null) & sizeMask : e.ToUInt64(null);
            if (value == 0UL)
                return Zero;

            if (String.IsNullOrEmpty(separator))
                separator = EnumExtensions.DefaultFormatSeparator;
            StringBuilder result = new StringBuilder();
            bool first = true;

            for (int i = 0; i <= 63; i++)
            {
                ulong flagValue = 1UL << i;
                if ((value & flagValue) != 0UL)
                {
                    if (!first)
                        result.Append(separator);
                    else
                        first = false;

                    if (NumValueNamePairs.TryGetValue(flagValue, out string name))
                        result.Append(name);
                    else
                        result.Append(!isSigned ? flagValue.ToString(CultureInfo.InvariantCulture) : ToSignedIntegerString((long)flagValue));

                    value &= ~flagValue;
                    if (value == 0UL)
                        break;
                }
            }

            return result.ToString();
        }

        private static string FormatFlags(TEnum e, string separator, bool allowNumberWithNames)
        {
            ulong origNumValue = isSigned ? (ulong)e.ToInt64(null) & sizeMask : e.ToUInt64(null);
            ulong value = origNumValue;
            if (value == 0UL)
                return Zero;

            if (String.IsNullOrEmpty(separator))
                separator = EnumExtensions.DefaultFormatSeparator;
            StringBuilder result = new StringBuilder();
            IList<ulong> numValues = NumValueNamePairs.Keys;
            bool first = true;

            // processing existing values
            for (int i = numValues.Count - 1; value > 0 && i >= 0; i--)
            {
                ulong biggestValue = numValues[i];
                if (biggestValue == 0UL)
                    break;

                if ((value & biggestValue) == biggestValue)
                {
                    if (!first)
                        result.Insert(0, separator);
                    else
                        first = false;
                    result.Insert(0, numValueNamePairs.Values[i]);
                    value &= ~biggestValue;
                }
            }

            // processing rest
            if (value != 0UL)
            {
                if (allowNumberWithNames)
                {
                    if (!first)
                        result.Insert(0, separator);
                    result.Insert(0, isSigned ? ToSignedIntegerString((long)value) : value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    return isSigned ? ToSignedIntegerString((long)origNumValue) : origNumValue.ToString(CultureInfo.InvariantCulture);
                }
            }

            return result.ToString();
        }

        private static string ToSignedIntegerString(long nativeValue)
        {
            if (nativeValue <= (long)max)
                return nativeValue.ToString(CultureInfo.InvariantCulture);

            // eg. (int)-10: after the ulong->long conversion this is a bigger positive number as max
            unchecked
            {
                if (underlyingType == Reflector.IntType)
                    return ((int)nativeValue).ToString(CultureInfo.InvariantCulture);

                if (underlyingType == Reflector.ShortType)
                    return ((short)nativeValue).ToString(CultureInfo.InvariantCulture);

                if (underlyingType == Reflector.SByteType)
                    return ((sbyte)nativeValue).ToString(CultureInfo.InvariantCulture);
            }

            // should never occur, throwing internal error without resource
            throw new InvalidOperationException("Unexpected signed base type");
        }

        #endregion

        #endregion
    }
}
