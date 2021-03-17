#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentComparer.cs
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
using System.Globalization;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
#if NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0
#pragma warning disable CS1574, CS1580 // the documentation contains types that are not available in every target
#endif

    /// <summary>
    /// Represents a string comparison operation that uses specific case and culture-based or ordinal comparison rules
    /// allowing comparing strings by <see cref="string">string</see>, <see cref="StringSegment"/> and <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> instances.
    /// <br/>See the static properties for more details.
    /// </summary>
    [Serializable]
    public abstract class StringSegmentComparer : IEqualityComparer<StringSegment>, IComparer<StringSegment>,
        IEqualityComparer<string>, IComparer<string>,
        IEqualityComparer, IComparer
    {
        #region Nested classes

        #region StringSegmentOrdinalComparer class

        [Serializable]
        private sealed class StringSegmentOrdinalComparer : StringSegmentComparer
        {
            #region Methods

            #region Public Methods

            #region String

            public override bool Equals(string? x, string? y) => x == y;

            public override int GetHashCode(string obj)
            {
                if (obj == null!)
                    Throw.ArgumentNullException(Argument.obj);
                return GetHashCodeOrdinal(obj);
            }

            public override int Compare(string? x, string? y) => String.CompareOrdinal(x, y);

            #endregion

            #region StringSegment

            public override bool Equals(StringSegment x, StringSegment y) => x.Equals(y);
            public override int GetHashCode(StringSegment obj) => obj.GetHashCode();
            public override int Compare(StringSegment x, StringSegment y) => x.CompareTo(y);

            #endregion

            #region Span
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)

            public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.SequenceEqual(y);
            public override int GetHashCode(ReadOnlySpan<char> obj) => GetHashCodeOrdinal(obj);
            public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.CompareTo(y, StringComparison.Ordinal);

#endif
            #endregion

            #endregion

            #region Internal Methods

            internal override bool Equals(StringSegment x, string? y) => x.Equals(y);
            internal override bool Equals(StringSegmentInternal x, string y) => x.Equals(y);
            internal override int GetHashCode(StringSegmentInternal obj) => obj.GetHashCode();

            #endregion

            #endregion
        }

        #endregion

        #region StringSegmentOrdinalIgnoreCaseComparer class

        [Serializable]
        private sealed class StringSegmentOrdinalIgnoreCaseComparer : StringSegmentComparer
        {
            #region Methods

            #region Public Methods

            #region String

            public override bool Equals(string? x, string? y) => String.Equals(x, y, StringComparison.OrdinalIgnoreCase);

            public override int GetHashCode(string obj)
            {
                if (obj == null!)
                    Throw.ArgumentNullException(Argument.obj);
                return GetHashCodeOrdinalIgnoreCase(obj);
            }

            public override int Compare(string? x, string? y) => String.Compare(x, y, StringComparison.OrdinalIgnoreCase);

            #endregion

            #region StringSegment

            public override bool Equals(StringSegment x, StringSegment y) => StringSegment.EqualsOrdinalIgnoreCase(x, y);
            public override int GetHashCode(StringSegment obj) => obj.GetHashCodeOrdinalIgnoreCase();
            public override int Compare(StringSegment x, StringSegment y) => StringSegment.Compare(x, y, StringComparison.OrdinalIgnoreCase);

            #endregion

            #region Span
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)

            public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.Equals(y, StringComparison.OrdinalIgnoreCase);
            public override int GetHashCode(ReadOnlySpan<char> obj) => GetHashCodeOrdinalIgnoreCase(obj);
            public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.CompareTo(y, StringComparison.OrdinalIgnoreCase);

#endif
            #endregion

            #endregion

            #region Internal Methods

            internal override bool Equals(StringSegment x, string? y) => StringSegment.EqualsOrdinalIgnoreCase(x, y);
            internal override bool Equals(StringSegmentInternal x, string y) => x.EqualsOrdinalIgnoreCase(y);
            internal override int GetHashCode(StringSegmentInternal obj) => obj.GetHashCodeOrdinalIgnoreCase();

            #endregion

            #endregion
        }

        #endregion

        #region CultureAwareComparer class

        [Serializable]
        private sealed class StringSegmentCultureAwareComparer : StringSegmentComparer
        {
            #region Fields

            private readonly CompareInfo compareInfo;
            private readonly CompareOptions options;
#if NET35 || NET40 || NET45
            private readonly StringComparer stringComparer;
#endif
#if NETSTANDARD2_1 || NETCOREAPP3_0
            private readonly StringComparison? knownComparison;
#endif

            #endregion

            #region Constructors

            internal StringSegmentCultureAwareComparer(CultureInfo culture, bool ignoreCase)
            {
                if (culture == null!)
                    Throw.ArgumentNullException(Argument.culture);
                compareInfo = culture.CompareInfo;
                options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
#if NET35 || NET40 || NET45
                stringComparer = StringComparer.Create(culture, ignoreCase);
#endif
#if NETSTANDARD2_1 || NETCOREAPP3_0
                // span comparison is not supported with a custom culture even in .NET Core 3 so using StringComparison when possible
                knownComparison = culture.Equals(CultureInfo.InvariantCulture)
                    ? ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture
                    : culture.Equals(CultureInfo.CurrentCulture)
                        ? ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture
                        : default(StringComparison?);
#endif
            }

            #endregion

            #region Methods

            #region Public Methods

            #region String

            public override bool Equals(string? x, string? y) => compareInfo.Compare(x, y, options) == 0;

            public override int GetHashCode(string obj)
            {
                if (obj == null!)
                    Throw.ArgumentNullException(Argument.obj);

#if NET35 || NET40 || NET45
                return stringComparer.GetHashCode(obj);
#else
                return compareInfo.GetHashCode(obj, options);
#endif
            }

            public override int Compare(string? x, string? y) => compareInfo.Compare(x, y, options);

            #endregion

            #region StringSegment

            public override bool Equals(StringSegment x, StringSegment y) => StringSegment.Compare(x, y, compareInfo, options) == 0;

            public override int GetHashCode(StringSegment obj)
            {
                if (obj.IsNull)
                    return 0;

#if NET35 || NET40 || NET45
                return stringComparer.GetHashCode(obj.ToString());
#elif NET472 || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1
                return compareInfo.GetHashCode(obj.ToString(), options);
#else
                return compareInfo.GetHashCode(obj.AsSpan, options);
#endif
            }

            public override int Compare(StringSegment x, StringSegment y) => StringSegment.Compare(x, y, compareInfo, options);

            #endregion

            #region Span
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)

            public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
            {
#if NETSTANDARD2_1 || NETCOREAPP3_0
                return knownComparison != null
                    ? x.Equals(y, knownComparison.Value)
                    : compareInfo.Compare(x.ToString(), y.ToString(), options) == 0;
#else
                return compareInfo.Compare(x, y, options) == 0;
#endif
            }

            public override int GetHashCode(ReadOnlySpan<char> obj)
            {
#if NETSTANDARD2_1
                return compareInfo.GetHashCode(obj.ToString(), options);
#else
                return compareInfo.GetHashCode(obj, options);
#endif
            }

            public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
            {
#if NETSTANDARD2_1 || NETCOREAPP3_0
                return knownComparison != null
                    ? x.CompareTo(y, knownComparison.Value)
                    : compareInfo.Compare(x.ToString(), y.ToString(), options);
#else
                return compareInfo.Compare(x, y, options);
#endif
            }

#endif
            #endregion

            #endregion

            #region Internal Methods

            internal override bool Equals(StringSegment x, string? y) => StringSegment.Compare(x, y, compareInfo, options) == 0;
            internal override int GetHashCode(StringSegmentInternal obj) => Throw.InternalError<int>("Not expected to be called");
            internal override bool Equals(StringSegmentInternal x, string y) => Throw.InternalError<bool>("Not expected to be called");

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Constants

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_0)
        private const int lengthThreshold = 32;
#endif
        #endregion

        #region Fields

        private static StringSegmentComparer? ordinalComparer;
        private static StringSegmentComparer? ordinalIgnoreCaseComparer;
        private static StringSegmentComparer? invariantComparer;
        private static StringSegmentComparer? invariantIgnoreCaseComparer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive ordinal string comparison.
        /// <br/>The methods of the returned <see cref="StringSegmentComparer"/> instance can be called with <see cref="string">string</see>, <see cref="StringSegment"/>
        /// and <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> parameter values, which will not allocate new strings on any platform.
        /// </summary>
        public static StringSegmentComparer Ordinal => ordinalComparer ??= new StringSegmentOrdinalComparer();

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive ordinal string comparison.
        /// <br/>The methods of the returned <see cref="StringSegmentComparer"/> instance can be called with <see cref="string">string</see>, <see cref="StringSegment"/>
        /// and <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> parameter values, which will not allocate new strings on any platform.
        /// </summary>
        public static StringSegmentComparer OrdinalIgnoreCase => ordinalIgnoreCaseComparer ??= new StringSegmentOrdinalIgnoreCaseComparer();

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive string comparison using the word comparison rules of the invariant culture.
        /// <br/>Depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> method might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.
        /// </summary>
        public static StringSegmentComparer InvariantCulture => invariantComparer ??= new StringSegmentCultureAwareComparer(CultureInfo.InvariantCulture, false);

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive string comparison using the word comparison rules of the invariant culture.
        /// <br/>Depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> method might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.
        /// </summary>
        public static StringSegmentComparer InvariantCultureIgnoreCase => invariantIgnoreCaseComparer ??= new StringSegmentCultureAwareComparer(CultureInfo.InvariantCulture, true);

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive string comparison using the word comparison rules of the current culture.
        /// <br/>Depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> method might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.
        /// </summary>
        public static StringSegmentComparer CurrentCulture => new StringSegmentCultureAwareComparer(CultureInfo.CurrentCulture, false);

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs case-insensitive string comparisons using the word comparison rules of the current culture.
        /// <br/>Depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> method might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.
        /// </summary>
        public static StringSegmentComparer CurrentCultureIgnoreCase => new StringSegmentCultureAwareComparer(CultureInfo.CurrentCulture, true);

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> instance based on the specified <paramref name="comparison"/>.
        /// <br/>Please note that the returned <see cref="StringSegmentComparer"/> may allocate new strings in some cases. See the description of the properties for more details.
        /// </summary>
        /// <param name="comparison">A <see cref="StringComparison"/> value from which a <see cref="StringSegmentComparer"/> is about to be obtained.</param>
        /// <returns>A <see cref="StringSegmentComparer"/> instance representing the equivalent value of the specified <paramref name="comparison"/> instance.</returns>
        public static StringSegmentComparer FromComparison(StringComparison comparison)
        {
            switch (comparison)
            {
                case StringComparison.Ordinal:
                    return Ordinal;
                case StringComparison.OrdinalIgnoreCase:
                    return OrdinalIgnoreCase;
                case StringComparison.CurrentCulture:
                    return CurrentCulture;
                case StringComparison.CurrentCultureIgnoreCase:
                    return CurrentCultureIgnoreCase;
                case StringComparison.InvariantCulture:
                    return InvariantCulture;
                case StringComparison.InvariantCultureIgnoreCase:
                    return InvariantCultureIgnoreCase;
                default:
                    Throw.EnumArgumentOutOfRange(Argument.comparison, comparison);
                    return default;
            }
        }

        /// <summary>
        /// Creates a <see cref="StringSegmentComparer"/> object that compares strings according to the rules of a specified <paramref name="culture"/>.
        /// <br/>Please note that the returned <see cref="StringSegmentComparer"/> may allocate new strings in some cases when targeting older frameworks.
        /// See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="culture">A culture whose linguistic rules are used to perform a comparison.</param>
        /// <param name="ignoreCase"><see langword="true"/>&#160;to specify that comparison operations be case-insensitive;
        /// <see langword="false"/>&#160;to specify that comparison operations be case-sensitive.</param>
        /// <returns>A new <see cref="StringSegmentComparer"/> object that performs string comparisons according to the comparison rules used by
        /// the <paramref name="culture"/> parameter and the case rule specified by the <paramref name="ignoreCase"/> parameter.</returns>
        /// <remarks>
        /// <para>If <paramref name="culture"/> is either the <see cref="CultureInfo.InvariantCulture"/> or the <see cref="CultureInfo.CurrentCulture"/>,
        /// then depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> and <see cref="GetHashCode(ReadOnlySpan{char})"/> methods might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.</para>
        /// <para>If <paramref name="culture"/> is any <see cref="CultureInfo"/> other than the <see cref="CultureInfo.InvariantCulture"/> and <see cref="CultureInfo.CurrentCulture"/>,
        /// then depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/>, <see cref="GetHashCode(ReadOnlySpan{char})"/>, <see cref="Equals(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
        /// and <see cref="Compare(ReadOnlySpan{char}, ReadOnlySpan{char})"/> methods might allocate a new string. In .NET Core 3.0 and above
        /// none of the members with <see cref="StringSegment"/> parameters will allocate new strings. And methods with <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> parameters
        /// (<see cref="Equals(ReadOnlySpan{char}, ReadOnlySpan{char})"/> and <see cref="Compare(ReadOnlySpan{char}, ReadOnlySpan{char})"/>) can avoid allocating strings when targeting .NET 5.0 or higher.</para>
        /// </remarks>
        public static StringSegmentComparer Create(CultureInfo culture, bool ignoreCase) => new StringSegmentCultureAwareComparer(culture, ignoreCase);

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinal(string s)
        {
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_0)
            if (s.Length > lengthThreshold)
                return s.GetHashCode();
#endif
            var result = 13;

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (int i = 0; i < s.Length; i++)
                result = result * 397 + s[i];

            return result;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinal(string s, int offset, int length)
        {
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_0)
            if (length > lengthThreshold)
                return String.GetHashCode(s.AsSpan(offset, length));
#endif
            var result = 13;
            for (int i = 0; i < length; i++)
                result = result * 397 + s[i + offset];

            return result;
        }

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinal(ReadOnlySpan<char> s)
        {
#if !NETSTANDARD2_1
            if (s.Length > lengthThreshold)
                return String.GetHashCode(s); 
#endif
            var result = 13;

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (int i = 0; i < s.Length; i++)
                result = result * 397 + s[i];

            return result;
        }
#endif

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinalIgnoreCase(string s)
        {
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_0)
            if (s.Length > lengthThreshold)
                return s.GetHashCode(StringComparison.OrdinalIgnoreCase);
#endif

            var result = 13;

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (int i = 0; i < s.Length; i++)
                result = result * 397 + Char.ToUpperInvariant(s[i]);

            return result;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinalIgnoreCase(string s, int offset, int length)
        {
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_0)
            if (length > lengthThreshold)
                return String.GetHashCode(s.AsSpan(offset, length), StringComparison.OrdinalIgnoreCase);
#endif
            var result = 13;
            for (int i = 0; i < length; i++)
                result = result * 397 + Char.ToUpperInvariant(s[i + offset]);

            return result;
        }

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> s)
        {
#if !NETSTANDARD2_1
            if (s.Length > lengthThreshold)
                return String.GetHashCode(s, StringComparison.OrdinalIgnoreCase);
#endif
            var result = 13;

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (int i = 0; i < s.Length; i++)
                result = result * 397 + Char.ToUpperInvariant(s[i]);

            return result;
        }
#endif

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        #region StringSegment

        /// <summary>
        /// When overridden in a derived class, indicates whether two <see cref="StringSegment"/> instances are equal.
        /// </summary>
        /// <param name="x">A <see cref="StringSegment"/> to compare to <paramref name="y"/>.</param>
        /// <param name="y">A <see cref="StringSegment"/> to compare to <paramref name="x"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="x"/> and <paramref name="y"/> are equal; otherwise, <see langword="false"/>.</returns>
        public abstract bool Equals(StringSegment x, StringSegment y);

        /// <summary>
        /// When overridden in a derived class, gets the hash code for the specified <see cref="StringSegment"/>.
        /// </summary>
        /// <param name="obj">The <see cref="StringSegment"/> to get the hash code for.</param>
        /// <returns>
        /// A hash code for the <see cref="StringSegment"/>, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public abstract int GetHashCode(StringSegment obj);

        /// <summary>
        /// When overridden in a derived class, compares two <see cref="StringSegment"/> instances and returns an indication of their relative sort order.
        /// </summary>
        /// <param name="x">A <see cref="StringSegment"/> to compare to <paramref name="y"/>.</param>
        /// <param name="y">A <see cref="StringSegment"/> to compare to <paramref name="x"/>.</param>
        /// <returns>
        /// A signed integer that indicates the relative order of <paramref name="x" /> and <paramref name="y" />.
        /// </returns>
        public abstract int Compare(StringSegment x, StringSegment y);

        #endregion

        #region String

        /// <summary>
        /// When overridden in a derived class, indicates whether two <see cref="string">string</see> instances are equal.
        /// </summary>
        /// <param name="x">A <see cref="string">string</see> to compare to <paramref name="y"/>.</param>
        /// <param name="y">A <see cref="string">string</see> to compare to <paramref name="x"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="x"/> and <paramref name="y"/> are equal; otherwise, <see langword="false"/>.</returns>
        public abstract bool Equals(string? x, string? y);

        /// <summary>
        /// When overridden in a derived class, gets the hash code for the specified <see cref="string">string</see>.
        /// </summary>
        /// <param name="obj">The <see cref="string">string</see> to get the hash code for.</param>
        /// <returns>
        /// A hash code for the <see cref="string">string</see>, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public abstract int GetHashCode(string obj);

        /// <summary>
        /// When overridden in a derived class, compares two <see cref="string">string</see> instances and returns an indication of their relative sort order.
        /// </summary>
        /// <param name="x">A <see cref="string">string</see> to compare to <paramref name="y"/>.</param>
        /// <param name="y">A <see cref="string">string</see> to compare to <paramref name="x"/>.</param>
        /// <returns>
        /// A signed integer that indicates the relative order of <paramref name="x" /> and <paramref name="y" />.
        /// </returns>
        public abstract int Compare(string? x, string? y);

        #endregion

        #region Object

        /// <summary>
        /// When overridden in a derived class, indicates whether two objects are equal.
        /// </summary>
        /// <param name="x">An object to compare to <paramref name="y"/>.</param>
        /// <param name="y">An object to compare to <paramref name="x"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="x"/> and <paramref name="y"/> refer to the same object, or <paramref name="x"/> and <paramref name="y"/> are both
        /// the same type of object and those objects are equal, or both <paramref name="x"/> and <paramref name="y"/> are <see langword="null"/>; otherwise, <see langword="false"/>.</returns>
        public new bool Equals(object? x, object? y)
        {
            if (x == y)
                return true;

            StringSegment a;
            if (x == null)
                a = StringSegment.Null;
            else if (x is StringSegment ss)
                a = ss;
            else if (x is string s)
                a = s;
            else
                return x.Equals(y);

            StringSegment b;
            if (y == null)
                b = StringSegment.Null;
            else if (y is StringSegment ss)
                b = ss;
            else if (y is string s)
                b = s;
            else
                return y.Equals(x);

            return Equals(a, b);
        }

        /// <summary>
        /// When overridden in a derived class, gets the hash code for the specified object.
        /// </summary>
        /// <param name="obj">An object.</param>
        /// <returns>
        /// A 32-bit signed hash code calculated from the value of the <paramref name="obj"/> parameter.
        /// </returns>
        public int GetHashCode(object obj)
        {
            // Unlike in Equals, null is not accepted here. StringSegment.Null will still work.
            if (obj == null!)
                Throw.ArgumentNullException(Argument.obj);

            return obj switch
            {
                StringSegment ss => GetHashCode(ss),
                string s => GetHashCode(s),
                _ => obj.GetHashCode()
            };
        }

        /// <summary>
        /// When overridden in a derived class, compares two objects and returns an indication of their relative sort order.
        /// </summary>
        /// <param name="x">An object to compare to <paramref name="y"/>.</param>
        /// <param name="y">An object to compare to <paramref name="x"/>.</param>
        /// <returns>
        /// A signed integer that indicates the relative order of <paramref name="x" /> and <paramref name="y" />.
        /// </returns>
        public int Compare(object? x, object? y)
        {
            if (x == y)
                return 0;

            StringSegment a;
            if (x == null)
                a = StringSegment.Null;
            else if (x is StringSegment ss)
                a = ss;
            else if (x is string s)
                a = s;
            else
                return (x as IComparable)?.CompareTo(y) ?? Throw.ArgumentException<int>(Argument.x, Res.NotAnInstanceOfType(typeof(IComparable)));

            StringSegment b;
            if (y == null)
                b = StringSegment.Null;
            else if (y is StringSegment ss)
                b = ss;
            else if (y is string s)
                b = s;
            else
                return (y as IComparable)?.CompareTo(x) ?? Throw.ArgumentException<int>(Argument.y, Res.NotAnInstanceOfType(typeof(IComparable)));

            return Compare(a, b);
        }

        #endregion

        #region Span
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)

        /// <summary>
        /// When overridden in a derived class, indicates whether two <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> instances are equal.
        /// </summary>
        /// <param name="x">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to compare to <paramref name="y"/>.</param>
        /// <param name="y">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to compare to <paramref name="x"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="x"/> and <paramref name="y"/> are equal; otherwise, <see langword="false"/>.</returns>
        public abstract bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y);

        /// <summary>
        /// When overridden in a derived class, gets the hash code for the specified <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see>.
        /// </summary>
        /// <param name="obj">The <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to get the hash code for.</param>
        /// <returns>
        /// A hash code for the <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see>, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public abstract int GetHashCode(ReadOnlySpan<char> obj);

        /// <summary>
        /// When overridden in a derived class, compares two <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> instances and returns an indication of their relative sort order.
        /// </summary>
        /// <param name="x">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to compare to <paramref name="y"/>.</param>
        /// <param name="y">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to compare to <paramref name="x"/>.</param>
        /// <returns>
        /// A signed integer that indicates the relative order of <paramref name="x" /> and <paramref name="y" />.
        /// </returns>
        public abstract int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y);

#endif
        #endregion

        #endregion

        #region Internal Methods

        #region StringSegment

        internal abstract bool Equals(StringSegment x, string? y);

        #endregion

        #region StringSegmentInternal

        internal abstract bool Equals(StringSegmentInternal x, string y);
        internal abstract int GetHashCode(StringSegmentInternal obj);

        #endregion

        #endregion

        #endregion

        #endregion
    }
}
