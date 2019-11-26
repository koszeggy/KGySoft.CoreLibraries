#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegment.cs
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
using System.Diagnostics;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Similar to Memory/ArraySegment{char} but this is mutable, can be used in any platform and is optimized
    /// for quite a few special operations.
    /// </summary>
    internal struct StringSegment : IEquatable<StringSegment>
    {
        #region StringSegmentIgnoreCaseComparer class

        private sealed class StringSegmentIgnoreCaseComparer : IEqualityComparer<StringSegment>
        {
            #region Methods

            public bool Equals(StringSegment x, StringSegment y)
            {
                if (x.Length != y.Length)
                    return false;
                if (ReferenceEquals(x.str, y.str) && x.offset == y.offset)
                    return true;

                if (x.str.Length == x.Length && y.str.Length == y.Length)
                    return String.Equals(x.str, y.str, StringComparison.OrdinalIgnoreCase);

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
                return x.str.AsSpan(x.offset, x.Length).Equals(y.str.AsSpan(y.offset, y.Length), StringComparison.OrdinalIgnoreCase);
#else
                for (int i = 0; i < x.Length; i++)
                {
                    if (Char.ToUpperInvariant(x[i]) != Char.ToUpperInvariant(y[i]))
                        return false;
                }

                return true;
#endif
            }

            public int GetHashCode(StringSegment obj)
            {
                if (obj.Length == 0)
                    return 0;

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
                if (obj.Length == obj.str.Length)
                    return obj.str.GetHashCode(StringComparison.OrdinalIgnoreCase);
#endif

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1)
                return String.GetHashCode(obj.str.AsSpan(obj.offset, obj.Length), StringComparison.OrdinalIgnoreCase);
#else
                var result = 13;
                for (int i = 0; i < obj.Length; i++)
                    result = result * 397 + Char.ToUpperInvariant(obj[i]);

                return result;
#endif
            }

            #endregion
        }

        #endregion

        #region Fields

        #region Static Fields

        private static StringSegmentIgnoreCaseComparer ignoreCaseComparer;

        #endregion

        #region Instance Fields

        #region Internal Fields

        internal int Length;

        #endregion

        #region Private Fields

        private readonly string str;

        private int offset;

        #endregion

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        internal static IEqualityComparer<StringSegment> IgnoreCaseComparer => ignoreCaseComparer ??= new StringSegmentIgnoreCaseComparer();

        #endregion

        #region Indexers

        internal char this[int index] => str[offset + index];

        #endregion

        #endregion

        #region Constructors

        internal StringSegment(string s, int offset, int length)
        {
            str = s;
            this.offset = offset;
            Length = length;
        }

        internal StringSegment(string s, int offset) : this(s, offset, s.Length - offset)
        {
        }

        internal StringSegment(string s) : this(s, 0, s.Length)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        public bool Equals(StringSegment other)
        {
            if (Length != other.Length)
                return false;
            if (ReferenceEquals(str, other.str) && offset == other.offset)
                return true;

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
            // It would be better by Vector but that needs a char->ushort conversion
            if (Length >= 20)
                return str.AsSpan(offset, Length).SequenceEqual(other.str.AsSpan(other.offset, other.Length));
#endif
            for (int i = 0; i < Length; i++)
            {
                if (this[i] != other[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is StringSegment other && Equals(other);

        public override int GetHashCode()
        {
            if (Length == 0)
                return 0;

            // This is a much cheaper hash code than the one used by string
            // Of course, we utilize that StringString is internal and used in dictionaries for enums with typically short names.
            var result = 13;
            for (int i = 0; i < Length; i++)
                result = result * 397 + this[i];

            return result;
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

        internal StringSegment Substring(int start, int length) =>
            new StringSegment(str, offset + start, length);

        internal StringSegment Substring(int start) =>
            new StringSegment(str, offset + start, Length - start);

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

                //value *= 10;
                //value += digit;
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

        internal int IndexOf(string s)
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

        internal bool TryGetNextSegment(string separator, out StringSegment result)
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

        #endregion

        #endregion
    }
}
