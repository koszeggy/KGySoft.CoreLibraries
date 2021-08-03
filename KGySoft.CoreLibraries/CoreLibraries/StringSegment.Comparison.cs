#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegment.Comparison.cs
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
using System.Globalization;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    partial struct StringSegment
    {
        #region Operators

        /// <summary>
        /// Determines whether two specified <see cref="StringSegment"/> instances have the same value.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(StringSegment a, StringSegment b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified <see cref="StringSegment"/> instances have different values.
        /// </summary>
        /// <param name="a">The left argument of the inequality check.</param>
        /// <param name="b">The right argument of the inequality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(StringSegment a, StringSegment b) => !a.Equals(b);

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Determines whether two specified <see cref="StringSegment"/> instances have the same value
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="a">The first <see cref="StringSegment"/> to compare.</param>
        /// <param name="b">The second <see cref="StringSegment"/> to compare.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specifies how to perform the comparison. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns><see langword="true"/>&#160;if the specified <see cref="StringSegment"/> instances are equal; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static bool Equals(StringSegment a, StringSegment b, StringComparison comparison = StringComparison.Ordinal)
            => comparison switch
            {
                StringComparison.Ordinal => a.Equals(b),
                StringComparison.OrdinalIgnoreCase => EqualsOrdinalIgnoreCase(a, b),
                _ => Compare(a, b, comparison) == 0
            };

        /// <summary>
        /// Compares two specified <see cref="StringSegment"/> instances using the specified <paramref name="comparison"/>,
        /// and returns an integer that indicates their relative position in the sort order.
        /// </summary>
        /// <param name="a">The first string to compare.</param>
        /// <param name="b">The second string to compare.</param>
        /// <param name="comparison">A <see cref="StringComparison"/> value that specifies how to perform the comparison. This parameter is optional.
        /// <br/>Default value: <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>A 32-bit signed integer that indicates the lexical relationship between the specified <see cref="StringSegment"/> instances.</returns>
        public static int Compare(StringSegment a, StringSegment b, StringComparison comparison = StringComparison.Ordinal)
        {
            if (comparison == StringComparison.Ordinal)
                return a.CompareTo(b);

            CheckComparison(comparison);
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

        /// <summary>
        /// Compares two specified <see cref="StringSegment"/> instances, ignoring or honoring their case, and using the specified <paramref name="culture"/>,
        /// and returns an integer that indicates their relative position in the sort order.
        /// </summary>
        /// <param name="a">The first string to compare.</param>
        /// <param name="b">The second string to compare.</param>
        /// <param name="ignoreCase"><see langword="true"/>&#160;to ignore case during the comparison; otherwise, <see langword="false"/>.</param>
        /// <param name="culture">An object that supplies culture-specific comparison information.
        /// if <see langword="null"/>, then <see cref="CultureInfo.CurrentCulture">CultureInfo.CurrentCulture</see> will be used.</param>
        /// <returns>A 32-bit signed integer that indicates the lexical relationship between the specified <see cref="StringSegment"/> instances.</returns>
        public static int Compare(StringSegment a, StringSegment b, bool ignoreCase, CultureInfo? culture)
            => Compare(a, b, (culture ?? CultureInfo.CurrentCulture).CompareInfo, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);

        #endregion

        #region Internal Methods

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

        #region Private Methods

        private static void CheckComparison(StringComparison comparison)
        {
            if ((uint)comparison > (uint)StringComparison.OrdinalIgnoreCase)
                Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);
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
        [MethodImpl(MethodImpl.AggressiveInlining)]
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
        public bool Equals(string? other)
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
        /// Determines whether the specified <see cref="object">object</see> is equal to this instance.
        /// </summary>
        /// <param name="obj">A <see cref="StringSegment"/> or <see cref="string">string</see> object to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified object is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
            => obj == null ? IsNull
            : obj is StringSegment other ? Equals(other)
            : obj is string s && Equals(s);

        /// <summary>
        /// Compares this instance to the specified <see cref="StringSegment"/> using ordinal comparison, and indicates whether this instance precedes, follows, or appears in the same position in the sort order as the specified <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="other">The <see cref="StringSegment"/> to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates whether this instance precedes, follows, or appears in the same position in the sort order as the <paramref name="other"/> parameter.</returns>
        /// <remarks><note>Unlike the <see cref="string.CompareTo(string)">String.CompareTo</see> method, this one performs an ordinal comparison.
        /// Use the <see cref="O:KGySoft.CoreLibraries.StringSegment.Compare">Compare</see> methods to perform a custom comparison.</note></remarks>
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
        /// Compares this instance to the specified object using ordinal comparison, and indicates whether this instance precedes, follows, or appears in the same position in the sort order as the specified <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="obj">A <see cref="StringSegment"/> or <see cref="string">string</see> object to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates whether this instance precedes, follows, or appears in the same position in the sort order as the <paramref name="obj"/> parameter.</returns>
        /// <remarks><note>Unlike the <see cref="String.CompareTo(object)">String.CompareTo</see> method, this one performs an ordinal comparison.
        /// Use the <see cref="O:KGySoft.CoreLibraries.StringSegment.Compare">Compare</see> methods to perform a custom comparison.</note></remarks>
        public int CompareTo(object? obj)
            => obj switch
            {
                StringSegment ss => CompareTo(ss),
                string s => CompareTo(s),
                null => CompareTo(Null),
                _ => Throw.ArgumentException<int>(Argument.obj, Res.NotAnInstanceOfType(typeof(StringSegment)))
            };

        #endregion

        #region Internal Methods

        internal static bool EqualsOrdinalIgnoreCase(StringSegment a, StringSegment b)
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

        #endregion

        #endregion

        #endregion
    }
}
