using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using KGySoft.Diagnostics;
using KGySoft.Reflection;

namespace KGySoft.Collections.ObjectModel
{
    /// <summary>
    /// Similar to <see cref="Collection{T}"/> but <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> and <see cref="VirtualCollection{T}.Contains">Contains</see> methods
    /// have O(1) access if the underlying collection is changed through only the <see cref="FastLookupCollection{T}"/> class.
    /// </summary>
    /// <remarks>
    /// <para>If <see cref="CheckConsistency"/> is <see langword="true"/>, then the <see cref="FastLookupCollection{T}"/> class is tolerant with direct modifications of the underlying collection directly but
    /// when inconsistency is detected, the cost of <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> and <see cref="VirtualCollection{T}.Contains">Contains</see> methods can fall back to O(n)
    /// where n is the count of the elements in the collection.</para>
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="VirtualCollection{T}" />
    /// <seealso cref="Collection{T}" />
    // TODO: IndexDictionary<T> instead of AllowNullDictionary<T, CircularList<int>>, which is IDictionary<T, int> and internally has an AllowNullDictionary<T, int> and AllowNullDictionary<T, CircularList<int>> - if the first one is -1, there is the 2nd one.
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [Serializable]
    public class FastLookupCollection<T> : VirtualCollection<T>
    {
        [NonSerialized] private AllowNullDictionary<T, CircularList<int>> itemToIndex = new AllowNullDictionary<T, CircularList<int>>();

        public bool CheckConsistency { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx) => BuildIndexMap();

        /// <summary>
        /// Initializes an empty instance of the <see cref="FastLookupCollection{T}"/> class.
        /// </summary>
        public FastLookupCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastLookupCollection{T}"/> class as a wrapper for the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The list that is wrapped by the new collection.</param>
        /// <param name="checkConsistency"><see langword="true"/> to keep checking consistency of the wrapped <paramref name="list"/> and the inner storage;
        /// <see langword="false"/> to not check whether the wrapped <paramref name="list"/> changed. It can be <see langword="false"/> if the wrapped list is not changed outside of this <see cref="FastLookupCollection{T}"/> instance. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null" />.</exception>
        public FastLookupCollection(IList<T> list, bool checkConsistency = true) : base(list)
        {
            BuildIndexMap();
            CheckConsistency = checkConsistency;
        }

        private void BuildIndexMap()
        {
            if (itemToIndex.Count > 0)
                itemToIndex = new AllowNullDictionary<T, CircularList<int>>();
            int length = Count;
            for (int i = 0; i < length; i++)
            {
                T item = base.GetItem(i);
                AddIndex(itemToIndex, item, i);
            }
        }

        /// <summary>
        /// Rebuilds the internally stored index mapping. Call if <see cref="CheckConsistency"/> is <see langword="false"/>
        /// and the internally wrapped list has been changed directly.
        /// </summary>
        public virtual void InnerListChanged() => BuildIndexMap();

        protected override int GetItemIndex(T item)
        {
            int result = GetFirstIndex(itemToIndex, item);
            if (!CheckConsistency)
                return result;

            if (result < 0 || result < Count && AreEqual(item, base.GetItem(result)))
                return result;

            // the underlying collection is inconsistent
            BuildIndexMap();
            return GetFirstIndex(itemToIndex, item);
        }

        protected override T GetItem(int index)
        {
            T result = base.GetItem(index);
            if (CheckConsistency && !ContainsIndex(itemToIndex, result, index))
                BuildIndexMap();

            return result;
        }

        protected override void SetItem(int index, T item)
        {
            if (CheckConsistency && !ContainsIndex(itemToIndex, item, index))
                BuildIndexMap();

            T original = base.GetItem(index);
            if (ReferenceEquals(original, item))
                return;

            // here we can't ignore inconsistency because we need to update the maintained indices
            if (!RemoveIndex(itemToIndex, original, index) || !AddIndex(itemToIndex, item, index))
            {
                BuildIndexMap();
                RemoveIndex(itemToIndex, original, index);
                AddIndex(itemToIndex, item, index);
            }

            base.SetItem(index, item);
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            int length = Count;

            // here we can't ignore consistency because we need to update the maintained indices
            if (index + 1 < length)
            {
                HashSet<T> adjustedValues = CreateAdjustSet(length);
                for (int i = index + 1; i < length; i++)
                {
                    if (!AdjustIndex(itemToIndex, base.GetItem(i), index, 1, adjustedValues))
                    {
                        BuildIndexMap();
                        return;
                    }
                }
            }

            if (!AddIndex(itemToIndex, item, index))
                BuildIndexMap();
        }

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal /*private protected*/ static HashSet<T> CreateAdjustSet(int length)
        {
#if NET35 || NET40 || NET45
            HashSet<T> result = new HashSet<T>();
            if (length > 50) // based on performance tests, preallocating capacity by reflection starts to be beneficial from 50 elements
                result.Initialize(length);
#else
            HashSet<T> result = new HashSet<T>(length);
#endif
            return result;
        }

        protected override void RemoveItem(int index)
        {
            T original = base.GetItem(index);
            base.RemoveItem(index);

            // here we can't ignore inconsistency because we need to update the maintained indices
            if (!RemoveIndex(itemToIndex, original, index))
            {
                BuildIndexMap();
                return;
            }

            int length = Count;
            if (index < length)
            {
                HashSet<T> adjustedValues = CreateAdjustSet(length);
                for (int i = index; i < length; i++)
                {
                    if (!AdjustIndex(itemToIndex, base.GetItem(i), index, -1, adjustedValues))
                    {
                        BuildIndexMap();
                        return;
                    }
                }
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            itemToIndex.Clear();
        }

        internal /*private protected*/ static bool AreEqual(T x, T y)
            => EqualityComparer<T>.Default.Equals(x, y);

        internal /*private protected*/ static int GetFirstIndex(AllowNullDictionary<T, CircularList<int>> map, T item) 
            => map.TryGetValue(item, out CircularList<int> indices) ? indices[0] : -1;

        internal /*private protected*/ static bool ContainsIndex(AllowNullDictionary<T, CircularList<int>> map, T item, int index)
            => map.TryGetValue(item, out var indices) && indices.Contains(index);

        /// <summary>Adds an index to the map and returns whether things still seem to be consistent.</summary>
        internal /*private protected*/ static bool AddIndex(AllowNullDictionary<T, CircularList<int>> map, T item, int index)
        {
            if (!map.TryGetValue(item, out CircularList<int> indices))
            {
                indices = new CircularList<int>(1);
                map[item] = indices;
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
        internal /*private protected*/ static bool RemoveIndex(AllowNullDictionary<T, CircularList<int>> map, T item, int index)
        {
            if (!map.TryGetValue(item, out CircularList<int> indices) || !RemoveIndex(indices, index))
                return false;
            if (indices.Count == 0)
                map.Remove(item);
            return true;
        }

        /// <summary>Removes an index from the map and returns whether things still seem to be consistent.</summary>
        private static bool RemoveIndex(CircularList<int> indices, int index)
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

        internal /*private protected*/ static bool AdjustIndex(AllowNullDictionary<T, CircularList<int>> map, T item, int startIndex, int diff, HashSet<T> adjustedValues)
        {
            if (adjustedValues.Contains(item))
                return true;
            adjustedValues.Add(item);
            if (!map.TryGetValue(item, out var indices))
                return false;
            AdjustIndex(indices, startIndex, diff);
            return true;
        }

        private static void AdjustIndex(CircularList<int> indices, int startIndex, int diff)
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
