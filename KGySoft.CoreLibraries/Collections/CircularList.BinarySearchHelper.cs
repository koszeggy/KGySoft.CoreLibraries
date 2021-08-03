#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CircularList.BinarySearchHelper.cs
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
    /// <summary>
    /// Base class for performing binary search on a <see cref="CircularList{T}"/>. This class uses a comparer for the search.
    /// This class accesses the <see cref="CircularList{T}"/> through its indexer, so it is slower than Array.BinarySearch, so used only when section to search is wrapped.
    /// Not a nested private class because must have its own generic parameter due to the derived <see cref="ComparableBinarySearchHelper{TComparable}"/>.
    /// </summary>
    /// <typeparam name="T">Same as T in <see cref="CircularList{T}"/>.</typeparam>
    internal class BinarySearchHelper<T>
    {
        #region Methods

        #region Static Methods

        /// <summary>
        /// Performs a binary search on the list using the comparer.
        /// </summary>
        private protected static int BinarySearchWithComparer(CircularList<T> list, int index, int length, T value, IComparer<T> comparer)
        {
            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(list.ElementAt(i), value);

                if (order == 0)
                    return i;

                if (order < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            return ~lo;
        }

        #endregion

        #region Instance Methods

        /// <summary>
        /// Performs a binary search on the list using the comparer or the default comparer, when comparer is null.
        /// </summary>
        internal virtual int BinarySearch(CircularList<T> list, int index, int length, T value, IComparer<T> comparer)
        {
            try
            {
                return BinarySearchWithComparer(list, index, length, value, comparer);
            }
#pragma warning disable CA1031 // false alarm, exception is re-thrown
            catch (Exception e)
#pragma warning restore CA1031 // false alarm, exception is re-thrown
            {
                return Throw.InvalidOperationException<int>(Res.CircularListComparerFail, e);
            }
        }

        #endregion

        #endregion
    }
}
