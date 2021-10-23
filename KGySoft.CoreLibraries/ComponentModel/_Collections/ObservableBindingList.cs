#if !NET35
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObservableBindingList.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using KGySoft.Annotations;
using KGySoft.Collections.ObjectModel;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a class that combines the features of an <see cref="ObservableCollection{T}"/> and <see cref="BindingList{T}"/>. Unlike <see cref="ObservableCollection{T}"/>, can raise the <see cref="CollectionChanged"/> event also when a property
    /// of a contained element changes.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <seealso cref="ObservableCollection{T}" />
    /// <seealso cref="IBindingList" />
    /// <remarks>
    /// <note>In NET Framework 3.5 this class is not available because it would require to reference <c>System.WindowsBase.dll</c>.</note>
    /// <para>The <see cref="ObservableBindingList{T}"/> can be used in any environment where either <see cref="IBindingList"/> or <see cref="INotifyCollectionChanged"/> (mainly <see cref="ObservableCollection{T}"/>) types are supported.</para>
    /// <para>If initialized by another <see cref="IBindingList"/> or <see cref="INotifyCollectionChanged"/> implementations, the <see cref="ObservableBindingList{T}"/> will capture and delegate also the events of the inner collections.</para>
    /// <para>If the <see cref="ObservableBindingList{T}"/> is initialized by the default constructor it will use a <see cref="SortableBindingList{T}"/> inside.</para>
    /// <note type="tip">In an environment, which supports only the <see cref="IBindingList"/> or <see cref="INotifyCollectionChanged"/> interface but not the other, <see cref="ObservableBindingList{T}"/> can be used as a bridge between the two worlds.
    /// For example, by passing an <see cref="ObservableCollection{T}"/> to the constructor, it will be able to be accessed as an <see cref="IBindingList"/> implementation, and vice-versa: by wrapping an <see cref="IBindingList"/> instance
    /// (such as <see cref="FastBindingList{T}"/> or <see cref="SortableBindingList{T}"/>), it can be used as an <see cref="INotifyCollectionChanged"/> implementation by the <see cref="ObservableBindingList{T}"/> class.</note>
    /// <para><strong>Differences</strong> to the <see cref="ObservableCollection{T}"/> class:
    /// <list type="bullet">
    /// <item>The <see cref="PropertyChanged"/> event is raised also for a sort of additional properties, such as <see cref="AllowNew"/>, <see cref="AllowEdit"/>, <see cref="AllowRemove"/>, <see cref="RaiseCollectionChangedEvents"/>,
    /// <see cref="RaiseItemChangedEvents"/> and <see cref="RaiseListChangedEvents"/>.</item>
    /// <item>If <typeparamref name="T"/> implements <see cref="INotifyPropertyChanged"/>, then the <see cref="CollectionChanged"/> event can be raised even for property changes of the contained elements.
    /// This behavior can be adjusted by the <see cref="RaiseItemChangedEvents"/> property.</item>
    /// <item>There is no constructor that accepts an <see cref="IEnumerable{T}"/> instance. Instead, an instance of <see cref="IList{T}"/> must be provided.</item>
    /// <item>When the <see cref="ObservableBindingList{T}"/> is initialized by another <see cref="IList{T}"/>, then the passed instance will be wrapped rather than just copying the elements.
    /// Therefore the changes performed on the <see cref="ObservableBindingList{T}"/> will be reflected also in the wrapped list.
    /// <br/>In contrast, passing any collection to an <see cref="ObservableCollection{T}"/> will copy the items in a new <see cref="List{T}"/> to be used inside the <see cref="ObservableCollection{T}"/>.</item>
    /// <item>In <see cref="ObservableBindingList{T}"/> the <see cref="OnCollectionChanged">OnCollectionChanged</see> method simply raises the <see cref="CollectionChanged"/> event without calling <see cref="BlockReentrancy">BlockReentrancy</see>,
    /// which is rather called explicitly whenever it is needed.</item>
    /// <item>In <see cref="ObservableBindingList{T}"/> the <see cref="Collection{T}.Add">Add</see>, <see cref="Collection{T}.Insert">Insert</see>, <see cref="Collection{T}.Remove">Remove</see>, <see cref="Collection{T}.RemoveAt">RemoveAt</see>,
    /// <see cref="Collection{T}.Clear">Clear</see> and <see cref="Move">Move</see> methods, and the setter of the <see cref="Collection{T}.Items">Items</see> property can throw a <see cref="NotSupportedException"/> if the wrapped collection is read-only.</item>
    /// </list>
    /// </para>
    /// <para><strong>Differences</strong> to the <see cref="BindingList{T}"/> class:
    /// <list type="bullet">
    /// <item>In case of multiple subscribers of the <see cref="CollectionChanged"/> and <see cref="ListChanged"/> events reentrant changes during the event invocation is protected similarly to the <see cref="ObservableCollection{T}"/> class.
    /// However, the <see cref="BlockReentrancy">BlockReentrancy</see> method is not called from the <see cref="OnListChanged">OnListChanged</see> and <see cref="OnCollectionChanged">OnCollectionChanged</see> methods. Instead, it should be called
    /// explicitly when raising any or both of these events.</item>
    /// <item>If the initializer collection that is passed to the constructor is an <see cref="IBindingList"/> or <see cref="INotifyCollectionChanged"/> implementation, then changes performed directly on the wrapped collection are
    /// also captured and delegated to the self events.</item>
    /// <item>Accessing the <see cref="ObservableBindingList{T}"/> through the <see cref="IBindingList"/>, <see cref="ICancelAddNew"/> and <see cref="IRaiseItemChangedEvents"/> interfaces are in some aspects different from a regular
    /// <see cref="BindingList{T}"/>. Their implementation is as follows:
    /// <list type="definition">
    /// <item><term><see cref="IBindingList"/></term><description>
    /// <list type="bullet">
    /// <item><see cref="IBindingList.AddNew">AddNew</see>:
    /// <list type="bullet">
    /// <item>If <see cref="AllowNew"/> returns <see langword="false"/>, then an <see cref="InvalidOperationException"/> will be thrown.</item>
    /// <item>Otherwise, if the underlying list implements <see cref="IBindingList"/>, then its <see cref="IBindingList.AddNew">AddNew</see> implementation will be called.</item>
    /// <item>Otherwise, if <typeparamref name="T"/> is a value type or has a parameterless constructor, then a new item of <typeparamref name="T"/> is created and added to the list.</item>
    /// <item>Otherwise, an <see cref="InvalidOperationException"/> will be thrown.</item>
    /// </list></item>
    /// <item><see cref="IBindingList.AllowNew"/>:
    /// <list type="bullet">
    /// <item>Returns <see langword="false"/>&#160;if the underlying list implements <see cref="IBindingList"/>, and its <see cref="IBindingList.AllowNew"/> returns <see langword="false"/>.</item>
    /// <item>Otherwise, returns the lastly set value. Or, if was never set, returns <see langword="true"/>&#160;if <typeparamref name="T"/> is a value type or has a parameterless constructor, and the underlying collection is not read-only.</item>
    /// </list></item>
    /// <item><see cref="IBindingList.AllowEdit"/> and <see cref="IBindingList.AllowRemove"/>:
    /// <list type="bullet">
    /// <item>Both return <see langword="false"/>&#160;if the underlying list implements <see cref="IBindingList"/>, and its <see cref="IBindingList.AllowEdit"/>/<see cref="IBindingList.AllowRemove"/> return <see langword="false"/>.</item>
    /// <item>Otherwise, they return the lastly set value. Or, if they were never set, they return <see langword="true"/>&#160;if the underlying collection is not read-only.</item>
    /// </list></item>
    /// <item><see cref="IBindingList.SupportsChangeNotification"/>: returns always <see langword="true"/>.</item>
    /// <item><see cref="IBindingList.SupportsSearching"/>, <see cref="IBindingList.SupportsSorting"/> and <see cref="IBindingList.IsSorted"/>: If the underlying list implements <see cref="IBindingList"/>, then the values of the underlying
    /// properties are returned; otherwise, they all return <see langword="false"/>.</item>
    /// <item><see cref="IBindingList.SortProperty"/>: If the underlying list implements <see cref="IBindingList"/>, then the value of the underlying property is returned; otherwise, returns <see langword="null"/>.</item>
    /// <item><see cref="IBindingList.SortDirection"/>: If the underlying list implements <see cref="IBindingList"/>, then the value of the underlying property is returned; otherwise, returns <see cref="ListSortDirection.Ascending"/>.</item>
    /// <item><see cref="IBindingList.Find">Find</see>, <see cref="IBindingList.ApplySort">ApplySort</see> and <see cref="IBindingList.RemoveSort">RemoveSort</see>: If the underlying list implements <see cref="IBindingList"/>, then their
    /// underlying implementation is called; otherwise, they all throw <see cref="NotSupportedException"/>.</item>
    /// <item><see cref="IBindingList.AddIndex">AddIndex</see> and <see cref="IBindingList.RemoveIndex">RemoveIndex</see>: If the underlying list implements <see cref="IBindingList"/>, then their
    /// underlying implementation is called; otherwise, they do nothing.</item>
    /// </list>
    /// </description></item>
    /// <item><term><see cref="ICancelAddNew"/></term><description>
    /// <list type="bullet">
    /// <item><see cref="ICancelAddNew.EndNew">EndNew</see>: If the underlying list implements <see cref="ICancelAddNew"/>, then the underlying implementation is called; otherwise, commits the last pending new item added by
    /// the <see cref="AddNew">AddNew</see> method.</item>
    /// <item><see cref="ICancelAddNew.CancelNew">CancelNew</see>: If the underlying list implements <see cref="ICancelAddNew"/>, then the underlying implementation is called; otherwise, discards the last pending new item added by
    /// the <see cref="AddNew">AddNew</see> method.</item>
    /// </list>
    /// </description></item>
    /// <item><term><see cref="IRaiseItemChangedEvents"/></term><description>Returns the value of the <see cref="RaiseItemChangedEvents"/>, which is a settable property. See the <see cref="RaiseItemChangedEvents"/> property for more details.</description></item>
    /// </list>
    /// </item>
    /// </list>
    /// </para>
    /// <para><strong><see cref="IDisposable"/> support</strong>:
    /// <br/>The <see cref="ObservableBindingList{T}"/> implements the <see cref="IDisposable"/> interface. When an instance is disposed, then both
    /// incoming and outgoing event subscriptions (self events and <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the elements) are removed. If the wrapped collection passed
    /// to the constructor is disposable, then it will also be disposed. After disposing accessing the public members may throw <see cref="ObjectDisposedException"/>.</para>
    /// </remarks>
    [Serializable]
    public class ObservableBindingList<T> : Collection<T>, IDisposable,
        INotifyCollectionChanged, INotifyPropertyChanged,
        IBindingList, ICancelAddNew, IRaiseItemChangedEvents
    {
        #region SimpleMonitor class

        [Serializable]
        private class SimpleMonitor : IDisposable
        {
            #region Fields

            private int busyCount;

            #endregion

            #region Properties

            public bool Busy => busyCount > 0;

            #endregion

            #region Methods

            public void Enter() => ++busyCount;
            public void Dispose() => --busyCount;

            #endregion
        }

        #endregion

        #region Constants

        private const string indexerName = "Item[]";

        #endregion

        #region Fields

        #region Static Fields

        private static readonly bool canAddNew = typeof(T).CanBeCreatedWithoutParameters();
        private static readonly bool canRaiseItemChange = typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T));

        #endregion

        #region Instance Fields

        private readonly SimpleMonitor monitor = new SimpleMonitor();

        private bool disposed;
        private bool raiseItemChangedEvents;
        private bool raiseListChangedEvents;
        private bool raiseCollectionChangedEvents;
        private bool allowNew;
        private bool allowEdit;
        private bool allowRemove;
        private int addNewPos = -1;

        [NonSerialized]private bool isAddingNew;
        [NonSerialized]private bool isExplicitChanging; // TODO: nullable enum instead of bool so if on insert/replace/change there comes a sort (reset/move) it can be allowed
        [NonSerialized]private int lastChangeIndex = -1;
        [NonSerialized]private PropertyChangedEventHandler? propertyChangedHandler;
        [NonSerialized]private NotifyCollectionChangedEventHandler? collectionChangedHandler;
        [NonSerialized]private ListChangedEventHandler? listChangedHandler;
        [NonSerialized]private PropertyDescriptorCollection? propertyDescriptors;

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the list or an item in the list changes.
        /// </summary>
        /// <remarks>
        /// <para>This event also occurs if the underlying collection that was passed to the constructor implements the <see cref="INotifyCollectionChanged"/> or <see cref="IBindingList"/> interfaces and an inner
        /// <see cref="INotifyCollectionChanged.CollectionChanged"/> or <see cref="IBindingList.ListChanged"/> event is captured.</para>
        /// <para>Raising this event can be disabled and enabled by the <see cref="RaiseListChangedEvents"/> property.</para>
        /// <para>Raising this event for item property changes can be disabled and enabled by the <see cref="RaiseItemChangedEvents"/> property.</para>
        /// </remarks>
        public event ListChangedEventHandler? ListChanged
        {
            add => value.AddSafe(ref listChangedHandler);
            remove => value.RemoveSafe(ref listChangedHandler);
        }

        /// <summary>
        /// Occurs when the list or an item in the list changes.
        /// </summary>
        /// <remarks>
        /// <para>This event also occurs if the underlying collection that was passed to the constructor implements the <see cref="INotifyCollectionChanged"/> or <see cref="IBindingList"/> interfaces and an inner
        /// <see cref="INotifyCollectionChanged.CollectionChanged"/> or <see cref="IBindingList.ListChanged"/> event is captured.</para>
        /// <para>Raising this event can be disabled and enabled by the <see cref="RaiseCollectionChangedEvents"/> property.</para>
        /// <note>In <see cref="ObservableBindingList{T}"/> this event can also be raised if a property of an element changed.
        /// <br/>In contrast, <see cref="ObservableCollection{T}"/> does not raise this event in such case.
        /// <br/>Raising this event for item property changes can be disabled and enabled by the <see cref="RaiseItemChangedEvents"/> property.
        /// </note>
        /// </remarks>
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => value.AddSafe(ref collectionChangedHandler);
            remove => value.RemoveSafe(ref collectionChangedHandler);
        }

        /// <summary>
        /// Occurs when a property value changes. Occurs also when the elements in the list changes, in which case the value of the <see cref="PropertyChangedEventArgs.PropertyName"/> will be <c>Item[]</c>.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => value.AddSafe(ref propertyChangedHandler);
            remove => value.RemoveSafe(ref propertyChangedHandler);
        }

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets whether <see cref="ListChanged"/> and <see cref="CollectionChanged"/> events are invoked with
        /// <see cref="ListChangedType.ItemChanged"/>/<see cref="NotifyCollectionChangedAction.Replace"/> change type when a property of an item changes.
        /// <br/>If <typeparamref name="T"/> does not implement <see cref="INotifyPropertyChanged"/>, then this property returns <see langword="false"/>.
        /// Otherwise, the default value is <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>Setting this property to <see langword="false"/>&#160;can result in better performance if the underlying list has a poor lookup performance.
        /// <note>If the <see cref="ObservableBindingList{T}"/> is initialized by its default constructor, then the element lookup has O(1) cost.</note>
        /// </para>
        /// <para>This property returns always <see langword="false"/>&#160;if <typeparamref name="T"/> does not implement the <see cref="INotifyPropertyChanged"/> interface.</para>
        /// <para><see cref="ListChanged"/> is invoked only if <see cref="RaiseListChangedEvents"/> is <see langword="true"/>; and <see cref="CollectionChanged"/> is raised only
        /// if <see cref="RaiseCollectionChangedEvents"/> is <see langword="true"/>.</para>
        /// </remarks>
        public virtual bool RaiseItemChangedEvents
        {
            get => raiseItemChangedEvents && canRaiseItemChange;
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
                if (value == raiseItemChangedEvents)
                    return;
                bool raiseChange = value != RaiseItemChangedEvents;
                raiseItemChangedEvents = value;
                if (raiseChange)
                    FirePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether new items can be added to the list by the <see cref="AddNew">AddNew</see> method.
        /// <br/>If the underlying list implements <see cref="IBindingList"/> and its <see cref="IBindingList.AllowNew"/> returns <see langword="false"/>, then this property returns also <see langword="false"/>.
        /// Otherwise, the default value is <see langword="true"/>&#160;if the wrapped list is not read-only and <typeparamref name="T"/> is a value type or has a parameterless constructor; otherwise, <see langword="false"/>.
        /// </summary>
        public virtual bool AllowNew
        {
            get => allowNew && (AsBindingList?.AllowNew).GetValueOrDefault(true);
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
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

        /// <summary>
        /// Gets or sets whether item properties can be edited in the list.
        /// <br/>If the underlying list implements <see cref="IBindingList"/> and its <see cref="IBindingList.AllowEdit"/> returns <see langword="false"/>, then this property returns also <see langword="false"/>.
        /// Otherwise, the default value is <see langword="true"/>.
        /// </summary>
        public virtual bool AllowEdit
        {
            get => allowEdit && (AsBindingList?.AllowEdit).GetValueOrDefault(true);
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
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

        /// <summary>
        /// Gets or sets whether items can be removed from the list by the <see cref="VirtualCollection{T}.Remove">Remove</see>, <see cref="VirtualCollection{T}.RemoveAt">RemoveAt</see> and <see cref="VirtualCollection{T}.Clear">Clear</see> methods.
        /// <br/>If the underlying list implements <see cref="IBindingList"/> and its <see cref="IBindingList.AllowRemove"/> returns <see langword="false"/>, then this property returns also <see langword="false"/>.
        /// Otherwise, the default value is <see langword="true"/>.
        /// </summary>
        public virtual bool AllowRemove
        {
            get => allowRemove && (AsBindingList?.AllowRemove).GetValueOrDefault(true);
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
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

        /// <summary>
        /// Gets or sets whether adding or removing items within the list raises <see cref="ListChanged"/> events.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public virtual bool RaiseListChangedEvents
        {
            get => raiseListChangedEvents;
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
                if (value == raiseListChangedEvents)
                    return;

                raiseListChangedEvents = value;
                FirePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether adding or removing items within the list raises <see cref="CollectionChanged"/> events.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public virtual bool RaiseCollectionChangedEvents
        {
            get => raiseCollectionChangedEvents;
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
                if (value == raiseCollectionChangedEvents)
                    return;

                raiseCollectionChangedEvents = value;
                FirePropertyChanged();
            }
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the property descriptors of <typeparamref name="T"/>.
        /// </summary>
        protected PropertyDescriptorCollection PropertyDescriptors
            // ReSharper disable once ConstantNullCoalescingCondition - it CAN be null if an ICustomTypeDescriptor is implemented so
            => propertyDescriptors ??= TypeDescriptor.GetProperties(typeof(T)) ?? new PropertyDescriptorCollection(null); // not static so custom providers can be registered before creating an instance

        #endregion

        #region Private Properties

        private bool IsBindingList => Items is IBindingList;
        private IBindingList? AsBindingList => Items as IBindingList;
        private bool HookItemsPropertyChanged => !IsBindingList && canRaiseItemChange;
        private bool IsDualNotifyCollectionType => IsBindingList && Items is INotifyCollectionChanged;

        #endregion

        #region Explicitly Implemented Interface Properties

        bool IRaiseItemChangedEvents.RaisesItemChangedEvents => RaiseItemChangedEvents;
        bool IBindingList.SupportsChangeNotification => true;
        bool IBindingList.SupportsSearching => AsBindingList?.SupportsSearching ?? false;
        bool IBindingList.SupportsSorting => AsBindingList?.SupportsSorting ?? false;
        bool IBindingList.IsSorted => AsBindingList?.IsSorted ?? false;
        PropertyDescriptor? IBindingList.SortProperty => AsBindingList?.SortProperty;
        ListSortDirection IBindingList.SortDirection => AsBindingList?.SortDirection ?? default;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableBindingList{T}"/> class with a <see cref="SortableBindingList{T}"/> internally.
        /// </summary>
        public ObservableBindingList() : this(new SortableBindingList<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableBindingList{T}"/> class with the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">An <see cref="IList{T}" /> of items to be contained in the <see cref="ObservableBindingList{T}" />.</param>
        /// <remarks>
        /// <para>The <paramref name="list"/> will be wrapped by the new <see cref="ObservableBindingList{T}"/> instance. Changes performed on the <see cref="ObservableBindingList{T}"/> will be reflected in the
        /// wrapped <paramref name="list"/> as well.</para>
        /// <para>If the wrapped <paramref name="list"/> implements the <see cref="INotifyCollectionChanged"/> or <see cref="IBindingList"/> interface, then their <see cref="INotifyCollectionChanged.CollectionChanged"/> and
        /// <see cref="IBindingList.ListChanged"/> events will be captured and raised as self <see cref="CollectionChanged"/> and <see cref="ListChanged"/> events.</para>
        /// <note type="tip">In an environment, which supports only the <see cref="IBindingList"/> or <see cref="INotifyCollectionChanged"/> interface but not the other, <see cref="ObservableBindingList{T}"/> can be used as a bridge between the two worlds.
        /// For example, by passing an <see cref="ObservableCollection{T}"/> to the constructor, it will be able to be accessed as an <see cref="IBindingList"/> implementation, and vice-versa: by wrapping an <see cref="IBindingList"/> instance
        /// (such as <see cref="FastBindingList{T}"/> or <see cref="SortableBindingList{T}"/>), it can be used as an <see cref="INotifyCollectionChanged"/> implementation by the <see cref="ObservableBindingList{T}"/> class.</note>
        /// </remarks>
        public ObservableBindingList(IList<T> list) : base(list) => Initialize();

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Releases the list and removes both incoming and outgoing subscriptions.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Adds a new item to the collection.
        /// </summary>
        /// <returns>The item added to the list.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="AllowNew"/> property returns <see langword="false"/>
        /// <br/>-or-
        /// <br/>The underlying collection could not add the new item, and <typeparamref name="T"/> is not a value type or has no parameterless constructor.</exception>
        /// <remarks>
        /// <para>If <see cref="AllowNew"/> returns <see langword="false"/>, then an <see cref="InvalidOperationException"/> will be thrown.</para>
        /// <para>Otherwise, if the underlying list implements <see cref="IBindingList"/>, then its <see cref="IBindingList.AddNew">AddNew</see> implementation will be called.</para>
        /// <para>Otherwise, if <typeparamref name="T"/> is a value type or has a parameterless constructor, then a new item of <typeparamref name="T"/> is created and added to the list.</para>
        /// <para>Otherwise, an <see cref="InvalidOperationException"/> will be thrown.</para>
        /// </remarks>
        public T AddNew()
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (!AllowNew)
                Throw.InvalidOperationException(Res.ComponentModelAddNewDisabled);

            if (Items is IBindingList bindingList)
            {
                T result;
                isAddingNew = true;
                try
                {
                    // if AddNew returns an invalid type we let the invalid cast exception come
                    result = (T)bindingList.AddNew()!;
                }
                finally
                {
                    isAddingNew = false;
                }

                return result;
            }

            if (!canAddNew)
                Throw.InvalidOperationException(Res.ComponentModelCannotAddNewObservableBindingList(typeof(T)));

            T newItem = (T)Reflector.CreateInstance(typeof(T));
            Add(newItem);
            addNewPos = Count - 1;
            return newItem;
        }

        /// <summary>
        /// Discards a pending new item added by the <see cref="AddNew">AddNew</see> method.
        /// </summary>
        /// <param name="itemIndex">The index of the item that was previously added to the collection.</param>
        /// <remarks>
        /// <para>If the underlying list implements <see cref="ICancelAddNew"/>, then the underlying implementation is called; otherwise, discards the last pending new item added by
        /// the <see cref="AddNew">AddNew</see> method.</para>
        /// </remarks>
        public virtual void CancelNew(int itemIndex)
        {
            if (disposed)
                Throw.ObjectDisposedException();
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

        /// <summary>
        /// Commits a pending new item added by the <see cref="AddNew">AddNew</see> method.
        /// </summary>
        /// <param name="itemIndex">The index of the item that was previously added to the collection.</param>
        /// <remarks>
        /// <para>If the underlying list implements <see cref="ICancelAddNew"/>, then the underlying implementation is called; otherwise, commits the last pending new item added by
        /// the <see cref="AddNew">AddNew</see> method.</para>
        /// </remarks>
        public virtual void EndNew(int itemIndex)
        {
            if (disposed)
                Throw.ObjectDisposedException();

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

        /// <summary>
        /// Moves the item at the specified index to a new location in the <see cref="ObservableBindingList{T}"/>.
        /// </summary>
        /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
        /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
        public void Move(int oldIndex, int newIndex)
        {
            if (Items.IsReadOnly)
                Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            if (oldIndex < 0 || oldIndex >= Count)
                Throw.ArgumentOutOfRangeException(Argument.oldIndex);
            if (newIndex < 0 || newIndex >= Count)
                Throw.ArgumentOutOfRangeException(Argument.newIndex);
            MoveItem(oldIndex, newIndex);
        }

        /// <summary>
        /// Raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.Reset"/>
        /// and the <see cref="CollectionChanged"/> event of type <see cref="NotifyCollectionChangedAction.Reset"/>.
        /// </summary>
        public void ResetBindings()
        {
            if (disposed)
                Throw.ObjectDisposedException();
            FireCollectionReset(false, false);
        }

        /// <summary>
        /// Raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemChanged"/>
        /// and the <see cref="CollectionChanged"/> event of type <see cref="NotifyCollectionChangedAction.Replace"/>
        /// at the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">A zero-based index of the item to be reset.</param>
        public void ResetItem(int position)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            FireItemChanged(position, this[position], null);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Releases the resources used by this <see cref="ObservableBindingList{T}"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to a call to <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            monitor.Dispose();
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

        /// <summary>
        /// Replaces the <paramref name="item" /> at the specified <paramref name="index" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index.</param>
        /// <remarks>
        /// <para>After the item is set, <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemChanged"/>
        /// and <see cref="CollectionChanged"/> event of type <see cref="NotifyCollectionChangedAction.Replace"/> are raised indicating the index of the item that was set.</para>
        /// </remarks>
        protected override void SetItem(int index, T item)
        {
            if (disposed)
                Throw.ObjectDisposedException();

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

        /// <summary>
        /// Inserts an element into the <see cref="ObservableBindingList{T}" /> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        /// <remarks>
        /// <para><see cref="InsertItem">InsertItem</see> performs the following operations:
        /// <list type="number">
        /// <item>Calls <see cref="EndNew(int)">EndNew</see> to commit the last possible uncommitted item added by the <see cref="AddNew">AddNew</see> method.</item>
        /// <item>Inserts the item at the specified index.</item>
        /// <item>Raises a <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemChanged"/>
        /// and <see cref="CollectionChanged"/> event of type <see cref="NotifyCollectionChangedAction.Add"/> indicating the index of the item that was inserted.</item>
        /// </list>
        /// </para>
        /// </remarks>
        protected override void InsertItem(int index, T item)
        {
            if (disposed)
                Throw.ObjectDisposedException();

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

        /// <summary>
        /// Removes the element at the specified <paramref name="index" /> from the <see cref="ObservableBindingList{T}" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="InvalidOperationException"><see cref="AllowRemove"/> is <see langword="false"/>&#160;and the item to remove is not an uncommitted one added by the <see cref="AddNew">AddNew</see> method.</exception>
        /// <remarks>
        /// <para>This method raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemDeleted"/>
        /// and <see cref="CollectionChanged"/> event of type <see cref="NotifyCollectionChangedAction.Remove"/>.</para>
        /// </remarks>
        protected override void RemoveItem(int index)
        {
            if (disposed)
                Throw.ObjectDisposedException();

            CheckReentrancy();

            // even if remove not allowed we can remove the element being just added and yet uncommitted
            if (!allowRemove && !(addNewPos >= 0 && addNewPos == index))
                Throw.InvalidOperationException(Res.ComponentModelRemoveDisabled);

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

        /// <summary>
        /// Removes all elements from the <see cref="ObservableBindingList{T}"/>.
        /// </summary>
        /// <remarks>
        /// <para>This method raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.Reset"/>
        /// and <see cref="CollectionChanged"/> event of type <see cref="NotifyCollectionChangedAction.Reset"/>.</para>
        /// </remarks>
        protected override void ClearItems()
        {
            if (disposed)
                Throw.ObjectDisposedException();

            CheckReentrancy();

            if (Count == 0)
                return;

            // even if remove not allowed we can remove the element being just added and yet uncommitted
            if (!allowRemove && !(addNewPos == 0 && Count == 1))
                Throw.InvalidOperationException(Res.ComponentModelRemoveDisabled);

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

        /// <summary>
        /// Moves the item at the specified index to a new location in the <see cref="ObservableBindingList{T}"/>.
        /// </summary>
        /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
        /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
        /// <remarks>
        /// <para>This method raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemMoved"/>
        /// and <see cref="CollectionChanged"/> event of type <see cref="NotifyCollectionChangedAction.Move"/>.</para>
        /// </remarks>
        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            if (disposed)
                Throw.ObjectDisposedException();

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

        /// <summary>
        /// Raises the <see cref="PropertyChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => propertyChangedHandler?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => collectionChangedHandler?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="ListChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="ListChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnListChanged(ListChangedEventArgs e) => listChangedHandler?.Invoke(this, e);

        /// <summary>
        /// Disallows reentrant attempts to change this collection.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> instance that can be used to create a block of protected scope.</returns>
        /// <remarks>
        /// Typical usage is to wrap event invocations with a <see langword="using"/>&#160;scope:
        /// <code lang="C#">
        /// using (BlockReentrancy())
        /// {
        ///     OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        ///     OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        /// }
        /// </code>
        /// </remarks>
        protected IDisposable BlockReentrancy()
        {
            monitor.Enter();
            return monitor;
        }

        /// <summary>
        /// Checks for reentrant attempts to change this collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">The result of a previous <see cref="BlockReentrancy">BlockReentrancy</see> call was not disposed yet.
        /// Typically, this means there are additional attempts to change this collection during a <see cref="CollectionChanged"/> or <see cref="ListChanged"/> event.</exception>
        protected void CheckReentrancy()
        {
            if (monitor.Busy)
            {
                // we can allow changes if there's only one listener
                if ((collectionChangedHandler?.GetInvocationList().Length ?? 0) + (listChangedHandler?.GetInvocationList().Length ?? 0) > 1)
                    Throw.InvalidOperationException(Res.ComponentModelReentrancyNotAllowed);
            }
        }

        #endregion

        #region Private Methods

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

        private void HookPropertyChanged(T item)
        {
            if (item is not INotifyPropertyChanged notifyPropertyChanged)
                return;

            notifyPropertyChanged.PropertyChanged += Item_PropertyChanged;
        }

        private void UnhookPropertyChanged(T item)
        {
            if (item is not INotifyPropertyChanged notifyPropertyChanged)
                return;

            notifyPropertyChanged.PropertyChanged -= Item_PropertyChanged;
        }

        private void EndNew() => addNewPos = -1;

        private void FireItemReplace(int index, T oldItem, T newItem)
        {
            NotifyCollectionChangedEventHandler? handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler? handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, index));
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
            }

            FirePropertyChanged(indexerName);
        }

        private void FireItemChanged(int index, T item, PropertyDescriptor? pd)
        {
            if (!RaiseItemChangedEvents)
                return;
            NotifyCollectionChangedEventHandler? handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler? handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item, index));
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index, pd));
            }

            FirePropertyChanged(indexerName);
        }

        private void FireItemAdded(int index, T item)
        {
            NotifyCollectionChangedEventHandler? handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler? handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
            }

            FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
        }

        private void FireItemRemoved(int index, T item)
        {
            NotifyCollectionChangedEventHandler? handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler? handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
            }

            FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
        }

        private void FireItemMoved(T item, int newIndex, int oldIndex)
        {
            NotifyCollectionChangedEventHandler? handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler? handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemMoved, newIndex, oldIndex));
            }

            FirePropertyChanged(indexerName);
        }

        private void FireCollectionReset(bool countChanged, bool listChangeOnly)
        {
            NotifyCollectionChangedEventHandler? handlerCollChanged = collectionChangedHandler;
            ListChangedEventHandler? handlerListChanged = listChangedHandler;
            if ((handlerCollChanged == null || !RaiseCollectionChangedEvents) && (handlerListChanged == null || !RaiseListChangedEvents))
                return;
            using (BlockReentrancy())
            {
                if (!listChangeOnly)
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }

            if (countChanged)
                FirePropertyChanged(nameof(Count));
            FirePropertyChanged(indexerName);
        }

        [NotifyPropertyChangedInvocator]
        private void FirePropertyChanged([CallerMemberName] string propertyName = null!) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification = "False alarm, the new analyzer includes the complexity of local methods. And moving them outside this method would be a bad idea.")]
        private void ProcessCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            #region Local Methods

            void HookNewItems(IList? newItems)
            {
                if (newItems == null || !HookItemsPropertyChanged)
                    return;

                foreach (object? newItem in newItems)
                {
                    if (newItem is T t)
                        HookPropertyChanged(t);
                }
            }

            void UnhookOldItems(IList? oldItems)
            {
                if (oldItems == null || !HookItemsPropertyChanged)
                    return;

                foreach (object? oldItem in oldItems)
                {
                    if (oldItem is T t)
                        UnhookPropertyChanged(t);
                }
            }

            #endregion

            using (BlockReentrancy())
            {
                OnCollectionChanged(e);
                if (IsDualNotifyCollectionType)
                    return;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems?.Count > 1)
                        {
                            HookNewItems(e.NewItems);
                            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                            break;
                        }

                        if (HookItemsPropertyChanged)
                            HookPropertyChanged(this[e.NewStartingIndex]);
                        OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, e.NewStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        UnhookOldItems(e.OldItems);
                        if (e.OldItems?.Count > 1)
                        {
                            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                            break;
                        }

                        OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, e.OldStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (!(e.OldItems?.Count == 1 && e.NewItems?.Count == 1 && ReferenceEquals(e.OldItems[0], e.NewItems[0])))
                        {
                            UnhookOldItems(e.OldItems);
                            HookNewItems(e.NewItems);
                        }

                        if (e.NewStartingIndex == e.OldStartingIndex && e.OldItems?.Count == 1 && e.NewItems?.Count == 1)
                            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, e.NewStartingIndex));
                        else
                            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        OnListChanged(new ListChangedEventArgs(ListChangedType.ItemMoved, e.NewStartingIndex, e.OldStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        UnhookOldItems(e.OldItems);
                        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                        break;
                }
            }
        }

        private void ProcessListChanged(ListChangedEventArgs e)
        {
            using (BlockReentrancy())
            {
                if (RaiseListChangedEvents)
                    OnListChanged(e);

                if (!RaiseCollectionChangedEvents || IsDualNotifyCollectionType)
                    return;

                switch (e.ListChangedType)
                {
                    case ListChangedType.ItemAdded:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this[e.NewIndex], e.NewIndex));
                        break;
                    case ListChangedType.ItemDeleted:
                        // note: after the remove we can't retrieve the old item
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, default(T), e.NewIndex));
                        break;
                    case ListChangedType.ItemMoved:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, this[e.NewIndex], e.NewIndex, e.OldIndex));
                        break;
                    case ListChangedType.ItemChanged:
                        if (e.PropertyDescriptor != null && !RaiseItemChangedEvents)
                            return;

                        // note: in case of replace we can't retrieve the old item
                        T? item = e.NewIndex >= 0 && e.NewIndex < Count ? this[e.NewIndex] : default;
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, e.PropertyDescriptor != null ? item : default(T), e.NewIndex));
                        break;
                    default:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        break;
                }
            }
        }

        #endregion

        #region Event handlers

        private void NotifyCollectionChanged_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (isExplicitChanging)
                return;

            if (e.Action.In(NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Reset))
                EndNew();

            // We can jump out early only if we don't need to maintain item subscriptions
            if (!HookItemsPropertyChanged && !RaiseCollectionChangedEvents && !RaiseListChangedEvents)
                return;

            ProcessCollectionChanged(e);
        }

        private void BindingList_ListChanged(object? sender, ListChangedEventArgs e)
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

            ProcessListChanged(e);
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!RaiseItemChangedEvents || (!RaiseListChangedEvents && !RaiseCollectionChangedEvents))
                return;

            // Invalid sender or property name: simply resetting
            if (e == null! || sender is not T item || string.IsNullOrEmpty(e.PropertyName))
            {
                ResetBindings();
                return;
            }

            // in case of a slow IndexOf implementation caching last changed index
            int pos = lastChangeIndex;
            if (pos < 0 || pos >= Count || !Equals(this[pos], item))
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

            PropertyDescriptor? pd = e.PropertyName == null ? null : PropertyDescriptors.Find(e.PropertyName, true);
            FireItemChanged(pos, item, pd);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction) => (AsBindingList ?? Throw.NotSupportedException<IBindingList>()).ApplySort(property, direction);
        int IBindingList.Find(PropertyDescriptor property, object key) => AsBindingList?.Find(property, key) ?? Throw.NotSupportedException<int>();
        void IBindingList.AddIndex(PropertyDescriptor property) => AsBindingList?.AddIndex(property);
        void IBindingList.RemoveIndex(PropertyDescriptor property) => AsBindingList?.RemoveIndex(property);
        void IBindingList.RemoveSort() => (AsBindingList ?? Throw.NotSupportedException<IBindingList>()).RemoveSort();
        object? IBindingList.AddNew() => AddNew();

        #endregion

        #endregion
    }
}
#endif