#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringExtensions.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    partial class StringExtensions
    {
        #region Nested classes

        #region Parser class

        /// <summary>
        /// A separate class so knowing all the supported types do not impact the <see cref="StringExtensions"/> class.
        /// </summary>
        private static class Parser
        {
            #region Delegates

            private delegate bool ParseDelegate(string s, CultureInfo culture, out object value);

            #endregion

            #region Constants

            private const NumberStyles floatStyle = NumberStyles.Float | NumberStyles.AllowThousands;

            #endregion

            #region Fields

            private static readonly Dictionary<Type, ParseDelegate> knownTypes = new Dictionary<Type, ParseDelegate>
            {
                { Reflector.BoolType, TryParseBoolean },

                { Reflector.ByteType, TryParseByte },
                { Reflector.SByteType, TryParseSByte },
                { Reflector.ShortType, TryParseInt16 },
                { Reflector.UShortType, TryParseUInt16 },
                { Reflector.IntType, TryParseInt32 },
                { Reflector.UIntType, TryParseUInt32 },
                { Reflector.LongType, TryParseInt64 },
                { Reflector.ULongType, TryParseUInt64 },

                { Reflector.IntPtrType, TryParseIntPtr },
                { Reflector.UIntPtrType, TryParseUIntPtr },

                { Reflector.CharType, TryParseChar },

                { Reflector.FloatType, TryParseSingle },
                { Reflector.DoubleType, TryParseDouble },
                { Reflector.DecimalType, TryParseDecimal },

                { Reflector.TimeSpanType, TryParseTimeSpan },
                { Reflector.DateTimeType, TryParseDateTime },
                { Reflector.DateTimeOffsetType, TryParseDateTimeOffset },
            };

            #endregion

            #region Methods

            #region Internal Methods

            internal static bool TryParse(string s, Type type, CultureInfo culture, out object value, out Exception error)
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

                type = Nullable.GetUnderlyingType(type) ?? type;
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
                        return Enum.TryParse(type, s, out value);
#endif
                    }

                    if (type == Reflector.StringType)
                    {
                        value = s;
                        return true;
                    }

                    if (knownTypes.TryGetValue(type, out var tryParseMethod))
                        return tryParseMethod.Invoke(s, culture, out value);

                    if (type.In(Reflector.Type, Reflector.RuntimeType
#if !NET35 && !NET40
                        , Reflector.TypeInfo
#endif
                    ))
                    {
                        value = Reflector.ResolveType(s);
                        return value != null;
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
                catch (Exception e) when (!e.IsCritical())
                {
                    error = e;
                    value = null;
                    return false;
                }
            }

            #endregion

            #region Private Methods

            private static bool TryParseBoolean(string s, CultureInfo culture, out object value)
            {
                s = s.Trim();
                if (String.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }

                if (s.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (Double.TryParse(s, floatStyle, culture, out double result))
                {
                    value = !result.Equals(0d);
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseByte(string s, CultureInfo culture, out object value)
            {
                if (Byte.TryParse(s, NumberStyles.Integer, culture, out byte result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseSByte(string s, CultureInfo culture, out object value)
            {
                if (SByte.TryParse(s, NumberStyles.Integer, culture, out sbyte result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt16(string s, CultureInfo culture, out object value)
            {
                if (Int16.TryParse(s, NumberStyles.Integer, culture, out short result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt16(string s, CultureInfo culture, out object value)
            {
                if (UInt16.TryParse(s, NumberStyles.Integer, culture, out ushort result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt32(string s, CultureInfo culture, out object value)
            {
                if (Int32.TryParse(s, NumberStyles.Integer, culture, out int result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt32(string s, CultureInfo culture, out object value)
            {
                if (UInt32.TryParse(s, NumberStyles.Integer, culture, out uint result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt64(string s, CultureInfo culture, out object value)
            {
                if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt64(string s, CultureInfo culture, out object value)
            {
                if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseIntPtr(string s, CultureInfo culture, out object value)
            {
                if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                {
                    value = new IntPtr(result);
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUIntPtr(string s, CultureInfo culture, out object value)
            {
                if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                {
                    value = new UIntPtr(result);
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseChar(string s, CultureInfo culture, out object value)
            {
                if (Char.TryParse(s, out char result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseSingle(string s, CultureInfo culture, out object value)
            {
                if (!Single.TryParse(s, floatStyle, culture, out float result))
                {
                    value = null;
                    return false;
                }

                if (result.Equals(0f) && s.Trim().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                    result = -0f;
                value = result;
                return true;
            }

            private static bool TryParseDouble(string s, CultureInfo culture, out object value)
            {
                if (!Double.TryParse(s, floatStyle, culture, out double result))
                {
                    value = null;
                    return false;
                }

                if (result.Equals(0d) && s.Trim().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                    result = -0d;
                value = result;
                return true;
            }

            private static bool TryParseDecimal(string s, CultureInfo culture, out object value)
            {
                if (!Decimal.TryParse(s, floatStyle, culture, out decimal result))
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }

            private static bool TryParseTimeSpan(string s, CultureInfo culture, out object value)
            {
#if NET35
                if (!TimeSpan.TryParse(s, out TimeSpan result))
#else
                if (!TimeSpan.TryParse(s, culture, out TimeSpan result))
#endif
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }

            private static bool TryParseDateTime(string s, CultureInfo culture, out object value)
            {
                DateTimeStyles style = s.EndsWith("Z", StringComparison.Ordinal) ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                if (!DateTime.TryParse(s, culture, style, out DateTime result))
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }

            private static bool TryParseDateTimeOffset(string s, CultureInfo culture, out object value)
            {
                DateTimeStyles style = s.EndsWith("Z", StringComparison.Ordinal) ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                if (!DateTimeOffset.TryParse(s, culture, style, out DateTimeOffset result))
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }

#endregion

#endregion
        }

#endregion

#endregion
    }
}
