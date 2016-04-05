using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace KGySoft.Libraries.Diagnostics
{
    internal sealed class DictionaryKeyCollectionDebugView<TKey, TValue>
    {
        private readonly ICollection<TKey> collection;

        ///<summary>
        /// Creates a new instance of CollectionDebugView
        ///</summary>
        public DictionaryKeyCollectionDebugView(ICollection<TKey> collection)
        {
            this.collection = collection;
        }

        /// <summary>
        /// Gets the visible items in debugger view
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TKey[] Items
        {
            get
            {
                TKey[] items = new TKey[collection.Count];
                collection.CopyTo(items, 0);
                return items;
            }
        }
    }
}
