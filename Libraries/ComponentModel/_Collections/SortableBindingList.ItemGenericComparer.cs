using System;
using System.Collections.Generic;

namespace KGySoft.ComponentModel
{
    using SortIndex = KeyValuePair<int, object>;

    /// <summary>
    /// Helper class for providing sort logic for the <see cref="SortableBindingList{T}"/> class.
    /// Not a nested private class because it has a different type parameter than the parent class, whose type parameter is irrelevant here.
    /// </summary>
    internal sealed class ItemGenericComparer<TElement> : IComparer<SortIndex>
    {
        private readonly bool ascending;

        public ItemGenericComparer(bool ascending) => this.ascending = ascending;

        public int Compare(SortIndex x, SortIndex y)
        {
            int sign = ascending ? 1 : -1;

            if (x.Value == null)
                return y.Value == null ? 0 : -sign;
            return sign * ((IComparable<TElement>)x.Value).CompareTo((TElement)y.Value);
        }
    }
}
