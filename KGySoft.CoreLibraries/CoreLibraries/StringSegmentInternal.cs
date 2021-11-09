#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentInternal.cs
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

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Similar to Memory/ArraySegment/ReadOnlySpan{char} but this is mutable, can be used in any platform and is optimized
    /// for quite a few special operations.
    /// NOTE: This struct is actually the same as the <see cref="StringSegment"/> struct before making it public.
    /// The original file history belongs to the <see cref="StringSegment"/> struct.
    /// The reintroduction occurred because this has a better performance but it cannot be readonly, which could be confusing as a public API
    /// </summary>
    internal struct StringSegmentInternal : IEquatable<StringSegmentInternal>
    {
        #region Fields

        internal readonly string String;

        internal int Offset;
        internal int Length;

        #endregion

        #region Indexers

        internal char this[int index] => String[Offset + index];

        #endregion

        #region Constructors

        internal StringSegmentInternal(string s, int offset, int length)
        {
            String = s;
            Offset = offset;
            Length = length;
        }

        internal StringSegmentInternal(string s) : this(s, 0, s.Length)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        public bool Equals(StringSegmentInternal other)
        {
            if (Length != other.Length)
                return false;
            if (ReferenceEquals(String, other.String) && Offset == other.Offset)
                return true;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (Length >= 20)
                return String.Compare(String, Offset, other.String, other.Offset, Length, StringComparison.Ordinal) == 0;
#endif
            for (int i = 0; i < Length; i++)
            {
                if (this[i] != other[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is StringSegmentInternal other && Equals(other);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "These are not expected to be changed in a hashed collection")]
        public override int GetHashCode()
        {
            // just for a default instance
            if (String == null!)
                return 0;
            return Length == String.Length
                ? StringSegmentComparer.GetHashCodeOrdinal(String)
                : StringSegmentComparer.GetHashCodeOrdinal(String, Offset, Length);
        }

        public override string ToString() => Length == 0 ? String.Empty : String.Substring(Offset, Length);

        #endregion

        #region Internal Methods

        internal void Trim()
        {
            TrimStart();
            TrimEnd();
        }

        internal void TrimStart()
        {
            int start = 0;
            while (start < Length && Char.IsWhiteSpace(this[start]))
                start += 1;

            if (start != 0)
                Slice(start);
        }

        internal void TrimStart(char trimChar)
        {
            int start = 0;
            while (start < Length && this[start] == trimChar)
                start += 1;

            if (start != 0)
                Slice(start);
        }

        internal void TrimEnd()
        {
            int end = Length - 1;
            while (end >= 0 && Char.IsWhiteSpace(this[end]))
                end -= 1;

            Slice(0, end + 1);
        }

        internal void TrimEnd(char trimChar)
        {
            int end = Length - 1;
            while (end >= 0 && this[end] == trimChar)
                end -= 1;

            Slice(0, end + 1);
        }

        internal StringSegmentInternal Substring(int start, int length) =>
            new StringSegmentInternal(String, Offset + start, length);

        internal StringSegmentInternal Substring(int start) =>
            new StringSegmentInternal(String, Offset + start, Length - start);

        /// <summary>
        /// Similar to <see cref="Substring(int,int)"/> but mutates self instance.
        /// </summary>
        internal void Slice(int start, int length)
        {
            Offset += start;
            Length = length;
        }

        /// <summary>
        /// Similar to <see cref="Substring(int)"/> but mutates self instance.
        /// </summary>
        internal void Slice(int start)
        {
            Offset += start;
            Length -= start;
        }

        internal List<StringSegmentInternal> Split(char separator)
        {
            Debug.Assert(Length != 0);

            var result = new List<StringSegmentInternal>(16);
            while (TryGetNextSegment(separator, out StringSegmentInternal segment))
                result.Add(segment);

            Debug.Assert(Length == 0);
            return result;
        }

        internal List<StringSegmentInternal> Split(string separator)
        {
            Debug.Assert(Length != 0);
            Debug.Assert(!String.IsNullOrEmpty(separator));
            if (separator.Length == 1)
                return Split(separator[0]);

            var result = new List<StringSegmentInternal>(16);
            while (TryGetNextSegment(separator, out StringSegmentInternal segment))
                result.Add(segment);

            Debug.Assert(Length == 0);
            return result;
        }

        internal bool TryParseIntQuick(bool allowNegative, ulong max, out ulong result)
        {
            Debug.Assert(Length > 0, $"Nonzero length is expected in {nameof(TryParseIntQuick)}");
            Debug.Assert(!allowNegative || max < UInt64.MaxValue, "If negative values are allowed max should be less than UInt64.MaxValue");

            result = 0UL;
            bool isNegative = false;
            int i = 0;

            switch (this[0])
            {
                case '+':
                    i += 1;
                    break;
                case '-':
                    isNegative = true;
                    i += 1;
                    break;
            }

            ulong value = 0UL;
            while (i < Length)
            {
                uint digit = this[i] - (uint)'0';
                if (digit > 9)
                    return false;

                ulong newValue = value * 10 + digit;

                // overflow
                if (newValue < value)
                    return false;

                value = newValue;
                i += 1;
            }

            if (isNegative)
            {
                if (value == 0)
                    return true;

                // For negative values the MaxValue of the appropriate range is expected (eg 127 for SByte) but actually -128 should be accepted, too
                if (!allowNegative || value > max + 1)
                    return false;

                result = (ulong)-(long)value;
                return true;
            }

            if (value > max)
                return false;

            result = value;
            return true;
        }

        internal bool TryGetNextSegment(char separator, out StringSegmentInternal result)
        {
            if (Length == 0)
            {
                result = default;
                return false;
            }

            int pos = IndexOf(separator);

            // last segment
            if (pos == -1)
            {
                result = this;
                this = default;
                return true;
            }

            // returning next segment and advance
            result = Substring(0, pos);
            Slice(pos + 1);
            return true;
        }

        internal bool TryGetNextSegment(string separator, out StringSegmentInternal result)
        {
            Debug.Assert(separator.Length > 0);
            if (Length == 0)
            {
                result = default;
                return false;
            }

            int pos = IndexOf(separator);

            // last segment
            if (pos == -1)
            {
                result = this;
                this = default;
                return true;
            }

            // returning next segment and advance
            result = Substring(0, pos);
            Slice(pos + separator.Length);
            return true;
        }

        internal bool TryGetNextSegment(StringSegment separator, out StringSegmentInternal result)
        {
            Debug.Assert(separator.Length > 0);
            if (Length == 0)
            {
                result = default;
                return false;
            }

            int pos = IndexOf(separator);

            // last segment
            if (pos == -1)
            {
                result = this;
                this = default;
                return true;
            }

            // returning next segment and advance
            result = Substring(0, pos);
            Slice(pos + separator.Length);
            return true;
        }

        internal bool Equals(string other)
        {
            Debug.Assert(other != null!);
            if (Length != other!.Length)
                return false;
            if (ReferenceEquals(String, other) && Offset == 0)
                return true;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (Length >= 20)
                return String.Compare(String, Offset, other, 0, Length, StringComparison.Ordinal) == 0;
#endif
            for (int i = 0; i < Length; i++)
            {
                if (this[i] != other[i])
                    return false;
            }

            return true;
        }

        internal bool EqualsOrdinalIgnoreCase(string other)
        {
            Debug.Assert(other != null!);
            if (Length != other!.Length)
                return false;
            if (ReferenceEquals(String, other) && Offset == 0)
                return true;

            if (String.Length == other.Length)
                return String.Equals(String, other, StringComparison.OrdinalIgnoreCase);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
                for (int i = 0; i < Length; i++)
                {
                    if (Char.ToUpperInvariant(this[i]) != Char.ToUpperInvariant(other[i]))
                        return false;
                }

                return true;
#else
            return String.AsSpan(Offset, Length).Equals(other.AsSpan(), StringComparison.OrdinalIgnoreCase);
#endif
        }

        internal bool EqualsOrdinalIgnoreCase(StringSegmentInternal other)
        {
            Debug.Assert(other.String != null!);
            if (Length != other.Length)
                return false;
            if (ReferenceEquals(String, other.String) && Offset == 0)
                return true;

            if (String.Length == Length && other.String!.Length == other.Length)
                return String.Equals(String, other.String, StringComparison.OrdinalIgnoreCase);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
                for (int i = 0; i < Length; i++)
                {
                    if (Char.ToUpperInvariant(this[i]) != Char.ToUpperInvariant(other[i]))
                        return false;
                }

                return true;
#else
            return String.AsSpan(Offset, Length).Equals(other.String.AsSpan(other.Offset, other.Length), StringComparison.OrdinalIgnoreCase);
#endif
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal int GetHashCodeOrdinalIgnoreCase()
        {
            // just for default instance
            if (String == null!)
                return 0;
            return Length == String.Length
                ? StringSegmentComparer.GetHashCodeOrdinalIgnoreCase(String)
                : StringSegmentComparer.GetHashCodeOrdinalIgnoreCase(String, Offset, Length);
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private int IndexOf(char c)
        {
            Debug.Assert(Length > 0);
            int result = String.IndexOf(c, Offset, Length);
            return result >= 0 ? result - Offset : -1;
        }

        private int IndexOf(string s)
        {
            Debug.Assert(Length > 0);
            Debug.Assert(s.Length > 0);
            if (s.Length == 1)
                return IndexOf(s[0]);

            int result = String.IndexOf(s, Offset, Length, StringComparison.Ordinal);
            return result >= 0 ? result - Offset : -1;
        }

        private int IndexOf(StringSegment s)
        {
            Debug.Assert(Length > 0);
            Debug.Assert(s.Length > 0);
            if (s.Length == 1)
                return IndexOf(s.GetCharInternal(0));
            if (s.Length == s.UnderlyingString!.Length)
                return IndexOf(s.UnderlyingString);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            if (s.Length >= Length)
            {
                if (s.Length != Length)
                    return -1;

                if (Offset == 0)
                    return s.UnderlyingString == String ? 0 : -1;
            }

            char first = s.GetCharInternal(0);
            int end = Offset + Length - s.Length + 1;
            for (int i = Offset; i < end; i++)
            {
                if (String[i] != first)
                    continue;

                // first char matches: looking for difference in other chars if any
                for (int j = 1; j < s.Length; j++)
                {
                    if (String[i + j] == s.GetCharInternal(j))
                        continue;

                    // here we have a difference: continuing with skipping the matched characters
                    i += j - 1;
                    goto continueOuter; // yes, a dreadful goto which is actually a continue
                }

                // Here we have full match. As single char separators are not handled here we could have
                // check this into the inner loop to avoid goto but that requires an extra condition.
                return i - Offset;

                continueOuter:;
            }

            return -1;
#else
            return String.AsSpan(Offset, Length).IndexOf(s.AsSpan, StringComparison.Ordinal);
#endif
        }

        #endregion

        #endregion
    }
}
