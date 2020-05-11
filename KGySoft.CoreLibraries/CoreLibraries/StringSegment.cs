#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegment.cs
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a segment of a <see cref="string"/>. This type is similar to <see cref="ReadOnlyMemory{T}"/>/<see cref="ArraySegment{T}"/>/<see cref="Span{T}"/> of <see cref="char">char</see>
    /// but <see cref="StringSegment"/> can be used also in old platforms and is optimized for a few dedicated string operations.
    /// <br/>To create an instance use the <see cref="O:KGySoft.CoreLibraries.StringExtensions.GetSegment"/> extension methods or just cast a string instance to <see cref="StringSegment"/>.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>To create a <see cref="StringSegment"/> instance from a string you can use the implicit conversion, or the <see cref="StringExtensions.AsSegment">AsSegment</see>
    /// and <see cref="O:KGySoft.CoreLibraries.StringExtensions.GetSegment"/> extension methods.</para>
    /// <note type="note">For public usage this type behaves as an immutable type. However, <see cref="StringSegment"/> is not declared as <see langword="readonly"/>, and for the best performance you should
    /// not declare a <see langword="readonly"/>&#160;<see cref="StringSegment"/> field. For example, when the <see cref="ToString()">ToString</see> method is called for the first time, the internally stored string is replaced to store the actual represented segment only.
    /// If you declare a <see cref="StringSegment"/> as <see langword="readonly"/>, then the CLR generates a defensive copy so a new <see cref="string">string</see> might be allocated on each <see cref="ToString()">ToString</see> call.</note>
    ///
    /// TODO: .NET 3.5/4.0/4.5 .NET Standard 2.0/2.1 .NET Core 2.0: Non-ordinal GetHashCode may allocate a new string
    /// TODO: As opposed to string, CompareTo default is by ordinal
    /// TODO: assign, compare string
    /// TODO: assign works even with null, IsNull is true, Length is 0
    /// </remarks>
    [Serializable]
    [SuppressMessage("Design", "CA1036:Override methods on comparable types",
        Justification = "Not implementing <, <=, >, >= operators because even string does not implement them")]
    [DebuggerDisplay("{" + nameof(ToString) + "(false)}")]
    public struct StringSegment : IEquatable<StringSegment>, IComparable<StringSegment>, IComparable
    {
        #region Fields

        #region Static Fields

        public static StringSegment Empty = String.Empty;

        public static StringSegment Null = default;

        #endregion

        #region Instance Fields

        private readonly string str;

        private int offset;
        private int length;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets the length of this <see cref="StringSegment"/>.
        /// </summary>
        public int Length => length;

        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance was created from a <see langword="null"/>&#160;<see cref="string"/>.
        /// <br/>Please note that the <see cref="ToString">ToString</see> method returns <see langword="null"/>&#160;when this property returns <see langword="true"/>.
        /// </summary>
        public bool IsNull => str == null;

        /// <summary>
        /// Gets whether this <see cref="StringSegment"/> instance represents an empty segment or was created from a <see langword="null"/>&#160;<see cref="string"/>.
        /// </summary>
        public bool IsNullOrEmpty => length == 0;

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        public ReadOnlySpan<char> Span => str.AsSpan(offset, length);

        public ReadOnlyMemory<char> Memory => str.AsMemory(offset, length); 
#endif

        #endregion

        #region Indexers

        /// <summary>
        /// Gets the character at the specified position in this <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="index">The index of the character to obtain.</param>
        /// <returns>The character at the specified position in this <see cref="StringSegment"/>.</returns>
        public char this[int index]
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                StringSegment a = null;
                bool e = a == null;
                if (str == null)
                    Throw.InvalidOperationException(Res.StringSegmentNull);
                if ((uint)index >= (uint)length)
                    Throw.ArgumentOutOfRangeException(Argument.index);
                return GetCharInternal(index);
            }
        }

        #endregion

        #endregion

        #region Operators

        /// <summary>
        /// Performs an implicit conversion from <see cref="string">string</see> to <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="s">The string to be converted to a <see cref="StringSegment"/>.</param>
        /// <returns>
        /// A <see cref="StringSegment"/> instance that represents the original string.
        /// </returns>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
                Justification = "The named alternative exists in StringExtensions.AsSegment")]
        public static implicit operator StringSegment(string s) => s == null ? Null : new StringSegment(s);

        /// <summary>
        /// Performs an explicit conversion from <see cref="StringSegment"/> to <see cref="string">string</see>.
        /// </summary>
        /// <param name="stringSegment">The <see cref="StringSegment"/> to be converted to a string.</param>
        /// <returns>
        /// A <see cref="string">string</see> instance that represents the specified <see cref="StringSegment"/>.
        /// </returns>
        public static explicit operator string(StringSegment stringSegment) => stringSegment.ToString();

        public static bool operator ==(StringSegment a, StringSegment b) => a.Equals(b);

        public static bool operator !=(StringSegment a, StringSegment b) => !(a == b);

        #endregion

        #region Constructors

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment(string s, int offset, int length)
        {
            Debug.Assert(s != null);
            str = s;
            this.offset = offset;
            this.length = length;
        }

        internal StringSegment(string s, int offset) : this(s, offset, s.Length - offset)
        {
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment(string s)
        {
            Debug.Assert(s != null);
            str = s;
            offset = 0;
            length = s.Length;
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        public static bool Equals(StringSegment a, StringSegment b, StringComparison comparison = StringComparison.Ordinal)
        {
            switch (comparison)
            {
                case StringComparison.Ordinal:
                    return a.Equals(b);
                case StringComparison.OrdinalIgnoreCase:
                    return EqualsOrdinalIgnoreCase(a, b);
                default:
                    return Compare(a, b, comparison) == 0;
            }
        }

        public static int Compare(StringSegment a, StringSegment b, StringComparison comparison = StringComparison.Ordinal)
        {
            if (comparison == StringComparison.Ordinal)
                return a.CompareTo(b);

            if (!comparison.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

            if (a.str == null || b.str == null)
            {
                // They are both null
                if (ReferenceEquals(a.str, b.str))
                    return 0;

                return a.str == null ? -1 : 1;
            }

            // comparing withing the common length
            int result = String.Compare(a.str, a.offset, b.str, b.offset, Math.Min(a.length, b.length), comparison);

            // if they are equal then the longer will be the larger
            return result == 0 ? a.length - b.length : result;
        }

        public static int Compare(StringSegment a, StringSegment b, bool ignoreCase, CultureInfo culture)
            => Compare(a, b, (culture ?? CultureInfo.CurrentCulture).CompareInfo, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);

        #endregion

        #region Internal Methods

        internal static bool EqualsOrdinalIgnoreCase(StringSegment a, StringSegment b)
        {
            if (ReferenceEquals(a.str, b.str) && a.offset == b.offset)
                return true;
            if (a.length != b.length || a.str == null || b.str == null)
                return false;

            // TODO performance String.Equals vs String.Compare
            if (a.str.Length == a.length && b.str.Length == b.length)
                return String.Equals(a.str, b.str, StringComparison.OrdinalIgnoreCase);

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
            // TODO: fix for length
            return String.Compare(a.str, a.offset, b.str, b.offset, a.length, StringComparison.OrdinalIgnoreCase) == 0;
            // TODO
            return a.str.AsSpan(a.offset, a.length).Equals(b.str.AsSpan(b.offset, b.length), StringComparison.OrdinalIgnoreCase);
#else
            for (int i = 0; i < a.length; i++)
            {
                if (Char.ToUpperInvariant(a.GetCharInternal(i)) != Char.ToUpperInvariant(b.GetCharInternal(i)))
                    return false;
            }

            return true;
#endif
        }

        internal static int Compare(StringSegment a, StringSegment b, CompareInfo compareInfo, CompareOptions options)
        {
            if (a.str == null || b.str == null)
            {
                // They are both null
                if (ReferenceEquals(a.str, b.str))
                    return 0;

                return a.str == null ? -1 : 1;
            }

            return compareInfo.Compare(a.str, a.offset, a.length, b.str, b.offset, b.length, options);
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Indicates whether the current <see cref="StringSegment"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// </summary>
        /// <param name="other">A <see cref="StringSegment"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public bool Equals(StringSegment other)
        {
            if (ReferenceEquals(str, other.str) && offset == other.offset)
                return true;
            if (length != other.length || str == null || other.str == null)
                return false;

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0)
            // TODO performance String.Equals vs String.Compare
            // It would be better by Vector but that needs a char->ushort conversion
            if (length >= 16)
                return String.Compare(str, offset, other.str, other.offset, length, StringComparison.Ordinal) == 0;
            //return str.AsSpan(offset, length).SequenceEqual(other.str.AsSpan(other.offset, other.length));
#endif

            for (int i = 0; i < length; i++)
            {
                if (GetCharInternal(i) != other.GetCharInternal(i))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj)
            => obj == null ? IsNull
            : obj is StringSegment other ? Equals(other)
            : obj is string s && Equals(s);

        /// <summary>
        /// Returns a hash code for this <see cref="StringSegment"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            if (str == null)
                return 0;

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1)
            if (length >= 16)
                return String.GetHashCode(Span);
#endif

            // For shorter strings this is a much cheaper hash code than the one used by string.
            // This does not use a randomized hash but is used only for short strings anyway.
            var result = 13;
            for (int i = 0; i < length; i++)
                result = result * 397 + GetCharInternal(i);

            return result;
        }

        public int GetHashCode(StringComparison comparison)
        {
            switch (comparison)
            {
                case StringComparison.Ordinal:
                    return GetHashCode();
                case StringComparison.OrdinalIgnoreCase:
                    return GetHashCodeOrdinalIgnoreCase();

#if NET35 || NET40 || NET45
                case StringComparison.CurrentCulture:
                    return StringSegmentComparer.CurrentCulture.GetHashCode(this);
                case StringComparison.CurrentCultureIgnoreCase:
                    return StringSegmentComparer.CurrentCultureIgnoreCase.GetHashCode(this);
                case StringComparison.InvariantCulture:
                    return StringSegmentComparer.InvariantCulture.GetHashCode(this);
                case StringComparison.InvariantCultureIgnoreCase:
                    return StringSegmentComparer.InvariantCultureIgnoreCase.GetHashCode(this);
#else
                case StringComparison.CurrentCulture:
                    return GetHashCode(CultureInfo.CurrentCulture.CompareInfo, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return GetHashCode(CultureInfo.CurrentCulture.CompareInfo, CompareOptions.IgnoreCase);
                case StringComparison.InvariantCulture:
                    return GetHashCode(CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return GetHashCode(CultureInfo.InvariantCulture.CompareInfo, CompareOptions.IgnoreCase);
#endif
                default:
                    Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);
                    return default;
            }
        }

        /// <summary>
        /// Compares this instance to a specified <see cref="StringSegment"/> using ordinal comparison, and indicates whether this instance precedes, follows, or appears in the same position in the sort order as the specified <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="other">The <see cref="StringSegment"/> to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates whether this instance precedes, follows, or appears in the same position in the sort order as the <paramref name="other"/> parameter.</returns>
        /// <remarks><note>Unlike the <see cref="string.CompareTo(string)">String.CompareTo</see></note> method, this one performs an ordinal comparison.
        /// Use the </remarks>
        public int CompareTo(StringSegment other)
        {
            if (str == null || other.str == null)
            {
                // They are both null
                if (ReferenceEquals(str, other.str))
                    return 0;

                return str == null ? -1 : 1;
            }

            int result = String.CompareOrdinal(str, offset, other.str, other.offset, Math.Min(length, other.length));
            return result == 0 ? length - other.length : result;
        }

        /// <summary>
        /// Gets a <see cref="string">string</see> that is represented by this <see cref="StringSegment"/> instance, or <see langword="null"/>, if
        /// this instance represents a <see langword="null"/>&#160;<see cref="string">string</see>. That is, when the <see cref="IsNull"/> property returns <see langword="true"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string">string</see> that is represented by this <see cref="StringSegment"/> instance, or <see langword="null"/>, if
        /// this instance was created from a <see langword="null"/>&#160;<see cref="string">string</see>.
        /// </returns>
        /// <returns>
        /// <note>As opposed to the usual <a href="https://docs.microsoft.com/en-us/dotnet/api/system.object.tostring#notes-to-inheritors" target="_blank">ToString guidelines</a>
        /// this method can return <see cref="String.Empty">String.Empty</see> or even <see langword="null"/>.</note>
        /// </returns>
        /// <para>Once this method is called the internally stored string is replaced to the segment this <see cref="StringSegment"/> represents
        /// so calling this method repeatedly does not allocate a new string again and again.</para>
        public override string ToString() => ToString(true);

        public string ToString(bool normalize)
        {
            if (str == null)
                return null;
            if (length == str.Length)
                return str;
            string result = str.Substring(offset, length);
            if (normalize)
                this = result;
            return result;
        }

        /// <summary>
        /// Compares this instance with a specified <see cref="object">object</see> and indicates whether this instance precedes,
        /// follows, or appears in the same position in the sort order as the specified object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj is StringSegment ss)
                return CompareTo(ss);
            if (obj is string s)
                return CompareTo(s);
            if (obj == null)
                return CompareTo(Null);
            Throw.ArgumentException(Argument.obj, Res.NotAnInstanceOfType(typeof(StringSegment)));
            return default;
        }

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
        public StringSegment Substring(int offset) => Substring(this.offset + offset, length - offset);

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal char GetCharInternal(int index) => str[offset + index];

#if !(NET35 || NET40 || NET45)
        internal int GetHashCode(CompareInfo compareInfo, CompareOptions options)
        {
            if (str == null)
                return 0;
#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1)
            return compareInfo.GetHashCode(Span, options);
#else
            return compareInfo.GetHashCode(ToString(), options);
#endif
        } 
#endif

        internal int GetHashCodeOrdinalIgnoreCase()
        {
            if (str == null)
                return 0;

#if !(NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1)
            return String.GetHashCode(Span, StringComparison.OrdinalIgnoreCase);
#else
            var result = 13;
            for (int i = 0; i < length; i++)
                result = result * 397 + Char.ToUpperInvariant(GetCharInternal(i));

            return result;
#endif
        }

        internal void TrimInternal()
        {
            int start = 0;
            while (start < length && Char.IsWhiteSpace(GetCharInternal(start)))
                start += 1;
            SliceInternal(start);

            int end = length - 1;
            while (end >= 0 && Char.IsWhiteSpace(GetCharInternal(end)))
                end -= 1;
            SliceInternal(0, end + 1);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment SubstringInternal(int start, int length) =>
            new StringSegment(str, offset + start, length);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment SubstringInternal(int start) =>
            new StringSegment(str, offset + start, length - start);

        /// <summary>
        /// Similar to <see cref="SubstringInternal(int,int)"/> but mutates self instance.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal void SliceInternal(int start, int length)
        {
            offset += start;
            this.length = length;
        }

        /// <summary>
        /// Similar to <see cref="SubstringInternal(int)"/> but mutates self instance.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal void SliceInternal(int start)
        {
            offset += start;
            length -= start;
        }

        internal bool TryParseIntQuick(bool allowNegative, ulong max, out ulong result)
        {
            Debug.Assert(length > 0, $"Nonzero length is expected in {nameof(TryParseIntQuick)}");

            result = 0UL;
            bool isNegative = false;
            int i = 0;

            switch (GetCharInternal(0))
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
            while (i < length)
            {
                uint digit = GetCharInternal(i) - (uint)'0';
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

            if (len >= length)
            {
                if (len != length)
                    return -1;
                if (offset == 0)
                    return s == str ? 0 : -1;
            }

            char first = s[0];

            // single char separator: the simple way
            if (len == 1)
            {
                for (int i = offset; i < offset + length; i++)
                {
                    if (str[i] == first)
                        return i - offset;
                }

                return -1;
            }

            int end = offset + length - len + 1;
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
            if (length == 0)
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
            result = SubstringInternal(0, pos);
            SliceInternal(pos + separator.Length);
            return true;
        }

        #endregion

        #endregion

        #endregion
    }
}
