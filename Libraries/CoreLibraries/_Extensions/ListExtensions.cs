#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ListExtensions.cs
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
    /// Contains extension methods for the <see cref="IList{T}"/> type.
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

        #endregion
    }
}
