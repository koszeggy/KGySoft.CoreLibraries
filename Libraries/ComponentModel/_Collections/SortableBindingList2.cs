using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KGySoft.Collections;
using KGySoft.Libraries;
using KGySoft.Reflection;

namespace KGySoft.ComponentModel
{
    // Compatible with BindingList<T> but allows sorting (with not reordering the underlying elements) and allows turning on/off not just list change events but also element change events.
    // Turning off element change events makes the list scalable (makes performance similar to ObservableCollection)
    // New features:
    // - Disposable: removes both incoming (self events) and outgoing (elements PropertyChanged) subscriptions
    // - RaiseListChangedEvents is virtual
    // - RaiseItemChangedEvents
    // - AllowEdit/Remove/New - properties are virtual
    // Changes to BindingList<T>:
    // - AllowEdit/Remove/New - initialized by IsReadOnly
    // - AddNewCore returns T instead of object; throws InvalidOperationException if AllowNew is true but cannot add new item without event or override
    // - Type of AddingNew event is EventHandler<AddingNewEventArgs<T>> instead of AddingNewEventHandler
    // - If AddingNew does not create an item of T t
    // - Return value of AllowNew does not depend on whether AddingNew is subscribed. It must be set explicitly if we want to allow new events.
    [Serializable]
    class SortableBindingList2<T> : FastBindingList<T>
    {
        private struct SortIndex
        {
            internal object Value { get; }
            internal int OrigIndex { get; }

            internal SortIndex(object value, int origIndex)
            {
                Value = value;
                OrigIndex = origIndex;
            }
        }

        //private static readonly object @null = new object();
        private bool sorted;
        private PropertyDescriptor sortProperty;
        private ListSortDirection? sortDirection;
        //private List<int> origToSortedIndex;
        //private Dictionary<T, int> itemToOrigIndex;
        //private Dictionary<T, int> itemToSortedIndex;

        protected override bool SupportsSortingCore => true;
        public bool IsSorted => sorted;
        protected override PropertyDescriptor SortPropertyCore => sortProperty;
        protected override ListSortDirection SortDirectionCore => sortDirection.GetValueOrDefault();

        protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
        {
            if (property != null && !PropertyDescriptors.Contains(property))
                throw new ArgumentException(Res.SortableBindingListInvalidProperty(property, typeof(T)), nameof(property));

            if (!Enum<ListSortDirection>.IsDefined(direction))
                throw new ArgumentOutOfRangeException(nameof(direction), Res.EnumOutOfRange(direction));

            sortProperty = null;
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

        private CircularList<SortIndex> sortedToOrigIndex; // int is not enough, because contains the evaluated property value can be used for comparison when an item is inserted/changed
        private IComparer<SortIndex> itemComparer;

        private sealed class ItemComparer : IComparer<SortIndex>
        {
            private readonly bool ascending;

            public ItemComparer(bool ascending) => this.ascending = ascending;

            public int Compare(SortIndex x, SortIndex y)
            {
                int sign = ascending ? 1 : -1;

                if (x.Value == null)
                    return y.Value == null ? 0 : sign;
                if (x.Value.Equals(y.Value))
                    return 0;

                if (x.Value is IComparable comparable)
                    return sign * comparable.CompareTo(y.Value);

                // ReSharper disable once StringCompareToIsCultureSpecific - now this is intended
                return sign * x.Value.ToString().CompareTo(y.Value?.ToString());
            }
        }

        private sealed class ItemGenericComparer<TElement> : IComparer<SortIndex>
        {
            private readonly bool ascending;

            public ItemGenericComparer(bool ascending) => this.ascending = ascending;

            public int Compare(SortIndex x, SortIndex y)
            {
                int sign = ascending ? 1 : -1;

                if (x.Value == null)
                    return y.Value == null ? 0 : sign;
                return sign * ((IComparable<TElement>)x.Value).CompareTo((TElement)y.Value);
            }
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
            // reset, ha volt rendezve, de nem talaljuk a regi elemeket

            if (sortedToOrigIndex == null)
                sortedToOrigIndex = new CircularList<SortIndex>(Count);
            else
                sortedToOrigIndex.Reset();

            for (var i = 0; i < Items.Count; i++)
            {
                T item = Items[i];
                sortedToOrigIndex.AddLast(new SortIndex(sortProperty == null ? item : sortProperty.GetValue(item), i));
            }

            itemComparer = CreateComparer(sortDirection.GetValueOrDefault() == ListSortDirection.Ascending, sortProperty == null ? typeof(T) : sortProperty.PropertyType);
            sortedToOrigIndex.Sort(itemComparer);

            sorted = true;
            FireListChanged(ListChangedType.Reset, -1);
        }

        public override IEnumerator<T> GetEnumerator() => sorted ? new SortedEnumerator(list, sortIndex, SortDirection) : list.GetEnumerator();

        #region IList[<T>] hacks

        public new int IndexOf(T item) => SortedIndex(list.IndexOf(item));

        public new T this[int index]
        {
            get => sorted ? list[OriginalIndex(index)] : list[index];
            set
            {
                if (sorted)
                {
                    if (isMoving) // if the list is bound to an object without a specific property name (eg. to a PropertyGrid.SelectedObject a whole element is bound), then on moving the currency manager tries to set the elements back.
                        return;
                    list[OriginalIndex(index)] = value;
                    if (!IsBindingList)
                        DoSort();
                }
                else
                    list[index] = value;
            }
        }

        T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        int IList<T>.IndexOf(T item) => SortedIndex(list.IndexOf(item));


        int IList.Add(object value)
        {
            Add((T)value);
            return SortedIndex(Count - 1);
        }


        int IList.IndexOf(object value) => SortedIndex(list.IndexOf(item));

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        #endregion

    }
}
