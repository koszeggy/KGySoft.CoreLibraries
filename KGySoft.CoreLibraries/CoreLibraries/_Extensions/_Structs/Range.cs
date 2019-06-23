#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Range.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a range with lower and upper bounds.
    /// </summary>
    /// <typeparam name="T">The type of the range.</typeparam>
    public struct Range<T> : IEquatable<Range<T>>
        where T : IComparable<T>
    {
#if !NET35 && !NET40 && !NET45
#error TODO: .NET 4.7 and above:
        ///// <summary>
        ///// Deconstructs this <see cref="Range{T}"/> instance.
        ///// </summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public void Deconstruct(out T lowerBound, out T upperBound)
        //{
        //    lowerBound = LowerBound;
        //    upperBound = UpperBound;
        //}

        //public static implicit operator Range<T>(T upperBound) => new Range<T>(upperBound);

        //public static implicit operator Range<T>((T LowerBound, T UpperBound) bounds) => new Range<T>(bounds.LowerBound, bounds.UpperBound);

        //public static implicit operator (T LowerBound, T UpperBound)(Range<T> range) => (range.LowerBound, range.UpperBound);
#endif

        #region Fields

        private static readonly IEqualityComparer<T> comparer = typeof(T).IsEnum ? (IEqualityComparer<T>)EnumComparer<T>.Comparer : EqualityComparer<T>.Default;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the lower bound of the range.
        /// </summary>
        public T LowerBound { get; }

        /// <summary>
        /// Gets the upper bound of the range. Whether this is an exclusive or inclusive bound, it depends on the context it is used in.
        /// </summary>
        public T UpperBound { get; }

        #endregion

        #region Operators

        /// <summary>
        /// Implements the equality check operator.
        /// </summary>
        /// <param name="left">The left argument of the equality check.</param>
        /// <param name="right">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(Range<T> left, Range<T> right) => left.Equals(right);

        /// <summary>
        /// Implements the inequality check operator.
        /// </summary>
        /// <param name="left">The left argument of the inequality check.</param>
        /// <param name="right">The right argument of the inequality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(Range<T> left, Range<T> right) => !left.Equals(right);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Range{T}"/>&#160;<see langword="struct"/>&#160;between the default value of <typeparamref name="T"/> and
        /// the specified <paramref name="upperBound"/>.
        /// </summary>
        /// <param name="upperBound">The upper bound. Whether this is an exclusive or inclusive bound, it depends on the context it is used in.</param>
        public Range(T upperBound)
            : this(default, upperBound)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Range{T}"/>&#160;<see langword="struct"/>&#160;between the specified <paramref name="lowerBound"/> and <paramref name="upperBound"/>.
        /// </summary>
        /// <param name="lowerBound">The lower bound.</param>
        /// <param name="upperBound">The upper bound. Whether this is an exclusive or inclusive bound, it depends on the context it is used in.</param>
        public Range(T lowerBound, T upperBound)
        {
            if (lowerBound.CompareTo(upperBound) > 0)
                throw new ArgumentOutOfRangeException(nameof(upperBound), Res.MaxValueLessThanMinValue);
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Indicates whether the current <see cref="Range{T}"/> instance is equal to another one specified in the <paramref name="other"/> prameter.
        /// </summary>
        /// <param name="other">An <see cref="Range{T}"/> instance to compare with this instance.</param>
        /// <returns><see langword="true" />&#160;if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
        public bool Equals(Range<T> other) => comparer.Equals(other.LowerBound, LowerBound) && comparer.Equals(other.UpperBound, UpperBound);

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj) => obj is Range<T> range && Equals(range);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() => ((comparer.GetHashCode(UpperBound) & 0xFFFF) << 16) | (comparer.GetHashCode(LowerBound) & 0xFFFF);

        /// <summary>
        /// Gets the string representation of this <see cref="Range{T}"/> instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this <see cref="Range{T}"/> instance.
        /// </returns>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)",
            Justification = "Possible usage of current culture is preferred here.")]
        public override string ToString() => $"[{LowerBound}..{UpperBound}]";

        #endregion
    }
}
