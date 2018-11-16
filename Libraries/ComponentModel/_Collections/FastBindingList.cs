using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using KGySoft.Collections.ObjectModel;
using KGySoft.Libraries;
using KGySoft.Reflection;

// ReSharper disable RedundantBaseQualifier - not redundant: they are virtual members and we prevent to call possible derived methods
namespace KGySoft.ComponentModel
{
    // Compatible with BindingList<T> but allows turning on/off not just list change events but also element change events and provides more flexible overriding.
    // Better performance than BindingList<T>, even if child change is enabled because IndexOf is O(1) due to FastLookupCollection<T> base.
    // New features:
    // - Disposable: removes both incoming (self events) and outgoing (elements PropertyChanged) subscriptions. If the collection passed to the ctor is disposable, it also will be disposed.
    // - RaiseListChangedEvents is virtual
    // - RaiseItemChangedEvents
    // - AllowEdit/Remove/New - properties are virtual
    // - SupportsSearching returns true
    // - Several ApplySort and Find overloads (sort is not supported by this base class but see SortableBindingList)
    // Changes to BindingList<T>:
    // - AllowEdit/Remove/New - initialized by IsReadOnly
    // - AddNewCore returns T instead of object; throws InvalidOperationException if AllowNew is true but cannot add new item without event or override
    // - If OnAddingNew does not create an item of T the AddNewCore will not throw an InvalidCastException but creates a compatible element if can or an InvalidOperationException is thrown
    // - Type of AddingNew event is EventHandler<AddingNewEventArgs<T>> instead of AddingNewEventHandler
    // - Subscription change to AddingNew event does not reset the list because return value of AllowNew does not depend on whether AddingNew is subscribed. It must be set explicitly if we want to allow new events.
    // - IBingindList.ApplySort/Find/RemoveSort are implicit implementations as public methods
    [Serializable]
    public class FastBindingList<T> : FastLookupCollection<T>, IBindingList, ICancelAddNew, IRaiseItemChangedEvents, IDisposable
    {
        private static readonly bool canAddNew = typeof(T).CanBeCreatedWithoutParameters();
        private static readonly bool canRaiseItemChange = typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T));

        private bool disposed;
        private bool allowNew;
        private bool allowEdit;
        private bool allowRemove;
        private int addNewPos = -1;
        private bool raiseItemChangedEvents;
        private bool raiseListChangedEvents;
        [NonSerialized] private bool isAddingNew;
        [NonSerialized] private PropertyDescriptorCollection propertyDescriptors;
        [NonSerialized] private EventHandler<AddingNewEventArgs<T>> addingNewHandler;
        [NonSerialized] private ListChangedEventHandler listChangedHandler;

        /// <summary>
        /// Gets the property descriptors of <typeparamref name="T"/>.
        /// </summary>
        protected PropertyDescriptorCollection PropertyDescriptors
            // ReSharper disable once ConstantNullCoalescingCondition - it CAN be null if an ICustomTypeDescriptor implemented so
            => propertyDescriptors ?? (propertyDescriptors = TypeDescriptor.GetProperties(typeof(T)) ?? new PropertyDescriptorCollection(null)); // not static so custom providers can be registered before creating an instance

        protected bool IsAddingNew => isAddingNew;

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="FastBindingList{T}"/> class using default settings.
        /// </summary>
        public FastBindingList() => Initialize();

        /// <summary>
        /// Initializes a new instance of the <see cref="FastBindingList{T}"/> class with the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">An <see cref="IList{T}" /> of items to be contained in the <see cref="FastBindingList{T}" />.</param>
        /// TODO: remark: do not wrap another binding list or observable collection as their events are not captured here. To capture and generate events for both wrapped and self list operations use ObservableBindingList instead.
        public FastBindingList(IList<T> list) : base(list) => Initialize();

        private void Initialize()
        {
            // Default: if T is ValueType or has parameterless constructor (but can be turned on and off)
            bool readOnly = Items.IsReadOnly;
            allowNew = canAddNew && !Items.IsReadOnly;
            allowRemove = !readOnly;
            allowEdit = Items is IList list ? !list.IsReadOnly : !readOnly; // for editing taking the non-generic IList.IsReadOnly, which is false for fixed size but otherwise writable collections.

            raiseListChangedEvents = true;

            // Default: T is INotifyPropertyChanged. It still can be turned off for better performance/scaling.
            raiseItemChangedEvents = canRaiseItemChange;
            if (!canRaiseItemChange)
                return;

            foreach (T item in Items)
                HookPropertyChanged(item);
        }

        #endregion

        #region PropertyChange

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

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!RaiseItemChangedEvents)
                return;

            // Invalid sender or property name: simply resetting
            if (!(sender is T item) || string.IsNullOrEmpty(e?.PropertyName))
            {
                ResetBindings();
                return;
            }

            // Note: if indices can be different in a derived class that index will be reported
            int pos = GetItemIndex(item);

            // item was removed but we still receive events
            if (pos < 0)
            {
                UnhookPropertyChanged(item);
                ResetBindings();
            }

            PropertyDescriptor pd = e.PropertyName == null ? null : PropertyDescriptors.Find(e.PropertyName, true);
            ItemPropertyChanged(item, pos, pd);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, pos, pd));
        }

        /// <summary>
        /// Called when an item contained in the <see cref="FastBindingList{T}"/> changes. Can be used if the binding list is sorted or uses indices.
        /// <br/>The base implementation does nothing.
        /// </summary>
        /// <param name="item">The changed item.</param>
        /// <param name="itemIndex">Index of the item determined by the virtual <see cref="FastLookupCollection{T}.GetItemIndex">GetItemIndex</see> method.</param>
        /// <param name="property">The descriptor of the changed property.</param>
        protected virtual void ItemPropertyChanged(T item, int itemIndex, PropertyDescriptor property)
        {
        }

        #endregion

        #region AddingNew event

        public event EventHandler<AddingNewEventArgs<T>> AddingNew
        {
            add => addingNewHandler += value; // no need to fire ListChange as in the original version because we don't change AllowNew
            remove => addingNewHandler -= value;
        }

        protected virtual void OnAddingNew(AddingNewEventArgs<T> e) => addingNewHandler?.Invoke(this, e);

        #endregion

        #region IBindingList interface

        public T AddNew()
        {
            isAddingNew = true;
            try
            {
                return AddNewCore();
            }
            finally
            {
                isAddingNew = false;
            }
        }

        object IBindingList.AddNew() => AddNew();

        // Remarks: Can throw InvalidOperationException
        // Will not throw InvalidCastException as BindingList
        protected virtual T AddNewCore()
        {
            var e = new AddingNewEventArgs<T>();
            OnAddingNew(e);
            T newItem = e.NewObject is T t ? t : canAddNew ? (T)Reflector.Construct(typeof(T)) : throw new InvalidOperationException(Res.FastBindingListCannotAddNew(typeof(T)));
            Add(newItem);

            // Return new item to caller
            return newItem;
        }

        public virtual bool AllowNew
        {
            get => allowNew;
            set
            {
                if (value == allowNew)
                    return;
                allowNew = value;
                FireListChanged(ListChangedType.Reset, -1);
            }
        }

        public virtual bool AllowEdit
        {
            get => allowEdit;
            set
            {
                if (allowEdit == value)
                    return;
                allowEdit = value;
                FireListChanged(ListChangedType.Reset, -1);
            }
        }


        public virtual bool AllowRemove
        {
            get => allowRemove;
            set
            {
                if (allowRemove == value)
                    return;
                allowRemove = value;
                FireListChanged(ListChangedType.Reset, -1);
            }
        }


        bool IBindingList.SupportsChangeNotification => SupportsChangeNotificationCore;

        protected virtual bool SupportsChangeNotificationCore => true;

        bool IBindingList.SupportsSearching => SupportsSearchingCore;

        protected virtual bool SupportsSearchingCore => true;

        bool IBindingList.SupportsSorting => SupportsSortingCore;

        protected virtual bool SupportsSortingCore => false;

        public bool IsSorted => IsSortedCore;

        protected virtual bool IsSortedCore => false;

        public PropertyDescriptor SortProperty => SortPropertyCore;

        protected virtual PropertyDescriptor SortPropertyCore => null;

        ListSortDirection IBindingList.SortDirection => SortDirectionCore;

        protected virtual ListSortDirection SortDirectionCore => default;

        public void ApplySort(ListSortDirection direction)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            ApplySortCore(null, direction);
        }

        // property cannot be null here
        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            if (property == null)
                throw new ArgumentNullException(nameof(property), Res.ArgumentNull);
            if (!PropertyDescriptors.Contains(property))
                throw new ArgumentException(Res.FastBindingListInvalidProperty(property, typeof(T)), nameof(property));
            ApplySortCore(property, direction);
        }

        public void ApplySort(string propertyName, ListSortDirection direction)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            PropertyDescriptor property = PropertyDescriptors[propertyName ?? throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull)];
            if (property == null)
                throw new ArgumentException(Res.FastBindingListPropertyNotExists(propertyName, typeof(T)), nameof(propertyName));
            ApplySortCore(property, direction);
        }

        // property can be null
        protected virtual void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
        {
            // TODO: Res
            throw new NotSupportedException();
        }

        public void RemoveSort() => RemoveSortCore();

        protected virtual void RemoveSortCore()
        {
        }

        // remark: property cannot be null here, to search for the whole element use IndexOf
        public int Find(PropertyDescriptor property, object key)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            if (property == null)
                throw new ArgumentNullException(nameof(property), Res.ArgumentNull);
            if (!PropertyDescriptors.Contains(property))
                throw new ArgumentException(Res.FastBindingListInvalidProperty(property, typeof(T)), nameof(property));
            return FindCore(property, key);
        }

        public int Find(string propertyName, object key)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            var property = PropertyDescriptors[propertyName ?? throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull)];
            if (property == null)
                throw new ArgumentException(Res.FastBindingListPropertyNotExists(propertyName, typeof(T)), nameof(propertyName));
            return FindCore(property, key);
        }

        protected virtual int FindCore(PropertyDescriptor property, object key)
        {
            int length = Count;
            for (int i = 0; i < length; i++)
            {
                // virtual GetItem call is intended here
                if (Equals(property.GetValue(GetItem(i)), key))
                    return i;
            }

            return -1;
        }

        void IBindingList.AddIndex(PropertyDescriptor property) => AddIndexCore(property);

        protected virtual void AddIndexCore(PropertyDescriptor property)
        {
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property) => RemoveIndexCore(property);

        protected virtual void RemoveIndexCore(PropertyDescriptor property)
        {
        }

        #endregion

        #region ICollection<T>

        protected override void SetItem(int index, T item)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (canRaiseItemChange)
                UnhookPropertyChanged(base.GetItem(index));

            base.SetItem(index, item);

            if (canRaiseItemChange)
                HookPropertyChanged(item);

            FireListChanged(ListChangedType.ItemChanged, index);
        }

        protected override void InsertItem(int index, T item)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            EndNew();
            if (isAddingNew)
                addNewPos = index;

            base.InsertItem(index, item);

            // subscribing even if raising events is turned off now right now so we don't have to go through all items when raising is toggled
            if (canRaiseItemChange)
                HookPropertyChanged(item);

            FireListChanged(ListChangedType.ItemAdded, index);
        }

        protected override void RemoveItem(int index)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            // even if remove not allowed we can remove the element being just added and yet uncommitted
            if (!allowRemove && !(addNewPos >= 0 && addNewPos == index))
            {
                // TODO: Res (and rather InvalidOperatonException)
                throw new NotSupportedException();
            }

            EndNew();
            if (canRaiseItemChange)
                UnhookPropertyChanged(base.GetItem(index));

            base.RemoveItem(index);
            FireListChanged(ListChangedType.ItemDeleted, index);
        }

        protected override void ClearItems()
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            EndNew();
            if (canRaiseItemChange)
            {
                foreach (T item in Items)
                    UnhookPropertyChanged(item);
            }

            base.ClearItems();
            FireListChanged(ListChangedType.Reset, -1);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            raiseListChangedEvents = false;
            if (canRaiseItemChange)
            {
                foreach (T item in Items)
                    UnhookPropertyChanged(item);
            }

            (Items as IDisposable)?.Dispose();

            listChangedHandler = null;
            addingNewHandler = null;
            disposed = true;
        }

        /// <summary>
        /// Clears the list and removes both incoming and outgoing subscriptions.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region ICancelAddNew

        public virtual void CancelNew(int itemIndex)
        {
            // TODO: in comment: indices can have different order in a derived class, must be overridden along with RemoveItem, which is called from this method with the specified index
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            if (addNewPos < 0 || addNewPos != itemIndex)
                return;

            // Attention: this is a virtual method, meaning of index can be different in a derived class
            RemoveItem(addNewPos);
        }

        public virtual void EndNew(int itemIndex)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            // inside from this class this should be called to make sure the index is not sorted.
            if (addNewPos >= 0 && addNewPos == itemIndex)
                EndNew();
        }

        protected virtual void EndNew() => addNewPos = -1;

        #endregion

        #region ListChanged event

        public event ListChangedEventHandler ListChanged
        {
            add => listChangedHandler += value;
            remove => listChangedHandler -= value;
        }

        protected virtual void OnListChanged(ListChangedEventArgs e) => listChangedHandler?.Invoke(this, e);

        public void ResetBindings() => FireListChanged(ListChangedType.Reset, -1);

        // TODO: sorting
        public virtual void ResetItem(int position) => FireListChanged(ListChangedType.ItemChanged, position);

        internal /*private protected*/ void FireListChanged(ListChangedType type, int index)
        {
            if (!raiseListChangedEvents)
                return;
            OnListChanged(new ListChangedEventArgs(type, index));
        }

        public virtual bool RaiseListChangedEvents
        {
            get => raiseListChangedEvents;
            set => raiseListChangedEvents = value;
        }

        #endregion

        #region IRaiseItemChangedEvents

        // It is for the ListChanged event with ItemChanged type when a property of an element changes. Can be turned off for better performance.
        // note: gets false if T is not INotifyPropertyChanged or when list change events are turned off
        // remark: If off, still can come ListChanged with ItemChanged, if an item is replaced by the indexer or when ResetItem is called.
        public virtual bool RaiseItemChangedEvents
        {
            get => raiseItemChangedEvents && canRaiseItemChange && raiseListChangedEvents;
            set => raiseItemChangedEvents = value;
        }

        bool IRaiseItemChangedEvents.RaisesItemChangedEvents => canRaiseItemChange;

        #endregion
    }
}
