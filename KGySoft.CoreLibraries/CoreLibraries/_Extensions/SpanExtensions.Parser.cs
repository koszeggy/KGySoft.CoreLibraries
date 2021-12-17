#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SpanExtensions.Parser.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
#if !NET35
using System.Numerics;
#endif
#if NETCOREAPP3_0_OR_GREATER
using System.Text;
#endif

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
                { Reflector.BigIntegerType, TryParseBigInteger },

                { Reflector.IntPtrType, TryParseIntPtr },
                { Reflector.UIntPtrType, TryParseUIntPtr },

                { Reflector.CharType, TryParseChar },
#if NETCOREAPP3_0_OR_GREATER
                { Reflector.RuneType, TryParseRune }, 
#endif

                { Reflector.FloatType, TryParseSingle },
                { Reflector.DoubleType, TryParseDouble },
                { Reflector.DecimalType, TryParseDecimal },
#if NET5_0_OR_GREATER
                { Reflector.HalfType, TryParseHalf }, 
#endif

                { Reflector.TimeSpanType, TryParseTimeSpan },
                { Reflector.DateTimeType, TryParseDateTime },
                { Reflector.DateTimeOffsetType, TryParseDateTimeOffset },
#if NET6_0_OR_GREATER
                { Reflector.DateOnlyType, TryParseDateOnly },
                { Reflector.TimeOnlyType, TryParseTimeOnly }, 
#endif
            };

            #endregion

            #region Methods

            #region Internal Methods

            internal static bool TryParse(ReadOnlySpan<char> s, Type type, CultureInfo? culture, bool tryKnownTypes, bool safeMode, out object? value, out Exception? error)
            {
                if (type == null!)
                    Throw.ArgumentNullException(Argument.type);

                error = null;
                value = null;
                if (s == null! && type.CanAcceptValue(null))
                    return true;

                type = Nullable.GetUnderlyingType(type) ?? type;
                if (type.IsByRef)
                    type = type.GetElementType()!;
                culture ??= CultureInfo.InvariantCulture;

                try
                {
                    // ReSharper disable once PossibleNullReferenceException
                    if (type.IsEnum)
                    {
#if NET6_0_OR_GREATER
                        return Enum.TryParse(type, s, out value);
#else
                        return Enum.TryParse(type, s.ToString(), out value);
#endif
                    }

                    if (type.IsAssignableFrom(Reflector.StringType))
                    {
                        value = s.ToString();
                        return true;
                    }

                    if (tryKnownTypes && knownTypes.TryGetValue(type, out var tryParseMethod) && tryParseMethod.Invoke(s, culture, out value))
                        return true;

                    if (type.In(Reflector.Type, Reflector.RuntimeType
#if !NET35 && !NET40
                        , Reflector.TypeInfo
#endif
                    ))
                    {
                        var options = ResolveTypeOptions.AllowPartialAssemblyMatch;
                        if (!safeMode)
                            options |= ResolveTypeOptions.TryToLoadAssemblies;
                        value = Reflector.ResolveType(s.ToString(), options);
                        return value != null;
                    }

                    // a registered converter from string - in this case there will be a string allocation
                    switch (Reflector.StringType.GetConversions(type, true).FirstOrDefault())
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

                    if (TryParseDecimalAsInteger(s, type, culture, out value))
                        return true;

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
                if (!TryParse(s, type, culture, false, false, out object? result, out error) || !type.CanAcceptValue(result))
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
                Justification = "Intended, in Release JIT compiler will eliminate all but exactly one branch. For nice solutions see the separated object-returning methods")]
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

                // Only for BigInteger, allowing float style because the fallback decimal range might not be enough
                // Unfortunately, this will allow only .0* beyond the decimal range.
                if (typeof(T) == typeof(BigInteger))
                {
                    if (BigInteger.TryParse(s, floatStyle, culture, out BigInteger result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(IntPtr))
                {
#if NET6_0_OR_GREATER
                    if (IntPtr.TryParse(s, NumberStyles.Integer, culture, out IntPtr result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
#else
                    if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                    {
                        value = (T)(object)new IntPtr(result);
                        return true;
                    }
#endif
                }

                if (typeof(T) == typeof(UIntPtr))
                {
#if NET6_0_OR_GREATER
                    if (UIntPtr.TryParse(s, NumberStyles.Integer, culture, out UIntPtr result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
#else
                    if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                    {
                        value = (T)(object)new UIntPtr(result);
                        return true;
                    }
#endif
                }

                if (typeof(T) == typeof(char))
                {
                    if (s.Length == 1)
                    {
                        value = (T)(object)s[0];
                        return true;
                    }
                }

#if NETCOREAPP3_0_OR_GREATER
                if (typeof(T) == typeof(Rune))
                {
                    // Issue: there is no Rune.TryParse or Rune.TryGetRuneAt overload with span
                    if (s.Length == 1 && Rune.TryCreate(s[0], out Rune rune)
                        || s.Length == 2 && Rune.TryCreate(s[0], s[1], out rune))
                    {
                        value = (T)(object)rune;
                        return true;
                    }

                }
#endif

                if (typeof(T) == typeof(float))
                {
                    if (Single.TryParse(s, floatStyle, culture, out float result))
                    {
#if !NETCOREAPP3_0_OR_GREATER
                        if (result.Equals(0f) && s.TrimStart().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                            result = FloatExtensions.NegativeZero;
#endif
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(double))
                {
                    if (Double.TryParse(s, floatStyle, culture, out double result))
                    {
#if !NETCOREAPP3_0_OR_GREATER
                        if (result.Equals(0d) && s.TrimStart().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                            result = DoubleExtensions.NegativeZero;
#endif
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

#if NET5_0_OR_GREATER
                if (typeof(T) == typeof(Half))
                {
                    if (Half.TryParse(s, floatStyle, culture, out Half result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }
#endif

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
                    if (s.Length > 0)
                    {
                        if (DateTime.TryParse(s, culture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out DateTime result))
                        {
                            value = (T)(object)result;
                            return true;
                        }
                    }
                }

                if (typeof(T) == typeof(DateTimeOffset))
                {
                    if (s.Length > 0)
                    {
                        if (DateTimeOffset.TryParse(s, culture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset result))
                        {
                            value = (T)(object)result;
                            return true;
                        }
                    }
                }

#if NET6_0_OR_GREATER
                if (typeof(T) == typeof(DateOnly))
                {
                    if (s.Length > 0)
                    {
                        // Parsing as DateTime so allowing possible timezone information, too
                        if (DateTime.TryParse(s, culture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out DateTime result))
                        {
                            value = (T)(object)DateOnly.FromDateTime(result);
                            return true;
                        }
                    }
                }

                if (typeof(T) == typeof(TimeOnly))
                {
                    if (s.Length > 0)
                    {
                        if (TimeOnly.TryParse(s, culture, DateTimeStyles.AllowWhiteSpaces, out TimeOnly result))
                        {
                            value = (T)(object)result;
                            return true;
                        }
                    }
                }
#endif

                value = default;
                return false;
            }

            private static bool TryParseDecimalAsInteger(ReadOnlySpan<char> s, Type type, CultureInfo culture, [MaybeNullWhen(false)] out object value)
            {
                // Parsing as an integer type but regular TryParse has been failed: trying also as a decimal and rounding the result if possible.
                // This is needed to be compatible with general IConvertible behavior, which allows converting fractional numbers
                TypeCode typeCode = Type.GetTypeCode(type);
                if ((typeCode is >= TypeCode.SByte and <= TypeCode.UInt64 || type.In(Reflector.IntPtrType, Reflector.UIntPtrType, Reflector.BigIntegerType))
                    && Decimal.TryParse(s, floatStyle, culture, out decimal result))
                {
                    result = Math.Round(result);
                    if (type == Reflector.BigIntegerType)
                    {
                        value = new BigInteger(result);
                        return true;
                    }

                    var rangeInfo = RangeInfo.GetRangeInfo(type);
                    if (result >= rangeInfo.MinValue && result <= rangeInfo.MaxValue)
                    {
                        value = typeCode != TypeCode.Object ? Convert.ChangeType(result, typeCode, culture)
                            : type == Reflector.IntPtrType ? new IntPtr((long)result)
                            : new UIntPtr((ulong)result);
                        return true;
                    }
                }

                value = null;
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

            private static bool TryParseBigInteger(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)] out object value)
            {
                // Only for BigInteger, allowing float style because the fallback decimal range might not be enough
                // Unfortunately, this will allow only .0* beyond the decimal range.
                if (BigInteger.TryParse(s, floatStyle, culture, out BigInteger result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseIntPtr(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
#if NET6_0_OR_GREATER
                if (IntPtr.TryParse(s, NumberStyles.Integer, culture, out IntPtr result))
                {
                    value = result;
                    return true;
                }
#else
                if (Int64.TryParse(s, NumberStyles.Integer, culture, out long result))
                {
                    value = new IntPtr(result);
                    return true;
                }
#endif

                value = null;
                return false;
            }

            private static bool TryParseUIntPtr(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
#if NET6_0_OR_GREATER
                if (UIntPtr.TryParse(s, NumberStyles.Integer, culture, out UIntPtr result))
                {
                    value = result;
                    return true;
                }
#else
                if (UInt64.TryParse(s, NumberStyles.Integer, culture, out ulong result))
                {
                    value = new UIntPtr(result);
                    return true;
                }
#endif

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

#if NETCOREAPP3_0_OR_GREATER
            private static bool TryParseRune(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)] out object value)
            {
                // Issue: there is no Rune.TryParse or Rune.TryGetRuneAt overload with span
                if (s.Length == 1 && Rune.TryCreate(s[0], out Rune rune)
                    || s.Length == 2 && Rune.TryCreate(s[0], s[1], out rune))
                {
                    value = rune;
                    return true;
                }

                value = null;
                return false;
            }
#endif

            private static bool TryParseSingle(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (!Single.TryParse(s, floatStyle, culture, out float result))
                {
                    value = null;
                    return false;
                }

#if !NETCOREAPP3_0_OR_GREATER
                if (result.Equals(0f) && s.TrimStart().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                    result = FloatExtensions.NegativeZero;
#endif
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

#if !NETCOREAPP3_0_OR_GREATER
                if (result.Equals(0d) && s.TrimStart().StartsWith(culture.NumberFormat.NegativeSign, StringComparison.Ordinal))
                    result = DoubleExtensions.NegativeZero;
#endif
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

#if NET5_0_OR_GREATER
            private static bool TryParseHalf(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)] out object value)
            {
                if (!Half.TryParse(s, floatStyle, culture, out Half result))
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }
#endif

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
                if (s.Length > 0)
                {
                    if (DateTime.TryParse(s, culture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out DateTime result))
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
                if (s.Length > 0)
                {
                    if (DateTimeOffset.TryParse(s, culture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset result))
                    {
                        value = result;
                        return true;
                    }
                }

                value = null;
                return false;
            }

#if NET6_0_OR_GREATER
            private static bool TryParseDateOnly(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)] out object value)
            {
                if (s.Length > 0)
                {
                    // Parsing as DateTime so allowing possible timezone information, too
                    if (DateTime.TryParse(s, culture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out DateTime result))
                    {
                        value = DateOnly.FromDateTime(result);
                        return true;
                    }
                }

                value = null;
                return false;
            }

            private static bool TryParseTimeOnly(ReadOnlySpan<char> s, CultureInfo culture, [MaybeNullWhen(false)] out object value)
            {
                if (!TimeOnly.TryParse(s, culture, DateTimeStyles.AllowWhiteSpaces, out TimeOnly result))
                {
                    value = null;
                    return false;
                }

                value = result;
                return true;
            }
#endif

            #endregion

            #endregion
        }

        #endregion

        #endregion
    }
}

#endif