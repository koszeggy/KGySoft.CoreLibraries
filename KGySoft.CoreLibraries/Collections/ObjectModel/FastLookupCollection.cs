#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastLookupCollection.cs
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using KGySoft.Diagnostics;
using KGySoft.Reflection;

#endregion

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
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [Serializable]
    public class FastLookupCollection<T> : VirtualCollection<T>
    {
        #region Fields

        [NonSerialized] private AllowNullDictionary<T, CircularList<int>> itemToIndex = new AllowNullDictionary<T, CircularList<int>>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether consistency of the stored items should be checked when items are get or set in the collection.
        /// <br/>Default value: <see langword="false"/>, if the <see cref="FastLookupCollection{T}"/> was initialized by the default constructor; otherwise, as it was specified.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="CheckConsistency"/> is <see langword="true"/>, then the <see cref="FastLookupCollection{T}"/> class is tolerant with direct modifications of the underlying collection directly but
        /// when inconsistency is detected, the cost of <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> and <see cref="VirtualCollection{T}.Contains">Contains</see> methods can fall back to O(n)
        /// where n is the count of the elements in the collection.</para>
        /// </remarks>
        public bool CheckConsistency { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty instance of the <see cref="FastLookupCollection{T}"/> class  with a <see cref="CircularList{T}"/> internally.
        /// </summary>
        public FastLookupCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastLookupCollection{T}"/> class as a wrapper for the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The list that is wrapped by the new collection.</param>
        /// <param name="checkConsistency"><see langword="true"/>&#160;to keep checking consistency of the wrapped <paramref name="list"/> and the inner storage;
        /// <see langword="false"/>&#160;to not check whether the wrapped <paramref name="list"/> changed. It can be <see langword="false"/>&#160;if the wrapped list is not changed outside of this <see cref="FastLookupCollection{T}"/> instance. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null" />.</exception>
        public FastLookupCollection(IList<T> list, bool checkConsistency = true) : base(list)
        {
            BuildIndexMap();
            CheckConsistency = checkConsistency;
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Internal Methods

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

        #endregion

        #region Private Methods

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

        private static void AdjustIndex(CircularList<int> indices, int startIndex, int diff)
        {
            int length = indices.Count;
            for (int i = 0; i < length; i++)
            {
                if (indices[i] >= startIndex)
                    indices[i] += diff;
            }
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Rebuilds the internally stored index mapping. Call if <see cref="CheckConsistency"/> is <see langword="false"/>&#160;
        /// and the internally wrapped list has been changed explicitly.
        /// </summary>
        public virtual void InnerListChanged() => BuildIndexMap();

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the zero-based index of the first of the specified <paramref name="item" /> within the <see cref="FastLookupCollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="FastLookupCollection{T}"/>. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns>
        /// The zero-based index of the found occurrence of <paramref name="item" /> within the <see cref="FastLookupCollection{T}"/>, if found; otherwise, <c>-1</c>.
        /// </returns>
        /// <remarks>
        /// <para>In <see cref="FastLookupCollection{T}"/> this method has an O(1) cost, unless <see cref="CheckConsistency"/> is <see langword="true"/>&#160;and inconsistency is
        /// detected, in which case it has an O(n) cost. Inconsistency can happen if the underlying collection has been modified directly instead of accessing it only via this instance.</para>
        /// </remarks>
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

        /// <summary>
        /// Gets the element at the specified <paramref name="index" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified <paramref name="index"/>.</returns>
        /// <remarks>
        /// <para>This method has an O(1) cost, unless <see cref="CheckConsistency"/> is <see langword="true"/>&#160;and inconsistency is
        /// detected, in which case it has an O(n) cost. Inconsistency can happen if the underlying collection has been modified directly instead of accessing it only via this instance.</para>
        /// </remarks>
        protected override T GetItem(int index)
        {
            T result = base.GetItem(index);
            if (CheckConsistency && !ContainsIndex(itemToIndex, result, index))
                BuildIndexMap();

            return result;
        }

        /// <summary>
        /// Replaces the <paramref name="item" /> at the specified <paramref name="index" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index.</param>
        /// <remarks>
        /// <para>This method has an O(1) cost, unless <see cref="CheckConsistency"/> is <see langword="true"/>&#160;and inconsistency is
        /// detected, in which case it has an O(n) cost. Inconsistency can happen if the underlying collection has been modified directly instead of accessing it only via this instance.</para>
        /// </remarks>
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

        /// <summary>
        /// Inserts an element into the <see cref="FastLookupCollection{T}"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
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

        /// <summary>
        /// Removes the element at the specified <paramref name="index" /> from the <see cref="FastLookupCollection{T}" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
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

        /// <summary>
        /// Removes all elements from the <see cref="FastLookupCollection{T}" />.
        /// </summary>
        protected override void ClearItems()
        {
            base.ClearItems();
            itemToIndex.Clear();
        }

        #endregion

        #region Private Methods

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx) => BuildIndexMap();

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

        #endregion

        #endregion

        #endregion
    }
}
