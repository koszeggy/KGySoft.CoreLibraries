using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Forces objects to be compared by reference.
    /// </summary>
    internal sealed class ReferenceEqualityComparer: IEqualityComparer<object>
    {
        private static ReferenceEqualityComparer comparer;

        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }

        internal static ReferenceEqualityComparer Comparer
        {
            get { return comparer ?? (comparer = new ReferenceEqualityComparer()); }
        }
    }
}
