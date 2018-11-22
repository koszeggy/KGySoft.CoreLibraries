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
using KGySoft.Libraries;
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
    // - ICancelAddNew support: if underlying list is IBindingList and ICancelAddNew, then its implementation; otherwise, commits/remove the last local AddNew item
    // General remarks (above all of above):
    // - Disposable implementation: Removing external subscriptions to ListChanged, PropertyChanged and CollectionChanged and self subscriptions to elements PropertyChanged and to the events of the wrapped list
    [Serializable]
    public class ObservableBindingList<T> : Collection<T>, IDisposable,
        INotifyCollectionChanged, INotifyPropertyChanged,
        IBindingList, ICancelAddNew, IRaiseItemChangedEvents
    {
        // TODO: for each: Local version, FastBindingList version, embedded list (Old sortable version), Fire events
        // - SetItem: Local version, FastBindingList version, embedded list (Old sortable version), Fire events
        // - InsertItem: Local version, FastBindingList version, embedded list (Old sortable version), Fire events
        // - RemoveItem: Local version, FastBindingList version, embedded list (Old sortable version), Fire events

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

        [NonSerialized] private bool isExplicitChanging;
        [NonSerialized] private int lastChangeIndex = -1;
        [NonSerialized] private PropertyChangedEventHandler propertyChangedHandler;
        [NonSerialized] private NotifyCollectionChangedEventHandler collectionChangedHandler;
        [NonSerialized] private ListChangedEventHandler listChangedHandler;
        [NonSerialized] private PropertyDescriptorCollection propertyDescriptors;

        private bool IsBindingList => Items is IBindingList;
        private bool IsCancelAddNew => Items is ICancelAddNew;
        private IBindingList AsBindingList => Items as IBindingList;
        private bool HookItemsPropertyChanged => !IsBindingList && canRaiseItemChange;

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

        bool IBindingList.SupportsChangeNotification => true;
        bool IBindingList.SupportsSearching => AsBindingList?.SupportsSearching ?? false;
        bool IBindingList.SupportsSorting => AsBindingList?.SupportsSorting ?? false;
        ListSortDirection IBindingList.SortDirection => AsBindingList?.SortDirection ?? default;

        public ObservableBindingList() : this(new FastLookupCollection<T>())
        {
        }

        public ObservableBindingList(IList<T> list) : base((list as ObservableBindingList<T>)?.Items ?? list) => Initialize();

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
            if (isExplicitChanging || (!RaiseCollectionChangedEvents && !RaiseItemChangedEvents))
                return;

            using (BlockReentrancy())
            {
                if (RaiseCollectionChangedEvents)
                    OnCollectionChanged(e);

                if (!RaiseListChangedEvents)
                    return;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        throw new NotImplementedException();
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        throw new NotImplementedException();
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        throw new NotImplementedException();
                        break;
                    case NotifyCollectionChangedAction.Move:
                        throw new NotImplementedException();
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                        break;
                }
            }
        }

        private void BindingList_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (isExplicitChanging || (!RaiseCollectionChangedEvents && !RaiseItemChangedEvents))
                return;

            using (BlockReentrancy())
            {
                if (RaiseListChangedEvents)
                    OnListChanged(e);

                if (!RaiseCollectionChangedEvents)
                    return;

                switch (e.ListChangedType)
                {
                    case ListChangedType.Reset:
                        throw new NotImplementedException();
                        break;
                    case ListChangedType.ItemAdded:
                        throw new NotImplementedException();
                        break;
                    case ListChangedType.ItemDeleted:
                        throw new NotImplementedException();
                        break;
                    case ListChangedType.ItemMoved:
                        throw new NotImplementedException();
                        break;
                    case ListChangedType.ItemChanged:
                        if (!RaiseItemChangedEvents)
                            return;
                        throw new NotImplementedException();
                        break;
                    case ListChangedType.PropertyDescriptorAdded:
                        throw new NotImplementedException();
                        break;
                    case ListChangedType.PropertyDescriptorDeleted:
                        throw new NotImplementedException();
                        break;
                    case ListChangedType.PropertyDescriptorChanged:
                        throw new NotImplementedException();
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
                throw new InvalidOperationException(Res.IBindingListAddNewDisabled);

            if (Items is IBindingList bindingList)
                return (T)bindingList.AddNew();

            T newItem = canAddNew ? (T)Reflector.Construct(typeof(T)) : throw new InvalidOperationException(Res.ObservableBindingListCannotAddNew(typeof(T)));
            Add(newItem);
            if (!IsCancelAddNew)
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
                return;
            }

            if (addNewPos < 0 || addNewPos != itemIndex)
                return;

            // if we are here Items is neither IBindingList nor ICancelAddNew (if it is IBindingList addNewPos is never set)
            RemoveItem(addNewPos);
        }

        public virtual void EndNew(int itemIndex)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);

            if (Items is ICancelAddNew cancelAddNew)
            {
                cancelAddNew.EndNew(itemIndex);
                return;
            }

            // inside from this class this should be called to make sure the index is not sorted.
            if (addNewPos >= 0 && addNewPos == itemIndex)
                EndNew();
        }

        private void EndNew() => addNewPos = -1;

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when the list is being cleared;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void ClearItems()
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);

            CheckReentrancy();

            if (Count == 0)
                return;

            // even if remove not allowed we can remove the element being just added and yet uncommitted
            // Note: Clear does not work for a single element after AddNew if Items is ICancelAddNew and thus addNewPos is not maintained
            if (!allowRemove && !(addNewPos == 0 && Count == 1))
                throw new InvalidOperationException(Res.IBindingListRemoveDisabled);

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

        private void FireCollectionReset(bool clearItems, bool listChangeOnly)
        {
            NotifyCollectionChangedEventHandler handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                if (!listChangeOnly && RaiseCollectionChangedEvents)
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    if (!clearItems)
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this.ToList()));
                }
                if (RaiseListChangedEvents)
                    OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }

            if (clearItems)
                FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
        }

        private void FireItemChanged(int index, T item, PropertyDescriptor pd)
        {
            NotifyCollectionChangedEventHandler handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                if (RaiseCollectionChangedEvents)
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item, index));
                if (RaiseListChangedEvents)
                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index, pd));
            }

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
            FireItemChanged(position);
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when an item is removed from list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void RemoveItem(int index)
        {
            CheckReentrancy();
            T removedItem = this[index];

            base.RemoveItem(index);

            FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when an item is added to list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void InsertItem(int index, T item)
        {
            CheckReentrancy();
            base.InsertItem(index, item);

            FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        /// <summary>
        /// Called by base class Collection&lt;T&gt; when an item is set in list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected override void SetItem(int index, T item)
        {
            CheckReentrancy();
            T originalItem = this[index];
            base.SetItem(index, item);

            FirePropertyChanged(indexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, item, index);
        }

        /// <summary>
        /// Called by base class ObservableCollection&lt;T&gt; when an item is to be moved within the list;
        /// raises a CollectionChanged event to any listeners.
        /// </summary>
        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            CheckReentrancy();

            T removedItem = this[oldIndex];

            base.RemoveItem(oldIndex);
            base.InsertItem(newIndex, removedItem);

            FirePropertyChanged(indexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex);
        }


        /// <summary>
        /// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => propertyChangedHandler?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event.
        /// </summary>
        /// <remarks>
        /// When overriding this method, you can call the <see cref="BlockReentrancy"/> to guard against reentrant collection changes.
        /// </remarks>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => collectionChangedHandler?.Invoke(this, e);

        protected virtual void OnListChanged(ListChangedEventArgs e) => listChangedHandler?.Invoke(this, e);

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
                    throw new InvalidOperationException(Res.ObservableBindingListReentrancyNotAllowed);
            }
        }

        [NotifyPropertyChangedInvocator]
        private void FirePropertyChanged([CallerMemberName] string propertyName = null) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        ///// <summary>
        ///// Helper to raise CollectionChanged event to any listeners
        ///// </summary>
        //private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        //{
        //    OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        //}

        ///// <summary>
        ///// Helper to raise CollectionChanged event to any listeners
        ///// </summary>
        //private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
        //{
        //    OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
        //}

        ///// <summary>
        ///// Helper to raise CollectionChanged event to any listeners
        ///// </summary>
        //private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
        //{
        //    OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
        //}

        ///// <summary>
        ///// Helper to raise CollectionChanged event with action == Reset to any listeners
        ///// </summary>
        //private void OnCollectionReset()
        //{
        //    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        //}

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
