#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CollectionDebugView.cs
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

using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides a debug view applicable for <see cref="DebuggerTypeProxyAttribute"/> for <see cref="ICollection{T}"/> types.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public sealed class CollectionDebugView<T>
    {
        #region Fields

        private readonly ICollection<T> collection;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the visible items in debugger view
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] items = new T[collection.Count];
                collection.CopyTo(items, 0);
                return items;
            }
        }

        #endregion

        #region Constructors

        ///<summary>
        /// Creates a new instance of CollectionDebugView
        ///</summary>
        /// <param name="collection">The collection to provide the view for.</param>
        public CollectionDebugView(ICollection<T> collection) => this.collection = collection;

        #endregion
    }
}
