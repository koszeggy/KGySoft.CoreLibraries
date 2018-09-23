using System.Collections.Generic;
using System.Diagnostics;

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides a debug view applicable for <see cref="DebuggerTypeProxyAttribute"/>
    /// for <see cref="IDictionary{TKey,TValue}"/> types.
    /// </summary>
    public sealed class DictionaryDebugView<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dict;

        ///<summary>
        /// Creates a new instance of <see cref="DictionaryDebugView{TKey,TValue}"/> class.
        ///</summary>
        public DictionaryDebugView(IDictionary<TKey, TValue> dictionary)
        {
            dict = dictionary;
        }

        /// <summary>
        /// Gets the visible items in the debugger view
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items
        {
            get
            {
                KeyValuePair<TKey, TValue>[] items = new KeyValuePair<TKey, TValue>[dict.Count];
                dict.CopyTo(items, 0);
                return items;
            }
        }
    }
}
