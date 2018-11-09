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
        [NonSerialized] private bool isAddingNew;
        [NonSerialized] private bool sortPending; // indicates that the list is sorted but contains unsorted elements due to insertion
        [NonSerialized] private PropertyDescriptor sortProperty;
        private string sortPropertyName; // for serialization
        private ListSortDirection? sortDirection;
        [NonSerialized] private CircularList<SortIndex> sortedToBaseIndex; // int is not enough, because contains the evaluated property value can be used for comparison when an item is inserted/changed
        [NonSerialized] private IComparer<SortIndex> itemComparer;
        [NonSerialized] private AllowNullDictionary<T, CircularList<int>> itemToSortedIndex = new AllowNullDictionary<T, CircularList<int>>();

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => sortDirection != null;
        protected override PropertyDescriptor SortPropertyCore => sortProperty;
        protected override ListSortDirection SortDirectionCore => sortDirection.GetValueOrDefault();

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
            sortProperty = null;
            sortDirection = null;

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
            // TODO: original list
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
            if (itemToSortedIndex.Count > 0)
                itemToSortedIndex = new AllowNullDictionary<T, CircularList<int>>();
            int length = Count;
            for (int i = 0; i < length; i++)
            {
                T item = GetItemBySortedIndexUnchecked(i);
                AddIndex(itemToSortedIndex, item, i);
            }

            sortPending = false;

            if (reset)
                FireListChanged(ListChangedType.Reset, -1);
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

        private T GetItemBySortedIndexUnchecked(int index) => Items[sortedToBaseIndex[index].Key];

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

            if (CheckConsistency && !ContainsIndex(itemToSortedIndex, item, index))
                BuildSortedIndexMap(true);
            T original = GetItemBySortedIndex(index);

            if (!AreEqual(original, item) && (!RemoveIndex(itemToSortedIndex, original, index) || !AddIndex(itemToSortedIndex, item, index)))
                BuildSortedIndexMap(true);
            else
                sortedToBaseIndex[index] = new SortIndex(sortedToBaseIndex[index].Key, sortProperty == null ? item : sortProperty.GetValue(item));

            isChanging = true;
            try
            {
                base.SetItem(sortedToBaseIndex[index].Key, item);
            }
            finally
            {
                isChanging = false;
            }

            FireListChanged(ListChangedType.ItemChanged, index);
        }

        protected override T AddNewCore()
        {
            isAddingNew = true;
            try
            {
                return base.AddNewCore();
            }
            finally
            {
                isAddingNew = false;
            }
        }

        protected override void InsertItem(int index, T item)
        {
            if (sortDirection == null)
            {
                base.InsertItem(index, item);
                return;
            }

            isChanging = true;
            try
            {
                base.InsertItem(index, item);
                int last = sortedToBaseIndex.Count;
                sortedToBaseIndex.AddLast(new SortIndex(index, sortProperty == null ? item : sortProperty.GetValue(item)));
                int compareResult = last == 0 ? 0 : itemComparer.Compare(sortedToBaseIndex[last], sortedToBaseIndex[last - 1]);
                if (!sortPending && compareResult != 0)
                    sortPending = sortDirection == ListSortDirection.Ascending && compareResult < 0 || sortDirection == ListSortDirection.Descending && compareResult > 0;
                // TODO: AddIndex
#error itt
            }
            finally
            {
                isChanging = false;
            }

            FireListChanged(ListChangedType.ItemAdded, Count - 1);
            if (sortPending && !isAddingNew)
                DoSort();
        }

        // TODO RemoveItem
        // TODO ClearItems

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if (isChanging)
                return;
            base.OnListChanged(e);
        }

        // TODO: CancelNew
        // TODO: EndNew

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
