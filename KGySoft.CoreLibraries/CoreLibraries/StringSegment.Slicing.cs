#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegment.Slicing.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
using System.Diagnostics;
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
                if (Char.IsWhiteSpace(rest.str[i]))
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
            return new StringSegment(rest.str, offset, pos);
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
            return new StringSegment(rest.str, offset, pos);
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
            return new StringSegment(rest.str, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, in StringSegment separator)
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
            return new StringSegment(rest.str, offset, pos);
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
            return new StringSegment(rest.str, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, string[] separators)
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
            rest = rest.SubstringInternal(pos + separators[separatorIndex].Length);
            return new StringSegment(rest.str, offset, pos);
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
            return new StringSegment(rest.str, offset, pos);
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        #region Trim

        /// <summary>
        /// Removes all leading and trailing white-space characters from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all white-space
        /// characters are removed from the start and end of the current <see cref="StringSegment"/>.</returns>
        public StringSegment Trim() => TrimStart().TrimEnd();

        /// <summary>
        /// Removes all the leading white-space characters from the current <see cref="StringSegment"/>.
        /// </summary>
        /// <returns>A <see cref="StringSegment"/> that represents the string that remains after all white-space
        /// characters are removed from the start of the current <see cref="StringSegment"/>.</returns>
        public StringSegment TrimStart()
        {
            if (str == null)
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
            if (str == null)
                return this;
            int end = length - 1;
            while (end >= 0 && Char.IsWhiteSpace(GetCharInternal(end)))
                end -= 1;

            return SubstringInternal(0, end + 1);
        }

        #endregion

        #region Substring

        /// <summary>
        /// Gets a <see cref="StringSegment"/> instance, which represents a substring of the current instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first character of the returned segment.</param>
        /// <param name="length">The desired length of the returned segment.</param>
        /// <returns>The subsegment of the current <see cref="StringSegment"/> instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "ParameterHidesMember", Justification = "Intended because of compatibility with string and because it will be the new length of the returned instance")]
        public StringSegment Substring(int startIndex, int length)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if ((uint)startIndex > (uint)this.length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)length > (uint)this.length - startIndex)
                Throw.ArgumentOutOfRangeException(Argument.length);

            return new StringSegment(str, offset + startIndex, length);
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
            return new StringSegment(str, start, length - startIndex);
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
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by whitespace characters.</returns>
        public IList<StringSegment> Split(int? maxLength = default, bool removeEmptyEntries = false)
        {
            IList<StringSegment> result = SplitCommon(maxLength, removeEmptyEntries);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);

            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them

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
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by whitespace characters.</returns>
        public IList<StringSegment> Split(bool removeEmptyEntries) => Split(default(int?), removeEmptyEntries);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, char)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A character that delimits the segments in this <see cref="StringSegment"/>.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(char separator, int? maxLength = default, bool removeEmptyEntries = false)
        {
            IList<StringSegment> result = SplitCommon(maxLength, removeEmptyEntries);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0 && rest[0] == separator)
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
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(char separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, char[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of characters that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(char[] separators, int? maxLength, bool removeEmptyEntries = false)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separators == null || separators.Length == 0)
                return Split(maxLength, removeEmptyEntries);
            if (separators.Length == 1)
                return Split(separators[0], maxLength, removeEmptyEntries);

            IList<StringSegment> result = SplitCommon(maxLength, removeEmptyEntries);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separators);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0)
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

                if (rest.length > 0 || !removeEmptyEntries)
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
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(char[] separators, bool removeEmptyEntries) => Split(separators, default, removeEmptyEntries);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, char[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of characters that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(params char[] separators) => Split(separators, default, false);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, StringSegment)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A <see cref="StringSegment"/> that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or empty, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(in StringSegment separator, int? maxLength = default, bool removeEmptyEntries = false)
        {
            if (separator.length == 1)
                return Split(separator[0], maxLength, removeEmptyEntries);

            IList<StringSegment> result = SplitCommon(maxLength, removeEmptyEntries);
            if (result != null)
                return result;

            // null or empty string separator: returning whole string (compatibility with String.Split)
            if (separator.length == 0)
                return new[] { this };

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length >= separator.length && rest.StartsWith(separator))
                    rest = rest.SubstringInternal(separator.length);

                if (rest.length > 0 || !removeEmptyEntries)
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
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(in StringSegment separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, StringSegment[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of <see cref="StringSegment"/> instances that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(StringSegment[] separators, int? maxLength, bool removeEmptyEntries = false)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separators == null || separators.Length == 0)
                return Split(maxLength, removeEmptyEntries);
            if (separators.Length == 1)
                return Split(separators[0], maxLength, removeEmptyEntries);

            IList<StringSegment> result = SplitCommon(maxLength, removeEmptyEntries);
            if (result != null)
                return result;

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separators);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0)
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

                if (rest.length > 0 || !removeEmptyEntries)
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
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(StringSegment[] separators, bool removeEmptyEntries) => Split(separators, default, removeEmptyEntries);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, StringSegment[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of <see cref="StringSegment"/> instances that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(params StringSegment[] separators) => Split(separators, default, false);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, string)"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separator">A string that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or empty, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(string separator, int? maxLength = default, bool removeEmptyEntries = false)
        {
            if (separator?.Length == 1)
                return Split(separator[0], maxLength, removeEmptyEntries);

            IList<StringSegment> result = SplitCommon(maxLength, removeEmptyEntries);
            if (result != null)
                return result;

            // null or empty string separator: returning whole string (compatibility with String.Split)
            if (String.IsNullOrEmpty(separator))
                return new[] { this };

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);
            result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length >= separator.Length && rest.StartsWithInternal(separator))
                    rest = rest.SubstringInternal(separator.Length);

                if (rest.length > 0 || !removeEmptyEntries)
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
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separator"/>.</returns>
        public IList<StringSegment> Split(string separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances of no more than <paramref name="maxLength"/> segments, without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, string[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of strings that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <param name="maxLength">The maximum number of segments to return. If <see langword="null"/>, then the whole string is processed represented by this <see cref="StringSegment"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(string[] separators, int? maxLength, bool removeEmptyEntries = false)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separators == null || separators.Length == 0)
                return Split(maxLength, removeEmptyEntries);
            if (separators.Length == 1)
                return Split(separators[0], maxLength, removeEmptyEntries);

            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);

            if (maxLength <= 1)
            {
                if (maxLength < 0)
                    Throw.ArgumentException(Argument.maxLength, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                return maxLength == 0 ? Reflector.EmptyArray<StringSegment>() : new[] { this };
            }

            if (length == 0)
                return removeEmptyEntries ? Reflector.EmptyArray<StringSegment>() : new[] { this };

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);

            var result = new List<StringSegment>(Math.Min(limit, 16));
            StringSegment rest = this;
            limit -= 1; // so the last segment is not searched if there are too many of them

            while (!rest.IsNull && result.Count < limit)
            {
                StringSegment segment = GetNextSegment(ref rest, separators);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0)
                {
                    foreach (string sep in separators)
                    {
                        if (String.IsNullOrEmpty(sep) || sep.Length > rest.length)
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
        /// <param name="removeEmptyEntries"><see langword="true"/>&#160;to disallow returning empty segments in the result; <see langword="false"/>&#160;to allow returning empty segments.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(string[] separators, bool removeEmptyEntries) => Split(separators, default, removeEmptyEntries);

        /// <summary>
        /// Splits this <see cref="StringSegment"/> instance into a collection of <see cref="StringSegment"/> instances without allocating new strings.
        /// Alternatively, you can use the <see cref="StringSegmentExtensions.ReadToSeparator(ref StringSegment, string[])"/> extension method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="StringSegment"/> type for details and some examples.
        /// </summary>
        /// <param name="separators">An array of strings that delimits the segments in this <see cref="StringSegment"/>. If <see langword="null"/>&#160;or contains no elements,
        /// then the split operation will use whitespace separators. If contains only <see langword="null"/>&#160;or empty elements, then no splitting will occur.</param>
        /// <returns>A list of <see cref="StringSegment"/> instances, whose elements contain the substrings in this <see cref="StringSegment"/> that are
        /// delimited by <paramref name="separators"/>.</returns>
        public IList<StringSegment> Split(params string[] separators) => Split(separators, default, false);

        #endregion

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "ParameterHidesMember", Justification = "Intended because it will be the new length of the returned instance")]
        internal StringSegment SubstringInternal(int start, int length) =>
            new StringSegment(str, offset + start, length);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment SubstringInternal(int start) =>
            new StringSegment(str, offset + start, length - start);

        #endregion

        #region Private Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private StringSegment[] SplitCommon(int? maxLength, bool removeEmptyEntries)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);

            if (maxLength <= 1)
            {
                if (maxLength < 0)
                    Throw.ArgumentOutOfRangeException(Argument.maxLength);
                return maxLength == 0 ? Reflector.EmptyArray<StringSegment>() : new[] { this };
            }

            if (length == 0)
                return removeEmptyEntries ? Reflector.EmptyArray<StringSegment>() : new[] { this };

            return null;
        }

        #endregion

        #endregion

        #endregion
    }
}
