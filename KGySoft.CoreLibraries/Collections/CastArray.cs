#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CastArray.cs
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
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a one dimensional array (or a section of it) where the original element type of <typeparamref name="TFrom"/> is cast to another element type of <typeparamref name="TTo"/>.
    /// Can be helpful on platforms where <see cref="Span{T}"/> is not available or wherever you must use arrays that you want to reinterpret. For example, you want to retrieve
    /// only byte arrays from the <see cref="ArrayPool{T}"/> but you want to reinterpret them as other array types.
    /// </summary>
    /// <typeparam name="TFrom">The actual element type of the underlying array.</typeparam>
    /// <typeparam name="TTo">The reinterpreted element type of the underlying array.</typeparam>
    /// <remarks>
    /// TODO: Pooling example. Returning to the pool must be done by the caller. You can use a self-allocating ArraySection, which can be released in the end.
    /// TODO: Slice may throw ArgumentException
    /// </remarks>
    public readonly struct CastArray<TFrom, TTo> : /*IList<TTo>, IList,*/ IEquatable<CastArray<TFrom, TTo>> // TODO
#if !(NET35 || NET40)
        //, IReadOnlyList<TTo>
#endif
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        // TODO: Collection interfaces
        // TODO: ToArray
        // TODO: Debugger info

        #region Nested Types

        #region CastArrayMemoryManager class

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private sealed class CastArrayMemoryManager : MemoryManager<TTo>
        {
            #region Fields

            private readonly CastArray<TFrom, TTo> castArray;

            private GCHandle pinnedHandle;
            private int pinCount;

            #endregion

            #region Constructors

            internal CastArrayMemoryManager(CastArray<TFrom, TTo> castArray) => this.castArray = castArray;

            #endregion

            #region Methods

            #region Public Methods

            public override Span<TTo> GetSpan() => castArray.AsSpan;

            public override unsafe MemoryHandle Pin(int elementIndex = 0)
            {
                // This must be before mutating anything because the index validation can throw an exception.
                ref TTo refResult = ref castArray[elementIndex];

                // It's alright to lock on this, this instance is not exposed publicly.
                lock (this)
                {
                    if (!pinnedHandle.IsAllocated)
                        pinnedHandle = GCHandle.Alloc(castArray.buffer.UnderlyingArray, GCHandleType.Pinned);
                }

                Interlocked.Increment(ref pinCount);

                // Not returning the GCHandle in the result because if there are concurrent pinners, they could unpin the memory too early.
                // Passing only this instance so Unpin will be called that handles everything correctly.
#if NETCOREAPP3_0_OR_GREATER
                return new MemoryHandle(Unsafe.AsPointer(ref refResult), default, this);
#else
                // Actually fixed is not needed to pin the reference here, but the cast does not without it...
                fixed (void* ptr = &refResult)
                    return new MemoryHandle(ptr, default, this);
#endif
            }

            public override void Unpin()
            {
                // Can occur if a MemoryHandle of Pin was copied and more than one copies were disposed.
                if (!pinnedHandle.IsAllocated)
                    return;

                if (Interlocked.Decrement(ref pinCount) == 0)
                    pinnedHandle.Free();
            }

            #endregion

            #region Protected Methods

            protected override void Dispose(bool disposing)
            {
                if (pinnedHandle.IsAllocated)
                    pinnedHandle.Free();
            }

            #endregion

            #endregion
        }
#endif

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        /// <summary>
        /// Represents the <see langword="null"/>&#160;<see cref="CastArray{TFrom,TTo}"/>. This field is read-only.
        /// </summary>
        public static readonly CastArray<TFrom, TTo> Null = default;

        /// <summary>
        /// Represents the empty <see cref="CastArray{TFrom,TTo}"/>. This field is read-only.
        /// </summary>
        public static readonly CastArray<TFrom, TTo> Empty = new CastArray<TFrom, TTo>(ArraySection<TFrom>.Empty);

        #endregion

        #region Instance Fields

        private readonly ArraySection<TFrom> buffer;
        private readonly int length;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets the underlying buffer of this <see cref="CastArray{TFrom,TTo}"/> as an <see cref="ArraySection{T}"/> instance.
        /// </summary>
        public ArraySection<TFrom> Buffer => buffer;

        /// <summary>
        /// Gets the number of <typeparamref name="TTo"/> elements in this <see cref="CastArray{TFrom,TTo}"/>.
        /// To get the number of <typeparamref name="TFrom"/> elements use the <see cref="Buffer"/> property.
        /// </summary>
        public int Length => length;

        public bool IsNull => buffer.IsNull;

        public bool IsNullOrEmpty => buffer.IsNullOrEmpty;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns this <see cref="CastArray{TFrom,TTo}"/> as a <see cref="Memory{T}"/> instance.
        /// Please note that getting this property allocates a custom <see cref="MemoryManager{T}"/> instance internally.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public Memory<TTo> AsMemory => IsNull ? default : new CastArrayMemoryManager(this).Memory;

        /// <summary>
        /// Returns this <see cref="CastArray{TFrom,TTo}"/> as a <see cref="Span{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public Span<TTo> AsSpan => MemoryMarshal.Cast<TFrom, TTo>(buffer.AsSpan);
#endif

        #endregion

        #region Indexers

        public ref TTo this[int index]
        {
            [SecuritySafeCritical]
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)length)
                    Throw.IndexOutOfRangeException();
                return ref UnsafeGetRef(index);
            }
        }

        #endregion

        #endregion

        #region Operators

        /// <summary>
        /// Performs an implicit conversion from array of <typeparamref name="TFrom"/> to <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="array">The array to be converted to a <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <returns>
        /// A <see cref="CastArray{TFrom,TTo}"/> instance that represents the original array cast to an array of <typeparamref name="TTo"/>.
        /// </returns>
        public static implicit operator CastArray<TFrom, TTo>(TFrom[]? array) => new CastArray<TFrom, TTo>(array.AsSection());

        /// <summary>
        /// Performs an implicit conversion from <see cref="ArraySegment{T}"/> to <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="arraySegment">The <see cref="ArraySegment{T}"/> to be converted to a <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <returns>
        /// A <see cref="CastArray{TFrom,TTo}"/> instance that represents the original <see cref="ArraySegment{T}"/> cast to an array of <typeparamref name="TTo"/>.
        /// </returns>
        public static implicit operator CastArray<TFrom, TTo>(ArraySegment<TFrom> arraySegment) => new CastArray<TFrom, TTo>(arraySegment.AsSection());

        /// <summary>
        /// Performs an implicit conversion from <see cref="ArraySection{T}"/> to <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="arraySection">The <see cref="ArraySection{T}"/> to be converted to a <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <returns>
        /// A <see cref="CastArray{TFrom,TTo}"/> instance that represents the original <see cref="ArraySection{T}"/> cast to an array of <typeparamref name="TTo"/>.
        /// </returns>
        public static implicit operator CastArray<TFrom, TTo>(ArraySection<TFrom> arraySection) => new CastArray<TFrom, TTo>(arraySection);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Performs an implicit conversion from <see cref="CastArray{TFrom,TTo}"/> to <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="castArray">The <see cref="CastArray{TFrom,TTo}"/> to be converted to a <see cref="Span{T}"/>.</param>
        /// <returns>
        /// A <see cref="Span{T}"/> instance that represents the specified <see cref="CastArray{TFrom,TTo}"/>.
        /// </returns>
        public static implicit operator Span<TTo>(CastArray<TFrom, TTo> castArray) => castArray.AsSpan;
#endif

        /// <summary>
        /// Determines whether two specified <see cref="CastArray{TFrom,TTo}"/> instances have the same value.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(CastArray<TFrom, TTo> a, CastArray<TFrom, TTo> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified <see cref="CastArray{TFrom,TTo}"/> instances have different values.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(CastArray<TFrom, TTo> a, CastArray<TFrom, TTo> b) => !(a == b);

        #endregion

        #region Constructors

        #region Static Constructor

        static CastArray()
        {
            // The unmanaged constraint is just a C# thing for new compilers but the CLR still allows any struct.
            if (typeof(TFrom).IsManaged())
                Throw.InvalidOperationException(Res.CastArrayNotAnUnmanagedType(typeof(TFrom)));
            else if (typeof(TTo).IsManaged())
                Throw.InvalidOperationException(Res.CastArrayNotAnUnmanagedType(typeof(TTo)));
        }

        #endregion

        #region Instance Constructors

        #region Public Constructors

        [SecuritySafeCritical]
        public unsafe CastArray(ArraySection<TFrom> buffer)
        {
            this.buffer = buffer;

            // Not "caching" the sizes into variables (or static fields) so in Release build the JIT compiler can eliminate the false branches.
            // Same size: the simplest case
            if (sizeof(TFrom) == sizeof(TTo))
            {
                length = buffer.Length;
                return;
            }

            // byte-sized source
            if (sizeof(TFrom) == 1)
            {
                length = buffer.Length / sizeof(TTo);
                return;
            }

            // Any size: it can happen that length in TOut is larger than Int32.MaxValue.
            // Though we could still support it by a long length and via the direct indexer only, Memory/Span conversions could not work so not allowing it.
            try
            {
                length = checked((int)((long)buffer.Length * sizeof(TFrom) / sizeof(TTo)));
            }
            catch (OverflowException e)
            {
                Throw.ArgumentException(Argument.buffer, Res.CastArrayBufferTooBigForCastLength(typeof(TTo)), e);
            }
        }

        #endregion

        #region Private Constructors

        private CastArray(ArraySection<TFrom> buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
        }

        #endregion

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        public CastArray<TFrom, TTo> Slice(int startIndex) => Slice(startIndex, length - startIndex);

        [SecuritySafeCritical]
        public unsafe CastArray<TFrom, TTo> Slice(int startIndex, int length)
        {
            // After this validation there is no need for overflow check like in the constructor because the new length can only be smaller than the original one.
            if ((uint)startIndex > (uint)this.length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)length > (uint)(this.length - startIndex))
                Throw.ArgumentOutOfRangeException(Argument.length);

            // Not "caching" the sizes into variables (or static fields) so in Release build the JIT compiler can eliminate the false branches.
            // Same size: the simplest case
            if (sizeof(TFrom) == sizeof(TTo))
                return new CastArray<TFrom, TTo>(buffer.Slice(startIndex, length), length);

            // 1 byte from size: any offset will work
            if (sizeof(TFrom) == 1)
                return new CastArray<TFrom, TTo>(buffer.Slice(startIndex * sizeof(TTo), buffer.Length - length * sizeof(TTo)), length);

            // Here we need to validate the alignment
            long byteOffset = sizeof(TTo) * startIndex;
            if (byteOffset % sizeof(TFrom) != 0)
                Throw.ArgumentException(Argument.startIndex, Res.CastArraySliceWrongStartIndex(startIndex), typeof(TFrom), typeof(TTo));
            return new CastArray<TFrom, TTo>(buffer.Slice((int)(byteOffset / sizeof(TFrom)), (int)((long)length * sizeof(TTo) / sizeof(TFrom))), length);
        }

        /// <summary>
        /// Gets this <see cref="CastArray{TFrom,TTo}"/> as an <see cref="CastArray2D{TFrom,TTo}"/> instance
        /// using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// The <see cref="CastArray2D{TFrom,TTo}"/> must have enough capacity for the specified dimensions.
        /// </summary>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance using this <see cref="CastArray{TFrom,TTo}"/> as its underlying buffer that has the specified dimensions.</returns>
        public CastArray2D<TFrom, TTo> As2D(int height, int width) => new CastArray2D<TFrom, TTo>(this, height, width);

        /// <summary>
        /// Gets this <see cref="CastArray{TFrom,TTo}"/> as an <see cref="CastArray2D{TFrom,TTo}"/> instance
        /// using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// The <see cref="CastArray2D{TFrom,TTo}"/> must have enough capacity for the specified dimensions.
        /// </summary>
        /// <param name="depth">The depth of the array to be returned.</param>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance using this <see cref="CastArray{TFrom,TTo}"/> as its underlying buffer that has the specified dimensions.</returns>
        public CastArray3D<TFrom, TTo> As3D(int depth, int height, int width) => new CastArray3D<TFrom, TTo>(this, depth, height, width);

        public CastArray<TFrom, T> Cast<T>()
            where T : unmanaged
        {
            return buffer.Cast<TFrom, T>();
        }

        public CastArray2D<TFrom, T> Cast2D<T>(int height, int width)
            where T : unmanaged
        {
            return buffer.Cast2D<TFrom, T>(height, width);
        }

        public CastArray3D<TFrom, T> Cast3D<T>(int depth, int height, int width)
            where T : unmanaged
        {
            return buffer.Cast3D<TFrom, T>(depth, height, width);
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public ref TTo UnsafeGetRef(int index)
        {
#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.Add(ref Unsafe.As<TFrom, TTo>(ref buffer.GetElementReferenceInternal(0)), index);
#else
            unsafe
            {
                fixed (TFrom* pBuf = &buffer.GetElementReferenceInternal(0))
                    return ref ((TTo*)pBuf)[index];
            }
#endif
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public ref TTo GetPinnableReference()
        {
            if (buffer.IsNullOrEmpty)
                Throw.InvalidOperationException(Res.ArraySectionEmpty);

#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.As<TFrom, TTo>(ref buffer.GetElementReferenceInternal(0));
#else
            unsafe
            {
                fixed (TFrom* pBuf = &buffer.GetElementReferenceInternal(0))
                    return ref *((TTo*)pBuf);
            }
#endif
        }

        public bool Equals(CastArray<TFrom, TTo> other) => buffer == other.buffer && length == other.length;

        public override bool Equals(object? obj) => obj is CastArray<TFrom, TTo> other && Equals(other);

        public override int GetHashCode()
        {
            if (buffer.IsNull)
                return 0;
            return (buffer, length).GetHashCode();
        }

        #endregion

        #region Private Methods

        #endregion

        #endregion
    }
}
