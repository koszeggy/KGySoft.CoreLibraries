#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Range.cs
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
using System.Collections.Generic;
using System.ComponentModel;

#endregion

#region Suppressions

#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8604 // Possible null reference argument. - IEqualityComparer.Equals
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a range with lower and upper bounds.
    /// </summary>
    /// <typeparam name="T">The type of the range.</typeparam>
    public readonly struct Range<T> : IEquatable<Range<T>>
        where T : IComparable<T>
    {
        #region Fields

        private static readonly IEqualityComparer<T> comparer = ComparerHelper<T>.EqualityComparer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the lower bound of the range.
        /// </summary>
        public T? LowerBound { get; }

        /// <summary>
        /// Gets the upper bound of the range. Whether this is an exclusive or inclusive bound, it depends on the context the <see cref="Range{T}"/> is used in.
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

#if !(NET35 || NET40 || NET45)
        /// <summary>
        /// Performs an implicit conversion from <typeparamref name="T"/> to <see cref="Range{T}"/> using the provided value as upper bound.
        /// </summary>
        /// <param name="upperBound">The upper bound of the range. Whether this is an exclusive or inclusive bound, it depends on the context the <see cref="Range{T}"/> is used in.</param>
        /// <returns>
        /// A <see cref="Range{T}"/> instance representing a range between the default value of <typeparamref name="T"/> and <paramref name="upperBound"/>.
        /// </returns>
        public static implicit operator Range<T>(T upperBound) => new Range<T>(upperBound);

        /// <summary>
        /// Performs an implicit conversion from <see cref="ValueTuple{T, T}"/> to <see cref="Range{T}"/>.
        /// </summary>
        /// <param name="bounds">The <see cref="ValueTuple{T, T}"/> instance to be converted to <see cref="Range{T}"/>.</param>
        /// <returns>
        /// A <see cref="Range{T}"/> instance representing a range between the provided <see cref="ValueTuple{T, T}.Item1"/> and <see cref="ValueTuple{T, T}.Item2"/>.
        /// </returns>
        public static implicit operator Range<T>((T LowerBound, T UpperBound) bounds) => new Range<T>(bounds.LowerBound, bounds.UpperBound);
#endif

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
        public Range(T? lowerBound, T upperBound)
        {
            if (lowerBound?.CompareTo(upperBound) > 0)
                Throw.ArgumentOutOfRangeException(Argument.upperBound, Res.MaxValueLessThanMinValue);
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deconstructs this <see cref="Range{T}"/> instance.
        /// </summary>
        /// <param name="lowerBound">Returns the lower bound of the range.</param>
        /// <param name="upperBound">Returns the upper bound of the range. Whether this is an exclusive or inclusive bound, it depends on the context the <see cref="Range{T}"/> is used in.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out T? lowerBound, out T upperBound)
        {
            lowerBound = LowerBound;
            upperBound = UpperBound;
        }

        /// <summary>
        /// Indicates whether the current <see cref="Range{T}"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// </summary>
        /// <param name="other">An <see cref="Range{T}"/> instance to compare with this instance.</param>
        /// <returns><see langword="true" />&#160;if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
        public bool Equals(Range<T> other) => comparer.Equals(other.LowerBound, LowerBound) && comparer.Equals(other.UpperBound, UpperBound);

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => obj is Range<T> range && Equals(range);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() => (LowerBound, UpperBound).GetHashCode();

        /// <summary>
        /// Gets the string representation of this <see cref="Range{T}"/> instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this <see cref="Range{T}"/> instance.
        /// </returns>
        public override string ToString() => $"[{LowerBound}..{UpperBound}]";

        #endregion
    }
}
