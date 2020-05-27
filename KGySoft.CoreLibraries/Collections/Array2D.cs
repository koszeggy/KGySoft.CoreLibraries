#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Array2D.cs
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
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a rectangular array. It supports accessing its rows or the whole content as a single dimension <see cref="ArraySection{T}"/>.
    /// Depending on the used platform it supports <see cref="ArrayPool{T}"/> allocation and casting to <see cref="Span{T}"/>.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <typeparam name="T">The type of the element in the collection.</typeparam>
    /// <remarks>
    /// <para>In .NET Core 3.0/.NET Standard 2.1 and above an <see cref="Array2D{T}"/> instance can be easily turned to a <see cref="Span{T}"/> instance (either by cast or by the <see cref="AsSpan"/> property).</para>
    /// <para>If the current platform supports it, the underlying array might be obtained by using the <see cref="ArrayPool{T}"/>.
    /// <note>Unlike the underlying <see cref="ArraySection{T}"/>, the <see cref="Array2D{T}"/> implements the <see cref="IDisposable"/> interface.
    /// Calling the <see cref="Dispose">Dispose</see> method is required if the <see cref="Array2D{T}"/> was not created from an existing <see cref="ArraySection{T}"/>
    /// instance. Not calling the <see cref="Dispose">Dispose</see> method may lead to decreased application performance.</note></para>
    /// <para>As <see cref="Array2D{T}"/> is a non-<c>readonly</c>&#160;<see langword="struct"/>&#160;it is not recommended to use it as a <c>readonly</c> field; otherwise,
    /// accessing its members would make the compiler to create a defensive copy, which leads to a slight performance degradation.</para>
    /// </remarks>
    [Serializable]
    public struct Array2D<T> : IDisposable, IEquatable<Array2D<T>>, IEnumerable<T>
    {
        #region Fields

        private readonly int width;
        private readonly int height;

        [SuppressMessage("Style", "IDE0044:Add readonly modifier",
                Justification = "Must not be readonly to prevent defensive copy when accessing members")]
        private ArraySection<T> buffer;

        #endregion

        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets the width of this <see cref="Array2D{T}"/> instance.
        /// </summary>
        public int Width => width;

        /// <summary>
        /// Gets the height of this <see cref="Array2D{T}"/> instance.
        /// </summary>
        public int Height => height;

        /// <summary>
        /// Gets the total length of this <see cref="Array2D{T}"/> instance.
        /// </summary>
        public int Length => buffer.Length;

        /// <summary>
        /// Gets the underlying buffer as a single dimensional <see cref="ArraySection{T}"/>.
        /// </summary>
        public ArraySection<T> Buffer => buffer;

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        /// <summary>
        /// Returns the current <see cref="Array2D{T}"/> instance as a <see cref="Memory{T}"/>.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 3.0/.NET Standard 2.1 and above.</note></remarks>
        public Memory<T> AsMemory => buffer.AsMemory;

        /// <summary>
        /// Returns the current <see cref="Array2D{T}"/> instance as a <see cref="Span{T}"/>.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 3.0/.NET Standard 2.1 and above.</note></remarks>
        public Span<T> AsSpan => buffer.AsSpan;
#endif

        #endregion

        #region Indexers

        /// <summary>
        /// Gets or sets the element at the specified indices. Parameter order is the same as in case of a regular two-dimensional array.
        /// <br/>To return a reference to an element use the <see cref="GetElementReference">GetElementReference</see> method instead.
        /// </summary>
        /// <param name="y">The row index of the item to get or set.</param>
        /// <param name="x">The column index of the item to get or set.</param>
        /// <returns>The element at the specified indices.</returns>
        public T this[int y, int x]
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if ((uint)y >= (uint)height)
                    Throw.ArgumentOutOfRangeException(Argument.y);
                if ((uint)x >= (uint)width)
                    Throw.ArgumentOutOfRangeException(Argument.x);
                return buffer.GetItemInternal(y * width + x);
            }
            [MethodImpl(MethodImpl.AggressiveInlining)]
            set
            {
                if ((uint)y >= (uint)height)
                    Throw.ArgumentOutOfRangeException(Argument.y);
                if ((uint)x >= (uint)width)
                    Throw.ArgumentOutOfRangeException(Argument.x);
                buffer.SetItemInternal(y * width + x, value);
            }
        }

        #endregion

        #endregion

        #region Operators

        /// <summary>
        /// Performs an implicit conversion from <see cref="Array2D{T}"/> to <see cref="ArraySection{T}"/>.
        /// </summary>
        /// <param name="array">The <see cref="Array2D{T}"/> to be converted to an <see cref="ArraySection{T}"/>.</param>
        /// <returns>
        /// An <see cref="ArraySection{T}"/> instance that represents the original array.
        /// </returns>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
                Justification = "See the Buffer property")]
        public static implicit operator ArraySection<T>(in Array2D<T> array) => array.buffer;

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        /// <summary>
        /// Performs an implicit conversion from <see cref="Array2D{T}"/> to <see cref="Span{T}"><![CDATA[Span<T>]]></see>.
        /// </summary>
        /// <param name="array">The <see cref="Array2D{T}"/> to be converted to a <see cref="Span{T}"><![CDATA[Span<T>]]></see>.</param>
        /// <returns>
        /// A <see cref="Span{T}"><![CDATA[Span<T>]]></see> instance that represents the specified <see cref="Array2D{T}"/>.
        /// </returns>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
                Justification = "False alarm, see AsSpan")]
        public static implicit operator Span<T>(in Array2D<T> array) => array.AsSpan;
#endif

        /// <summary>
        /// Determines whether two specified <see cref="Array2D{T}"/> instances have the same value.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(in Array2D<T> a, in Array2D<T> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified <see cref="Array2D{T}"/> instances have different values.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(in Array2D<T> a, in Array2D<T> b) => !(a == b);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Array2D{T}"/> struct using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// Parameter order is the same as in case of instantiating a regular two-dimensional array.
        /// <br/>If the created <see cref="Array2D{T}"/> is not used anymore the <see cref="Dispose">Dispose</see> method should be called to
        /// return the possibly <see cref="ArrayPool{T}"/>-allocated underlying buffer to the pool.
        /// </summary>
        /// <param name="height">The height of the array to be created.</param>
        /// <param name="width">The width of the array to be created.</param>
        public Array2D(int height, int width)
        {
            if (height < 0)
                Throw.ArgumentOutOfRangeException(Argument.width);
            if (width < 0)
                Throw.ArgumentOutOfRangeException(Argument.height);
            this.height = height;
            this.width = width;
            buffer = new ArraySection<T>(height * width);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Array2D{T}"/> struct from an existing <see cref="ArraySection{T}"/>
        /// using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// </summary>
        /// <param name="buffer">The desired underlying buffer for the <see cref="Array2D{T}"/> instance to be created.
        /// It must have sufficient capacity for the specified dimensions. Even if <paramref name="buffer"/> owns an array rented from
        /// the <see cref="ArrayPool{T}"/>, calling the <see cref="Dispose">Dispose</see> method on the created <see cref="Array2D{T}"/>
        /// instance does not return the underlying array to the pool. In such case it is the caller's responsibility to release the <paramref name="buffer"/>.</param>
        /// <param name="height">The height of the array to be created.</param>
        /// <param name="width">The width of the array to be created.</param>
        public Array2D(ArraySection<T> buffer, int height, int width)
        {
            if (buffer.IsNull)
                Throw.ArgumentNullException(Argument.buffer);
            if (height < 0)
                Throw.ArgumentOutOfRangeException(Argument.width);
            if (width < 0)
                Throw.ArgumentOutOfRangeException(Argument.height);
            int size = height * width;
            if (buffer.Length < size)
                Throw.ArgumentException(Argument.buffer, Res.ArraySectionInsufficientCapacity);

            this.height = height;
            this.width = width;

            // slicing even if length matches size to prevent Dispose returning the backing array to the pool
            this.buffer = buffer.Slice(0, size);
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Gets the reference to the element at the specified indices.
        /// </summary>
        /// <param name="y">The row index of the item to get the reference for.</param>
        /// <param name="x">The column index of the item to get the reference for.</param>
        /// <returns>The reference to the element at the specified index.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public ref T GetElementReference(int y, int x)
        {
            if ((uint)y >= (uint)height)
                Throw.ArgumentOutOfRangeException(Argument.y);
            if ((uint)x >= (uint)width)
                Throw.ArgumentOutOfRangeException(Argument.x);
            return ref buffer.GetElementReferenceInternal(y * width + x);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the items of this <see cref="Array2D{T}"/>.
        /// </summary>
        /// <returns>An <see cref="ArraySectionEnumerator{T}"/> instance that can be used to iterate though the elements of this <see cref="Array2D{T}"/>.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ArraySectionEnumerator<T> GetEnumerator() => buffer.GetEnumerator();

        /// <summary>
        /// Releases the underlying buffer.
        /// </summary>
        public void Dispose()
        {
            buffer.Release();
            this = default;
        }

        /// <summary>
        /// Returns a reference to the first element in this <see cref="Array2D{T}"/>.
        /// This makes possible to use the <see cref="Array2D{T}"/> in a <see langword="fixed"/>&#160;statement.
        /// </summary>
        /// <returns>A reference to the first element in this <see cref="Array2D{T}"/>, or <see langword="null"/>&#160;if <see cref="Length"/> is zero.</returns>
        public ref T GetPinnableReference() => ref buffer.GetPinnableReference();

        /// <summary>
        /// Indicates whether the current <see cref="Array2D{T}"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// </summary>
        /// <param name="other">An <see cref="Array2D{T}"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public bool Equals(Array2D<T> other) => width == other.width && height == other.height && buffer.Equals(other.buffer);

        /// <summary>
        /// Determines whether the specified <see cref="object">object</see> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified object is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj) => obj is Array2D<T> other && Equals(other);

        /// <summary>
        /// Returns a hash code for this <see cref="Array2D{T}"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode",
            Justification = "Field 'buffer' is practically read-only but it is not marked as so to prevent creating defensive copies")]
        public override int GetHashCode()
        {
            if (buffer.IsNull)
                return 0;
            return (buffer, width, height).GetHashCode();
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}
