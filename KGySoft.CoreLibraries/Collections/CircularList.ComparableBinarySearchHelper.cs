#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CircularList.ComparableBinarySearchHelper.cs
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
    /// Helper class for performing binary search on a <see cref="CircularList{T}"/>. This class can handles elements as <see cref="IComparable{T}"/> instances.
    /// This class accesses the <see cref="CircularList{T}"/> through its indexer, so it is slower than Array.BinarySearch, so used only when section to search is wrapped.
    /// Not a nested private class because must have its own generic parameter due to the <see cref="IComparable{T}"/> constraint.
    /// </summary>
    /// <typeparam name="TComparable">Represents a <see cref="IComparable{T}"/> type.</typeparam>
    internal class ComparableBinarySearchHelper<TComparable> : BinarySearchHelper<TComparable> where TComparable : IComparable<TComparable>
    {
        #region Methods

        #region Static Methods

        /// <summary>
        /// Performs a binary search on the list. Elements are constrained to be <see cref="IComparable{T}"/> instances.
        /// </summary>
        private static int BinarySearchAsComparable(CircularList<TComparable> list, int index, int length, TComparable value)
        {
            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                TComparable item = list.ElementAt(i);

                int order;
                if (item == null!)
                {
                    if (value == null!)
                        order = 0;
                    else
                        order = -1;
                }
                else
                    order = item.CompareTo(value!);

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
        /// Performs a binary search on the list. When comparer is specified, it is used, otherwise, using <see cref="IComparable{T}.CompareTo"/> on elements.
        /// </summary>
        internal override int BinarySearch(CircularList<TComparable> list, int index, int length, TComparable value, IComparer<TComparable> comparer)
        {
            try
            {
                return ReferenceEquals(comparer, Comparer<TComparable>.Default)
                    ? BinarySearchAsComparable(list, index, length, value)
                    : BinarySearchWithComparer(list, index, length, value, comparer);
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
