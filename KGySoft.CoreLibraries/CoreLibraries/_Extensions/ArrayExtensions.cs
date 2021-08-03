#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArrayExtensions.cs
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
using System.Runtime.CompilerServices;

using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for arrays.
    /// </summary>

    public static class ArrayExtensions
    {
        #region Nested classes

        private static class ElementInfo<T>
        {
            #region Fields

            internal static readonly bool IsPrimitive = typeof(T).IsPrimitive;
            internal static readonly int ElementSizeExponent = IsPrimitive ? (int)Math.Log(Reflector<T>.SizeOf, 2) : 0;

            #endregion
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Gets an <see cref="ArraySection{T}"/> instance, which represents a section of the specified <paramref name="array"/>.
        /// No heap allocation occurs when using this method.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="array">The array to create the <see cref="ArraySection{T}"/> from.</param>
        /// <param name="offset">The zero-based offset that points to the first element of the returned section.</param>
        /// <param name="length">The desired length of the returned section.</param>
        /// <returns>An <see cref="ArraySection{T}"/> instance, which represents a section of the specified <paramref name="array"/>.</returns>
        public static ArraySection<T> AsSection<T>(this T[] array, int offset, int length) => new ArraySection<T>(array, offset, length);

        /// <summary>
        /// Gets an <see cref="ArraySection{T}"/> instance, which represents a section of the specified <paramref name="array"/>.
        /// No heap allocation occurs when using this method.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="array">The array to create the <see cref="ArraySection{T}"/> from.</param>
        /// <param name="offset">The zero-based offset that points to the first element of the returned section.</param>
        /// <returns>An <see cref="ArraySection{T}"/> instance, which represents a section of the specified <paramref name="array"/>.</returns>
        public static ArraySection<T> AsSection<T>(this T[] array, int offset) => new ArraySection<T>(array, offset);

        /// <summary>
        /// Gets the specified <paramref name="array"/> as an <see cref="ArraySection{T}"/> instance.
        /// No heap allocation occurs when using this method.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="array">The array to create the <see cref="ArraySection{T}"/> from.</param>
        /// <returns>An <see cref="ArraySection{T}"/> instance for the specified <paramref name="array"/>.</returns>
        public static ArraySection<T> AsSection<T>(this T[]? array) => array == null ? ArraySection<T>.Null : new ArraySection<T>(array);

        /// <summary>
        /// Gets an <see cref="Array2D{T}"/> wrapper for the specified <paramref name="array"/>.
        /// The array must have enough capacity for the specified <paramref name="height"/> and <paramref name="width"/>.
        /// No heap allocation occurs when using this method.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="array">The desired underlying buffer for the <see cref="Array2D{T}"/> instance to be created.
        /// It must have sufficient capacity for the specified dimensions.</param>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>An <see cref="Array2D{T}"/> instance using the specified <paramref name="array"/> as its underlying buffer that has the specified dimensions.</returns>
        public static Array2D<T> AsArray2D<T>(this T[] array, int height, int width) => new Array2D<T>(array, height, width);

        /// <summary>
        /// Gets an <see cref="Array3D{T}"/> wrapper for the specified <paramref name="array"/>.
        /// The array must have enough capacity for the specified <paramref name="depth"/>, <paramref name="height"/> and <paramref name="width"/>.
        /// No heap allocation occurs when using this method.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="array">The desired underlying buffer for the <see cref="Array3D{T}"/> instance to be created.
        /// It must have sufficient capacity for the specified dimensions.</param>
        /// <param name="depth">The depth of the array to be returned.</param>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>An <see cref="Array3D{T}"/> instance using the specified <paramref name="array"/> as its underlying buffer that has the specified dimensions.</returns>
        public static Array3D<T> AsArray3D<T>(this T[] array, int depth, int height, int width) => new Array3D<T>(array, depth, height, width);

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static void CopyElements<T>(this T[] source, int sourceIndex, T[] dest, int destIndex, int count)
        {
            if (ElementInfo<T>.IsPrimitive)
            {
                Buffer.BlockCopy(source, sourceIndex << ElementInfo<T>.ElementSizeExponent, dest, destIndex << ElementInfo<T>.ElementSizeExponent, count << ElementInfo<T>.ElementSizeExponent);
                return;
            }

            Array.Copy(source, sourceIndex, dest, destIndex, count);
        }

        #endregion

        #endregion
    }
}