using System.Collections.Generic;
using System.Diagnostics;

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides a debug view applicable for <see cref="DebuggerTypeProxyAttribute"/>
    /// for <see cref="ICollection{T}"/> types.
    /// </summary>
    public sealed class CollectionDebugView<T>
    {
        private readonly ICollection<T> collection;

        ///<summary>
        /// Creates a new instance of CollectionDebugView
        ///</summary>
        public CollectionDebugView(ICollection<T> collection)
        {
            this.collection = collection;
        }

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
    }
}