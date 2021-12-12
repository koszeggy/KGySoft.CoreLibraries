#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CircularList.cs
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Implements an <see cref="IList{T}"/> where inserting/removing at the beginning/end position are O(1) operations.
    /// <see cref="CircularList{T}"/> is fully compatible with <see cref="List{T}"/> but has a better performance in several cases.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <remarks>
    /// <para>Circularity means a dynamic start/end position in the internal store. If inserting/removing elements is required typically
    /// at the first/last position, then using <see cref="CircularList{T}"/> could be a good choice because these operations have an O(1) cost. Inserting/removing at
    /// other positions have generally O(n) cost, though these operations are optimized for not moving more than n/2 elements.</para>
    /// <para><see cref="CircularList{T}"/> is fully compatible with <see cref="List{T}"/> as it implements all of the public members of <see cref="List{T}"/>.</para>
    /// <para><see cref="CircularList{T}"/> can be a good option instead of <see cref="LinkedList{T}"/>, too. Generally <see cref="LinkedList{T}"/> is considered
    /// to be a good choice when elements should be inserted/removed at the middle of the list. However, unless a direct reference is kept to a
    /// <see cref="LinkedListNode{T}"/> to be removed or before/after which the new element has to be inserted, then the insert/remove operation will be
    /// very slow in <see cref="LinkedList{T}"/>, as it has no indexed access, and the node has to be located by iterating the elements. Therefore in most cases
    /// <see cref="CircularList{T}"/> outperforms <see cref="LinkedList{T}"/> in most practical cases.</para>
    /// <para>Adding elements to the first/last position are generally O(1) operations. If the capacity needs to be increased to accommodate the
    /// new element, adding becomes an O(n) operation, where n is <see cref="Count"/>. Though when elements are continuously added to the list,
    /// the amortized cost of adding methods (<see cref="Add">Add</see>, <see cref="AddLast(T)">AddLast</see>, <see cref="AddFirst(T)">AddFirst</see> or <see cref="Insert">Insert</see>
    /// at the first/last position) is O(1) due to the low frequency of increasing capacity. For example, when 20 million
    /// items are added to a <see cref="CircularList{T}"/> that was created by the default constructor, capacity is increased only 23 times.</para>
    /// <para>Inserting more elements are also optimized. If the length of the <see cref="CircularList{T}"/> is n and the length of the collection to insert is m, then the cost of <see cref="InsertRange">InsertRange</see>
    /// to the first or last position is O(m). When the collection is inserted in the middle of the <see cref="CircularList{T}"/>, the cost is O(Max(n, m)), and in practice no more than n/2 elements are moved in the <see cref="CircularList{T}"/>.
    /// In contrast, inserting a collection into the first position of a <see cref="List{T}"/> moves all of the already existing elements. And if the collection to insert does not implement <see cref="ICollection{T}"/>, then
    /// <see cref="List{T}"/> works with an O(n * m) cost as it will insert the elements one by one, shifting the elements by 1 in every iteration.</para>
    /// <para><see cref="CircularList{T}"/> provides an O(1) solution also for emptying the collection: the <see cref="Reset">Reset</see> method clears all elements and resets the internal capacity to 0 in one
    /// quick step. The effect is the same as calling the <see cref="Clear">Clear</see> and <see cref="TrimExcess">TrimExcess</see> methods in a row but the latter solution has an O(n) cost.</para>
    /// <para>When a list is populated only by the <see cref="Add">Add</see> method or by the indexer, and then it is never modified, <see cref="List{T}"/> class can have a slightly better
    /// performance. Though when the list is enumerated as an <see cref="IEnumerable{T}"/> implementation (occurs for example, when the list is used in LINQ queries or when used
    /// as an <see cref="IList{T}"/> instance), then <see cref="CircularList{T}"/> can be a better choice than <see cref="List{T}"/> because the enumerator of
    /// <see cref="List{T}"/> class has worse performance when it boxed into an <see cref="IEnumerator{T}"/> reference. While the <see cref="GetEnumerator">GetEnumerator</see> method of
    /// <see cref="CircularList{T}"/> returns a value type enumerator (similarly to the <see cref="List{T}"/> class), when the enumerator is obtained via the <see cref="IEnumerable{T}"/>
    /// interface, the <see cref="CircularList{T}"/> returns a reference type to avoid boxing and to provide a better performance.
    /// </para>
    /// </remarks>
    /// <div style="display:none"><example/></div>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; T = {typeof(" + nameof(T) + ").Name}")]
    [Serializable]
    public sealed class CircularList<T> : ISupportsRangeList<T>, IList
    {
        #region Nested types

        #region ComparisonWrapper class

        /// <summary>
        /// Wraps a <see cref="Comparison{T}"/> delegate into an <see cref="IComparer{T}"/> implementation.
        /// </summary>
        private sealed class ComparisonWrapper : IComparer<T>
        {
            #region Fields

            private readonly Comparison<T> comparison;

            #endregion

            #region Constructors

            internal ComparisonWrapper(Comparison<T> comparison) => this.comparison = comparison;

            #endregion

            #region Methods

            public int Compare(T? x, T? y) => comparison(x!, y!);

            #endregion
        }

        #endregion

        #region EnumeratorAsReference class

        /// <summary>
        /// Enumerates the elements of a <see cref="CircularList{T}"/>. This enumerator is exactly the same as <see cref="Enumerator"/>,
        /// but is implemented as a reference type. This is returned when enumerator is requested as an <see cref="IEnumerator{T}"/> interface
        /// to avoid performance hit of boxing.
        /// </summary>
        [Serializable]
        private class EnumeratorAsReference : IEnumerator<T>
        {
            #region Fields

            private readonly CircularList<T> list;
            private readonly int version;
            private readonly int capacity;

            private int index;
            private int steps;
            [AllowNull]private T current = default!;

            #endregion

            #region Properties

            #region Public Properties

            public T Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object? IEnumerator.Current
            {
                get
                {
                    if (steps == 0 || steps > list.Count)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal EnumeratorAsReference(CircularList<T> list)
            {
                this.list = list;
                index = list.startIndex;
                version = list.version;
                capacity = list.items.Length;
            }

            #endregion

            #region Methods

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (version != list.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                if (steps < list.size)
                {
                    current = list.items[index];
                    index += 1;
                    if (index == capacity)
                        index = 0;
                    steps += 1;
                    return true;
                }

                steps = list.size + 1;
                current = default(T);
                return false;
            }

            public void Reset()
            {
                if (version != list.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = list.startIndex;
                steps = 0;
                current = default(T);
            }

            #endregion
        }

        #endregion

        #region SimpleEnumeratorAsReference class

        /// <summary>
        /// Enumerates the elements of a <see cref="CircularList{T}"/> when start index is 0. This enumerator is returned when enumerator is
        /// requested as an <see cref="IEnumerator{T}"/> interface to avoid performance hit of boxing.
        /// </summary>
        [Serializable]
        private class SimpleEnumeratorAsReference : IEnumerator<T>
        {
            #region Fields

            private readonly CircularList<T> list;
            private readonly int version;

            private int index;
            [AllowNull]private T current = default!;

            #endregion

            #region Properties

            #region Public Properties

            public T Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object? IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index > list.Count)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal SimpleEnumeratorAsReference(CircularList<T> list)
            {
                this.list = list;
                version = list.version;
            }

            #endregion

            #region Methods

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (version != list.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                if (index < list.size)
                {
                    current = list.items[index];
                    index += 1;
                    return true;
                }

                index = list.size + 1;
                current = default(T);
                return false;
            }

            public void Reset()
            {
                if (version != list.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = 0;
                current = default(T);
            }

            #endregion
        }

        #endregion

        #region Enumerator struct

        /// <summary>
        /// Enumerates the elements of a <see cref="CircularList{T}"/>.
        /// </summary>
        [Serializable]
        public struct Enumerator : IEnumerator<T>
        {
            #region Fields

            private readonly CircularList<T> list;
            private readonly int version;
            private readonly int capacity;
            private readonly int count;

            private int index;
            private int steps;
            [AllowNull]private T current;

            #endregion

            #region Properties

            #region Public Properties

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public readonly T Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object? IEnumerator.Current
            {
                get
                {
                    if (steps == 0 || steps > list.Count)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(CircularList<T> list)
            {
                this.list = list;
                index = list.startIndex;
                version = list.version;
                capacity = list.items.Length;
                count = list.size;
                steps = 0;
                current = default(T);
            }

            #endregion

            #region Methods

            /// <summary>
            /// Releases the enumerator
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/>&#160;if the enumerator was successfully advanced to the next element; <see langword="false"/>&#160;if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            [MethodImpl(MethodImpl.AggressiveInlining)]
            public bool MoveNext()
            {
                if (version != list.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                if (steps < count)
                {
                    current = list.items[index];
                    index += 1;
                    if (index == capacity)
                        index = 0;
                    steps += 1;
                    return true;
                }

                steps = count + 1;
                current = default(T);
                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                if (version != list.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = list.startIndex;
                steps = 0;
                current = default(T);
            }

            #endregion
        }

        #endregion

        #endregion

        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        #region Static Fields

        private static readonly Type typeOfT = typeof(T);

        private static BinarySearchHelper<T>? binarySearchHelper;

        #endregion

        #region Instance Fields

        private T[] items;
        private int size;
        private int startIndex;
        private int version;
        [NonSerialized]
        private object? syncRoot;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Static Properties

        private static BinarySearchHelper<T> BinarySearchHelperInstance
        {
            get
            {
                if (binarySearchHelper == null)
                {
                    if (typeof(IComparable<T>).IsAssignableFrom(typeOfT))
                    {
                        Type typeHelper = typeof(ComparableBinarySearchHelper<>).GetGenericType(typeOfT);
                        binarySearchHelper = (BinarySearchHelper<T>)Activator.CreateInstance(typeHelper, true)!;
                    }
                    else
                        binarySearchHelper = new BinarySearchHelper<T>();
                }

                return binarySearchHelper;
            }
        }

        #endregion

        #region Instance Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets the actual size of the internal storage of held elements.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Capacity is set to a value that is less than <see cref="Count"/>. </exception>
        /// <remarks>
        /// <para>Capacity is the number of elements that the <see cref="CircularList{T}"/> can store before resizing is required, whereas
        /// <see cref="Count"/> is the number of elements that are actually in the <see cref="CircularList{T}"/>.</para>
        /// <para>Capacity is always greater than or equal to <see cref="Count"/>. If <see cref="Count"/> exceeds <see cref="Capacity"/> while adding elements,
        /// the capacity is increased by automatically reallocating the internal array before copying the old elements and adding the new elements.</para>
        /// <para>If the capacity is significantly larger than the count and you want to reduce the memory used by the <see cref="CircularList{T}"/>,
        /// you can decrease capacity by calling the <see cref="TrimExcess">TrimExcess</see> method or by setting the <see cref="Capacity"/> property explicitly.
        /// When the value of <see cref="Capacity"/> is set explicitly, the internal array is also reallocated to accommodate the specified capacity,
        /// and all the elements are copied.</para>
        /// <para>Retrieving the value of this property is an O(1) operation; setting the property is an O(n) operation, where n is the number of stored elements.</para>
        /// </remarks>
        public int Capacity
        {
            get => items.Length;
            set
            {
                if (value == items.Length)
                    return;
                if (value < size)
                    Throw.ArgumentOutOfRangeException(Argument.value, Res.CircularListCapacityTooSmall);

                if (value > 0)
                {
                    T[] newItems = new T[value];
                    if (size > 0)
                        CopyTo(newItems);

                    items = newItems;
                    startIndex = 0;
                }
                else
                    items = Reflector.EmptyArray<T>();
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the list.
        /// </summary>
        public int Count => size;

        #endregion

        #region Internal Properties

        internal T[] Items => items;
        internal int Version => version;
        internal int StartIndex => startIndex;

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<T>.IsReadOnly => false;
        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

        #endregion

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        /// <summary>
        /// Gets or sets the element at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero or greater or equal to <see cref="Count"/>.</exception>
        public T this[int index]
        {
            get
            {
                // casting to uint reduces the range check by one
                if ((uint)index >= (uint)size)
                    // the getter is 2.5x times faster if the throw expression is in another method
                    Throw.ArgumentOutOfRangeException(Argument.index);

                // even inlining the rest or just calling ElementAt are slower than this solution
                return startIndex == 0 ? items[index] : ElementAtNonZeroStart(index);
            }
            set
            {
                // casting to uint reduces the range check by one
                if ((uint)index >= (uint)size)
                    Throw.ArgumentOutOfRangeException(Argument.index);

                SetElementAt(index, value);
                version += 1;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                Throw.ThrowIfNullIsInvalid<T>(value);
                try
                {
                    this[index] = (T)value!;
                }
                catch (InvalidCastException)
                {
                    Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeOfT));
                }
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="CircularList{T}"/>.
        /// </summary>
        public CircularList() => items = Reflector.EmptyArray<T>();

        /// <summary>
        /// Creates a new instance of <see cref="CircularList{T}"/> that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public CircularList(int capacity) => items = new T[capacity];

        /// <summary>
        /// Creates a new instance of <see cref="CircularList{T}"/> with the elements of provided <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new <see cref="CircularList{T}"/>.</param>
        public CircularList(IEnumerable<T> collection)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);

            if (collection is ICollection<T> coll)
            {
                size = coll.Count;
                items = new T[size];
                coll.CopyTo(items, 0);
                return;
            }

            items = Reflector.EmptyArray<T>();
            foreach (T item in collection)
                AddLast(item);
        }

        #endregion

        #region Methods

        #region Static Methods

        private static bool CanAccept(object? value) => value is T || value == null && default(T) == null;

        #endregion

        #region Instance Methods

        #region Public Methods

        #region Add

        /// <summary>
        /// Adds an <paramref name="item"/> to the end of the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="item">The item to add to the <see cref="CircularList{T}"/>.</param>
        /// <remarks>
        /// <para><see cref="CircularList{T}"/> accepts <see langword="null"/>&#160;as a valid value for reference and nullable types and allows duplicate elements.</para>
        /// <para>If <see cref="Count"/> already equals <see cref="Capacity"/>, the capacity of the list is increased by automatically reallocating the internal array, and the existing elements are copied to the new array before the new element is added.</para>
        /// <para>If <see cref="Count"/> is less than <see cref="Capacity"/>, this method is an O(1) operation. If the capacity needs to be increased to accommodate the
        /// new element, this method becomes an O(n) operation, where n is <see cref="Count"/>.
        /// When adding elements continuously, the amortized cost of this method is O(1) due to the low frequency of increasing capacity. For example, when 20 million
        /// items are added to a <see cref="CircularList{T}"/> that was created by the default constructor, capacity is increased only 23 times.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void Add(T item) => AddLast(item);

        /// <summary>
        /// Adds an <paramref name="item"/> to the end of the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="item">The item to add to the <see cref="CircularList{T}"/>.</param>
        /// <remarks>
        /// <para><see cref="CircularList{T}"/> accepts <see langword="null"/>&#160;as a valid value for reference and nullable types and allows duplicate elements.</para>
        /// <para>If <see cref="Count"/> already equals <see cref="Capacity"/>, the capacity of the list is increased by automatically reallocating the internal array, and the existing elements are copied to the new array before the new element is added.</para>
        /// <para>If <see cref="Count"/> is less than <see cref="Capacity"/>, this method is an O(1) operation. If the capacity needs to be increased to accommodate the
        /// new element, this method becomes an O(n) operation, where n is <see cref="Count"/>.
        /// When adding elements continuously, the amortized cost of this method is O(1) due to the low frequency of increasing capacity. For example, when 20 million
        /// items are added to a <see cref="CircularList{T}"/> that was created by the default constructor, capacity is increased only 23 times.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void AddLast(T item)
        {
            if (size == items.Length)
                EnsureCapacity(size + 1);

            if (startIndex == 0)
                items[size] = item;
            else
            {
                // faster than items[(startIndex + size++) % length] = item;
                int pos = startIndex + size;
                if (pos >= items.Length)
                    pos -= items.Length;
                items[pos] = item;
            }

            size += 1;
            version += 1;
        }

        /// <summary>
        /// Adds an <paramref name="item"/> to the beginning of the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="item">The item to add to the <see cref="CircularList{T}"/>.</param>
        /// <remarks>
        /// <para><see cref="CircularList{T}"/> accepts <see langword="null"/>&#160;as a valid value for reference and nullable types and allows duplicate elements.</para>
        /// <para>If <see cref="Count"/> already equals <see cref="Capacity"/>, the capacity of the list is increased by automatically reallocating the internal array,
        /// and the existing elements are copied to the new array before the new element is added.</para>
        /// <para>If <see cref="Count"/> is less than <see cref="Capacity"/>, this method is an O(1) operation. If the capacity needs to be increased to accommodate the
        /// new element, this method becomes an O(n) operation, where n is <see cref="Count"/>.
        /// When adding elements continuously, the amortized cost of this method is O(1) due to the low frequency of increasing capacity. For example, when 20 million
        /// items are added to a <see cref="CircularList{T}"/> that was created by the default constructor, capacity is increased only 23 times.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void AddFirst(T item)
        {
            if (size == items.Length)
                EnsureCapacity(size + 1);

            if (startIndex == 0)
            {
                if (items.Length > 1)
                    startIndex = items.Length - 1;
            }
            else
                startIndex -= 1;
            items[startIndex] = item;
            size += 1;
            version += 1;
        }

        /// <summary>
        /// Adds a <paramref name="collection"/> to the end of the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="collection">The collection to add to the <see cref="CircularList{T}"/>.</param>
        /// <remarks>
        /// <para>If the length of the <see cref="CircularList{T}"/> is n and the length of the collection to insert is m, then this method has O(m) cost.</para>
        /// <para>If capacity increase is needed (considering actual list size) the cost is O(Max(n, m)).</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);

            AddLast(collection);
        }

        /// <summary>
        /// Inserts an <paramref name="item"/> to the <see cref="CircularList{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="CircularList{T}"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the list.</exception>
        /// <remarks>Inserting an item at the first or last position are O(1) operations if no capacity increase is needed. Otherwise, insertion is an O(n) operation.</remarks>
        public void Insert(int index, T item)
        {
            if (index == 0)
                AddFirst(item);
            else if (index == size)
                AddLast(item);
            else
                InsertAt(index, item);
        }

        /// <summary>
        /// Inserts a <paramref name="collection"/> into the <see cref="CircularList{T}"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="collection">The collection to insert into the list.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>If the length of the <see cref="CircularList{T}"/> is n and the length of the collection to insert is m, then inserting to the first or last position has O(m) cost.</para>
        /// <para>If capacity increase is needed (considering actual list size), or when the collection is inserted in the middle of the <see cref="CircularList{T}"/>, the cost is O(Max(n, m)), and in practice no more than n/2 elements are moved.</para>
        /// </remarks>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);

            if (index == 0)
            {
                AddFirst(collection);
                return;
            }

            if (index == size)
            {
                AddLast(collection);
                return;
            }

            // if we are here, shifting is necessary
            if ((uint)index > (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);

            T[]? asArray = ReferenceEquals(collection, this) ? ToArray() : collection as T[];
            ICollection<T>? asCollection = asArray != null ? null : collection as ICollection<T>;

            // ReSharper disable PossibleMultipleEnumeration - collection.ToArray is called only if not evaluated yet
            // to prevent O(n * m) cost with inserting IEnumerable elements one by one we create a temp buffer
            if (asArray == null && asCollection == null)
                asArray = collection.ToArray();

            int collectionSize = asArray?.Length ?? asCollection!.Count;
            if (collectionSize == 0)
                return;

            int capacity = items.Length;
            if (size + collectionSize > capacity)
            {
                IncreaseCapacityWithInsert(index, (asArray ?? asCollection)!);
                return;
            }

            // calculating position
            int pos = startIndex + index;
            if (pos >= capacity)
                pos -= capacity;

            // optimized for minimal data moving
            if (index >= (size >> 1))
                ShiftUp(pos, size - index, collectionSize);
            else
            {
                // decreasing startIndex and pos
                startIndex -= collectionSize;
                if (startIndex < 0)
                    startIndex += capacity;

                int topIndex = pos > 0 ? pos - 1 : capacity - 1;

                pos -= collectionSize;
                if (pos < 0)
                    pos += capacity;

                ShiftDown(topIndex, index, collectionSize);
            }

            // if non-array collection that fits into list without wrapping
            if (asCollection != null && pos + collectionSize <= capacity)
                asCollection.CopyTo(items, pos);
            // otherwise, as array copied up to two sessions
            else
            {
                asArray ??= collection.ToArray();
                int carry = pos + collectionSize - capacity;
                asArray.CopyElements(0, items, pos, carry <= 0 ? collectionSize : collectionSize - carry);
                if (carry > 0)
                    asArray.CopyElements(collectionSize - carry, items, 0, carry);
            }
            // ReSharper restore PossibleMultipleEnumeration

            size += collectionSize;
            version += 1;
        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes the first occurrence of the specific <paramref name="item"/> from the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="CircularList{T}"/>.</param>
        /// <returns>
        /// <see langword="true"/>&#160;if <paramref name="item"/> was successfully removed from the <see cref="CircularList{T}"/>; otherwise, <see langword="false"/>.
        /// This method also returns false if <paramref name="item"/> is not found in the original list.
        /// </returns>
        /// <remarks>
        /// <para>If the position of the element to remove is known it is recommended to use the <see cref="RemoveAt">RemoveAt</see> method instead.</para>
        /// <para>This method has an O(n) cost.</para>
        /// </remarks>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the last element from the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <returns><see langword="true"/>, if the list was not empty before the removal, otherwise, <see langword="false"/>.</returns>
        /// <remarks>This method has an O(1) cost.</remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public bool RemoveLast()
        {
            if (size == 0)
                return false;

            size -= 1;
            int pos = startIndex + size;
            if (pos >= items.Length)
                pos -= items.Length;

            if (Reflector<T>.IsManaged)
                items[pos] = default(T)!;

            if (size == 0)
                startIndex = 0;
            version += 1;
            return true;
        }

        /// <summary>
        /// Removes the first element of the see <see cref="CircularList{T}"/>.
        /// </summary>
        /// <returns><see langword="true"/>, if the list was not empty before the removal, otherwise, <see langword="false"/>.</returns>
        /// <remarks>This method has an O(1) cost.</remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public bool RemoveFirst()
        {
            if (size == 0)
                return false;

            if (Reflector<T>.IsManaged)
                items[startIndex] = default(T)!;
            startIndex += 1;

            size -= 1;
            if (size == 0 || startIndex == items.Length)
                startIndex = 0;
            version += 1;
            return true;
        }

        /// <summary>
        /// Removes the item from the <see cref="CircularList{T}"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.</exception>
        /// <remarks>Removing an item at the first or last position are O(1) operations. At other positions removal is an O(n) operation, though
        /// the method is optimized for not moving more than n/2 elements.</remarks>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);

            if (index == 0)
                RemoveFirst();
            else if (index == size - 1)
                RemoveLast();
            else
                RemoveMiddle(index);
        }

        /// <summary>
        /// Removes <paramref name="count"/> amount of items from the <see cref="CircularList{T}"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the first item to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.
        /// <br/>-or-
        /// <br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the list.</exception>
        /// <remarks>Removing items at the first or last positions are O(1) operations considering list size. At other positions removal is an O(n) operation, though
        /// the method is optimized for not moving more than n/2 elements.</remarks>
        public void RemoveRange(int index, int count)
        {
            if ((uint)index >= (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > size)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count == 0)
                return;

            if (index == 0)
                RemoveFirst(count);
            else if (index + count == size)
                RemoveLast(count);
            else
                RemoveMiddle(index, count);
        }

        /// <summary>
        /// Removes all the elements from the <see cref="CircularList{T}"/> that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements to remove.</param>
        /// <returns>The number of elements removed from the list.</returns>
        /// <remarks>
        /// <para>The <see cref="Predicate{T}"/> is a delegate to a method that returns <see langword="true"/>&#160;if the object passed to it matches the conditions defined in the delegate.
        /// The elements of the current <see cref="CircularList{T}"/> are individually passed to the <see cref="Predicate{T}"/> delegate, and the elements that match the conditions
        /// are removed from the list.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null!)
                Throw.ArgumentNullException(Argument.match);

            // the first free slot in items array
            int freeIndex = 0;

            // Find the first item which needs to be removed.
            while (freeIndex < size && !match(ElementAt(freeIndex)))
                freeIndex += 1;

            if (freeIndex >= size)
                return 0;

            int current = freeIndex + 1;
            while (current < size)
            {
                // Find the first item which needs to be kept.
                while (current < size && match(ElementAt(current)))
                    current += 1;

                // copy item to the free slot.
                if (current < size)
                {
                    SetElementAt(freeIndex, ElementAt(current));
                    freeIndex += 1;
                    current += 1;
                }
            }

            int result = size - freeIndex;

            // RemoveLast will adjust size and version
            RemoveLast(result);
            return result;
        }

        /// <summary>
        /// Removes all items from the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <remarks>
        /// <para><see cref="Count"/> is set to 0, and references to other objects from elements of the collection are also released.</para>
        /// <para>If <typeparamref name="T"/> is a value type and contains no managed references, then this method is an O(1) operation.
        /// Otherwise, this method is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// <para><see cref="Capacity"/> remains unchanged. To reset the capacity of the list to 0 as well, call the <see cref="Reset"/> method instead, which is an O(1) operation.
        /// Calling <see cref="TrimExcess">TrimExcess</see> after <see cref="Clear">Clear</see> also resets the list, though <see cref="Clear">Clear</see> has more cost.</para>
        /// </remarks>
        public void Clear()
        {
            if (size == 0)
                return;

            if (Reflector<T>.IsManaged)
            {
                int carry = startIndex + size - items.Length;
                Array.Clear(items, startIndex, carry <= 0 ? size : size - carry);
                if (carry > 0)
                    Array.Clear(items, 0, carry);
            }

            size = 0;
            startIndex = 0;
            version += 1;
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the list, if that number (<see cref="Count"/>) is less than the 90 percent of <see cref="Capacity"/>.
        /// </summary>
        /// <remarks>
        /// <para>This method can be used to minimize a collection's memory overhead if no new elements will be added to the collection.
        /// The cost of reallocating and copying a large list can be considerable, however, so the <see cref="TrimExcess">TrimExcess</see> method does nothing if the list is
        /// at more than 90 percent of capacity. This avoids incurring a large reallocation cost for a relatively small gain.</para>
        /// <para>This method is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// <para>To reset a list to its initial state, call the <see cref="Reset">Reset</see> method. Calling the <see cref="Clear">Clear</see> and <see cref="TrimExcess">TrimExcess</see> methods has the same effect; however,
        /// <see cref="Reset">Reset</see> method is an O(1) operation, while <see cref="Clear">Clear</see> is an O(n) operation. Trimming an empty list sets the capacity of the list to 0.</para>
        /// <para>The capacity can also be set using the <see cref="Capacity"/> property.</para>
        /// </remarks>
        public void TrimExcess()
        {
            int threshold = (int)(items.Length * 0.9);
            if (size < threshold)
                Capacity = size;
        }

        /// <summary>
        /// Removes all items from the list and resets the <see cref="Capacity"/> of the list to 0.
        /// </summary>
        /// <remarks>
        /// <para><see cref="Count"/> and <see cref="Capacity"/> are set to 0, and references to other objects from elements of the collection are also released.</para>
        /// <para>This method is an O(1) operation.</para>
        /// <para>Calling <see cref="Clear">Clear</see> and then <see cref="TrimExcess">TrimExcess</see> methods also resets the list, though <see cref="Clear">Clear</see> is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        public void Reset()
        {
            if (size == 0)
                return;
            items = Reflector.EmptyArray<T>();
            startIndex = 0;
            size = 0;
            version += 1;
        }

        #endregion

        #region Replace

        /// <summary>
        /// Removes <paramref name="count"/> amount of items from the <see cref="CircularList{T}"/> at the specified <paramref name="index"/> and
        /// inserts the specified <paramref name="collection"/> at the same position. The number of elements in <paramref name="collection"/> can be different from the amount of removed items.
        /// </summary>
        /// <param name="index">The zero-based index of the first item to remove and also the index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <param name="collection">The collection to insert into the list.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularList{T}"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>If the length of the <see cref="CircularList{T}"/> is n and the length of the collection to insert is m, then replacement at the first or last position has O(m) cost.</para>
        /// <para>If the elements to remove and to add have the same size, then the cost is O(m) at any position.</para>
        /// <para>If capacity increase is needed (considering actual list size), or when the replacement of different amount of elements to remove and insert is performed in the middle of the <see cref="CircularList{T}"/>, the cost is O(Max(n, m)), and in practice no more than n/2 elements are moved.</para>
        /// </remarks>
        public void ReplaceRange(int index, int count, IEnumerable<T> collection)
        {
            if (count == 0)
            {
                InsertRange(index, collection);
                return;
            }

            if ((uint)index >= (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > size)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (collection == null!)
                Throw.ArgumentNullException(Argument.collection);

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                // Copying elements while possible
                int elementsCopied = 0;
                while (count > 0 && enumerator.MoveNext())
                {
                    SetElementAt(index + elementsCopied, enumerator.Current);
                    elementsCopied += 1;
                    count -= 1;
                }

                // all inserted, removing the rest
                if (count > 0)
                {
                    RemoveRange(index + elementsCopied, count);
                    return;
                }

                // all removed (overwritten), inserting the rest
                IList<T> rest = collection is IList<T> list ? new ListSegment<T>(list, elementsCopied) : enumerator.RestToList();
                if (rest.Count > 0)
                {
                    InsertRange(index + elementsCopied, rest);
                    return;
                }

                // elements to replace had the same size
                version += 1;
            }
        }

        #endregion

        #region Lookup

        /// <summary>
        /// Determines whether the list contains the specific <paramref name="item"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if <paramref name="item"/> is found in the <see cref="CircularList{T}"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="CircularList{T}"/>.</param>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public bool Contains(T item) => IndexOf(item) >= 0;

        /// <summary>
        /// Determines the index of a specific item in the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="CircularList{T}"/>.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <remarks>
        /// <para>The list is searched forward starting at the first element and ending at the last element.</para>
        /// <para>Only in .NET Framework, this method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/>&#160;type.
        /// Otherwise, the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> will be used.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        /// </remarks>
        public int IndexOf(T item)
        {
#if NETFRAMEWORK
            IEqualityComparer<T> comparer = ComparerHelper<T>.EqualityComparer;
            if (!ReferenceEquals(comparer, EqualityComparer<T>.Default))
                return FindIndex(0, size, enumItem => comparer.Equals(enumItem, item)); 
#endif

            int carry = startIndex + size - items.Length;

            int result = Array.IndexOf(items, item, startIndex, carry <= 0 ? size : size - carry);
            if (result >= 0)
                return result - startIndex;
            if (carry > 0)
                result = Array.IndexOf(items, item, 0, carry);
            if (result >= 0)
                return items.Length - startIndex + result;
            return result;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the range
        /// of elements in the <see cref="CircularList{T}"/> that extends from the specified index to the last element.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="CircularList{T}"/>.</param>
        /// <param name="index">The zero-based starting index of the search.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of item within the range of elements in the <see cref="CircularList{T}"/>
        /// that extends from index to the last element, if found; otherwise, –1.
        /// </returns>
        /// <para>The list is searched forward starting at <paramref name="index"/> and ending at the last element.</para>
        /// <para>Only in .NET Framework, this method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/>&#160;type.
        /// Otherwise, the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> will be used.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int IndexOf(T item, int index) => IndexOf(item, index, size - index);

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the range
        /// of elements in the <see cref="CircularList{T}"/> that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="CircularList{T}"/>.</param>
        /// <param name="index">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found within the range of elements in the list that starts at <paramref name="index"/>
        /// and contains <paramref name="count"/> number of elements, if found; otherwise, -1.
        /// </returns>
        /// <para>The list is searched forward starting at <paramref name="index"/> and ending at <paramref name="index"/> plus <paramref name="count"/> minus 1,
        /// if <paramref name="count"/> is greater than 0.</para>
        /// <para>Only in .NET Framework, this method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/>&#160;type.
        /// Otherwise, the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> will be used.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int IndexOf(T item, int index, int count)
        {
            if ((uint)index > (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0 || index > size - count)
                Throw.ArgumentOutOfRangeException(Argument.count);

#if NETFRAMEWORK
            IEqualityComparer<T> comparer = ComparerHelper<T>.EqualityComparer;
            if (!ReferenceEquals(comparer, EqualityComparer<T>.Default))
                return FindIndex(index, count, enumItem => comparer.Equals(enumItem, item)); 
#endif

            // every searched element is carried
            int capacity = items.Length;
            int start = startIndex + index;
            int result;
            if (start >= capacity)
            {
                start -= capacity;
                result = Array.IndexOf(items, item, start, count);
                if (result >= 0)
                    return capacity - startIndex + result;

                return result;
            }

            // there are also not carried elements to search
            int carry = start + count - capacity;

            result = Array.IndexOf(items, item, start, carry <= 0 ? count : count - carry);
            if (result >= 0)
                return result - startIndex;
            if (carry > 0)
                result = Array.IndexOf(items, item, 0, carry);
            if (result >= 0)
                return capacity - startIndex + result;
            return result;
        }

        /// <summary>
        /// Determines the index of the last occurrence of a specific item in the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="CircularList{T}"/>.</param>
        /// <returns>
        /// The index of the last occurrence of the <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <para>The list is searched backward starting at the last element and ending at the first element.</para>
        /// <para>Only in .NET Framework, this method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/>&#160;type.
        /// Otherwise, the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> will be used.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int LastIndexOf(T item)
        {
            int carry = startIndex + size - items.Length;

#if NETFRAMEWORK
            IEqualityComparer<T> comparer = ComparerHelper<T>.EqualityComparer;
            if (!ReferenceEquals(comparer, EqualityComparer<T>.Default))
                return FindLastIndex(size - 1, size, enumItem => comparer.Equals(enumItem, item)); 
#endif

            int result;
            if (carry > 0)
            {
                result = Array.LastIndexOf(items, item, carry - 1, carry);
                if (result >= 0)
                    return items.Length - startIndex + result;
            }
            if (carry < 0)
                carry = 0;
            result = Array.LastIndexOf(items, item, startIndex + size - carry - 1, size - carry);
            if (result >= 0)
                return result - startIndex;
            return result;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last occurrence within the range of elements in the <see cref="CircularList{T}"/> that
        /// extends from the first element to the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="CircularList{T}"/>.</param>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <returns>
        /// The zero-based index of the last occurrence of <paramref name="item"/> within the range of elements in the list that extends from the first element to <paramref name="index"/>, if found; otherwise, –1.
        /// </returns>
        /// <para>The list is searched backward starting at <paramref name="index"/> and ending at the first element.</para>
        /// <para>Only in .NET Framework, this method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/>&#160;type.
        /// Otherwise, the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> will be used.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int LastIndexOf(T item, int index) => LastIndexOf(item, index, index + 1);

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last occurrence within the range of elements in the <see cref="CircularList{T}"/> that
        /// contains the specified number of elements and ends at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="CircularList{T}"/>.</param>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <returns>
        /// The zero-based index of the last occurrence of <paramref name="item"/> within the range of elements in the list that contains <paramref name="count"/> number of elements and ends at <paramref name="item"/>, if found; otherwise, –1.
        /// </returns>
        /// <para>The list is searched backward starting at the last element and ending at the first element.</para>
        /// <para>Only in .NET Framework, this method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/>&#160;type.
        /// Otherwise, the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> will be used.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int LastIndexOf(T item, int index, int count)
        {
            if (size != 0 && index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (size != 0 && count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (size == 0)
                return -1;

            if (index >= size)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count > index + 1)
                Throw.ArgumentOutOfRangeException(Argument.count);

#if NETFRAMEWORK
            IEqualityComparer<T> comparer = ComparerHelper<T>.EqualityComparer;
            if (!ReferenceEquals(comparer, EqualityComparer<T>.Default))
                return FindLastIndex(index, count, enumItem => comparer.Equals(enumItem, item)); 
#endif

            // every searched element is carried
            int capacity = items.Length;
            int start = startIndex + index;
            int result;
            if (start - count + 1 >= capacity)
            {
                start -= capacity;
                result = Array.LastIndexOf(items, item, start, count);
                if (result >= 0)
                    return capacity - startIndex + result;

                return result;
            }

            // there are not carried (and optionally carried) elements to search
            int carry = start - capacity + 1;
            if (carry > 0)
            {
                result = Array.LastIndexOf(items, item, carry - 1, carry);
                if (result >= 0)
                    return capacity - startIndex + result;

                start -= carry;
            }
            if (carry < 0)
                carry = 0;
            result = Array.LastIndexOf(items, item, start, count - carry);
            if (result >= 0)
                return result - startIndex;
            return result;
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the zero-based index of the first occurrence within the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public int FindIndex(Predicate<T> match) => FindIndex(0, size, match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based
        /// index of the first occurrence within the range of elements in the <see cref="CircularList{T}"/> that extends from the specified index to the last element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the search.</param>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public int FindIndex(int index, Predicate<T> match) => FindIndex(index, size - index, match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of
        /// the first occurrence within the range of elements in the <see cref="CircularList{T}"/> that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="index">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public int FindIndex(int index, int count, Predicate<T> match)
        {
            if ((uint)index > (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0 || index > size - count)
                Throw.ArgumentOutOfRangeException(Argument.count);

            if (match == null!)
                Throw.ArgumentNullException(Argument.match);

            int capacity = items.Length;
            int start = startIndex + index;
            int carry = start + count - capacity;

            if (start <= capacity)
            {
                int endIndex = carry > 0 ? capacity : start + count;
                for (int i = start; i < endIndex; i++)
                {
                    if (match(items[i]))
                        return i - startIndex;
                }
            }

            if (carry > 0)
            {
                for (int i = 0; i < carry; i++)
                {
                    if (match(items[i]))
                        return capacity - startIndex + i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the zero-based index of the last occurrence within the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public int FindLastIndex(Predicate<T> match) => FindLastIndex(size - 1, size, match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index
        /// of the last occurrence within the range of elements in the <see cref="CircularList{T}"/> that extends from the first element to the specified index.
        /// </summary>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public int FindLastIndex(int index, Predicate<T> match) => FindLastIndex(index, index + 1, match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of
        /// the last occurrence within the range of elements in the <see cref="CircularList{T}"/> that contains the specified number of elements and ends at the specified index.
        /// </summary>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public int FindLastIndex(int index, int count, Predicate<T> match)
        {
            if ((uint)index > (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0 || index - count + 1 < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (match == null!)
                Throw.ArgumentNullException(Argument.match);

            int capacity = items.Length;
            int start = startIndex + index;
            int carry = start - capacity + 1;

            if (carry > 0)
            {
                int endIndex = count >= carry ? 0 : carry - count;
                for (int i = carry - 1; i >= endIndex; i--)
                {
                    if (match(items[i]))
                        return capacity - startIndex + i;
                }

                if (count <= carry)
                    return -1;

                count -= carry;
            }

            for (int i = (carry >= 0 ? capacity : start) - 1; count > 0; i--, count--)
            {
                if (match(items[i]))
                    return i - startIndex;
            }

            return -1;
        }

        /// <summary>
        /// Determines whether the <see cref="CircularList{T}"/> contains elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The delegate that defines the conditions of the elements to search for.</param>
        /// <returns><see langword="true"/>&#160;if the list contains one or more elements that match the conditions defined by the specified predicate; otherwise, <see langword="false"/>.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public bool Exists(Predicate<T> match) => FindIndex(match) != -1;

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the first occurrence within the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The first element that matches the conditions defined by the specified predicate, if found;
        /// otherwise, the default value for type <typeparamref name="T"/>.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public T? Find(Predicate<T> match)
        {
            if (match == null!)
                Throw.ArgumentNullException(Argument.match);

            int carry = startIndex + size - items.Length;
            int endIndex = carry > 0 ? items.Length : startIndex + size;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(items[i]))
                    return items[i];
            }

            if (carry > 0)
            {
                for (int i = 0; i < carry; i++)
                {
                    if (match(items[i]))
                        return items[i];
                }
            }

            return default(T);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the last occurrence within the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The last element that matches the conditions defined by the specified predicate, if found;
        /// otherwise, the default value for type <typeparamref name="T"/>.</returns>
        /// <remarks>This method performs a linear search; therefore, this method is an O(n) operation.</remarks>
        public T? FindLast(Predicate<T> match)
        {
            if (match == null!)
                Throw.ArgumentNullException(Argument.match);

            int carry = startIndex + size - items.Length;
            if (carry > 0)
            {
                for (int i = carry - 1; i >= 0; i--)
                {
                    if (match(items[i]))
                        return items[i];
                }
            }

            int endIndex = carry > 0 ? items.Length : startIndex + size;
            for (int i = endIndex - 1; i >= startIndex; i--)
            {
                if (match(items[i]))
                    return items[i];
            }

            return default(T);
        }

        /// <summary>
        /// Retrieves all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>A <see cref="CircularList{T}"/> containing all the elements that match the conditions defined by the specified predicate, if found;
        /// otherwise, an empty <see cref="CircularList{T}"/>.</returns>
        /// <remarks>This method is an O(n) operation.</remarks>
        public CircularList<T> FindAll(Predicate<T> match)
        {
            if (match == null!)
                Throw.ArgumentNullException(Argument.match);

            var result = new CircularList<T>(Count);
            int carry = startIndex + size - items.Length;
            int endIndex = carry > 0 ? items.Length : startIndex + size;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(items[i]))
                    result.Add(items[i]);
            }

            if (carry > 0)
            {
                for (int i = 0; i < carry; i++)
                {
                    if (match(items[i]))
                        result.Add(items[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Searches the entire sorted list for an element using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns>The zero-based index of <paramref name="item"/> in the sorted <see cref="CircularList{T}"/>, if <paramref name="item"/> is found; otherwise, a negative number
        /// that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
        /// <remarks>
        /// <para>If <typeparamref name="T"/> is an <see langword="enum"/>, this method uses the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>; otherwise,
        /// the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for type <typeparamref name="T"/> to determine the order of list elements.
        /// The <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> property checks whether type <typeparamref name="T"/> implements the <see cref="IComparable{T}"/> generic interface
        /// and uses that implementation, if available. If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether type <typeparamref name="T"/> implements
        /// the <see cref="IComparable"/> interface. If type <typeparamref name="T"/> does not implement either interface, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see>
        /// throws an <see cref="InvalidOperationException"/>.</para>
        /// <para>The list must already be sorted according to the comparer implementation; otherwise, the result is incorrect.</para>
        /// <para>Comparing <see langword="null"/>&#160;with any reference type is allowed and does not generate an exception when using the <see cref="IComparable{T}"/> generic interface. When sorting, <see langword="null"/>&#160;is considered to be less than any other object.</para>
        /// <para>If the list contains more than one element with the same value, the method returns only one of the occurrences, and it might return any one of the occurrences, not necessarily the first one.</para>
        /// <para>If the list does not contain the specified value, the method returns a negative integer. You can apply the bitwise complement operation (~) to this negative integer to get the index of the first element that is larger than the search value. When inserting the value into the <see cref="CircularList{T}"/>, this index should be used as the insertion point to maintain the sort order.</para>
        /// <para>This method is an O(log n) operation, where n is the number of elements in the range.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The default comparer <see cref="Comparer{T}.Default"/> cannot find an implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.</exception>
        public int BinarySearch(T item) => BinarySearch(0, size, item, null);

        /// <summary>
        /// Searches the entire sorted <see cref="CircularList{T}"/> for an element using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/>&#160;to use the <see cref="EnumComparer{TEnum}.Comparer"/> for <see langword="enum"/>&#160;element types, or the default comparer <see cref="Comparer{T}.Default"/> for other element types.</param>
        /// <returns>The zero-based index of <paramref name="item"/> in the sorted list, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
        /// <remarks>
        /// <para>The <paramref name="comparer"/> customizes how the elements are compared. For example, you can use a <see cref="CaseInsensitiveComparer"/> instance as the comparer to perform case-insensitive string searches.</para>
        /// <para>If <paramref name="comparer"/> is provided, the elements of the list are compared to the specified value using the specified <see cref="IComparer{T}"/> implementation.</para>
        /// <para>If comparer is <see langword="null"/>, then if <typeparamref name="T"/> is an <see langword="enum"/>, this method uses the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>; otherwise,
        /// the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for type <typeparamref name="T"/> to determine the order of list elements.
        /// The <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> property checks whether type <typeparamref name="T"/> implements the <see cref="IComparable{T}"/> generic interface
        /// and uses that implementation, if available. If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether type <typeparamref name="T"/> implements
        /// the <see cref="IComparable"/> interface. If type <typeparamref name="T"/> does not implement either interface, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see>
        /// throws an <see cref="InvalidOperationException"/>.</para>
        /// <para>The list must already be sorted according to the comparer implementation; otherwise, the result is incorrect.</para>
        /// <para>If comparer is <see langword="null"/>, comparing <see langword="null"/>&#160;with any reference type is allowed and does not generate an exception when using the <see cref="IComparable{T}"/> generic interface. When sorting, <see langword="null"/>&#160;is considered to be less than any other object.</para>
        /// <para>If the list contains more than one element with the same value, the method returns only one of the occurrences, and it might return any one of the occurrences, not necessarily the first one.</para>
        /// <para>If the list does not contain the specified value, the method returns a negative integer. You can apply the bitwise complement operation (~) to this negative integer to get the index of the first element that is larger than the search value. When inserting the value into the <see cref="CircularList{T}"/>, this index should be used as the insertion point to maintain the sort order.</para>
        /// <para>This method is an O(log n) operation, where n is the number of elements in the range.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and the default comparer <see cref="Comparer{T}.Default"/> cannot find an implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.</exception>
        public int BinarySearch(T item, IComparer<T> comparer) => BinarySearch(0, size, item, comparer);

        /// <summary>
        /// Searches a range of elements in the sorted <see cref="CircularList{T}"/> for an element using the specified <paramref name="comparer"/> and returns the zero-based index of the element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to search.</param>
        /// <param name="count">The length of the range to search.</param>
        /// <param name="item">The object to locate. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/>&#160;to use the
        /// <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> for <see langword="enum"/>&#160;element types, or the default comparer
        /// <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for other element types.</param>
        /// <returns>The zero-based index of <paramref name="item"/> in the sorted list, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
        /// <remarks>
        /// <para>The <paramref name="comparer"/> customizes how the elements are compared. For example, you can use a <see cref="CaseInsensitiveComparer"/> instance as the comparer to perform case-insensitive string searches.</para>
        /// <para>If <paramref name="comparer"/> is provided, the elements of the list are compared to the specified value using the specified <see cref="IComparer{T}"/> implementation.</para>
        /// <para>If comparer is <see langword="null"/>, then if <typeparamref name="T"/> is an <see langword="enum"/>, this method uses the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>; otherwise,
        /// the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for type <typeparamref name="T"/> to determine the order of list elements.
        /// The <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> property checks whether type <typeparamref name="T"/> implements the <see cref="IComparable{T}"/> generic interface
        /// and uses that implementation, if available. If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether type <typeparamref name="T"/> implements
        /// the <see cref="IComparable"/> interface. If type <typeparamref name="T"/> does not implement either interface, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see>
        /// throws an <see cref="InvalidOperationException"/>.</para>
        /// <para>The list must already be sorted according to the comparer implementation; otherwise, the result is incorrect.</para>
        /// <para>If comparer is <see langword="null"/>, comparing <see langword="null"/>&#160;with any reference type is allowed and does not generate an exception when using the <see cref="IComparable{T}"/> generic interface. When sorting, <see langword="null"/>&#160;is considered to be less than any other object.</para>
        /// <para>If the list contains more than one element with the same value, the method returns only one of the occurrences, and it might return any one of the occurrences, not necessarily the first one.</para>
        /// <para>If the list does not contain the specified value, the method returns a negative integer. You can apply the bitwise complement operation (~) to this negative integer to get the index of the first element that is larger than the search value. When inserting the value into the <see cref="CircularList{T}"/>, this index should be used as the insertion point to maintain the sort order.</para>
        /// <para>This method is an O(log n) operation, where n is the number of elements in the range.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.
        /// <br/>-or-
        /// <br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the list.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and the default comparer <see cref="Comparer{T}.Default"/> cannot find an implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.</exception>
        public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
        {
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > size)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            comparer ??= ComparerHelper<T>.Comparer;
            if (startIndex == 0)
                return Array.BinarySearch(items, index, count, item, comparer);

            return DoBinarySearch(index, count, item, comparer);
        }

        #endregion

        #region Transform

        /// <summary>
        /// Copies the elements of the list to a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of the list.</returns>
        public T[] ToArray()
        {
            T[] array = new T[size];
            CopyTo(array);
            return array;
        }

        /// <summary>
        /// Returns a read-only <see cref="IList{T}"/> wrapper for the current collection.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyCollection{T}"/> that acts as a read-only wrapper around the current <see cref="CircularList{T}"/>.</returns>
        /// <remarks>
        /// <para>To prevent any modifications to <see cref="CircularList{T}"/>, expose <see cref="CircularList{T}"/> only through this wrapper.
        /// A <see cref="ReadOnlyCollection{T}"/> does not expose methods that modify the collection. However, if changes are made to the underlying <see cref="CircularList{T}"/>,
        /// the read-only collection reflects those changes.</para>
        /// <para>This method is an O(1) operation.</para>
        /// </remarks>
        public ReadOnlyCollection<T> AsReadOnly() => new ReadOnlyCollection<T>(this);

        /// <summary>
        /// Converts the elements in the current list to another type, and returns a <see cref="CircularList{T}"/> containing the converted elements.
        /// </summary>
        /// <typeparam name="TOutput">The type of the elements of the target list.</typeparam>
        /// <param name="converter">A <see cref="Converter{TInput,TOutput}"/> delegate that converts each element from one type to another type.</param>
        /// <returns>A <see cref="CircularList{T}"/> of the target type containing the converted elements from the current list.</returns>
        public CircularList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null!)
                Throw.ArgumentNullException(Argument.converter);

            CircularList<TOutput> list = new CircularList<TOutput>(size);
            int targetIndex = 0;
            int carry = startIndex + size - items.Length;
            int endIndex = carry > 0 ? items.Length : startIndex + size;
            for (int i = startIndex; i < endIndex; i++)
            {
                list.items[targetIndex] = converter(items[i]);
                targetIndex += 1;
            }

            if (carry > 0)
            {
                for (int i = 0; i < carry; i++)
                {
                    list.items[targetIndex] = converter(items[i]);
                    targetIndex += 1;
                }
            }

            return list;
        }

        /// <summary>
        /// Copies the entire <see cref="CircularList{T}"/> to a compatible one-dimensional array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from the <see cref="CircularList{T}"/>.
        /// The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0 equal to or greater than the length of <paramref name="array"/>.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source list is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex = 0)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
            if (array.Length - arrayIndex < size)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

            // Delegating rest error checking to Array.Copy.
            if (size <= 0)
                return;
            int carry = startIndex + size - items.Length;
            items.CopyElements(startIndex, array, arrayIndex, carry <= 0 ? size : size - carry);
            if (carry > 0)
                items.CopyElements(0, array, size - carry + arrayIndex, carry);
        }

        /// <summary>
        /// Copies a range of elements from the <see cref="CircularList{T}"/> to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="index">The zero-based index in the source list at which copying begins.</param>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from the <see cref="CircularList{T}"/>.
        /// The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.
        /// <br/>-or-<br/>
        /// <paramref name="arrayIndex"/> is less than 0.
        /// <br/>-or-<br/>
        /// <paramref name="count"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
        /// <br/>-or-<br/>
        /// The number of elements in the source list is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (size - index < count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            // ReSharper disable once PossibleNullReferenceException
            if (array.Length - arrayIndex < count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

            // Delegating rest error checking to Array.Copy.
            if (size <= 0)
                return;

            // every element to be copied is carried
            int start = startIndex + index;
            if (start >= items.Length)
            {
                start -= items.Length;
                items.CopyElements(start, array, arrayIndex, count);
                return;
            }

            // there are also not carried elements to copy
            int carry = start + count - items.Length;
            items.CopyElements(start, array, arrayIndex, carry <= 0 ? count : count - carry);
            if (carry > 0)
                items.CopyElements(0, array, count - carry + arrayIndex, carry);
        }

        #endregion

        #region Manipulate

        /// <summary>
        /// Reverses the order of the elements in the entire <see cref="CircularList{T}"/>.
        /// </summary>
        public void Reverse() => Reverse(0, size);

        /// <summary>
        /// Reverses the order of the elements in the specified range.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to reverse.</param>
        /// <param name="count">The number of elements in the range to reverse.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the list.
        /// <br/>-or-
        /// <br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the list.</exception>
        public void Reverse(int index, int count)
        {
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > size)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            int start = startIndex + index;
            int carry = start + count - items.Length;

            // the section to reverse is wrapped: reversing manually via indexer
            if (carry > 0 && start < items.Length)
            {
                int i = index;
                int j = index + count - 1;
                while (i < j)
                {
                    T temp = ElementAtNonZeroStart(i);
                    SetElementAt(i, ElementAtNonZeroStart(j));
                    SetElementAt(j, temp);
                    i += 1;
                    j -= 1;
                }

                version += 1;
                return;
            }

            // fast reverse
            if (start >= items.Length)
                start -= items.Length;
            Array.Reverse(items, start, count);
            version += 1;
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="CircularList{T}"/> using the default comparer.
        /// </summary>
        /// <remarks>
        /// <para>If <typeparamref name="T"/> is an <see langword="enum"/>, this method uses
        /// the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>; otherwise, the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether type <typeparamref name="T"/>
        /// implements the <see cref="IComparable{T}"/> generic interface and uses that implementation, if available. If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks
        /// whether type <typeparamref name="T"/> implements the <see cref="IComparable"/> interface.
        /// If type <typeparamref name="T"/> does not implement either interface, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> throws an <see cref="InvalidOperationException"/>.</para>
        /// <para>This implementation performs an unstable sort; that is, if two elements are equal, their order might not be preserved.
        /// In contrast, a stable sort preserves the order of elements that are equal.</para>
        /// <para>On average, this method is an O(n log n) operation, where n is <see cref="Count"/>; in the worst case it is an O(n ^ 2) operation.</para>
        /// </remarks>
        public void Sort() => Sort(0, size, null);

        /// <summary>
        /// Sorts the elements in the entire <see cref="CircularList{T}"/> using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/>&#160;
        /// to use the default comparer <see cref="Comparer{T}.Default"/>.</param>
        /// <exception cref="ArgumentException">The implementation of <paramref name="comparer"/> caused an error during the sort.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and the default comparer <see cref="Comparer{T}.Default"/>
        /// cannot find implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// <para>If comparer is <see langword="null"/>, then if <typeparamref name="T"/> is an <see langword="enum"/>, this method uses
        /// the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>; otherwise, the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether type <typeparamref name="T"/>
        /// implements the <see cref="IComparable{T}"/> generic interface and uses that implementation, if available. If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks
        /// whether type <typeparamref name="T"/> implements the <see cref="IComparable"/> interface.
        /// If type <typeparamref name="T"/> does not implement either interface, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> throws an <see cref="InvalidOperationException"/>.</para>
        /// <para>This implementation performs an unstable sort; that is, if two elements are equal, their order might not be preserved.
        /// In contrast, a stable sort preserves the order of elements that are equal.</para>
        /// <para>On average, this method is an O(n log n) operation, where n is <see cref="Count"/>; in the worst case it is an O(n ^ 2) operation.</para>
        /// </remarks>
        public void Sort(IComparer<T> comparer) => Sort(0, size, comparer);

        /// <summary>
        /// Sorts the elements in the entire <see cref="CircularList{T}"/> using the specified <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="comparison">The <see cref="Comparison{T}"/> to use when comparing elements.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The implementation of <paramref name="comparison"/> caused an error during the sort.</exception>
        /// <remarks>
        /// <para>This implementation performs an unstable sort; that is, if two elements are equal, their order might not be preserved.
        /// In contrast, a stable sort preserves the order of elements that are equal.</para>
        /// <para>On average, this method is an O(n log n) operation, where n is <see cref="Count"/>; in the worst case it is an O(n ^ 2) operation.</para>
        /// </remarks>
        public void Sort(Comparison<T> comparison)
        {
            if (comparison == null!)
                Throw.ArgumentNullException(Argument.comparison);

            if (size > 0)
                Sort(0, size, new ComparisonWrapper(comparison));
        }

        /// <summary>
        /// Sorts the elements in a range of elements in <see cref="CircularList{T}"/> using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/>&#160;
        /// to use the default comparer <see cref="Comparer{T}.Default"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.
        /// <br/>-or-<br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not specify a valid range in the <see cref="CircularList{T}"/>.
        /// <br/>-or-<br/><paramref name="count"/>The implementation of <paramref name="comparer"/> caused an error during the sort.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and the default comparer <see cref="Comparer{T}.Default"/>
        /// cannot find implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// <para>If comparer is <see langword="null"/>, then if <typeparamref name="T"/> is an <see langword="enum"/>, this method uses
        /// the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>; otherwise, the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether type <typeparamref name="T"/>
        /// implements the <see cref="IComparable{T}"/> generic interface and uses that implementation, if available. If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks
        /// whether type <typeparamref name="T"/> implements the <see cref="IComparable"/> interface.
        /// If type <typeparamref name="T"/> does not implement either interface, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> throws an <see cref="InvalidOperationException"/>.</para>
        /// <para>This implementation performs an unstable sort; that is, if two elements are equal, their order might not be preserved.
        /// In contrast, a stable sort preserves the order of elements that are equal.</para>
        /// <para>On average, this method is an O(n log n) operation, where n is <see cref="Count"/>; in the worst case it is an O(n ^ 2) operation.</para>
        /// </remarks>
        public void Sort(int index, int count, IComparer<T>? comparer)
        {
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > size)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            comparer ??= ComparerHelper<T>.Comparer;
            int capacity = items.Length;
            int start = startIndex + index;
            int carry = start + count - capacity;

            // the section to sort is wrapped
            if (carry > 0 && start < capacity)
            {
                // fast solution: the whole list must be sorted
                if (index == 0 && count == size)
                {
                    // fastest solution: the list is full, the full list will be sorted so no move is needed
                    if (capacity == size)
                        startIndex = 0;
                    else
                    {
                        int nonWrapped = size - carry;

                        // moving down the elements from the end
                        if (nonWrapped <= carry)
                        {
                            items.CopyElements(startIndex, items, carry, nonWrapped);
                            Array.Clear(items, capacity - nonWrapped, nonWrapped);
                            startIndex = 0;
                        }
                        // moving up the elements from the start
                        else
                        {
                            items.CopyElements(0, items, capacity - count, carry);
                            Array.Clear(items, 0, carry);
                            startIndex = capacity - count;
                        }
                    }

                    start = startIndex;
                }
                // slower solution: copying into new array before sorting
                else
                {
                    T[] newItems = new T[size];
                    CopyTo(newItems);
                    items = newItems;
                    startIndex = 0;
                    start = index;
                }
            }

            if (start >= capacity)
                start -= capacity;
            Array.Sort(items, start, count, comparer);
            version += 1;
        }

        /// <summary>
        /// Performs the specified action on each element of the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="action">The <see cref="Action{T}"/> delegate to perform on each element of the <see cref="CircularList{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The list has been modified during the operation.</exception>
        public void ForEach(Action<T> action)
        {
            if (action == null!)
                Throw.ArgumentNullException(Argument.action);

            int ver = version;
            int carry = startIndex + size - items.Length;
            int endIndex = carry > 0 ? items.Length : startIndex + size;
            for (int i = startIndex; i < endIndex; i++)
            {
                action(items[i]);
                if (ver != version)
                    break;
            }

            if (carry > 0 && ver == version)
            {
                for (int i = 0; i < carry; i++)
                {
                    action(items[i]);
                    if (ver != version)
                        break;
                }
            }

            if (ver != version)
                Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);
        }

        #endregion

        #region Query

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CircularList{T}"/>.
        /// </summary>
        /// <returns>An <see cref="Enumerator"/> instance that can be used to iterate though the elements of the <see cref="CircularList{T}"/>.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Creates a shallow copy of a range of elements in the source <see cref="CircularList{T}"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which the range starts.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <returns>A shallow copy of a range of elements in the source <see cref="CircularList{T}"/>.</returns>
        /// <remarks>
        /// <para>A shallow copy of a collection of reference types, or a subset of that collection, contains only the references to the elements of the collection.
        /// The objects themselves are not copied. The references in the new list point to the same objects as the references in the original list.</para>
        /// <para>A shallow copy of a collection of value types, or a subset of that collection, contains the elements of the collection.
        /// However, if the elements of the collection contain references to other objects, those objects are not copied.
        /// The references in the elements of the new collection point to the same objects as the references in the elements of the original collection.</para>
        /// <para>In contrast, a deep copy of a collection copies the elements and everything directly or indirectly referenced by the elements.</para>
        /// <para>This method is an O(n) operation, where n is count.</para>
        /// </remarks>
        public CircularList<T> GetRange(int index, int count)
        {
            if ((uint)index >= (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > size)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            CircularList<T> result = new CircularList<T>(count) { size = count };

            // every element to copy is carried
            int start = startIndex + index;
            if (start >= items.Length)
            {
                start -= items.Length;
                items.CopyElements(start, result.items, 0, count);
                return result;
            }

            // there are also not carried elements to copy
            int carry = start + count - items.Length;
            items.CopyElements(start, result.items, 0, carry <= 0 ? count : count - carry);
            if (carry > 0)
                items.CopyElements(0, result.items, count - carry, carry);

            return result;
        }

        /// <summary>
        /// Determines whether every element in the <see cref="CircularList{T}"/> matches the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions to check against the elements.</param>
        /// <returns><see langword="true"/>&#160;if every element in the list matches the conditions defined by the specified predicate; otherwise, <see langword="false"/>.
        /// If the list has no elements, the return value is <see langword="true"/>.</returns>
        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null!)
                Throw.ArgumentNullException(Argument.match);

            int carry = startIndex + size - items.Length;
            int endIndex = carry > 0 ? items.Length : startIndex + size;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (!match(items[i]))
                    return false;
            }

            if (carry > 0)
            {
                for (int i = 0; i < carry; i++)
                {
                    if (!match(items[i]))
                        return false;
                }
            }

            return true;
        }

        #endregion

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets an element without check
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal T ElementAt(int index) => startIndex == 0 ? items[index] : ElementAtNonZeroStart(index);

        /// <summary>
        /// Performs a binary search without check
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal int InternalBinarySearch(int index, int count, T item, IComparer<T> comparer)
            => startIndex == 0 ? Array.BinarySearch(items, index, count, item, comparer) : DoBinarySearch(index, count, item, comparer);

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a <paramref name="collection"/> to the end of the list.
        /// </summary>
        /// <param name="collection">The collection to add to the list.</param>
        private void AddLast(IEnumerable<T> collection)
        {
            T[]? asArray = ReferenceEquals(collection, this) ? ToArray() : collection as T[];
            ICollection<T>? asCollection = asArray != null ? null : collection as ICollection<T>;

            // as simple enumerable
            if (asArray == null && asCollection == null)
            {
                using (var enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                        AddLast(enumerator.Current);
                }

                return;
            }

            int collectionSize = asArray?.Length ?? asCollection!.Count;
            if (collectionSize == 0)
                return;

            EnsureCapacity(size + collectionSize);

            // calculating insert position
            int pos = startIndex + size;
            if (pos >= items.Length)
                pos -= items.Length;

            // if collection that fits into list without wrapping
            if (asCollection != null && pos + collectionSize <= items.Length)
                asCollection.CopyTo(items, pos);
            // otherwise, as array copied up to in two sessions
            else
            {
                asArray ??= collection.ToArray();
                int carry = pos + collectionSize - items.Length;
                asArray.CopyElements(0, items, pos, carry <= 0 ? collectionSize : collectionSize - carry);
                if (carry > 0)
                    asArray.CopyElements(collectionSize - carry, items, 0, carry);
            }

            size += collectionSize;
            version += 1;
        }

        /// <summary>
        /// Adds a <paramref name="collection"/> to the beginning of the list.
        /// </summary>
        /// <param name="collection">The collection to add to the list.</param>
        private void AddFirst(IEnumerable<T> collection)
        {
            T[]? asArray = ReferenceEquals(collection, this) ? ToArray() : collection as T[];
            ICollection<T>? asCollection = asArray != null ? null : collection as ICollection<T>;

            // ReSharper disable PossibleMultipleEnumeration - collection is not enumerated multiple times
            // to prevent O(n ^ 2) cost (n is collection.Count) we create a temp buffer
            if (asArray == null && asCollection == null)
                asArray = collection.ToArray();

            int collectionSize = asArray?.Length ?? asCollection!.Count;
            if (collectionSize == 0)
                return;

            EnsureCapacity(size + collectionSize);

            // calculating insert position
            int pos = startIndex - collectionSize;
            if (pos < 0)
                pos += items.Length;

            // if non-array collection that fits into list without wrapping
            if (asCollection != null && pos + collectionSize <= items.Length)
                asCollection.CopyTo(items, pos);
            // otherwise, as array copied up to two sessions
            else
            {
                asArray ??= collection.ToArray();
                int carry = pos + collectionSize - items.Length;
                asArray.CopyElements(0, items, pos, carry <= 0 ? collectionSize : collectionSize - carry);
                if (carry > 0)
                    asArray.CopyElements(collectionSize - carry, items, 0, carry);
            }
            // ReSharper restore PossibleMultipleEnumeration

            startIndex = pos;
            size += collectionSize;
            version += 1;
        }

        /// <summary>
        /// Removes the first n elements of the list where n is <paramref name="count"/>.
        /// </summary>
        /// <param name="count">Number of items to remove.</param>
        private void RemoveFirst(int count)
        {
            if (count == size)
            {
                Clear();
                return;
            }

            int carry = startIndex + count - items.Length;
            if (Reflector<T>.IsManaged)
                Array.Clear(items, startIndex, carry <= 0 ? count : count - carry);
            if (carry > 0)
            {
                if (Reflector<T>.IsManaged)
                    Array.Clear(items, 0, carry);
                startIndex = carry;
            }
            else
            {
                startIndex += count;
                if (startIndex == items.Length)
                    startIndex = 0;
            }

            size -= count;
            version += 1;
        }

        /// <summary>
        /// Removes the last n elements of the list where n is <paramref name="count"/>.
        /// </summary>
        /// <param name="count">Number of items to remove.</param>
        private void RemoveLast(int count)
        {
            if (count == size)
            {
                Clear();
                return;
            }

            int pos = startIndex + size - count;
            if (pos >= items.Length)
                pos -= items.Length;

            if (Reflector<T>.IsManaged)
            {
                int carry = pos + count - items.Length;
                Array.Clear(items, pos, carry <= 0 ? count : count - carry);
                if (carry > 0)
                    Array.Clear(items, 0, carry);
            }

            size -= count;
            version += 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureCapacity(int min)
        {
            if (items.Length >= min)
                return;

            int newCapacity = items.Length == 0 ? defaultCapacity : items.Length << 1;
            if (newCapacity < min)
                newCapacity = min;
            Capacity = newCapacity;
        }

        private void InsertAt(int index, T item)
        {
            if ((uint)index > (uint)size)
                Throw.ArgumentOutOfRangeException(Argument.index);

            if (size == items.Length)
            {
                IncreaseCapacityWithInsert(index, item);
                return;
            }

            // calculating position
            int pos = startIndex + index;
            int capacity = items.Length;
            if (pos >= capacity)
                pos -= capacity;

            // optimized for minimal data moving
            if (index >= (size >> 1))
                ShiftUp(pos, size - index);
            else
            {
                // decreasing startIndex and pos
                if (startIndex > 0)
                    startIndex -= 1;
                else
                    startIndex = capacity - 1;

                if (pos > 0)
                    pos -= 1;
                else
                    pos = capacity - 1;

                ShiftDown(pos, index);
            }

            items[pos] = item;
            size += 1;
            version += 1;
        }

        private void IncreaseCapacityWithInsert(int index, T item)
        {
            Debug.Assert(size > 0);
            Debug.Assert(index > 0);

            T[] newItems = new T[items.Length << 1];
            int newSize = size + 1;

            // elements before item
            int carry = startIndex + index - items.Length;
            items.CopyElements(startIndex, newItems, 0, carry <= 0 ? index : index - carry);
            if (carry > 0)
                items.CopyElements(0, newItems, index - carry, carry);

            // item
            newItems[index] = item;

            // elements after item
            int pos = startIndex + index;
            if (pos >= items.Length)
                pos -= items.Length;

            int restCount = size - index;
            carry = pos + restCount - items.Length;
            items.CopyElements(pos, newItems, index + 1, carry <= 0 ? restCount : restCount - carry);
            if (carry > 0)
                items.CopyElements(0, newItems, newSize - carry, carry);

            items = newItems;
            size = newSize;
            startIndex = 0;
        }

        private void IncreaseCapacityWithInsert(int index, ICollection<T> collection)
        {
            Debug.Assert(size > 0);
            Debug.Assert(index > 0);
            int newCapacity = items.Length << 1;
            int newSize = size + collection.Count;
            if (newCapacity < newSize)
                newCapacity = newSize;

            T[] newItems = new T[newCapacity];

            // elements before collection items
            int carry = startIndex + index - items.Length;
            items.CopyElements(startIndex, newItems, 0, carry <= 0 ? index : index - carry);
            if (carry > 0)
                items.CopyElements(0, newItems, index - carry, carry);

            // collection items
            if (collection is T[] array)
                array.CopyElements(0, newItems, index, array.Length);
            else
                collection.CopyTo(newItems, index);

            // elements after collection items
            int pos = startIndex + index;
            if (pos >= items.Length)
                pos -= items.Length;

            int restCount = size - index;
            carry = pos + restCount - items.Length;
            items.CopyElements(pos, newItems, index + collection.Count, carry <= 0 ? restCount : restCount - carry);
            if (carry > 0)
                items.CopyElements(0, newItems, newSize - carry, carry);

            items = newItems;
            size = newSize;
            startIndex = 0;
            version += 1;
        }

        private void RemoveMiddle(int index)
        {
            // if we are here, shifting is necessary and there are at least 3 elements
            // optimized for minimal data moving
            size -= 1;
            if (index <= size >> 1)
            {
                ShiftUp(startIndex, index);
                if (Reflector<T>.IsManaged)
                    items[startIndex] = default(T)!;
                startIndex += 1;
                if (startIndex == items.Length)
                    startIndex = 0;
            }
            else
            {
                // calculating end position
                int pos = startIndex + size;
                int length = items.Length;
                if (pos >= length)
                    pos -= length;

                ShiftDown(pos, size - index);
                if (Reflector<T>.IsManaged)
                    items[pos] = default(T)!;
            }

            version += 1;
        }

        private void RemoveMiddle(int index, int count)
        {
            int capacity = items.Length;

            // optimized for minimal data moving
            if (index <= size >> 1)
            {
                ShiftUp(startIndex, index, count);
                if (Reflector<T>.IsManaged)
                {
                    int carry = startIndex + count - capacity;
                    Array.Clear(items, startIndex, carry <= 0 ? count : count - carry);
                    if (carry > 0)
                        Array.Clear(items, 0, carry);
                }

                startIndex += count;
                if (startIndex >= capacity)
                    startIndex -= capacity;
            }
            else
            {
                int topIndex = startIndex + size - 1;
                if (topIndex > capacity)
                    topIndex -= capacity;

                ShiftDown(topIndex, size - index - count, count);
                if (Reflector<T>.IsManaged)
                {
                    int carry = -(topIndex - count + 1);
                    if (carry <= 0)
                        Array.Clear(items, -carry, count);
                    else
                    {
                        Array.Clear(items, capacity - carry, carry);
                        Array.Clear(items, 0, count - carry);
                    }
                }
            }

            size -= count;
            version += 1;
        }

        /// <summary>
        /// Shifts elements up in the stored items by 1.
        /// </summary>
        /// <param name="index">Bottom index of the elements to shift.</param>
        /// <param name="elemCount">Count of elements to shift up.</param>
        private void ShiftUp(int index, int elemCount)
        {
            // determining count of wrapped elements (the ones at the beginning of the physical array)
            int carry = index + elemCount - items.Length;

            if (carry >= 0)
            {
                // if needed, moving them up by one
                if (carry != 0)
                {
                    items.CopyElements(0, items, 1, carry);
                    elemCount -= carry;
                }

                // moving the last item in the physical array to the first position
                items[0] = items[items.Length - 1];
                elemCount -= 1;
            }

            // moving the rest of the items normally
            if (elemCount > 0)
                items.CopyElements(index, items, index + 1, elemCount);
        }

        /// <summary>
        /// Shifts elements up in the stored items by <paramref name="shiftCount"/>.
        /// </summary>
        /// <param name="index">Bottom index of the elements to shift.</param>
        /// <param name="elemCount">Count of elements to shift up.</param>
        /// <param name="shiftCount">Distance of the shift.</param>
        private void ShiftUp(int index, int elemCount, int shiftCount)
        {
            int carry = index + elemCount - items.Length;

            // 1.) Moving up wrapped elements at the beginning of the physical array by shiftCount
            if (carry > 0)
            {
                items.CopyElements(0, items, shiftCount, carry);
                elemCount -= carry;
                carry = 0;
            }

            // 2.) Moving min(shiftCount,elemCount) elements from the end of the array to the beginning
            carry += shiftCount;
            if (carry > 0)
            {
                int carryActual = Math.Min(carry, elemCount);
                items.CopyElements(index + elemCount - carryActual, items, carry - carryActual, carryActual);
                elemCount -= carryActual;
            }

            // 3.) Moving rest of the items up normally
            if (elemCount > 0)
                items.CopyElements(index, items, index + shiftCount, elemCount);
        }

        /// <summary>
        /// Shifts elements down in the stored items by 1.
        /// </summary>
        /// <param name="index">Top index of the elements to shift.</param>
        /// <param name="elemCount">Count of elements to shift down</param>
        private void ShiftDown(int index, int elemCount)
        {
            // determining count of wrapped elements (the ones at the end of the physical array)
            int carry = -(index - elemCount + 1);

            if (carry >= 0)
            {
                // if needed, moving them down by one
                if (carry != 0)
                {
                    items.CopyElements(items.Length - carry, items, items.Length - carry - 1, carry);
                    elemCount -= carry;
                }

                // moving the first item in the physical array to the last position
                items[items.Length - 1] = items[0];
                elemCount -= 1;
            }

            // moving the rest of the items normally
            if (elemCount > 0)
                items.CopyElements(index - elemCount + 1, items, index - elemCount, elemCount);
        }

        /// <summary>
        /// Shifts elements down in the stored items by <paramref name="shiftCount"/>.
        /// </summary>
        /// <param name="index">Top index of the elements to shift.</param>
        /// <param name="elemCount">Count of elements to shift down</param>
        /// <param name="shiftCount">Distance of the shift.</param>
        private void ShiftDown(int index, int elemCount, int shiftCount)
        {
            int carry = -(index - elemCount + 1);
            int sourceIndex;

            // 1.) Moving down wrapped elements at the end of the physical array by shiftCount
            if (carry > 0)
            {
                sourceIndex = items.Length - carry;
                items.CopyElements(sourceIndex, items, sourceIndex - shiftCount, carry);
                elemCount -= carry;
                carry = 0;
            }

            // 2.) Moving min(shiftCount,elemCount) elements from the beginning of the array to the end
            carry += shiftCount;
            if (carry > 0)
            {
                int carryActual = Math.Min(carry, elemCount);
                items.CopyElements(index - elemCount + 1, items, items.Length - carry, carryActual);
                elemCount -= carryActual;
            }

            // 3.) Moving rest of the items down normally
            if (elemCount > 0)
            {
                sourceIndex = index - elemCount + 1;
                items.CopyElements(sourceIndex, items, sourceIndex - shiftCount, elemCount);
            }
        }

        /// <summary>
        /// Gets an element without check, from any base.
        /// </summary>
        private T ElementAtNonZeroStart(int index)
        {
            int pos = startIndex + index;
            int length = items.Length;
            if (pos >= length)
                pos -= length;

            return items[pos];
        }

        private void SetElementAt(int index, T value)
        {
            if (startIndex == 0)
                items[index] = value;
            else
            {
                int pos = startIndex + index;
                int length = items.Length;
                if (pos >= length)
                    pos -= length;
                items[pos] = value;
            }
        }

        private int DoBinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            int capacity = items.Length;
            int start = startIndex + index;
            int carry = start + count - capacity;

            // the section to search is wrapped: doing it manually
            if (carry > 0 && start < capacity)
                return BinarySearchHelperInstance.BinarySearch(this, index, count, item, comparer);

            // search in the non-wrapped part
            int result;
            if (carry <= 0)
            {
                result = Array.BinarySearch(items, start, count, item, comparer);
                return result >= 0 ? result - startIndex : ~(~result - startIndex);
            }

            // search in the wrapped-only part
            start -= capacity;
            result = Array.BinarySearch(items, start, count, item, comparer);
            return result >= 0 ? capacity - startIndex + result : ~(capacity - startIndex + ~result);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            // Since result is handled as an interface, returning a reference type enumerator to avoid boxing
            => startIndex == 0 ? (IEnumerator<T>)new SimpleEnumeratorAsReference(this) : new EnumeratorAsReference(this);

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        int IList.Add(object? value)
        {
            Throw.ThrowIfNullIsInvalid<T>(value);
            try
            {
                AddLast((T)value!);
            }
            catch (InvalidCastException)
            {
                Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeOfT));
            }

            return size - 1;
        }

        bool IList.Contains(object? value) => CanAccept(value) && Contains((T)value!);

        int IList.IndexOf(object? value) => CanAccept(value) ? IndexOf((T)value!) : -1;

        void IList.Insert(int index, object? value)
        {
            Throw.ThrowIfNullIsInvalid<T>(value);
            try
            {
                Insert(index, (T)value!);
            }
            catch (InvalidCastException)
            {
                Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeOfT));
            }
        }

        void IList.Remove(object? value)
        {
            Throw.ThrowIfNullIsInvalid<T>(value);
            try
            {
                Remove((T)value!);
            }
            catch (InvalidCastException)
            {
                Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeOfT));
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);

            if (array is T[] typedArray)
            {
                CopyTo(typedArray, index);
                return;
            }

            if (index < 0 || index > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (array.Length - index < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (array.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            if (array is object?[] objectArray)
            {
                for (int i = 0; i < size; i++)
                {
                    objectArray[index] = ElementAt(i);
                    index += 1;
                }

                return;
            }

            Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
        }

        #endregion

        #endregion

        #endregion
    }
}
