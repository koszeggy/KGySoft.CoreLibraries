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

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a <see cref="StringSegment"/> comparison operation that uses specific case and culture-based or ordinal comparison rules.
    /// </summary>
    [Serializable]
    public abstract class StringSegmentComparer : IEqualityComparer<StringSegment>, IComparer<StringSegment>, IEqualityComparer, IComparer
    {
        #region Nested classes

        #region StringSegmentOrdinalComparer class

        [Serializable]
        private sealed class StringSegmentOrdinalComparer : StringSegmentComparer
        {
            #region Methods

            public override bool Equals(StringSegment x, StringSegment y) => x.Equals(y);
            public override int GetHashCode(StringSegment obj) => obj.GetHashCode();
            public override int Compare(StringSegment x, StringSegment y) => x.CompareTo(y);

            #endregion
        }

        #endregion

        #region StringSegmentOrdinalIgnoreCaseComparer class

        [Serializable]
        private sealed class StringSegmentOrdinalIgnoreCaseComparer : StringSegmentComparer
        {
            #region Methods

            public override bool Equals(StringSegment x, StringSegment y) => StringSegment.EqualsOrdinalIgnoreCase(x, y);
            public override int GetHashCode(StringSegment obj) => obj.GetHashCodeOrdinalIgnoreCase();
            public override int Compare(StringSegment x, StringSegment y) => StringSegment.Compare(x, y, StringComparison.OrdinalIgnoreCase);

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

            #endregion

            #region Constructors

            internal StringSegmentCultureAwareComparer(CultureInfo culture, bool ignoreCase)
            {
                if (culture == null)
                    Throw.ArgumentNullException(Argument.culture);
                compareInfo = culture.CompareInfo;
                options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
#if NET35 || NET40 || NET45
                stringComparer = StringComparer.Create(culture, ignoreCase);
#endif
            }

            #endregion

            #region Methods

            public override bool Equals(StringSegment x, StringSegment y) => StringSegment.Compare(x, y, compareInfo, options) == 0;
            public override int Compare(StringSegment x, StringSegment y) => StringSegment.Compare(x, y, compareInfo, options);

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

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private static StringSegmentComparer ordinalComparer;
        private static StringSegmentComparer ordinalIgnoreCaseComparer;
        private static StringSegmentComparer invariantComparer;
        private static StringSegmentComparer invariantIgnoreCaseComparer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive ordinal <see cref="StringSegment"/> comparison.
        /// </summary>
        public static StringSegmentComparer Ordinal => ordinalComparer ??= new StringSegmentOrdinalComparer();

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive ordinal <see cref="StringSegment"/> comparison.
        /// </summary>
        public static StringSegmentComparer OrdinalIgnoreCase => ordinalIgnoreCaseComparer ??= new StringSegmentOrdinalIgnoreCaseComparer();

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive string comparison using the word comparison rules of the invariant culture.
        /// </summary>
        public static StringSegmentComparer InvariantCulture => invariantComparer ??= new StringSegmentCultureAwareComparer(CultureInfo.InvariantCulture, false);

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive string comparison using the word comparison rules of the invariant culture.
        /// </summary>
        public static StringSegmentComparer InvariantCultureIgnoreCase => invariantIgnoreCaseComparer ??= new StringSegmentCultureAwareComparer(CultureInfo.InvariantCulture, true);

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive string comparison using the word comparison rules of the current culture.
        /// </summary>
        public static StringSegmentComparer CurrentCulture => new StringSegmentCultureAwareComparer(CultureInfo.CurrentCulture, false);

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs case-insensitive string comparisons using the word comparison rules of the current culture.
        /// </summary>
        public static StringSegmentComparer CurrentCultureIgnoreCase => new StringSegmentCultureAwareComparer(CultureInfo.CurrentCulture, true);

        #endregion

        #region Methods

        #region Static Methods

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> instance based on the specified <paramref name="comparison"/>.
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
        /// </summary>
        /// <param name="culture">A culture whose linguistic rules are used to perform a string comparison.</param>
        /// <param name="ignoreCase"><see langword="true"/>&#160;to specify that comparison operations be case-insensitive;
        /// <see langword="false"/>&#160;to specify that comparison operations be case-sensitive.</param>
        /// <returns>A new <see cref="StringSegmentComparer"/> object that performs string comparisons according to the comparison rules used by
        /// the <paramref name="culture"/> parameter and the case rule specified by the <paramref name="ignoreCase"/> parameter.</returns>
        public static StringSegmentComparer Create(CultureInfo culture, bool ignoreCase) => new StringSegmentCultureAwareComparer(culture, ignoreCase);

        #endregion

        #region Instance Methods

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

        /// <summary>
        /// When overridden in a derived class, indicates whether two objects are equal.
        /// </summary>
        /// <param name="x">An object to compare to <paramref name="y"/>.</param>
        /// <param name="y">An object to compare to <paramref name="x"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="x"/> and <paramref name="y"/> refer to the same object, or <paramref name="x"/> and <paramref name="y"/> are both
        /// the same type of object and those objects are equal, or both <paramref name="x"/> and <paramref name="y"/> are <see langword="null"/>; otherwise, <see langword="false"/>.</returns>
        public new bool Equals(object x, object y)
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
            if (obj == null)
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
        public int Compare(object x, object y)
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

        #endregion
    }
}
