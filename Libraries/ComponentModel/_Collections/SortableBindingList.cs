#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SortableBindingList.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.ComponentModel;
using System.Linq;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a sortable view for an existing <see cref="IList{T}"/> without reordering the underlying list itself.
    /// </summary>
    /// <typeparam name="T">Type of the contained elements.</typeparam>
    /// TODO:
    /// - Összes member leírása
    /// - DoSort-ban előző/új property is a jó Moved jelentéshez (ha nem jó a Moved, és az item IEditableObject, a bindingsource/grid rosszul használja a begin/end-et)
    /// - ListItem: IComparable{T} támogatása (most csak sima IComparable van a key-re, aztán ToString)
    /// - Add/Insert/Set(this)/Remove-ba LocalChange(...) hívás, ha nem binding(/observable) list van wrappelve - Ez belül hívja az OnListChange-et - így támogatnánk a ListChanged-et akkor is, ha nem bindinglist van belül
    /// - AddNew-ban legyen default akkor is, ha nem bindinglist a belseje
    /// - ObservableCollection támogatás - feliratkozás az ő eseményeire, és hasonló tartalom, mint a BindingList_ListChanged-ben
    /// - ObservableBindingList: ObservableCollection-ből származzon, akár ennek az oszyálynak is lehetne az őse
    public sealed class SortableBindingList<T> : IList<T>, IBindingList, ICancelAddNew
    {
        #region Nested classes

        #region ListItem class

        private class ListItem : IComparable<ListItem>
        {
            #region Properties

            public object Key { get; }
            public int BaseIndex { get; set; }

            #endregion

            #region Constructors

            public ListItem(object key, int baseIndex)
            {
                Key = key;
                BaseIndex = baseIndex;
            }

            #endregion

            #region Methods

            public int CompareTo(ListItem other)
            {
                object target = other.Key;

                if (Key is IComparable comparable)
                    return comparable.CompareTo(target);

                if (Key == null)
                    return target == null ? 0 : 1;

                if (Key.Equals(target))
                    return 0;

                if (target == null)
                    return 1;

                // ReSharper disable once StringCompareToIsCultureSpecific - now this is intended
                return ToString().CompareTo(target.ToString());
            }

            public override string ToString() => Key.ToString();

            #endregion
        }

        #endregion

        #region SortedEnumerator class

        private class SortedEnumerator : IEnumerator<T>
        {
            #region Fields

            private readonly IList<T> list;
            private readonly List<ListItem> sortIndex;
            private readonly ListSortDirection sortOrder;

            private int index;

            #endregion

            #region Properties

            #region Public Properties

            public T Current => list[sortIndex[index].BaseIndex];

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current => list[sortIndex[index].BaseIndex];

            #endregion

            #endregion

            #region Constructors

            public SortedEnumerator(IList<T> list, List<ListItem> sortIndex, ListSortDirection direction)
            {
                this.list = list;
                this.sortIndex = sortIndex;
                sortOrder = direction;
                Reset();
            }

            #endregion

            #region Methods

            #region Public Methods

            public bool MoveNext()
            {
                if (sortOrder == ListSortDirection.Ascending)
                {
                    if (index < sortIndex.Count - 1)
                    {
                        index++;
                        return true;
                    }

                    return false;
                }

                if (index > 0)
                {
                    index--;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                if (sortOrder == ListSortDirection.Ascending)
                    index = -1;
                else
                    index = sortIndex.Count;
            }

            void IDisposable.Dispose()
            {
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private readonly IList<T> list;
        private readonly List<ListItem> sortIndex = new List<ListItem>();

        private bool sorted;
        private PropertyDescriptor sortProperty;
        private ListSortDirection newSortDirection;
        private ListSortDirection? currentSortDirection;
        private bool explicitAddNew;

        #endregion

        #region Events

        /// <summary>
        /// Raised to indicate that the list's data has changed.
        /// </summary>
        /// <remarks>
        /// This event is raised if the underling IList object's data changes
        /// (assuming the underling IList also implements the IBindingList
        /// interface). It is also raised if the sort property or direction
        /// is changed to indicate that the view's data has changed. See
        /// Chapter 5 for details.
        /// </remarks>
        public event ListChangedEventHandler ListChanged;

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Private Properties

        private bool IsBindingList => list is IBindingList;
        private IBindingList AsBindingList => list as IBindingList;

        #endregion

        #region Public Properties

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        public bool AllowEdit => AsBindingList?.AllowEdit ?? false;

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        public bool AllowNew => AsBindingList?.AllowNew ?? false;

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        public bool AllowRemove => AsBindingList?.AllowRemove ?? false;

        /// <summary>
        /// Gets a value indicating whether the view is currently sorted.
        /// </summary>
        public bool IsSorted => sorted;

        /// <summary>
        /// Returns the direction of the current sort.
        /// </summary>
        public ListSortDirection SortDirection => currentSortDirection.GetValueOrDefault();

        /// <summary>
        /// Returns the PropertyDescriptor of the current sort.
        /// </summary>
        public PropertyDescriptor SortProperty => sortProperty;

        /// <summary>
        /// Returns <see langword="true"/> since this object does raise the
        /// ListChanged event.
        /// </summary>
        public bool SupportsChangeNotification => true;

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        public bool SupportsSearching => AsBindingList?.SupportsSearching ?? false;

        /// <summary>
        /// Returns <see langword="true"/>. Sorting is supported.
        /// </summary>
        public bool SupportsSorting => true;

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        public int Count => list.Count;

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        public bool IsReadOnly => list.IsReadOnly;

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => list;

        bool IList.IsFixedSize => false;

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        /// <summary>
        /// Gets the child item at the specified index in the list,
        /// honoring the sort order of the items.
        /// </summary>
        /// <param name="index">The index of the item in the sorted list.</param>
        public T this[int index]
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

        #endregion

        #region Explicitly Implemented Interface Indexers

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new view based on the provided IList object.
        /// </summary>
        /// <param name="list">The IList (collection) containing the data.</param>
        public SortableBindingList(IList<T> list)
        {
            this.list = list;
            if (!(this.list is IBindingList))
                return;
            if (list is IBindingList bindingList)
                bindingList.ListChanged += BindingList_ListChanged;
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Returns an enumerator for the list, honoring
        /// any sort that is active at the time.
        /// </summary>
        public IEnumerator<T> GetEnumerator() => sorted ? new SortedEnumerator(list, sortIndex, SortDirection) : list.GetEnumerator();

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="property">Property on which
        /// to build the index.</param>
        public void AddIndex(PropertyDescriptor property) => AsBindingList?.AddIndex(property);

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        public object AddNew()
        {
            explicitAddNew = true;
            try
            {
                return AsBindingList?.AddNew();
            }
            finally
            {
                explicitAddNew = false;
            }
        }

        /// <summary>
        /// Applies a sort to the view.
        /// </summary>
        /// <param name="property">A PropertyDescriptor for the property on which to sort.</param>
        /// <param name="direction">The direction to sort the data.</param>
        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            sortProperty = property ?? throw new ArgumentNullException(nameof(property));
            newSortDirection = direction;

            DoSort();
        }

        /// <summary>
        /// Applies a sort to the view.
        /// </summary>
        /// <param name="propertyName">Name of the property on which to sort.</param>
        /// <param name="direction">The direction to sort the data.</param>
        public void ApplySort(string propertyName, ListSortDirection direction)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var property = TypeDescriptor.GetProperties(typeof(T))[propertyName];
            if (property == null)
                throw new ArgumentException($"Property {propertyName} not found", propertyName);
            ApplySort(property, direction);
        }

        /// <summary>
        /// Applies a sort to the view based on the items themselves rather than properties.
        /// </summary>
        /// <param name="direction">The direction to sort the data.</param>
        public void ApplySort(ListSortDirection direction)
        {
            sortProperty = null;
            newSortDirection = direction;
            DoSort();
        }

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="key">Key value for which to search.</param>
        /// <param name="property">Property to search for the key
        /// value.</param>
        public int Find(PropertyDescriptor property, object key)
        {
            int originalIndex = AsBindingList?.Find(property, key) ?? -1;
            return originalIndex > -1 ? SortedIndex(originalIndex) : -1;
        }

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="property">Property for which the
        /// index should be removed.</param>
        public void RemoveIndex(PropertyDescriptor property) => AsBindingList?.RemoveIndex(property);

        /// <summary>
        /// Removes any sort currently applied to the view.
        /// </summary>
        public void RemoveSort()
        {
            sortIndex.Clear();
            sortProperty = null;
            currentSortDirection = null;
            sorted = false;

            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="array">Array to receive the data.</param>
        /// <param name="arrayIndex">Starting array index.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            int pos = arrayIndex;
            foreach (T child in this)
            {
                array[pos] = child;
                pos++;
            }
        }

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="item">Item to add to the list.</param>
        public void Add(T item)
        {
            list.Add(item);
            if (sorted && !IsBindingList)
                DoSort();
        }

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        public void Clear() => list.Clear();

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="item">Item for which to search.</param>
        public bool Contains(T item) => list.Contains(item);

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="item">Item for which to search.</param>
        public int IndexOf(T item) => SortedIndex(list.IndexOf(item));

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="index">Index at
        /// which to insert the item.</param>
        /// <param name="item">Item to insert.</param>
        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            if (sorted && !IsBindingList)
                DoSort();
        }

        /// <summary>
        /// Implemented by IList source object.
        /// </summary>
        /// <param name="item">Item to be removed.</param>
        public bool Remove(T item) => list.Remove(item);

        /// <summary>
        /// Removes the child object at the specified index
        /// in the list, resorting the display as needed.
        /// </summary>
        /// <param name="index">The index of the object to remove.</param>
        public void RemoveAt(int index)
        {
            if (sorted)
            {
                int baseIndex = OriginalIndex(index);
                list.RemoveAt(baseIndex);
            }
            else
                list.RemoveAt(index);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Raises the <see cref="ListChanged"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnListChanged(ListChangedEventArgs e) => ListChanged?.Invoke(this, e);

        private void DoSort()
        {
            // TODO: same split for current/new property; otherwise, Moved cannot be reported correctly
            object @null = new object();
            int index = 0;

            int length = Count;
            var origIndexes = new Dictionary<object, int>();
            if (currentSortDirection != null && sortIndex.Count == length)
            {
                for (int i = 0; i < length; i++)
                {
                    origIndexes[sortIndex[i].Key ?? @null] = currentSortDirection == ListSortDirection.Ascending ? i : length - i - 1;
                }
            }

            bool reset = length != sortIndex.Count;
            sortIndex.Clear();
            if (SortProperty == null)
            {
                foreach (T obj in list)
                {
                    sortIndex.Add(new ListItem(obj, index));
                    index++;
                }
            }
            else
            {
                foreach (T obj in list)
                {
                    sortIndex.Add(new ListItem(SortProperty.GetValue(obj), index));
                    index++;
                }
            }

            sortIndex.Sort();
            sorted = true;
            currentSortDirection = newSortDirection;

            // Problems: with ItemMoved:
            // - not just currentSortDirection but also sortProperty should be checked for good results
            // - if there is a binding without property name (whole object), then the elements will be mixed
            if (reset)
            {
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                return;
            }

            isMoving = true;
            for (int i = 0; i < length; i++)
            {
                int oldIndex = origIndexes.TryGetValue(sortIndex[i].Key ?? @null, out int orig) ? orig : sortIndex[i].BaseIndex;
                int newIndex = newSortDirection == ListSortDirection.Ascending ? i : length - i - 1;
                if (oldIndex != newIndex)
                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemMoved, newIndex, oldIndex));

            }
            isMoving = false;
        }

        private bool isMoving;

        private int OriginalIndex(int sortedIndex)
        {
            if (sortedIndex == -1)
                return -1;

            if (sorted)
            {
                // underlying list changed
                if (sortIndex.Count != list.Count)
                    DoSort();

                return SortDirection == ListSortDirection.Ascending ? sortIndex[sortedIndex].BaseIndex : sortIndex[sortIndex.Count - 1 - sortedIndex].BaseIndex;
            }

            return sortedIndex;
        }

        private int SortedIndex(int originalIndex)
        {
            if (originalIndex == -1) return -1;
            int result = 0;
            if (sorted)
            {
                for (int index = 0; index < sortIndex.Count; index++)
                {
                    if (sortIndex[index].BaseIndex == originalIndex)
                    {
                        result = index;
                        break;
                    }
                }
                if (SortDirection == ListSortDirection.Descending)
                    result = sortIndex.Count - 1 - result;
            }
            else
                result = originalIndex;
            return result;
        }

        private int InternalIndex(int originalIndex)
        {
            int result = 0;
            if (sorted)
            {
                for (int index = 0; index < sortIndex.Count; index++)
                {
                    if (sortIndex[index].BaseIndex == originalIndex)
                    {
                        result = index;
                        break;
                    }
                }
            }
            else
                result = originalIndex;
            return result;
        }

        #endregion

        #region Event handlers

        private void BindingList_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (!sorted)
            {
                OnListChanged(e);
                return;
            }

            // sorting if necessary and/or translating the indexes to the consumer
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    T newItem = list[e.NewIndex];
                    var newKey = SortProperty != null ? SortProperty.GetValue(newItem) : newItem;
                    if (SortDirection == ListSortDirection.Ascending)
                        sortIndex.Add(new ListItem(newKey, e.NewIndex));
                    else
                        sortIndex.Insert(0, new ListItem(newKey, e.NewIndex));
                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, SortedIndex(e.NewIndex)));

                    if (!explicitAddNew || e.NewIndex < list.Count - 1)
                        DoSort();

                    break;

                case ListChangedType.ItemChanged:
                    // an item changed - just relay the event with a translated index value
                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, SortedIndex(e.NewIndex), e.PropertyDescriptor));
                    break;

                case ListChangedType.ItemDeleted:
                    var internalIndex = InternalIndex(e.NewIndex);
                    var sortedIndex = internalIndex;
                    if (sorted && SortDirection == ListSortDirection.Descending)
                        sortedIndex = sortIndex.Count - 1 - internalIndex;

                    // remove from internal list
                    sortIndex.RemoveAt(internalIndex);

                    // now fix up all index pointers in the sort index
                    foreach (ListItem item in sortIndex)
                    {
                        if (item.BaseIndex > e.NewIndex)
                            item.BaseIndex -= 1;
                    }

                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, sortedIndex, e.PropertyDescriptor));
                    break;

                default:
                    DoSort();
                    break;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void ICollection.CopyTo(Array array, int index)
        {
            T[] tmp = new T[array.Length];
            CopyTo(tmp, index);
            Array.Copy(tmp, 0, array, index, array.Length);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IList.Add(object value)
        {
            Add((T)value);
            return SortedIndex(list.Count - 1);
        }

        bool IList.Contains(object value) => Contains((T)value);

        int IList.IndexOf(object value) => IndexOf((T)value);

        void IList.Insert(int index, object value) => Insert(index, (T)value);

        void IList.Remove(object value) => Remove((T)value);

        void ICancelAddNew.CancelNew(int itemIndex)
        {
            if (itemIndex < 0)
                return;

            if (list is ICancelAddNew cancelAddNew)
                cancelAddNew.CancelNew(OriginalIndex(itemIndex));
            else
                list.RemoveAt(OriginalIndex(itemIndex));
        }

        void ICancelAddNew.EndNew(int itemIndex)
        {
            if (list is ICancelAddNew cancelAddNew)
                cancelAddNew.EndNew(OriginalIndex(itemIndex));
        }

        #endregion

        #endregion
    }
}
