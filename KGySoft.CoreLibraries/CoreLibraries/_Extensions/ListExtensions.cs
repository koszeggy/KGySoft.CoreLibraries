#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ListExtensions.cs
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

using System;
using System.Collections.Generic;

using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="IList{T}"/> type.
    /// </summary>
    public static class ListExtensions
    {
        #region Methods

        /// <summary>
        /// Returns a <see cref="LockingList{T}"/>, which provides a thread-safe wrapper for the specified <paramref name="list"/>.
        /// This only means that if the members are accessed through the returned <see cref="LockingList{T}"/>, then the inner state of the wrapped list remains always consistent and not that all of the multi-threading concerns can be ignored.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="LockingList{T}"/> class for details and some examples.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="list">The list to create a thread-safe wrapper for.</param>
        /// <returns>A <see cref="LockingCollection{T}"/>, which provides a thread-safe wrapper for the specified <paramref name="list"/>.</returns>
        public static LockingList<T> AsThreadSafe<T>(this IList<T> list) => new LockingList<T>(list);

        /// <summary>
        /// Inserts a <paramref name="collection"/> into the <paramref name="target"/>&#160;<see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collections.</typeparam>
        /// <param name="target">The target collection.</param>
        /// <param name="index">The zero-based index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="collection">The collection to insert into the <paramref name="target"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> or <paramref name="collection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.</exception>
        /// <remarks>
        /// <note>If <paramref name="target"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeList{T}"/> implementation,
        /// then the elements of <paramref name="collection"/> will be inserted one by one.</note>
        /// </remarks>
        public static void InsertRange<T>(this IList<T> target, int index, IEnumerable<T> collection)
        {
            if (target == null!)
                Throw.ArgumentNullException(Argument.target);
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);

            switch (target)
            {
                case ISupportsRangeList<T> supportsRangeList:
                    supportsRangeList.InsertRange(index, collection);
                    return;
                case List<T> list:
                    list.InsertRange(index, collection);
                    return;
                default:
                    collection.ForEach(item => target.Insert(index++, item));
                    return;
            }
        }

        /// <summary>
        /// Removes <paramref name="count"/> amount of items from the specified <paramref name="collection"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The list to remove the elements from.</param>
        /// <param name="index">The zero-based index of the first item to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.
        /// <br/>-or-
        /// <br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the list.</exception>
        /// <remarks>
        /// <note>If <paramref name="collection"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeList{T}"/> implementation,
        /// then the elements will be removed one by one.</note>
        /// </remarks>
        public static void RemoveRange<T>(this IList<T> collection, int index, int count)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);

            switch (collection)
            {
                case ISupportsRangeList<T> supportsRangeList:
                    supportsRangeList.RemoveRange(index, count);
                    return;
                case List<T> list:
                    list.RemoveRange(index, count);
                    return;
                default:
                    int len = collection.Count;
                    if ((uint)index >= (uint)len)
                        Throw.ArgumentOutOfRangeException(Argument.index);
                    if (count < 0)
                        Throw.ArgumentOutOfRangeException(Argument.count);
                    if (index + count > len)
                        Throw.ArgumentException(Res.IListInvalidOffsLen);
                    for (int i = 0; i < count; i++)
                        collection.RemoveAt(index);
                    return;
            }
        }

        /// <summary>
        /// Removes <paramref name="count"/> amount of items from the <paramref name="target"/>&#160;<see cref="IList{T}"/> at the specified <paramref name="index"/>, and
        /// inserts the specified <paramref name="collection"/> at the same position. The number of elements in <paramref name="collection"/> can be different from the amount of removed items.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collections.</typeparam>
        /// <param name="target">The target collection.</param>
        /// <param name="index">The zero-based index of the first item to remove and also the index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <param name="collection">The collection to insert into the <paramref name="target"/> list.</param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> or <paramref name="collection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.</exception>
        /// <remarks>
        /// <note>If <paramref name="target"/> is neither a <see cref="List{T}"/> or an <see cref="ISupportsRangeList{T}"/> implementation,
        /// then after overwriting the elements of the overlapping range, the difference will be removed or inserted one by one.</note>
        /// </remarks>
        public static void ReplaceRange<T>(this IList<T> target, int index, int count, IEnumerable<T> collection)
        {
            if (target == null!)
                Throw.ArgumentNullException(Argument.target);
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);

            switch (target)
            {
                case ISupportsRangeList<T> supportsRangeList:
                    supportsRangeList.ReplaceRange(index, count, collection);
                    return;
                default:
                    int len = target.Count;
                    if ((uint)index >= (uint)len)
                        Throw.ArgumentOutOfRangeException(Argument.index);
                    if (count < 0)
                        Throw.ArgumentOutOfRangeException(Argument.count);
                    if (index + count > len)
                        Throw.ArgumentException(Res.IListInvalidOffsLen);

                    using (IEnumerator<T> enumerator = collection.GetEnumerator())
                    {
                        // Copying elements while possible
                        int elementsCopied = 0;
                        while (count > 0 && enumerator.MoveNext())
                        {
                            target[index + elementsCopied] = enumerator.Current;
                            elementsCopied += 1;
                            count -= 1;
                        }

                        // all inserted, removing the rest
                        if (count > 0)
                        {
                            target.RemoveRange(index + elementsCopied, count);
                            return;
                        }

                        // all removed (overwritten), inserting the rest
                        IList<T> rest = collection is IList<T> list ? new ListSegment<T>(list, elementsCopied) : enumerator.RestToList();
                        if (rest.Count > 0)
                            target.InsertRange(index + elementsCopied, rest);

                        return;
                    }

            }
        }

        #endregion
    }
}
