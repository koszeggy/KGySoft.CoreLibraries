#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ItemGenericComparer.cs
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

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Helper class for providing sort logic for the <see cref="SortableBindingList{T}"/> class.
    /// Not a nested private class because it has a different type parameter than the parent class, whose type parameter is irrelevant here.
    /// </summary>
    internal sealed class ItemGenericComparer<TElement> : IComparer<(int Index, object? Value)>
    {
        #region Fields

        private readonly bool ascending;

        #endregion

        #region Constructors

        public ItemGenericComparer(bool ascending) => this.ascending = ascending;

        #endregion

        #region Methods

        public int Compare((int Index, object? Value) x, (int Index, object? Value) y)
        {
            int sign = ascending ? 1 : -1;

            if (x.Value == null)
                return y.Value == null ? 0 : -sign;
            if (y.Value == null)
                return sign;
            return sign * ((IComparable<TElement>)x.Value).CompareTo((TElement)y.Value);
        }

        #endregion
    }
}
