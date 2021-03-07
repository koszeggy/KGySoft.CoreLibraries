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
using System.Diagnostics.CodeAnalysis;
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

            private delegate bool ParseDelegate(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value);

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

            internal static bool TryParse(string? s, Type type, CultureInfo? culture, bool tryKnownTypes, out object? value, out Exception? error)
            {
                if (type == null)
                    Throw.ArgumentNullException(Argument.type);

                error = null;
                value = null;
                if (s == null)
                {
                    if (type.CanAcceptValue(null))
                        return true;

                    Throw.ArgumentNullException(Argument.s);
                }

                type = Nullable.GetUnderlyingType(type) ?? type;
                if (type.IsByRef)
                    type = type.GetElementType()!;
                culture ??= CultureInfo.InvariantCulture;

                try
                {
                    // ReSharper disable once PossibleNullReferenceException
                    if (type.IsEnum)
                    {
#if NET35 || NET40 || NET45 || NET472 || NETSTANDARD2_0
                        value = Enum.Parse(type, s);
                        return true;
#else
                        return Enum.TryParse(type, s, out value);
#endif
                    }

                    if (type.IsInstanceOfType(s))
                    {
                        value = s;
                        return true;
                    }

                    if (tryKnownTypes && knownTypes.TryGetValue(type, out var tryParseMethod))
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

            internal static bool TryParse<T>(string? s, CultureInfo? culture, out T? value, out Exception? error)
            {
                Throw.ThrowIfNullIsInvalid<T>(s, Argument.s);
                error = null;

                // if s is null here, then T can accept null
                if (s == null)
                {
                    value = default!;
                    return true;
                }

                culture ??= CultureInfo.InvariantCulture;
                Type type = typeof(T);

                // The fast path: the JITted version will contain one or zero branches of the checked types
                if (type.IsValueType && TryParseKnownValueType(s, culture, out value))
                    return true;

                if (s is T t)
                {
                    value = t;
                    return true;
                }

                // The slow path: for value types boxing will occur
                if (!TryParse(s, type, culture, false, out object? result, out error) || !type.CanAcceptValue(result))
                {
                    value = default;
                    return false;
                }

                value = (T)result!;
                return true;
            }

            internal static bool TryParseHexByte(string s, int pos, out byte value)
            {
                Debug.Assert(s.Length > pos + 1);
                if (TryParseHexDigit(s[pos], out int hi) && TryParseHexDigit(s[pos + 1], out int lo))
                {
                    value = (byte)((hi << 4) | lo);
                    return true;
                }

                value = default;
                return false;
            }

            internal static bool TryParseHexByte(StringSegmentInternal s, out byte value)
            {
                if (s.Length > 0)
                {
                    s.TrimStart('0');

                    if (s.Length == 2 && TryParseHexDigit(s[0], out int hi) && TryParseHexDigit(s[1], out int lo))
                    {
                        value = (byte)((hi << 4) | lo);
                        return true;
                    }

                    if (s.Length == 1 && TryParseHexDigit(s[0], out int result))
                    {
                        value = (byte)result;
                        return true;
                    }

                    // if now empty it means valid 0
                    if (s.Length == 0)
                    {
                        value = 0;
                        return true;
                    }
                }

                value = default;
                return false;
            }

            #endregion

            #region Private Methods

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
                Justification = "Intended, in Release JIT compiler will eliminate all but exactly one branch. For nice solutions see the separated object-returning methods")]
            private static bool TryParseKnownValueType<T>(string s, CultureInfo culture, [MaybeNullWhen(false)]out T value)
            {
                Debug.Assert(typeof(T).IsValueType, "T must be a value type so the branches can be optimized away by the JIT compiler");
                // Important:
                // - Branches will be optimized away by JIT but only if we use typeof(SomeValueType) and not Reflector.XXXType
                // - In release build there will be no boxing for (T)(object)value and the JITted code will be much
                //   simpler compared to the usually more elegant pattern matching

                if (typeof(T) == typeof(bool))
                {
                    var segment = new StringSegmentInternal(s);
                    segment.Trim();
                    if (segment.EqualsOrdinalIgnoreCase(Boolean.FalseString))
                    {
                        value = (T)(object)false;
                        return true;
                    }

                    if (segment.EqualsOrdinalIgnoreCase(Boolean.TrueString))
                    {
                        value = (T)(object)true;
                        return true;
                    }

                    // allowing also an integer, which will be true for nonzero value
                    if (segment.TryParseIntQuick(true, Int64.MaxValue, out ulong result))
                    {
                        value = (T)(object)(result != 0L);
                        return true;
                    }

                    value = default;
                    return false;
                }

                if (typeof(T) == typeof(byte))
                {
                    if (Byte.TryParse(s, NumberStyles.Integer, culture, out byte result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(sbyte))
                {
                    if (SByte.TryParse(s, NumberStyles.Integer, culture, out sbyte result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(short))
                {
                    if (Int16.TryParse(s, NumberStyles.Integer, culture, out short result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(ushort))
                {
                    if (UInt16.TryParse(s, NumberStyles.Integer, culture, out ushort result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(int))
                {
                    if (Int32.TryParse(s, NumberStyles.Integer, culture, out int result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(uint))
                {
                    if (UInt32.TryParse(s, NumberStyles.Integer, culture, out uint result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(long))
                {
                    if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(ulong))
                {
                    if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(IntPtr))
                {
                    if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                    {
                        value = (T)(object)new IntPtr(result);
                        return true;
                    }
                }

                if (typeof(T) == typeof(UIntPtr))
                {
                    if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                    {
                        value = (T)(object)new UIntPtr(result);
                        return true;
                    }
                }

                if (typeof(T) == typeof(char))
                {
                    if (Char.TryParse(s, out char result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(float))
                {
                    if (Single.TryParse(s, floatStyle, culture, out float result))
                    {
                        if (result.Equals(0f) && s.Trim().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                            result = FloatExtensions.NegativeZero;
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(double))
                {
                    if (Double.TryParse(s, floatStyle, culture, out double result))
                    {
                        if (result.Equals(0d) && s.Trim().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                            result = DoubleExtensions.NegativeZero;
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(decimal))
                {
                    if (Decimal.TryParse(s, floatStyle, culture, out decimal result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(TimeSpan))
                {
#if NET35
                    if (TimeSpan.TryParse(s, out TimeSpan result))
#else
                    if (TimeSpan.TryParse(s, culture, out TimeSpan result))
#endif
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(DateTime))
                {
                    s = s.TrimEnd();
                    if (s.Length > 0)
                    {
                        DateTimeStyles style = s[s.Length - 1] == 'Z' ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                        if (DateTime.TryParse(s, culture, style, out DateTime result))
                        {
                            value = (T)(object)result;
                            return true;
                        }
                    }
                }

                if (typeof(T) == typeof(DateTimeOffset))
                {
                    s = s.TrimEnd();
                    if (s.Length > 0)
                    {
                        DateTimeStyles style = s[s.Length - 1] == 'Z' ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                        if (DateTimeOffset.TryParse(s, culture, style, out DateTimeOffset result))
                        {
                            value = (T)(object)result;
                            return true;
                        }
                    }
                }

                value = default;
                return false;
            }

            private static bool TryParseBoolean(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                var segment = new StringSegmentInternal(s);
                segment.Trim();
                if (segment.EqualsOrdinalIgnoreCase(Boolean.FalseString))
                {
                    value = false;
                    return true;
                }

                if (segment.EqualsOrdinalIgnoreCase(Boolean.TrueString))
                {
                    value = true;
                    return true;
                }

                // allowing also an integer, which will be true for nonzero value
                if (segment.TryParseIntQuick(true, Int64.MaxValue, out ulong result))
                {
                    value = result != 0L;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseByte(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Byte.TryParse(s, NumberStyles.Integer, culture, out byte result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseSByte(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (SByte.TryParse(s, NumberStyles.Integer, culture, out sbyte result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt16(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Int16.TryParse(s, NumberStyles.Integer, culture, out short result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt16(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (UInt16.TryParse(s, NumberStyles.Integer, culture, out ushort result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt32(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Int32.TryParse(s, NumberStyles.Integer, culture, out int result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt32(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (UInt32.TryParse(s, NumberStyles.Integer, culture, out uint result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt64(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt64(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseIntPtr(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                {
                    value = new IntPtr(result);
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUIntPtr(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                {
                    value = new UIntPtr(result);
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseChar(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Char.TryParse(s, out char result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseSingle(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (!Single.TryParse(s, floatStyle, culture, out float result))
                {
                    value = null;
                    return false;
                }

                if (result.Equals(0f) && s.TrimStart().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                    result = FloatExtensions.NegativeZero;
                value = result;
                return true;
            }

            private static bool TryParseDouble(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (!Double.TryParse(s, floatStyle, culture, out double result))
                {
                    value = null;
                    return false;
                }

                if (result.Equals(0d) && s.TrimStart().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                    result = DoubleExtensions.NegativeZero;
                value = result;
                return true;
            }

            private static bool TryParseDecimal(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (!Decimal.TryParse(s, floatStyle, culture, out decimal result))
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }

            private static bool TryParseTimeSpan(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
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

            private static bool TryParseDateTime(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                s = s.TrimEnd();
                if (s.Length > 0)
                {
                    DateTimeStyles style = s[s.Length - 1] == 'Z' ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                    if (DateTime.TryParse(s, culture, style, out DateTime result))
                    {
                        value = result;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            private static bool TryParseDateTimeOffset(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                s = s.TrimEnd();
                if (s.Length > 0)
                {
                    DateTimeStyles style = s[s.Length - 1] == 'Z' ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                    if (DateTimeOffset.TryParse(s, culture, style, out DateTimeOffset result))
                    {
                        value = result;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            private static bool TryParseHexDigit(char c, out int value)
            {
                if (c >= '0' && c <= '9')
                {
                    value = c - '0';
                    return true;
                }

                const int shiftUpper = 'A' - 10;
                if (c >= 'A' && c <= 'F')
                {
                    value = c - shiftUpper;
                    return true;
                }

                const int shiftLower = 'a' - 10;
                if (c >= 'a' && c <= 'f')
                {
                    value = c - shiftLower;
                    return true;
                }

                value = default;
                return false;
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion
    }
}
