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
    /// TODO: Slice may throw ArgumentException
    /// </remarks>
    public struct CastArray<TFrom, TTo> : IDisposable, /*IList<TTo>, IList,*/ IEquatable<CastArray<TFrom, TTo>> // TODO
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
        // TODO: ArraySection.Cast
        // TODO: AsArray2D/3D
        // TODO: ToArray
        // TODO: Extensions (array[section] conversions, sort)

        #region Fields

        #region Static Fields

        [SecuritySafeCritical]
        private unsafe static readonly int sizeFrom = sizeof(TFrom);
        [SecuritySafeCritical]
        private unsafe static readonly int sizeTo = sizeof(TTo);

        #endregion

        #region Instance Fields

        private readonly int length;
        private ArraySection<TFrom> buffer;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        public readonly ArraySection<TFrom> Buffer => buffer;

        public readonly int Length => length;

        public readonly bool IsNull => buffer.IsNull;

        public readonly bool IsNullOrEmpty => buffer.IsNullOrEmpty;

        #endregion

        #region Indexers

        public readonly ref TTo this[int index]
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

        public CastArray(int length, bool assureClean = true)
        {
            int bufLen;
            if (sizeFrom == sizeTo)
                bufLen = sizeFrom;
            else if (sizeTo == 1)
                bufLen = length / ((sizeFrom + (sizeFrom - 1)) / sizeFrom);
            else
            {
                try
                {
                    bufLen = checked((int)((long)sizeTo * length / ((sizeFrom + (sizeFrom - 1)) / sizeFrom)));
                }
                catch (OverflowException e)
                {
                    Throw.ArgumentException(Res.CastArrayLengthTooBigForBufferLength(length, typeof(TFrom), typeof(TTo)), e);
                    return;
                }
            }

            buffer = new ArraySection<TFrom>(bufLen, assureClean);
            this.length = length;
        }

        public CastArray(ArraySection<TFrom> buffer)
        {
            int lenFrom = buffer.Length;
            this.buffer = buffer.Slice(0, buffer.Length);

            if (sizeFrom == sizeTo)
            {
                length = lenFrom;
                return;
            }

            // TFrom is 1 byte wide
            if (sizeFrom == 1)
            {
                length = lenFrom / sizeTo;
                return;
            }

            // Any size: it can happen that length in TOut is larger than Int32.MaxValue.
            // Though we could still support it by a long length and via the direct indexer only, Memory/Span conversions could not work so not allowing it.
            try
            {
                length = checked((int)((long)lenFrom * sizeFrom / sizeTo));
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

        public void Dispose()
        {
            buffer.Release();
            this = default;
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public readonly ref TTo GetPinnableReference()
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

        public readonly CastArray<TFrom, TTo> Slice(int startIndex) => Slice(startIndex, length - startIndex);

        public readonly CastArray<TFrom, TTo> Slice(int startIndex, int length)
        {
            // After this validation there is no need for overflow check like in the constructor because the new length can only be smaller than the original one.
            if ((uint)startIndex > (uint)this.length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)length > (uint)(this.length - startIndex))
                Throw.ArgumentOutOfRangeException(Argument.length);

            // same size: simplest case
            if (sizeFrom == sizeTo)
                return new CastArray<TFrom, TTo>(buffer.Slice(startIndex, length), length);

            // 1 byte from size: any offset will work
            if (sizeFrom == 1)
                return new CastArray<TFrom, TTo>(buffer.Slice(startIndex * sizeTo, buffer.Length - length * sizeTo), length);

            // here we need to validate the alignment
            long byteOffset = sizeTo * startIndex;
            if (byteOffset % sizeFrom != 0)
                Throw.ArgumentException(Argument.startIndex, Res.CastArraySliceWrongStartIndex(startIndex));
            return new CastArray<TFrom, TTo>(buffer.Slice((int)(byteOffset / sizeFrom), (int)(buffer.Length - ((long)length * sizeTo / sizeFrom))), length);
        }

        public readonly bool Equals(CastArray<TFrom, TTo> other) => buffer == other.buffer && length == other.length;

        public readonly override bool Equals(object? obj) => obj is CastArray<TFrom, TTo> other && Equals(other);

        public readonly override int GetHashCode()
        {
            if (buffer.IsNull)
                return 0;
            return (buffer, length).GetHashCode();
        }

        #endregion

        #region Private Methods

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private readonly ref TTo UnsafeGetRef(int index)
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
