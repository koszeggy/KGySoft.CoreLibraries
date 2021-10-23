#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FastBindingList.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

using KGySoft.Collections;
using KGySoft.Collections.ObjectModel;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a generic list that is able to notify its consumer about changes and supports data binding.
    /// <br/>See the <strong>Remarks</strong> section for the differences compared to the <see cref="BindingList{T}"/> class.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <remarks>
    /// <note><see cref="FastBindingList{T}"/> is mainly compatible with <see cref="BindingList{T}"/> but has a better performance than that because element lookup
    /// in <see cref="FastBindingList{T}"/> is an O(1) operation. In contrast, element lookup in <see cref="BindingList{T}"/> is an O(n) operation, which makes
    /// <see cref="BindingList{T}.AddNew"><![CDATA[BindingList<T>.AddNew]]></see> and <see cref="BindingList{T}.ListChanged"><![CDATA[BindingList<T>.ListChanged]]></see> invocation (when an element is changed)
    /// slow because they call the <see cref="Collection{T}.IndexOf"><![CDATA[Collection{T}.IndexOf]]></see> method to determine the position of the added or changed element.</note>
    /// <h1 class="heading">Comparison with <see cref="BindingList{T}"/></h1>
    /// <para><strong>Incompatibility</strong> with <see cref="BindingList{T}"/>:
    /// <list type="bullet">
    /// <item><see cref="BindingList{T}"/> is derived from <see cref="Collection{T}"/>, whereas <see cref="FastBindingList{T}"/> is derived from <see cref="FastLookupCollection{T}"/>, which is derived from <see cref="VirtualCollection{T}"/>.
    /// Both types implement the <see cref="IList{T}"/> interface though.</item>
    /// <item><see cref="BindingList{T}.AddingNew"><![CDATA[BindingList<T>.AddingNew]]></see> event has <see cref="AddingNewEventHandler"/> type, which uses <see cref="AddingNewEventArgs"/>,
    /// whereas in <see cref="FastBindingList{T}"/> the <see cref="AddingNew"/> event has <see cref="EventHandler{TEventArgs}"/> type where <em>TEventArgs</em> is <see cref="AddingNewEventArgs{T}"/>. The main difference between the two event arguments
    /// that the latter is generic.</item>
    /// <item>In <see cref="FastBindingList{T}"/> the <see cref="AllowRemove"/> property is initialized to <see langword="false"/>&#160;if the wrapped list is read-only.
    /// <br/>In contrast, in <see cref="BindingList{T}"/> this property is <see langword="true"/>&#160;by default.</item>
    /// <item>In <see cref="FastBindingList{T}"/> the <see cref="AllowNew"/> property is initialized to <see langword="false"/>&#160;if the wrapped list is read-only, or when <typeparamref name="T"/> is not a value type and has no parameterless constructor.
    /// The return value of <see cref="AllowNew"/> does not change when <see cref="AddingNew"/> event is subscribed and setting <see cref="AllowNew"/> does not reset the list.
    /// <br/>In contrast, in <see cref="BindingList{T}"/> this property is <see langword="false"/>&#160;if <typeparamref name="T"/> is not a primitive type and has no public parameterless constructor.
    /// However, return value of <see cref="BindingList{T}.AllowNew"><![CDATA[BindingList<T>.AllowNew]]></see> can change when <see cref="BindingList{T}.AddingNew"><![CDATA[BindingList<T>.AddingNew]]></see> event is subscribed,
    /// and setting the <see cref="BindingList{T}.AllowNew"><![CDATA[BindingList<T>.AllowNew]]></see> property resets the list.</item>
    /// <item>Calling <see cref="AddNew">AddNew</see> throws <see cref="InvalidOperationException"/> if <see cref="AllowNew"/> is <see langword="false"/>.</item>
    /// <item>Calling <see cref="VirtualCollection{T}.Remove">Remove</see> or <see cref="VirtualCollection{T}.Clear">Clear</see> throws <see cref="InvalidOperationException"/> if <see cref="AllowRemove"/> is <see langword="false"/>.</item>
    /// <item><see cref="AddNewCore">AddNewCore</see> returns <typeparamref name="T"/> instead of <see cref="object">object</see>.</item>
    /// <item>If <see cref="AddNewCore">AddNewCore</see> is called for a <typeparamref name="T"/> type, which cannot be instantiated automatically and the <see cref="AddingNew"/> event is not subscribed or returns <see langword="null"/>,
    /// then an <see cref="InvalidOperationException"/> will be thrown. In contrast, <see cref="BindingList{T}.AddNewCore"><![CDATA[BindingList<T>.AddNewCore]]></see> can throw an <see cref="InvalidCastException"/> or <see cref="NotSupportedException"/>.</item>
    /// <item><see cref="FastBindingList{T}"/> might not work properly if items can change their hash code while they are added to the collection.</item>
    /// </list>
    /// <note type="warning">Do not store elements in a <see cref="FastBindingList{T}"/> that may change their hash code while they are added to the collection.
    /// Finding such elements may fail even if <see cref="FastLookupCollection{T}.CheckConsistency"/> is <see langword="true"/>. If hash code is derived from some identifier property or field, you can prepare
    /// a <typeparamref name="T"/> instance by overriding the <see cref="AddNewCore">AddNewCore</see> method or by subscribing the <see cref="AddingNew"/> event
    /// to make <see cref="IBindingList.AddNew">IBindingList.AddNew</see> implementation work properly.</note> 
    /// </para>
    /// <para><strong>New features and improvements</strong> compared to <see cref="BindingList{T}"/>:
    /// <list type="bullet">
    /// <item><term>Disposable</term><description>The <see cref="FastBindingList{T}"/> implements the <see cref="IDisposable"/> interface. When an instance is disposed, then both
    /// incoming and outgoing event subscriptions (self events and <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the elements) are removed. If the wrapped collection passed
    /// to the constructor is disposable, then it will also be disposed. After disposing accessing the public members may throw <see cref="ObjectDisposedException"/>.</description></item>
    /// <item><term>Overridable properties</term><description>In <see cref="FastBindingList{T}"/> the <see cref="AllowNew"/>, <see cref="AllowRemove"/>, <see cref="AllowEdit"/>
    /// and <see cref="RaiseListChangedEvents"/> properties are virtual.</description></item>
    /// <item><term>Find support</term><description>In <see cref="FastBindingList{T}"/> the <see cref="IBindingList.Find">IBindingList.Find</see> method is supported.
    /// In <see cref="BindingList{T}"/> this throws a <see cref="NotSupportedException"/>.</description></item>
    /// <item><term>Public members for finding and sorting</term><description><see cref="FastBindingList{T}"/> offers several public <see cref="O:KGySoft.ComponentModel.FastBindingList`1.Find">Find</see>
    /// and <see cref="O:KGySoft.ComponentModel.FastBindingList`1.ApplySort">ApplySort</see> overloads. <see cref="RemoveSort">RemoveSort</see> method and <see cref="IsSorted"/>/<see cref="SortProperty"/> properties are also public instead of explicit interface implementations.</description></item>
    /// <item><term>New virtual members</term><description><see cref="AddIndexCore">AddIndexCore</see> and <see cref="RemoveIndexCore">RemoveIndexCore</see> methods can be overridden to implement <see cref="IBindingList.AddIndex">IBindingList.AddIndex</see> and <see cref="IBindingList.RemoveIndex">IBindingList.RemoveIndex</see> calls.</description></item>
    /// </list>
    /// </para>
    /// <note type="tip"><see cref="FastBindingList{T}"/> does not implement sorting. See the derived <see cref="SortableBindingList{T}"/> class for an <see cref="IBindingList"/> implementation with sorting support.</note>
    /// </remarks>
    [Serializable]
    public class FastBindingList<T> : FastLookupCollection<T>, IBindingList, ICancelAddNew, IRaiseItemChangedEvents, IDisposable
    {
        #region Fields

        #region Static Fields

        private static readonly bool canAddNew = typeof(T).CanBeCreatedWithoutParameters();
        private static readonly bool canRaiseItemChange = typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T));

        #endregion

        #region Instance Fields

        // For tracking actually subscribed elements. Not an issue that is not serialized because the clone's inner list is not accessible from outside.
        [NonSerialized]private readonly HashSet<T>? trackedSubscriptions;

        private bool disposed;
        private bool allowNew;
        private bool allowEdit;
        private bool allowRemove;
        private int addNewPos = -1;
        private bool raiseListChangedEvents;
        [NonSerialized]private bool isAddingNew;
        [NonSerialized]private PropertyDescriptorCollection? propertyDescriptors;
        [NonSerialized]private EventHandler<AddingNewEventArgs<T>>? addingNewHandler;
        [NonSerialized]private ListChangedEventHandler? listChangedHandler;

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a new item is added to the list by the <see cref="AddNew">AddNew</see> method.
        /// </summary>
        /// <remarks>
        /// By handling this event a custom item creation of <typeparamref name="T"/> can be provided.
        /// </remarks>
        public event EventHandler<AddingNewEventArgs<T>>? AddingNew
        {
            add => value.AddSafe(ref addingNewHandler); // no need to fire ListChange as in the original version because we don't change AllowNew
            remove => value.RemoveSafe(ref addingNewHandler);
        }

        /// <summary>
        /// Occurs when the list or an item in the list changes.
        /// </summary>
        public event ListChangedEventHandler? ListChanged
        {
            add => value.AddSafe(ref listChangedHandler);
            remove => value.RemoveSafe(ref listChangedHandler);
        }

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets whether new items can be added to the list by the <see cref="AddNew">AddNew</see> method.
        /// <br/>Default value: <see langword="true"/>&#160;if the wrapped list is not read-only and <typeparamref name="T"/> is a value type or has a parameterless constructor; otherwise, <see langword="false"/>.
        /// </summary>
        public virtual bool AllowNew
        {
            get => allowNew;
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
                if (value == allowNew)
                    return;
                allowNew = value;
                FireListChanged(ListChangedType.Reset, -1);
            }
        }

        /// <summary>
        /// Gets or sets whether item properties can be edited in the list.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public virtual bool AllowEdit
        {
            get => allowEdit;
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
                if (allowEdit == value)
                    return;
                allowEdit = value;
                FireListChanged(ListChangedType.Reset, -1);
            }
        }

        /// <summary>
        /// Gets or sets whether items can be removed from the list by the <see cref="VirtualCollection{T}.Remove">Remove</see>, <see cref="VirtualCollection{T}.RemoveAt">RemoveAt</see> and <see cref="VirtualCollection{T}.Clear">Clear</see> methods.
        /// <br/>Default value: <see langword="true"/>&#160;if the wrapped list is not read-only; otherwise, <see langword="false"/>.
        /// </summary>
        public virtual bool AllowRemove
        {
            get => allowRemove;
            set
            {
                if (disposed)
                    Throw.ObjectDisposedException();
                if (allowRemove == value)
                    return;
                allowRemove = value;
                FireListChanged(ListChangedType.Reset, -1);
            }
        }

        /// <summary>
        /// Gets whether the items in the list are sorted.
        /// </summary>
        /// <remarks>
        /// <para>This property returns the value of the overridable <see cref="IsSortedCore"/> property.</para>
        /// <note><see cref="FastBindingList{T}"/> returns always <see langword="false"/>&#160;for this property. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// </remarks>
        public bool IsSorted => IsSortedCore;

        /// <summary>
        /// Gets a <see cref="PropertyDescriptor" /> that is being used for sorting. Returns <see langword="null"/>&#160;if the list is not sorted or
        /// when it is sorted by the values of <typeparamref name="T"/> rather than by one of its properties.
        /// </summary>
        /// <remarks>
        /// <para>This property returns the value of the overridable <see cref="SortPropertyCore"/> property.</para>
        /// <note><see cref="FastBindingList{T}"/> returns always <see langword="null"/>&#160;for this property. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// </remarks>
        public PropertyDescriptor? SortProperty => SortPropertyCore;

        /// <summary>
        /// Gets or sets whether adding or removing items within the list raises <see cref="ListChanged"/> events.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public virtual bool RaiseListChangedEvents
        {
            get => raiseListChangedEvents;
            set => raiseListChangedEvents = value;
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the property descriptors of <typeparamref name="T"/>.
        /// </summary>
        protected PropertyDescriptorCollection PropertyDescriptors
            // ReSharper disable once ConstantNullCoalescingCondition - it CAN be null if an ICustomTypeDescriptor implemented so
            => propertyDescriptors ??= TypeDescriptor.GetProperties(typeof(T)) ?? new PropertyDescriptorCollection(null); // not static so custom providers can be registered before creating an instance

        /// <summary>
        /// Gets whether <see cref="ListChanged"/> events are enabled.
        /// <br/>The base implementation returns <see langword="true"/>.
        /// </summary>
        protected virtual bool SupportsChangeNotificationCore => true;

        /// <summary>
        /// Gets whether the list supports searching.
        /// <br/>The base implementation returns <see langword="true"/>.
        /// </summary>
        protected virtual bool SupportsSearchingCore => true;

        /// <summary>
        /// Gets whether the list supports sorting.
        /// <br/>The base implementation returns <see langword="false"/>.
        /// </summary>
        protected virtual bool SupportsSortingCore => false;

        /// <summary>
        /// Gets whether the list is sorted.
        /// <br/>The base implementation returns <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <note><see cref="FastBindingList{T}"/> returns always <see langword="false"/>&#160;for this property. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// </remarks>
        protected virtual bool IsSortedCore => false;

        /// <summary>
        /// Gets the property descriptor that is used for sorting the list if sorting, or <see langword="null"/>&#160;if the list is not sorted or
        /// when it is sorted by the values of <typeparamref name="T"/> rather than by one of its properties.
        /// <br/>The base implementation returns <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// <note><see cref="FastBindingList{T}"/> returns always <see langword="null"/>&#160;for this property. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// </remarks>
        protected virtual PropertyDescriptor? SortPropertyCore => null;

        /// <summary>
        /// Gets the direction of the sort.
        /// <br/>The base implementation returns <see cref="ListSortDirection.Ascending"/>.
        /// </summary>
        protected virtual ListSortDirection SortDirectionCore => default;

        #endregion

        #region Private Protected Properties

        private protected bool IsAddingNew => isAddingNew;

        #endregion

        #region Explicitly Implemented Interface Properties

        bool IBindingList.SupportsChangeNotification => SupportsChangeNotificationCore;
        bool IBindingList.SupportsSearching => SupportsSearchingCore;
        bool IBindingList.SupportsSorting => SupportsSortingCore;
        ListSortDirection IBindingList.SortDirection => SortDirectionCore;
        bool IRaiseItemChangedEvents.RaisesItemChangedEvents => canRaiseItemChange;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FastBindingList{T}"/> class with a <see cref="CircularList{T}"/> internally.
        /// </summary>
        public FastBindingList() => Initialize();

        /// <summary>
        /// Initializes a new instance of the <see cref="FastBindingList{T}"/> class with the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">An <see cref="IList{T}" /> of items to be contained in the <see cref="FastBindingList{T}" />.</param>
        /// <remarks>
        /// <note>Do not wrap another <see cref="IBindingList"/> or <see cref="ObservableCollection{T}"/> as their events are not captured by the <see cref="FastBindingList{T}"/> class.
        /// To capture and generate events for both wrapped and self list operations use <see cref="ObservableBindingList{T}"/> instead.</note>
        /// </remarks>
        public FastBindingList(IList<T> list) : base(list)
        {
            // whether we track items depends also on CheckConsistency, which is initialized by true from this constructor but can be turned off.
            if (canRaiseItemChange && !typeof(T).IsValueType)
                trackedSubscriptions = new HashSet<T>(ReferenceEqualityComparer<T>.Comparer);
            Initialize();
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Adds a new item to the collection.
        /// </summary>
        /// <returns>The item added to the list.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="AllowNew"/> property returns <see langword="false"/>
        /// <br/>-or-
        /// <br/><see cref="AddingNew"/> is not subscribed or returned <see langword="null"/>, and <typeparamref name="T"/> is not a value type
        /// or has no parameterless constructor.</exception>
        /// <remarks>
        /// <para>To customize the behavior either subscribe the <see cref="AddingNew"/> event or override the <see cref="AddNewCore">AddNewCore</see> method in a derived class.</para>
        /// </remarks>
        [return:NotNull]
        public T AddNew()
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (!AllowNew)
                Throw.InvalidOperationException(Res.ComponentModelAddNewDisabled);
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

        /// <summary>
        /// Sorts the list by the values of <typeparamref name="T"/> rather than one of its properties based on the specified <paramref name="direction"/>.
        /// </summary>
        /// <param name="direction">The desired direction of the sort.</param>
        /// <exception cref="NotSupportedException"><see cref="SupportsSortingCore"/> returns <see langword="false"/>.</exception>
        /// <remarks>
        /// <para>To customize the behavior override the <see cref="ApplySortCore">ApplySortCore</see> method in a derived class.</para>
        /// <note><see cref="FastBindingList{T}"/> throws a <see cref="NotSupportedException"/> for this method. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// </remarks>
        public void ApplySort(ListSortDirection direction)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            ApplySortCore(null, direction);
        }

        /// <summary>
        /// Sorts the list based on the specified <paramref name="property"/> and <paramref name="direction"/>.
        /// </summary>
        /// <param name="property">A <see cref="PropertyDescriptor"/> instance to sort by.</param>
        /// <param name="direction">The desired direction of the sort.</param>
        /// <exception cref="NotSupportedException"><see cref="SupportsSortingCore"/> returns <see langword="false"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="property"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="property"/> is not a property of <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// <para>To customize the behavior override the <see cref="ApplySortCore">ApplySortCore</see> method in a derived class.</para>
        /// <note><see cref="FastBindingList{T}"/> throws a <see cref="NotSupportedException"/> for this method. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// <note>In this overload <paramref name="property"/> cannot be <see langword="null"/>. To sort by the values of <typeparamref name="T"/> rather than one of its properties use the <see cref="ApplySort(ListSortDirection)"/> overload.</note>
        /// </remarks>
        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (property == null!)
                Throw.ArgumentNullException(Argument.property);
            if (!PropertyDescriptors.Contains(property))
                Throw.ArgumentException(Argument.property, Res.ComponentModelInvalidProperty(property, typeof(T)));
            ApplySortCore(property, direction);
        }

        /// <summary>
        /// Sorts the list based on the specified <paramref name="propertyName"/> and <paramref name="direction"/>.
        /// </summary>
        /// <param name="propertyName">A property name of <typeparamref name="T"/> to sort by.</param>
        /// <param name="direction">The desired direction of the sort.</param>
        /// <exception cref="NotSupportedException"><see cref="SupportsSortingCore"/> returns <see langword="false"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyName"/> has no corresponding <see cref="PropertyDescriptor"/> in <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// <para>To customize the behavior override the <see cref="ApplySortCore">ApplySortCore</see> method in a derived class.</para>
        /// <note><see cref="FastBindingList{T}"/> throws a <see cref="NotSupportedException"/> for this method. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// <note>In this overload a property must be specified. To sort by the values of <typeparamref name="T"/> rather than one of its properties use the <see cref="ApplySort(ListSortDirection)"/> overload.</note>
        /// </remarks>
        public void ApplySort(string propertyName, ListSortDirection direction)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (propertyName == null!)
                Throw.ArgumentNullException(Argument.propertyName);
            PropertyDescriptor? property = PropertyDescriptors[propertyName];
            if (property == null)
                Throw.ArgumentException(Argument.property, Res.ComponentModelPropertyNotExists(propertyName, typeof(T)));
            ApplySortCore(property, direction);
        }

        /// <summary>
        /// Removes any sort applied by the <see cref="O:KGySoft.ComponentModel.FastBindingList`1.ApplySort">ApplySort</see> overloads.
        /// </summary>
        /// <remarks>
        /// <remarks>
        /// <para>To customize the behavior override the <see cref="RemoveSortCore">RemoveSortCore</see> method in a derived class.</para>
        /// <note><see cref="FastBindingList{T}"/> throws a <see cref="NotSupportedException"/> for this method. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// </remarks>
        /// </remarks>
        public void RemoveSort() => RemoveSortCore();

        /// <summary>
        /// Searches for the index of the item that has the specified property descriptor with the specified value.
        /// </summary>
        /// <param name="property">The <see cref="PropertyDescriptor" /> to search on.</param>
        /// <param name="key">The value of the <paramref name="property" /> parameter to search for.</param>
        /// <returns>The zero-based index of the item that matches the property descriptor and contains the specified value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="property"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="property"/> is not a property of <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// <para>To customize the behavior override the <see cref="FindCore">FindCore</see> method in a derived class.</para>
        /// <note><paramref name="property"/> cannot be <see langword="null"/>. To search by the whole value of <typeparamref name="T"/> rather than by one of its properties
        /// use the <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> method.</note>
        /// </remarks>
        public int Find(PropertyDescriptor property, object key)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (property == null!)
                Throw.ArgumentNullException(Argument.property);
            if (!PropertyDescriptors.Contains(property))
                Throw.ArgumentException(Argument.property, Res.ComponentModelInvalidProperty(property, typeof(T)));
            return FindCore(property, key);
        }

        /// <summary>
        /// Searches for the index of the item that has the specified property descriptor with the specified value.
        /// </summary>
        /// <param name="propertyName">A property name of <typeparamref name="T"/> to search on.</param>
        /// <param name="key">The value of the specified property to search for.</param>
        /// <returns>The zero-based index of the item that matches the property descriptor and contains the specified value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyName"/> has no corresponding <see cref="PropertyDescriptor"/> in <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// <para>To customize the behavior override the <see cref="FindCore">FindCore</see> method in a derived class.</para>
        /// <note><paramref name="propertyName"/> cannot be <see langword="null"/>. To search by the whole value of <typeparamref name="T"/> rather than by one of its properties
        /// use the <see cref="VirtualCollection{T}.IndexOf">IndexOf</see> method.</note>
        /// </remarks>
        public int Find(string propertyName, object key)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (propertyName == null!)
                Throw.ArgumentNullException(Argument.propertyName);
            var property = PropertyDescriptors[propertyName];
            if (property == null)
                Throw.ArgumentException(Argument.property, Res.ComponentModelPropertyNotExists(propertyName, typeof(T)));
            return FindCore(property, key);
        }

        /// <summary>
        /// Releases the list and removes both incoming and outgoing subscriptions.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Discards a pending new item added by the <see cref="AddNew">AddNew</see> method.
        /// </summary>
        /// <param name="itemIndex">The index of the item that was previously added to the collection.</param>
        public virtual void CancelNew(int itemIndex)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (addNewPos < 0 || addNewPos != itemIndex)
                return;

            // Attention: this is a virtual method, meaning that index can be different in a derived class
            RemoveItemAt(addNewPos);
        }

        /// <summary>
        /// Commits a pending new item added by the <see cref="AddNew">AddNew</see> method.
        /// </summary>
        /// <param name="itemIndex">The index of the item that was previously added to the collection.</param>
        public virtual void EndNew(int itemIndex)
        {
            if (disposed)
                Throw.ObjectDisposedException();

            // inside from this class this should be called to make sure the index is not sorted.
            if (addNewPos >= 0 && addNewPos == itemIndex)
                EndNew();
        }

        /// <summary>
        /// Raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.Reset"/>.
        /// </summary>
        public void ResetBindings()
        {
            if (disposed)
                Throw.ObjectDisposedException();
            FireListChanged(ListChangedType.Reset, -1);
        }

        /// <summary>
        /// Raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemChanged"/> at the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">A zero-based index of the item to be reset.</param>
        public void ResetItem(int position)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            FireListChanged(ListChangedType.ItemChanged, position);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Raises the <see cref="AddingNew" /> event.
        /// </summary>
        /// <param name="e">The <see cref="AddingNewEventArgs{T}" /> instance containing the event data.</param>
        protected virtual void OnAddingNew(AddingNewEventArgs<T> e) => addingNewHandler?.Invoke(this, e);

        /// <summary>
        /// Adds a new item to the collection.
        /// </summary>
        /// <returns>The item that was added to the collection.</returns>
        /// <exception cref="InvalidOperationException"><see cref="AddingNew"/> is not subscribed or returned <see langword="null"/>, and <typeparamref name="T"/> is not a value type
        /// or has no parameterless constructor.</exception>
        /// <remarks>
        /// <para>This is the overridable implementation of the <see cref="AddNew">AddNew</see> method. The base implementation raises the <see cref="AddingNew"/> event. If it is not
        /// handled or returns <see langword="null"/>, then tries to create a new instance of <typeparamref name="T"/> and adds it to the end of the list.</para>
        /// </remarks>
        [return:NotNull]
        protected virtual T AddNewCore()
        {
            var e = new AddingNewEventArgs<T>();
            OnAddingNew(e);
            T newItem = e.NewObject is T t ? t
                : canAddNew ? (T)Reflector.CreateInstance(typeof(T))
                : Throw.InvalidOperationException<T>(Res.ComponentModelCannotAddNewFastBindingList(typeof(T)));
            Add(newItem);

            // Return new item to caller
            return newItem!;
        }

        /// <summary>
        /// If overridden in a derived class, sorts the items of the list.
        /// <br/>The base implementation throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="property">A <see cref="PropertyDescriptor"/> that specifies the property to sort on. If <see langword="null"/>, then the list will be sorted
        /// by the values of <typeparamref name="T"/> rather than one of its properties.</param>
        /// <param name="direction">The desired direction of the sort.</param>
        /// <exception cref="NotSupportedException"><see cref="SupportsSortingCore"/> returns <see langword="false"/>.</exception>
        /// <remarks>
        /// <note><see cref="FastBindingList{T}"/> throws a <see cref="NotSupportedException"/> for this method. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// </remarks>
        protected virtual void ApplySortCore(PropertyDescriptor? property, ListSortDirection direction) => Throw.NotSupportedException(Res.NotSupported);

        /// <summary>
        /// Removes any sort applied by the <see cref="O:KGySoft.ComponentModel.FastBindingList`1.ApplySort">ApplySort</see> overloads.
        /// <br/>The base implementation throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException"><see cref="SupportsSortingCore"/> returns <see langword="false"/>.</exception>
        /// <remarks>
        /// <note><see cref="FastBindingList{T}"/> throws a <see cref="NotSupportedException"/> for this method. Use the <see cref="SortableBindingList{T}"/> to be able to use sorting.</note>
        /// </remarks>
        protected virtual void RemoveSortCore() => Throw.NotSupportedException(Res.NotSupported);

        /// <summary>
        /// Searches for the index of the item that has the specified property descriptor with the specified value.
        /// <br/>The base implementation performs a linear search on the items.
        /// </summary>
        /// <param name="property">A <see cref="PropertyDescriptor"/> that specifies the property to search for.</param>
        /// <param name="key">The value of <paramref name="property"/> to match.</param>
        /// <returns>The zero-based index of the item that matches the property descriptor and contains the specified value.</returns>
        /// <remarks>
        /// <note><see cref="FastBindingList{T}"/> performs a linear search for this method, therefore has an O(n) cost, where n is the number of elements in the list.
        /// <br/><see cref="SortableBindingList{T}"/> is able to perform a binary search if <paramref name="property"/> equals <see cref="SortProperty"/>, in which case the cost of this method is O(log n).</note>
        /// </remarks>
        protected virtual int FindCore(PropertyDescriptor property, object key)
        {
            if (property == null!)
                Throw.ArgumentNullException(Argument.property);

            int length = Count;
            for (int i = 0; i < length; i++)
            {
                // virtual GetItem call is intended here
                T item = GetItem(i);
                if (item == null)
                    continue;

                if (Equals(property.GetValue(item), key))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// If overridden in a derived class, adds the <see cref="PropertyDescriptors"/> to the indices used for searching.
        /// <br/>The base implementation does nothing.
        /// </summary>
        /// <param name="property">The property to add to the indices used for searching.</param>
        protected virtual void AddIndexCore(PropertyDescriptor property)
        {
        }

        /// <summary>
        /// If overridden in a derived class, removes the <see cref="PropertyDescriptors"/> from the indices used for searching.
        /// <br/>The base implementation does nothing.
        /// </summary>
        /// <param name="property">The property to remove from the indices used for searching.</param>
        protected virtual void RemoveIndexCore(PropertyDescriptor property)
        {
        }

        /// <summary>
        /// Gets the element at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified <paramref name="index"/>.</returns>
        protected override T GetItem(int index)
        {
            T result = base.GetItem(index);
            if (CheckConsistency && trackedSubscriptions?.Contains(result) == false)
                HookPropertyChanged(result);
            return result;
        }

        /// <summary>
        /// Replaces the <paramref name="item" /> at the specified <paramref name="index" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index.</param>
        /// <remarks>
        /// <para>After the item is set, <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemChanged"/> is raised indicating the index of the item that was set.</para>
        /// </remarks>
        protected override void SetItem(int index, T item)
        {
            if (disposed)
                Throw.ObjectDisposedException();

            if (canRaiseItemChange || CheckConsistency)
            {
                T originalItem = base.GetItem(index);
                if (canRaiseItemChange)
                    UnhookPropertyChanged(originalItem);
            }

            base.SetItem(index, item);

            if (canRaiseItemChange)
                HookPropertyChanged(item);

            FireListChanged(ListChangedType.ItemChanged, index);
        }

        /// <summary>
        /// Inserts an element into the <see cref="FastBindingList{T}" /> at the specified <paramref name="index" />.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        /// <remarks>
        /// <para><see cref="InsertItem">InsertItem</see> performs the following operations:
        /// <list type="number">
        /// <item>Calls <see cref="EndNew(int)">EndNew</see> to commit the last possible uncommitted item added by the <see cref="AddNew">AddNew</see> method.</item>
        /// <item>Inserts the item at the specified index.</item>
        /// <item>Raises a <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemChanged"/> indicating the index of the item that was inserted.</item>
        /// </list>
        /// </para>
        /// </remarks>
        protected override void InsertItem(int index, T item)
        {
            if (disposed)
                Throw.ObjectDisposedException();

            EndNew();
            if (isAddingNew)
                addNewPos = index;

            base.InsertItem(index, item);

            // subscribing even if raising events is turned off now right now so we don't have to go through all items when raising is toggled
            if (canRaiseItemChange)
                HookPropertyChanged(item);

            FireListChanged(ListChangedType.ItemAdded, index);
        }

        /// <summary>
        /// Removes the element at the specified <paramref name="index" /> from the <see cref="FastBindingList{T}" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="InvalidOperationException"><see cref="AllowRemove"/> is <see langword="false"/>&#160;and the item to remove is not an uncommitted one added by the <see cref="AddNew">AddNew</see> method.</exception>
        /// <remarks>
        /// <para>This method raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.ItemDeleted"/>.</para>
        /// </remarks>
        protected override void RemoveItemAt(int index)
        {
            if (disposed)
                Throw.ObjectDisposedException();

            // even if remove not allowed we can remove the element being just added and yet uncommitted
            if (!AllowRemove && !(addNewPos >= 0 && addNewPos == index))
                Throw.InvalidOperationException(Res.ComponentModelRemoveDisabled);

            EndNew();
            if (canRaiseItemChange)
                UnhookPropertyChanged(base.GetItem(index));

            base.RemoveItemAt(index);
            FireListChanged(ListChangedType.ItemDeleted, index);
        }

        /// <summary>
        /// Removes all elements from the <see cref="FastBindingList{T}"/>.
        /// </summary>
        /// <remarks>
        /// <para>This method raises the <see cref="ListChanged"/> event of type <see cref="ListChangedType.Reset"/>.</para>
        /// </remarks>
        protected override void ClearItems()
        {
            if (disposed)
                Throw.ObjectDisposedException();

            if (Count == 0)
                return;

            // even if remove not allowed we can remove the element being just added and yet uncommitted
            if (!AllowRemove && !(addNewPos == 0 && Count == 1))
                Throw.InvalidOperationException(Res.ComponentModelRemoveDisabled);

            EndNew();
            UnhookPropertyChangedAll();

            base.ClearItems();
            FireListChanged(ListChangedType.Reset, -1);
        }

        /// <inheritdoc/>
        protected override void OnMapRebuilt()
        {
            if (CheckConsistency)
            {
                UnhookPropertyChangedAll();
                HookPropertyChangedAll();
            }

            FireListChanged(ListChangedType.Reset, -1);
        }

        /// <summary>
        /// Releases the resources used by this <see cref="FastBindingList{T}"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to a call to <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;

            raiseListChangedEvents = false;
            UnhookPropertyChangedAll();

            (Items as IDisposable)?.Dispose();
            listChangedHandler = null;
            addingNewHandler = null;
            disposed = true;
        }

        /// <summary>
        /// Commits a pending new item of any position added by the <see cref="AddNew">AddNew</see> method.
        /// </summary>
        protected virtual void EndNew() => addNewPos = -1;

        /// <summary>
        /// Raises the <see cref="ListChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="ListChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnListChanged(ListChangedEventArgs e) => listChangedHandler?.Invoke(this, e);

        #endregion

        #region Private Protected Methods

        private protected void FireListChanged(ListChangedType type, int index)
        {
            if (!raiseListChangedEvents)
                return;
            OnListChanged(new ListChangedEventArgs(type, index));
        }

        /// <summary>
        /// Called when an item contained in the <see cref="FastBindingList{T}"/> changes. Can be used if the binding list is sorted or uses indices.
        /// <br/>The base implementation does nothing.
        /// </summary>
        /// <param name="item">The changed item.</param>
        /// <param name="itemIndex">Index of the item determined by the virtual <see cref="FastLookupCollection{T}.GetItemIndex">GetItemIndex</see> method.</param>
        /// <param name="property">The descriptor of the changed property.</param>
        private protected virtual void ItemPropertyChanged([DisallowNull]T item, int itemIndex, PropertyDescriptor? property) { }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            // Default: if T is ValueType or has parameterless constructor (but can be turned on and off)
            bool readOnly = Items.IsReadOnly;
            allowNew = canAddNew && !readOnly;
            allowRemove = !readOnly;
            allowEdit = true; //Items is IList list ? !list.IsReadOnly : !readOnly; // for editing taking the non-generic IList.IsReadOnly, which is false for fixed size but otherwise writable collections.

            raiseListChangedEvents = true;
            HookPropertyChangedAll();
        }

        private void HookPropertyChanged(T item)
        {
            if (item is not INotifyPropertyChanged notifyPropertyChanged)
                return;

            notifyPropertyChanged.PropertyChanged += Item_PropertyChanged;
            if (CheckConsistency)
                trackedSubscriptions?.Add(item);
        }

        private void UnhookPropertyChanged(T item)
        {
            if (item is not INotifyPropertyChanged notifyPropertyChanged)
                return;

            notifyPropertyChanged.PropertyChanged -= Item_PropertyChanged;
            trackedSubscriptions?.Remove(item);
        }

        private void HookPropertyChangedAll()
        {
            if (!canRaiseItemChange)
                return;
            foreach (T item in Items)
                HookPropertyChanged(item);
        }

        private void UnhookPropertyChangedAll()
        {
            if (!canRaiseItemChange)
                return;
            foreach (T item in Items)
                UnhookPropertyChanged(item);
            if (trackedSubscriptions?.Count > 0)
            {
                // ToList is intended because UnhookPropertyChanged changes trackedSubscriptions
                foreach (T item in trackedSubscriptions.ToList())
                    UnhookPropertyChanged(item);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx) => HookPropertyChangedAll();

        #endregion

        #region Event handlers

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Invalid sender or property name: simply resetting
            if (e == null! || sender is not T item || String.IsNullOrEmpty(e.PropertyName))
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

            PropertyDescriptor? pd = e.PropertyName == null ? null : PropertyDescriptors.Find(e.PropertyName, true);
            ItemPropertyChanged(item, pos, pd);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, pos, pd));
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        object IBindingList.AddNew() => AddNew();
        void IBindingList.AddIndex(PropertyDescriptor property) => AddIndexCore(property);
        void IBindingList.RemoveIndex(PropertyDescriptor property) => RemoveIndexCore(property);

        #endregion

        #endregion
    }
}
