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

        #endregion
    }
}
