#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastLookupCollection.cs
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
using System.Collections.ObjectModel;
using System.Diagnostics;

using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;
#if NET35 || NET40 || NET45 || NETSTANDARD2_0
using KGySoft.Reflection;
#endif

#endregion

namespace KGySoft.Collections.ObjectModel
{
    /// <summary>
    /// Similar to <see cref="Collection{T}"/> but <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> and <see cref="VirtualCollection{T}.Contains">Contains</see> methods
    /// have O(1) access if the underlying collection is changed through only the <see cref="FastLookupCollection{T}"/> class.
    /// </summary>
    /// <remarks>
    /// <para>If <see cref="CheckConsistency"/> is <see langword="true"/>, then the <see cref="FastLookupCollection{T}"/> class is tolerant with direct modifications of the underlying collection but
    /// when inconsistency is detected, the cost of <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> and <see cref="VirtualCollection{T}.Contains">Contains</see> methods can fall back to O(n)
    /// where n is the count of the elements in the collection.</para>
    /// <note type="warning">Do not store elements in a <see cref="FastLookupCollection{T}"/> that may change their hash code while they are added to the collection.
    /// Finding such elements may fail even if <see cref="CheckConsistency"/> is <see langword="true"/>.</note>
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="VirtualCollection{T}" />
    /// <seealso cref="Collection{T}" />
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [Serializable]
    public class FastLookupCollection<T> : VirtualCollection<T>
    {
        #region Fields

        #region Static Fields

        private protected static readonly IEqualityComparer<T> Comparer = ComparerHelper<T>.EqualityComparer;

        #endregion

        #region Instance Fields

        /// <summary>
        /// A lazy-initialized item-index cache. A negated index means that the item occurs multiple times so on remove the cache has to be invalidated.
        /// </summary>
        [NonSerialized] private AllowNullDictionary<T, int>? itemToIndex;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether consistency of the stored items should be checked when items are get or set in the collection.
        /// <br/>Default value: <see langword="false"/>, if the <see cref="FastLookupCollection{T}"/> was initialized by the default constructor; otherwise, as it was specified.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="CheckConsistency"/> is <see langword="true"/>, then the <see cref="FastLookupCollection{T}"/> class is tolerant with direct modifications of the underlying collection but
        /// when inconsistency is detected, the cost of <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> and <see cref="VirtualCollection{T}.Contains">Contains</see> methods can fall back to O(n)
        /// where n is the count of the elements in the collection.</para>
        /// <note type="warning">Do not store elements in a <see cref="FastLookupCollection{T}"/> that may change their hash code while they are added to the collection.
        /// Finding such elements may fail even if <see cref="CheckConsistency"/> is <see langword="true"/>.</note>
        /// </remarks>
        public bool CheckConsistency { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty instance of the <see cref="FastLookupCollection{T}"/> class  with a <see cref="CircularList{T}"/> internally.
        /// </summary>
        public FastLookupCollection() : base(new CircularList<T>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastLookupCollection{T}"/> class as a wrapper for the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The list that is wrapped by the new collection.</param>
        /// <param name="checkConsistency"><see langword="true"/>&#160;to keep checking consistency of the wrapped <paramref name="list"/> and the inner storage;
        /// <see langword="false"/>&#160;to not check whether the wrapped <paramref name="list"/> changed. It can be <see langword="false"/>&#160;if the wrapped list is not changed outside of this <see cref="FastLookupCollection{T}"/> instance. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null" />.</exception>
        public FastLookupCollection(IList<T> list, bool checkConsistency = true) : base(list) => CheckConsistency = checkConsistency;

        #endregion

        #region Methods

        #region Static Methods

        private protected static int GetActualIndex(int index) => index >= 0 ? index : ~index;
        private protected static bool IsDuplicate(int index) => index < 0;

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Invalidates the internally stored index mapping. Call if the wrapped list that has been passed to the constructor has been changed explicitly.
        /// </summary>
        public virtual void InnerListChanged() => InvalidateMapping(true);

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
        /// <para>In <see cref="FastLookupCollection{T}"/> this method has an O(n) cost for the first time or then the internal mapping has to be rebuilt (because,
        /// for example, <see cref="CheckConsistency"/> is <see langword="true"/>&#160;and inconsistency is detected); otherwise, it has an O(1) cost.
        /// Inconsistency can happen if the underlying collection has been modified directly instead of accessing it only via this instance.</para>
        /// </remarks>
        protected override int GetItemIndex(T item) => DoGetItemIndex(item, true);

        /// <summary>
        /// Replaces the <paramref name="item" /> at the specified <paramref name="index" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index.</param>
        protected override void SetItem(int index, T item)
        {
            if (itemToIndex != null)
            {
                T original = base.GetItem(index);
                if (ReferenceEquals(original, item))
                    return;

                // invalidating if mapping to original is not unique or not consistent
                if (!itemToIndex.TryGetValue(original, out int origIndex)
                    || IsDuplicate(origIndex)
                    || origIndex != index)
                {
                    // rebuild notification on inconsistency: original was not found or unique index was incorrect
                    InvalidateMapping(!IsDuplicate(origIndex));
                }
                else
                {
                    // removing original item from mapping
                    itemToIndex.Remove(original);

                    // adding new item to mapping
                    if (itemToIndex.TryGetValue(item, out int existingIndex))
                    {
                        if (!ConsistencyCheck(existingIndex, item))
                            InvalidateMapping(true);
                        else if (!IsDuplicate(existingIndex))
                            itemToIndex[item] = ~existingIndex;
                    }
                    else
                        itemToIndex[item] = index;
                }
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
            if (itemToIndex == null)
                return;

            // adding to the last position
            if (index == Count - 1)
            {
                if (itemToIndex.TryGetValue(item, out int existingIndex))
                {
                    if (!ConsistencyCheck(existingIndex, item))
                        InvalidateMapping(true);
                    else if (!IsDuplicate(existingIndex))
                        itemToIndex[item] = ~existingIndex;
                }
                else
                    itemToIndex[item] = index;
            }
            // insertion: dropping index map because an O(n) traversal would be needed
            else
                InvalidateMapping(false);
        }

        /// <summary>
        /// Removes the element at the specified <paramref name="index" /> from the <see cref="FastLookupCollection{T}" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        protected override void RemoveItemAt(int index)
        {
            if (itemToIndex == null)
            {
                base.RemoveItemAt(index);
                return;
            }

            // we remove item first, and then do the adjustments because the possible rebuilding notification must be sent with the new list
            T item = base.GetItem(index);
            base.RemoveItemAt(index);

            // removing from the last position
            if (index == Count)
            {
                // invalidating if mapping to the item was not unique or not consistent
                if (!itemToIndex.TryGetValue(item, out int existingIndex)
                    || IsDuplicate(existingIndex)
                    || existingIndex != index)
                {
                    // rebuild notification on inconsistency: original was not found or unique index was incorrect
                    InvalidateMapping(IsDuplicate(existingIndex));
                }
                else
                    itemToIndex.Remove(item);
            }
            // removing non-last: dropping index map because an O(n) traversal would be needed
            else
                InvalidateMapping(false);
        }

        /// <summary>
        /// Removes the first occurrence of <paramref name="item"/> from the <see cref="FastLookupCollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="FastLookupCollection{T}"/>.</param>
        /// <returns><see langword="true"/>, if an occurrence of <paramref name="item" /> was removed; otherwise, <see langword="false"/>.</returns>
        protected override bool RemoveItem(T item)
        {
            // if item to index map is not built we do not allow building it because it likely will be invalidated on remove anyway
            int index = DoGetItemIndex(item, false);
            if (index < 0)
                return false;
            RemoveItemAt(index);
            return true;
        }

        /// <summary>
        /// Removes all elements from the <see cref="FastLookupCollection{T}" />.
        /// </summary>
        protected override void ClearItems()
        {
            base.ClearItems();
            InvalidateMapping(false);
        }

        /// <summary>
        /// Called after the internal index map has been rebuilt either when inconsistency has been detected or when <see cref="InnerListChanged">InnerListChanged</see> has been called.
        /// </summary>
        protected virtual void OnMapRebuilt()
        {
            // NOTE: this method actually no longer means an actual index rebuild. It might also be called after detecting an inconsistency,
            // but without rebuilding the index map (which is nullable now). It is still called OnMapRebuilt to remain compatible.
            // From outside it is actually transparent whether there is an up-to-date internal map.
        }

        #endregion

        #region Private Methods

        private void BuildIndexMap(bool notifyRebuilt)
        {
            int count = Count;
            if (itemToIndex == null || itemToIndex.Count > 0)
                itemToIndex = new AllowNullDictionary<T, int>();
            for (int i = 0; i < count; i++)
            {
                T item = base.GetItem(i);
                if (!itemToIndex.TryGetValue(item, out int index))
                    itemToIndex[item] = i;
                else if (!IsDuplicate(index))
                    itemToIndex[item] = ~index;
            }

            if (notifyRebuilt)
                OnMapRebuilt();
        }

        private int DoGetItemIndex(T item, bool allowBuildIndexMap)
        {
            int index;
            if (itemToIndex != null)
            {
                if (!itemToIndex.TryGetValue(item, out index))
                    return -1;

                if (ConsistencyCheck(index, item))
                    return GetActualIndex(index);
            }

            if (!allowBuildIndexMap)
                return base.GetItemIndex(item);
            BuildIndexMap(itemToIndex != null);
            return itemToIndex!.TryGetValue(item, out index) ? GetActualIndex(index) : -1;
        }

        private void InvalidateMapping(bool notifyRebuilt)
        {
            itemToIndex = null;

            // NOTE: There is no rebuild here because it is now lazily postponed for next GetItemIndex call but this is how we remain compatible
            if (notifyRebuilt)
                OnMapRebuilt();
        }

        private bool ConsistencyCheck(int index, T item)
        {
            if (!CheckConsistency)
                return true;
            if (IsDuplicate(index))
                index = ~index;
            return (uint)index < (uint)Count && Comparer.Equals(item, base.GetItem(index));
        }

        #endregion

        #endregion

        #endregion
    }
}
