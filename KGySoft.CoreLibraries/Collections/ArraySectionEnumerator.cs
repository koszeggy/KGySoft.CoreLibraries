#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySectionEnumerator.cs
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
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Enumerates the elements of an <see cref="ArraySection{T}"/>, <see cref="Array2D{T}"/> or <see cref="Array3D{T}"/> instance.
    /// </summary>
    public struct ArraySectionEnumerator<T> : IEnumerator<T>
    {
        #region Fields

        private readonly T[] array;
        private readonly int start;
        private readonly int end;

        private int index;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public T Current
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get => index >= start && index < end ? array[index] : default;
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        object IEnumerator.Current
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

        internal ArraySectionEnumerator(T[] array, int offset, int length)
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