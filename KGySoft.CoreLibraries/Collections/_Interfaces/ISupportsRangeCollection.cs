#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ISupportsRangeCollection.cs
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

#endregion

namespace KGySoft.Collections
{
#if NET35 || NET40
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

    /// <summary>
    /// Represents a collection that supports the <see cref="AddRange">AddRange</see> method.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <seealso cref="ICollection{T}" />
    /// <seealso cref="IReadOnlyCollection{T}" />
    /// <seealso cref="CircularList{T}" />
    public interface ISupportsRangeCollection<T> : ICollection<T>
#if !(NET35 || NET40)
        , IReadOnlyCollection<T>
#endif

    {
        #region Methods

        /// <summary>
        /// Adds a <paramref name="collection"/> to this <see cref="ISupportsRangeCollection{T}"/>.
        /// </summary>
        /// <param name="collection">The collection to add to the <see cref="ISupportsRangeCollection{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        void AddRange(IEnumerable<T> collection);

        #endregion
    }
}