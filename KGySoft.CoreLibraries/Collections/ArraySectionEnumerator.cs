#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySectionEnumerator.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Enumerates the elements of an <see cref="ArraySection{T}" />, <see cref="Array2D{T}" /> or <see cref="Array3D{T}" /> instance.
    /// </summary>
    /// <typeparam name="T">The type of the enumerated elements.</typeparam>
    public struct ArraySectionEnumerator<T> : IEnumerator<T>
    {
        #region Fields

        private readonly T[]? array;
        private readonly int start;
        private readonly int end;

        private int index;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        [SuppressMessage("ReSharper", "ReturnTypeCanBeNotNullable", Justification = "False alarm, can return null before/after enumerating, even if T is not nullable. Actually it should be T? also on IEnumerator<T>")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "ReSharper issue")]
        public readonly T? Current
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes). - actually it should be T? also on IEnumerator<T>
            get => index >= start && index < end ? array![index] : default!;
#pragma warning restore CS8766
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        object? IEnumerator.Current
        {
            get
            {
                if (index < start || index >= end)
                    Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                return Current;
            }
        }

        #endregion

        #endregion

        #region Constructors

        internal ArraySectionEnumerator(T[]? array, int offset, int length)
        {
            this.array = array;
            start = offset;
            end = offset + length;
            index = start - 1;
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if the enumerator was successfully advanced to the next element; <see langword="false"/>&#160;if the enumerator has passed the end of the collection.
        /// </returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public bool MoveNext()
        {
            if (index >= end)
                return false;

            index += 1;
            return index < end;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset() => index = start - 1;

        #endregion

        #region Explicitly Implemented Interface Methods

        void IDisposable.Dispose()
        {
        }

        #endregion

        #endregion
    }
}