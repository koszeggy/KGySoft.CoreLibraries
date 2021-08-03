#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DictionaryValueCollectionDebugView.cs
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

using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace KGySoft.Diagnostics
{
    // ReSharper disable once UnusedTypeParameter - otherwise, DebuggerTypeProxyAttribute does not work
    internal sealed class DictionaryValueCollectionDebugView<TKey, TValue>
    {
        #region Fields

        private readonly ICollection<TValue> collection;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the visible items in debugger view
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TValue[] Items
        {
            get
            {
                TValue[] items = new TValue[collection.Count];
                collection.CopyTo(items, 0);
                return items;
            }
        }

        #endregion

        #region Constructors

        ///<summary>
        /// Creates a new instance of CollectionDebugView
        ///</summary>
        public DictionaryValueCollectionDebugView(ICollection<TValue> collection)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);
            this.collection = collection;
        }

        #endregion
    }
}
