using System;
using System.Collections.Generic;

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a collection that supports the <see cref="AddRange">AddRange</see> method.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <seealso cref="ICollection{T}" />
    /// <seealso cref="IReadOnlyCollection{T}" />
    /// <seealso cref="CircularList{T}" />
    public interface ISupportsRangeColletion<T> : ICollection<T>
#if !(NET35 || NET40)
        , IReadOnlyCollection<T>
#endif
    {
        /// <summary>
        /// Adds a <paramref name="collection"/> to this <see cref="ISupportsRangeColletion{T}"/>.
        /// </summary>
        /// <param name="collection">The collection to add to the <see cref="ISupportsRangeColletion{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        void AddRange(IEnumerable<T> collection);
    }
}