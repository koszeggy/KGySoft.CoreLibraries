using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using KGySoft.Collections;
using KGySoft.Libraries;
using KGySoft.Reflection;

namespace KGySoft.ComponentModel
{
    using SortIndex = KeyValuePair<int, object>;

    // Compatible with BindingList<T> but allows sorting (with not reordering the underlying elements) and allows turning on/off not just list change events but also element change events.
    // Turning off element change events makes the list scalable (makes performance similar to ObservableCollection)
    [Serializable]
    public class SortableBindingList<T> : FastBindingList<T>
    {
        //private static readonly object @null = new object();
        private bool sorted;
        private PropertyDescriptor sortProperty;
        private ListSortDirection? sortDirection;
        private CircularList<SortIndex> sortedToOrigIndex; // int is not enough, because contains the evaluated property value can be used for comparison when an item is inserted/changed
        private IComparer<SortIndex> itemComparer;
        //private List<int> origToSortedIndex;
        //private Dictionary<T, int> itemToSortedIndex;

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => sorted;
        protected override PropertyDescriptor SortPropertyCore => sortProperty;
        protected override ListSortDirection SortDirectionCore => sortDirection.GetValueOrDefault();

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
            sortedToOrigIndex = null;
            sortProperty = null;
            sortDirection = null;
            sorted = false;

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

            if (sortedToOrigIndex == null)
                sortedToOrigIndex = new CircularList<SortIndex>(Count);
            else
                sortedToOrigIndex.Reset();

            for (var i = 0; i < Items.Count; i++)
            {
                T item = Items[i];
                sortedToOrigIndex.AddLast(new SortIndex(i, sortProperty == null ? item : sortProperty.GetValue(item)));
            }

            itemComparer = CreateComparer(sortDirection.GetValueOrDefault() == ListSortDirection.Ascending, sortProperty == null ? typeof(T) : sortProperty.PropertyType);
            sortedToOrigIndex.Sort(itemComparer);

            // TODO: build itemToSortedIndex

            sorted = true;
            FireListChanged(ListChangedType.Reset, -1);
        }

        private bool isInserting;
        protected override void InsertItem(int index, T item)
        {
            if (sorted)
            {
                // TODO: test without isAddingNewCore (but with correct sorted index in GetItemIndex)
                // if dgv fails to work, then forcing to be the last item when isAddingNewCore

                isInserting = true;
                // TODO: - here adding new index to every dict (see also FLC.InsertItem, AdjustIndex, AddIndex)
                // - if inserting, suppress OnListChanged
            }
            base.InsertItem(index, item); // todo invokes OnListChanged
            if (sorted)
                DoSort();

            // todo: finally
            if (isInserting)
            {
                isInserting = false;
                FireListChanged(ListChangedType.ItemAdded, index);
            }
        }

        protected override T GetItem(int index) 
            => base.GetItem(sorted ? sortedToOrigIndex[index].Key : index);

        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, item);
            if (sorted)
            {
                //if (isMoving) // if the list is bound to an object without a specific property name (eg. to a PropertyGrid.SelectedObject a whole element is bound), then on moving the currency manager tries to set the elements back.
                //    return;
                //list[OriginalIndex(index)] = value;
                DoSort();
            }
        }

        protected override int GetItemIndex(T item)
        {
            //TODO
            //if (!sorted)
                return base.GetItemIndex(item);

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
