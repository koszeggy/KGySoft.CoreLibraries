#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
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
    partial class SpanExtensions
    {
        #region Nested classes

        #region Parser class

        /// <summary>
        /// A separate class so knowing all the supported types do not impact the <see cref="StringExtensions"/> class.
        /// </summary>
        private static class Parser
        {
            #region Delegates

            private delegate bool ParseDelegate(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value);

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

            internal static bool TryParse(ReadOnlySpan<char> s, Type type, CultureInfo? culture, bool tryKnownTypes, out object? value, out Exception? error)
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
#if NETSTANDARD2_1 || NETCOREAPP3_0 || NET5_0
                        return Enum.TryParse(type, s.ToString(), out value);
#else
                        // as of 06/2020 there is no such overload yet but we hope it for the future...
                        return Enum.TryParse(type, s, out value);
#endif
                    }

                    if (type.IsAssignableFrom(Reflector.StringType))
                    {
                        value = s.ToString();
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
                        value = Reflector.ResolveType(s.ToString());
                        return value != null;
                    }

                    // a registered converter from string - in this case there will be a string allocation
                    switch (Reflector.StringType.GetConversions(type, true).ElementAtOrDefault(0))
                    {
                        case ConversionAttempt conversionAttempt:
                            if (conversionAttempt.Invoke(s.ToString(), type, culture, out value) && type.CanAcceptValue(value))
                                return true;
                            break;
                        case Conversion conversion:
                            value = conversion.Invoke(s.ToString(), type, culture);
                            if (type.CanAcceptValue(value))
                                return true;
                            break;
                    }

                    // Trying type converter as a fallback - in this case there will be a string allocation
                    TypeConverter converter = TypeDescriptor.GetConverter(type);
                    if (converter.CanConvertFrom(Reflector.StringType))
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute - false alarm, context can be null
                        value = converter.ConvertFrom(null, culture, s.ToString());
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

            internal static bool TryParse<T>(ReadOnlySpan<char> s, CultureInfo? culture, [MaybeNull]out T value, out Exception? error)
            {
                error = null;
                culture ??= CultureInfo.InvariantCulture;
                Type type = typeof(T);

                // The fast path: the JITted version will contain one or zero branches of the checked types
                if (type.IsValueType && TryParseKnownValueType(s, culture, out value))
                    return true;

                // The slow path: for value types boxing will occur
                if (!TryParse(s, type, culture, false, out object? result, out error) || !type.CanAcceptValue(result))
                {
                    value = default;
                    return false;
                }

                value = (T)result!;
                return true;
            }

            #endregion

            #region Private Methods

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
                Justification = "Intended, in Release JIT compliler will eliminate all but exactly one branch. For nice solutions see the separated object-returning methods")]
            private static bool TryParseKnownValueType<T>(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out T value)
            {
                Debug.Assert(typeof(T).IsValueType, "T must be a value type so the branches can be optimized away by the JIT compiler");
                // Important:
                // - Branches will be optimized away by JIT but only if we use typeof(SomeValueType) and not Reflector.XXXType
                // - In release build there will be no boxing for (T)(object)value and the JITted code will be much
                //   simpler compared to the usually more elegant pattern matching

                if (typeof(T) == typeof(bool))
                {
                    s = s.Trim();
                    if (s.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                    {
                        value = (T)(object)false;
                        return true;
                    }

                    if (s.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                    {
                        value = (T)(object)true;
                        return true;
                    }

                    // allowing also an integer, which will be true for nonzero value
                    if (s.TryParseIntQuick(true, Int64.MaxValue, out ulong result))
                    {
                        value = (T)(object)(result != 0L);
                        return true;
                    }
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
                    if (s.Length == 1)
                    {
                        value = (T)(object)s[0];
                        return true;
                    }
                }

                if (typeof(T) == typeof(float))
                {
                    if (Single.TryParse(s, floatStyle, culture, out float result))
                    {
                        if (result.Equals(0f) && s.TrimStart().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                            result = FloatExtensions.NegativeZero;
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(double))
                {
                    if (Double.TryParse(s, floatStyle, culture, out double result))
                    {
                        if (result.Equals(0d) && s.TrimStart().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
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
                    if (TimeSpan.TryParse(s, culture, out TimeSpan result))
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
                        DateTimeStyles style = s[^1] == 'Z' ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
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
                        DateTimeStyles style = s[^1] == 'Z' ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
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

            private static bool TryParseBoolean(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                s = s.Trim();
                if (s.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }

                if (s.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                // allowing also an integer, which will be true for nonzero value
                if (s.TryParseIntQuick(true, Int64.MaxValue, out ulong result))
                {
                    value = result != 0L;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseByte(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Byte.TryParse(s, NumberStyles.Integer, culture, out byte result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseSByte(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (SByte.TryParse(s, NumberStyles.Integer, culture, out sbyte result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt16(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Int16.TryParse(s, NumberStyles.Integer, culture, out short result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt16(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (UInt16.TryParse(s, NumberStyles.Integer, culture, out ushort result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt32(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Int32.TryParse(s, NumberStyles.Integer, culture, out int result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt32(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (UInt32.TryParse(s, NumberStyles.Integer, culture, out uint result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseInt64(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt64(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseIntPtr(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                {
                    value = new IntPtr(result);
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUIntPtr(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                {
                    value = new UIntPtr(result);
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseChar(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (s.Length == 1)
                {
                    value = s[0];
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseSingle(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
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

            private static bool TryParseDouble(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
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

            private static bool TryParseDecimal(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (!Decimal.TryParse(s, floatStyle, culture, out decimal result))
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }

            private static bool TryParseTimeSpan(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (!TimeSpan.TryParse(s, culture, out TimeSpan result))
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }

            private static bool TryParseDateTime(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                s = s.TrimEnd();
                if (s.Length > 0)
                {
                    DateTimeStyles style = s[^1] == 'Z' ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                    if (DateTime.TryParse(s, culture, style, out DateTime result))
                    {
                        value = result;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            private static bool TryParseDateTimeOffset(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                s = s.TrimEnd();
                if (s.Length > 0)
                {
                    DateTimeStyles style = s[^1] == 'Z' ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
                    if (DateTimeOffset.TryParse(s, culture, style, out DateTimeOffset result))
                    {
                        value = result;
                        return true;
                    }
                }

                value = null;
                return false;
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion
    }
}

#endif