#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegment.Lookup.cs
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
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    partial struct StringSegment
    {
        #region Methods

        #region Public Methods

        #region IndexOf

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using ordinal comparison.
        /// </summary>
        /// <param name="value">The string to seek.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that string is found, or -1 if it is not.
        /// If value is <see cref="String.Empty"/>, the return value is 0.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(string value)
        {
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);
            return IsNull ? -1 : IndexOfInternal(value, 0, length);
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/>, <paramref name="count"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The string to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that string is found, or -1 if it is not.
        /// If value is <see cref="String.Empty"/>, the return value is <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(string value, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)startIndex + count > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            CheckComparison(comparison);

            if (length == 0)
                return IsNull || value.Length > 0 ? -1 : 0;

            if (comparison == StringComparison.Ordinal)
                return IndexOfInternal(value, startIndex, count);

            int result = str!.IndexOf(value, offset + startIndex, count, comparison);
            return result >= 0 ? result - offset : -1;
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The string to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that string is found, or -1 if it is not.
        /// If value is <see cref="String.Empty"/>, the return value is <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(string value, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => IndexOf(value, startIndex, length - startIndex, comparison);

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The string to seek.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that string is found, or -1 if it is not.
        /// If value is <see cref="String.Empty"/>, the return value is 0.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(string value, StringComparison comparison)
            => comparison == StringComparison.Ordinal ? IndexOf(value) : IndexOf(value, 0, length, comparison);

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using ordinal comparison.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to seek.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="StringSegment"/> is found, or -1 if it is not.
        /// If value is <see cref="Empty"/>, the return value is 0.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(StringSegment value)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            return IsNull ? -1 : IndexOfInternal(value, 0, length);
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/>, <paramref name="count"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="StringSegment"/> is found, or -1 if it is not.
        /// If value is <see cref="Empty"/>, the return value is <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(StringSegment value, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)startIndex + count > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            CheckComparison(comparison);

            if (length == 0)
                return IsNull || value.length > 0 ? -1 : 0;

            if (comparison == StringComparison.Ordinal)
                return IndexOfInternal(value, startIndex, count);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            int result = str.AsSpan(offset + startIndex, count).IndexOf(value, comparison);
            return result >= 0 ? result + startIndex : -1;
#else
            int result = str!.IndexOf(value.ToString()!, offset + startIndex, count, comparison);
            return result >= 0 ? result - offset : -1;
#endif
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="StringSegment"/> is found, or -1 if it is not.
        /// If value is <see cref="Empty"/>, the return value is <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(StringSegment value, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => IndexOf(value, startIndex, length - startIndex, comparison);

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to seek.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="StringSegment"/> is found, or -1 if it is not.
        /// If value is <see cref="Empty"/>, the return value is 0.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(StringSegment value, StringComparison comparison)
            => comparison == StringComparison.Ordinal ? IndexOf(value) : IndexOf(value, 0, length, comparison);

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/> and <paramref name="count"/> values.
        /// </summary>
        /// <param name="value">The character to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that character is found, or -1 if it is not.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(char value, int startIndex, int count)
        {
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)startIndex + count > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            return IndexOfInternal(value, startIndex, count);
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The character to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that character is found, or -1 if it is not.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(char value, int startIndex)
            => IndexOf(value, startIndex, length - startIndex);

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="value">The character to seek.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that character is found, or -1 if it is not.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(char value) => IndexOfInternal(value, 0, length);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using ordinal comparison.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to seek.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> is found, or -1 if it is not.
        /// If value is <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see>, the return value is 0.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(ReadOnlySpan<char> value) => IsNull ? -1 : IndexOfInternal(value, 0, length);

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/>, <paramref name="count"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> is found, or -1 if it is not.
        /// If value is <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see>, the return value is <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(ReadOnlySpan<char> value, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)startIndex + count > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            CheckComparison(comparison);

            if (length == 0)
                return IsNull || !value.IsEmpty ? -1 : 0;

            if (comparison == StringComparison.Ordinal)
                return IndexOfInternal(value, startIndex, count);

            int result = str.AsSpan(offset + startIndex, count).IndexOf(value, comparison);
            return result >= 0 ? result + startIndex : -1;
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> is found, or -1 if it is not.
        /// If value is <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see>, the return value is <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(ReadOnlySpan<char> value, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => IndexOf(value, startIndex, length - startIndex, comparison);

        /// <summary>
        /// Gets the zero-based index of the first occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to seek.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> is found, or -1 if it is not.
        /// If value is <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see>, the return value is 0.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(ReadOnlySpan<char> value, StringComparison comparison)
            => comparison == StringComparison.Ordinal ? IndexOf(value) : IndexOf(value, 0, length, comparison);
#endif

        #endregion

        #region LastIndexOf

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/>, <paramref name="count"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The string to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that string is found, or -1 if it is not.
        /// If value is <see cref="String.Empty"/>, the return value is the smaller of <paramref name="startIndex"/> and the last index position of this <see cref="StringSegment"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(string value, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            CheckComparison(comparison);

            if (length == 0)
                return IsNull || value.Length > 0 ? -1 : 0;

            int result = str!.LastIndexOf(value, offset + startIndex + count - 1, count, comparison);
            return result >= 0 ? result - offset : -1;
        }

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The string to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that string is found, or -1 if it is not.
        /// If value is <see cref="String.Empty"/>, the return value is the smaller of <paramref name="startIndex"/> and the last index position of this <see cref="StringSegment"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(string value, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(value, startIndex, length - startIndex, comparison);

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The string to seek.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that string is found, or -1 if it is not.
        /// If value is <see cref="String.Empty"/>, the return value is the last index position of this <see cref="StringSegment"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(string value, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(value, 0, length, comparison);

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/>, <paramref name="count"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="StringSegment"/> is found, or -1 if it is not.
        /// If value is <see cref="Empty"/>, the return value is the smaller of <paramref name="startIndex"/> and the last index position of this <see cref="StringSegment"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(StringSegment value, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            CheckComparison(comparison);

            if (length == 0)
                return IsNull || value.length > 0 ? -1 : 0;

#if NETCOREAPP3_0_OR_GREATER
            int result = str.AsSpan(offset + startIndex, count).LastIndexOf(value, comparison);
            return result >= 0 ? result + startIndex : -1;
#else
            int result = str!.LastIndexOf(value.ToString()!, offset + startIndex + count - 1, count, comparison);
            return result >= 0 ? result - offset : -1;
#endif
        }

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="StringSegment"/> is found, or -1 if it is not.
        /// If value is <see cref="Empty"/>, the return value is the smaller of <paramref name="startIndex"/> and the last index position of this <see cref="StringSegment"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(StringSegment value, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(value, startIndex, length - startIndex, comparison);

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to seek.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="StringSegment"/> is found, or -1 if it is not.
        /// If value is <see cref="Empty"/>, the return value is the last index position of this <see cref="StringSegment"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(StringSegment value, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(value, 0, length, comparison);

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/> and <paramref name="count"/> values.
        /// </summary>
        /// <param name="value">The character to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that character is found, or -1 if it is not.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(char value, int startIndex, int count)
        {
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)startIndex + count > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (length == 0)
                return -1;
            int result = str!.LastIndexOf(value, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="value">The character to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that character is found, or -1 if it is not.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(char value, int startIndex)
            => LastIndexOf(value, startIndex, length - startIndex);

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="value">The character to seek.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that character is found, or -1 if it is not.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(char value)
        {
            if (length == 0)
                return -1;
            int result = str!.LastIndexOf(value, offset, length);
            return result >= 0 ? result - offset : -1;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/>, <paramref name="count"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> is found, or -1 if it is not.
        /// If value is <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see>, the return value is the smaller of <paramref name="startIndex"/> and the last index position of this <see cref="StringSegment"/>.</returns>
        /// <remarks>
        /// <para>If <paramref name="comparison"/> is <see cref="StringComparison.Ordinal"/>, then no new string allocation occurs on any platforms.</para>
        /// <para>If <paramref name="comparison"/> is other than <see cref="StringComparison.Ordinal"/>, then depending on the targeted platform a new string allocation may occur.
        /// The .NET Core 3.0 and newer builds do not allocate a new string with any <paramref name="comparison"/> values.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(ReadOnlySpan<char> value, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            CheckComparison(comparison);

            if (length == 0)
                return IsNull || value.Length > 0 ? -1 : 0;

#if NETCOREAPP3_0_OR_GREATER
            int result = str.AsSpan(offset + startIndex, count).LastIndexOf(value, comparison);
            return result >= 0 ? result + startIndex : -1;
#else
            int result = comparison == StringComparison.Ordinal
                ? str.AsSpan(offset + startIndex, count).LastIndexOf(value)
                : str!.LastIndexOf(value.ToString(), offset + startIndex, count, comparison);
            return result < 0 ? -1
                : comparison == StringComparison.Ordinal ? result + startIndex
                : result - offset;
#endif
        }

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="startIndex"/> and <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> is found, or -1 if it is not.
        /// If value is <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see>, the return value is the smaller of <paramref name="startIndex"/> and the last index position of this <see cref="StringSegment"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(ReadOnlySpan<char> value, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(value, startIndex, length - startIndex, comparison);

        /// <summary>
        /// Gets the zero-based index of the last occurrence of the specified <paramref name="value"/> in this <see cref="StringSegment"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to seek.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specified the rules for the search. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>The zero-based index position of <paramref name="value"/> if that <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> is found, or -1 if it is not.
        /// If value is <see cref="ReadOnlySpan{T}.Empty"><![CDATA[ReadOnlySpan<char>.Empty]]></see>, the return value is the last index position of this <see cref="StringSegment"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(ReadOnlySpan<char> value, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(value, 0, length, comparison);
#endif

        #endregion

        #region IndexOfAny

        /// <summary>
        /// Gets the zero-based index of the first occurrence in this <see cref="StringSegment"/> of any character in the specified array
        /// using the specified <paramref name="startIndex"/> and <paramref name="count"/> values.
        /// </summary>
        /// <param name="values">The character values to search.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <returns>The zero-based index position of the first occurrence in this <see cref="StringSegment"/> where any character in the specified array was found; otherwise, -1.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOfAny(char[] values, int startIndex, int count)
        {
            if (values == null!)
                Throw.ArgumentNullException(Argument.values);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)startIndex + count > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (length == 0)
                return -1;
            return IndexOfAnyInternal(values, offset + startIndex, count);
        }

        /// <summary>
        /// Gets the zero-based index of the first occurrence in this <see cref="StringSegment"/> of any character in the specified array
        /// using the specified <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="values">The character values to search.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <returns>The zero-based index position of the first occurrence in this <see cref="StringSegment"/> where any character in the specified array was found; otherwise, -1.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOfAny(char[] values, int startIndex) => IndexOfAny(values, startIndex, length - startIndex);

        /// <summary>
        /// Gets the zero-based index of the first occurrence in this <see cref="StringSegment"/> of any character in the specified array.
        /// </summary>
        /// <param name="values">The character values to search.</param>
        /// <returns>The zero-based index position of the first occurrence in this <see cref="StringSegment"/> where any character in the specified array was found; otherwise, -1.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOfAny(params char[] values) => IndexOfAny(values, 0, length);

        #endregion

        #region LastIndexOfAny

        /// <summary>
        /// Gets the zero-based index of the last occurrence in this <see cref="StringSegment"/> of any character in the specified array
        /// using the specified <paramref name="startIndex"/> and <paramref name="count"/> values.
        /// </summary>
        /// <param name="values">The character values to search.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <param name="count">The number of character positions to examine.</param>
        /// <returns>The zero-based index position of the first occurrence in this <see cref="StringSegment"/> where any character in the specified array was found; otherwise, -1.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOfAny(char[] values, int startIndex, int count)
        {
            if (values == null!)
                Throw.ArgumentNullException(Argument.values);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)startIndex + count > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (length == 0)
                return -1;
            int result = str!.LastIndexOfAny(values, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        /// <summary>
        /// Gets the zero-based index of the last occurrence in this <see cref="StringSegment"/> of any character in the specified array
        /// using the specified <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="values">The character values to search.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <returns>The zero-based index position of the first occurrence in this <see cref="StringSegment"/> where any character in the specified array was found; otherwise, -1.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOfAny(char[] values, int startIndex) => LastIndexOfAny(values, startIndex, length - startIndex);

        /// <summary>
        /// Gets the zero-based index of the last occurrence in this <see cref="StringSegment"/> of any character in the specified array.
        /// </summary>
        /// <param name="values">The character values to search.</param>
        /// <returns>The zero-based index position of the first occurrence in this <see cref="StringSegment"/> where any character in the specified array was found; otherwise, -1.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOfAny(params char[] values) => LastIndexOfAny(values, 0, length);

        #endregion

        #region StartsWith

        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance starts with the specified <paramref name="value"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The string to compare.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specifies how to perform the comparison. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns><see langword="true"/>&#160;if this <see cref="StringSegment"/> begins with <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public bool StartsWith(string value, StringComparison comparison = StringComparison.Ordinal)
        {
            if (comparison != StringComparison.Ordinal)
                return StartsWith(new StringSegment(value), comparison);

            if (value == null!)
                Throw.ArgumentNullException(Argument.value);

            if (IsNull)
                return false;
            int len = value.Length;
            if (len > length)
                return false;
            if (len == 0)
                return true;

            return StartsWithInternal(value);
        }

        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance starts with the specified <paramref name="value"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to compare.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specifies how to perform the comparison. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns><see langword="true"/>&#160;if this <see cref="StringSegment"/> begins with <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public bool StartsWith(StringSegment value, StringComparison comparison = StringComparison.Ordinal)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            CheckComparison(comparison);
            if (IsNull)
                return false;

            int len = value.length;
            if (len > length)
                return false;
            if (len == 0)
                return true;
            return length == len
                ? Equals(this, value, comparison)
                : Equals(SubstringInternal(0, len), value, comparison);
        }

        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance starts with the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The character to compare.</param>
        /// <returns><see langword="true"/>&#160;if this <see cref="StringSegment"/> begins with <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public bool StartsWith(char value) => length > 0 && GetCharInternal(0) == value;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance starts with the specified <paramref name="value"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to compare.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specifies how to perform the comparison. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns><see langword="true"/>&#160;if this <see cref="StringSegment"/> begins with <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public bool StartsWith(ReadOnlySpan<char> value, StringComparison comparison = StringComparison.Ordinal)
        {
            CheckComparison(comparison);
            if (IsNull)
                return false;

            int len = value.Length;
            if (len > length)
                return false;
            if (len == 0)
                return true;
            return length == len
                ? value.Equals(this, comparison)
                : value.Equals(SubstringInternal(0, len), comparison);
        }
#endif

        #endregion

        #region EndsWith

        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance ends with the specified <paramref name="value"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to compare.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specifies how to perform the comparison. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns><see langword="true"/>&#160;if this <see cref="StringSegment"/> ends with <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public bool EndsWith(StringSegment value, StringComparison comparison = StringComparison.Ordinal)
        {
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.s);
            CheckComparison(comparison);
            if (IsNull)
                return false;

            int len = value.length;
            if (len > length)
                return false;
            if (len == 0)
                return true;
            return length == len
                ? Equals(this, value, comparison)
                : Equals(SubstringInternal(length - len, len), value, comparison);
        }

        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance ends with the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The character to compare.</param>
        /// <returns><see langword="true"/>&#160;if this <see cref="StringSegment"/> ends with <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public bool EndsWith(char value) => length > 0 && GetCharInternal(length - 1) == value;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance ends with the specified <paramref name="value"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to compare.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specifies how to perform the comparison. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns><see langword="true"/>&#160;if this <see cref="StringSegment"/> ends with <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        public bool EndsWith(ReadOnlySpan<char> value, StringComparison comparison = StringComparison.Ordinal)
        {
            CheckComparison(comparison);
            if (IsNull)
                return false;

            int len = value.Length;
            if (len > length)
                return false;
            if (len == 0)
                return true;
            return length == len
                ? value.Equals(this, comparison)
                : value.Equals(SubstringInternal(length - len, len), comparison);
        }
#endif

        #endregion

        #endregion

        #region Private Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private int IndexOfInternal(char c, int startIndex, int count)
        {
            if (length == 0)
                return -1;
            int result = str!.IndexOf(c, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private int IndexOfInternal(string s, int startIndex, int count)
        {
            Debug.Assert(!IsNull);
            Debug.Assert((uint)startIndex <= (uint)length && startIndex + count <= length);
            if (s.Length <= 1)
                return s.Length == 0 ? startIndex : IndexOfInternal(s[0], startIndex, count);

            int result = str!.IndexOf(s, offset + startIndex, count, StringComparison.Ordinal);
            return result >= 0 ? result - offset : -1;
        }

        private int IndexOfInternal(StringSegment s, int startIndex, int count)
        {
            Debug.Assert(!IsNull);
            Debug.Assert(!s.IsNull);
            Debug.Assert((uint)startIndex <= (uint)length && startIndex + count <= length);

            if (s.length <= 1)
                return s.length == 0 ? startIndex : IndexOfInternal(s.GetCharInternal(0), startIndex, count);
            if (s.length == s.UnderlyingString!.Length)
                return IndexOfInternal(s.UnderlyingString, startIndex, count);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            if (s.length >= count)
            {
                if (s.length != count)
                    return -1;

                // possible shortcut if s.Length == count == this.Length
                if (count == length)
                {
                    Debug.Assert(startIndex == 0);
                    return Equals(s) ? 0 : -1;
                }
            }

            char first = s.GetCharInternal(0);
            int start = offset + startIndex;

            int end = start + count - s.length + 1;
            for (int i = offset + startIndex; i < end; i++)
            {
                if (str![i] != first)
                    continue;

                // first char matches: looking for difference in other chars if any
                for (int j = 1; j < s.length; j++)
                {
                    if (str[i + j] == s.GetCharInternal(j))
                        continue;

                    // here we have a difference: continuing with skipping the matched characters
                    i += j - 1;
                    goto continueOuter; // yes, a dreadful goto which is actually a continue
                }

                // Here we have full match. As single char patterns are not handled here we could have
                // check this into the inner loop to avoid goto but that requires an extra condition.
                return i - offset;

            continueOuter:;
            }

            return -1;
#else
            int result = str.AsSpan(offset + startIndex, count).IndexOf(s.AsSpan, StringComparison.Ordinal);
            return result >= 0 ? result + startIndex : -1;
#endif
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private int IndexOfInternal(ReadOnlySpan<char> s, int startIndex, int count)
        {
            Debug.Assert(!IsNull);
            Debug.Assert((uint)startIndex <= (uint)length && startIndex + count <= length);

            int result = str.AsSpan(offset + startIndex, count).IndexOf(s);
            return result >= 0 ? result + startIndex : -1;
        }
#endif

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private int IndexOfAnyInternal(char[] values, int startIndex, int count)
        {
            Debug.Assert(length != 0);
            int result = str!.IndexOfAny(values, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        private int IndexOfAnyInternal(StringSegment[] separators, int startIndex, int count, out int separatorIndex)
        {
            Debug.Assert(!separators.IsNullOrEmpty(), "Non-empty separators are expected here");

            for (int i = startIndex; i < count; i++)
            {
                for (int j = 0; j < separators.Length; j++)
                {
                    StringSegment separator = separators[j];
                    if (separator.IsNullOrEmpty)
                        continue;

                    int sepLength = separator.length;
                    if (GetCharInternal(i) != separator.GetCharInternal(0) || sepLength > count - i)
                        continue;
                    if (sepLength == 1 || SubstringInternal(i, sepLength).Equals(separator))
                    {
                        separatorIndex = j;
                        return i;
                    }
                }
            }

            separatorIndex = -1;
            return -1;
        }

        private int IndexOfAnyInternal(string?[] separators, int startIndex, int count, out int separatorIndex)
        {
            Debug.Assert(!separators.IsNullOrEmpty(), "Non-empty separators are expected here");

            for (int i = startIndex; i < count; i++)
            {
                for (int j = 0; j < separators.Length; j++)
                {
                    string? separator = separators[j];
                    if (String.IsNullOrEmpty(separator))
                        continue;

                    int sepLength = separator!.Length;
                    if (GetCharInternal(i) != separator[0] || sepLength > count - i)
                        continue;
                    if (sepLength == 1 || SubstringInternal(i, sepLength).Equals(separator))
                    {
                        separatorIndex = j;
                        return i;
                    }
                }
            }

            separatorIndex = -1;
            return -1;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private bool StartsWithInternal(string value)
        {
            Debug.Assert(!String.IsNullOrEmpty(value) && value.Length <= length);
            if (length == value.Length)
                return Equals(value);
#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            for (int i = 0; i < value.Length; i++)
            {
                if (GetCharInternal(i) != value[i])
                    return false;
            }

            return true;
#else
            // for ordinal comparison String.Compare is faster than Span.[Sequence]Equals
            return String.Compare(str, offset, value, 0, value.Length, StringComparison.Ordinal) == 0;
#endif
        }

        #endregion

        #endregion
    }
}
