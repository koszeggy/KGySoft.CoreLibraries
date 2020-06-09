#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MutableStringSegment.cs
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
    internal struct MutableStringSegment : IEquatable<MutableStringSegment>
    {
        #region Fields

        #region Internal Fields

        internal int Length;

        #endregion

        #region Private Fields

        private readonly string str;

        private int offset;

        #endregion

        #endregion

        #region Indexers

        internal char this[int index] => str[offset + index];

        #endregion

        #region Constructors

        internal MutableStringSegment(string s, int offset, int length)
        {
            str = s;
            this.offset = offset;
            Length = length;
        }

        internal MutableStringSegment(string s) : this(s, 0, s.Length)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        public bool Equals(MutableStringSegment other)
        {
            if (Length != other.Length)
                return false;
            if (ReferenceEquals(str, other.str) && offset == other.offset)
                return true;

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
            // It would be better by Vector but that needs a char->ushort conversion
            if (Length >= 20)
                return String.Compare(str, offset, other.str, other.offset, Length, StringComparison.Ordinal) == 0;
#endif
            for (int i = 0; i < Length; i++)
            {
                if (this[i] != other[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is MutableStringSegment other && Equals(other);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override int GetHashCode()
        {
            if (str == null)
                return 0;
            return Length == str.Length
                ? StringSegmentComparer.GetHashCodeOrdinal(str)
                : StringSegmentComparer.GetHashCodeOrdinal(str, offset, Length);
        }

        public override string ToString() => Length == 0 ? String.Empty : str.Substring(offset, Length);

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

            Slice(start);
        }

        internal void TrimEnd()
        {
            int end = Length - 1;
            while (end >= 0 && Char.IsWhiteSpace(this[end]))
                end -= 1;

            Slice(0, end + 1);
        }

        internal MutableStringSegment Substring(int start, int length) =>
            new MutableStringSegment(str, offset + start, length);

        internal MutableStringSegment Substring(int start) =>
            new MutableStringSegment(str, offset + start, Length - start);

        /// <summary>
        /// Similar to <see cref="Substring(int,int)"/> but mutates self instance.
        /// </summary>
        internal void Slice(int start, int length)
        {
            offset += start;
            Length = length;
        }

        /// <summary>
        /// Similar to <see cref="Substring(int)"/> but mutates self instance.
        /// </summary>
        internal void Slice(int start)
        {
            offset += start;
            Length -= start;
        }

        internal bool TryParseIntQuick(bool allowNegative, ulong max, out ulong result)
        {
            Debug.Assert(Length > 0, $"Nonzero length is expected in {nameof(TryParseIntQuick)}");

            result = 0UL;
            bool isNegative = false;
            int i = 0;

            switch (this[0])
            {
                case '+':
                    i += 1;
                    break;
                case '-':
                    if (!allowNegative)
                        return false;
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

            // we check it only here to minimize the performance overhead for valid cases
            if (value > max && !(isNegative && value == max + 1))
                return false;

            result = isNegative ? (ulong)-(long)value : value;
            return true;
        }

        internal bool TryGetNextSegment(string separator, out MutableStringSegment result)
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
            Slice(pos + separator.Length);
            return true;
        }

        internal bool TryGetNextSegment(StringSegment separator, out MutableStringSegment result)
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
            Slice(pos + separator.Length);
            return true;
        }

        internal bool Equals(string other)
        {
            Debug.Assert(other != null);
            if (Length != other.Length)
                return false;
            if (ReferenceEquals(str, other) && offset == 0)
                return true;

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
            // It would be better by Vector but that needs a char->ushort conversion
            if (Length >= 20)
                return String.Compare(str, offset, other, 0, Length, StringComparison.Ordinal) == 0;
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
            Debug.Assert(other != null);
            if (Length != other.Length)
                return false;
            if (ReferenceEquals(str, other) && offset == 0)
                return true;

            if (str.Length == other.Length)
                return String.Equals(str, other, StringComparison.OrdinalIgnoreCase);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
                for (int i = 0; i < Length; i++)
                {
                    if (Char.ToUpperInvariant(this[i]) != Char.ToUpperInvariant(other[i]))
                        return false;
                }

                return true;
#else
            return str.AsSpan(offset, Length).Equals(other.AsSpan(), StringComparison.OrdinalIgnoreCase);
#endif
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal int GetHashCodeOrdinalIgnoreCase()
        {
            if (str == null)
                return 0;
            return Length == str.Length
                ? StringSegmentComparer.GetHashCodeOrdinalIgnoreCase(str)
                : StringSegmentComparer.GetHashCodeOrdinalIgnoreCase(str, offset, Length);
        }

        #endregion

        #region Private Methods

        private int IndexOf(string s)
        {
            // This would be the native version, which is much slower even in .NET Core:
            //int result = str.IndexOf(s, offset, Length, StringComparison.Ordinal);
            //return result >= 0 ? result - offset : -1;

            int len = s.Length;
            if (len == 0)
                return 0;

            if (len >= Length)
            {
                if (len != Length)
                    return -1;
                if (offset == 0)
                    return s == str ? 0 : -1;
            }

            char first = s[0];

            // single char separator: the simple way
            if (len == 1)
            {
                for (int i = offset; i < offset + Length; i++)
                {
                    if (str[i] == first)
                        return i - offset;
                }

                return -1;
            }

            int end = offset + Length - len + 1;
            for (int i = offset; i < end; i++)
            {
                if (str[i] != first)
                    continue;

                // first char matches: looking for difference in other chars if any
                for (int j = 1; j < len; j++)
                {
                    if (str[i + j] == s[j])
                        continue;

                    // here we have a difference: continuing with skipping the matched characters
                    i += j - 1;
                    goto continueOuter; // yes, a dreadful goto which is actually a continue
                }

                // Here we have full match. As single char separators are not handled here we could have
                // check this into the inner loop to avoid goto but that requires an extra condition.
                return i - offset;

                continueOuter:;
            }

            return -1;
        }

        private int IndexOf(StringSegment s)
        {
            Debug.Assert(!s.IsNull);

            int len = s.Length;
            if (len == 0)
                return 0;

            if (len >= Length)
            {
                if (len != Length)
                    return -1;

                if (offset == 0)
                    return s.UnderlyingString == str ? 0 : -1;
            }

            char first = s.GetCharInternal(0);

            // single char separator: the simple way
            if (len == 1)
            {
                for (int i = offset; i < offset + Length; i++)
                {
                    if (str[i] == first)
                        return i - offset;
                }

                return -1;
            }

            int end = offset + Length - len + 1;
            for (int i = offset; i < end; i++)
            {
                if (str[i] != first)
                    continue;

                // first char matches: looking for difference in other chars if any
                for (int j = 1; j < len; j++)
                {
                    if (str[i + j] == s.GetCharInternal(j))
                        continue;

                    // here we have a difference: continuing with skipping the matched characters
                    i += j - 1;
                    goto continueOuter; // yes, a dreadful goto which is actually a continue
                }

                // Here we have full match. As single char separators are not handled here we could have
                // check this into the inner loop to avoid goto but that requires an extra condition.
                return i - offset;

                continueOuter:;
            }

            return -1;
        }

        #endregion

        #endregion
    }
}
