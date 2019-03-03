#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ListSegment.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.Collections;
using System.Collections.Generic;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Wraps a segment of an <see cref="IList{T}"/> for read-only purposes.
    /// </summary>
    internal sealed class ListSegment<T> : IList<T>
#if !(NET35 || NET40)
        , IReadOnlyList<T>
#endif
    {
        #region Fields

        private readonly IList<T> list;
        private readonly int offset;

        #endregion

        #region Properties and Indexers

        #region Properties

        public bool IsReadOnly => true;
        public int Count => list.Count - offset;

        #endregion

        #region Indexers

        public T this[int index]
        {
            get => list[index + offset];
            set => throw new NotSupportedException(Res.NotSupported);
        }

        #endregion

        #endregion

        #region Constructors

        internal ListSegment(IList<T> list, int offset)
        {
            this.list = list;
            this.offset = offset;
        }

        #endregion

        #region Methods

        #region Public Methods

        public IEnumerator<T> GetEnumerator()
        {
            int len = Count;
            for (int i = 0; i < len; i++)
                yield return this[i];
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Not checking bounds as we always create ListSegment for ourselves.
            int len = Count;
            for (int i = 0; i < len; i++)
                array[arrayIndex + i] = list[offset + i];
        }

        public bool Contains(T item) => IndexOf(item) >= 0;

        public int IndexOf(T item)
        {
            int len = Count;
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < len; i++)
            {
                if (comparer.Equals(item, this[i]))
                    return i;
            }

            return -1;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<T>.Add(T item) => throw new NotSupportedException(Res.NotSupported);
        void ICollection<T>.Clear() => throw new NotSupportedException(Res.NotSupported);
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException(Res.NotSupported);
        void IList<T>.Insert(int index, T item) => throw new NotSupportedException(Res.NotSupported);
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException(Res.NotSupported);

        #endregion

        #endregion
    }
}
