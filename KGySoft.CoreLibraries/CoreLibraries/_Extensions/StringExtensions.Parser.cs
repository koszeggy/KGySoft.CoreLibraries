#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringExtensions.Parser.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
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
#if !NET35 || !NET40
using System.Reflection;
#endif
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
#if NETCOREAPP3_0_OR_GREATER
using System.Text;
#endif

using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    partial class StringExtensions
    {
        #region Nested classes

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
#if !NET35
                { Reflector.BigIntegerType, TryParseBigInteger },
#endif

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
                { typeof(NFloat), TryParseNFloat },
#endif
#if NET7_0_OR_GREATER
                { Reflector.Int128Type, TryParseInt128 }, 
                { Reflector.UInt128Type, TryParseUInt128 },
#endif
#if NET11_0_OR_GREATER
                { typeof(BFloat16), TryParseBFloat16 },
                { typeof(Decimal32), TryParseDecimal32 },
                { typeof(Decimal64), TryParseDecimal64 },
                { typeof(Decimal128), TryParseDecimal128 },
#endif
            };

            #endregion

            #region Methods

            #region Internal Methods

            [RequiresUnreferencedCode("Reflector.ResolveType, TypeDescriptor.GetConverter")]
            internal static bool TryParse(string? s, [DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors | DynamicallyAccessedMembers.Interfaces)]Type type,
                CultureInfo? culture, bool tryKnownTypes, out object? value, out Exception? error)
            {
                if (type == null!)
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
#if NETFRAMEWORK || NETSTANDARD2_0
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

                    if (tryKnownTypes && knownTypes.TryGetValue(type, out var tryParseMethod) && tryParseMethod.Invoke(s, culture, out value))
                        return true;

                    if (type == typeof(Type)
#if !NET35 && !NET40
                        || type == typeof(TypeInfo)
#endif
                        || type.IsRuntimeType())
                    {
                        value = Reflector.ResolveType(s, ResolveTypeOptions.AllowPartialAssemblyMatch);
                        return value != null;
                    }

                    // a registered converter from string
                    switch (typeof(string).GetConversions(type, true).FirstOrDefault())
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

                    if (TryParseDecimalAsInteger(s, type, culture, out value))
                        return true;

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

            [RequiresUnreferencedCode(parseRequiresUnreferencedCode)]
            internal static bool TryParse<[DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors | DynamicallyAccessedMembers.Interfaces)]T>(
                string? s, CultureInfo? culture, out T? value, out Exception? error)
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

                // The fast path: the JIT-ed version will contain one or zero branches of the checked types
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
                Justification = "Intended, in Release build the JIT/AOT compiler will eliminate all but exactly one branch. For nice solutions see the separated object-returning methods")]
            private static bool TryParseKnownValueType<T>(string s, CultureInfo culture, [MaybeNullWhen(false)]out T value)
            {
                Debug.Assert(typeof(T).IsValueType, "T must be a value type so the branches can be optimized away by the JIT compiler");
                // Important:
                // - Branches will be optimized away by JIT but only if we use typeof(SomeValueType) and not Reflector.XXXType
                // - In release build there will be no boxing for (T)(object)value and the JIT-ed code will be much
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

#if !NET35
                // For BigInteger, allowing float style because the fallback decimal range might not be enough
                // Unfortunately, this will allow only .0* beyond the decimal range.
                if (typeof(T) == typeof(BigInteger))
                {
                    if (BigInteger.TryParse(s, floatStyle, culture, out BigInteger result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }
#endif

                if (typeof(T) == typeof(IntPtr))
                {
#if NET5_0_OR_GREATER
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
#if NET5_0_OR_GREATER
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
                    if (Char.TryParse(s, out char result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

#if NETCOREAPP3_0_OR_GREATER
                if (typeof(T) == typeof(Rune))
                {
                    if (Rune.TryGetRuneAt(s, 0, out Rune result) && result.Utf16SequenceLength == s.Length)
                    {
                        value = (T)(object)result;
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

                if (typeof(T) == typeof(NFloat))
                {
#if NET7_0_OR_GREATER
                    if (NFloat.TryParse(s, floatStyle, culture, out NFloat result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
#else
                    if (IntPtr.Size == 4)
                    {
                        if (Single.TryParse(s, floatStyle, culture, out float result))
                        {
                            value = (T)(object)new NFloat(result);
                            return true;
                        }
                    }
                    else
                    {
                        if (Double.TryParse(s, floatStyle, culture, out double result))
                        {
                            value = (T)(object)new NFloat(result);
                            return true;
                        }
                    }
#endif
                }
#endif

#if NET7_0_OR_GREATER
#if !NET11_0_OR_GREATER
                // Allowing float style for [U]Int128 because the fallback decimal range might not be enough
                // Unfortunately, this will allow only .0* beyond the decimal range. 
                const NumberStyles style = floatStyle;
#else
                const NumberStyles style = NumberStyles.Integer;
#endif
                if (typeof(T) == typeof(Int128))
                {
                    if (Int128.TryParse(s, style, culture, out Int128 result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(UInt128))
                {
                    if (UInt128.TryParse(s, style, culture, out UInt128 result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }
#endif

#if NET11_0_OR_GREATER
                if (typeof(T) == typeof(BFloat16))
                {
                    if (BFloat16.TryParse(s, floatStyle, culture, out BFloat16 result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(Decimal32))
                {
                    if (Decimal32.TryParse(s, floatStyle, culture, out Decimal32 result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(Decimal64))
                {
                    if (Decimal64.TryParse(s, floatStyle, culture, out Decimal64 result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }

                if (typeof(T) == typeof(Decimal128))
                {
                    if (Decimal128.TryParse(s, floatStyle, culture, out Decimal128 result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }
#endif

                value = default;
                return false;
            }

            private static bool TryParseDecimalAsInteger(string s, Type type, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                // Parsing as an integer type but regular TryParse has been failed: trying also as a decimal and rounding the result if possible.
                // This is needed to be compatible with general IConvertible behavior, which allows converting fractional numbers
                TypeCode typeCode = Type.GetTypeCode(type);
                if ((typeCode is >= TypeCode.SByte and <= TypeCode.UInt64 || type.In(Reflector.IntPtrType, Reflector.UIntPtrType
#if !NET35 // .NET 11: there is no Decimal128 -> BigInteger conversion, so trying as simple decimal first TODO: && !NET12_0_OR_GREATER
#if NET12_0_OR_GREATER
#error Check if Decimal128 can already be directly converted to BigInteger.
#endif
                    , Reflector.BigIntegerType
#endif
#if NET7_0_OR_GREATER && !NET11_0_OR_GREATER
                    , Reflector.Int128Type, Reflector.UInt128Type
#endif 
                    )) && Decimal.TryParse(s, floatStyle, culture, out decimal result))
                {
                    result = Math.Round(result);

#if !NET35 && !NET12_0_OR_GREATER // TODO: adjust NET12 if it still has no Decimal128 -> BigInteger conversion
                    if (type == Reflector.BigIntegerType)
                    {
                        value = new BigInteger(result);
                        return true;
                    }
#endif

#if NET7_0_OR_GREATER && !NET11_0_OR_GREATER
                    if (type == Reflector.Int128Type)
                    {
                        value = (Int128)result;
                        return true;
                    }

                    if (type == Reflector.UInt128Type)
                    {
                        if (result < 0m)
                        {
                            value = null;
                            return false;
                        }

                        value = (UInt128)result;
                        return true;
                    }
#endif 

                    var rangeInfo = RangeInfo.GetRangeInfo(type);
                    if (result >= rangeInfo.MinValue && result <= rangeInfo.MaxValue)
                    {
                        value = typeCode != TypeCode.Object ? Convert.ChangeType(result, typeCode, culture)
                            : type == Reflector.IntPtrType ? new IntPtr((long)result)
                            : new UIntPtr((ulong)result);
                        return true;
                    }
                }

#if NET11_0_OR_GREATER
                // In .NET11+ we try to use Decimal128 for large integer types
                if (type.In(typeof(BigInteger), typeof(Int128), typeof(UInt128)) && Decimal128.TryParse(s, floatStyle, culture, out Decimal128 decimalValue))
                {
                    decimalValue = Decimal128.Round(decimalValue);
                    if (type == typeof(Int128))
                    {
                        if (decimalValue < (Decimal128)Int128.MinValue || decimalValue > (Decimal128)Int128.MaxValue)
                        {
                            value = null;
                            return false;
                        }

                        value = (Int128)decimalValue;
                        return true;
                    }

                    if (type == typeof(UInt128))
                    {
                        if (decimalValue < Decimal128.Zero || decimalValue > (Decimal128)UInt128.MaxValue)
                        {
                            value = null;
                            return false;
                        }

                        value = (UInt128)decimalValue;
                        return true;
                    }

                    Debug.Assert(type == typeof(BigInteger));
#if NET12_0_OR_GREATER
                    value = (BigInteger)decimalValue; // BigInteger.CreateTruncating(decimalValue); - NOTE: compiles fine, can fail at runtime
                    // or: Decimal128.ConvertToInteger<BigInteger>(decimalValue)  NOTE: compiles fine, can fail at runtime
#else
                    // NOTE: .NET 11 does not support converting Decimal128 to BigInteger, so using string as an intermediate step.
                    // This time using invariant culture for the Decimal128 -> string -> BigInteger conversion.
                    value = BigInteger.Parse(decimalValue.ToString("R", CultureInfo.InvariantCulture), floatStyle, CultureInfo.InvariantCulture);
#endif
                }
#endif

                value = null;
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
                if (Int32.TryParse(s, floatStyle, culture, out int result))
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

#if !NET35
            private static bool TryParseBigInteger(string s, CultureInfo culture, [MaybeNullWhen(false)] out object value)
            {
                // For BigInteger, allowing float style, because the fallback decimal range might not be enough.
                // Unfortunately, this will allow only .0* beyond the decimal range, but allows huge integers in exponential format without the fallback path.
                if (BigInteger.TryParse(s, floatStyle, culture, out BigInteger result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }
#endif

            private static bool TryParseIntPtr(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
#if NET5_0_OR_GREATER
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

            private static bool TryParseUIntPtr(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
#if NET5_0_OR_GREATER
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

#if NETCOREAPP3_0_OR_GREATER
            private static bool TryParseRune(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Rune.TryGetRuneAt(s, 0, out Rune rune) && rune.Utf16SequenceLength == s.Length)
                {
                    value = rune;
                    return true;
                }

                value = null;
                return false;
            }
#endif

            private static bool TryParseSingle(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
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

            private static bool TryParseDouble(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
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

#if NET5_0_OR_GREATER
            private static bool TryParseHalf(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
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

            private static bool TryParseDateTimeOffset(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
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
            private static bool TryParseDateOnly(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
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

            private static bool TryParseTimeOnly(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (TimeOnly.TryParse(s, culture, DateTimeStyles.AllowWhiteSpaces, out TimeOnly result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseNFloat(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
#if NET7_0_OR_GREATER
                if (NFloat.TryParse(s, floatStyle, culture, out NFloat result))
                {
                    value = result;
                    return true;
                }
#else
                if (IntPtr.Size == 4)
                {
                    if (Single.TryParse(s, floatStyle, culture, out float result))
                    {
                        value = new NFloat(result);
                        return true;
                    }
                }
                else
                {
                    if (Double.TryParse(s, floatStyle, culture, out double result))
                    {
                        value = new NFloat(result);
                        return true;
                    }
                }
#endif

                value = null;
                return false;
            }

#endif

#if NET7_0_OR_GREATER
            private static bool TryParseInt128(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                // Allowing float style for Int128 because the fallback decimal range might not be enough
                // Unfortunately, this will allow only .0* beyond the decimal range.
                if (Int128.TryParse(s, floatStyle, culture, out Int128 result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseUInt128(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                // Allowing float style for UInt128 because the fallback decimal range might not be enough
                // Unfortunately, this will allow only .0* beyond the decimal range.
                if (UInt128.TryParse(s, floatStyle, culture, out UInt128 result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }
#endif

#if NET11_0_OR_GREATER
            private static bool TryParseBFloat16(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (BFloat16.TryParse(s, floatStyle, culture, out BFloat16 result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseDecimal32(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Decimal32.TryParse(s, floatStyle, culture, out Decimal32 result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseDecimal64(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Decimal64.TryParse(s, floatStyle, culture, out Decimal64 result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryParseDecimal128(string s, CultureInfo culture, [MaybeNullWhen(false)]out object value)
            {
                if (Decimal128.TryParse(s, floatStyle, culture, out Decimal128 result))
                {
                    value = result;
                    return true;
                }

                value = null;
                return false;
            }
#endif

            private static bool TryParseHexDigit(char c, out int value)
            {
                if (c is >= '0' and <= '9')
                {
                    value = c - '0';
                    return true;
                }

                const int shiftUpper = 'A' - 10;
                if (c is >= 'A' and <= 'F')
                {
                    value = c - shiftUpper;
                    return true;
                }

                const int shiftLower = 'a' - 10;
                if (c is >= 'a' and <= 'f')
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
    }
}
