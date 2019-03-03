#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CollectionExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.Collections.Generic;
using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="ICollection{T}"/> type.
    /// </summary>
    public static class CollectionExtensions
    {
        #region Methods

        /// <summary>
        /// Returns a <see cref="LockingCollection{T}"/>, which provides a thread-safe wrapper for the specified <paramref name="collection"/>.
        /// This only means that if the members are accessed through the returned <see cref="LockingCollection{T}"/>, then the inner state of the wrapped collection remains always consistent and not that all of the multi-threading concerns can be ignored.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="LockingCollection{T}"/> class for details and some examples.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="collection">The collection to create a thread-safe wrapper for.</param>
        /// <returns>A <see cref="LockingCollection{T}"/>, which provides a thread-safe wrapper for the specified <paramref name="collection"/>.</returns>
        public static LockingCollection<T> AsThreadSafe<T>(this ICollection<T> collection) => new LockingCollection<T>(collection);

        /// <summary>
        /// Adds a <paramref name="collection"/> to the <paramref name="target"/>&#160;<see cref="ICollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collections.</typeparam>
        /// <param name="target">The target collection.</param>
        /// <param name="collection">The collection to add to the <paramref name="target"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> or <paramref name="collection"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <note>If <paramref name="target"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeColletion{T}"/> implementation,
        /// then the elements of <paramref name="collection"/> will be added one by one.</note>
        /// </remarks>
        public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> collection)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), Res.ArgumentNull);
            if (collection == null)
                throw new ArgumentNullException(nameof(collection), Res.ArgumentNull);

            switch (target)
            {
                case ISupportsRangeColletion<T> supportsRangeColletion:
                    supportsRangeColletion.AddRange(collection);
                    return;
                case List<T> list:
                    list.AddRange(collection);
                    return;
                default:
                    collection.ForEach(target.Add);
                    return;
            }
        }

        #endregion
    }
}
