using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using KGySoft.Collections;
using KGySoft.Collections.ObjectModel;
using KGySoft.Libraries;
using KGySoft.Reflection;

namespace KGySoft.ComponentModel
{
    // Compatible with BindingList<T> but allows turning on/off not just list change events but also element change events and provides more flexible overriding.
    // Better performance than BindingList<T>, even if child change is enabled because IndexOf is O(1) due to FastLookupCollection<T> base.
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
    public class FastBindingList<T> : FastLookupCollection<T>, IBindingList, ICancelAddNew, IRaiseItemChangedEvents, IDisposable
    {

        private static readonly bool canAddNew = typeof(T).CanBeCreatedWithoutParameters();
        private static readonly bool canRaiseItemChange = typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T));

        private bool disposed;
        private bool allowNew;
        private bool allowEdit;
        private bool allowRemove;
        private int addNewPos = -1; // TODO: move when sort!
        private bool raiseItemChangedEvents;
        private bool raiseListChangedEvents;

        [NonSerialized] private PropertyDescriptorCollection propertyDescriptors;
        [NonSerialized] private EventHandler<AddingNewEventArgs<T>> addingNewHandler;
        [NonSerialized] private ListChangedEventHandler listChangedHandler;
        [NonSerialized] private int lastChangeIndex = -1;

        /// <summary>
        /// Gets the property descriptors of <typeparamref name="T"/>.
        /// </summary>
        protected PropertyDescriptorCollection PropertyDescriptors
            // ReSharper disable once ConstantNullCoalescingCondition - it CAN be null if an ICustomTypeDescriptor implemented so
            => propertyDescriptors ?? (propertyDescriptors = TypeDescriptor.GetProperties(typeof(T)) ?? new PropertyDescriptorCollection(null)); // not static so custom providers can be registered before creating an instance

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
            // TODO: sort
            // TODO: item change
            if (!RaiseItemChangedEvents)
                return;

            if (sender == null || string.IsNullOrEmpty(e?.PropertyName))
            {
                // Fire reset event (per INotifyPropertyChanged spec)
                ResetBindings();
                return;
            }

            // The change event is broken should someone pass an item to us that is not
            // of type T.  Still, if they do so, detect it and ignore.  It is an incorrect
            // and rare enough occurrence that we do not want to slow the mainline path
            // with "is" checks.
            T item;

            try
            {
                item = (T)sender;
            }
            catch (InvalidCastException)
            {
                ResetBindings();
                return;
            }

            int pos = lastChangeIndex;
            if (pos < 0 || pos >= Count || !this[pos].Equals(item))
            {
                pos = GetItemIndex(item);
                lastChangeIndex = pos;
            }

            // item removed from the underlying list
            if (pos == -1)
            {
                UnhookPropertyChanged(item);
                ResetBindings();
            }

            PropertyDescriptor pd = e.PropertyName == null ? null : PropertyDescriptors.Find(e.PropertyName, true);

            // Create event args.  If there was no matching property descriptor,
            // we raise the list changed anyway.
            ListChangedEventArgs args = new ListChangedEventArgs(ListChangedType.ItemChanged, pos, pd);

            // Fire the ItemChanged event
            OnListChanged(args);
        }

        #endregion

        #region AddingNew event

        public event EventHandler<AddingNewEventArgs<T>> AddingNew
        {
            add => addingNewHandler += value; // no need to fire ListChange as in the original version because we don't change AddNew
            remove => addingNewHandler -= value;
        }

        protected virtual void OnAddingNew(AddingNewEventArgs<T> e) => addingNewHandler?.Invoke(this, e);

        #endregion

        #region IBindingList interface

        public T AddNew()
        {
            // Create new item and add it to list
            object newItem = AddNewCore();

            // TODO: On EndNew it can be moved to its place (DoSort) - property: AllowSortOnNewAdded, field: sortUpToDate/pending
            // Record position of new item (to support cancellation later on)
            addNewPos = (newItem != null) ? GetItemIndex((T)newItem) : -1;

            // Return new item to caller
            return (T)newItem;
        }

        object IBindingList.AddNew() => AddNew();

        // Remarks: Can throw InvalidOperationException
        protected virtual T AddNewCore()
        {
            var e = new AddingNewEventArgs<T>();
            OnAddingNew(e);
            T newItem = e.NewObject is T t ? t : canAddNew ? (T)Reflector.Construct(typeof(T)) : throw new InvalidOperationException(Res.SortableBindingCannotAddNew(typeof(T)));
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

        protected virtual bool SupportsSearchingCore => false;

        bool IBindingList.SupportsSorting => SupportsSortingCore;

        protected virtual bool SupportsSortingCore => false;

        public bool IsSorted => IsSortedCore;

        protected virtual bool IsSortedCore => false;

        public PropertyDescriptor SortProperty => SortPropertyCore;

        protected virtual PropertyDescriptor SortPropertyCore => null;

        ListSortDirection IBindingList.SortDirection => SortDirectionCore;

        protected virtual ListSortDirection SortDirectionCore => default;

        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
            => ApplySortCore(property ?? throw new ArgumentNullException(nameof(property), Res.ArgumentNull), direction);

        public void ApplySort(ListSortDirection direction)
            => ApplySortCore(null, direction);

        public void ApplySort(string propertyName, ListSortDirection direction)
        {
            var property = PropertyDescriptors[propertyName ?? throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull)];
            if (property == null)
                throw new ArgumentException(Res.SortableBindingListPropertyNotExists(propertyName, typeof(T)), nameof(propertyName));
            ApplySortCore(property, direction);
        }

        protected virtual void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
        {
        }

        public void RemoveSort() => RemoveSortCore();

        protected virtual void RemoveSortCore()
        {
        }

        int IBindingList.Find(PropertyDescriptor prop, object key)
        {
            return FindCore(prop, key);
        }

        protected virtual int FindCore(PropertyDescriptor prop, object key)
        {
            throw new NotSupportedException();
        }

        // TODO: AddIndexCore - for sorting and finding - base does nothing - it uses only the sorted index if can
        void IBindingList.AddIndex(PropertyDescriptor prop)
        {
            // Not supported
        }

        // TODO: RemoveIndexCore - for sorting and finding
        void IBindingList.RemoveIndex(PropertyDescriptor prop)
        {
            // Not supported
        }

        #endregion

        #region ICollection<T>

        protected override void ClearItems()
        {
            // TODO: consider sort
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            EndNew(addNewPos);

            if (canRaiseItemChange)
            {
                foreach (T item in Items)
                    UnhookPropertyChanged(item);
            }

            base.ClearItems();
            FireListChanged(ListChangedType.Reset, -1);
        }

        protected override void InsertItem(int index, T item)
        {
            // TODO: consider sort
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            EndNew(addNewPos);
            base.InsertItem(index, item);

            // subscribing even if raising events is turned off now right now so we don't have to go through all items when raising is toggled
            if (canRaiseItemChange)
                HookPropertyChanged(item);

            FireListChanged(ListChangedType.ItemAdded, index);
        }

        protected override void RemoveItem(int index)
        {
            // TODO: consider sort
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            // even if if remove not allowed we can remove the element being just added and yet uncommitted
            if (!allowRemove && !(addNewPos >= 0 && addNewPos == index))
            {
                // TODO: rather InvalidOperatonException
                throw new NotSupportedException();
            }

            EndNew(addNewPos);

            if (canRaiseItemChange)
            {
                UnhookPropertyChanged(this[index]);
            }

            base.RemoveItem(index);
            FireListChanged(ListChangedType.ItemDeleted, index);
        }

        protected override void SetItem(int index, T item)
        {
            // TODO: consider sort
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (canRaiseItemChange)
                UnhookPropertyChanged(this[index]);

            base.SetItem(index, item);

            if (canRaiseItemChange)
                HookPropertyChanged(item);

            FireListChanged(ListChangedType.ItemChanged, index);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            raiseListChangedEvents = false;
            Clear();
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
            // TODO: consider sort
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            if (addNewPos < 0 || addNewPos != itemIndex)
                return;
            RemoveItem(addNewPos);
            addNewPos = -1;
        }

        public virtual void EndNew(int itemIndex)
        {
            // TODO: consider sort - see AddNew comment (AllowSortOnNewAdded)
            if (disposed)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            if (addNewPos >= 0 && addNewPos == itemIndex)
            {
                addNewPos = -1;
            }
        }

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
