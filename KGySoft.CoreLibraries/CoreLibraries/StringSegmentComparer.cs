#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentComparer.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
#if NET35 || NET40 || NET45 || NETSTANDARD2_1 || NETCOREAPP2_1 || NETCOREAPP3_0
using System.Runtime.Serialization;
#endif

using KGySoft.Collections;

#endregion

#region Suppressions

#if !NET9_0_OR_GREATER
#pragma warning disable CS1574, CS1580 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a string comparison operation that uses specific case and culture-based or ordinal comparison rules
    /// allowing comparing strings by <see cref="string">string</see>, <see cref="StringSegment"/> and <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> instances.
    /// <br/>See the static properties for more details.
    /// </summary>
    /// <remarks>
    /// <para>This class exists to support lookup operations by <see cref="StringSegment"/> and <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> instances
    /// for <see cref="IStringKeyedDictionary{TValue}"/> and <see cref="IStringKeyedReadOnlyDictionary{TValue}"/> implementations, such as the <see cref="StringKeyedDictionary{TValue}"/> class.</para>
    /// <para>Actually you can use this comparer anywhere where you would just use a <see cref="StringComparer"/>.
    /// Some members may provide bette performance or improved functionality than the regular <see cref="StringComparer"/> members, especially on older platforms.</para>
    /// <note type="tip">Starting with .NET 9.0 this class implements the <see cref="IAlternateEqualityComparer{TAlternate,T}"/> interface, supporting both <see cref="StringSegment"/>
    /// and <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> types. This makes possible to use a regular <see cref="Dictionary{TKey,TValue}"/> class
    /// with <see cref="StringSegment"/> lookup if you initialize it with a <see cref="StringSegmentComparer"/> instance (see the static members) and use
    /// the <see cref="System.Collections.Generic.CollectionExtensions.GetAlternateLookup{TKey,TValue,TAlternateKey}">GetAlternateLookup</see> extension,
    /// specifying <see cref="StringSegment"/> for the <c>TAlternateKey</c> generic argument. Such an extension exists for the <see cref="HashSet{T}"/> class, too.</note>
    /// </remarks>
    [Serializable]
    public abstract class StringSegmentComparer : IEqualityComparer<StringSegment>, IComparer<StringSegment>,
        IEqualityComparer<string>, IComparer<string>,
#if NET9_0_OR_GREATER
        IAlternateEqualityComparer<StringSegment, string?>, IAlternateEqualityComparer<ReadOnlySpan<char>, string?>,
#endif
        IEqualityComparer, IComparer
    {
        #region Nested classes

        #region StringSegmentOrdinalComparer class

        [Serializable]
        private class StringSegmentOrdinalComparer : StringSegmentComparer
        {
            #region Fields

            internal static readonly StringSegmentOrdinalComparer Instance = new StringSegmentOrdinalComparer();

            #endregion

            #region Properties

            public override CompareOptions CompareOptions => CompareOptions.Ordinal;

            #endregion

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
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

            public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.SequenceEqual(y);
            public override int GetHashCode(ReadOnlySpan<char> obj) => GetHashCodeOrdinal(obj);
            public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.CompareTo(y, StringComparison.Ordinal);

#endif
            #endregion

            #region Object

            public override bool Equals(object? obj) => obj?.GetType() == typeof(StringSegmentOrdinalComparer);
            public override int GetHashCode() => typeof(StringSegmentOrdinalComparer).GetHashCode();

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
        private class StringSegmentOrdinalIgnoreCaseComparer : StringSegmentComparer
        {
            #region Fields

            internal static readonly StringSegmentOrdinalIgnoreCaseComparer Instance = new StringSegmentOrdinalIgnoreCaseComparer();

            #endregion

            #region Properties

            public override CompareOptions CompareOptions => CompareOptions.OrdinalIgnoreCase;

            #endregion

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
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

            public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.Equals(y, StringComparison.OrdinalIgnoreCase);
            public override int GetHashCode(ReadOnlySpan<char> obj) => GetHashCodeOrdinalIgnoreCase(obj);
            public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.CompareTo(y, StringComparison.OrdinalIgnoreCase);

#endif
            #endregion

            #region Object

            public override bool Equals(object? obj) => obj?.GetType() == typeof(StringSegmentOrdinalIgnoreCaseComparer);
            public override int GetHashCode() => typeof(StringSegmentOrdinalIgnoreCaseComparer).GetHashCode();

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

            #region Static Fields

            internal static readonly StringSegmentCultureAwareComparer Invariant = new StringSegmentCultureAwareComparer(CultureInfo.InvariantCulture, false);
            internal static readonly StringSegmentCultureAwareComparer InvariantIgnoreCase = new StringSegmentCultureAwareComparer(CultureInfo.InvariantCulture, true);

            #endregion

            #region Instance Fields

            private readonly CompareInfo compareInfo;
            private readonly CompareOptions options;
#if NET35 || NET40 || NET45
            [NonSerialized]private StringComparer stringComparer = null!;
#endif
#if NETSTANDARD2_1 || NETCOREAPP2_1 || NETCOREAPP3_0
            [NonSerialized]private StringComparison? knownComparison;
#endif

            #endregion

            #endregion

            #region Properties

            public override CompareInfo CompareInfo => compareInfo;
            public override CompareOptions CompareOptions => options;

            #endregion

            #region Constructors

#if NET35 || NET40 || NET45
            internal StringSegmentCultureAwareComparer(CultureInfo culture, bool ignoreCase)
            {
                if (culture == null!)
                    Throw.ArgumentNullException(Argument.culture);
                compareInfo = culture.CompareInfo;
                options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;
                InitComparer(culture, ignoreCase);
            }
#else
            internal StringSegmentCultureAwareComparer(CultureInfo culture, bool ignoreCase)
                : this(culture, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None)
            {
            }

            internal StringSegmentCultureAwareComparer(CultureInfo culture, CompareOptions options)
            {
                if (culture == null!)
                    Throw.ArgumentNullException(Argument.culture);
                if (!options.AllFlagsDefined())
                    Throw.FlagsEnumArgumentOutOfRange(Argument.options, options);
                compareInfo = culture.CompareInfo;
                this.options = options;
#if NETSTANDARD2_1 || NETCOREAPP2_1 || NETCOREAPP3_0
                if (options is CompareOptions.None or CompareOptions.IgnoreCase)
                    InitComparison(culture, options == CompareOptions.IgnoreCase);
#endif
            }
#endif

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

#if NETCOREAPP3_0_OR_GREATER
                return compareInfo.GetHashCode(obj.AsSpan, options);
#elif NET46_OR_GREATER || NETCOREAPP || NETSTANDARD
                return compareInfo.GetHashCode(obj.ToString()!, options);
#else
                return stringComparer.GetHashCode(obj.ToString()!);
#endif
            }

            public override int Compare(StringSegment x, StringSegment y) => StringSegment.Compare(x, y, compareInfo, options);

            #endregion

            #region Span
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

            public override bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
            {
#if NET5_0_OR_GREATER
                return compareInfo.Compare(x, y, options) == 0;
#else
                return knownComparison != null
                    ? x.Equals(y, knownComparison.Value)
                    : compareInfo.Compare(x.ToString(), y.ToString(), options) == 0;
#endif
            }

            public override int GetHashCode(ReadOnlySpan<char> obj)
            {
#if NETCOREAPP3_0_OR_GREATER
                return compareInfo.GetHashCode(obj, options);
#else
                return compareInfo.GetHashCode(obj.ToString(), options);
#endif
            }

            public override int Compare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
            {
#if NET5_0_OR_GREATER
                return compareInfo.Compare(x, y, options);
#else
                return knownComparison != null
                    ? x.CompareTo(y, knownComparison.Value)
                    : compareInfo.Compare(x.ToString(), y.ToString(), options);
#endif
            }

#endif
            #endregion

            #region Object

            public override bool Equals(object? obj) => obj is StringSegmentCultureAwareComparer other
                && options == other.options
                && compareInfo.Equals(other.compareInfo);

            public override int GetHashCode() => compareInfo.GetHashCode() ^ (int)(options & CompareOptions.IgnoreCase);

            #endregion

            #endregion

            #region Internal Methods

            internal override bool Equals(StringSegment x, string? y) => StringSegment.Compare(x, y, compareInfo, options) == 0;
            internal override int GetHashCode(StringSegmentInternal obj) => Throw.InternalError<int>("Not expected to be called");
            internal override bool Equals(StringSegmentInternal x, string y) => Throw.InternalError<bool>("Not expected to be called");

            #endregion

            #region Private Methods

#if NET35 || NET40 || NET45
            private void InitComparer(CultureInfo culture, bool ignoreCase) => stringComparer = StringComparer.Create(culture, ignoreCase);

            [OnDeserialized]
            private void OnDeserialized(StreamingContext ctx)
                => InitComparer(CultureInfo.GetCultureInfo(compareInfo.Name), options == CompareOptions.IgnoreCase);
#endif

#if NETSTANDARD2_1 || NETCOREAPP2_1 || NETCOREAPP3_0
            private void InitComparison(CultureInfo culture, bool ignoreCase)
            {
                // span comparison is not supported with a custom culture even in .NET Core 3 so using StringComparison when possible
                knownComparison = culture.Equals(CultureInfo.InvariantCulture)
                    ? ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture
                    : culture.Equals(CultureInfo.CurrentCulture)
                        ? ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture
                        : default(StringComparison?);
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext ctx)
                => InitComparison(CultureInfo.GetCultureInfo(compareInfo.Name), options == CompareOptions.IgnoreCase);
#endif

            #endregion

            #endregion
        }

        #endregion

        #region StringSegmentOrdinalRandomizedComparer

        [Serializable]
        private sealed class StringSegmentOrdinalRandomizedComparer : StringSegmentOrdinalComparer
        {
            #region Fields

            #region Fields

            internal static new readonly StringSegmentOrdinalRandomizedComparer Instance = new StringSegmentOrdinalRandomizedComparer();

            #endregion

            #region Instance Fields

#if !NETCOREAPP3_0_OR_GREATER
            private readonly int seed;
            private readonly int factor1;
            private readonly int factor2;
#endif

            #endregion

            #endregion

            #region Constructors

#if !NETCOREAPP3_0_OR_GREATER
            internal StringSegmentOrdinalRandomizedComparer()
            {
                Random rnd = ThreadSafeRandom.Instance;
                seed = HashHelper.GetNextPrime(rnd.Next(11, 1 << 30));
                factor1 = HashHelper.GetNextPrime(rnd.Next(11, 1 << 16));
                factor2 = HashHelper.GetNextPrime(rnd.Next(1 << 16, 1 << 30));
            }
#endif

            #endregion

            #region Methods

            #region Public Methods

            public override int GetHashCode(string obj)
            {
                if (obj == null!)
                    Throw.ArgumentNullException(Argument.obj);
#if NETCOREAPP3_0_OR_GREATER
                return obj.GetHashCode();
#else
                return GetHashCode(obj, 0, obj.Length);
#endif
            }

            public override int GetHashCode(StringSegment obj)
            {
                if (obj.IsNull)
                    return 0;
#if NETCOREAPP3_0_OR_GREATER
                return String.GetHashCode(obj.AsSpan);
#else
                return GetHashCode(obj.UnderlyingString!, obj.Offset, obj.Length);
#endif
            }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            public override int GetHashCode(ReadOnlySpan<char> s)
            {
#if NETCOREAPP3_0_OR_GREATER
                return String.GetHashCode(s);
#else
                // using two factors to avoid possible bucket size match of a consumer collection, in which case the hash code would depend on the last char only
                int result = seed;
                for (int i = 0; i < s.Length; i++)
                    result = result * ((i & 1) == 0 ? factor1 : factor2) + s[i];
                return result;
#endif
            }
#endif

            public override bool Equals(object? obj) => obj is StringSegmentOrdinalRandomizedComparer;
            public override int GetHashCode() => typeof(StringSegmentOrdinalRandomizedComparer).GetHashCode();

            #endregion

            #region Internal Methods

            internal override int GetHashCode(StringSegmentInternal obj) =>
#if NETCOREAPP3_0_OR_GREATER
                String.GetHashCode(obj.String.AsSpan(obj.Offset, obj.Length));
#else
                GetHashCode(obj.String, obj.Offset, obj.Length);
#endif

            #endregion

            #region Private Methods

#if !NETCOREAPP3_0_OR_GREATER
            private int GetHashCode(string s, int offset, int length)
            {
                // using two factors to avoid possible bucket size match of a consumer collection, in which case the hash code would depend on the last char only
                int result = seed;
                for (int i = offset; i < length; i++)
                    result = result * ((i & 1) == 0 ? factor1 : factor2) + s[i];
                return result;
            } 
#endif

            #endregion

            #endregion
        }

        #endregion

        #region StringSegmentOrdinalIgnoreCaseRandomizedComparer

        [Serializable]
        private sealed class StringSegmentOrdinalIgnoreCaseRandomizedComparer : StringSegmentOrdinalIgnoreCaseComparer
        {
            #region Fields

            #region Static Fields

            internal static new readonly StringSegmentOrdinalIgnoreCaseRandomizedComparer Instance = new StringSegmentOrdinalIgnoreCaseRandomizedComparer();

            #endregion

            #region Instance Fields

#if !NETCOREAPP3_0_OR_GREATER
            private readonly int seed;
            private readonly int factor1;
            private readonly int factor2;
#endif

            #endregion

            #endregion

            #region Constructors

#if !NETCOREAPP3_0_OR_GREATER
            internal StringSegmentOrdinalIgnoreCaseRandomizedComparer()
            {
                Random rnd = ThreadSafeRandom.Instance;
                seed = HashHelper.GetNextPrime(rnd.Next(11, 1 << 30));
                factor1 = HashHelper.GetNextPrime(rnd.Next(11, 1 << 16));
                factor2 = HashHelper.GetNextPrime(rnd.Next(1 << 16, 1 << 30));
            }
#endif

            #endregion

            #region Methods

            #region Public Methods

            public override int GetHashCode(string obj)
            {
                if (obj == null!)
                    Throw.ArgumentNullException(Argument.obj);
#if NETCOREAPP3_0_OR_GREATER
                return obj.GetHashCode(StringComparison.OrdinalIgnoreCase);
#else
                return GetHashCode(obj, 0, obj.Length);
#endif
            }

            public override int GetHashCode(StringSegment obj)
            {
                if (obj.IsNull)
                    return 0;
#if NETCOREAPP3_0_OR_GREATER
                return String.GetHashCode(obj.AsSpan, StringComparison.OrdinalIgnoreCase);
#else
                return GetHashCode(obj.UnderlyingString!, obj.Offset, obj.Length);
#endif
            }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            public override int GetHashCode(ReadOnlySpan<char> s)
            {
#if NETCOREAPP3_0_OR_GREATER
                return String.GetHashCode(s, StringComparison.OrdinalIgnoreCase);
#else
                // using two factors to avoid possible bucket size match of a consumer collection, in which case the hash code would depend on the last char only
                int result = seed;
                for (int i = 0; i < s.Length; i++)
                    result = result * ((i & 1) == 0 ? factor1 : factor2) + Char.ToUpperInvariant(s[i]);
                return result;
#endif
            }
#endif

            public override bool Equals(object? obj) => obj is StringSegmentOrdinalIgnoreCaseRandomizedComparer;
            public override int GetHashCode() => typeof(StringSegmentOrdinalIgnoreCaseRandomizedComparer).GetHashCode();

            #endregion

            #region Internal Methods

            internal override int GetHashCode(StringSegmentInternal obj) =>
#if NETCOREAPP3_0_OR_GREATER
                String.GetHashCode(obj.String.AsSpan(obj.Offset, obj.Length), StringComparison.OrdinalIgnoreCase);
#else
                GetHashCode(obj.String, obj.Offset, obj.Length);
#endif

            #endregion

            #region Private Methods

#if !NETCOREAPP3_0_OR_GREATER
            private int GetHashCode(string s, int offset, int length)
            {
                // using two factors to avoid possible bucket size match of a consumer collection, in which case the hash code would depend on the last char only
                int result = seed;
                for (int i = offset; i < length; i++)
                    result = result * ((i & 1) == 0 ? factor1 : factor2) + Char.ToUpperInvariant(s[i]);
                return result;
            } 
#endif

            #endregion

            #endregion
        }

        #endregion

        #region StringSegmentOrdinalNonRandomizedComparer

        [Serializable]
        private sealed class StringSegmentOrdinalNonRandomizedComparer : StringSegmentOrdinalComparer
        {
            #region Fields

            internal static new readonly StringSegmentOrdinalNonRandomizedComparer Instance = new StringSegmentOrdinalNonRandomizedComparer();

            #endregion

            #region Methods

            #region Public Methods

            public override int GetHashCode(string obj)
            {
                if (obj == null!)
                    Throw.ArgumentNullException(Argument.obj);
                return GetHashCodeOrdinalNonRandomized(obj);
            }

            public override int GetHashCode(StringSegment obj) => obj.IsNull ? 0 : GetHashCodeOrdinalNonRandomized(obj.UnderlyingString!, obj.Offset, obj.Length);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            public override int GetHashCode(ReadOnlySpan<char> obj) => GetHashCodeOrdinalNonRandomized(obj);
#endif

            public override bool Equals(object? obj) => obj is StringSegmentOrdinalNonRandomizedComparer;
            public override int GetHashCode() => typeof(StringSegmentOrdinalNonRandomizedComparer).GetHashCode();

            #endregion

            #region Internal Methods

            internal override int GetHashCode(StringSegmentInternal obj) => GetHashCodeOrdinalNonRandomized(obj.String, obj.Offset, obj.Length);

            #endregion

            #endregion
        }

        #endregion

        #region StringSegmentOrdinalIgnoreCaseNonRandomizedComparer

        [Serializable]
        private sealed class StringSegmentOrdinalIgnoreCaseNonRandomizedComparer : StringSegmentOrdinalComparer
        {
            #region Fields

            internal static new readonly StringSegmentOrdinalIgnoreCaseNonRandomizedComparer Instance = new StringSegmentOrdinalIgnoreCaseNonRandomizedComparer();

            #endregion

            #region Methods

            #region Public Methods

            public override int GetHashCode(string obj)
            {
                if (obj == null!)
                    Throw.ArgumentNullException(Argument.obj);
                return GetHashCodeOrdinalIgnoreCaseNonRandomized(obj);
            }

            public override int GetHashCode(StringSegment obj) => obj.IsNull ? 0 : GetHashCodeOrdinalIgnoreCaseNonRandomized(obj.UnderlyingString!, obj.Offset, obj.Length);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            public override int GetHashCode(ReadOnlySpan<char> obj) => GetHashCodeOrdinalIgnoreCaseNonRandomized(obj);
#endif

            public override bool Equals(object? obj) => obj is StringSegmentOrdinalIgnoreCaseNonRandomizedComparer;
            public override int GetHashCode() => typeof(StringSegmentOrdinalIgnoreCaseNonRandomizedComparer).GetHashCode();

            #endregion

            #region Internal Methods

            internal override int GetHashCode(StringSegmentInternal obj) => GetHashCodeOrdinalIgnoreCaseNonRandomized(obj.String, obj.Offset, obj.Length);

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Constants

#if NETCOREAPP3_0_OR_GREATER
        private const int lengthThreshold = 32;
#endif
        #endregion

        #region Properties

        #region Static Properties
        
        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive ordinal string comparison.
        /// <br/>The methods of the returned <see cref="StringSegmentComparer"/> instance can be called with <see cref="string">string</see>, <see cref="StringSegment"/>
        /// and <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> parameter values, which will not allocate new strings on any platform.
        /// </summary>
        /// <remarks>
        /// <note>The comparer returned by this property does not generate randomized hash codes for strings no longer than 32 characters (and for longer strings it is platform-dependent).
        /// Use the <see cref="OrdinalRandomized"/> property to get a comparer with randomized hash for any lengths on all platforms,
        /// or the <see cref="OrdinalNonRandomized"/> property to never use randomized hash codes.</note>
        /// </remarks>
        public static StringSegmentComparer Ordinal => StringSegmentOrdinalComparer.Instance;

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive ordinal string comparison.
        /// <br/>The methods of the returned <see cref="StringSegmentComparer"/> instance can be called with <see cref="string">string</see>, <see cref="StringSegment"/>
        /// and <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> parameter values, which will not allocate new strings on any platform.
        /// </summary>
        /// <remarks>
        /// <note>The comparer returned by this property does not generate randomized hash codes for strings no longer than 32 characters (and for longer strings it is platform-dependent).
        /// Use the <see cref="OrdinalIgnoreCaseRandomized"/> property to get a comparer with randomized hash for any lengths on all platforms,
        /// or the <see cref="OrdinalIgnoreCaseNonRandomized"/> property to never use randomized hash codes.</note>
        /// </remarks>
        public static StringSegmentComparer OrdinalIgnoreCase => StringSegmentOrdinalIgnoreCaseComparer.Instance;

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive string comparison using the word comparison rules of the invariant culture.
        /// <br/>Depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> method might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.
        /// </summary>
        public static StringSegmentComparer InvariantCulture => StringSegmentCultureAwareComparer.Invariant;

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive string comparison using the word comparison rules of the invariant culture.
        /// <br/>Depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> method might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.
        /// </summary>
        public static StringSegmentComparer InvariantCultureIgnoreCase => StringSegmentCultureAwareComparer.InvariantIgnoreCase;

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

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive ordinal string comparison. The returned comparer is functionally equivalent
        /// with <see cref="Ordinal"/> but it ensures that the hash code of a specific string is stable only within the same process and <see cref="AppDomain"/>.
        /// </summary>
        public static StringSegmentComparer OrdinalRandomized => StringSegmentOrdinalRandomizedComparer.Instance;

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive ordinal string comparison. The returned comparer is functionally equivalent
        /// with <see cref="OrdinalIgnoreCase"/> but it ensures that the hash code of a specific string is stable only within the same process and <see cref="AppDomain"/>.
        /// </summary>
        public static StringSegmentComparer OrdinalIgnoreCaseRandomized => StringSegmentOrdinalIgnoreCaseRandomizedComparer.Instance;

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive ordinal string comparison. The returned comparer is functionally equivalent
        /// with <see cref="Ordinal"/> but it ensures that the hash code of a specific string is always the same, regardless of the targeted platform or the length of the string.
        /// </summary>
        /// <remarks>
        /// <note type="security">Never use this comparer for a collection that can be populated by a publicly available service because it can be a target
        /// of hash collision attacks, which may radically degrade the performance.</note>
        /// </remarks>
        public static StringSegmentComparer OrdinalNonRandomized => StringSegmentOrdinalNonRandomizedComparer.Instance;

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive ordinal string comparison. The returned comparer is functionally equivalent
        /// with <see cref="OrdinalIgnoreCase"/> but it ensures that the hash code of a specific string is stable only within the same process and <see cref="AppDomain"/>.
        /// </summary>
        /// <remarks>
        /// <note type="security">Never use this comparer for a collection that can be populated by a publicly available service because it can be a target
        /// of hash collision attacks, which may radically degrade the performance.</note>
        /// </remarks>
        public static StringSegmentComparer OrdinalIgnoreCaseNonRandomized => StringSegmentOrdinalIgnoreCaseNonRandomizedComparer.Instance;

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets a <see cref="System.Globalization.CompareInfo"/> that is associated with this <see cref="StringSegmentComparer"/>
        /// or <see langword="null"/> if this <see cref="StringSegmentComparer"/> is not a culture-aware one.
        /// </summary>
        public virtual CompareInfo? CompareInfo => null;

        /// <summary>
        /// Gets the <see cref="System.Globalization.CompareOptions"/> that is associated with this <see cref="StringSegmentComparer"/>.
        /// </summary>
        public abstract CompareOptions CompareOptions { get; }

        #endregion

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
        /// <param name="ignoreCase"><see langword="true"/> to specify that comparison operations be case-insensitive;
        /// <see langword="false"/> to specify that comparison operations be case-sensitive.</param>
        /// <returns>A new <see cref="StringSegmentComparer"/> object that performs string comparisons according to the comparison rules used by
        /// the <paramref name="culture"/> parameter and the case rule specified by the <paramref name="ignoreCase"/> parameter.</returns>
        /// <remarks>
        /// <para>If <paramref name="culture"/> is either the <see cref="CultureInfo.InvariantCulture"/> or the <see cref="CultureInfo.CurrentCulture"/>,
        /// then depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> and <see cref="GetHashCode(ReadOnlySpan{char})"/> methods might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.</para>
        /// <para>If <paramref name="culture"/> is any <see cref="CultureInfo"/> other than the <see cref="CultureInfo.InvariantCulture"/> and <see cref="CultureInfo.CurrentCulture"/>,
        /// then depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/>, <see cref="GetHashCode(ReadOnlySpan{char})"/>, <see cref="Equals(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
        /// and <see cref="Compare(ReadOnlySpan{char}, ReadOnlySpan{char})"/> methods might allocate a new string. In .NET Core 3.0 and above
        /// none of the members with <see cref="StringSegment"/> parameters will allocate new strings. And methods with <see cref="ReadOnlySpan{T}"/> parameters
        /// (<see cref="Equals(ReadOnlySpan{char}, ReadOnlySpan{char})"/> and <see cref="Compare(ReadOnlySpan{char}, ReadOnlySpan{char})"/>) can avoid allocating strings when targeting .NET 5.0 or higher.</para>
        /// </remarks>
        public static StringSegmentComparer Create(CultureInfo culture, bool ignoreCase) => new StringSegmentCultureAwareComparer(culture, ignoreCase);

#if !(NET35 || NET40 || NET45) // because GetHashCode cannot respect CompareOptions on these platforms
        /// <summary>
        /// Creates a <see cref="StringSegmentComparer"/> object that compares strings according to the rules of a specified <paramref name="culture"/>.
        /// <br/>Please note that the returned <see cref="StringSegmentComparer"/> may allocate new strings in some cases when targeting older frameworks.
        /// See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="culture">A culture whose linguistic rules are used to perform a comparison.</param>
        /// <param name="options">Specifies the options for the comparisons.</param>
        /// <returns>A new <see cref="StringSegmentComparer"/> object that performs string comparisons according to the comparison rules used by
        /// the <paramref name="culture"/> <paramref name="options"/> parameters.</returns>
        /// <remarks>
        /// <para>If <paramref name="culture"/> is either the <see cref="CultureInfo.InvariantCulture"/> or the <see cref="CultureInfo.CurrentCulture"/>,
        /// then depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/> and <see cref="GetHashCode(ReadOnlySpan{char})"/> methods might allocate a new string.
        /// In .NET Core 3.0 and above none of the members of the returned <see cref="StringSegmentComparer"/> will allocate new strings.</para>
        /// <para>If <paramref name="culture"/> is any <see cref="CultureInfo"/> other than the <see cref="CultureInfo.InvariantCulture"/> and <see cref="CultureInfo.CurrentCulture"/>,
        /// then depending on the targeted platform, the <see cref="GetHashCode(StringSegment)"/>, <see cref="GetHashCode(ReadOnlySpan{char})"/>, <see cref="Equals(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
        /// and <see cref="Compare(ReadOnlySpan{char}, ReadOnlySpan{char})"/> methods might allocate a new string. In .NET Core 3.0 and above
        /// none of the members with <see cref="StringSegment"/> parameters will allocate new strings. And methods with <see cref="ReadOnlySpan{T}"/> parameters
        /// (<see cref="Equals(ReadOnlySpan{char}, ReadOnlySpan{char})"/> and <see cref="Compare(ReadOnlySpan{char}, ReadOnlySpan{char})"/>) can avoid allocating strings when targeting .NET 5.0 or higher.</para>
        /// </remarks>
        public static StringSegmentComparer Create(CultureInfo culture, CompareOptions options) => new StringSegmentCultureAwareComparer(culture, options);
#endif

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinal(string s)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (s.Length > lengthThreshold)
                return s.GetHashCode();
#endif
            return GetHashCodeOrdinalNonRandomized(s);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinal(string s, int offset, int length)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (length > lengthThreshold)
                return String.GetHashCode(s.AsSpan(offset, length));
#endif
            return GetHashCodeOrdinalNonRandomized(s, offset, length);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinal(ReadOnlySpan<char> s)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (s.Length > lengthThreshold)
                return String.GetHashCode(s); 
#endif
            return GetHashCodeOrdinalNonRandomized(s);
        }
#endif

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinalIgnoreCase(string s)
        {
#if NETCOREAPP3_0_OR_GREATER // would work also for 2.1 but must be consistent with the Span version
            if (s.Length > lengthThreshold)
                return s.GetHashCode(StringComparison.OrdinalIgnoreCase);
#endif

            return GetHashCodeOrdinalIgnoreCaseNonRandomized(s);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinalIgnoreCase(string s, int offset, int length)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (length > lengthThreshold)
                return String.GetHashCode(s.AsSpan(offset, length), StringComparison.OrdinalIgnoreCase);
#endif
            return GetHashCodeOrdinalIgnoreCaseNonRandomized(s, offset, length);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> s)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (s.Length > lengthThreshold)
                return String.GetHashCode(s, StringComparison.OrdinalIgnoreCase);
#endif
            return GetHashCodeOrdinalIgnoreCaseNonRandomized(s);
        }
#endif

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int GetHashCodeOrdinalNonRandomized(string s)
        {
            var result = 13;

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (int i = 0; i < s.Length; i++)
                result = result * 397 + s[i];

            return result;
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static int GetHashCodeOrdinalIgnoreCaseNonRandomized(string s)
        {
            var result = 13;

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (int i = 0; i < s.Length; i++)
                result = result * 397 + Char.ToUpperInvariant(s[i]);

            return result;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static int GetHashCodeOrdinalNonRandomized(string s, int offset, int length)
        {
            var result = 13;
            for (int i = 0; i < length; i++)
                result = result * 397 + s[i + offset];

            return result;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static int GetHashCodeOrdinalIgnoreCaseNonRandomized(string s, int offset, int length)
        {
            var result = 13;
            for (int i = 0; i < length; i++)
                result = result * 397 + Char.ToUpperInvariant(s[i + offset]);

            return result;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static int GetHashCodeOrdinalNonRandomized(ReadOnlySpan<char> s)
        {
            var result = 13;

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (int i = 0; i < s.Length; i++)
                result = result * 397 + s[i];

            return result;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static int GetHashCodeOrdinalIgnoreCaseNonRandomized(ReadOnlySpan<char> s)
        {
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
        /// <returns><see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> are equal; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> are equal; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> refer to the same object, or <paramref name="x"/> and <paramref name="y"/> are both
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
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// When overridden in a derived class, indicates whether two <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> instances are equal.
        /// </summary>
        /// <param name="x">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to compare to <paramref name="y"/>.</param>
        /// <param name="y">A <see cref="ReadOnlySpan{T}"><![CDATA[ReadOnlySpan<char>]]></see> to compare to <paramref name="x"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> are equal; otherwise, <see langword="false"/>.</returns>
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

        #region Explicitly Implemented Interface Methods

#if NET9_0_OR_GREATER
        bool IAlternateEqualityComparer<StringSegment, string?>.Equals(StringSegment x, string? y) => Equals(x, y);
        string? IAlternateEqualityComparer<StringSegment, string?>.Create(StringSegment x) => x.ToString();

        bool IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Equals(ReadOnlySpan<char> x, string? y) => Equals(x, y);
        string? IAlternateEqualityComparer<ReadOnlySpan<char>, string?>.Create(ReadOnlySpan<char> x) => x.ToString();
#endif

        #endregion

        #endregion

        #endregion
    }
}
