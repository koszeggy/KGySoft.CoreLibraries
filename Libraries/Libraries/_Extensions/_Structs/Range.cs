using System;
using System.Collections.Generic;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Represents a range with lower and upper bounds.
    /// </summary>
    public struct Range<T> : IEquatable<Range<T>>
        where T : IComparable<T>
    {
        private static readonly IEqualityComparer<T> comparer = typeof(T).IsEnum ? (IEqualityComparer<T>)EnumComparer<T>.Comparer : EqualityComparer<T>.Default;

        /// <summary>
        /// Gets the lower bound of the range.
        /// </summary>
        public T LowerBound { get; }

        /// <summary>
        /// Gets the upper bound of the range. Whether this is an exclusive or inclusive bound, it depends on the context it is used in.
        /// </summary>
        public T UpperBound { get; }

        public Range(T upperBound)
            : this(default, upperBound)
        {
        }

        public Range(T lowerBound, T upperBound)
        {
            if (lowerBound.CompareTo(upperBound) > 0)
                throw new ArgumentOutOfRangeException(nameof(upperBound), Res.Get(Res.MaxValueLessThanMinValue));
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        // TODO: .NET 4.7 and above:
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

        public bool Equals(Range<T> other) => comparer.Equals(other.LowerBound, LowerBound) && comparer.Equals(other.UpperBound, UpperBound);

        public override bool Equals(object obj) => obj is Range<T> range && Equals(range);

        public override int GetHashCode() => ((comparer.GetHashCode(UpperBound) & 0xFFFF) << 16) | (comparer.GetHashCode(LowerBound) & 0xFFFF);

        public static bool operator ==(Range<T> left, Range<T> right) => left.Equals(right);

        public static bool operator !=(Range<T> left, Range<T> right) => !left.Equals(right);
    }
}
