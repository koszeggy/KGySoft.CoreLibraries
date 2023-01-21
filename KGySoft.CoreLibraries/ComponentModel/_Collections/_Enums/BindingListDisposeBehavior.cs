#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BindingListDisposeBehavior.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents disposing strategy for the wrapped collection and elements when a <see cref="FastBindingList{T}"/> instance is disposed.
    /// </summary>
    public enum BindingListDisposeBehavior
    {
        /// <summary>
        /// Indicates that the wrapped collection should be disposed if it implements the <see cref="IDisposable"/> interface.
        /// </summary>
        DisposeCollection,

        /// <summary>
        /// Indicates that the wrapped collection and the items should be disposed if they implement the <see cref="IDisposable"/> interface.
        /// Elements are never disposed when they are overwritten or removed, only when the parent <see cref="FastBindingList{T}"/> is disposed.
        /// </summary>
        DisposeCollectionAndItems,

        /// <summary>
        /// Indicates that neither the wrapped collection nor the items should be disposed when the parent <see cref="FastBindingList{T}"/> is disposed,
        /// in which case only the event subscriptions are removed.
        /// </summary>
        KeepAlive,
    }
}