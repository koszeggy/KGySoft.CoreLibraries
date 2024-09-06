#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CastArray3D.cs
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
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
using System.Buffers;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Security;

#endregion

#region Suppressions

#if NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif
#if !NET5_0_OR_GREATER
// ReSharper disable UnusedMember.Local - CastArray3DDebugView.Items
#endif

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a cubic (three-dimensional) array backed by a single-dimensional array of element type <typeparamref name="TFrom"/>
    /// where the reinterpreted element type is cast to <typeparamref name="TTo"/>.
    /// It supports accessing its planes as <see cref="CastArray2D{TFrom,TTo}"/> instances, or the whole content as a single dimensional <see cref="CastArray{TFrom,TTo}"/>.
    /// Depending on the used platform the reinterpreted elements can also be accessed as a <see cref="Span{T}"/>.
    /// </summary>
    /// <typeparam name="TFrom">The actual element type of the underlying array.</typeparam>
    /// <typeparam name="TTo">The reinterpreted element type of the underlying array.</typeparam>
    /// <remarks>
    /// <para>In .NET Core 2.1/.NET Standard 2.1 and above a <see cref="CastArray3D{TFrom,TTo}"/> instance can be easily turned to a <see cref="Span{T}"/> instance (either by cast or by the <see cref="AsSpan"/> property).</para>
    /// <para>The single dimensional buffer can be accessed by the <see cref="Buffer"/> property that returns a <see cref="CastArray{TFrom,TTo}"/> structure.
    /// The actual underlying single dimensional array can be accessed via its <see cref="CastArray{TFrom,TTo}.Buffer"/> property that returns
    /// an <see cref="ArraySection{T}"/> structure and has an <see cref="ArraySection{T}.UnderlyingArray"/> property.</para>
    /// <para>Unlike <see cref="Array3D{T}"/>, <see cref="CastArray3D{TFrom,TTo}"/> has no self-allocating constructors and it does not implement the <see cref="IDisposable"/> interface.
    /// But you can pass an <see cref="ArraySection{T}"/> instance to the constructor that allocated a buffer by itself. In such case it's the caller's responsibility to
    /// call the <see cref="ArraySection{T}.Release">Release</see> method in the end to return the possibly rented array to the pool.</para>
    /// <note type="tip">See more details and some examples about KGy SOFT's span-like types at the <strong>Remarks</strong> section of the <see cref="ArraySection{T}"/> type.</note>
    /// </remarks>
    /// <seealso cref="ArraySection{T}"/>
    /// <seealso cref="Array2D{T}"/>
    /// <seealso cref="Array3D{T}"/>
    /// <seealso cref="CastArray{TFrom,TTo}"/>
    /// <seealso cref="CastArray2D{TFrom,TTo}"/>
    [Serializable]
    [DebuggerDisplay("{typeof(" + nameof(TTo) + ")." + nameof(Type.Name) + ",nq}[{" + nameof(Height) + "}, {" + nameof(Width) + "}]")]
    [DebuggerTypeProxy(typeof(CastArray3D<,>.CastArray3DDebugView))]
    public readonly struct CastArray3D<TFrom, TTo> : IEquatable<CastArray3D<TFrom, TTo>>, IEnumerable<TTo>
#if NETFRAMEWORK // To make the type compatible with older compilers. Unmanaged is asserted in the wrapped CastArray<TFrom, TTo>.
        where TFrom : struct
        where TTo : struct
#else
        where TFrom : unmanaged
        where TTo : unmanaged
#endif
    {
        #region Nested Types

        private sealed class CastArray3DDebugView
        {
            #region Fields

            private CastArray3D<TFrom, TTo> array;

            #endregion

            #region Properties

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public TTo[,,]? Items => array.To3DArray();

            #endregion

            #region Constructors

            internal CastArray3DDebugView(CastArray3D<TFrom, TTo> array) => this.array = array;

            #endregion
        }

        #endregion

        #region Fields

        private readonly CastArray<TFrom, TTo> buffer;
        private readonly int depth;
        private readonly int height;
        private readonly int width;

        [NonSerialized]
        private readonly int planeSize; // cached value of height * width

        #endregion

        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets the width of this <see cref="CastArray3D{TFrom,TTo}"/> instance.
        /// </summary>
        public int Width => width;

        /// <summary>
        /// Gets the height of this <see cref="CastArray3D{TFrom,TTo}"/> instance.
        /// </summary>
        public int Height => height;

        /// <summary>
        /// Gets the depth of this <see cref="CastArray3D{TFrom,TTo}"/> instance.
        /// </summary>
        public int Depth => depth;

        /// <summary>
        /// Gets the total number of <typeparamref name="TTo"/> elements in this <see cref="CastArray3D{TFrom,TTo}"/> instance.
        /// </summary>
        public int Length => buffer.Length;

        /// <summary>
        /// Gets the underlying buffer as a single dimensional <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        public CastArray<TFrom, TTo> Buffer => buffer;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns this <see cref="CastArray3D{TFrom,TTo}"/> as a <see cref="Memory{T}"/> instance.
        /// Please note that getting this property allocates a custom <see cref="MemoryManager{T}"/> instance on the heap internally.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public Memory<TTo> AsMemory => buffer.AsMemory;

        /// <summary>
        /// Returns this <see cref="CastArray3D{TFrom,TTo}"/> as a <see cref="Span{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public Span<TTo> AsSpan => buffer.AsSpan;
#endif

        /// <summary>
        /// Gets whether this <see cref="CastArray3D{TFrom,TTo}"/> instance represents a <see langword="null"/> array.
        /// <br/>Please note that the <see cref="ToArray">ToArray</see>/<see cref="To3DArray">To3DArray</see>/<see cref="ToJaggedArray">ToJaggedArray</see> methods
        /// return <see langword="null"/> when this property returns <see langword="true"/>.
        /// </summary>
        public bool IsNull => buffer.IsNull;

        /// <summary>
        /// Gets whether this <see cref="CastArray3D{TFrom,TTo}"/> instance represents an empty or a <see langword="null"/> array.
        /// </summary>
        public bool IsNullOrEmpty => buffer.IsNullOrEmpty;

        #endregion

        #region Indexers

        /// <summary>
        /// Gets or sets the element at the specified indices. Parameter order is the same as in case of a regular three-dimensional array.
        /// </summary>
        /// <param name="z">The Z-coordinate (depth index) of the item to get or set.</param>
        /// <param name="y">The Y-coordinate (row index) of the item to get or set.</param>
        /// <param name="x">The X-coordinate (column index) of the item to get or set.</param>
        /// <returns>The element at the specified indices.</returns>
        /// <remarks>
        /// <para>Though this member does not validate the coordinates separately, it does not allow indexing beyond the <see cref="Length"/> of the underlying <see cref="Buffer"/>.
        /// To omit also the length check use the <see cref="GetElementUnsafe">GetElementUnsafe</see>/<see cref="SetElementUnsafe">SetElementUnsafe</see> methods instead.</para>
        /// <para>If the compiler you use supports members that return a value by reference, you can also use the <see cref="GetElementReference">GetElementReference</see> method.</para>
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">The specified indices refer to an item outside the bounds of the underlying <see cref="Buffer"/>.</exception>
        /// <exception cref="NotSupportedException">.NET Framework only: you access this member in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        public TTo this[int z, int y, int x]
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get => buffer[z * planeSize + y * width + x];
            [MethodImpl(MethodImpl.AggressiveInlining)]
            set => buffer[z * planeSize + y * width + x] = value;
        }

        /// <summary>
        /// Gets a plane of the <see cref="CastArray3D{TFrom,TTo}"/> as a <see cref="CastArray2D{TFrom,TTo}"/> instance.
        /// Please note that the size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> must be divisible by the size of <typeparamref name="TFrom"/>.
        /// </summary>
        /// <param name="z">The depth index of the plane to obtain.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance that represents a plane of this <see cref="CastArray3D{TFrom,TTo}"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="z"/> is out of range.</exception>
        /// <exception cref="ArgumentException">The size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> is not divisible by the size of <typeparamref name="TFrom"/>.</exception>
        /// <remarks>
        /// <note>If the size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> is not divisible by the size of <typeparamref name="TFrom"/>,
        /// then this property throws an <see cref="ArgumentException"/>. If the targeted platform supports the <see cref="Span{T}"/> type and misaligned memory access,
        /// then you can try to use the <see cref="AsSpan"/> property and the <see cref="MemoryMarshal.Cast{TFrom,TTo}(Span{TFrom})">MemoryMarshal.Cast</see> method.</note>
        /// </remarks>
        public CastArray2D<TFrom, TTo> this[int z]
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if ((uint)z >= (uint)depth)
                    Throw.ArgumentOutOfRangeException(Argument.y);
                return new CastArray2D<TFrom, TTo>(buffer.Slice(z * planeSize, planeSize), height, width);
            }
        }

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Gets a plane of the <see cref="CastArray3D{TFrom,TTo}"/> as a <see cref="CastArray2D{TFrom,TTo}"/> instance.
        /// Please note that the size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> must be divisible by the size of <typeparamref name="TFrom"/>.
        /// </summary>
        /// <param name="z">The depth index of the plane to obtain.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance that represents a plane of this <see cref="CastArray3D{TFrom,TTo}"/> instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="z"/> is out of range.</exception>
        /// <exception cref="ArgumentException">The size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> is not divisible by the size of <typeparamref name="TFrom"/>.</exception>
        /// <remarks>
        /// <note><list type="bullet">
        /// <item>If the size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> is not divisible by the size of <typeparamref name="TFrom"/>,
        /// then this property throws an <see cref="ArgumentException"/>. If the targeted platform supports the <see cref="Span{T}"/> type and misaligned memory access,
        /// then you can try to use the <see cref="AsSpan"/> property and the <see cref="MemoryMarshal.Cast{TFrom,TTo}(Span{TFrom})">MemoryMarshal.Cast</see> method.</item>
        /// <item>This member is available in .NET Core 3.0/.NET Standard 2.1 and above.</item>
        /// </list></note>
        /// </remarks>
        public CastArray2D<TFrom, TTo> this[Index z]
        {
            // Note: must be implemented explicitly because the auto generated indexer would misinterpret Length
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get => this[z.GetOffset(depth)];
        }

        /// <summary>
        /// Gets a new <see cref="CastArray3D{TFrom,TTo}"/> instance, which represents a subrange of planes of the current instance indicated by the specified <paramref name="range"/>.
        /// Please note that the size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> must be divisible by the size of <typeparamref name="TFrom"/>.
        /// </summary>
        /// <param name="range">The range of rows to get.</param>
        /// <returns>The subrange of planes of the current <see cref="CastArray3D{TFrom,TTo}"/> instance indicated by the specified <paramref name="range"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="range"/> is out of range.</exception>
        /// <exception cref="ArgumentException">The size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> is not divisible by the size of <typeparamref name="TFrom"/>.</exception>
        /// <remarks>
        /// <note><list type="bullet">
        /// <item>If the size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> is not divisible by the size of <typeparamref name="TFrom"/>,
        /// then this property throws an <see cref="ArgumentException"/>. If the targeted platform supports the <see cref="Span{T}"/> type and misaligned memory access,
        /// then you can try to use the <see cref="AsSpan"/> property and the <see cref="MemoryMarshal.Cast{TFrom,TTo}(Span{TFrom})">MemoryMarshal.Cast</see> method.</item>
        /// <item>This member is available in .NET Core 3.0/.NET Standard 2.1 and above.</item>
        /// </list></note>
        /// </remarks>
        public CastArray3D<TFrom, TTo> this[Range range]
        {
            // Note: must be implemented explicitly because the auto generated indexer would misinterpret Length
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                int startIndex = range.Start.GetOffset(height);
                return Slice(startIndex, range.End.GetOffset(depth) - startIndex);
            }
        }
#endif

        #endregion

        #endregion

        #region Operators

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Performs an implicit conversion from <see cref="CastArray3D{TFrom,TTo}"/> to <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="array">The <see cref="CastArray3D{TFrom,TTo}"/> to be converted to a <see cref="Span{T}"/> of element type <typeparamref name="TTo"/>.</param>
        /// <returns>
        /// A <see cref="Span{T}"/> of element type <typeparamref name="TTo"/> that represents the specified <see cref="CastArray3D{TFrom,TTo}"/>.
        /// </returns>
        public static implicit operator Span<TTo>(CastArray3D<TFrom, TTo> array) => array.AsSpan;
#endif

        /// <summary>
        /// Determines whether two specified <see cref="CastArray3D{TFrom,TTo}"/> instances have the same value.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(CastArray3D<TFrom, TTo> a, CastArray3D<TFrom, TTo> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified <see cref="CastArray3D{TFrom,TTo}"/> instances have different values.
        /// </summary>
        /// <param name="a">The left argument of the inequality check.</param>
        /// <param name="b">The right argument of the inequality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(CastArray3D<TFrom, TTo> a, CastArray3D<TFrom, TTo> b) => !(a == b);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CastArray3D{TFrom,TTo}"/> struct from an existing <see cref="CastArray{TFrom,TTo}"/>
        /// using the specified <paramref name="depth"/>, <paramref name="height"/> and <paramref name="width"/>.
        /// </summary>
        /// <param name="buffer">The desired underlying buffer for the <see cref="CastArray3D{TFrom,TTo}"/> instance to be created.
        /// It must have sufficient capacity for the specified dimensions.</param>
        /// <param name="depth">The depth of the array to be created.</param>
        /// <param name="height">The height of the array to be created.</param>
        /// <param name="width">The width of the array to be created.</param>
        public CastArray3D(CastArray<TFrom, TTo> buffer, int depth, int height, int width)
        {
            if (buffer.IsNull)
                Throw.ArgumentNullException(Argument.buffer);
            if (height < 0)
                Throw.ArgumentOutOfRangeException(Argument.width);
            if (width < 0)
                Throw.ArgumentOutOfRangeException(Argument.height);
            planeSize = height * width;
            int size = depth * planeSize;
            if (buffer.Length < size)
                Throw.ArgumentException(Argument.buffer, Res.ArraySectionInsufficientCapacity);

            // Slicing when capacity was bigger than needed. This must always work because it already starts at TFrom boundary so using the faster constructor.
            this.buffer = size == buffer.Length ? buffer : new CastArray<TFrom, TTo>(buffer.Buffer, size);
            this.depth = depth;
            this.height = height;
            this.width = width;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CastArray3D{TFrom,TTo}"/> struct from an existing <see cref="ArraySection{T}"/>
        /// using the specified <paramref name="depth"/>, <paramref name="height"/> and <paramref name="width"/>.
        /// </summary>
        /// <param name="buffer">The desired underlying buffer for the <see cref="CastArray3D{TFrom,TTo}"/> instance to be created.
        /// It must have sufficient capacity for the specified dimensions.</param>
        /// <param name="depth">The depth of the array to be created.</param>
        /// <param name="height">The height of the array to be created.</param>
        /// <param name="width">The width of the array to be created.</param>
        public CastArray3D(ArraySection<TFrom> buffer, int depth, int height, int width)
            : this(buffer.Cast<TFrom, TTo>(), depth, height, width)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Gets a new <see cref="CastArray3D{TFrom,TTo}"/> instance, which represents a subrange of planes of the current instance starting with the specified <paramref name="startPlaneIndex"/>.
        /// Please note that the size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> must be divisible by the size of <typeparamref name="TFrom"/>.
        /// </summary>
        /// <param name="startPlaneIndex">The offset that points to the first plane of the returned <see cref="CastArray3D{TFrom,TTo}"/>.</param>
        /// <returns>The subrange of planes of the current <see cref="CastArray3D{TFrom,TTo}"/> instance starting with the specified <paramref name="startPlaneIndex"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startPlaneIndex"/> is out of range.</exception>
        /// <exception cref="ArgumentException">The size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> is not divisible by the size of <typeparamref name="TFrom"/>.</exception>
        public CastArray3D<TFrom, TTo> Slice(int startPlaneIndex) => new CastArray3D<TFrom, TTo>(buffer.Slice(startPlaneIndex * planeSize), depth - startPlaneIndex, height, width);

        /// <summary>
        /// Gets a new <see cref="CastArray3D{TFrom,TTo}"/> instance, which represents a subrange of planes of the current instance starting with the specified <paramref name="startPlaneIndex"/> and <paramref name="planeCount"/>.
        /// Please note that the size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> must be divisible by the size of <typeparamref name="TFrom"/>.
        /// </summary>
        /// <param name="startPlaneIndex">The offset that points to the first plane of the returned <see cref="CastArray3D{TFrom,TTo}"/>.</param>
        /// <param name="planeCount">The desired number of planes of the returned <see cref="CastArray3D{TFrom,TTo}"/>.</param>
        /// <returns>The subrange of planes of the current <see cref="CastArray3D{TFrom,TTo}"/> instance indicated by the specified <paramref name="startPlaneIndex"/> and <paramref name="planeCount"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startPlaneIndex"/> or <paramref name="planeCount"/> is out of range.</exception>
        /// <exception cref="ArgumentException">The size of <typeparamref name="TTo"/> multiplied by <see cref="Width"/> times <see cref="Height"/> is not divisible by the size of <typeparamref name="TFrom"/>.</exception>
        public CastArray3D<TFrom, TTo> Slice(int startPlaneIndex, int planeCount) => new CastArray3D<TFrom, TTo>(buffer.Slice(startPlaneIndex * planeSize, planeCount * planeSize), planeCount, height, width);

        /// <summary>
        /// Gets the reference to the element at the specified indices. Parameter order is the same as in case of a regular three-dimensional array.
        /// </summary>
        /// <param name="z">The Z-coordinate (depth index) of the item to get the reference for.</param>
        /// <param name="y">The Y-coordinate (row index) of the item to get the reference for.</param>
        /// <param name="x">The X-coordinate (column index) of the item to get the reference for.</param>
        /// <returns>The reference to the element at the specified coordinates.</returns>
        /// <remarks>
        /// <para>Though this method does not validate the coordinates separately, it does not allow indexing beyond the <see cref="Length"/> of the underlying <see cref="Buffer"/>.
        /// To allow getting any item in the actual underlying array use
        /// then use the <see cref="GetElementReferenceUnsafe">GetElementReferenceUnsafe</see> method instead.</para>
        /// <note>This method returns a value by reference. If this library is used by an older compiler that does not support such members,
        /// use the <see cref="this[int,int,int]">indexer</see> instead.</note>
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">The specified indices refer to an item outside the bounds of the underlying <see cref="Buffer"/>.</exception>
        /// <exception cref="VerificationException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public ref TTo GetElementReference(int z, int y, int x) => ref buffer.GetElementReference(z * planeSize + y * width + x);

        /// <summary>
        /// Gets the element at the specified indices without any range check.
        /// To validate the coordinates against <see cref="Length"/> use the appropriate <see cref="this[int,int,int]">indexer</see> instead.
        /// Parameter order is the same as in case of a regular three-dimensional array.
        /// </summary>
        /// <param name="z">The Z-coordinate (depth index) of the item to get the reference for.</param>
        /// <param name="y">The Y-coordinate (row index) of the item to get.</param>
        /// <param name="x">The X-coordinate (column index) of the item to get.</param>
        /// <returns>The element at the specified indices.</returns>
        /// <remarks>
        /// <note type="caution">You must ensure that the specified indices designate an element in the bounds
        /// of the actual underlying array. Attempting to access protected memory may crash the runtime.</note>
        /// <para>If the compiler you use supports members that return a value by reference, you can also use
        /// the <see cref="GetElementReferenceUnsafe">GetElementReferenceUnsafe</see> method.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> returns <see langword="true"/>.</exception>
        /// <exception cref="NotSupportedException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TTo GetElementUnsafe(int z, int y, int x) => buffer.GetElementUnsafe(z * planeSize + y * width + x);

        /// <summary>
        /// Sets the element at the specified indices without any range check.
        /// To validate the coordinates against <see cref="Length"/> use the appropriate <see cref="this[int,int,int]">indexer</see> instead.
        /// Parameter order is the same as in case of a regular three-dimensional array.
        /// </summary>
        /// <param name="z">The Z-coordinate (depth index) of the item to get the reference for.</param>
        /// <param name="y">The Y-coordinate (row index) of the item to set.</param>
        /// <param name="x">The X-coordinate (column index) of the item to set.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// <note type="caution">You must ensure that the specified indices designate an element in the bounds
        /// of the actual underlying array. Attempting to access protected memory may crash the runtime.</note>
        /// <para>If the compiler you use supports members that return a value by reference, you can also use
        /// the <see cref="GetElementReferenceUnsafe">GetElementReferenceUnsafe</see> method.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> returns <see langword="true"/>.</exception>
        /// <exception cref="NotSupportedException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetElementUnsafe(int z, int y, int x, TTo value) => buffer.SetElementUnsafe(z * planeSize + y * width + x, value);

        /// <summary>
        /// Gets the reference to the element at the specified coordinates without any range check.
        /// To validate the coordinates against <see cref="Length"/> use
        /// the <see cref="GetElementReference">GetElementReference</see> method instead.
        /// Parameter order is the same as in case of a regular three-dimensional array.
        /// </summary>
        /// <param name="z">The Z-coordinate (depth index) of the item to get the reference for.</param>
        /// <param name="y">The Y-coordinate (row index) of the item to get the reference for.</param>
        /// <param name="x">The X-coordinate (column index) of the item to get the reference for.</param>
        /// <returns>The reference to the element at the specified coordinates.</returns>
        /// <remarks>
        /// <note type="caution">You must ensure that the specified indices designate an element in the bounds
        /// of the actual underlying array. Attempting to access protected memory may crash the runtime.</note>
        /// <note>This method returns a value by reference. If this library is used by an older compiler that does not support such members,
        /// use the <see cref="GetElementUnsafe">GetElementUnsafe</see>/<see cref="SetElementUnsafe">SetElementUnsafe</see> methods instead.</note>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> returns <see langword="true"/>.</exception>
        /// <exception cref="VerificationException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public ref TTo GetElementReferenceUnsafe(int z, int y, int x) => ref buffer.GetElementReferenceUnsafe(z * planeSize + y * width + x);

        /// <summary>
        /// Returns an enumerator that iterates through the items of this <see cref="CastArray3D{TFrom,TTo}"/>.
        /// </summary>
        /// <returns>A <see cref="CastArrayEnumerator{TFrom,TTo}"/> instance that can be used to iterate though the elements of this <see cref="CastArray3D{TFrom,TTo}"/>.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public CastArrayEnumerator<TFrom, TTo> GetEnumerator() => buffer.GetEnumerator();

        /// <summary>
        /// Returns a reference to the first element in this <see cref="CastArray3D{TFrom,TTo}"/>.
        /// This makes possible to use the <see cref="CastArray3D{TFrom,TTo}"/> in a <see langword="fixed"/> statement.
        /// </summary>
        /// <returns>A reference to the first element in this <see cref="CastArray3D{TFrom,TTo}"/>.</returns>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> is <see langword="true"/>.</exception>
        /// <exception cref="VerificationException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        public ref TTo GetPinnableReference() => ref buffer.GetPinnableReference();

        /// <summary>
        /// Indicates whether the current <see cref="CastArray3D{TFrom,TTo}"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// </summary>
        /// <param name="other">A <see cref="CastArray3D{TFrom,TTo}"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public bool Equals(CastArray3D<TFrom, TTo> other) => width == other.width && height == other.height && depth == other.depth && buffer.Equals(other.buffer);

        /// <summary>
        /// Determines whether the specified <see cref="object">object</see> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if the specified object is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => obj is CastArray3D<TFrom, TTo> other && Equals(other);

        /// <summary>
        /// Returns a hash code for this <see cref="CastArray3D{TFrom,TTo}"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            if (buffer.IsNull)
                return 0;
            return (buffer, width, height, depth).GetHashCode();
        }

        /// <summary>
        /// Copies the elements of this <see cref="CastArray3D{TFrom,TTo}"/> to a new single dimensional array of element type <typeparamref name="TTo"/>.
        /// </summary>
        /// <returns>An array of element type <typeparamref name="TTo"/> containing copies of the elements of this <see cref="CastArray3D{TFrom,TTo}"/>,
        /// or <see langword="null"/> if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        public TTo[]? ToArray() => buffer.ToArray();

        /// <summary>
        /// Copies the elements of this <see cref="CastArray3D{TFrom,TTo}"/> to a new three-dimensional array of element type <typeparamref name="TTo"/>.
        /// </summary>
        /// <returns>An array of element type <typeparamref name="TTo"/> containing copies of the elements of this <see cref="CastArray3D{TFrom,TTo}"/>,
        /// or <see langword="null"/> if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        [SecuritySafeCritical]
        public TTo[,,]? To3DArray()
        {
            if (buffer.IsNull)
                return null;
            var result = new TTo[depth, height, width];
            int i = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        result[z, y, x] = buffer.GetElementReferenceInternal(i);
                        i += 1;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Copies the elements of this <see cref="CastArray3D{TFrom,TTo}"/> to a new jagged array of element type <typeparamref name="TTo"/>.
        /// </summary>
        /// <returns>An array containing copies of the elements of this <see cref="CastArray3D{TFrom,TTo}"/>,
        /// or <see langword="null"/> if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        [SecuritySafeCritical]
        public TTo[][][]? ToJaggedArray()
        {
            if (buffer.IsNull)
                return null;
            TTo[][][] result = new TTo[depth][][];
            int i = 0;
            for (int z = 0; z < depth; z++)
            {
                TTo[][] plane = new TTo[height][];
                result[z] = plane;
                for (int y = 0; y < height; y++)
                {
                    TTo[] row = new TTo[width];
                    plane[y] = row;
                    for (int x = 0; x < width; x++)
                    {
                        row[x] = buffer.GetElementReferenceInternal(i);
                        i += 1;
                    }
                }
            }

            return result;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator<TTo> IEnumerable<TTo>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}