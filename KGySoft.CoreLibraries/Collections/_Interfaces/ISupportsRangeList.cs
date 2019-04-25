using System;
using System.Collections.Generic;

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a list that supports range operations.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    /// <seealso cref="ISupportsRangeColletion{T}" />
    /// <seealso cref="IList{T}" />
    /// <seealso cref="IReadOnlyList{T}" />
    /// <seealso cref="CircularList{T}" />
    public interface ISupportsRangeList<T> : ISupportsRangeColletion<T>, IList<T>
#if !(NET35 || NET40)
        , IReadOnlyList<T>
#endif
    {
        /// <summary>
        /// Inserts a <paramref name="collection"/> into this <see cref="ISupportsRangeList{T}"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="collection">The collection to insert into the list.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        void InsertRange(int index, IEnumerable<T> collection);

        /// <summary>
        /// Removes <paramref name="count"/> amount of items from this <see cref="ISupportsRangeList{T}"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the first item to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.
        /// <br/>-or-
        /// <br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the list.</exception>
        void RemoveRange(int index, int count);

        /// <summary>
        /// Removes <paramref name="count"/> amount of items from this <see cref="ISupportsRangeList{T}"/> at the specified <paramref name="index"/>, and
        /// inserts the specified <paramref name="collection"/> at the same position. The number of elements in <paramref name="collection"/> can be different from the amount of removed items.
        /// </summary>
        /// <param name="index">The zero-based index of the first item to remove and also the index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <param name="collection">The collection to insert into the list.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>If the length of the <see cref="CircularList{T}"/> is n and the length of the collection to insert is m, then replacement at the first or last position has O(m) cost.</para>
        /// <para>If the elements to remove and to add have the same size, then the cost is O(m) at any position.</para>
        /// <para>If capacity increase is needed (considering actual list size), or when the replacement of different amount of elements to remove and insert is performed in the middle of the <see cref="CircularList{T}"/>, the cost is O(Max(n, m)), and in practice no more than n/2 elements are moved.</para>
        /// </remarks>
        void ReplaceRange(int index, int count, IEnumerable<T> collection);
    }
}
