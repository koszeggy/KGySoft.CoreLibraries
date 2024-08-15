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

#endregion

namespace KGySoft.Collections
{
    public readonly struct CastArray3D<TFrom, TTo>
#if NETFRAMEWORK // To make the type compatible with older compilers. Unmanaged is asserted in the wrapped CastArray<TFrom, TTo>.
        where TFrom : struct
        where TTo : struct
#else
        where TFrom : unmanaged
        where TTo : unmanaged
#endif
    {
        #region Fields

        private readonly CastArray<TFrom, TTo> buffer;
        private readonly int depth;
        private readonly int height;
        private readonly int width;

        [NonSerialized]
        private readonly int planeSize; // cached value of height * width

        #endregion

        #region Properties

        public CastArray<TFrom, TTo> Buffer => buffer;
        // TODO: Span, Memory
        // TODO: Indexer

        #endregion

        #region Constructors

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

        public CastArray3D(ArraySection<TFrom> buffer, int depth, int height, int width)
            : this(buffer.Cast<TFrom, TTo>(), depth, height, width)
        {
        }

        #endregion
    }
}