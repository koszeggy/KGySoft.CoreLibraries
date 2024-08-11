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
using System.Security;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a one dimensional array (or a section of it) of element type <typeparamref name="TFrom"/> cast to another array of element type <typeparamref name="TTo"/>.
    /// Can be helpful on targets where <see cref="Span{T}"/> is not available or wherever you must use arrays that you want to reinterpret. For example, you want to retrieve
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
        // TODO: Span/Memory
        // TODO: Cast
        // TODO: Operators
        // TODO: Collection interfaces
        // TODO: ToArray

        #region Fields

        private readonly ArraySection<TFrom> buffer;
        private readonly int length;

        #endregion

        #region Properties and Indexers

        #region Properties

        public ArraySection<TFrom> Buffer => buffer;

        public int Length => length;

        public bool IsNull => buffer.IsNull;

        public bool IsNullOrEmpty => buffer.IsNullOrEmpty;

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
                Throw.ArgumentException(Argument.startIndex, Res.CastArraySliceWrongStartIndex(startIndex));
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

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private ref TTo UnsafeGetRef(int index)
        {
#if !NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.Add(ref Unsafe.As<TFrom, TTo>(ref buffer.GetElementReferenceInternal(0)), index);
#else
            unsafe
            {
                fixed (TFrom* pBuf = &buffer.GetElementReferenceInternal(0))
                    return ref ((TTo*)pBuf)[index];
            }
#endif

        }

        #endregion

        #endregion
    }
}
