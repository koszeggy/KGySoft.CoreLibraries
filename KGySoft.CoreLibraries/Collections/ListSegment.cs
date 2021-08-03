#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ListSegment.cs
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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using KGySoft.Diagnostics;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Wraps a segment of an <see cref="IList{T}"/> for read-only purposes.
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; T = {typeof(" + nameof(T) + ").Name}")]
    internal sealed class ListSegment<T> : IList<T>
    {
        #region Fields

        private readonly IList<T> list;
        private readonly int offset;
        private readonly int? count;

        #endregion

        #region Properties and Indexers

        #region Properties

        public bool IsReadOnly => true;
        public int Count => count ?? (list.Count - offset);

        #endregion

        #region Indexers

        // ReSharper disable once ValueParameterNotUsed - false alarm: throw
        public T this[int index]
        {
            get => list[index + offset];
            set => Throw.NotSupportedException(Res.NotSupported);
        }

        #endregion

        #endregion

        #region Constructors

        internal ListSegment(IList<T> list, int offset, int? count = null)
        {
            this.list = list;
            this.offset = offset;
            this.count = count;
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
        void ICollection<T>.Add(T item) => Throw.NotSupportedException(Res.NotSupported);
        void ICollection<T>.Clear() => Throw.NotSupportedException(Res.NotSupported);
        bool ICollection<T>.Remove(T item) => Throw.NotSupportedException<bool>(Res.NotSupported);
        void IList<T>.Insert(int index, T item) => Throw.NotSupportedException(Res.NotSupported);
        void IList<T>.RemoveAt(int index) => Throw.NotSupportedException(Res.NotSupported);

        #endregion

        #endregion
    }
}
