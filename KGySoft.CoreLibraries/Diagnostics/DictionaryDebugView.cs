#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DictionaryDebugView.cs
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
using System.Diagnostics.CodeAnalysis;

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides a debug view applicable for <see cref="DebuggerTypeProxyAttribute"/>
    /// for <see cref="IDictionary{TKey,TValue}"/> types.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">Type of the values in the dictionary.</typeparam>
    public sealed class DictionaryDebugView<TKey, TValue>
    {
        #region Fields

        private readonly IDictionary<TKey, TValue> dict;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the visible items in the debugger view
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Must be an array but it is not a problem as always a new array is created.")]
        public KeyValuePair<TKey, TValue>[] Items
        {
            get
            {
                KeyValuePair<TKey, TValue>[] items = new KeyValuePair<TKey, TValue>[dict.Count];
                dict.CopyTo(items, 0);
                return items;
            }
        }

        #endregion

        #region Constructors

        ///<summary>
        /// Creates a new instance of <see cref="DictionaryDebugView{TKey,TValue}"/> class.
        ///</summary>
        /// <param name="dictionary">The dictionary to provide the view for.</param>
        public DictionaryDebugView(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            dict = dictionary;
        }

        #endregion
    }
}
