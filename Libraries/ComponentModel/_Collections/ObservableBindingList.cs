using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.Collections.ObjectModel;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a class that combines the features of an <see cref="ObservableCollection{T}"/> and <see cref="BindingList{T}"/>.
    /// If initialized by another <see cref="IBindingList"/> or <see cref="INotifyCollectionChanged"/> implementations captures and delegates also their events.
    /// If initialized by the default constructor will use a <see cref="SortableBindingList{T}"/> inside.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <seealso cref="ObservableCollection{T}" />
    /// <seealso cref="IBindingList" />
    // Changes to ObservableCollection<T>:
    // - PropertyChanged also for other properties
    // - There is no IEnumerable<T> constructor and the IList<T> constructor wraps the original list rather than copying the elements
    // - OnCollectionChanged does not call BlockReentrancy. It is called from the caller methods instead.
    // - Move can thrown NotSupportedException if the underlying list is read-only
    // Changes to BindingList<T>
    // - In case of multiple subscribers of the CollectionChanged and ListChanged events reentrant changes during the event invocation is protected similarly to the ObservableCollection class
    // - IBindingList/ICancelAddNew features (sorting, searching) are supported when the collection passed to the ctor(IList) supports those. SortableBindingList<T> supports sorting, FastBindingList supports searching for example.
    //   With a non-IBindingList list or by the default ctor only AddNew is supported if <copy from FastBindingList>
    // - If the initializer collection is IBindingList or INotifyCollectionChanged their changes are captured and delegated to the self events.
    // - IRaiseItemChangedEvents support: NOT taken from underlying list, RaisesItemChangedEvents returns RaiseItemChangedEvents
    // - IBindingList support:
    //   - AllowNew: get/set (raises PropertyChanged), false if underlying list is IBindingList and returns false or T cannot be created without parameters; otherwise, true by default and can be toggled.
    //   - AllowEdit: get/set (raises PropertyChanged), false if underlying list is IBindingList and returns false; otherwise, !IsReadOnly by default and can be toggled.
    //   - AllowRemove: get/set (raises PropertyChanged), false if underlying list is IBindingList and returns false; otherwise, !IsReadOnly by default and can be toggled.
    //   - AddNew: underlying AddNew if the initializer collection was an IBindingList; or, a new T if can be initialized without parameters; otherwise, throws InvalidOperationException
    //   - Find/Sort: in underlying collection is IBindingList and supports find/sort
    // - ICancelAddNew support: if underlying list is IBindingList and ICancelAddNew, then its implementation; otherwise, commits/remove the last local AddNew item if index is the same
    // General remarks (above all of above):
    // - Disposable implementation: Removing external subscriptions to ListChanged, PropertyChanged and CollectionChanged and self subscriptions to elements PropertyChanged and to the events of the wrapped list
    [Serializable]
    public class ObservableBindingList<T> : Collection<T>, IDisposable,
        INotifyCollectionChanged, INotifyPropertyChanged,
        IBindingList, ICancelAddNew, IRaiseItemChangedEvents
    {
        // TODO: for each:+Local version,+FastBindingList version, embedded list (Old sortable version), Fire events
        // - Ha nem jók az observablecollectionné fordított Remove/Replace eventek a régi elemek nélkül, akkor kell egy int-T dictionary is az elemekkel, amit külön karbantartunk

        private const string indexerName = "Item[]";
        private static readonly bool canAddNew = typeof(T).CanBeCreatedWithoutParameters();
        private static readonly bool canRaiseItemChange = typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T));

        private readonly SimpleMonitor monitor = new SimpleMonitor();
        private bool disposed;
        private bool raiseItemChangedEvents;
        private bool raiseListChangedEvents;
        private bool raiseCollectionChangedEvents;

        private bool allowNew;
        private bool allowEdit;
        private bool allowRemove;
        private int addNewPos = -1;

        [NonSerialized] private bool isAddingNew;
        [NonSerialized] private bool isExplicitChanging; // TODO: nullable enum instead of bool so if on insert/replace/change there comes a sort (reset/move) it can be allowed
        [NonSerialized] private int lastChangeIndex = -1;
        [NonSerialized] private PropertyChangedEventHandler propertyChangedHandler;
        [NonSerialized] private NotifyCollectionChangedEventHandler collectionChangedHandler;
        [NonSerialized] private ListChangedEventHandler listChangedHandler;
        [NonSerialized] private PropertyDescriptorCollection propertyDescriptors;

        private bool IsBindingList => Items is IBindingList;
        private IBindingList AsBindingList => Items as IBindingList;
        private bool HookItemsPropertyChanged => !IsBindingList && canRaiseItemChange;
        private bool IsDualNotifyCollectionType => IsBindingList && Items is INotifyCollectionChanged;

        /// <summary>
        /// Gets or sets whether <see cref="ListChanged"/> and <see cref="CollectionChanged"/> events are invoked with
        /// <see cref="ListChangedType.ItemChanged"/>/<see cref="NotifyCollectionChangedAction.Replace"/> change type when a property of an item changes.
        /// <br/>Default value: <see langword="true"/> if <typeparamref name="T"/> implements <see cref="INotifyPropertyChanged"/>; otherwise, <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>Setting this property to <see langword="false"/> can result in better performance if the underlying list has a poor lookup performance.</para>
        /// <para>This property returns always <see langword="false"/> if <typeparamref name="T"/> does not implement the <see cref="INotifyPropertyChanged"/> interface.</para>
        /// <para><see cref="ListChanged"/> is invoked only if <see cref="RaiseListChangedEvents"/> is <see langword="true"/>; and <see cref="CollectionChanged"/> is raised only
        /// if <see cref="RaiseCollectionChangedEvents"/> is <see langword="true"/>.</para>
        /// </remarks>
        public virtual bool RaiseItemChangedEvents
        {
            get => raiseItemChangedEvents && canRaiseItemChange;
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                if (value == raiseItemChangedEvents)
                    return;
                bool raiseChange = value != RaiseItemChangedEvents;
                raiseItemChangedEvents = value;
                if (raiseChange)
                    FirePropertyChanged();
            }
        }

        public virtual bool AllowNew
        {
            get => allowNew && (AsBindingList?.AllowNew).GetValueOrDefault(canAddNew);
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                if (value == allowNew)
                    return;
                bool raiseChange = value != AllowNew;
                allowNew = value;
                if (raiseChange)
                {
                    FireCollectionReset(false, true);
                    FirePropertyChanged();
                }
            }
        }

        public virtual bool AllowEdit
        {
            get => allowEdit && (AsBindingList?.AllowEdit).GetValueOrDefault(true);
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                if (value == allowEdit)
                    return;
                bool raiseChange = value != AllowEdit;
                allowEdit = value;
                if (raiseChange)
                {
                    FireCollectionReset(false, true);
                    FirePropertyChanged();
                }
            }
        }

        public virtual bool AllowRemove
        {
            get => allowRemove && (AsBindingList?.AllowRemove).GetValueOrDefault(true);
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                if (value == allowRemove)
                    return;
                bool raiseChange = value != AllowRemove;
                allowRemove = value;
                if (raiseChange)
                {
                    FireCollectionReset(false, true);
                    FirePropertyChanged();
                }
            }
        }

        public virtual bool RaiseListChangedEvents
        {
            get => raiseListChangedEvents;
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                if (value == raiseListChangedEvents)
                    return;

                raiseListChangedEvents = value;
                FirePropertyChanged();
            }
        }

        public virtual bool RaiseCollectionChangedEvents
        {
            get => raiseCollectionChangedEvents;
            set
            {
                if (disposed)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                if (value == raiseCollectionChangedEvents)
                    return;

                raiseCollectionChangedEvents = value;
                FirePropertyChanged();
            }
        }

        bool IRaiseItemChangedEvents.RaisesItemChangedEvents => RaiseItemChangedEvents;

        bool IBindingList.SupportsChangeNotification => true;
        bool IBindingList.SupportsSearching => AsBindingList?.SupportsSearching ?? false;
        bool IBindingList.SupportsSorting => AsBindingList?.SupportsSorting ?? false;
        bool IBindingList.IsSorted => AsBindingList?.IsSorted ?? false;
        PropertyDescriptor IBindingList.SortProperty => AsBindingList?.SortProperty;
        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction) => (AsBindingList ?? throw new NotSupportedException(Res.NotSupported)).ApplySort(property, direction);
        int IBindingList.Find(PropertyDescriptor property, object key) => AsBindingList?.Find(property, key) ?? throw new NotSupportedException(Res.NotSupported);
        void IBindingList.AddIndex(PropertyDescriptor property) => AsBindingList?.AddIndex(property);
        void IBindingList.RemoveIndex(PropertyDescriptor property) => AsBindingList?.RemoveIndex(property);
        void IBindingList.RemoveSort() => (AsBindingList ?? throw new NotSupportedException(Res.NotSupported)).RemoveSort();

        ListSortDirection IBindingList.SortDirection => AsBindingList?.SortDirection ?? default;

        public ObservableBindingList() : this(new FastLookupCollection<T>())
        {
        }

        public ObservableBindingList(IList<T> list) : base(list) => Initialize();

        private void Initialize()
        {
            bool readOnly = Items.IsReadOnly;
            allowNew = canAddNew && !readOnly;
            allowRemove = !readOnly;
            allowEdit = Items is IList list ? !list.IsReadOnly : !readOnly; // for editing taking the non-generic IList.IsReadOnly, which is false for fixed size but otherwise writable collections.

            raiseListChangedEvents = true;
            raiseCollectionChangedEvents = true;
            raiseItemChangedEvents = canRaiseItemChange;

            if (Items is IBindingList bindingList)
                bindingList.ListChanged += BindingList_ListChanged;
            if (Items is INotifyCollectionChanged notifyCollectionChanged)
                notifyCollectionChanged.CollectionChanged += NotifyCollectionChanged_CollectionChanged;

            // if list is IBindingList we do not subscribe the elements here but rely on the forwarded events.
            if (!HookItemsPropertyChanged)
                return;

            foreach (T item in Items)
                HookPropertyChanged(item);
        }

        private void NotifyCollectionChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            void HookNewItems(IList newItems)
            {
                if (newItems == null || !HookItemsPropertyChanged)
                    return;

                foreach (object newItem in newItems)
                {
                    if (newItem is T t)
                        HookPropertyChanged(t);
                }
            }

            void UnhookOldItems(IList oldItems)
            {
                if (oldItems == null || !HookItemsPropertyChanged)
                    return;

                foreach (object oldItem in oldItems)
                {
                    if (oldItem is T t)
                        UnhookPropertyChanged(t);
                }
            }

            if (isExplicitChanging)
                return;

            if (e.Action.In(NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Reset))
                EndNew();

            // We can jump out early only if we don't need to maintain item subscriptions
            if (!HookItemsPropertyChanged && !RaiseCollectionChangedEvents && !RaiseListChangedEvents)
                return;

            using (BlockReentrancy())
            {
                FireCollectionChanged(e);
                if (IsDualNotifyCollectionType)
                    return;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems?.Count > 1)
                        {
                            HookNewItems(e.NewItems);
                            FireListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                            break;
                        }

                        if (HookItemsPropertyChanged)
                            HookPropertyChanged(this[e.NewStartingIndex]);
                        FireListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, e.NewStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        UnhookOldItems(e.OldItems);
                        if (e.OldItems?.Count > 1)
                        {
                            FireListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                            break;
                        }

                        FireListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, e.OldStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (!(e.OldItems?.Count == 1 && e.NewItems?.Count == 1 && ReferenceEquals(e.OldItems[0], e.NewItems[0])))
                        {
                            UnhookOldItems(e.OldItems);
                            HookNewItems(e.NewItems);
                        }

                        if (e.NewStartingIndex == e.OldStartingIndex && e.OldItems?.Count == 1 && e.NewItems?.Count == 1)
                            FireListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, e.NewStartingIndex));
                        else
                            FireListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        FireListChanged(new ListChangedEventArgs(ListChangedType.ItemMoved, e.NewStartingIndex, e.OldStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        UnhookOldItems(e.OldItems);
                        FireListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                        break;
                }
            }
        }

        private void BindingList_ListChanged(object sender, ListChangedEventArgs e)
        {
            // we don't maintain item subscriptions here because it is the inner IBindingList's task
            if (isExplicitChanging)
                return;

            if (isAddingNew && e.ListChangedType == ListChangedType.ItemAdded)
                addNewPos = e.NewIndex;
            if (e.ListChangedType.In(ListChangedType.ItemAdded, ListChangedType.ItemDeleted, ListChangedType.Reset))
                EndNew();

            if (!RaiseCollectionChangedEvents && !RaiseListChangedEvents)
                return;

            using (BlockReentrancy())
            {
                if (RaiseListChangedEvents)
                    FireListChanged(e);

                if (!RaiseCollectionChangedEvents || IsDualNotifyCollectionType)
                    return;

                switch (e.ListChangedType)
                {
                    case ListChangedType.ItemAdded:
                        FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this[e.NewIndex], e.NewIndex));
                        break;
                    case ListChangedType.ItemDeleted:
                        // note: after the remove we can't retrieve the old item
                        FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, default(T), e.NewIndex));
                        break;
                    case ListChangedType.ItemMoved:
                        FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, this[e.NewIndex], e.NewIndex, e.OldIndex));
                        break;
                    case ListChangedType.ItemChanged:
                        if (e.PropertyDescriptor != null && !RaiseItemChangedEvents)
                            return;

                        // note: in case of replace we can't retrieve the old item
                        T item = e.NewIndex >= 0 && e.NewIndex < Count ? this[e.NewIndex] : default;
                        FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, e.PropertyDescriptor != null ? item : default(T), e.NewIndex));
                        break;
                    default:
                        FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        break;
                }
            }
        }

        private void HookPropertyChanged(T item)
        {
            if (!(item is INotifyPropertyChanged notifyPropertyChanged))
                return;

            notifyPropertyChanged.PropertyChanged += Item_PropertyChanged;
        }

        private void UnhookPropertyChanged(T item)
        {
            if (!(item is INotifyPropertyChanged notifyPropertyChanged))
                return;

            notifyPropertyChanged.PropertyChanged -= Item_PropertyChanged;
        }

        /// <summary>
        /// Gets the property descriptors of <typeparamref name="T"/>.
        /// </summary>
        protected PropertyDescriptorCollection PropertyDescriptors
            // ReSharper disable once ConstantNullCoalescingCondition - it CAN be null if an ICustomTypeDescriptor implemented so
            => propertyDescriptors ?? (propertyDescriptors = TypeDescriptor.GetProperties(typeof(T)) ?? new PropertyDescriptorCollection(null)); // not static so custom providers can be registered before creating an instance

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!RaiseItemChangedEvents || (!RaiseListChangedEvents && !RaiseCollectionChangedEvents))
                return;

            // Invalid sender or property name: simply resetting
            if (!(sender is T item) || string.IsNullOrEmpty(e?.PropertyName))
            {
                ResetBindings();
                return;
            }

            // in case of a slow IndexOf implementation caching last changed index
            int pos = lastChangeIndex;
            if (pos < 0 || pos >= Count || !this[pos].Equals(item))
            {
                pos = IndexOf(item);
                lastChangeIndex = pos;
            }

            // item was removed but we still receive events
            if (pos < 0)
            {
                UnhookPropertyChanged(item);
                ResetBindings();
            }

            PropertyDescriptor pd = e.PropertyName == null ? null : PropertyDescriptors.Find(e.PropertyName, true);
            FireItemChanged(pos, item, pd);
        }

        public event ListChangedEventHandler ListChanged
        {
            add => listChangedHandler += value;
            remove => listChangedHandler -= value;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => collectionChangedHandler += value;
            remove => collectionChangedHandler -= value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            propertyChangedHandler = null;
            collectionChangedHandler = null;
            listChangedHandler = null;
            if (HookItemsPropertyChanged)
            {
                foreach (T item in Items)
                    UnhookPropertyChanged(item);
            }

            if (Items is IBindingList bindingList)
                bindingList.ListChanged -= BindingList_ListChanged;
            if (Items is INotifyCollectionChanged notifyCollectionChanged)
                notifyCollectionChanged.CollectionChanged -= NotifyCollectionChanged_CollectionChanged;

            (Items as IDisposable)?.Dispose();
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => propertyChangedHandler += value;
            remove => propertyChangedHandler -= value;
        }

        object IBindingList.AddNew() => AddNew();

        public T AddNew()
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            if (!AllowNew)
                throw new InvalidOperationException(Res.ComponentModelAddNewDisabled);

            if (Items is IBindingList bindingList)
            {
                T result;
                isAddingNew = true;
                try
                {
                    result = (T)bindingList.AddNew();
                }
                finally
                {
                    isAddingNew = false;
                }
                return result;
            }

            T newItem = canAddNew ? (T)Reflector.CreateInstance(typeof(T)) : throw new InvalidOperationException(Res.ComponentModelCannotAddNewObservableBindingList(typeof(T)));
            Add(newItem);
            addNewPos = Count - 1;
            return newItem;
        }

        public virtual void CancelNew(int itemIndex)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            if (Items is ICancelAddNew cancelAddNew)
            {
                cancelAddNew.CancelNew(itemIndex);
                EndNew();
                return;
            }

            if (addNewPos < 0 || addNewPos != itemIndex)
                return;
            RemoveItem(addNewPos);
        }

        public virtual void EndNew(int itemIndex)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);

            if (Items is ICancelAddNew cancelAddNew)
            {
                cancelAddNew.EndNew(itemIndex);
                EndNew();
                return;
            }

            // inside from this class this should be called to make sure the index is not sorted.
            if (addNewPos >= 0 && addNewPos == itemIndex)
                EndNew();
        }

        private void EndNew() => addNewPos = -1;

        protected override void SetItem(int index, T item)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);

            T originalItem = this[index];
            if (ReferenceEquals(originalItem, item))
                return;

            CheckReentrancy();

            if (HookItemsPropertyChanged)
                UnhookPropertyChanged(originalItem);

            isExplicitChanging = true;
            try
            {
                base.SetItem(index, item);
            }
            finally
            {
                isExplicitChanging = false;
            }

            if (HookItemsPropertyChanged)
                HookPropertyChanged(item);

            FireItemReplace(index, originalItem, item);
        }

        protected override void InsertItem(int index, T item)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);

            CheckReentrancy();
            EndNew();
            isExplicitChanging = true;
            try
            {
                base.InsertItem(index, item);
            }
            finally
            {
                isExplicitChanging = false;
            }

            if (HookItemsPropertyChanged)
                HookPropertyChanged(item);

            FireItemAdded(index, item);
        }

        protected override void RemoveItem(int index)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);

            CheckReentrancy();

            // even if remove not allowed we can remove the element being just added and yet uncommitted
            if (!allowRemove && !(addNewPos >= 0 && addNewPos == index))
                throw new InvalidOperationException(Res.ComponentModelRemoveDisabled);

            EndNew();
            T removedItem = this[index];
            if (HookItemsPropertyChanged)
                UnhookPropertyChanged(removedItem);

            isExplicitChanging = true;
            try
            {
                base.RemoveItem(index);
            }
            finally
            {
                isExplicitChanging = false;
            }

            FireItemRemoved(index, removedItem);
        }

        protected override void ClearItems()
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);

            CheckReentrancy();

            if (Count == 0)
                return;

            // even if remove not allowed we can remove the element being just added and yet uncommitted
            if (!allowRemove && !(addNewPos == 0 && Count == 1))
                throw new InvalidOperationException(Res.ComponentModelRemoveDisabled);

            EndNew();
            if (HookItemsPropertyChanged)
            {
                foreach (T item in Items)
                    UnhookPropertyChanged(item);
            }

            isExplicitChanging = true;
            try
            {
                base.ClearItems();
            }
            finally
            {
                isExplicitChanging = false;
            }

            FireCollectionReset(true, false);
        }

        public void Move(int oldIndex, int newIndex)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            if (oldIndex < 0 || oldIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(oldIndex), Res.ArgumentOutOfRange);
            if (newIndex < 0 || newIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(newIndex), Res.ArgumentOutOfRange);
            MoveItem(oldIndex, newIndex);
        }

        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);

            CheckReentrancy();
            EndNew();
            T removedItem = this[oldIndex];

            isExplicitChanging = true;
            try
            {
                base.RemoveItem(oldIndex);
                base.InsertItem(newIndex, removedItem);
            }
            finally
            {
                isExplicitChanging = false;
            }

            FireItemMoved(removedItem, newIndex, oldIndex);
        }

        private void FireItemReplace(int index, T oldItem, T newItem)
        {
            NotifyCollectionChangedEventHandler handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, index));
                FireListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
            }

            FirePropertyChanged(indexerName);
        }

        private void FireItemChanged(int index, T item, PropertyDescriptor pd)
        {
            if (!RaiseItemChangedEvents)
                return;
            NotifyCollectionChangedEventHandler handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item, index));
                FireListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index, pd));
            }

            FirePropertyChanged(indexerName);
        }

        private void FireItemAdded(int index, T item)
        {
            NotifyCollectionChangedEventHandler handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                FireListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
            }

            FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
        }

        private void FireItemRemoved(int index, T item)
        {
            NotifyCollectionChangedEventHandler handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                FireListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
            }

            FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
        }

        private void FireItemMoved(T item, int newIndex, int oldIndex)
        {
            NotifyCollectionChangedEventHandler handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
                FireListChanged(new ListChangedEventArgs(ListChangedType.ItemMoved, newIndex, oldIndex));
            }

            FirePropertyChanged(indexerName);
        }

        private void FireCollectionReset(bool countChanged, bool listChangeOnly)
        {
            NotifyCollectionChangedEventHandler handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                if (!listChangeOnly)
                    FireCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                FireListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }

            if (countChanged)
                FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
        }

        public void ResetBindings()
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            FireCollectionReset(false, false);
        }

        public void ResetItem(int position)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            FireItemChanged(position, this[position], null);
        }

        private void FireCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (RaiseCollectionChangedEvents)
                collectionChangedHandler?.Invoke(this, e);
        }

        private void FireListChanged(ListChangedEventArgs e)
        {
            if (RaiseListChangedEvents)
                listChangedHandler?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => propertyChangedHandler?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        /// <remarks>
        /// When overriding this method, you can call the <see cref="BlockReentrancy"/> to guard against reentrant collection changes.
        /// </remarks>
        protected virtual void xOnCollectionChanged(NotifyCollectionChangedEventArgs e) => collectionChangedHandler?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="ListChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="ListChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void xOnListChanged(ListChangedEventArgs e) => listChangedHandler?.Invoke(this, e);

        /// <summary>
        /// Disallow reentrant attempts to change this collection. E.g. a event handler
        /// of the CollectionChanged event is not allowed to make changes to this collection.
        /// </summary>
        /// <remarks>
        /// typical usage is to wrap e.g. a OnCollectionChanged call with a using() scope:
        /// <code>
        ///         using (BlockReentrancy())
        ///         {
        ///             CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item, index));
        ///         }
        /// </code>
        /// </remarks>
        protected IDisposable BlockReentrancy()
        {
            monitor.Enter();
            return monitor;
        }

        /// <summary> Check and assert for reentrant attempts to change this collection. </summary>
        /// <exception cref="InvalidOperationException"> raised when changing the collection
        /// while another collection change is still being notified to other listeners </exception>
        protected void CheckReentrancy()
        {
            if (monitor.Busy)
            {
                // we can allow changes if there's only one listener
                if ((collectionChangedHandler?.GetInvocationList().Length ?? 0)
                    + (listChangedHandler?.GetInvocationList().Length ?? 0) > 1)
                    throw new InvalidOperationException(Res.ComponentModelReentrancyNotAllowed);
            }
        }

        [NotifyPropertyChangedInvocator]
        private void FirePropertyChanged([CallerMemberName] string propertyName = null) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));


        #region Private Types

        // this class helps prevent reentrant calls
        [Serializable]
        private class SimpleMonitor : IDisposable
        {
            public void Enter() => ++busyCount;

            public void Dispose() => --busyCount;

            public bool Busy => busyCount > 0;

            private int busyCount;
        }

        #endregion Private Types

    }
}
