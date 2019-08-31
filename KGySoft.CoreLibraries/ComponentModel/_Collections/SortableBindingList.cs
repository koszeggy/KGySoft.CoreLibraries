#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SortableBindingList.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    #region Usings

    using SortIndex = KeyValuePair<int, object>;

    #endregion

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
    /// </remarks>
    [Serializable]
    public class SortableBindingList<T> : FastBindingList<T>
    {
        #region Fields

        [NonSerialized] private bool isChangingOrRaisingChanged;
        [NonSerialized] private bool sortPending; // indicates that the list is sorted but contains unsorted elements due to insertion
        [NonSerialized] private PropertyDescriptor sortProperty;
        private string sortPropertyName; // for serialization
        private ListSortDirection? sortDirection;
        [NonSerialized] private CircularList<SortIndex> sortedToBaseIndex; // int is not enough, because contains the evaluated property value can be used for comparison when an item is inserted/changed
        [NonSerialized] private IComparer<SortIndex> itemComparer;
        [NonSerialized] private AllowNullDictionary<T, CircularList<int>> itemToSortedIndex;
        private bool sortOnChange;
        private int addNewPos = -1;
        [NonSerialized] private bool isCancelingNew;

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
                if (sortPending)
                    DoSort();
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
        protected override PropertyDescriptor SortPropertyCore => sortProperty;

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

        private static IComparer<SortIndex> CreateComparer(bool ascending, Type valueType)
        {
            if (valueType.GetInterfaces().Any(i => i.IsGenericTypeOf(typeof(IComparable<>)) && i.GetGenericArguments()[0] == valueType))
                return (IComparer<SortIndex>)Reflector.CreateInstance(typeof(ItemGenericComparer<>).GetGenericType(valueType), ascending);
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
                BuildSortedIndexMap();
        }

        /// <inheritdoc/>
        public override void CancelNew(int itemIndex)
        {
            if (sortDirection != null && sortedToBaseIndex.Count != Count)
            {
                // if there is inconsistency there will be an EndNew instead of cancel because we cannot be sure what to remove
                BuildSortedIndexMap();
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
            if (sortDirection != null && sortedToBaseIndex.Count != Count)
            {
                BuildSortedIndexMap();
                return;
            }

            if (addNewPos < 0 || addNewPos != itemIndex)
                return;

            base.EndNew(sortDirection == null ? itemIndex : GetBaseIndex(itemIndex));
            if (sortDirection != null && sortPending && SortOnChange)
                DoSort();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="SortableBindingList{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}" /> for the <see cref="SortableBindingList{T}"/>.</returns>
        /// <remarks>
        /// <para>If the list is sorted (that is, when <see cref="FastBindingList{T}.IsSorted"/> returns <see langword="true"/>), then the items are returned in the sorted order. Otherwise, the items are returned
        /// in the order as they are stored in the wrapped underlying collection.</para>
        /// <note>The returned enumerator does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public override IEnumerator<T> GetEnumerator()
        {
            int length = Count;
            for (int i = 0; i < length; i++)
                yield return GetItem(i);
        }

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
        protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
        {
            if (!Enum<ListSortDirection>.IsDefined(direction))
                throw new ArgumentOutOfRangeException(nameof(direction), Res.EnumOutOfRangeWithValues(direction));

            sortProperty = property;
            sortDirection = direction;
            DoSort();
        }

        /// <summary>
        /// Removes any sort applied by the <see cref="ApplySortCore">ApplySortCore</see> method.
        /// </summary>
        protected override void RemoveSortCore()
        {
            if (sortProperty == null)
                return;

            EndNew();
            sortedToBaseIndex = null;
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
            SortIndex value = new SortIndex(0, key);
            int lo = 0;
            int hi = Count - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = itemComparer.Compare(sortedToBaseIndex[i], value);

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
            => sortDirection == null ? base.GetItemIndex(item) : GetSortedItemIndex(item);

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

            T original = GetItemBySortedIndex(index);

            // here we can't ignore inconsistency because we need to update the maintained indices
            if (!RemoveIndex(itemToSortedIndex, original, index) || !AddIndex(itemToSortedIndex, item, index))
            {
                BuildSortedIndexMap();
                RemoveIndex(itemToSortedIndex, original, index);
                AddIndex(itemToSortedIndex, item, index);
            }
            else
                sortedToBaseIndex[index] = new SortIndex(sortedToBaseIndex[index].Key, sortProperty == null ? item : sortProperty.GetValue(item));

            int baseIndex = GetBaseIndex(index);
            isChangingOrRaisingChanged = true;
            try
            {
                base.SetItem(baseIndex, item);
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
        [SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "index+1")]
        protected override void InsertItem(int index, T item)
        {
            if (sortDirection == null)
            {
                base.InsertItem(index, item);
                if (IsAddingNew)
                    addNewPos = index;
                return;
            }

            // index refers the sorted list here so in the underlying collection we just add to the last position
            isChangingOrRaisingChanged = true;
            try
            {
                base.InsertItem(Count, item);
            }
            finally
            {
                isChangingOrRaisingChanged = false;
            }

            if (IsAddingNew)
                addNewPos = index;
            int newLength = Count;
            if (newLength != sortedToBaseIndex.Count + 1)
                BuildSortedIndexMap();
            else
            {
                // no need to adjust indices in sortedToBaseIndex because we added the item to the last position in underlying list
                sortedToBaseIndex.Insert(index, new SortIndex(newLength - 1, sortProperty == null ? item : sortProperty.GetValue(item)));
                if (index + 1 < newLength)
                {
                    HashSet<T> adjustedValues = CreateAdjustSet(newLength);
                    for (int i = index + 1; i < newLength; i++)
                    {
                        if (!AdjustIndex(itemToSortedIndex, GetItemBySortedIndexUnchecked(i), index, 1, adjustedValues))
                        {
                            BuildSortedIndexMap();
                            return;
                        }
                    }
                }

                if (!AddIndex(itemToSortedIndex, item, index))
                    BuildSortedIndexMap();
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
        protected override void RemoveItem(int index)
        {
            if (sortDirection == null)
            {
                base.RemoveItem(index);
                return;
            }

            if (isCancelingNew)
            {
                // RemoveItem is now called by the base.CancelNew so index is now the base index and the sorted index is the addNewPos
                if (addNewPos < 0)
                    return;
                index = addNewPos;
            }

            if (Count != sortedToBaseIndex.Count)
            {
                // Possible issue here: in case of inconsistency after the resort the removed element is actually random
                // But if canceling a new element we addNewPos is reset do we must return here.
                BuildSortedIndexMap();
                if (isCancelingNew)
                    return;
            }

            // the order of next lines are important because of possible resorting during consistency check
            T original = GetItemBySortedIndex(index);
            int baseIndex = GetBaseIndex(index);

            isChangingOrRaisingChanged = true;
            try
            {
                base.RemoveItem(baseIndex);
            }
            finally
            {
                isChangingOrRaisingChanged = false;
            }

            // here we can't ignore inconsistency because we need to update the maintained indices
            if (!RemoveIndex(itemToSortedIndex, original, index))
            {
                BuildSortedIndexMap();
                return;
            }

            // adjusting sortedToBaseIndex
            sortedToBaseIndex.RemoveAt(index);
            int length = Count;
            if (baseIndex < length)
            {
                for (int i = 0; i < length; i++)
                {
                    SortIndex sortIndex = sortedToBaseIndex[i];
                    if (sortIndex.Key > baseIndex)
                        sortedToBaseIndex[i] = new SortIndex(sortIndex.Key - 1, sortIndex.Value);
                }
            }

            // adjusting itemToSortedIndex
            if (index < length)
            {
                HashSet<T> adjustedValues = CreateAdjustSet(length);
                for (int i = index; i < length; i++)
                {
                    // Getting as unchecked to avoid possible double rebuild. Count difference were already checked above anyway.
                    if (!AdjustIndex(itemToSortedIndex, GetItemBySortedIndexUnchecked(i), index, -1, adjustedValues))
                    {
                        BuildSortedIndexMap();
                        return;
                    }
                }
            }

            FireListChanged(ListChangedType.ItemDeleted, index);
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
                sortedToBaseIndex.Reset();
                itemToSortedIndex.Clear();
                sortPending = false;
            }

            base.ClearItems();
        }

        /// <inheritdoc/>
        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e), Res.ArgumentNull);

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
                CheckNewItemSorted(e.NewIndex);
                if (sortOnChange && sortPending && e.NewIndex != addNewPos)
                    DoSort();
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

        private protected override void ItemPropertyChanged(T item, int itemIndex, PropertyDescriptor property)
        {
            base.ItemPropertyChanged(item, itemIndex, property);
            if (sortDirection == null)
                return;

            if (property != null && property.Name == sortProperty?.Name)
                sortedToBaseIndex[itemIndex] = new SortIndex(GetBaseIndex(itemIndex), sortProperty == null ? item : sortProperty.GetValue(item));
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
            BuildSortedIndexMap();

            // In theory, also Moved could be used, which actually solves the problem of uncommitted edits in WinForms but
            // a.) WPF does not tolerate it b.) IBindingList docs say ApplySort must raise a ListChanged event with the Reset enumeration.
            FireListChanged(ListChangedType.Reset, -1);
        }

        private void BuildSortedIndexMap()
        {
            // sortedIndex -> origIndex
            if (sortedToBaseIndex == null)
                sortedToBaseIndex = new CircularList<SortIndex>(Count);
            else
                sortedToBaseIndex.Reset();

            int count = Count;
            for (var i = 0; i < count; i++)
            {
                T item = base.GetItem(i);
                sortedToBaseIndex.AddLast(new SortIndex(i, sortProperty == null ? item : sortProperty.GetValue(item)));
            }

            sortedToBaseIndex.Sort(itemComparer);

            // T -> sortedIndex
            if (itemToSortedIndex == null || itemToSortedIndex.Count > 0)
                itemToSortedIndex = new AllowNullDictionary<T, CircularList<int>>();
            int length = Count;
            for (int i = 0; i < length; i++)
            {
                T item = GetItemBySortedIndexUnchecked(i);
                AddIndex(itemToSortedIndex, item, i);
            }

            sortPending = false;
            FireListChanged(ListChangedType.Reset, -1);
            EndNew();
        }

        private int GetSortedItemIndex(T item)
        {
            if (sortedToBaseIndex.Count != Count)
            {
                BuildSortedIndexMap();
                return GetFirstIndex(itemToSortedIndex, item);
            }

            int result = GetFirstIndex(itemToSortedIndex, item);
            if (!CheckConsistency)
                return result;

            int count = Count;
            if (count == sortedToBaseIndex.Count && (result < 0 || result < count && AreEqual(item, GetItemBySortedIndex(result))))
                return result;

            BuildSortedIndexMap();
            return GetFirstIndex(itemToSortedIndex, item);
        }

        private int GetBaseIndex(int sortedIndex) => sortedToBaseIndex[sortedIndex].Key;

        private T GetItemBySortedIndexUnchecked(int index) => base.GetItem(GetBaseIndex(index));

        private T GetItemBySortedIndex(int index)
        {
            if (sortedToBaseIndex.Count != Count)
            {
                BuildSortedIndexMap();
                return GetItemBySortedIndexUnchecked(index);
            }

            T result = GetItemBySortedIndexUnchecked(index);
            if (CheckConsistency && !ContainsIndex(itemToSortedIndex, result, index))
                BuildSortedIndexMap();

            return result;
        }

        private void CheckNewItemSorted(int index)
        {
            if (index >= sortedToBaseIndex.Count)
            {
                BuildSortedIndexMap();
                return;
            }

            if (sortPending)
                return;

            // check with previous
            int compareResult;
            if (index > 0)
            {
                compareResult = itemComparer.Compare(sortedToBaseIndex[index], sortedToBaseIndex[index - 1]);
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
            compareResult = itemComparer.Compare(sortedToBaseIndex[index + 1], sortedToBaseIndex[index]);
            if (compareResult != 0)
                sortPending = sortDirection == ListSortDirection.Ascending && compareResult < 0 || sortDirection == ListSortDirection.Descending && compareResult > 0;
        }

        #endregion

        #endregion

        #endregion
    }
}
