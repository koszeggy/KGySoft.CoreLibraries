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
using KGySoft.Collections;
using KGySoft.Reflection;

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
    /// TODO: CompareTo, [Last]IndexOf/StartsWith/EndsWith: As opposed to string default is by ordinal in these methods
    /// TODO example: assign, compare string
    /// TODO example: assign works even with null, IsNull is true, Length is 0
    /// </remarks>
    [Serializable]
    [SuppressMessage("Design", "CA1036:Override methods on comparable types",
            Justification = "Not implementing <, <=, >, >= operators because even string does not implement them")]
    [DebuggerDisplay("{" + nameof(ToString) + "()}")] // to display quotes and even the null value properly
    public readonly struct StringSegment : IEquatable<StringSegment>, IComparable<StringSegment>, IComparable, IEnumerable<char>
    {
        #region Enumerator struct

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
        /// Gets the underlying string of this <see cref="StringSegment"/>.
        /// </summary>
        public string UnderlyingString => str;

        /// <summary>
        /// Gets the offset, which denotes the start position of this <see cref="StringSegment"/> within the <see cref="UnderlyingString"/>.
        /// </summary>
        public int Offset => offset;

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

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool Equals(in StringSegment a, in StringSegment b, StringComparison comparison = StringComparison.Ordinal)
            => comparison switch
            {
                StringComparison.Ordinal => a.Equals(b),
                StringComparison.OrdinalIgnoreCase => EqualsOrdinalIgnoreCase(a, b),
                _ => Compare(a, b, comparison) == 0
            };

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
                if (rest.str[i].IsWhiteSpace())
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
            if (length != other.length)
                return false;
            if (ReferenceEquals(str, other.str) && offset == other.offset)
                return true;
            if (str == null || other.str == null)
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
        /// Indicates whether the current <see cref="StringSegment"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// </summary>
        /// <param name="other">A <see cref="StringSegment"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public bool Equals(string other)
        {
            if (ReferenceEquals(str, other) && offset == 0)
                return true;
            if (str == null || other == null)
                return false;
            if (length != other.Length)
                return false;

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            for (int i = 0; i < length; i++)
            {
                if (GetCharInternal(i) != other[i])
                    return false;
            }

            return true;
#else
            // for ordinal String.Compare is faster than Span.[Sequence]Equals
            return String.Compare(str, offset, other, 0, length, StringComparison.Ordinal) == 0;
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
        /// Use the <see cref="Compare(StringSegment, StringSegment, StringComparison)"/> method to perform a custom comparison.</remarks>
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
            while (start < length && GetCharInternal(start).IsWhiteSpace())
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
            while (end >= 0 && GetCharInternal(end).IsWhiteSpace())
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

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(in StringSegment value)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            return IndexOfInternal(value, 0, length);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(string value)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (value == null)
                Throw.ArgumentNullException(Argument.value);
            return IndexOfInternal(value, 0, length);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(in StringSegment value, StringComparison comparison)
            => comparison == StringComparison.Ordinal ? IndexOf(value) : IndexOf(value, 0, length, comparison);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(in StringSegment value, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => IndexOf(value, startIndex, length - startIndex, comparison);

        public int IndexOf(in StringSegment value, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (comparison == StringComparison.Ordinal)
                return IndexOfInternal(value, startIndex, count);

            if (!comparison.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            int result = str.IndexOf(value.ToString(), offset + startIndex, count, comparison);
            return result >= 0 ? result - offset : -1;
#else
            int result = AsSpan.Slice(startIndex, count).IndexOf(value.AsSpan, comparison);
            return result >= 0 ? result + startIndex : -1;
#endif

        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(char value)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            return IndexOfInternal(value, 0, length);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(char value, int startIndex)
            => IndexOf(value, startIndex, length - startIndex);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int IndexOf(char value, int startIndex, int count)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            return IndexOfInternal(value, startIndex, count);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(in StringSegment value, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(value, 0, length, comparison);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public int LastIndexOf(in StringSegment value, int startIndex, StringComparison comparison = StringComparison.Ordinal)
            => LastIndexOf(value, startIndex, length - startIndex, comparison);

        public int LastIndexOf(in StringSegment value, int startIndex, int count, StringComparison comparison = StringComparison.Ordinal)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.value);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (!comparison.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0
            int result = str.LastIndexOf(value.ToString(), offset + startIndex, count, comparison);
            return result >= 0 ? result - offset : -1;
#else
            int result = AsSpan.Slice(startIndex, count).LastIndexOf(value.AsSpan, comparison);
            return result >= 0 ? result + startIndex : -1;
#endif

        }

        public int LastIndexOf(char value)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            int result = str.LastIndexOf(value, offset, length);
            return result >= 0 ? result - offset : -1;
        }

        public int LastIndexOf(char value, int startIndex)
            => LastIndexOf(value, startIndex, length - startIndex);

        public int LastIndexOf(char value, int startIndex, int count)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            int result = str.LastIndexOf(value, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        public int IndexOfAny(params char[] values) => IndexOfAny(values, 0, length);

        public int IndexOfAny(char[] values, int startIndex) => IndexOfAny(values, startIndex, length - startIndex);

        public int IndexOfAny(char[] values, int startIndex, int count)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (values == null)
                Throw.ArgumentNullException(Argument.values);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            return IndexOfAnyInternal(values, offset + startIndex, count);
        }

        public int LastIndexOfAny(params char[] values) => LastIndexOfAny(values, 0, length);

        public int LastIndexOfAny(char[] values, int startIndex) => LastIndexOfAny(values, startIndex, length - startIndex);

        public int LastIndexOfAny(char[] values, int startIndex, int count)
        {
            if (str == null)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (values == null)
                Throw.ArgumentNullException(Argument.values);
            if ((uint)startIndex > (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if (count < 0 || startIndex + count > length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            int result = str.LastIndexOfAny(values, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        public bool StartsWith(in StringSegment value, StringComparison comparison = StringComparison.Ordinal)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.s);
            if (!comparison.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

            int len = value.length;
            if (len > length)
                return false;
            if (len == 0)
                return true;
            return length == len
                ? Equals(this, value, comparison)
                : Equals(SubstringInternal(0, len), value, comparison);
        }

        public bool StartsWith(string value, StringComparison comparison = StringComparison.Ordinal)
        {
            if (comparison != StringComparison.Ordinal)
                return StartsWith(new StringSegment(value), comparison);

            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (value == null)
                Throw.ArgumentNullException(Argument.s);
            if (!comparison.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

            int len = value.Length;
            if (len > length)
                return false;
            if (len == 0)
                return true;

            if (comparison == StringComparison.Ordinal)
                return StartsWithInternal(value);

            return length == len
                ? Equals(this, value, comparison)
                : Equals(SubstringInternal(0, len), value, comparison);
        }

        public bool StartsWith(char value)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            return length > 0 && GetCharInternal(offset) == value;
        }

        public bool EndsWith(in StringSegment value, StringComparison comparison = StringComparison.Ordinal)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            if (value.IsNull)
                Throw.ArgumentNullException(Argument.s);
            if (!comparison.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);

            int len = value.length;
            if (len > length)
                return false;
            if (len == 0)
                return true;
            return length == len
                ? Equals(this, value, comparison)
                : Equals(SubstringInternal(length - len, len), value, comparison);
        }

        public bool EndsWith(char value)
        {
            if (IsNull)
                Throw.InvalidOperationException(Res.StringSegmentNull);
            return length > 0 && GetCharInternal(offset + length - 1) == value;
        }

        // TODO: remarks: use maxLength if you are not interested in all segments
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
                if (removeEmptyEntries && result.Count == limit && rest.length > 0 && rest[0].IsWhiteSpace())
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

        #endregion

        #region Private Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private int IndexOfInternal(char c, int startIndex, int count)
        {
            int result = str.IndexOf(c, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        private int IndexOfInternal(string s, int startIndex, int count)
        {
            Debug.Assert((uint)startIndex <= (uint)length && startIndex + count <= length);

            int len = s.Length;
            if (len == 0)
                return startIndex;

            if (len >= count)
            {
                if (len != count)
                    return -1;

                // possible shortcut if s.Length == count == this.Length
                if (count == length)
                {
                    Debug.Assert(startIndex == 0);
                    return str.Length == len
                        ? str == s ? 0 : -1
                        : Equals(s) ? 0 : -1;
                }
            }

            char first = s[0];
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
                    if (str[i + j] == s[j])
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

        private int IndexOfInternal(in StringSegment s, int startIndex, int count)
        {
            Debug.Assert((uint)startIndex <= (uint)length && startIndex + count <= length);

            int len = s.length;
            if (len == 0)
                return startIndex;

            if (len >= count)
            {
                if (len != count)
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

        private int IndexOfAnyInternal(char[] values, int startIndex, int count)
        {
            int result = str.IndexOfAny(values, offset + startIndex, count);
            return result >= 0 ? result - offset : -1;
        }

        private int IndexOfAnyInternal(StringSegment[] separators, int startIndex, int count, out int separatorIndex)
        {
            Debug.Assert(separators != null && separators.Length > 0, "Non-empty separators are expected here");

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

        private int IndexOfAnyInternal(string[] separators, int startIndex, int count, out int separatorIndex)
        {
            Debug.Assert(separators != null && separators.Length > 0, "Non-empty separators are expected here");

            for (int i = startIndex; i < count; i++)
            {
                for (int j = 0; j < separators.Length; j++)
                {
                    string separator = separators[j];
                    if (String.IsNullOrEmpty(separator))
                        continue;

                    int sepLength = separator.Length;
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
            // for ordinal String.Compare is faster than Span.[Sequence]Equals
            return String.Compare(str, offset, value, 0, value.Length, StringComparison.Ordinal) == 0;
#endif
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
