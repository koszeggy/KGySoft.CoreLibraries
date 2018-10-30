using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KGySoft.Collections.ObjectModel
{
    /// <summary>
    /// Similar to <see cref="Collection{T}"/> but <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> and <see cref="VirtualCollection{T}.Contains">Contains</see> methods
    /// have O(1) access if the underlying collection is changed through only the <see cref="QuickLookupCollection{T}"/> class.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="QuickLookupCollection{T}"/> class is tolerant with modifying the underlying collection directly but in this case the
    /// cost of <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> and <see cref="VirtualCollection{T}.Contains">Contains</see> methods can fall back to O(n)
    /// where n is the count of the elements in the collection.</para>
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="VirtualCollection{T}" />
    /// <seealso cref="Collection{T}" />
    public class QuickLookupCollection<T> : VirtualCollection<T>
    {
        //private Dictionary<int, T> indexToItem = new Dictionary<int, T>();
        private Dictionary<T, CircularList<int>> itemToIndex = new Dictionary<T, CircularList<int>>();
        private CircularList<int> nullToIndex;

        /// <summary>
        /// Initializes an empty instance of the <see cref="QuickLookupCollection{T}"/> class.
        /// </summary>
        public QuickLookupCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickLookupCollection{T}"/> class as a wrapper for the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The list that is wrapped by the new collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null" />.</exception>
        public QuickLookupCollection(IList<T> list) : base(list) => BuildIndexMap();

        private void BuildIndexMap()
        {
            //indexToItem = new Dictionary<int, T>();
            itemToIndex = new Dictionary<T, CircularList<int>>();
            nullToIndex = null;
            int length = Count;
            for (int i = 0; i < length; i++)
            {
                T item = Items[i];
                AddIndex(item, i);
            }
        }

        protected override int GetItemIndex(T item)
        {
            int result = GetFirstIndex(item);
            if (result < 0 || result < Count && AreEqual(item, Items[result]))
                return result;

            // the underlying collection is inconsistent
            BuildIndexMap();
            return GetFirstIndex(item);
        }

        protected override void SetItem(int index, T item)
        {
            T original = Items[index];

            // the underlying collection is inconsistent
            if (!ContainsIndex(item, index))
                BuildIndexMap();

            if (!AreEqual(original, item))
            {
                RemoveIndex(original, index);
                AddIndex(item, index);
            }

            base.SetItem(index, item);
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            int length = Count;
            for (int i = index + 1; i < length; i++)
            {
                if (!AdjustIndex(Items[i], index, 1))
                {
                    BuildIndexMap();
                    return;
                }
            }
            if (!AddIndex(item, index))
                BuildIndexMap();
        }

        protected override void RemoveItem(int index)
        {
            T original = Items[index];
            base.RemoveItem(index);
            if (!RemoveIndex(original, index))
            {
                BuildIndexMap();
                return;
            }
            int length = Count;
            for (int i = index; i < length; i++)
            {
                if (!AdjustIndex(Items[i], index, -1))
                {
                    BuildIndexMap();
                    return;
                }
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            nullToIndex = null;
            itemToIndex.Clear();
        }

        private static bool AreEqual(T x, T y)
            => EqualityComparer<T>.Default.Equals(x, y);

        private int GetFirstIndex(T item) 
            => item == null 
                ? nullToIndex?[0] ?? -1
                : itemToIndex.TryGetValue(item, out var indices) ? indices[0] : -1;

        private bool ContainsIndex(T item, int index)
            => item == null
                ? nullToIndex?.Contains(index) == true
                : itemToIndex.TryGetValue(item, out var indices) && indices.Contains(index);

        /// <summary>Adds an index to the map and returns whether things still seem to be consistent.</summary>
        private bool AddIndex(T item, int index)
        {
            CircularList<int> indices;
            if (item == null)
                indices = nullToIndex ?? (nullToIndex = new CircularList<int>());
            else
            {
                if (!itemToIndex.TryGetValue(item, out indices))
                {
                    indices = new CircularList<int>();
                    itemToIndex[item] = indices;
                }
            }

            if (indices.Count == 0 || index > indices[indices.Count - 1])
            {
                indices.AddLast(index);
                return true;
            }

            var pos = indices.BinarySearch(index);
            if (pos >= 0)
                return false;
            indices.Insert(~pos, index);
            return true;
        }

        /// <summary>Removes an index from the map and returns whether things still seem to be consistent.</summary>
        private bool RemoveIndex(T item, int index)
        {
            if (item == null)
            {
                if (nullToIndex == null || !RemoveIndex(nullToIndex, index))
                    return false;
                if (nullToIndex.Count == 0)
                    nullToIndex = null;
                return true;
            }

            if (!itemToIndex.TryGetValue(item, out CircularList<int> indices) || !RemoveIndex(indices, index))
                return false;
            if (indices.Count == 0)
                itemToIndex.Remove(item);
            return true;
        }

        /// <summary>Removes an index from the map and returns whether things still seem to be consistent.</summary>
        private bool RemoveIndex(CircularList<int> indices, int index)
        {
            if (indices.Count == 0)
                return false;
            if (indices[0] == index)
            {
                indices.RemoveFirst();
                return true;
            }

            int pos = indices.BinarySearch(index);
            if (pos < 0)
                return false;
            indices.RemoveAt(pos);
            return true;
        }

        private bool AdjustIndex(T item, int startIndex, int diff)
        {
            if (item == null)
            {
                if (nullToIndex == null)
                    return false;
                AdjustIndex(nullToIndex, startIndex, diff);
                return true;
            }

            if (!itemToIndex.TryGetValue(item, out var indices))
                return false;
            AdjustIndex(indices, startIndex, diff);
            return true;
        }

        private void AdjustIndex(CircularList<int> indices, int startIndex, int diff)
        {
            int length = indices.Count;
            for (int i = 0; i < length; i++)
            {
                if (indices[i] >= startIndex)
                    indices[i] += diff;
            }
        }
    }
}
