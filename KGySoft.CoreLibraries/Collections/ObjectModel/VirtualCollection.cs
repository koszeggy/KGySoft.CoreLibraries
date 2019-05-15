#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: VirtualCollection.cs
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;

#endregion

namespace KGySoft.Collections.ObjectModel
{
    /// <summary>
    /// Similar to <see cref="Collection{T}"/> but provides virtual members not just for writing an setting but also for getting elements
    /// such as <see cref="GetItem">GetItem</see>, <see cref="GetItemIndex">GetItemIndex</see> and allows to override also some properties such as <see cref="IsReadOnly"/> and <see cref="CanSetItem"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <seealso cref="IList{T}" />
    /// <seealso cref="Collection{T}" />
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [Serializable]
    public class VirtualCollection<T> : IList<T>, IList
#if !(NET35 || NET40)
        , IReadOnlyList<T>
#endif
    {
        #region Fields

        private readonly IList<T> items;
        [NonSerialized] private object syncRoot;

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the number of elements actually contained in the <see cref="VirtualCollection{T}"/>.
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// Gets whether the <see cref="VirtualCollection{T}" /> is read-only. Affects the behavior of <see cref="Add">Add</see>, <see cref="Insert">Insert</see>,
        /// <see cref="Remove">Remove</see>, <see cref="RemoveAt">RemoveAt</see> and <see cref="Clear">Clear</see> methods.
        /// <br/>The base implementation returns the <see cref="ICollection{T}.IsReadOnly"/> property of the underlying collection.
        /// </summary>
        /// <seealso cref="CanSetItem"/>
        public virtual bool IsReadOnly => items.IsReadOnly;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets whether an item can be set through the <see cref="P:KGySoft.Collections.ObjectModel.VirtualCollection`1.Item(System.Int32)">indexer</see>.
        /// <br/>The base implementation returns <see langword="true"/>&#160;if <see cref="IsReadOnly"/> returns <see langword="false"/>&#160;or when the wrapped collection is a one dimensional zero based array of <typeparamref name="T"/>;
        /// otherwise, returns <see langword="false"/>.
        /// </summary>
        /// <seealso cref="IsReadOnly"/>
        protected virtual bool CanSetItem => !IsReadOnly || items is T[];

        /// <summary>
        /// Gets the wrapped underlying collection maintained by this <see cref="VirtualCollection{T}"/> instance.
        /// </summary>
        protected IList<T> Items => items;

        #endregion

        #region Explicitly Implemented Interface Properties

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                {
                    if (items is ICollection c)
                        syncRoot = c.SyncRoot;
                    else
                        Interlocked.CompareExchange(ref syncRoot, new object(), null);
                }

                return syncRoot;
            }
        }

        bool ICollection.IsSynchronized => false;

        bool IList.IsFixedSize => items is IList list ? list.IsFixedSize : IsReadOnly;

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        /// <summary>
        /// Gets or sets the element at the specified <paramref name="index"/>.
        /// <br/>When read, calls the overridable <see cref="GetItem">GetItem</see> method, and when set, calls the overridable <see cref="SetItem">SetItem</see> method.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or is equal to or greater than <see cref="Count"/>.</exception>
        /// <exception cref="NotSupportedException">The value is set and <see cref="CanSetItem"/> returns <see langword="false"/>.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), Res.ArgumentOutOfRange);
                return GetItem(index);
            }
            set
            {
                if (!CanSetItem)
                    throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), Res.ArgumentOutOfRange);
                SetItem(index, value);
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object IList.this[int index]
        {
            get => this[index];
            set
            {
                if (!typeof(T).CanAcceptValue(value))
                    throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(value, typeof(T)), nameof(value));
                this[index] = (T)value;
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty instance of the <see cref="VirtualCollection{T}"/> class with a <see cref="CircularList{T}"/> internally.
        /// </summary>
        public VirtualCollection() => items = new CircularList<T>();

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualCollection{T}"/> class as a wrapper for the specified <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The list that is wrapped by the new collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null" />.</exception>
        public VirtualCollection(IList<T> list) => items = list ?? throw new ArgumentNullException(nameof(list), Res.ArgumentNull);

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Adds an object to the end of the <see cref="VirtualCollection{T}"/>.
        /// <br/>Calls the overridable <see cref="InsertItem">InsertItem</see> method.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="VirtualCollection{T}"/>.</param>
        /// <exception cref="NotSupportedException"><see cref="IsReadOnly"/> returns <see langword="true"/>.</exception>
        public void Add(T item)
        {
            if (IsReadOnly)
                throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            InsertItem(Count, item);
        }

        /// <summary>
        /// Inserts an element into the <see cref="VirtualCollection{T}"/> at the specified <paramref name="index"/>.
        /// <br/>Calls the overridable <see cref="InsertItem">InsertItem</see> method.</summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or greater than <see cref="Count"/>.</exception>
        /// <exception cref="NotSupportedException"><see cref="IsReadOnly"/> returns <see langword="true"/>.</exception>
        public void Insert(int index, T item)
        {
            if (IsReadOnly)
                throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index), Res.ArgumentOutOfRange);

            InsertItem(index, item);
        }

        /// <summary>
        /// Removes one occurrence of a specific object from the <see cref="VirtualCollection{T}"/>.
        /// <br/>Calls the overridable <see cref="GetItemIndex">GetItemIndex</see> and <see cref="RemoveItem">RemoveItem</see> methods.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="VirtualCollection{T}"/>.</param>
        /// <returns><see langword="true"/>, if an occurrence of <paramref name="item"/> was removed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="NotSupportedException"><see cref="IsReadOnly"/> returns <see langword="true"/>.</exception>
        public bool Remove(T item)
        {
            if (IsReadOnly)
                throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            int index = GetItemIndex(item);
            if (index < 0)
                return false;

            RemoveItem(index);
            return true;
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="VirtualCollection{T}"/>.
        /// <br/>Calls the overridable <see cref="RemoveItem">RemoveItem</see> method.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="NotSupportedException"><see cref="IsReadOnly"/> returns <see langword="true"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or is equal to or greater than <see cref="Count"/>.</exception>
        public void RemoveAt(int index)
        {
            if (IsReadOnly)
                throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), Res.ArgumentOutOfRange);

            RemoveItem(index);
        }

        /// <summary>
        /// Removes all elements from the <see cref="VirtualCollection{T}"/>.
        /// <br/>Calls the overridable <see cref="ClearItems">ClearItems</see> method.
        /// </summary>
        /// <exception cref="NotSupportedException"><see cref="IsReadOnly"/> returns <see langword="true"/>.</exception>
        public void Clear()
        {
            if (IsReadOnly)
                throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            ClearItems();
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of an occurrence within the entire <see cref="VirtualCollection{T}"/>.
        /// <br/>Calls the overridable <see cref="GetItemIndex">GetItemIndex</see> method.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="VirtualCollection{T}"/>. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns>The zero-based index of the found occurrence of <paramref name="item" /> within the entire <see cref="VirtualCollection{T}"/>, if found; otherwise, <c>-1</c>.</returns>
        public int IndexOf(T item) => GetItemIndex(item);

        /// <summary>
        /// Determines whether an element is in the <see cref="VirtualCollection{T}"/>.
        /// <br/>Calls the overridable <see cref="ContainsItem">ContainsItem</see> method.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="VirtualCollection{T}"/>. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item" /> is found in the <see cref="VirtualCollection{T}"/>; otherwise, <see langword="false" />.</returns>
        public bool Contains(T item) => ContainsItem(item);

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="VirtualCollection{T}"/>.
        /// <br/>The base implementation returns the enumerator of the underlying collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}" /> for the <see cref="VirtualCollection{T}"/>.</returns>
        /// <remarks>
        /// <note>If the <see cref="VirtualCollection{T}"/> was instantiated by the default constructor, then returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method; otherwise, it depends on the wrapped collection.</note>
        /// </remarks>
        public virtual IEnumerator<T> GetEnumerator() => items.GetEnumerator();

        /// <summary>
        /// Copies the entire <see cref="VirtualCollection{T}"/> to a compatible one-dimensional <see cref="Array"/>, starting at the specified <paramref name="arrayIndex"/> of the target <paramref name="array"/>.
        /// <br/>Calls the overridable <see cref="GetItem">GetItem</see> method for each index between zero and <see cref="Count"/>, excluding upper bound.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array" /> that is the destination of the elements copied from <see cref="VirtualCollection{T}"/>. The <see cref="Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0 equal to or greater than the length of <paramref name="array"/>.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source list is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            int length = Count;
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.ArgumentNull);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), Res.ArgumentOutOfRange);
            if (array.Length - arrayIndex < length)
                throw new ArgumentException(Res.ICollectionCopyToDestArrayShort, nameof(array));
            for (int i = 0; i < length; i++)
                array[arrayIndex++] = GetItem(i);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the zero-based index of an occurrence of the specified <paramref name="item"/> within the <see cref="VirtualCollection{T}"/>.
        /// <br/>The base implementation calls the <see cref="IList{T}.IndexOf">IndexOf</see> method of the underlying collection.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="VirtualCollection{T}"/>. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns>The zero-based index of the found occurrence of <paramref name="item" /> within the <see cref="VirtualCollection{T}"/>, if found; otherwise, <c>-1</c>.</returns>
        protected virtual int GetItemIndex(T item) => items.IndexOf(item);

        /// <summary>
        /// Gets whether the specified <paramref name="item"/> is in the <see cref="VirtualCollection{T}"/>.
        /// <br/>The base implementation calls the <see cref="GetItemIndex">GetItemIndex</see> method.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="VirtualCollection{T}"/>. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item" /> is found in the <see cref="VirtualCollection{T}"/>; otherwise, <see langword="false" />.</returns>
        protected bool ContainsItem(T item) => GetItemIndex(item) >= 0;

        /// <summary>
        /// Gets the element at the specified <paramref name="index"/>.
        /// <br/>The base implementation gets the element at the specified <paramref name="index"/> by calling the <see cref="P:System.Collections.Generic.IList`1.Item(System.Int32)">indexer</see> of the underlying collection.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified <paramref name="index"/>.</returns>
        protected virtual T GetItem(int index) => items[index];

        /// <summary>
        /// Replaces the <paramref name="item"/> at the specified <paramref name="index"/>.
        /// <br/>The base implementation sets the <paramref name="item"/> in the underlying collection by its <see cref="P:System.Collections.Generic.IList`1.Item(System.Int32)">indexer</see>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index.</param>
        protected virtual void SetItem(int index, T item) => items[index] = item;

        /// <summary>
        /// Inserts an element into the <see cref="VirtualCollection{T}"/> at the specified <paramref name="index"/>.
        /// <br/>The base implementation calls the <see cref="IList{T}.Insert">Insert</see> method of the underlying collection.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        protected virtual void InsertItem(int index, T item) => items.Insert(index, item);

        /// <summary>
        /// Removes the element at the specified <paramref name="index"/> from the <see cref="VirtualCollection{T}"/>.
        /// <br/>The base implementation calls the <see cref="IList{T}.RemoveAt">RemoveAt</see> method of the underlying collection.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        protected virtual void RemoveItem(int index) => items.RemoveAt(index);

        /// <summary>
        /// Removes all elements from the <see cref="VirtualCollection{T}"/>.
        /// <br/>The base implementation calls the <see cref="ICollection{T}.Clear">Clear</see> method of the underlying collection.
        /// </summary>
        protected virtual void ClearItems() => items.Clear();

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.ArgumentNull);
            if (array.Rank != 1)
                throw new ArgumentException(Res.ICollectionCopyToSingleDimArrayOnly, nameof(array));

            if (array is T[] typedArray)
            {
                CopyTo(typedArray, index);
                return;
            }

            int length = Count;
            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), Res.ArgumentOutOfRange);
            if (array.Length - index < length)
                throw new ArgumentException(Res.ICollectionCopyToDestArrayShort, nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(Res.ICollectionCopyToSingleDimArrayOnly, nameof(array));

            if (array is object[] objectArray)
            {
                for (int i = 0; i < length; i++)
                    objectArray[index++] = GetItem(i);
                return;
            }

            throw new ArgumentException(Res.ICollectionArrayTypeInvalid, nameof(array));
        }

        bool IList.Contains(object value) => typeof(T).CanAcceptValue(value) && Contains((T)value);

        int IList.IndexOf(object value) => typeof(T).CanAcceptValue(value) ? IndexOf((T)value) : -1;

        int IList.Add(object value)
        {
            if (!typeof(T).CanAcceptValue(value))
                throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(value, typeof(T)), nameof(value));
            T item = (T)value;
            Add(item);
            return GetItemIndex(item);
        }

        void IList.Insert(int index, object value)
        {
            if (!typeof(T).CanAcceptValue(value))
                throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(value, typeof(T)), nameof(value));
            Insert(index, (T)value);
        }

        void IList.Remove(object value)
        {
            if (!typeof(T).CanAcceptValue(value))
                throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(value, typeof(T)), nameof(value));
            Remove((T)value);
        }

        #endregion

        #endregion
    }
}
