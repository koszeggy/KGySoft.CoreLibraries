using System.Collections.Generic;
using System.Diagnostics;

namespace KGySoft.Diagnostics
{
    internal sealed class DictionaryValueCollectionDebugView<TKey, TValue>
    {
        private readonly ICollection<TValue> collection;

        ///<summary>
        /// Creates a new instance of CollectionDebugView
        ///</summary>
        public DictionaryValueCollectionDebugView(ICollection<TValue> collection)
        {
            this.collection = collection;
        }

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
    }
}
