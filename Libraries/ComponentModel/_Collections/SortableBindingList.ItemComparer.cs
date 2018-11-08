using System;
using System.Collections.Generic;

namespace KGySoft.ComponentModel
{
    using SortIndex = KeyValuePair<int, object>;

    /// <summary>
    /// Helper class for providing sort logic for the <see cref="SortableBindingList{T}"/> class.
    /// Not a nested private class because the code is identical for all types of the enclosing class.
    /// </summary>
    internal sealed class ItemComparer : IComparer<SortIndex>
    {
        private readonly bool ascending;

        public ItemComparer(bool ascending) => this.ascending = ascending;

        public int Compare(SortIndex x, SortIndex y)
        {
            int sign = ascending ? 1 : -1;

            if (x.Value == null)
                return y.Value == null ? 0 : sign;
            if (x.Value.Equals(y.Value))
                return 0;

            if (x.Value is IComparable comparable)
                return sign * comparable.CompareTo(y.Value);

            // ReSharper disable once StringCompareToIsCultureSpecific - now this is intended
            return sign * x.Value.ToString().CompareTo(y.Value?.ToString());
        }
    }
}
