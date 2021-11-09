#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegment.Slicing.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    partial struct StringSegment
    {
        #region Methods

        #region Static Methods

        /// <summary>
        /// Reads until next whitespace.
        /// </summary>
        internal static StringSegment GetNextSegment(ref StringSegment rest)
        {
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : rest;
                rest = default;
                return result;
            }

            int pos = -1;
            int end = rest.offset + rest.length;
            for (int i = rest.offset; i < end; i++)
            {
                if (Char.IsWhiteSpace(rest.str![i]))
                {
                    pos = i - rest.offset;
                    break;
                }
            }

            // last segment
            if (pos == -1)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            // returning next segment and advance
            int offset = rest.offset;
            rest = rest.SubstringInternal(pos + 1);
            return new StringSegment(rest.str!, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, char separator)
        {
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : rest;
                rest = default;
                return result;
            }

            int pos = rest.IndexOf(separator);

            // last segment
            if (pos == -1)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            // returning next segment and advance
            int offset = rest.offset;
            rest = rest.SubstringInternal(pos + 1);
            return new StringSegment(rest.str!, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, char[] separators)
        {
            Debug.Assert(!separators.IsNullOrEmpty(), "Non-empty separators are expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : rest;
                rest = default;
                return result;
            }

            int pos = rest.IndexOfAnyInternal(separators, 0, rest.length);

            // last segment
            if (pos == -1)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            // returning next segment and advance
            int offset = rest.offset;
            rest = rest.SubstringInternal(pos + 1);
            return new StringSegment(rest.str!, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, StringSegment separator)
        {
            Debug.Assert(!separator.IsNullOrEmpty, "Non-empty separator is expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : rest;
                rest = default;
                return result;
            }

            int pos = rest.IndexOfInternal(separator, 0, rest.length);

            // last segment
            if (pos == -1)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            // returning next segment and advance
            int offset = rest.offset;
            rest = rest.SubstringInternal(pos + separator.length);
            return new StringSegment(rest.str!, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, string separator)
        {
            Debug.Assert(!String.IsNullOrEmpty(separator), "Non-empty separator is expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : rest;
                rest = default;
                return result;
            }

            int pos = rest.IndexOfInternal(separator, 0, rest.length);

            // last segment
            if (pos == -1)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            // returning next segment and advance
            int offset = rest.offset;
            rest = rest.SubstringInternal(pos + separator.Length);
            return new StringSegment(rest.str!, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, string?[] separators)
        {
            Debug.Assert(!separators.IsNullOrEmpty(), "Non-empty separators are expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : rest;
                rest = default;
                return result;
            }

            int pos = rest.IndexOfAnyInternal(separators, 0, rest.length, out int separatorIndex);

            // last segment
            if (pos == -1)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            // returning next segment and advance
            int offset = rest.offset;
            rest = rest.SubstringInternal(pos + separators[separatorIndex]!.Length);
            return new StringSegment(rest.str!, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, StringSegment[] separators)
        {
            Debug.Assert(!separators.IsNullOrEmpty(), "Non-empty separators are expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : rest;
                rest = default;
                return result;
            }

            int pos = rest.IndexOfAnyInternal(separators, 0, rest.length, out int separatorIndex);

            // last segment
            if (pos == -1)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            // returning next segment and advance
            int offset = rest.offset;
            rest = rest.SubstringInternal(pos + separators[separatorIndex].length);
            return new StringSegment(rest.str!, offset, pos);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, ReadOnlySpan<char> separator)
        {
            Debug.Assert(!separator.IsEmpty, "Non-empty separator is expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : rest;
                rest = default;
                return result;
            }

            int pos = rest.IndexOfInternal(separator, 0, rest.length);

            // last segment
            if (pos == -1)
            {
                StringSegment result = rest;
                rest = default;
                return result;
            }

            // returning next segment and advance
            int offset = rest.offset;
            rest = rest.SubstringInternal(pos + separator.Length);
            return new StringSegment(rest.str!, offset, pos);
        }
#endif

        #endregion

        #region Instance Methods

        #region Public Methods

        #region Trim

        /// <summary>
        /// Removes all leading and trailing white-space characters from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all white-space
        /// characters are removed from the start and end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment Trim()
        {
            if (length == 0)
                return this;

            int start = 0;
            while (start < length && Char.IsWhiteSpace(GetCharInternal(start)))
                start += 1;

            int end = length - 1;
            while (end >= start && Char.IsWhiteSpace(GetCharInternal(end)))
                end -= 1;

            return SubstringInternal(start, end - start + 1);
        }

        /// <summary>
        /// Removes all the leading white-space characters from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all white-space
        /// characters are removed from the start of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimStart()
        {
            if (length == 0)
                return this;
            int start = 0;
            while (start < length && Char.IsWhiteSpace(GetCharInternal(start)))
                start += 1;

            return SubstringInternal(start);
        }

        /// <summary>
        /// Removes all the trailing white-space characters from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all white-space
        /// characters are removed from the end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimEnd()
        {
            if (length == 0)
                return this;
            int end = length - 1;
            while (end >= 0 && Char.IsWhiteSpace(GetCharInternal(end)))
                end -= 1;

            return SubstringInternal(0, end + 1);
        }

        /// <summary>
        /// Removes all leading and trailing instances of a character from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChar">The character to remove.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all instances
        /// of the <paramref name="trimChar"/> character are removed from the start and end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment Trim(char trimChar) => TrimStart(trimChar).TrimEnd(trimChar);

        /// <summary>
        /// Removes all leading instances of a character from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChar">The character to remove.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all instances
        /// of the <paramref name="trimChar"/> character are removed from the start of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimStart(char trimChar)
        {
            if (length == 0)
                return this;
            int start = 0;
            while (start < length && GetCharInternal(start) == trimChar)
                start += 1;

            return SubstringInternal(start);
        }

        /// <summary>
        /// Removes all trailing instances of a character from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChar">The character to remove.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all instances
        /// of the <paramref name="trimChar"/> character are removed from the end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimEnd(char trimChar)
        {
            if (length == 0)
                return this;
            int end = length - 1;
            while (end >= 0 && GetCharInternal(end) == trimChar)
                end -= 1;

            return SubstringInternal(0, end + 1);
        }

        /// <summary>
        /// Removes all leading and trailing occurrences of a set of characters specified in an array from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChars">The characters to remove. If <see langword="null"/>&#160;or empty, then whitespace characters will be removed.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all occurrences of the characters
        /// in the <paramref name="trimChars"/> parameter are removed from the start and end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment Trim(params char[]? trimChars)
        {
            if (length == 0)
                return this;
            if ((trimChars?.Length ?? 0) == 0)
                return Trim();

            int start = 0;
            while (start < length && GetCharInternal(start).In(trimChars))
                start += 1;

            int end = length - 1;
            while (end >= start && GetCharInternal(end).In(trimChars))
                end -= 1;

            return SubstringInternal(start, end - start + 1);
        }

        /// <summary>
        /// Removes all leading occurrences of a set of characters specified in an array from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChars">The characters to remove. If <see langword="null"/>&#160;or empty, then whitespace characters will be removed.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all occurrences of the characters
        /// in the <paramref name="trimChars"/> parameter are removed from the start of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimStart(params char[]? trimChars)
        {
            if (length == 0)
                return this;
            if ((trimChars?.Length ?? 0) == 0)
                return TrimStart();

            int start = 0;
            while (start < length && GetCharInternal(start).In(trimChars))
                start += 1;

            return SubstringInternal(start);
        }

        /// <summary>
        /// Removes all trailing occurrences of a set of characters specified in an array from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChars">The characters to remove. If <see langword="null"/>&#160;or empty, then whitespace characters will be removed.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all occurrences of the characters
        /// in the <paramref name="trimChars"/> parameter are removed from the end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimEnd(params char[]? trimChars)
        {
            if (length == 0)
                return this;
            if ((trimChars?.Length ?? 0) == 0)
                return TrimEnd();

            int end = length - 1;
            while (end >= 0 && GetCharInternal(end).In(trimChars))
                end -= 1;

            return SubstringInternal(0, end + 1);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Removes all leading and trailing occurrences of a set of characters specified in an array from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChars">The characters to remove. If empty, then whitespace characters will be removed.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all occurrences of the characters
        /// in the <paramref name="trimChars"/> parameter are removed from the start and end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment Trim(ReadOnlySpan<char> trimChars)
        {
            if (length == 0)
                return this;
            if (trimChars.Length == 0)
                return Trim();

            int start = 0;
            while (start < length && GetCharInternal(start).In(trimChars))
                start += 1;

            int end = length - 1;
            while (end >= start && GetCharInternal(end).In(trimChars))
                end -= 1;

            return SubstringInternal(start, end - start + 1);
        }

        /// <summary>
        /// Removes all leading occurrences of a set of characters specified in an array from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChars">The characters to remove. If empty, then whitespace characters will be removed.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all occurrences of the characters
        /// in the <paramref name="trimChars"/> parameter are removed from the start of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimStart(ReadOnlySpan<char> trimChars)
        {
            if (length == 0)
                return this;
            if (trimChars.Length == 0)
                return TrimStart();

            int start = 0;
            while (start < length && GetCharInternal(start).In(trimChars))
                start += 1;

            return SubstringInternal(start);
        }

        /// <summary>
        /// Removes all trailing occurrences of a set of characters specified in an array from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="trimChars">The characters to remove. If empty, then whitespace characters will be removed.</param>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all occurrences of the characters
        /// in the <paramref name="trimChars"/> parameter are removed from the end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimEnd(ReadOnlySpan<char> trimChars)
        {
            if (length == 0)
                return this;
            if (trimChars.Length == 0)
                return TrimEnd();

            int end = length - 1;
            while (end >= 0 && GetCharInternal(end).In(trimChars))
                end -= 1;

            return SubstringInternal(0, end + 1);
        }
#endif

        #endregion

        #region Substring

        /// <summary>
        /// Gets a <see cref="StringSegment"/> instance, which represents a substring of the current instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first character of the returned segment.</param>
        /// <param name="length">The desired length of the returned segment.</param>
        /// <returns>The subsegment of the current <see cref="StringSegment"/> instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "ParameterHidesMember", Justification = "Intended because of compatibility with string and because it will be the new length of the returned instance")]
        public StringSegment Substring(int startIndex, int length)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if ((uint)startIndex > (uint)this.length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)length > (uint)this.length - startIndex)
                Throw.ArgumentOutOfRangeException(Argument.length);

            return new StringSegment(str!, offset + startIndex, length);
        }

        /// <summary>
        /// Gets a new <see cref="StringSegment"/> instance, which represents a substring of the current instance with the specified <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first character of the returned segment.</param>
        /// <returns>The subsegment of the current <see cref="StringSegment"/> instance with the specified <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public StringSegment Substring(int startIndex)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            int start = offset + startIndex;
            return new StringSegment(str!, start, length - startIndex);
        }

        #endregion

        #region Split

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// This overload uses the whitespace characters as separators. Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToWhiteSpace">ReadToWhiteSpace</see> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegmentSplitOptions.None"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by whitespace characters.</returns>
        public IList<StringSegment> Split(int? maxLength = default, StringSegmentSplitOptions options = default)
        {
            IList<StringSegment>? result = SplitCommon(maxLength, options);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);

            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them
            bool removeEmptyEntries = options.IsRemoveEmpty;

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0 && Char.IsWhiteSpace(rest[0]))
                    rest = rest.SubstringInternal(1);

                if (rest.length > 0 || !removeEmptyEntries)
                    result.Add(rest);
            }

            return result;
        }

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// This overload uses the whitespace characters as separators. Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToWhiteSpace">ReadToWhiteSpace</see> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by whitespace characters.</returns>
        public IList<StringSegment> Split(StringSegmentSplitOptions options) => Split(default(int?), options);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, char)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A character that delimits the segments in this <see cref="StringSegment"/>.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegmentSplitOptions.None"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(char separator, int? maxLength = default, StringSegmentSplitOptions options = default)
        {
            IList<StringSegment>? result = SplitCommon(maxLength, options);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them
            bool removeEmptyEntries = options.IsRemoveEmpty;
            bool trim = options.IsTrim;

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (trim && segment.length != 0)
                    segment = segment.Trim();
                if (segment.length != 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                if (trim && rest.length != 0)
                    rest = rest.Trim();

                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length != 0 && rest[0] == separator)
                    rest = rest.SubstringInternal(1);

                if (rest.length > 0 || !removeEmptyEntries)
                    result.Add(rest);
            }

            return result;
        }

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, char)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A character that delimits the segments in this <see cref="StringSegment"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(char separator, StringSegmentSplitOptions options) => Split(separator, default, options);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, char[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of characters that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegmentSplitOptions.None"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(char[]? separators, int? maxLength, StringSegmentSplitOptions options = default)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separators == null || separators.Length == 0)
                return Split(maxLength, options);
            if (separators.Length == 1)
                return Split(separators[0], maxLength, options);

            IList<StringSegment>? result = SplitCommon(maxLength, options);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them
            bool removeEmptyEntries = options.IsRemoveEmpty;
            bool trim = options.IsTrim;

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separators);
                if (trim && segment.length != 0)
                    segment = segment.Trim();
                if (segment.length != 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                if (trim && rest.length != 0)
                    rest = rest.Trim();

                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length != 0)
                {
                    for (int i = 0; i < separators.Length; i++)
                    {
                        if (rest[0] == separators[i])
                        {
                            rest = rest.SubstringInternal(1);
                            break;
                        }
                    }
                }

                if (rest.length != 0 || !removeEmptyEntries)
                    result.Add(rest);
            }

            return result;
        }

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, char[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of characters that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(char[]? separators, StringSegmentSplitOptions options) => Split(separators, default, options);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, char[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of characters that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(params char[]? separators) => Split(separators, default, StringSegmentSplitOptions.None);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, StringSegment)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A <see cref="StringSegment"/> that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or empty, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegmentSplitOptions.None"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(StringSegment separator, int? maxLength = default, StringSegmentSplitOptions options = default)
        {
            if (separator.length == 1)
                return Split(separator[0], maxLength, options);

            IList<StringSegment>? result = SplitCommon(maxLength, options);
            if (result != null)
                return result;

            // null or empty string separator: returning whole string (compatibility with String.Split)
            if (separator.length == 0)
                return new[] { this };

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them
            bool removeEmptyEntries = options.IsRemoveEmpty;
            bool trim = options.IsTrim;

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (trim && segment.length != 0)
                    segment = segment.Trim();
                if (segment.length != 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                if (trim && rest.length != 0)
                    rest = rest.Trim();

                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length >= separator.length && rest.StartsWith(separator))
                    rest = rest.SubstringInternal(separator.length);

                if (rest.length != 0 || !removeEmptyEntries)
                    result.Add(rest);
            }

            return result;
        }

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, StringSegment)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A <see cref="StringSegment"/> that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or empty, then no splitting will occur.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(StringSegment separator, StringSegmentSplitOptions options) => Split(separator, default, options);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, StringSegment[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of <see cref="StringSegment"/> instances that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegmentSplitOptions.None"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(StringSegment[]? separators, int? maxLength, StringSegmentSplitOptions options = default)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separators == null || separators.Length == 0)
                return Split(maxLength, options);
            if (separators.Length == 1)
                return Split(separators[0], maxLength, options);

            IList<StringSegment>? result = SplitCommon(maxLength, options);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them
            bool removeEmptyEntries = options.IsRemoveEmpty;
            bool trim = options.IsTrim;

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separators);
                if (trim && segment.length != 0 && segment.length != length) // trimming only if split occurred (compatibility with String.Split)
                    segment = segment.Trim();
                if (segment.length != 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                if (trim && rest.length != 0)
                    rest = rest.Trim();

                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length != 0)
                {
                    foreach (StringSegment sep in separators)
                    {
                        if (sep.length == 0 || sep.length > rest.length)
                            continue;
                        if (rest.StartsWith(sep))
                        {
                            rest = rest.SubstringInternal(sep.length);
                            break;
                        }
                    }
                }

                if (rest.length != 0 || !removeEmptyEntries)
                    result.Add(rest);
            }

            return result;
        }

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, StringSegment[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of <see cref="StringSegment"/> instances that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(StringSegment[]? separators, StringSegmentSplitOptions options) => Split(separators, default, options);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, StringSegment[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of <see cref="StringSegment"/> instances that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(params StringSegment[]? separators) => Split(separators, default, default);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, string)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A string that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or empty, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegmentSplitOptions.None"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(string? separator, int? maxLength = default, StringSegmentSplitOptions options = default)
        {
            if (separator?.Length == 1)
                return Split(separator[0], maxLength, options);

            IList<StringSegment>? result = SplitCommon(maxLength, options);
            if (result != null)
                return result;

            // null or empty string separator: returning whole string (compatibility with String.Split)
            if (String.IsNullOrEmpty(separator))
                return new[] { this };

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them
            bool removeEmptyEntries = options.IsRemoveEmpty;
            bool trim = options.IsTrim;

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separator!);
                if (trim && segment.length != 0)
                    segment = segment.Trim();
                if (segment.length != 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                if (trim && rest.length != 0)
                    rest = rest.Trim();

                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length >= separator!.Length && rest.StartsWithInternal(separator))
                    rest = rest.SubstringInternal(separator.Length);

                if (rest.length != 0 || !removeEmptyEntries)
                    result.Add(rest);
            }

            return result;
        }

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, string)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A string that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or empty, then no splitting will occur.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(string? separator, StringSegmentSplitOptions options) => Split(separator, default, options);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, string[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of strings that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegmentSplitOptions.None"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(string?[]? separators, int? maxLength, StringSegmentSplitOptions options = default)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separators == null || separators.Length == 0)
                return Split(maxLength, options);
            if (separators.Length == 1)
                return Split(separators[0], maxLength, options);

            IList<StringSegment>? result = SplitCommon(maxLength, options);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them
            bool removeEmptyEntries = options.IsRemoveEmpty;
            bool trim = options.IsTrim;

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separators);
                if (trim && segment.length != 0 && segment.length != length) // trimming only if split occurred (compatibility with String.Split)
                    segment = segment.Trim();
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                if (trim)
                    rest = rest.Trim();

                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0)
                {
                    foreach (string? sep in separators)
                    {
                        if (String.IsNullOrEmpty(sep) || sep!.Length > rest.length)
                            continue;
                        if (rest.StartsWithInternal(sep))
                        {
                            rest = rest.SubstringInternal(sep.Length);
                            break;
                        }
                    }
                }

                if (rest.length > 0 || !removeEmptyEntries)
                    result.Add(rest);
            }

            return result;
        }

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, string[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of strings that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(string?[]? separators, StringSegmentSplitOptions options) => Split(separators, default, options);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, string[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of strings that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(params string?[]? separators) => Split(separators, default, default);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, ReadOnlySpan{char})"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> that delimits the segments in this <see cref="StringSegment"/>. If empty, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries. This parameter is optional.
        /// <br/>Default value: <see cref="StringSegmentSplitOptions.None"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(ReadOnlySpan<char> separator, int? maxLength = default, StringSegmentSplitOptions options = default)
        {
            if (separator.Length == 1)
                return Split(separator[0], maxLength, options);

            IList<StringSegment>? result = SplitCommon(maxLength, options);
            if (result != null)
                return result;

            // empty string separator: returning whole string (compatibility with String.Split)
            if (separator.IsEmpty)
                return new[] { this };

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them
            bool removeEmptyEntries = options.IsRemoveEmpty;
            bool trim = options.IsTrim;

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (trim && segment.length != 0)
                    segment = segment.Trim();
                if (segment.length != 0|| !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                if (trim && rest.length != 0)
                    rest = rest.Trim();

                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length >= separator.Length && rest.StartsWith(separator))
                    rest = rest.SubstringInternal(separator.Length);

                if (rest.length != 0 || !removeEmptyEntries)
                    result.Add(rest);
            }

            return result;
        }

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, ReadOnlySpan{char})"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> that delimits the segments in this <see cref="StringSegment"/>. If empty, then no splitting will occur.</param>
        /// <param name="options">A <see cref="StringSegmentSplitOptions"/> value that specifies whether to trim segments and remove empty entries.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(ReadOnlySpan<char> separator, StringSegmentSplitOptions options) => Split(separator, default, options);
#endif

        #endregion

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "ParameterHidesMember", Justification = "Intended because it will be the new length of the returned instance")]
        internal StringSegment SubstringInternal(int start, int length) =>
            new StringSegment(str!, offset + start, length);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment SubstringInternal(int start) =>
            new StringSegment(str!, offset + start, length - start);

        #endregion

        #region Private Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private StringSegment[]? SplitCommon(int? maxLength, StringSegmentSplitOptions options)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            options.AssertValid();

            if (maxLength <= 1)
            {
                if (maxLength < 0)
                    Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                return maxLength == 0 ? Reflector.EmptyArray<StringSegment>()
                    : options.IsTrim ? new[] { Trim() }
                    : new[] { this };
            }

            if (length == 0)
                return options.IsRemoveEmpty ? Reflector.EmptyArray<StringSegment>() : new[] { Empty };

            return null;
        }

        #endregion

        #endregion

        #endregion
    }
}
