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

using KGySoft.Reflection;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
                StringSegment result = rest.IsNull ? default : Empty;
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
                StringSegment result = rest.IsNull ? default : Empty;
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
            rest = rest.SubstringInternal(pos + 1);
            return new StringSegment(rest.str, offset, pos);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static StringSegment GetNextSegment(ref StringSegment rest, char[] separators)
        {
            Debug.Assert(separators != null && separators.Length > 0, "Non-empty separators are expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : Empty;
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
                StringSegment result = rest.IsNull ? default : Empty;
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
            Debug.Assert(!separator.IsNullOrEmpty(), "Non-empty separator is expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : Empty;
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
            Debug.Assert(separators != null && separators.Length > 0, "Non-empty separators are expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : Empty;
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
            Debug.Assert(separators != null && separators.Length > 0, "Non-empty separators are expected here");
            if (rest.length == 0)
            {
                StringSegment result = rest.IsNull ? default : Empty;
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

        /// <summary>
        /// Gets a new <see cref="StringSegment"/> instance, which represents a subsegment of the current instance with the specified <paramref name="offset"/> and <paramref name="length"/>.
        /// </summary>
        /// <param name="offset">The offset that points to the first character of the returned segment.</param>
        /// <param name="length">The desired length of the returned segment.</param>
        /// <returns>The subsegment of the current <see cref="StringSegment"/> instance with the specified <paramref name="offset"/> and <paramref name="length"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public StringSegment Substring(int offset, int length)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (offset < 0)
                Throw.ArgumentOutOfRangeException(Argument.offset);
            return str.GetSegment(this.offset + offset, length);
        }

        /// <summary>
        /// Gets a new <see cref="StringSegment"/> instance, which represents a subsegment of the current instance with the specified <paramref name="offset"/>.
        /// </summary>
        /// <param name="offset">The offset that points to the first character of the returned segment.</param>
        /// <returns>The subsegment of the current <see cref="StringSegment"/> instance with the specified <paramref name="offset"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public StringSegment Substring(int offset)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (offset < 0)
                Throw.ArgumentOutOfRangeException(Argument.offset);
            return str.GetSegment(this.offset + offset, length - offset);
        }

        public IList<StringSegment> Split(int? maxLength = default, bool removeEmptyEntries = true)
        {
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

        public IList<StringSegment> Split(bool removeEmptyEntries) => Split(default(int?), removeEmptyEntries);

        public IList<StringSegment> Split(char separator, int? maxLength = default, bool removeEmptyEntries = false)
        {
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

        public IList<StringSegment> Split(char separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        public IList<StringSegment> Split(char[] separator, int? maxLength, bool removeEmptyEntries = false)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separator.IsNullOrEmpty())
                return Split(maxLength, removeEmptyEntries);
            if (separator.Length == 1)
                return Split(separator[0], maxLength, removeEmptyEntries);

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
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0)
                {
                    for (int i = 0; i < separator.Length; i++)
                    {
                        if (rest[0] == separator[i])
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

        public IList<StringSegment> Split(params char[] separator) => Split(separator, default, false);

        public IList<StringSegment> Split(char[] separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        public IList<StringSegment> Split(in StringSegment separator, int? maxLength = default, bool removeEmptyEntries = false)
        {
            if (separator.length == 1)
                return Split(separator[0], maxLength, removeEmptyEntries);

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

            // null or empty string separator: returning whole string (compatibility with String.Split)
            if (separator.length == 0)
                return new[] { this };

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);

            var result = new List<StringSegment>(Math.Min(limit, 16));
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

        public IList<StringSegment> Split(in StringSegment separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        public IList<StringSegment> Split(StringSegment[] separator, int? maxLength, bool removeEmptyEntries = false)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separator.IsNullOrEmpty())
                return Split(maxLength, removeEmptyEntries);
            if (separator.Length == 1)
                return Split(separator[0], maxLength, removeEmptyEntries);

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
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0)
                {
                    foreach (StringSegment sep in separator)
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

        public IList<StringSegment> Split(params StringSegment[] separator) => Split(separator, default, false);

        public IList<StringSegment> Split(StringSegment[] separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        public IList<StringSegment> Split(string separator, int? maxLength = default, bool removeEmptyEntries = false)
        {
            if (separator?.Length == 1)
                return Split(separator[0], maxLength, removeEmptyEntries);

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

            // null or empty string separator: returning whole string (compatibility with String.Split)
            if (String.IsNullOrEmpty(separator))
                return new[] { this };

            int limit = maxLength.GetValueOrDefault(Int32.MaxValue);

            var result = new List<StringSegment>(Math.Min(limit, 16));
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

        public IList<StringSegment> Split(string separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        public IList<StringSegment> Split(string[] separator, int? maxLength, bool removeEmptyEntries = false)
        {
            // No separator: splitting by white spaces (compatibility with String.Split)
            if (separator.IsNullOrEmpty())
                return Split(maxLength, removeEmptyEntries);
            if (separator.Length == 1)
                return Split(separator[0], maxLength, removeEmptyEntries);

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
                StringSegment segment = GetNextSegment(ref rest, separator);
                if (segment.length > 0 || !removeEmptyEntries)
                    result.Add(segment);
            }

            if (!rest.IsNull)
            {
                // if we reached limit but we are before a separator we remove it if empty segments are not allowed
                // (this is how String.Split also works)
                if (removeEmptyEntries && result.Count == limit && rest.length > 0)
                {
                    foreach (string sep in separator)
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

        public IList<StringSegment> Split(params string[] separator) => Split(separator, default, false);

        public IList<StringSegment> Split(string[] separator, bool removeEmptyEntries) => Split(separator, default, removeEmptyEntries);

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment SubstringInternal(int start, int length) =>
            new StringSegment(str, offset + start, length);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment SubstringInternal(int start) =>
            new StringSegment(str, offset + start, length - start);

        #endregion

        #endregion

        #endregion
    }
}
