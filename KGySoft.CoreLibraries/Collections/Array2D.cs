#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Array2D.cs
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
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Buffers; 
#endif
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#endregion

#region Suppressions

#if NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif
#if !NET5_0_OR_GREATER
// ReSharper disable UnusedMember.Local - Array2DDebugView.Items
#endif

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a rectangular array, whose indexer access is faster than a regular 2D array.
    /// It supports accessing its rows or the whole content as a single dimensional <see cref="ArraySection{T}"/>.
    /// Depending on the used platform it supports <see cref="ArrayPool{T}"/> allocation and casting to <see cref="Span{T}"/>.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <remarks>
    /// <para>In .NET Core 2.1/.NET Standard 2.1 and above an <see cref="Array2D{T}"/> instance can be easily turned to a <see cref="Span{T}"/> instance (either by cast or by the <see cref="AsSpan"/> property).</para>
    /// <para>The actual underlying single dimensional array can be accessed via the <see cref="Buffer"/> property that has an <see cref="ArraySection{T}.UnderlyingArray"/> property.</para>
    /// <para>If the current platform supports it, the underlying array might be obtained by using the <see cref="ArrayPool{T}"/>.
    /// <note>Unlike the underlying <see cref="ArraySection{T}"/>, the <see cref="Array2D{T}"/> implements the <see cref="IDisposable"/> interface.
    /// Calling the <see cref="Dispose">Dispose</see> method is required if the <see cref="Array2D{T}"/> was not created from an existing <see cref="ArraySection{T}"/>
    /// instance. Not calling the <see cref="Dispose">Dispose</see> method may lead to decreased application performance.</note></para>
    /// <para>Due to the <see cref="Dispose">Dispose</see> method <see cref="Array2D{T}"/> is a non-<c>readonly</c>&#160;<see langword="struct"/>.
    /// It is not recommended to use it as a <c>readonly</c> field; otherwise, accessing its members would make the pre-C# 8.0 compilers to create defensive copies,
    /// which leads to a slight performance degradation.</para>
    /// </remarks>
    [Serializable]
    [DebuggerDisplay("{typeof(" + nameof(T) + ")." + nameof(Type.Name) + ",nq}[{" + nameof(Height) + "}, {" + nameof(Width) + "}]")]
    [DebuggerTypeProxy(typeof(Array2D<>.Array2DDebugView))]
    public struct Array2D<T> : IDisposable, IEquatable<Array2D<T>>, IEnumerable<T>
    {
        #region Nested Types

        private sealed class Array2DDebugView
        {
            #region Fields

            private Array2D<T> array;

            #endregion

            #region Properties

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[,]? Items => array.To2DArray();

            #endregion

            #region Constructors

            internal Array2DDebugView(Array2D<T> array) => this.array = array;

            #endregion
        }

        #endregion

        #region Fields

        private readonly int width;
        private readonly int height;

        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Must not be readonly due to Dispose")]
        private ArraySection<T> buffer;

        #endregion

        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets the width of this <see cref="Array2D{T}"/> instance.
        /// </summary>
        public readonly int Width => width;

        /// <summary>
        /// Gets the height of this <see cref="Array2D{T}"/> instance.
        /// </summary>
        public readonly int Height => height;

        /// <summary>
        /// Gets the total length of this <see cref="Array2D{T}"/> instance.
        /// </summary>
        public readonly int Length => buffer.Length;

        /// <summary>
        /// Gets the underlying buffer as a single dimensional <see cref="ArraySection{T}"/>.
        /// </summary>
        public readonly ArraySection<T> Buffer => buffer;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns the current <see cref="Array2D{T}"/> instance as a <see cref="Memory{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public readonly Memory<T> AsMemory => buffer.AsMemory;

        /// <summary>
        /// Returns the current <see cref="Array2D{T}"/> instance as a <see cref="Span{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public readonly Span<T> AsSpan => buffer.AsSpan;
#endif

        /// <summary>
        /// Gets whether this <see cref="Array2D{T}"/> instance represents a <see langword="null"/>&#160;array.
        /// <br/>Please note that the <see cref="ToArray">ToArray</see>/<see cref="To2DArray">To2DArray</see>/<see cref="ToJaggedArray">ToJaggedArray</see> methods
        /// return <see langword="null"/>&#160;when this property returns <see langword="true"/>.
        /// </summary>
        public readonly bool IsNull => buffer.IsNull;

        /// <summary>
        /// Gets whether this <see cref="Array2D{T}"/> instance represents an empty or a <see langword="null"/>&#160;array.
        /// </summary>
        public readonly bool IsNullOrEmpty => buffer.IsNullOrEmpty;

        #endregion

        #region Indexers

        /// <summary>
        /// Gets or sets the element at the specified coordinates. Parameter order is the same as in case of a regular two-dimensional array.
        /// <br/>To return a reference to an element use the <see cref="GetElementReference">GetElementReference</see> method instead.
        /// </summary>
        /// <param name="y">The Y-coordinate (row index) of the item to get or set. Please note that for the best performance no separate range check is performed on the coordinates.</param>
        /// <param name="x">The X-coordinate (column index) of the item to get or set. Please note that for the best performance no separate range check is performed on the coordinates.</param>
        /// <returns>The element at the specified indices.</returns>
        public readonly T this[int y, int x]
        {
            // Note: for better performance we propagate the ArgumentOutOfRangeException to the buffer (allowing even negative values on some dimensions)
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get => buffer[y * width + x];
            [MethodImpl(MethodImpl.AggressiveInlining)]
            set => buffer[y * width + x] = value;
        }

        /// <summary>
        /// Gets a row of the <see cref="Array2D{T}"/> as an <see cref="ArraySection{T}"/> instance.
        /// </summary>
        /// <param name="y">The index of the row to obtain.</param>
        /// <returns>An <see cref="ArraySection{T}"/> instance that represents a row of this <see cref="Array2D{T}"/> instance.</returns>
        public readonly ArraySection<T> this[int y]
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if ((uint)y >= (uint)height)
                    Throw.ArgumentOutOfRangeException(Argument.y);
                return buffer.Slice(y * width, width);
            }
        }

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Gets a row of the <see cref="Array2D{T}"/> as an <see cref="ArraySection{T}"/> instance.
        /// </summary>
        /// <param name="y">The index of the row to obtain.</param>
        /// <returns>An <see cref="ArraySection{T}"/> instance that represents a row of this <see cref="Array2D{T}"/> instance.</returns>
        /// <remarks><note>This member is available in .NET Core 3.0/.NET Standard 2.1 and above.</note></remarks>
        public readonly ArraySection<T> this[Index y]
        {
            // Note: must be implemented explicitly because the auto generated indexer would misinterpret Length
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get => this[y.GetOffset(height)];
        }

        /// <summary>
        /// Gets a new <see cref="Array2D{T}"/> instance, which represents a subrange of rows of the current instance indicated by the specified <paramref name="range"/>.
        /// </summary>
        /// <param name="range">The range of rows to get.</param>
        /// <returns>The subrange of rows of the current <see cref="Array2D{T}"/> instance indicated by the specified <paramref name="range"/>.</returns>
        /// <remarks><note>This member is available in .NET Core 3.0/.NET Standard 2.1 and above.</note></remarks>
        public readonly Array2D<T> this[Range range]
        {
            // Note: must be implemented explicitly because the auto generated indexer would misinterpret Length
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                int startIndex = range.Start.GetOffset(height);
                return Slice(startIndex, range.End.GetOffset(height) - startIndex);
            }
        }
#endif

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
        public static implicit operator ArraySection<T>(Array2D<T> array) => array.buffer;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Performs an implicit conversion from <see cref="Array2D{T}"/> to <see cref="Span{T}"><![CDATA[Span<T>]]></see>.
        /// </summary>
        /// <param name="array">The <see cref="Array2D{T}"/> to be converted to a <see cref="Span{T}"><![CDATA[Span<T>]]></see>.</param>
        /// <returns>
        /// A <see cref="Span{T}"><![CDATA[Span<T>]]></see> instance that represents the specified <see cref="Array2D{T}"/>.
        /// </returns>
        public static implicit operator Span<T>(Array2D<T> array) => array.AsSpan;
#endif

        /// <summary>
        /// Determines whether two specified <see cref="Array2D{T}"/> instances have the same value.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(Array2D<T> a, Array2D<T> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified <see cref="Array2D{T}"/> instances have different values.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(Array2D<T> a, Array2D<T> b) => !(a == b);

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
                Throw.ArgumentOutOfRangeException(Argument.height);
            if (width < 0)
                Throw.ArgumentOutOfRangeException(Argument.width);
            this.height = height;
            this.width = width;
            buffer = new ArraySection<T>(height * width);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Array2D{T}"/> struct from an existing <see cref="ArraySection{T}"/>
        /// using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// No heap allocation occurs when using this constructor overload.
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
                Throw.ArgumentOutOfRangeException(Argument.height);
            if (width < 0)
                Throw.ArgumentOutOfRangeException(Argument.width);
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
        /// Gets a new <see cref="Array2D{T}"/> instance, which represents a subrange of rows of the current instance starting with the specified <paramref name="startRowIndex"/>.
        /// </summary>
        /// <param name="startRowIndex">The offset that points to the first row of the returned <see cref="Array2D{T}"/>.</param>
        /// <returns>The subrange of rows of the current <see cref="Array2D{T}"/> instance starting with the specified <paramref name="startRowIndex"/>.</returns>
        public readonly Array2D<T> Slice(int startRowIndex) => new Array2D<T>(buffer.Slice(startRowIndex * width), height - startRowIndex, width);

        /// <summary>
        /// Gets a new <see cref="Array2D{T}"/> instance, which represents a subrange of rows of the current instance indicated by the specified <paramref name="startRowIndex"/> and <paramref name="rowCount"/>.
        /// </summary>
        /// <param name="startRowIndex">The offset that points to the first row of the returned <see cref="Array2D{T}"/>.</param>
        /// <param name="rowCount">The desired number of rows of the returned <see cref="Array2D{T}"/>.</param>
        /// <returns>The subrange of rows of the current <see cref="Array2D{T}"/> instance indicated by the specified <paramref name="startRowIndex"/> and <paramref name="rowCount"/>.</returns>
        public readonly Array2D<T> Slice(int startRowIndex, int rowCount) => new Array2D<T>(buffer.Slice(startRowIndex * width, rowCount * width), rowCount, width);

        /// <summary>
        /// Gets the reference to the element at the specified indices. Parameter order is the same as in case of a regular two-dimensional array.
        /// </summary>
        /// <param name="y">The Y-coordinate (row index) of the item to get or set. Please note that for the best performance no separate range check is performed on the coordinates.</param>
        /// <param name="x">The X-coordinate (column index) of the item to get or set. Please note that for the best performance no separate range check is performed on the coordinates.</param>
        /// <returns>The reference to the element at the specified index.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public readonly ref T GetElementReference(int y, int x)
        {
            // Note: for better performance we propagate the ArgumentOutOfRangeException to the buffer (allowing even negative values on some dimensions)
            return ref buffer.GetElementReference(y * width + x);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the items of this <see cref="Array2D{T}"/>.
        /// </summary>
        /// <returns>An <see cref="ArraySectionEnumerator{T}"/> instance that can be used to iterate though the elements of this <see cref="Array2D{T}"/>.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public readonly ArraySectionEnumerator<T> GetEnumerator() => buffer.GetEnumerator();

        /// <summary>
        /// Releases the underlying buffer. If this <see cref="Array2D{T}"/> instance was instantiated by the <see cref="Array2D{T}(int,int)">self allocating constructor</see>,
        /// then this method must be called when the <see cref="Array2D{T}"/> is not used anymore.
        /// On platforms that do not support the <see cref="ArrayPool{T}"/> class this method simply clears the self instance.
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
        public readonly ref T GetPinnableReference() => ref buffer.GetPinnableReference();

        /// <summary>
        /// Indicates whether the current <see cref="Array2D{T}"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// </summary>
        /// <param name="other">An <see cref="Array2D{T}"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public readonly bool Equals(Array2D<T> other) => width == other.width && height == other.height && buffer.Equals(other.buffer);

        /// <summary>
        /// Determines whether the specified <see cref="object">object</see> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified object is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public readonly override bool Equals(object? obj) => obj is Array2D<T> other && Equals(other);

        /// <summary>
        /// Returns a hash code for this <see cref="Array2D{T}"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode",
            Justification = "Field 'buffer' is practically read-only but it is not marked as so to prevent creating defensive copies")]
        public readonly override int GetHashCode()
        {
            if (buffer.IsNull)
                return 0;
            return (buffer, width, height).GetHashCode();
        }

        /// <summary>
        /// Copies the elements of this <see cref="Array2D{T}"/> to a new single dimensional array.
        /// </summary>
        /// <returns>An array containing copies of the elements of this <see cref="Array2D{T}"/>,
        /// or <see langword="null"/>&#160;if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        public readonly T[]? ToArray() => buffer.ToArray();

        /// <summary>
        /// Copies the elements of this <see cref="Array2D{T}"/> to a new two dimensional array.
        /// </summary>
        /// <returns>An array containing copies of the elements of this <see cref="Array2D{T}"/>,
        /// or <see langword="null"/>&#160;if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        public readonly T[,]? To2DArray()
        {
            if (buffer.IsNull)
                return null;
            var result = new T[height, width];
            int i = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x] = buffer.GetItemInternal(i);
                    i += 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Copies the elements of this <see cref="Array2D{T}"/> to a new jagged array.
        /// </summary>
        /// <returns>An array containing copies of the elements of this <see cref="Array2D{T}"/>,
        /// or <see langword="null"/>&#160;if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        public readonly T[][]? ToJaggedArray()
        {
            if (buffer.IsNull)
                return null;
            T[][] result = new T[height][];
            int i = 0;
            for (int y = 0; y < height; y++)
            {
                T[] row = new T[width];
                result[y] = row;
                for (int x = 0; x < width; x++)
                {
                    row[x] = buffer.GetItemInternal(i);
                    i += 1;
                }
            }

            return result;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}
