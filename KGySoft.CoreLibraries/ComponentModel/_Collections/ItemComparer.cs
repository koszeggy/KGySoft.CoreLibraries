#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ItemComparer.cs
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
using System.Diagnostics.CodeAnalysis;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Helper class for providing sort logic for the <see cref="SortableBindingList{T}"/> class.
    /// Not a nested private class because the code is identical for all types of the enclosing class.
    /// </summary>
    internal sealed class ItemComparer : IComparer<(int Index, object? Value)>
    {
        #region Fields

        private readonly bool ascending;

        #endregion

        #region Constructors

        public ItemComparer(bool ascending) => this.ascending = ascending;

        #endregion

        #region Methods

        [SuppressMessage("Globalization", "CA1309:Use ordinal string comparison",
            Justification = "Intended fallback logic because ToString can depend on current culture, too")]
        public int Compare((int Index, object? Value) x, (int Index, object? Value) y)
        {
            int sign = ascending ? 1 : -1;

            if (x.Value == null)
                return y.Value == null ? 0 : -sign;
            if (y.Value == null)
                return sign;
            if (x.Value.Equals(y.Value))
                return 0;

            if (x.Value is IComparable comparable)
                return sign * comparable.CompareTo(y.Value);

            return sign * String.Compare(x.Value.ToString(),y.Value.ToString(), StringComparison.CurrentCulture);
        }

        #endregion
    }
}
