#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CastArrayEnumerator.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Enumerates the elements of a <see cref="CastArray{TFrom,TTo}" />, <see cref="CastArray2D{TFrom,TTo}" /> or <see cref="CastArray3D{TFrom,TTo}" /> instance.
    /// </summary>
    /// <typeparam name="TFrom">The element type of the actual underlying buffer.</typeparam>
    /// <typeparam name="TTo">The type of the enumerated elements.</typeparam>
    public struct CastArrayEnumerator<TFrom, TTo> : IEnumerator<TTo>
#if NETFRAMEWORK // To make the type compatible with older compilers. Unmanaged is asserted in the wrapped CastArray<TFrom, TTo>.
        where TFrom : struct
        where TTo : struct
#else
        where TFrom : unmanaged
        where TTo : unmanaged
#endif
    {
        #region Fields

#if NETFRAMEWORK || NETSTANDARD2_0
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Must not be readonly because it may cause a VerificationException from a partially trusted domain")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local", Justification = "Must not be readonly because it may cause a VerificationException from a partially trusted domain")]
        private CastArray<TFrom, TTo> castArray;
#else
        private readonly CastArray<TFrom, TTo> castArray;
#endif

        private int index;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        [SuppressMessage("ReSharper", "ReturnTypeCanBeNotNullable", Justification = "False alarm, can return null before/after enumerating, even if T is not nullable. Actually it should be T? also on IEnumerator<T>")]
        public readonly TTo Current
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            [SecuritySafeCritical]
#if NETFRAMEWORK || NETSTANDARD2_0 // To wrap a possible VerificationException into a much less scary NotSupportedException in a partially trusted domain.
            get => (uint)index >= castArray.Length ? default : castArray.GetElementUnsafe(index);
#else
            get => (uint)index >= castArray.Length ? default : castArray.GetElementReferenceInternal(index);
#endif
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        object IEnumerator.Current
        {
            get
            {
                if ((uint)index >= castArray.Length)
                    Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                return Current;
            }
        }

        #endregion

        #endregion

        #region Constructors

        internal CastArrayEnumerator(CastArray<TFrom, TTo> array)
        {
            this.castArray = array;
            index = -1;
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the collection.
        /// </returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public bool MoveNext()
        {
            if (index >= castArray.Length)
                return false;

            index += 1;
            return index < castArray.Length;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset() => index = -1;

        #endregion

        #region Explicitly Implemented Interface Methods

        void IDisposable.Dispose()
        {
        }

        #endregion

        #endregion
    }
}