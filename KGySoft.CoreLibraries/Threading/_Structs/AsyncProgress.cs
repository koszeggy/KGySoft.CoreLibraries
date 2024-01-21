#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AsyncProgress.cs
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
using System.Collections.Generic;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// Represents the progress of an operation that can be reported by an <see cref="IAsyncProgress"/> implementation.
    /// </summary>
    /// <typeparam name="T">The type of the descriptor of the operation returned by the <see cref="OperationType"/> property.</typeparam>
    public readonly struct AsyncProgress<T> : IEquatable<AsyncProgress<T>>
    {
        #region Fields

        private static readonly IEqualityComparer<T> operationComparer = ComparerHelper<T>.EqualityComparer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets information about the current operation in progress.
        /// </summary>
        public T OperationType { get; }

        /// <summary>
        /// Gets the maximum steps of this operation.
        /// </summary>
        public int MaximumValue { get; }

        /// <summary>
        /// Gets the current step of this operation. Its value is between zero and <see cref="MaximumValue"/>, inclusive bounds.
        /// </summary>
        public int CurrentValue { get; }

        #endregion

        #region Operators

        /// <summary>
        /// Gets whether two <see cref="AsyncProgress{T}"/> structures are equal.
        /// </summary>
        /// <param name="left">The <see cref="AsyncProgress{T}"/> instance that is to the left of the equality operator.</param>
        /// <param name="right">The <see cref="AsyncProgress{T}"/> instance that is to the right of the equality operator.</param>
        /// <returns><see langword="true"/> if the two <see cref="AsyncProgress{T}"/> structures are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(AsyncProgress<T> left, AsyncProgress<T> right) => left.Equals(right);

        /// <summary>
        /// Gets whether two <see cref="AsyncProgress{T}"/> structures are different.
        /// </summary>
        /// <param name="left">The <see cref="AsyncProgress{T}"/> instance that is to the left of the inequality operator.</param>
        /// <param name="right">The <see cref="AsyncProgress{T}"/> instance that is to the right of the inequality operator.</param>
        /// <returns><see langword="true"/> if the two <see cref="AsyncProgress{T}"/> structures are different; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(AsyncProgress<T> left, AsyncProgress<T> right) => !(left == right);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncProgress{T}"/> struct.
        /// </summary>
        /// <param name="operationType">Provides some information about the current operation.</param>
        /// <param name="maximumValue">The maximum value.</param>
        /// <param name="currentValue">The current value.</param>
        public AsyncProgress(T operationType, int maximumValue, int currentValue)
        {
            if (maximumValue < 0)
                Throw.ArgumentOutOfRangeException(nameof(maximumValue), Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if ((uint)currentValue > (uint)maximumValue)
                Throw.ArgumentOutOfRangeException(nameof(currentValue), Res.ArgumentMustBeBetween(0, maximumValue));

            OperationType = operationType;
            MaximumValue = maximumValue;
            CurrentValue = currentValue;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><see langword="true"/> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj) => obj is AsyncProgress<T> asyncProgress && Equals(asyncProgress);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => (OperationType, MaximumValue, CurrentValue).GetHashCode();

        /// <summary>
        /// Indicates whether the this <see cref="AsyncProgress{T}"/> is equal to another one.
        /// </summary>
        /// <param name="other">A <see cref="AsyncProgress{T}"/> instance to compare with this one.</param>
        /// <returns><see langword="true"/> if the current object is equal to the <paramref name="other"/>&#160;<see cref="AsyncProgress{T}"/>; otherwise, <see langword="false"/>.</returns>
        public bool Equals(AsyncProgress<T> other)
            => operationComparer.Equals(OperationType, other.OperationType) && MaximumValue == other.MaximumValue && CurrentValue == other.CurrentValue;

        #endregion
    }
}
