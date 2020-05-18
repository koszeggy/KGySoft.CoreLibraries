#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySection.cs
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
#if !(NETFRAMEWORK || NETSTANDARD2_0)
using System.Buffers;
#endif

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a section of a one dimensional array.
    /// This type is very similar to <see cref="ArraySegment{T}"/>/<see cref="Memory{T}"/> types but can be used on every platform in the same way
    /// and depending on the used platform supports <see cref="ArrayPool{T}"/> allocation.
    /// </summary>
    /// <typeparam name="T">The type of the element in the collection.</typeparam>
    // TODO: make public, when it is done
    internal struct ArraySection<T> : IDisposable// , IList...
    {
        #region Fields

        #region Static Fields

#if !(NETFRAMEWORK || NETSTANDARD2_0)
        private static readonly int poolingThreshold = Math.Min(2, 1024 / Reflector.SizeOf<T>());
#endif

        #endregion

        #region Instance Fields

        private readonly int offset;
        private readonly int length;
#if !(NETFRAMEWORK || NETSTANDARD2_0)
        private readonly bool poolArray;
#endif

        private T[] array;

        #endregion

        #endregion

        #region Indexers

        public ref T this[int index] => ref array[offset + index];

        #endregion

        #region Constructors

        public ArraySection(int length)
        {
            offset = 0;
            this.length = length;

#if !(NETFRAMEWORK || NETSTANDARD2_0)
            poolArray = length >= poolingThreshold;
            if (poolArray)
            {
                array = ArrayPool<T>.Shared.Rent(length);
                return;
            }
#endif

            array = new T[length];
        }

        public ArraySection(T[] array) : this(array, 0, array?.Length ?? 0)
        {
        }

        public ArraySection(T[] array, int offset, int length)
        {
            this.array = array;
            this.offset = offset;
            this.length = length;
#if !(NETFRAMEWORK || NETSTANDARD2_0)
            poolArray = false;
#endif
        }

        #endregion

        #region Methods

        #region Public Methods

        public ArraySection<T> Slice(int start) => new ArraySection<T>(array, offset + start, length - start);

        public ArraySection<T> Slice(int start, int length) => new ArraySection<T>(array, offset + start, length);

        public ref T GetPinnableReference() => ref this[0];

        public T[] ToArray()
        {
            if (length == 0)
                return Reflector.EmptyArray<T>();
            T[] result = new T[length];
            array.CopyElements(offset, result, 0, length);
            return result;
        }

        public void Dispose()
        {
#if !(NETFRAMEWORK || NETSTANDARD2_0)
            if (array != null && poolArray)
                ArrayPool<T>.Shared.Return(array);
#endif

            array = null;
        }

        #endregion

        #endregion
    }
}
