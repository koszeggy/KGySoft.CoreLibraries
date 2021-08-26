#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SortableBindingList.cs
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
using System.Collections;
using System.Collections.Generic;
#if !NET35
using System.Collections.ObjectModel; 
#endif
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

using KGySoft.Collections;
using KGySoft.Collections.ObjectModel;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a sortable generic list that is able to notify its consumer about changes and supports data binding.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <remarks>
    /// <note>Due to performance reasons the <see cref="SortableBindingList{T}"/> class is derived from <see cref="FastBindingList{T}"/> and not from <see cref="BindingList{T}"/>.
    /// <br/>See also the <strong>Remarks</strong> section of the <see cref="FastBindingList{T}"/> class for the differences compared to the <see cref="BindingList{T}"/> class.</note>
    /// <para>The <see cref="SortableBindingList{T}"/> class provides a sortable view for the wrapped list specified in the constructor.</para>
    /// <para>To sort the list the <see cref="O:KGySoft.ComponentModel.FastBindingList`1.ApplySort">ApplySort</see> overloads can be used. As sorting is supported via the standard <see cref="IBindingList"/> interface,
    /// binding a <see cref="SortableBindingList{T}"/> instance to UI controls (eg. to a grid) enables sorting automatically in several GUI frameworks.</para>
    /// <note>Sorting does not change the order of the elements in the wrapped underlying collection. When items are added while <see cref="FastBindingList{T}.IsSorted"/> returns <see langword="true"/>, then new items are added
    /// to the end of the underlying list.</note>
    /// <note type="warning">Do not store elements in a <see cref="SortableBindingList{T}"/> that may change their hash code while they are added to the collection.
    /// Finding such elements may fail even if <see cref="FastLookupCollection{T}.CheckConsistency"/> is <see langword="true"/>. If hash code is derived from some identifier property or field, you can prepare
    /// a <typeparamref name="T"/> instance by overriding the <see cref="FastBindingList{T}.AddNewCore">AddNewCore</see> method or by subscribing the <see cref="FastBindingList{T}.AddingNew"/> event
    /// to make <see cref="IBindingList.AddNew">IBindingList.AddNew</see> implementation work properly.</note> 
    /// </remarks>
    [Serializable]
    public class SortableBindingList<T> : FastBindingList<T>
    {
        #region Nested Classes

        private sealed class SortedEnumerator : IEnumerator<T>
        {
            #region Fields

            private readonly SortableBindingList<T> owner;
            private readonly CircularList<(int Index, object? Value)> mapReference;

            private int index;
            [AllowNull]private T current = default!;

            #endregion

            #region Properties

            #region Public Properties

            public T Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object? IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index > mapReference.Count)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal SortedEnumerator(SortableBindingList<T> owner)
            {
                this.owner = owner;
                mapReference = owner.sortedToBaseIndex!;
            }

            #endregion

            #region Methods

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (mapReference != owner.sortedToBaseIndex)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                if (index < mapReference.Count)
                {
                    current = owner.GetItemBySortedIndexUnchecked(index);
                    index += 1;
                    return true;
                }

                index = mapReference.Count + 1;
                current = default;
                return false;
            }

            public void Reset()
            {
                if (mapReference != owner.sortedToBaseIndex)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = 0;
                current = default;
            }

            #endregion
        }

        #endregion

        #region Fields

        [NonSerialized]private bool isChangingOrRaisingChanged;
        [NonSerialized]private bool sortPending; // indicates that the list is sorted but contains unsorted elements due to insertion
        [NonSerialized]private PropertyDescriptor? sortProperty;
        private string? sortPropertyName; // for serialization

        private ListSortDirection? sortDirection;
        [NonSerialized]private CircularList<(int Index, object? Value)>? sortedToBaseIndex; // int is not enough, because contains the evaluated property value can be used for comparison when an item is inserted/changed
        [NonSerialized]private IComparer<(int Index, object? Value)>? itemComparer;
        [NonSerialized]private AllowNullDictionary<T, int>? itemToSortedIndex;

        private int addNewPos = -1;
        private bool sortOnChange;
        [NonSerialized]private bool isCancelingNew;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets whether the <see cref="SortableBindingList{T}"/> should be immediately re-sorted when an item changes or a new item is added.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks><para>Setting this property to <see langword="true"/>&#160;may cause re-sorting the <see cref="SortableBindingList{T}"/> immediately.</para></remarks>
        public bool SortOnChange
        {
            get => sortOnChange;
            set
            {
                if (value == sortOnChange)
                    return;
                sortOnChange = value;
                if (value && sortPending)
                    BuildSortedIndexMap(true, false);
            }
        }

        /// <summary>
        /// Gets or sets the direction of the sort. Returns <see langword="null"/>, if the list is not sorted
        /// (that is, when <see cref="FastBindingList{T}.IsSorted"/> returns <see langword="false"/>).
        /// Setting <see langword="null"/>&#160;removes sorting. To change also the <see cref="FastBindingList{T}.SortProperty"/>
        /// call the <see cref="FastBindingList{T}.ApplySort(PropertyDescriptor, ListSortDirection)"/> method instead.
        /// </summary>
        public ListSortDirection? SortDirection
        {
            get => sortDirection;
            set
            {
                if (sortDirection == value)
                    return;
                if (value == null)
                    RemoveSortCore();
                else
                    ApplySortCore(sortProperty, value.Value);
            }
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets whether the list supports sorting.
        /// <br/>The <see cref="SortableBindingList{T}"/> returns <see langword="true"/>.
        /// </summary>
        protected override bool SupportsSortingCore => true;

        /// <summary>
        /// Gets whether the list is sorted.
        /// </summary>
        protected override bool IsSortedCore => sortDirection != null;

        /// <summary>
        /// Gets the property descriptor that is used for sorting the list if sorting, or <see langword="null"/>&#160;if the list is not sorted or
        /// when it is sorted by the values of <typeparamref name="T"/> rather than by one of its properties.
        /// </summary>
        protected override PropertyDescriptor? SortPropertyCore => sortProperty;

        /// <summary>
        /// Gets the direction of the sort.
        /// <br/>If the list is not sorted (that is, when <see cref="FastBindingList{T}.IsSorted"/> returns <see langword="false"/>), this property returns <see cref="ListSortDirection.Ascending"/>.
        /// </summary>
        protected override ListSortDirection SortDirectionCore => sortDirection.GetValueOrDefault();

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortableBindingList{T}"/> class with a <see cref="CircularList{T}"/> internally.
        /// </summary>
        public SortableBindingList()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortableBindingList{T}"/> class with the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">An <see cref="IList{T}" /> of items to be contained in the <see cref="SortableBindingList{T}" />.</param>
        /// <remarks>
        /// <note>Do not wrap another <see cref="IBindingList"/> or <see cref="ObservableCollection{T}"/> as their events are not captured by the <see cref="SortableBindingList{T}"/> class.
        /// To capture and generate events for both wrapped and self list operations use <see cref="ObservableBindingList{T}"/> instead.</note>
        /// </remarks>
        public SortableBindingList(IList<T> list) : base(list)
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        private static IComparer<(int, object?)> CreateComparer(bool ascending, Type valueType)
        {
            if (valueType.GetInterfaces().Any(i => i.IsGenericTypeOf(typeof(IComparable<>)) && i.GetGenericArguments()[0] == valueType))
                return (IComparer<(int, object?)>)Reflector.CreateInstance(typeof(ItemGenericComparer<>).GetGenericType(valueType), ascending);
            return new ItemComparer(ascending);
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <inheritdoc/>
        public override void InnerListChanged()
        {
            base.InnerListChanged();
            if (sortDirection != null)
                InvalidateMapping(true);
        }

        /// <inheritdoc/>
        public override void CancelNew(int itemIndex)
        {
            if (sortDirection != null && sortedToBaseIndex!.Count != Count)
            {
                // if there is inconsistency there will be an EndNew instead of cancel because we cannot be sure what to remove
                BuildSortedIndexMap(true, false);
                return;
            }

            if (addNewPos < 0 || addNewPos != itemIndex)
                return;

            isCancelingNew = true;
            try
            {
                // this will call RemoveItem with base index
                base.CancelNew(sortDirection == null ? itemIndex : GetBaseIndex(itemIndex));
            }
            finally
            {
                isCancelingNew = false;
            }
        }

        /// <inheritdoc/>
        public override void EndNew(int itemIndex)
        {
            if (sortDirection != null && sortedToBaseIndex!.Count != Count)
            {
                BuildSortedIndexMap(true, false);
                return;
            }

            if (addNewPos < 0 || addNewPos != itemIndex)
                return;

            base.EndNew(sortDirection == null ? itemIndex : GetBaseIndex(itemIndex));
            if (sortDirection != null && sortPending && SortOnChange)
                BuildSortedIndexMap(true, false);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="SortableBindingList{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}" /> for the <see cref="SortableBindingList{T}"/>.</returns>
        /// <remarks>
        /// <para>If the list is sorted (that is, when <see cref="FastBindingList{T}.IsSorted"/> returns <see langword="true"/>), then the items are returned in the sorted order. Otherwise, the items are returned
        /// in the order as they are stored in the wrapped underlying collection.</para>
        /// <note>If the <see cref="SortableBindingList{T}"/> is sorted, or it was instantiated by the default constructor, then the returned enumerator supports
        /// the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method; otherwise, it depends on the enumerator of the wrapped collection.</note>
        /// </remarks>
        public override IEnumerator<T> GetEnumerator() => sortDirection == null ? base.GetEnumerator() : new SortedEnumerator(this);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Sorts the items of the list.
        /// </summary>
        /// <param name="property">A <see cref="PropertyDescriptor" /> that specifies the property to sort on. If <see langword="null" />, then the list will be sorted
        /// by the values of <typeparamref name="T" /> rather than one of its properties.</param>
        /// <param name="direction">The desired direction of the sort.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="direction"/> is not of the supported values.</exception>
        /// <remarks>
        /// <para>The sorting does not change the order of the items in the wrapped underlying collection.</para>
        /// <para>If <paramref name="property"/> is not <see langword="null"/>, then finding an item by the <see cref="O:KGySoft.ComponentModel.FastBindingList`1.Find">Find</see> overloads on the same property
        /// will be also faster.</para>
        /// </remarks>
        protected override void ApplySortCore(PropertyDescriptor? property, ListSortDirection direction)
        {
            if (!Enum<ListSortDirection>.IsDefined(direction))
                Throw.EnumArgumentOutOfRangeWithValues(Argument.direction, direction);

            sortProperty = property;
            sortDirection = direction;
            DoSort();
        }

        /// <summary>
        /// Removes any sort applied by the <see cref="ApplySortCore">ApplySortCore</see> method.
        /// </summary>
        protected override void RemoveSortCore()
        {
            if (sortDirection == null)
                return;

            EndNew();
            sortedToBaseIndex = null;
            itemComparer = null;
            itemToSortedIndex = null;
            sortProperty = null;
            sortDirection = null;
            sortPending = false;

            // In theory, also Moved could be used, which actually solves the problem of uncommitted edits in WinForms but
            // a.) WPF does not tolerate it b.) IBindingList docs say ApplySort must raise a ListChanged event with the Reset enumeration.
            FireListChanged(ListChangedType.Reset, -1);
        }

        /// <summary>
        /// Searches for the index of the item that has the specified property descriptor with the specified value.
        /// </summary>
        /// <param name="property">A <see cref="PropertyDescriptor" /> that specifies the property to search for.</param>
        /// <param name="key">The value of <paramref name="property" /> to match.</param>
        /// <returns>The zero-based index of the item that matches the property descriptor and contains the specified value.</returns>
        /// <remarks>
        /// <para>If <paramref name="property"/> equals <see cref="FastBindingList{T}.SortProperty"/>, then a binary search is performed, in which case the cost of this method is O(log n) where n is the count of elements in the list.</para>
        /// <para>In any other cases a linear search is performed, in which case the cost of this method is O(n).</para>
        /// </remarks>
        protected override int FindCore(PropertyDescriptor property, object key)
        {
            if (sortDirection == null || sortPending || !Equals(property, sortProperty))
                return base.FindCore(property, key);

            // Binary search if list is sorted on the searched property
            var value = (0, key);
            int lo = 0;
            int hi = Count - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = itemComparer!.Compare(sortedToBaseIndex![i], value);

                if (order == 0)
                    return i;

                if (order < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            return ~lo;
        }

        /// <inheritdoc/>
        protected override int GetItemIndex(T item)
            => sortDirection == null ? base.GetItemIndex(item) : GetSortedItemIndex(item, true);

        /// <inheritdoc/>
        protected override T GetItem(int index)
            => sortDirection == null ? base.GetItem(index) : GetItemBySortedIndex(index);

        /// <summary>
        /// Replaces the <paramref name="item" /> at the specified <paramref name="index" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index.</param>
        /// <remarks>
        /// <para><see cref="SetItem">SetItem</see> performs the following operations:
        /// <list type="number">
        /// <item>Raises a <see cref="FastBindingList{T}.ListChanged"/> event of type <see cref="ListChangedType.ItemChanged"/> indicating the index of the item that was set.</item>
        /// <item>If both <see cref="FastBindingList{T}.IsSorted"/> and <see cref="SortOnChange"/> properties are <see langword="true"/>&#160;and the position of the new item would break the sort order, then a new sort is immediately applied,
        /// which raises a <see cref="FastBindingList{T}.ListChanged"/> event of type <see cref="ListChangedType.Reset"/>.</item>
        /// </list>
        /// </para>
        /// </remarks>
        protected override void SetItem(int index, T item)
        {
            if (sortDirection == null)
            {
                base.SetItem(index, item);
                return;
            }

            if (sortedToBaseIndex!.Count != Count)
                InvalidateMapping(true);
            else
            {
                sortedToBaseIndex[index] = (sortedToBaseIndex[index].Index, sortProperty == null || item == null ? item : sortProperty.GetValue(item));

                if (itemToSortedIndex != null)
                {
                    T original = GetItemBySortedIndex(index);
                    if (!ReferenceEquals(original, item) && itemToSortedIndex != null)
                    {
                        // invalidating if mapping to original is not unique or not consistent
                        if (!itemToSortedIndex.TryGetValue(original, out int origIndex)
                            || IsDuplicate(origIndex)
                            || origIndex != index)
                        {
                            // rebuild on inconsistency: original was not found or unique index was incorrect
                            InvalidateMapping(!IsDuplicate(origIndex));
                        }
                        else
                        {
                            // removing original item from mapping
                            itemToSortedIndex.Remove(original);

                            // adding new item to mapping
                            if (itemToSortedIndex.TryGetValue(item, out int existingIndex))
                            {
                                if (!ConsistencyCheck(existingIndex, item))
                                    InvalidateMapping(true);
                                else if (!IsDuplicate(existingIndex))
                                    itemToSortedIndex[item] = ~existingIndex;
                            }
                            else
                                itemToSortedIndex[item] = index;
                        }
                    }
                }
            }

            isChangingOrRaisingChanged = true;
            try
            {
                base.SetItem(GetBaseIndex(index), item);
            }
            finally
            {
                isChangingOrRaisingChanged = false;
            }

            FireListChanged(ListChangedType.ItemChanged, index);
        }

        /// <summary>
        /// Inserts an element into the <see cref="SortableBindingList{T}" /> at the specified <paramref name="index" />.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        /// <remarks>
        /// <para><see cref="InsertItem">InsertItem</see> performs the following operations:
        /// <list type="number">
        /// <item>Calls <see cref="EndNew(int)">EndNew</see> to commit the last possible uncommitted item added by the <see cref="FastBindingList{T}.AddNew">AddNew</see> method.</item>
        /// <item>Inserts the item at the specified index.</item>
        /// <item>Raises a <see cref="FastBindingList{T}.ListChanged"/> event of type <see cref="ListChangedType.ItemChanged"/> indicating the index of the item that was inserted.</item>
        /// <item>If both <see cref="FastBindingList{T}.IsSorted"/> and <see cref="SortOnChange"/> properties are <see langword="true"/>&#160;and the position of the new item would break the sort order, then a new sort is immediately applied,
        /// which raises a <see cref="FastBindingList{T}.ListChanged"/> event of type <see cref="ListChangedType.Reset"/>.</item>
        /// </list>
        /// </para>
        /// </remarks>
        protected override void InsertItem(int index, T item)
        {
            if (sortDirection == null)
            {
                base.InsertItem(index, item);
                if (IsAddingNew)
                    addNewPos = index;
                return;
            }

            int origCount = Count;
            isChangingOrRaisingChanged = true;
            try
            {
                // index refers the sorted list here so in the underlying collection we just add to the last position
                base.InsertItem(origCount, item);
            }
            finally
            {
                isChangingOrRaisingChanged = false;
            }

            if (IsAddingNew)
                addNewPos = index;
            if (sortedToBaseIndex!.Count != origCount)
                InvalidateMapping(true);
            else
            {
                // no need to adjust indices in sortedToBaseIndex because we will add the item to the last position in underlying list
                sortedToBaseIndex.Insert(index, (origCount, sortProperty == null || item == null ? item : sortProperty.GetValue(item)));

                if (itemToSortedIndex != null)
                {
                    // adding to the last position
                    if (index == origCount)
                    {
                        if (itemToSortedIndex.TryGetValue(item, out int existingIndex))
                        {
                            if (!ConsistencyCheck(existingIndex, item))
                                InvalidateMapping(true);
                            else if (!IsDuplicate(existingIndex))
                                itemToSortedIndex[item] = ~existingIndex;
                        }
                        else
                            itemToSortedIndex[item] = index;
                    }
                    // inserting in the middle: dropping index map because an O(n) traversal would be needed
                    else
                        InvalidateMapping(false);
                }
            }

            FireListChanged(ListChangedType.ItemAdded, index);
        }

        /// <summary>
        /// Removes the element at the specified <paramref name="index" /> from the <see cref="SortableBindingList{T}" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <remarks>
        /// <para>This method raises the <see cref="FastBindingList{T}.ListChanged"/> event of type <see cref="ListChangedType.ItemDeleted"/>.</para>
        /// </remarks>
        protected override void RemoveItemAt(int index)
        {
            if (sortDirection == null)
            {
                base.RemoveItemAt(index);
                return;
            }

            if (isCancelingNew)
            {
                // RemoveItem is now called by the base.CancelNew so index is now the base index and the sorted index is the addNewPos
                if (addNewPos < 0)
                    return;
                index = addNewPos;
            }

            int newCount = Count - 1;
            if (sortedToBaseIndex!.Count != newCount + 1)
            {
                // Possible issue here: in case of inconsistency after the resort the removed element is actually random
                // But if canceling a new element we reset addNewPos so we must return here.
                InvalidateMapping(true);
                if (isCancelingNew)
                    return;
            }

            // the order of next lines are important because of possible resorting during consistency check
            T item = GetItemBySortedIndex(index);
            int baseIndex = GetBaseIndex(index);

            // we remove item first, and then do the adjustments because the possible rebuilding notification must be sent with the new list
            isChangingOrRaisingChanged = true;
            try
            {
                base.RemoveItemAt(baseIndex);
            }
            finally
            {
                isChangingOrRaisingChanged = false;
            }

            // adjusting sortedToBaseIndex (it is already consistent due to GetItemBySortedIndex)
            sortedToBaseIndex.RemoveAt(index);
            if (baseIndex < newCount)
            {
                for (int i = 0; i < newCount; i++)
                {
                    var sortIndex = sortedToBaseIndex[i];
                    if (sortIndex.Index > baseIndex)
                        sortedToBaseIndex[i] = (sortIndex.Index - 1, sortIndex.Value);
                }
            }

            // adjusting itemToSortedIndex
            if (itemToSortedIndex != null)
            {
                // removing from the last position
                if (index == newCount)
                {
                    // invalidating if mapping to the item was not unique or not consistent
                    if (!itemToSortedIndex.TryGetValue(item, out int existingIndex)
                        || IsDuplicate(existingIndex)
                        || existingIndex != index)
                    {
                        // rebuild notification on inconsistency: original was not found or unique index was incorrect
                        InvalidateMapping(IsDuplicate(existingIndex));
                    }
                    else
                        itemToSortedIndex.Remove(item);
                }
                // removing from the middle: dropping index map because an O(n) traversal would be needed
                else
                    InvalidateMapping(false);
            }

            FireListChanged(ListChangedType.ItemDeleted, index);
        }

        /// <summary>
        /// Removes the first occurrence of <paramref name="item"/> from the <see cref="SortableBindingList{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="SortableBindingList{T}"/>.</param>
        /// <returns><see langword="true"/>, if an occurrence of <paramref name="item" /> was removed; otherwise, <see langword="false"/>.</returns>
        protected override bool RemoveItem(T item)
        {
            if (sortDirection == null)
                return base.RemoveItem(item);

            // if item to index map is not built we do not allow building it because it likely will be invalidated anyway on remove
            int index = GetSortedItemIndex(item, false);
            if (index < 0)
                return false;
            RemoveItemAt(index);
            return true;
        }

        /// <summary>
        /// Removes all elements from the <see cref="SortableBindingList{T}"/>.
        /// </summary>
        /// <remarks>
        /// This method raises the <see cref="FastBindingList{T}.ListChanged" /> event of type <see cref="ListChangedType.Reset" />.
        /// </remarks>
        protected override void ClearItems()
        {
            if (sortDirection != null)
            {
                sortedToBaseIndex!.Reset();
                InvalidateMapping(false);
                sortPending = false;
            }

            base.ClearItems();
        }

        /// <inheritdoc/>
        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if (e == null!)
                Throw.ArgumentNullException(Argument.e);

            if (isChangingOrRaisingChanged)
                return;
            isChangingOrRaisingChanged = true;
            try
            {
                base.OnListChanged(e);
            }
            finally
            {
                isChangingOrRaisingChanged = false;
            }

            if (sortDirection != null && (e.ListChangedType == ListChangedType.ItemAdded
                || (e.ListChangedType == ListChangedType.ItemChanged && (e.PropertyDescriptor == null || e.PropertyDescriptor?.Name == sortProperty?.Name))))
            {
                AdjustPending(e.NewIndex);
                if (sortOnChange && sortPending && e.NewIndex != addNewPos)
                    BuildSortedIndexMap(true, false);
            }
        }

        /// <inheritdoc/>
        protected override void EndNew()
        {
            addNewPos = -1;
            base.EndNew();
        }

        #endregion

        #region Private Protected Methods

        private protected override void ItemPropertyChanged([DisallowNull]T item, int itemIndex, PropertyDescriptor? property)
        {
            base.ItemPropertyChanged(item, itemIndex, property);
            if (sortDirection == null)
                return;

            Debug.Assert((uint)itemIndex < (uint)sortedToBaseIndex!.Count, "itemIndex is a result of GetItemIndex, consistency expected");
            if (property != null && property.Name == sortProperty?.Name)
                sortedToBaseIndex![itemIndex] = (GetBaseIndex(itemIndex), sortProperty == null ? item : sortProperty.GetValue(item));
        }

        #endregion

        #region Private Methods

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx) => sortPropertyName = sortProperty?.Name;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            if (sortDirection == null)
                return;
            sortProperty = sortPropertyName == null ? null : PropertyDescriptors[sortPropertyName];
            DoSort();
            sortPropertyName = null;
        }

        private void DoSort()
        {
            EndNew();
            itemComparer = CreateComparer(sortDirection.GetValueOrDefault() == ListSortDirection.Ascending, sortProperty == null ? typeof(T) : sortProperty.PropertyType);
            BuildSortedIndexMap(true, false);
        }

        private void BuildSortedIndexMap(bool forceSorting, bool buildItemToIndexMap)
        {
            Debug.Assert(forceSorting || sortedToBaseIndex != null, "If index map is null, forceSorting is expected to be true");
            int count = Count;
            bool sortPerformed = false;

            // sortedIndex -> origIndex: when forced (re-sort) or on inconsistency
            if (forceSorting || sortedToBaseIndex != null && sortedToBaseIndex.Count != count)
            {
                if (sortedToBaseIndex == null)
                    sortedToBaseIndex = new CircularList<(int, object?)>(count);
                else
                    sortedToBaseIndex.Clear();

                if (sortProperty == null)
                {
                    for (var i = 0; i < count; i++)
                        sortedToBaseIndex.AddLast((i, base.GetItem(i)));
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        T item = base.GetItem(i);
                        sortedToBaseIndex.AddLast((i, item == null ? null : sortProperty.GetValue(item)));
                    }
                }

                sortedToBaseIndex.Sort(itemComparer!);
                sortPending = false;
                sortPerformed = true;
            }

            // T -> sortedIndex: only when forced (IndexOf is needed)
            if (buildItemToIndexMap)
            {
                if (itemToSortedIndex == null || itemToSortedIndex.Count > 0)
                    itemToSortedIndex = new AllowNullDictionary<T, int>(count);
                for (int i = 0; i < count; i++)
                {
                    T item = GetItemBySortedIndexUnchecked(i);
                    if (!itemToSortedIndex.TryGetValue(item, out int index))
                        itemToSortedIndex[item] = i;
                    else if (!IsDuplicate(index))
                        itemToSortedIndex[item] = ~index;
                }
            }
            // invalidating old item to index mapping
            else if (sortPerformed)
                itemToSortedIndex = null;

            if (sortPerformed)
            {
                // In theory, also Moved could be used, which actually solves the problem of uncommitted edits in WinForms but
                // a.) WPF does not tolerate it b.) IBindingList docs say ApplySort must raise a ListChanged event with the Reset enumeration.
                FireListChanged(ListChangedType.Reset, -1);
                EndNew();
            }
        }

        private int GetSortedItemIndex(T item, bool allowBuildIndexMap)
        {
            if (sortedToBaseIndex!.Count != Count)
                BuildSortedIndexMap(true, allowBuildIndexMap);

            if (itemToSortedIndex == null)
            {
                if (!allowBuildIndexMap)
                    return GetSortedItemIndexSequential(item);

                // if index map is missing, trying to build it without sorting again
                BuildSortedIndexMap(false, true);
            }

            if (!itemToSortedIndex!.TryGetValue(item, out int index))
                return -1;

            if (ConsistencyCheck(index, item))
                return GetActualIndex(index);

            // here there is an inconsistency
            BuildSortedIndexMap(true, allowBuildIndexMap);
            return allowBuildIndexMap
                ? itemToSortedIndex!.TryGetValue(item, out index) ? GetActualIndex(index) : -1
                : GetSortedItemIndexSequential(item);
        }

        private int GetSortedItemIndexSequential(T item)
        {
            int count = Count;
            Debug.Assert(sortedToBaseIndex!.Count == count);

            for (int i = 0; i < count; i++)
            {
                if (Comparer.Equals(GetItemBySortedIndexUnchecked(i), item))
                    return i;
            }

            return -1;
        }

        private int GetBaseIndex(int sortedIndex)
        {
            Debug.Assert(sortedToBaseIndex!.Count == Count);
            return sortedToBaseIndex![sortedIndex].Index;
        }

        private T GetItemBySortedIndex(int index)
        {
            if (sortedToBaseIndex!.Count != Count)
            {
                BuildSortedIndexMap(true, false);
                return GetItemBySortedIndexUnchecked(index);
            }

            T result = GetItemBySortedIndexUnchecked(index);
            if (!ConsistencyCheck(index, result))
                BuildSortedIndexMap(true, false);

            return result;
        }

        private T GetItemBySortedIndexUnchecked(int index) => base.GetItem(GetBaseIndex(index));

        private void AdjustPending(int index)
        {
            if (index >= sortedToBaseIndex!.Count)
            {
                BuildSortedIndexMap(true, false);
                return;
            }

            if (sortPending)
                return;

            // check with previous
            int compareResult;
            if (index > 0)
            {
                compareResult = itemComparer!.Compare(sortedToBaseIndex[index], sortedToBaseIndex[index - 1]);
                if (compareResult != 0)
                {
                    sortPending = sortDirection == ListSortDirection.Ascending && compareResult < 0 || sortDirection == ListSortDirection.Descending && compareResult > 0;
                    if (sortPending)
                        return;
                }
            }

            if (index == Count - 1)
                return;

            // check with next
            compareResult = itemComparer!.Compare(sortedToBaseIndex[index + 1], sortedToBaseIndex[index]);
            if (compareResult != 0)
                sortPending = sortDirection == ListSortDirection.Ascending && compareResult < 0 || sortDirection == ListSortDirection.Descending && compareResult > 0;
        }

        private void InvalidateMapping(bool rebuildSortedIndex)
        {
            if (rebuildSortedIndex)
                BuildSortedIndexMap(true, false);
            else
                itemToSortedIndex = null;
        }

        private bool ConsistencyCheck(int index, T item)
        {
            Debug.Assert(sortDirection != null && sortedToBaseIndex != null);
            if (!CheckConsistency)
                return true;

            if (IsDuplicate(index))
                index = ~index;
            int count = Count;
            return count == sortedToBaseIndex!.Count
                && (uint)index < (uint)count
                && Comparer.Equals(item, GetItemBySortedIndexUnchecked(index));
        }

        #endregion

        #endregion

        #endregion
    }
}
