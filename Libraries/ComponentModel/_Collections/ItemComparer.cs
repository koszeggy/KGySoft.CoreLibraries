#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ItemComparer.cs
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

using System;
using System.Collections.Generic;

#endregion

namespace KGySoft.ComponentModel
{
    #region Usings

    using SortIndex = KeyValuePair<int, object>;

    #endregion

    /// <summary>
    /// Helper class for providing sort logic for the <see cref="SortableBindingList{T}"/> class.
    /// Not a nested private class because the code is identical for all types of the enclosing class.
    /// </summary>
    internal sealed class ItemComparer : IComparer<SortIndex>
    {
        #region Fields

        private readonly bool ascending;

        #endregion

        #region Constructors

        public ItemComparer(bool ascending) => this.ascending = ascending;

        #endregion

        #region Methods

        public int Compare(SortIndex x, SortIndex y)
        {
            int sign = ascending ? 1 : -1;

            if (x.Value == null)
                return y.Value == null ? 0 : -sign;
            if (x.Value.Equals(y.Value))
                return 0;

            if (x.Value is IComparable comparable)
                return sign * comparable.CompareTo(y.Value);

            // ReSharper disable once StringCompareToIsCultureSpecific - now this is intended
            return sign * x.Value.ToString().CompareTo(y.Value?.ToString());
        }

        #endregion
    }
}
