using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using KGySoft.Collections;
using KGySoft.Libraries;
using KGySoft.Reflection;

namespace KGySoft.ComponentModel
{
    using SortIndex = KeyValuePair<int, object>;

    // Compatible with BindingList<T> but allows sorting (with not reordering the underlying elements) and allows turning on/off not just list change events but also element change events.
    // Turning off element change events makes the list scalable (makes performance similar to ObservableCollection)
    // Differences to FastBindingList:
    // - Add/Insert adds to the required position and then sorts immediately, except when AddNew is called (can be followed by Moved events)
    [Serializable]
    public class SortableBindingList<T> : FastBindingList<T>
    {
        [NonSerialized] private bool isChanging;
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

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => sortDirection != null;
        protected override PropertyDescriptor SortPropertyCore => sortProperty;
        protected override ListSortDirection SortDirectionCore => sortDirection.GetValueOrDefault();

        /// <summary>
        /// Gets or sets whether the <see cref="SortableBindingList{T}"/> should be immediately re-sorted when an item changes or a new item is added.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks><para>Setting this property to <see langword="true"/> may cause re-sorting the <see cref="SortableBindingList{T}"/> immediately.</para></remarks>
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

        protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
        {
            if (property != null && !PropertyDescriptors.Contains(property))
                throw new ArgumentException(Res.SortableBindingListInvalidProperty(property, typeof(T)), nameof(property));

            if (!Enum<ListSortDirection>.IsDefined(direction))
                throw new ArgumentOutOfRangeException(nameof(direction), Res.EnumOutOfRange(direction));

            sortProperty = property;
            sortDirection = direction;
            DoSort();
        }

        protected override void RemoveSortCore()
        {
            sortedToBaseIndex = null;
            itemToSortedIndex = null;
            sortProperty = null;
            sortDirection = null;
            sortPending = false;
            FireListChanged(ListChangedType.Reset, -1);
        }

        private static IComparer<SortIndex> CreateComparer(bool ascending, Type valueType)
        {
            if (valueType.GetInterfaces().Any(i => i.IsGenericTypeOf(typeof(IComparable<>)) && i.GetGenericArguments()[0] == valueType))
                return (IComparer<SortIndex>)Reflector.Construct(typeof(ItemGenericComparer<>).MakeGenericType(valueType), ascending);
            return new ItemComparer(ascending);
        }

        private void DoSort()
        {
#error itt
            // TODO: original list + addNewPos
            // reset if it was sorted but we don't find the old elements

            itemComparer = CreateComparer(sortDirection.GetValueOrDefault() == ListSortDirection.Ascending, sortProperty == null ? typeof(T) : sortProperty.PropertyType);

            BuildSortedIndexMap(false);

            // TODO: Move if possible
            FireListChanged(ListChangedType.Reset, -1);
        }

        private void BuildSortedIndexMap(bool reset)
        {
            // sortedIndex -> origIndex
            if (sortedToBaseIndex == null)
                sortedToBaseIndex = new CircularList<SortIndex>(Count);
            else
                sortedToBaseIndex.Reset();

            for (var i = 0; i < Items.Count; i++)
            {
                T item = Items[i];
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

            if (reset)
            {
                FireListChanged(ListChangedType.Reset, -1);
                EndNew();
            }
        }

        public override void InnerListChanged()
        {
            base.InnerListChanged();
            if (sortDirection != null)
                BuildSortedIndexMap(true);
        }

        protected override int GetItemIndex(T item)
            => sortDirection == null ? base.GetItemIndex(item) : GetSortedItemIndex(item);

        private int GetSortedItemIndex(T item)
        {
            if (sortedToBaseIndex.Count != Count)
            {
                BuildSortedIndexMap(true);
                return GetFirstIndex(itemToSortedIndex, item);
            }

            int result = GetFirstIndex(itemToSortedIndex, item);
            if (!CheckConsistency)
                return result;

            int count = Count;
            if (count == sortedToBaseIndex.Count && (result < 0 || result < count && AreEqual(item, GetItemBySortedIndex(result))))
                return result;

            BuildSortedIndexMap(true);
            return GetFirstIndex(itemToSortedIndex, item);
        }

        protected override T GetItem(int index)
            => sortDirection == null ? base.GetItem(index) : GetItemBySortedIndex(index);

        private int GetBaseIndex(int sortedIndex) => sortedToBaseIndex[sortedIndex].Key;

        private T GetItemBySortedIndexUnchecked(int index) => Items[GetBaseIndex(index)];

        private T GetItemBySortedIndex(int index)
        {
            if (sortedToBaseIndex.Count != Count)
            {
                BuildSortedIndexMap(true);
                return GetItemBySortedIndexUnchecked(index);
            }

            T result = GetItemBySortedIndexUnchecked(index);
            if (CheckConsistency && !ContainsIndex(itemToSortedIndex, result, index))
                BuildSortedIndexMap(true);

            return result;
        }

        protected override void SetItem(int index, T item)
        {
            if (sortDirection == null)
            {
                base.SetItem(index, item);
                return;
            }

            if (Count != sortedToBaseIndex.Count || CheckConsistency && !ContainsIndex(itemToSortedIndex, item, index))
                BuildSortedIndexMap(true);

            T original = GetItemBySortedIndex(index);
            if (ReferenceEquals(original, item))
                return;

            // here we can't ignore inconsistency because we need to update the maintained indices
            if (!RemoveIndex(itemToSortedIndex, original, index) || !AddIndex(itemToSortedIndex, item, index))
            {
                BuildSortedIndexMap(true);
                RemoveIndex(itemToSortedIndex, original, index);
                AddIndex(itemToSortedIndex, item, index);
            }

            int baseIndex = GetBaseIndex(index);
            isChanging = true;
            try
            {
                base.SetItem(baseIndex, item);
            }
            finally
            {
                isChanging = false;
            }

            FireListChanged(ListChangedType.ItemChanged, index);
        }

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
            isChanging = true;
            try
            {
                base.InsertItem(Count, item);
            }
            finally
            {
                isChanging = false;
            }

            if (IsAddingNew)
                addNewPos = index;
            int newLength = Count;
            if (newLength != sortedToBaseIndex.Count + 1)
                BuildSortedIndexMap(true);
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
                            BuildSortedIndexMap(true);
                            break;
                        }
                    }
                }

                if (!AddIndex(itemToSortedIndex, item, index))
                    BuildSortedIndexMap(true);
            }

            FireListChanged(ListChangedType.ItemAdded, index);
        }

        private void CheckNewItemSorted(int index)
        {
            if (index >= sortedToBaseIndex.Count)
            {
                BuildSortedIndexMap(true);
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
                BuildSortedIndexMap(true);
                if (isCancelingNew)
                    return;
            }

            // the order of next lines are important because of possible resorting during consistency check
            T original = GetItemBySortedIndex(index);
            int baseIndex = GetBaseIndex(index);

            isChanging = true;
            try
            {
                base.RemoveItem(baseIndex);
            }
            finally
            {
                isChanging = false;
            }

            // here we can't ignore inconsistency because we need to update the maintained indices
            if (!RemoveIndex(itemToSortedIndex, original, index))
            {
                BuildSortedIndexMap(true);
                if (isCancelingNew)
                     return;
            }
            else
            {
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
                        if (!AdjustIndex(itemToSortedIndex, GetItemBySortedIndex(i), index, -1, adjustedValues))
                        {
                            BuildSortedIndexMap(true);
                            break;
                        }
                    }
                }
            }

            FireListChanged(ListChangedType.ItemDeleted, index);
        }

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

        protected override void ItemPropertyChanged(T item, int itemIndex, PropertyDescriptor property)
        {
            base.ItemPropertyChanged(item, itemIndex, property);
            if (sortDirection == null)
                return;

            if (property != null && property.Name == sortProperty?.Name)
                sortedToBaseIndex[itemIndex] = new SortIndex(GetBaseIndex(itemIndex), sortProperty == null ? item : sortProperty.GetValue(item));
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if (isChanging)
                return;
            base.OnListChanged(e);
            if (sortDirection != null && (e.ListChangedType == ListChangedType.ItemAdded || (e.ListChangedType == ListChangedType.ItemChanged && e.PropertyDescriptor?.Name == sortProperty?.Name)))
            {
                CheckNewItemSorted(e.NewIndex);
                if (sortOnChange && sortPending && e.NewIndex != addNewPos)
                    DoSort();
            }
        }

        public override void CancelNew(int itemIndex)
        {
            if (sortDirection != null && sortedToBaseIndex.Count != Count)
            {
                // if there is inconsistency there will be an EndNew instead of cancel because we cannot be sure what to remove
                BuildSortedIndexMap(true);
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

        public override void EndNew(int itemIndex)
        {
            if (sortDirection != null && sortedToBaseIndex.Count != Count)
            {
                BuildSortedIndexMap(true);
                return;
            }

            if (addNewPos < 0 || addNewPos != itemIndex)
                return;

            base.EndNew(sortDirection == null ? itemIndex : GetBaseIndex(itemIndex));
            if (sortDirection != null && sortPending)
                DoSort();
        }

        protected override void EndNew()
        {
            addNewPos = -1;
            base.EndNew();
        }

        public override IEnumerator<T> GetEnumerator()
        {
            int length = Count;
            for (int i = 0; i < length; i++)
                yield return GetItem(i);
        }

        //public override IEnumerator<T> GetEnumerator() => sorted ? new SortedEnumerator(list, sortIndex, SortDirection) : list.GetEnumerator();

        //#region IList[<T>] hacks

        //public new int IndexOf(T item) => SortedIndex(list.IndexOf(item));

        //public new T this[int index]
        //{
        //    get => sorted ? list[OriginalIndex(index)] : list[index];
        //    set
        //    {
        //        if (sorted)
        //        {
        //            if (isMoving) // if the list is bound to an object without a specific property name (eg. to a PropertyGrid.SelectedObject a whole element is bound), then on moving the currency manager tries to set the elements back.
        //                return;
        //            list[OriginalIndex(index)] = value;
        //            if (!IsBindingList)
        //                DoSort();
        //        }
        //        else
        //            list[index] = value;
        //    }
        //}

        //T IList<T>.this[int index]
        //{
        //    get => this[index];
        //    set => this[index] = value;
        //}

        //int IList<T>.IndexOf(T item) => SortedIndex(list.IndexOf(item));


        //int IList.Add(object value)
        //{
        //    Add((T)value);
        //    return SortedIndex(Count - 1);
        //}


        //int IList.IndexOf(object value) => SortedIndex(list.IndexOf(item));

        //object IList.this[int index]
        //{
        //    get => this[index];
        //    set => this[index] = (T)value;
        //}

        //#endregion
    }
}
