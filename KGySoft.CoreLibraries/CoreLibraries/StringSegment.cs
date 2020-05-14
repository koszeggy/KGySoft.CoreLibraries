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
using System.Collections;
using System.Collections.Generic;
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
    /// <para>To convert a <see cref="StringSegment"/> instance to <see cref="string">string</see> use an explicit cast or the <see cref="ToString()">ToString</see> method.</para>
    /// 
    /// TODO: .NET 3.5/4.0/4.5 .NET Standard 2.0/2.1 .NET Core 2.0: Non-ordinal GetHashCode may allocate a new string
    /// TODO: All but .NET Core 3: Non-ordinal IndexOf may allocate a new string
    /// TODO: CompareTo, IndexOf: As opposed to string default is by ordinal in these methods
    /// TODO: assign, compare string
    /// TODO: assign works even with null, IsNull is true, Length is 0
    /// </remarks>
    [Serializable]
    [SuppressMessage("Design", "CA1036:Override methods on comparable types",
        Justification = "Not implementing <, <=, >, >= operators because even string does not implement them")]
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public readonly struct StringSegment : IEquatable<StringSegment>, IComparable<StringSegment>, IComparable, IEnumerable<char>
    {
        #region Nested Types

        /// <summary>
        /// Enumerates the characters of a <see cref="StringSegment"/>.
        /// </summary>
        [Serializable]
        public struct Enumerator : IEnumerator<char>
        {
            #region Fields

            private StringSegment segment;
            private int index;
            private char current;

            #endregion

            #region Properties

            #region Public Properties

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public char Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index > segment.Length)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(in StringSegment segment)
            {
                this.segment = segment;
                index = 0;
                current = default;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Releases the enumerator
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/>&#160;if the enumerator was successfully advanced to the next element; <see langword="false"/>&#160;if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                if (index < segment.Length)
                {
                    current = segment.GetCharInternal(index);
                    index += 1;
                    return true;
                }

                current = default;
                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                index = 0;
                current = default;
            }

            #endregion
        }

        #endregion

        #region Fields

        #region Static Fields

        public static readonly StringSegment Empty = String.Empty;

        public static readonly StringSegment Null = default;

        #endregion

        #region Instance Fields

        private readonly string str;
        private readonly int offset;
        private readonly int length;

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
        public ReadOnlySpan<char> AsSpan => str.AsSpan(offset, length);

        public ReadOnlyMemory<char> AsMemory => str.AsMemory(offset, length); 
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
                if (str == null)
                    Throw.InvalidOperationException(Res.StringSegmentNull);

                // we let the ArgumentOutOfRangeException come from string, even if not localized
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
        public static explicit operator string(in StringSegment stringSegment) => stringSegment.ToString();

        public static bool operator ==(in StringSegment a, in StringSegment b) => a.Equals(b);

        public static bool operator !=(in StringSegment a, in StringSegment b) => !(a == b);

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

        public static bool Equals(in StringSegment a, in StringSegment b, StringComparison comparison = StringComparison.Ordinal)
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

        public static int Compare(in StringSegment a, in StringSegment b, StringComparison comparison = StringComparison.Ordinal)
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

        public static int Compare(in StringSegment a, in StringSegment b, bool ignoreCase, CultureInfo culture)
            => Compare(a, b, (culture ?? CultureInfo.CurrentCulture).CompareInfo, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);

        #endregion

        #region Internal Methods

        internal static bool EqualsOrdinalIgnoreCase(in StringSegment a, in StringSegment b)
        {
            if (ReferenceEquals(a.str, b.str) && a.offset == b.offset)
                return true;
            if (a.length != b.length || a.str == null || b.str == null)
                return false;

            if (a.str.Length == a.length && b.str.Length == b.length)
                return String.Equals(a.str, b.str, StringComparison.OrdinalIgnoreCase);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            for (int i = 0; i < a.length; i++)
            {
                if (Char.ToUpperInvariant(a.GetCharInternal(i)) != Char.ToUpperInvariant(b.GetCharInternal(i)))
                    return false;
            }

            return true;
#else
            // for ordinal ignore case Span.Equals is faster than String.Compare
            return a.str.AsSpan(a.offset, a.length).Equals(b.str.AsSpan(b.offset, b.length), StringComparison.OrdinalIgnoreCase);
#endif
        }

        internal static int Compare(in StringSegment a, in StringSegment b, CompareInfo compareInfo, CompareOptions options)
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

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            for (int i = 0; i < length; i++)
            {
                if (GetCharInternal(i) != other.GetCharInternal(i))
                    return false;
            }

            return true;
#else
            // for ordinal String.Compare is faster than Span.[Sequence]Equals
            return String.Compare(str, offset, other.str, other.offset, length, StringComparison.Ordinal) == 0;
#endif
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

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1
            // This does not use a randomized hash but at least this way we don't allocate a new string
            var result = 13;
            for (int i = 0; i < length; i++)
                result = result * 397 + GetCharInternal(i);

            return result;
#else
            return String.GetHashCode(AsSpan);
#endif
        }

        public int GetHashCode(StringComparison comparison)
        {
            switch (comparison)
            {
                case StringComparison.Ordinal:
                    return GetHashCode();
                case StringComparison.OrdinalIgnoreCase:
                    return GetHashCodeOrdinalIgnoreCase();

                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    return StringSegmentComparer.FromComparison(comparison).GetHashCode(this);

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
        /// Use the <see cref="Compare(StringSegment,StringSegment,StringComparison)"/> method to perform a custom comparison.</remarks>
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
        public override string ToString()
            => str == null ? null
                : length == str.Length ? str
                : str.Substring(offset, length);

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
            if (str == null)
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
        public StringSegment Substring(int offset) => Substring(this.offset + offset, length - offset);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(in StringSegment s)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (s.str == null)
                Throw.ArgumentNullException(Argument.s);
            return IndexOfInternal(s, 0, length);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(in StringSegment s, StringComparison comparison)
            => comparison == StringComparison.Ordinal ? IndexOf(s) : IndexOf(s, 0, length, comparison);

        // note: new string allocation may occur in <.NET Core 3
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(in StringSegment s, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => IndexOf(s, startIndex, length - startIndex, comparison);

        // note: new string allocation may occur in <.NET Core 3
        public int IndexOf(in StringSegment s, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (s.str == null)
                Throw.ArgumentNullException(Argument.s);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (comparison == StringComparison.Ordinal)
                return IndexOfInternal(s, startIndex, count);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            int result = str.IndexOf(s.ToString(), offset + startIndex, count, comparison);
            return result >= 0 ? result - offset : -1;
#else
            int result = AsSpan.Slice(startIndex, count).IndexOf(s.AsSpan, comparison);
            return result >= 0 ? result + startIndex : -1;
#endif
        }

        public int IndexOf(char c)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            int result = str.IndexOf(c, offset, length);
            return result >= 0 ? result - offset : -1;
        }

        public int IndexOf(char c, int startIndex)
            => IndexOf(c, startIndex, length - startIndex);

        public int IndexOf(char c, int startIndex, int count)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            int result = str.IndexOf(c, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(in StringSegment s, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(s, 0, length, comparison);

        // note: new string allocation may occur in <.NET Core 3
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(in StringSegment s, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(s, startIndex, length - startIndex, comparison);

        // note: new string allocation may occur in <.NET Core 3
        public int LastIndexOf(in StringSegment s, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (s.str == null)
                Throw.ArgumentNullException(Argument.s);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            int result = str.LastIndexOf(s.ToString(), offset + startIndex, count, comparison);
            return result >= 0 ? result - offset : -1;
#else
            int result = AsSpan.Slice(startIndex, count).LastIndexOf(s.AsSpan, comparison);
            return result >= 0 ? result + startIndex : -1;
#endif
        }

        public int LastIndexOf(char c)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            int result = str.LastIndexOf(c, offset, length);
            return result >= 0 ? result - offset : -1;
        }

        public int LastIndexOf(char c, int startIndex)
            => LastIndexOf(c, startIndex, length - startIndex);

        public int LastIndexOf(char c, int startIndex, int count)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            int result = str.LastIndexOf(c, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        public int IndexOfAny(params char[] anyOf) => IndexOfAny(anyOf, 0, length);

        public int IndexOfAny(char[] anyOf, int startIndex) => IndexOfAny(anyOf, startIndex, length - startIndex);

        public int IndexOfAny(char[] anyOf, int startIndex, int count)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            int result = str.IndexOfAny(anyOf, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="StringSegment"/> characters.
        /// </summary>
        /// <returns>An <see cref="Enumerator"/> instance that can be used to iterate though the characters of the <see cref="StringSegment"/>.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public Enumerator GetEnumerator() => new Enumerator(this);

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal char GetCharInternal(int index) => str[offset + index];

        internal int GetHashCodeOrdinalIgnoreCase()
        {
            if (str == null)
                return 0;

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1
            var result = 13;
            for (int i = 0; i < length; i++)
                result = result * 397 + Char.ToUpperInvariant(GetCharInternal(i));

            return result;
#else
            return String.GetHashCode(AsSpan, StringComparison.OrdinalIgnoreCase);
#endif
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment SubstringInternal(int start, int length) =>
            new StringSegment(str, offset + start, length);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal StringSegment SubstringInternal(int start) =>
            new StringSegment(str, offset + start, length - start);

        internal int IndexOfInternal(in StringSegment s, int startIndex, int count)
        {
            // this is the less optimized version than the IndexOfInternal(string) method but it is still faster than str.IndexOf
            Debug.Assert((uint)startIndex <= (uint)length && startIndex + count <= length);

            int len = s.length;
            if (len == 0)
                return startIndex;

            if (len >= count)
            {
                if (len != count)
                    return -1;
                if (count == length)
                {
                    Debug.Assert(startIndex == 0);
                    return Equals(s) ? 0 : -1;
                }
            }

            char first = s.GetCharInternal(0);
            int start = offset + startIndex;
            int end;

            // searching for a single char: the simple way
            if (len == 1)
            {
                end = start + count;
                for (int i = offset + startIndex; i < end; i++)
                {
                    if (str[i] == first)
                        return i - offset;
                }

                return -1;
            }

            end = start + count - len + 1;
            for (int i = offset + startIndex; i < end; i++)
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

                // Here we have full match. As single char patterns are not handled here we could have
                // check this into the inner loop to avoid goto but that requires an extra condition.
                return i - offset;

            continueOuter:;
            }

            return -1;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        int IComparable.CompareTo(object obj)
            => obj switch
            {
                StringSegment ss => CompareTo(ss),
                string s => CompareTo(s),
                null => CompareTo(Null),
                _ => Throw.ArgumentException<int>(Argument.obj, Res.NotAnInstanceOfType(typeof(StringSegment)))
            };

        IEnumerator<char> IEnumerable<char>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion

        #endregion
    }
}
