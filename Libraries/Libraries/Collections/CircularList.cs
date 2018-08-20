#region Used namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using KGySoft.Libraries.Diagnostics;
using KGySoft.Libraries.Resources;

#endregion

namespace KGySoft.Libraries.Collections
{
    /// <summary>
    /// Implements an <see cref="IList{T}"/> where inserting/removing at the beginning/end position are O(1) operations.
    /// <see cref="CircularList{T}"/> is fully compatible with <see cref="List{T}"/>, but
    /// can be ideal instead of <see cref="LinkedList{T}"/> as well, as <see cref="CircularList{T}"/> outperforms <see cref="LinkedList{T}"/> in most cases.
    /// </summary>
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
    /// the amortized cost of adding methods (<see cref="Add"/>, <see cref="AddLast(T)"/>, <see cref="AddFirst(T)"/> or <see cref="Insert"/> at the first/ last position)
    /// is O(1) due to the low frequency of increasing capacity. For example, when 20 million
    /// items are added to a <see cref="CircularList{T}"/> that was created by the default constructor, capacity is increased only 23 times.</para>
    /// <para>When a list is populated only by the <see cref="Add"/> method or by the indexer, and then it is never modified, <see cref="List{T}"/> class basically has better
    /// performance. Though when the list is enumerated as an <see cref="IEnumerable{T}"/> implementation (occurs for example, when the list is used in LINQ queries or when used
    /// as an <see cref="IList{T}"/> instance), then <see cref="CircularList{T}"/> can be a better choice than <see cref="List{T}"/>, because the enumerator of
    /// <see cref="List{T}"/> class has worse performance when it is cast to the <see cref="IEnumerator{T}"/> interface. While <see cref="GetEnumerator()"/> method of
    /// <see cref="CircularList{T}"/> returns a value type enumerator (similarly to the <see cref="List{T}"/> class), when the
    /// enumerator is obtained via the <see cref="IEnumerable{T}"/> interface, the <see cref="CircularList{T}"/> returns a reference type to avoid boxing and to
    /// provide a better performance.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}; T = {typeof(T)}")]
    [Serializable]
    public sealed class CircularList<T> : IList<T>, IList
#if NET45
, IReadOnlyList<T>
#elif !(NET35 || NET40)
#error .NET version is not set or not supported!
#endif
    {
        #region Nested types

        #region Nested structs

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
            private readonly int length;

            private int index;
            private int steps;
            private T current;

            #endregion

            #region Constructors

            internal Enumerator(CircularList<T> list)
            {
                this.list = list;
                index = list.startIndex;
                version = list.version;
                length = list.items.Length;
                steps = 0;
                current = default(T);
            }

            #endregion

            #region IEnumerator<T> Members

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public T Current
            {
                get { return current; }
            }

            #endregion

            #region IDisposable Members

            /// <summary>
            /// Releases the enumerator
            /// </summary>
            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                if (version != list.version)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                if (steps < list.size)
                {
                    current = list.items[index++];
                    if (index == length)
                        index = 0;
                    steps++;
                    return true;
                }
                steps = list.size + 1;
                current = default(T);
                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (steps == 0 || steps > list.Count)
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));
                    return current;
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                if (version != list.version)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                index = list.startIndex;
                steps = 0;
                current = default(T);
            }

            #endregion
        }

        #endregion

        #endregion

        #region Nested classes

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

            internal ComparisonWrapper(Comparison<T> comparison)
            {
                this.comparison = comparison;
            }

            #endregion

            #region Methods

            public int Compare(T x, T y)
            {
                return comparison(x, y);
            }

            #endregion
        }

        #endregion

        #region BinarySearchHelper class

        /// <summary>
        /// Base class for performing binary search on a CircularList. This class uses a comparer for the search.
        /// This class accesses the CircularList through its indexer, so it is slower than Array.BinarySearch, so used only when section to search is wrapped.
        /// </summary>
        /// <typeparam name="TElem">Same as T. Type must be generic, otherwise, the derived GenericBinarySearchHelper could not call the base methods.</typeparam>
        private class BinarySearchHelper<TElem>
        {
            #region Methods

            #region Static Methods

            /// <summary>
            /// Performs a binary search on the list using the comparer.
            /// </summary>
            protected static int BinarySearchWithComparer(CircularList<TElem> list, int index, int length, TElem value, IComparer<TElem> comparer)
            {
                int lo = index;
                int hi = index + length - 1;
                while (lo <= hi)
                {
                    int i = lo + ((hi - lo) >> 1);
                    int order = comparer.Compare(list.ElementAtNonZeroStart(i), value);

                    if (order == 0)
                        return i;

                    if (order < 0)
                        lo = i + 1;
                    else
                        hi = i - 1;
                }

                return ~lo;
            }

            #endregion

            #region Instance Methods

            /// <summary>
            /// Performs a binary search on the list using the comparer or the default comparer, when comparer is null.
            /// </summary>
            internal virtual int BinarySearch(CircularList<TElem> list, int index, int length, TElem value, IComparer<TElem> comparer)
            {
                try
                {
                    // in case of enum element, comparer is already assigned
                    if (comparer == null)
                        comparer = Comparer<TElem>.Default;

                    return BinarySearchWithComparer(list, index, length, value, comparer);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Res.Get(Res.ComparerFail), e);
                }
            }

            #endregion

            #endregion
        }

        #endregion

        #region GenericBinarySearchHelper class

        /// <summary>
        /// Helper class for performing binary search on a CircularList. This class can handle elements as generic IComparable instances.
        /// This class accesses the CircularList through its indexer, so it is slower than Array.BinarySearch, so used only when section to search is wrapped.
        /// </summary>
        /// <typeparam name="TComp">Represents a <see cref="IComparable{T}"/> type.</typeparam>
        private class GenericBinarySearchHelper<TComp> : BinarySearchHelper<TComp> where TComp : IComparable<TComp>
        {
            #region Methods

            #region Static Methods

            /// <summary>
            /// Performs a binary search on the list. Elements are constrainted to be <see cref="IComparable{T}"/> instances.
            /// </summary>
            private static int BinarySearchAsComparable(CircularList<TComp> list, int index, int length, TComp value)
            {
                int lo = index;
                int hi = index + length - 1;
                while (lo <= hi)
                {
                    int i = lo + ((hi - lo) >> 1);
                    TComp item = list.ElementAtNonZeroStart(i);

                    int order;
                    if (item == null)
                    {
                        if (value == null)
                            order = 0;
                        else
                            order = -1;
                    }
                    else
                        order = item.CompareTo(value);

                    if (order == 0)
                        return i;

                    if (order < 0)
                        lo = i + 1;
                    else
                        hi = i - 1;
                }

                return ~lo;
            }

            #endregion

            #region Instance Methods

            /// <summary>
            /// Performs a binary search on the list. When comparer is specified, it is used, otherwise, using <see cref="IComparable{T}.CompareTo"/> on elements.
            /// </summary>
            internal override int BinarySearch(CircularList<TComp> list, int index, int length, TComp value, IComparer<TComp> comparer)
            {
                try
                {
                    return comparer == null || comparer == Comparer<TComp>.Default
                        ? BinarySearchAsComparable(list, index, length, value)
                        : BinarySearchWithComparer(list, index, length, value, comparer);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Res.Get(Res.ComparerFail), e);
                }
            }

            #endregion

            #endregion
        }

        #endregion

        #region EnumeratorAsReference class

        /// <summary>
        /// Enumerates the elements of a <see cref="CircularList{T}"/>.
        /// This enumerator is exactly the same as <see cref="Enumerator"/>,
        /// but is implemented as a reference type. This is returned when
        /// enumerator is requested as an <see cref="IEnumerator{T}"/> interface
        /// to avoid performance hit of boxing.
        /// </summary>
        [Serializable]
        private class EnumeratorAsReference : IEnumerator<T>
        {
            #region Fields

            private readonly CircularList<T> list;
            private readonly int version;
            private readonly int length;

            private int index;
            private int steps;
            private T current;

            #endregion

            #region Constructors

            internal EnumeratorAsReference(CircularList<T> list)
            {
                this.list = list;
                index = list.startIndex;
                version = list.version;
                length = list.items.Length;
            }

            #endregion

            #region IEnumerator<T> Members

            public T Current
            {
                get { return current; }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            public bool MoveNext()
            {
                if (version != list.version)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                if (steps < list.size)
                {
                    current = list.items[index++];
                    if (index == length)
                        index = 0;
                    steps++;
                    return true;
                }
                steps = list.size + 1;
                current = default(T);
                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (steps == 0 || steps > list.Count)
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));
                    return current;
                }
            }

            public void Reset()
            {
                if (version != list.version)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                index = list.startIndex;
                steps = 0;
                current = default(T);
            }

            #endregion
        }

        #endregion

        #region SimpleEnumerator class

        /// <summary>
        /// Enumerates the elements of a <see cref="CircularList{T}"/> when start index is 0.
        /// This enumerator is returned when enumerator is requested as an <see cref="IEnumerator{T}"/> interface
        /// to avoid performance hit of boxing.
        /// </summary>
        [Serializable]
        private class SimpleEnumeratorAsReference : IEnumerator<T>
        {
            #region Fields

            private readonly CircularList<T> list;
            private readonly int version;

            private int index;
            private T current;

            #endregion

            #region Constructors

            internal SimpleEnumeratorAsReference(CircularList<T> list)
            {
                this.list = list;
                version = list.version;
            }

            #endregion

            #region IEnumerator<T> Members

            public T Current
            {
                get { return current; }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            public bool MoveNext()
            {
                if (version != list.version)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                if (index < list.size)
                {
                    current = list.items[index++];
                    return true;
                }

                index = list.size + 1;
                current = default(T);
                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index > list.Count)
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));
                    return current;
                }
            }

            public void Reset()
            {
                if (version != list.version)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                index = 0;
                current = default(T);
            }

            #endregion
        }

        #endregion

        #endregion

        #endregion

        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        #region Static Fields

        private static readonly T[] emptyArray = new T[0];
        private static readonly bool isEnum;
#if NET40 || NET45
        private static readonly bool isNonIntEnum;
#elif !NET35
#error .NET version is not set or not supported!
#endif
        private static readonly bool isPrimitive;
        private static readonly int elementSizeExponent;

        private static BinarySearchHelper<T> binarySearchHelper;

        #endregion

        #region Instance Fields

        private T[] items;
        private int size;
        private int startIndex;
        private int version;

        [NonSerialized]
        private object syncRoot;

        #endregion

        #endregion

        #region Properties

        #region Static Properties

        private static BinarySearchHelper<T> BinarySearchHelperInstance
        {
            get
            {
                if (binarySearchHelper == null)
                {
                    if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
                    {
                        Type typeHelper = typeof(GenericBinarySearchHelper<>).MakeGenericType(typeof(T), typeof(T));
                        binarySearchHelper = (BinarySearchHelper<T>)Activator.CreateInstance(typeHelper, true);
                    }
                    else
                    {
                        binarySearchHelper = new BinarySearchHelper<T>();
                    }
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
        /// <para>Capacity is always greater than or equal to <see cref="Count"/>. If <see cref="Count"/> exceeds Capacity while adding elements,
        /// the capacity is increased by automatically reallocating the internal array before copying the old elements and adding the new elements.</para>
        /// <para>If the capacity is significantly larger than the count and you want to reduce the memory used by the <see cref="CircularList{T}"/>,
        /// you can decrease capacity by calling the <see cref="TrimExcess"/> method or by setting the Capacity property explicitly.
        /// When the value of Capacity is set explicitly, the the internal array is also reallocated to accommodate the specified capacity,
        /// and all the elements are copied.</para>
        /// <para>Retrieving the value of this property is an O(1) operation; setting the property is an O(n) operation, where n is the new capacity.</para>
        /// </remarks>
        public int Capacity
        {
            get { return items.Length; }
            set
            {
                if (value != items.Length) // deleted: || startIndex > 0
                {
                    if (value < size)
                        throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.CapacityTooSmall));

                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (size > 0)
                            CopyTo(newItems, 0);

                        items = newItems;
                        startIndex = 0;
                    }
                    else
                    {
                        items = emptyArray;
                    }
                }
            }
        }

        #endregion

        #region Internal Properties

        internal T[] Items { get { return items; } }

        internal int Version { get { return version; } }

        internal int StartIndex { get { return startIndex; } }

        #endregion

        #endregion

        #endregion

        #region Constructors

        #region Static Constructor

        static CircularList()
        {
            Type type = typeof(T);
            isEnum = type.IsEnum;
#if NET40 || NET45
            if (isEnum)
                isNonIntEnum = Enum.GetUnderlyingType(type) != typeof(int);
#elif !NET35
#error .NET version is not set or not supported!
#endif
            isPrimitive = type.IsPrimitive;
            if (isPrimitive)
            {
                elementSizeExponent = (int)Math.Log(Buffer.ByteLength(new T[1]), 2);
            }
        }

        #endregion

        #region Instance Constructors

        /// <summary>
        /// Creates a new instance of <see cref="CircularList{T}"/>.
        /// </summary>
        public CircularList()
        {
            items = emptyArray;
        }

        /// <summary>
        /// Creates a new instance of <see cref="CircularList{T}"/> that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public CircularList(int capacity)
        {
            items = new T[capacity];
        }

        /// <summary>
        /// Creates a new instance of <see cref="CircularList{T}"/> with the elements of provided <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        public CircularList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection), Res.Get(Res.ArgumentNull));

            ICollection<T> c = collection as ICollection<T> ?? collection.ToArray();
            int length = c.Count;
            items = new T[length];
            c.CopyTo(items, 0);
            size = length;
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        private static void CopyElements(T[] source, int sourceIndex, T[] dest, int destIndex, int count)
        {
            if (isPrimitive)
            {
                Buffer.BlockCopy(source, sourceIndex << elementSizeExponent, dest, destIndex << elementSizeExponent, count << elementSizeExponent);
            }
            else
            {
                Array.Copy(source, sourceIndex, dest, destIndex, count);
            }
        }

        #endregion

        #region Instance Methods

        #region Public Methods


        /// <summary>
        /// Adds an <paramref name="item"/> to the end of the list.
        /// </summary>
        /// <param name="item">The item to add to the list.</param>
        /// <remarks>
        /// <para><see cref="CircularList{T}"/> accepts <see langword="null"/> as a valid value for reference and nullable types and allows duplicate elements.</para>
        /// <para>If <see cref="Count"/> already equals <see cref="Capacity"/>, the capacity of the list is increased by automatically reallocating the internal array, and the existing elements are copied to the new array before the new element is added.</para>
        /// <para>If <see cref="Count"/> is less than <see cref="Capacity"/>, this method is an O(1) operation. If the capacity needs to be increased to accommodate the
        /// new element, this method becomes an O(n) operation, where n is <see cref="Count"/>.
        /// When adding elements continuously, the amortized cost of this method is O(1) due to the low frequency of increasing capacity. For example, when 20 million
        /// items are added to a <see cref="CircularList{T}"/> that was created by the default constructor, capacity is increased only 23 times.</para>
        /// </remarks>
        public void AddLast(T item)
        {
            if (size == items.Length)
                EnsureCapacity(size + 1);

            // faster than items[(startIndex + count) % length] = item;
            int pos = startIndex + size++;
            int length = items.Length;
            if (pos >= length)
                pos -= length;
            items[pos] = item;
            version++;
        }

        /// <summary>
        /// Adds an <paramref name="item"/> to the beginning of the list.
        /// </summary>
        /// <param name="item">The item to add to the list.</param>
        /// <remarks>
        /// <para><see cref="CircularList{T}"/> accepts <see langword="null"/> as a valid value for reference and nullable types and allows duplicate elements.</para>
        /// <para>If <see cref="Count"/> already equals <see cref="Capacity"/>, the capacity of the list is increased by automatically reallocating the internal array,
        /// and the existing elements are copied to the new array before the new element is added.</para>
        /// <para>If <see cref="Count"/> is less than <see cref="Capacity"/>, this method is an O(1) operation. If the capacity needs to be increased to accommodate the
        /// new element, this method becomes an O(n) operation, where n is <see cref="Count"/>.
        /// When adding elements continuously, the amortized cost of this method is O(1) due to the low frequency of increasing capacity. For example, when 20 million
        /// items are added to a <see cref="CircularList{T}"/> that was created by the default constructor, capacity is increased only 23 times.</para>
        /// </remarks>
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
                startIndex--;
            items[startIndex] = item;
            size++;
            version++;
        }

        /// <summary>
        /// Removes the last element of the list. This method has an O(1) cost.
        /// </summary>
        /// <returns><see langword="true"/>, if the list was not empty before the removal, otherwise, <see langword="false"/>.</returns>
        public bool RemoveLast()
        {
            if (size == 0)
                return false;

            int pos = startIndex + --size;
            int length = items.Length;
            if (pos >= length)
                pos -= length;

            items[pos] = default(T);

            if (size == 0)
                startIndex = 0;
            version++;
            return true;
        }

        /// <summary>
        /// Removes the first element of the list. This method has an O(1) cost.
        /// </summary>
        /// <returns><see langword="true"/>, if the list was not empty before the removal, otherwise, <see langword="false"/>.</returns>
        public bool RemoveFirst()
        {
            if (size == 0)
                return false;

            items[startIndex++] = default(T);

            if (--size == 0 || startIndex == items.Length)
                startIndex = 0;
            version++;
            return true;
        }

        /// <summary>
        /// Adds a <paramref name="collection"/> to the end of the list.
        /// </summary>
        /// <param name="collection">The collection to add to the list.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection), Res.Get(Res.ArgumentNull));

            AddLast(collection);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the range
        /// of elements in the list that that extends from the specified index to the last element.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <param name="index">The zero-based starting index of the search.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of item within the range of elements in the list
        /// that extends from index to the last element, if found; otherwise, –1.
        /// </returns>
        /// <para>The list is searched forward starting at <paramref name="index"/> and ending at the last element.</para>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/> type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="T"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int IndexOf(T item, int index)
        {
            return IndexOf(item, index, size - index);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the range
        /// of elements in the list that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <param name="index">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found within the range of elements in the list that starts at <paramref name="index"/>
        /// and contains <paramref name="count"/> number of elements, if found; otherwise, -1.
        /// </returns>
        /// <para>The list is searched forward starting at <paramref name="index"/> and ending at <paramref name="index"/> plus <paramref name="count"/> minus 1,
        /// if <paramref name="count"/> is greater than 0.</para>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer"/> when <typeparamref name="T"/> is an <see langword="enum"/> type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default"/> for other <typeparamref name="T"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int IndexOf(T item, int index, int count)
        {
            if ((uint)index > (uint)size)
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
            if (count < 0 || index > size - count)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));

            if (
#if NET35
                isEnum
#elif NET40 || NET45
isNonIntEnum
#else
#error .NET version is not set or not supported!
#endif
)
            {
                EnumComparer<T> enumComparer = EnumComparer<T>.Comparer;
                return FindIndex(index, count, enumItem => enumComparer.Equals(enumItem, item));
            }

            // every searched element is carried
            int length = items.Length;
            int start = startIndex + index;
            int result;
            if (start >= length)
            {
                start -= length;
                result = Array.IndexOf(items, item, start, count);
                if (result >= 0)
                    return length - startIndex + result;

                return result;
            }

            // there are also not carried elements to search
            int carry = start + count - length;

            result = Array.IndexOf(items, item, start, carry <= 0 ? count : count - carry);
            if (result >= 0)
                return result - startIndex;
            if (carry > 0)
                result = Array.IndexOf(items, item, 0, carry);
            if (result >= 0)
                return length - startIndex + result;
            return result;
        }

        /// <summary>
        /// Determines the index of the last occurrence of a specific item in the list.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>
        /// The index of the last occurrence of the <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <para>The list is searched backward starting at the last element and ending at the first element.</para>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/> type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="T"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int LastIndexOf(T item)
        {
            int length = items.Length;
            int carry = startIndex + size - length;

            if (
#if NET35
                isEnum
#elif NET40 || NET45
isNonIntEnum
#else
#error .NET version is not set or not supported!
#endif
)
            {
                EnumComparer<T> enumComparer = EnumComparer<T>.Comparer;
                return FindLastIndex(size - 1, size, enumItem => enumComparer.Equals(enumItem, item));
            }

            int result;
            if (carry > 0)
            {
                result = Array.LastIndexOf(items, item, carry - 1, carry);
                if (result >= 0)
                    return length - startIndex + result;
            }
            if (carry < 0)
                carry = 0;
            result = Array.LastIndexOf(items, item, startIndex + size - carry - 1, size - carry);
            if (result >= 0)
                return result - startIndex;
            return result;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last occurrence within the range of elements in the list that extends from the first element to the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <returns>
        /// The zero-based index of the last occurrence of <paramref name="item"/> within the range of elements in the list that extends from the first element to <paramref name="index"/>, if found; otherwise, –1.
        /// </returns>
        /// <para>The list is searched backward starting at <paramref name="index"/> and ending at the first element.</para>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/> type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="T"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int LastIndexOf(T item, int index)
        {
            return LastIndexOf(item, index, index + 1);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last occurrence within the range of elements in the list that contains the specified number of elements and ends at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <returns>
        /// The zero-based index of the last occurrence of <paramref name="item"/> within the range of elements in the list that contains <paramref name="count"/> number of elements and ends at <paramref name="item"/>, if found; otherwise, –1.
        /// </returns>
        /// <para>The list is searched backward starting at the last element and ending at the first element.</para>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/> type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="T"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        public int LastIndexOf(T item, int index, int count)
        {
            if (size != 0 && index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
            if (size != 0 && count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));

            if (size == 0)
                return -1;

            if (index >= size)
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
            if (count > index + 1)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));

            if (
#if NET35
                isEnum
#elif NET40 || NET45
isNonIntEnum
#else
#error .NET version is not set or not supported!
#endif
)
            {
                EnumComparer<T> enumComparer = EnumComparer<T>.Comparer;
                return FindLastIndex(index, count, enumItem => enumComparer.Equals(enumItem, item));
            }

            // every searched element is carried
            int length = items.Length;
            int start = startIndex + index;
            int result;
            if (start - count + 1 >= length)
            {
                start -= length;
                result = Array.LastIndexOf(items, item, start, count);
                if (result >= 0)
                    return length - startIndex + result;

                return result;
            }

            // there are not carried (and optionally carried) elements to search
            int carry = start - length + 1;
            if (carry > 0)
            {
                result = Array.LastIndexOf(items, item, carry - 1, carry);
                if (result >= 0)
                    return length - startIndex + result;

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
        /// and returns the zero-based index of the first occurrence within the list.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, size, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based
        /// index of the first occurrence within the range of elements in the list that extends from the specified index to the last element.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return FindIndex(startIndex, size - startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of
        /// the first occurrence within the range of elements in the list that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if ((uint)startIndex > (uint)size)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Res.Get(Res.ArgumentOutOfRange));
            if (count < 0 || startIndex > size - count)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));

            if (match == null)
                throw new ArgumentNullException(nameof(match), Res.Get(Res.ArgumentNull));

            int length = items.Length;
            int start = this.startIndex + startIndex;
            int carry = start + count - length;

            if (start <= length)
            {
                int endIndex = carry > 0 ? length : start + count;
                for (int i = start; i < endIndex; i++)
                {
                    if (match(items[i]))
                        return i - this.startIndex;
                }
            }

            if (carry > 0)
            {
                for (int i = 0; i < carry; i++)
                {
                    if (match(items[i]))
                        return length - this.startIndex + i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Determines whether the list contains elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The delegate that defines the conditions of the elements to search for.</param>
        /// <returns><see langword="true"/> if the list contains one or more elements that match the conditions defined by the specified predicate; otherwise, <see langword="false"/>.</returns>
        public bool Exists(Predicate<T> match)
        {
            return (FindIndex(match) != -1);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the zero-based index of the last occurrence within the list.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        public int FindLastIndex(Predicate<T> match)
        {
            return FindLastIndex(size - 1, size, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index
        /// of the last occurrence within the range of elements in the list that extends from the first element to the specified index.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the backward search.</param>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of
        /// the last occurrence within the range of elements in the list that contains the specified number of elements and ends at the specified index.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, –1.</returns>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if ((uint)startIndex > (uint)size)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Res.Get(Res.ArgumentOutOfRange));
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));
            if (match == null)
                throw new ArgumentNullException(nameof(match), Res.Get(Res.ArgumentNull));

            int length = items.Length;
            int start = this.startIndex + startIndex;
            int carry = start - length + 1;

            if (carry > 0)
            {
                int endIndex = count >= carry ? 0 : carry - count;
                for (int i = carry - 1; i >= endIndex; i--)
                {
                    if (match(items[i]))
                        return length - this.startIndex + i;
                }

                if (count <= carry)
                    return -1;

                count -= carry;
            }

            for (int i = (carry >= 0 ? length : start) - 1; count > 0; i--, count--)
            {
                if (match(items[i]))
                    return i - this.startIndex;
            }

            return -1;
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the first occurrence within the list.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The first element that matches the conditions defined by the specified predicate, if found;
        /// otherwise, the default value for type <typeparamref name="T"/>.</returns>
        public T Find(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match), Res.Get(Res.ArgumentNull));

            int length = items.Length;
            int carry = startIndex + size - length;

            int endIndex = carry > 0 ? length : startIndex + size;
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
        /// and returns the last occurrence within the list.
        /// </summary>
        /// <param name="match">A delegate that defines the conditions of the element to search for.</param>
        /// <returns>The last element that matches the conditions defined by the specified predicate, if found;
        /// otherwise, the default value for type <typeparamref name="T"/>.</returns>
        public T FindLast(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match), Res.Get(Res.ArgumentNull));

            int length = items.Length;
            int carry = startIndex + size - length;

            if (carry > 0)
            {
                for (int i = carry - 1; i >= 0; i--)
                {
                    if (match(items[i]))
                        return items[i];
                }
            }
            int endIndex = carry > 0 ? length : startIndex + size;
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
        /// <returns>An <see cref="IEnumerable{T}"/> containing all the elements that match the conditions defined by the specified predicate, if found;
        /// otherwise, an empty <see cref="IEnumerable{T}"/>.</returns>
        public IEnumerable<T> FindAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match), Res.Get(Res.ArgumentNull));

            int length = items.Length;
            int carry = startIndex + size - length;

            int endIndex = carry > 0 ? length : startIndex + size;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(items[i]))
                    yield return items[i];
            }
            if (carry > 0)
            {
                for (int i = 0; i < carry; i++)
                {
                    if (match(items[i]))
                        yield return items[i];
                }
            }
        }

        /// <summary>
        /// Inserts a <paramref name="collection"/> into the list at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="collection">The collection to insert into the list.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the list.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> must not be <see langword="null"/>.</exception>
        /// <remarks>Inserting items at the first or last position are O(1) operations if no capacity increase is needed (considering actual list size).
        /// Otherwise, insertion is an O(n) operation.</remarks>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection), Res.Get(Res.ArgumentNull));

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
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));

            ICollection<T> c = collection as ICollection<T>;
            T[] array = null;
            if (c == null || c == this)
            {
                array = collection.ToArray();
                c = null;
            }

            int colLength = c != null ? c.Count : array.Length;
            if (colLength == 0)
                return;

            int length = items.Length;
            if (size + colLength > length)
            {
                IncreaseCapacityWithInsert(index, c ?? array);
                return;
            }

            // calculating position
            int pos = startIndex + index;
            if (pos >= length)
                pos -= length;

            // optimized for minimal data moving
            if (index >= (size >> 1))
                ShiftUp(pos, size - index, colLength);
            else
            {
                // decreasing startIndex and pos
                startIndex -= colLength;
                if (startIndex < 0)
                    startIndex += length;

                int topIndex = pos > 0 ? pos - 1 : length - 1;

                pos -= colLength;
                if (pos < 0)
                    pos += length;

                ShiftDown(topIndex, index, colLength);
            }

            // if collection that fits into list without wrapping
            if (c != null && pos + colLength <= length)
            {
                c.CopyTo(items, pos);
            }
            // otherwise, as array copied up to two sessions
            else
            {
                if (array == null)
                    array = collection as T[] ?? collection.ToArray();
                int carry = pos + colLength - length;
                CopyElements(array, 0, items, pos, carry <= 0 ? colLength : colLength - carry);
                if (carry > 0)
                    CopyElements(array, colLength - carry, items, 0, carry);
            }

            size += colLength;
            version++;
        }

        /// <summary>
        /// Removes <paramref name="count"/> amount of items from the list at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the first item to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the list.
        /// <br/>-or-
        /// <br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in the list.</exception>
        /// <remarks>Removing items at the first or last positions are O(1) operations considering list size. At other positions removal is an O(n) operation, though
        /// the method is optimized for not moving more than n/2 elements.</remarks>
        public void RemoveRange(int index, int count)
        {
            if ((uint)index >= (uint)size)
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));

            if (index + count > size)
                throw new ArgumentException(Res.Get(Res.InvalidOffsLen));

            if (count == 0)
                return;

            if (index == 0)
            {
                RemoveFirst(count);
                return;
            }

            if (index + count == size)
            {
                RemoveLast(count);
                return;
            }

            int length = items.Length;

            // optimized for minimal data moving
            if (index <= size >> 1)
            {
                ShiftUp(startIndex, index, count);
                int carry = startIndex + count - length;
                Array.Clear(items, startIndex, carry <= 0 ? count : count - carry);
                if (carry > 0)
                    Array.Clear(items, 0, carry);

                startIndex += count;
                if (startIndex >= length)
                    startIndex -= length;
            }
            else
            {
                int topIndex = startIndex + size - 1;
                if (topIndex > length)
                    topIndex -= length;

                ShiftDown(topIndex, size - index - count, count);
                int carry = -(topIndex - count + 1);
                if (carry <= 0)
                    Array.Clear(items, -carry, count);
                else
                {
                    Array.Clear(items, length - carry, carry);
                    Array.Clear(items, 0, count - carry);
                }
            }

            size -= count;
            version++;
        }

        /// <summary>
        /// Copies the elements of the list to a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of the list.</returns>
        public T[] ToArray()
        {
            T[] array = new T[size];
            CopyTo(array, 0);
            return array;
        }

        /// <summary>
        /// Reverses the order of the elements in the entire list.
        /// </summary>
        public void Reverse()
        {
            Reverse(0, size);
        }

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
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));
            if (index + count > size)
                throw new ArgumentException(Res.Get(Res.InvalidOffsLen));

            int length = items.Length;
            int start = startIndex + index;
            int carry = start + count - length;

            // the section to reverse is wrapped: reversing manually via indexer
            if (carry > 0 && start < length)
            {
                int i = index;
                int j = index + count - 1;
                while (i < j)
                {
                    T temp = ElementAtNonZeroStart(i);
                    SetElementAt(i, ElementAtNonZeroStart(j));
                    SetElementAt(j, temp);
                    i++;
                    j--;
                }

                version++;
                return;
            }

            // fast reverse
            if (start >= length)
                start -= length;
            Array.Reverse(items, start, count);
            version++;
        }

        /// <summary>
        /// Sorts the elements in the entire list using the default comparer.
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
        public void Sort()
        {
            Sort(0, size, null);
        }

        /// <summary>
        /// Sorts the elements in the entire list using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/>
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
        public void Sort(IComparer<T> comparer)
        {
            Sort(0, size, comparer);
        }

        /// <summary>
        /// Sorts the elements in the entire list using the specified <see cref="Comparison{T}"/>.
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
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison), Res.Get(Res.ArgumentNull));

            if (size > 0)
            {
                Sort(0, size, new ComparisonWrapper(comparison));
            }
        }

        /// <summary>
        /// Sorts the elements in a range of elements in list using the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/>
        /// to use the default comparer <see cref="Comparer{T}.Default"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.
        /// <br/>-or-<br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not specify a valid range in the list.
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
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));
            if (index + count > size)
                throw new ArgumentException(Res.Get(Res.InvalidOffsLen));

            if (comparer == null && isEnum)
                comparer = EnumComparer<T>.Comparer;

            int length = items.Length;
            int start = startIndex + index;
            int carry = start + count - length;

            // the section to sort is wrapped
            if (carry > 0 && start < length)
            {
                // fast solution: the whole list must be sorted
                if (index == 0 && count == size)
                {
                    // fastest solution: the list is full, the full list will be sorted so mo move is needed
                    if (length == size)
                    {
                        startIndex = 0;
                        start = index;
                    }
                    else
                    {
                        int nonWrapped = size - carry;

                        // moving down the elements from the end
                        if (nonWrapped <= carry)
                        {
                            CopyElements(items, startIndex, items, carry, nonWrapped);
                            Array.Clear(items, length - nonWrapped, nonWrapped);
                            startIndex = 0;
                            start = index;
                        }
                        // moving up the elements from the start
                        else
                        {
                            CopyElements(items, 0, items, length - count, carry);
                            Array.Clear(items, 0, carry);
                            startIndex = length - count;
                            start = startIndex + index;
                        }
                    }
                }
                // slower solution: copying into new array before sorting
                else
                {
                    T[] newItems = new T[size];
                    CopyTo(newItems, 0);
                    items = newItems;
                    startIndex = 0;
                    start = index;
                }
            }

            if (start >= length)
                start -= length;
            Array.Sort(items, start, count, comparer);
            version++;
        }

        /// <summary>
        /// Searches the entire sorted list for an element using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be <see langword="null"/> for reference types.</param>
        /// <returns>The zero-based index of <paramref name="item"/> in the sorted list, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
        /// <remarks>
        /// <para>If <typeparamref name="T"/> is an <see langword="enum"/>, this method uses the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>; otherwise,
        /// the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for type <typeparamref name="T"/> to determine the order of list elements.
        /// The <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> property checks whether type <typeparamref name="T"/> implements the <see cref="IComparable{T}"/> generic interface
        /// and uses that implementation, if available. If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether type <typeparamref name="T"/> implements
        /// the <see cref="IComparable"/> interface. If type <typeparamref name="T"/> does not implement either interface, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see>
        /// throws an <see cref="InvalidOperationException"/>.</para>
        /// <para>The list must already be sorted according to the comparer implementation; otherwise, the result is incorrect.</para>
        /// <para>Comparing <see langword="null"/> with any reference type is allowed and does not generate an exception when using the <see cref="IComparable{T}"/> generic interface. When sorting, <see langword="null"/> is considered to be less than any other object.</para>
        /// <para>If the list contains more than one element with the same value, the method returns only one of the occurrences, and it might return any one of the occurrences, not necessarily the first one.</para>
        /// <para>If the list does not contain the specified value, the method returns a negative integer. You can apply the bitwise complement operation (~) to this negative integer to get the index of the first element that is larger than the search value. When inserting the value into the <see cref="CircularList{T}"/>, this index should be used as the insertion point to maintain the sort order.</para>
        /// <para>This method is an O(log n) operation, where n is the number of elements in the range.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The default comparer <see cref="Comparer{T}.Default"/> cannot find an implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.</exception>
        public int BinarySearch(T item)
        {
            return BinarySearch(0, size, item, null);
        }

        /// <summary>
        /// Searches the entire sorted list for an element using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be <see langword="null"/> for reference types.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use the <see cref="EnumComparer{TEnum}.Comparer"/> for <see langword="enum"/> element types, or the default comparer <see cref="Comparer{T}.Default"/> for other element types.</param>
        /// <returns>The zero-based index of <paramref name="item"/> in the sorted list, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
        /// <remarks>
        /// <para>The <paramref name="comparer"/> customizes how the elements are compared. For example, you can use a <see cref="CaseInsensitiveComparer"/> instance as the comparer to perform case-insensitive string searches.</para>
        /// <para>If <paramref name="comparer"/> is provided, the elements of the list are compared to the specified value using the specified <see cref="IComparer{T}"/> implementation.</para>
        /// <para>If comparer is <see langword="null"/>, then if <typeparamref name="T"/> is an <see langword="enum"/>, this method uses
        /// the <see cref="EnumComparer{TEnum}.Comparer"/>; otherwise, the default comparer <see cref="Comparer{T}.Default"/> for type <typeparamref name="T"/> to
        /// determine the order of list elements. The <see cref="Comparer{T}.Default"/> property checks whether type <typeparamref name="T"/> implements
        /// the <see cref="IComparable{T}"/> generic interface and uses that implementation, if available. If not, <see cref="Comparer{T}.Default"/> checks whether
        /// type <typeparamref name="T"/> implements the <see cref="IComparable"/> interface. If type <typeparamref name="T"/> does not implement either
        /// interface, <see cref="Comparer{T}.Default"/> throws an <see cref="InvalidOperationException"/>.</para>
        /// <para>The list must already be sorted according to the comparer implementation; otherwise, the result is incorrect.</para>
        /// <para>If comparer is <see langword="null"/>, comparing <see langword="null"/> with any reference type is allowed and does not generate an exception when using the <see cref="IComparable{T}"/> generic interface. When sorting, <see langword="null"/> is considered to be less than any other object.</para>
        /// <para>If the list contains more than one element with the same value, the method returns only one of the occurrences, and it might return any one of the occurrences, not necessarily the first one.</para>
        /// <para>If the list does not contain the specified value, the method returns a negative integer. You can apply the bitwise complement operation (~) to this negative integer to get the index of the first element that is larger than the search value. When inserting the value into the <see cref="CircularList{T}"/>, this index should be used as the insertion point to maintain the sort order.</para>
        /// <para>This method is an O(log n) operation, where n is the number of elements in the range.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and the default comparer <see cref="Comparer{T}.Default"/> cannot find an implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.</exception>
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, size, item, comparer);
        }

        /// <summary>
        /// Searches a range of elements in the sorted list for an element using the specified <paramref name="comparer"/> and returns the zero-based index of the element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to search.</param>
        /// <param name="count">The length of the range to search.</param>
        /// <param name="item">The object to locate. The value can be <see langword="null"/> for reference types.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use the 
        /// <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> for <see langword="enum"/> element types, or the default comparer
        /// <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for other element types.</param>
        /// <returns>The zero-based index of <paramref name="item"/> in the sorted list, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
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
        /// <para>If comparer is <see langword="null"/>, comparing <see langword="null"/> with any reference type is allowed and does not generate an exception when using the <see cref="IComparable{T}"/> generic interface. When sorting, <see langword="null"/> is considered to be less than any other object.</para>
        /// <para>If the list contains more than one element with the same value, the method returns only one of the occurrences, and it might return any one of the occurrences, not necessarily the first one.</para>
        /// <para>If the list does not contain the specified value, the method returns a negative integer. You can apply the bitwise complement operation (~) to this negative integer to get the index of the first element that is larger than the search value. When inserting the value into the <see cref="CircularList{T}"/>, this index should be used as the insertion point to maintain the sort order.</para>
        /// <para>This method is an O(log n) operation, where n is the number of elements in the range.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.
        /// <br/>-or-
        /// <br/><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the list.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and the default comparer <see cref="Comparer{T}.Default"/> cannot find an implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.</exception>
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));
            if (index + count > size)
                throw new ArgumentException(Res.Get(Res.InvalidOffsLen));
            if (comparer == null && isEnum)
                comparer = EnumComparer<T>.Comparer;

            int length = items.Length;
            int start = startIndex + index;
            int carry = start + count - length;

            // the section to search is wrapped: doing it manually
            if (carry > 0 && start < length)
            {
                return BinarySearchHelperInstance.BinarySearch(this, index, count, item, comparer);
            }

            // search in the non-wrapped part
            int result;
            if (carry <= 0)
            {
                result = Array.BinarySearch(items, start, count, item, comparer);
                if (startIndex == 0)
                    return result;
                return result >= 0 ? result - startIndex : ~(~result - startIndex);
            }

            // search in the wrapped-only part
            start -= length;
            result = Array.BinarySearch(items, start, count, item, comparer);
            return result >= 0 ? length - startIndex + result : ~(length - startIndex + ~result);
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the list, if that number (<see cref="Count"/>) is less than the 90 percent of <see cref="Capacity"/>.
        /// </summary>
        /// <remarks>
        /// <para>This method can be used to minimize a collection's memory overhead if no new elements will be added to the collection.
        /// The cost of reallocating and copying a large list can be considerable, however, so the TrimExcess method does nothing if the list is 
        /// at more than 90 percent of capacity. This avoids incurring a large reallocation cost for a relatively small gain.</para>
        /// <para>This method is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// <para>To reset a list to its initial state, call the <see cref="Reset"/> method. Calling the <see cref="Clear"/> and TrimExcess methods has the same effect; however,
        /// <see cref="Reset"/> method is an O(1) operation, while <see cref="Clear"/> is an O(n) operation. Trimming an empty list sets the capacity of the list to 0.</para>
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
        /// <para>Calling <see cref="Clear"/> and then <see cref="TrimExcess"/> methods also resets the list, though <see cref="Clear"/> is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        public void Reset()
        {
            if (size == 0)
                return;

            items = emptyArray;
            startIndex = 0;
            size = 0;
            version++;
        }

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
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));

            if (index + count > size)
                throw new ArgumentException(Res.Get(Res.InvalidOffsLen));

            CircularList<T> result = new CircularList<T>(count);
            result.size = count;

            // every element to copy is carried
            int length = items.Length;
            int start = startIndex + index;
            if (start >= length)
            {
                start -= length;
                CopyElements(items, start, result.items, 0, count);
                return result;
            }

            // there are also not carried elements to copy
            int carry = start + count - length;
            CopyElements(items, start, result.items, 0, carry <= 0 ? count : count - carry);

            if (carry > 0)
                CopyElements(items, 0, result.items, count - carry, carry);

            return result;
        }

        /// <summary>
        /// Determines whether every element in the list matches the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions to check against the elements.</param>
        /// <returns><see langword="true"/> if every element in the list matches the conditions defined by the specified predicate; otherwise, <see langword="false"/>.
        /// If the list has no elements, the return value is <see langword="true"/>.</returns>
        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match), Res.Get(Res.ArgumentNull));

            int length = items.Length;
            int carry = startIndex + size - length;

            int endIndex = carry > 0 ? length : startIndex + size;
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

        /// <summary>
        /// Performs the specified action on each element of the list.
        /// </summary>
        /// <param name="action">The <see cref="Action{T}"/> delegate to perform on each element of the list.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The list has been modified during the operation.</exception>
        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), Res.Get(Res.ArgumentNull));

            int ver = version;
            int length = items.Length;
            int carry = startIndex + size - length;

            int endIndex = carry > 0 ? length : startIndex + size;
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
                throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));
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
        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }

        /// <summary>
        /// Converts the elements in the current list to another type, and returns a list containing the converted elements.
        /// </summary>
        /// <typeparam name="TOutput">The type of the elements of the target list.</typeparam>
        /// <param name="converter">A <see cref="Converter{TInput,TOutput}"/> delegate that converts each element from one type to another type.</param>
        /// <returns>A <see cref="CircularList{T}"/> of the target type containing the converted elements from the current list.</returns>
        public CircularList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter), Res.Get(Res.ArgumentNull));

            CircularList<TOutput> list = new CircularList<TOutput>(size);
            int targetIndex = 0;
            int length = items.Length;
            int carry = startIndex + size - length;

            int endIndex = carry > 0 ? length : startIndex + size;
            for (int i = startIndex; i < endIndex; i++)
            {
                list.items[targetIndex++] = converter(items[i]);
            }

            if (carry > 0)
            {
                for (int i = 0; i < carry; i++)
                {
                    list.items[targetIndex++] = converter(items[i]);
                }
            }

            return list;
        }

        /// <summary>
        /// Copies the entire list to a compatible one-dimensional array, starting at the beginning of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from the list.
        /// The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
        /// <br/>-or-<br/>
        /// The number of elements in the source list is greater than the length of the destination <paramref name="array"/>.
        /// <br/>-or-<br/>
        /// Type <typeparamref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        /// <summary>
        /// Copies a range of elements from the list to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="index">The zero-based index in the source list at which copying begins.</param>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from the list.
        /// The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.
        /// <br/>-or-<br/>
        /// <paramref name="arrayIndex"/> is less than 0.
        /// <br/>-or-<br/>
        /// <paramref name="count"/> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
        /// <br/>-or-<br/>
        /// <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
        /// <br/>-or-<br/>
        /// The number of elements in the source list is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        /// <br/>-or-<br/>
        /// Type <typeparamref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (size - index < count)
                throw new ArgumentException(Res.Get(Res.InvalidOffsLen));

            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));

            if (array.Length - arrayIndex < count)
                throw new ArgumentException(Res.Get(Res.DestArrayShort), nameof(array));

            // Delegating rest error checking to Array.Copy.
            if (size <= 0)
                return;

            // every element to be copied is carried
            int length = items.Length;
            int start = startIndex + index;
            if (start >= length)
            {
                start -= length;
                CopyElements(items, start, array, arrayIndex, count);
                return;
            }

            // there are also not carried elements to copy
            int carry = start + count - length;
            CopyElements(items, start, array, arrayIndex, carry <= 0 ? count : count - carry);
            if (carry > 0)
                CopyElements(items, 0, array, count - carry + arrayIndex, carry);
        }

        /// <summary>
        /// Removes all the elements from the list that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements to remove.</param>
        /// <returns>The number of elements removed from the list.</returns>
        /// <remarks>
        /// <para>The <see cref="Predicate{T}"/> is a delegate to a method that returns <see langword="true"/> if the object passed to it matches the conditions defined in the delegate.
        /// The elements of the current <see cref="CircularList{T}"/> are individually passed to the <see cref="Predicate{T}"/> delegate, and the elements that match the conditions
        /// are removed from the list.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match), Res.Get(Res.ArgumentNull));

            // the first free slot in items array
            int freeIndex = 0;

            // Find the first item which needs to be removed. 
            while (freeIndex < size && !match(ElementAt(freeIndex)))
                freeIndex++;

            if (freeIndex >= size)
                return 0;

            int current = freeIndex + 1;
            while (current < size)
            {
                // Find the first item which needs to be kept. 
                while (current < size && match(ElementAt(current)))
                    current++;

                if (current < size)
                {
                    // copy item to the free slot.
                    SetElementAt(freeIndex++, ElementAt(current++));
                }
            }

            int result = size - freeIndex;

            // RemoveLast will adjust size and version
            RemoveLast(result);
            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CircularList{T}"/>.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a <paramref name="collection"/> to the end of the list.
        /// </summary>
        /// <param name="collection">The collection to add to the list.</param>
        private void AddLast(IEnumerable<T> collection)
        {
            ICollection<T> c = collection as ICollection<T>;
            T[] array = null;
            if (c == null || c == this)
            {
                array = collection.ToArray();
                c = null;
            }

            int colLength = c != null ? c.Count : array.Length;
            if (colLength == 0)
                return;

            EnsureCapacity(size + colLength);

            // calculating insert position
            int pos = startIndex + size;
            int length = items.Length;
            if (pos >= length)
                pos -= length;

            // if collection that fits into list without wrapping
            if (c != null && pos + colLength <= length)
            {
                c.CopyTo(items, pos);
            }
            // otherwise, as array copied up to in two sessions
            else
            {
                if (array == null)
                    array = collection as T[] ?? collection.ToArray();
                int carry = pos + colLength - length;
                CopyElements(array, 0, items, pos, carry <= 0 ? colLength : colLength - carry);
                if (carry > 0)
                    CopyElements(array, colLength - carry, items, 0, carry);
            }

            size += colLength;
            version++;
        }

        /// <summary>
        /// Adds a <paramref name="collection"/> to the beginning of the list.
        /// </summary>
        /// <param name="collection">The collection to add to the list.</param>
        private void AddFirst(IEnumerable<T> collection)
        {
            ICollection<T> c = collection as ICollection<T>;
            T[] array = null;
            if (c == null || c == this)
            {
                array = collection.ToArray();
                c = null;
            }

            int colLength = c != null ? c.Count : array.Length;
            if (colLength == 0)
                return;

            EnsureCapacity(size + colLength);

            // calculating insert position
            int pos = startIndex - colLength;
            int length = items.Length;
            if (pos < 0)
                pos += length;

            // if collection that fits into list without wrapping
            if (c != null && pos + colLength <= length)
            {
                c.CopyTo(items, pos);
            }
            // otherwise, as array copied up to two sessions
            else
            {
                if (array == null)
                    array = collection as T[] ?? collection.ToArray();
                int carry = pos + colLength - length;
                CopyElements(array, 0, items, pos, carry <= 0 ? colLength : colLength - carry);
                if (carry > 0)
                    CopyElements(array, colLength - carry, items, 0, carry);
            }

            startIndex = pos;
            size += colLength;
            version++;
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

            int length = items.Length;
            int carry = startIndex + count - length;
            Array.Clear(items, startIndex, carry <= 0 ? count : count - carry);
            if (carry > 0)
            {
                Array.Clear(items, 0, carry);
                startIndex = carry;
            }
            else
            {
                startIndex += count;
                if (startIndex == length)
                    startIndex = 0;
            }

            size -= count;
            version++;
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
            int length = items.Length;
            if (pos >= length)
                pos -= length;

            int carry = pos + count - length;
            Array.Clear(items, pos, carry <= 0 ? count : count - carry);
            if (carry > 0)
                Array.Clear(items, 0, carry);

            size -= count;
            version++;
        }

        private void EnsureCapacity(int min)
        {
            if (items.Length < min)
            {
                int newCapacity = items.Length == 0 ? defaultCapacity : items.Length << 1;
                if (newCapacity < min)
                    newCapacity = min;
                Capacity = newCapacity;
            }
        }

        private void IncreaseCapacityWithInsert(int index, T item)
        {
            Debug.Assert(size > 0);
            Debug.Assert(index > 0);

            T[] newItems = new T[items.Length << 1];
            int newSize = size + 1;

            // elements before item
            int carry = startIndex + index - items.Length;
            CopyElements(items, startIndex, newItems, 0, carry <= 0 ? index : index - carry);
            if (carry > 0)
                CopyElements(items, 0, newItems, index - carry, carry);

            // item
            newItems[index] = item;

            // elements after item
            int pos = startIndex + index;
            if (pos >= items.Length)
                pos -= items.Length;

            int restCount = size - index;
            carry = pos + restCount - items.Length;
            CopyElements(items, pos, newItems, index + 1, carry <= 0 ? restCount : restCount - carry);
            if (carry > 0)
                CopyElements(items, 0, newItems, newSize - carry, carry);

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
            CopyElements(items, startIndex, newItems, 0, carry <= 0 ? index : index - carry);
            if (carry > 0)
                CopyElements(items, 0, newItems, index - carry, carry);

            // collection items
            collection.CopyTo(newItems, index);

            // elements after collection items
            int pos = startIndex + index;
            if (pos >= items.Length)
                pos -= items.Length;

            int restCount = size - index;
            carry = pos + restCount - items.Length;
            CopyElements(items, pos, newItems, index + collection.Count, carry <= 0 ? restCount : restCount - carry);
            if (carry > 0)
                CopyElements(items, 0, newItems, newSize - carry, carry);

            items = newItems;
            size = newSize;
            startIndex = 0;
        }

        /// <summary>
        /// Shifts elements up in the stored items by 1.
        /// </summary>
        /// <param name="index">Bottom index of the elements to shift.</param>
        /// <param name="elemCount">Count of elements to shift up.</param>
        private void ShiftUp(int index, int elemCount)
        {
            int length = items.Length;
            // determining count of wrapped elements (the ones at the beginning of the physical array)
            int carry = index + elemCount - length;
            // if needed, moving them up by one
            if (carry > 0)
            {
                CopyElements(items, 0, items, 1, carry);
                elemCount -= carry;
            }
            // if needed, moving the last item in the physical array to the first position
            if (carry >= 0)
            {
                items[0] = items[length - 1];
                elemCount--;
            }
            // moving the rest of the items normally
            if (elemCount > 0)
            {
                CopyElements(items, index, items, index + 1, elemCount);
            }
        }

        /// <summary>
        /// Shifts elements up in the stored items by <paramref name="shiftCount"/>.
        /// </summary>
        /// <param name="index">Bottom index of the elements to shift.</param>
        /// <param name="elemCount">Count of elements to shift up.</param>
        /// <param name="shiftCount">Distance of the shift.</param>
        private void ShiftUp(int index, int elemCount, int shiftCount)
        {
            int length = items.Length;
            int carry = index + elemCount - length;

            // 1.) Moving up wrapped elements at the beginning of the physical array by shiftCount
            if (carry > 0)
            {
                CopyElements(items, 0, items, shiftCount, carry);
                elemCount -= carry;
                carry = 0;
            }

            // 2.) Moving min(shiftCount,elemCount) elements from the end of the array to the beginning
            carry += shiftCount;
            if (carry > 0)
            {
                int carryActual = Math.Min(carry, elemCount);
                CopyElements(items, index + elemCount - carryActual, items, carry - carryActual, carryActual);
                elemCount -= carryActual;
            }

            // 3.) Moving rest of the items up normally
            if (elemCount > 0)
            {
                CopyElements(items, index, items, index + shiftCount, elemCount);
            }
        }

        /// <summary>
        /// Shifts elements down in the stored items by 1.
        /// </summary>
        /// <param name="index">Top index of the elements to shift.</param>
        /// <param name="elemCount">Count of elements to shift down</param>
        private void ShiftDown(int index, int elemCount)
        {
            int length = items.Length;

            // determining count of wrapped elements (the ones at the end of the physical array)
            int carry = -(index - elemCount + 1);
            // if needed, moving them down by one
            if (carry > 0)
            {
                CopyElements(items, length - carry, items, length - carry - 1, carry);
                elemCount -= carry;
            }
            // if needed, moving the first item in the physical array to the last position
            if (carry >= 0)
            {
                items[length - 1] = items[0];
                elemCount--;
            }
            // moving the rest of the items normally
            if (elemCount > 0)
            {
                CopyElements(items, index - elemCount + 1, items, index - elemCount, elemCount);
            }
        }

        /// <summary>
        /// Shifts elements down in the stored items by <paramref name="shiftCount"/>.
        /// </summary>
        /// <param name="index">Top index of the elements to shift.</param>
        /// <param name="elemCount">Count of elements to shift down</param>
        /// <param name="shiftCount">Distance of the shift.</param>
        private void ShiftDown(int index, int elemCount, int shiftCount)
        {
            int length = items.Length;
            int carry = -(index - elemCount + 1);
            int sourceIndex;

            // 1.) Moving down wrapped elements at the end of the physical array by shiftCount
            if (carry > 0)
            {
                sourceIndex = length - carry;
                CopyElements(items, sourceIndex, items, sourceIndex - shiftCount, carry);
                elemCount -= carry;
                carry = 0;
            }

            // 2.) Moving min(shiftCount,elemCount) elements from the beginning of the array to the end
            carry += shiftCount;
            if (carry > 0)
            {
                int carryActual = Math.Min(carry, elemCount);
                CopyElements(items, index - elemCount + 1, items, length - carry, carryActual);
                elemCount -= carryActual;
            }

            // 3.) Moving rest of the items down normally
            if (elemCount > 0)
            {
                sourceIndex = index - elemCount + 1;
                CopyElements(items, sourceIndex, items, sourceIndex - shiftCount, elemCount);
            }
        }

        /// <summary>
        /// Gets an element without check
        /// </summary>
        private T ElementAt(int index)
        {
            if (startIndex == 0)
                return items[index];

            return ElementAtNonZeroStart(index);
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

        #endregion

        #endregion

        #endregion

        #region IList<T> Members

        /// <summary>
        /// Determines the index of a specific item in the list.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <remarks>
        /// <para>The list is searched forward starting at the first element and ending at the last element.</para>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="T"/> is an <see langword="enum"/> type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="T"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        /// </remarks>
        public int IndexOf(T item)
        {
            if (
#if NET35
                isEnum
#elif NET40 || NET45
isNonIntEnum
#else
#error .NET version is not set or not supported!
#endif
)
            {
                EnumComparer<T> enumComparer = EnumComparer<T>.Comparer;
                return FindIndex(0, size, enumItem => enumComparer.Equals(enumItem, item));
            }

            int length = items.Length;
            int carry = startIndex + size - length;

            int result = Array.IndexOf(items, item, startIndex, carry <= 0 ? size : size - carry);
            if (result >= 0)
                return result - startIndex;
            if (carry > 0)
                result = Array.IndexOf(items, item, 0, carry);
            if (result >= 0)
                return length - startIndex + result;
            return result;
        }

        /// <summary>
        /// Inserts an <paramref name="item"/> to the list at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the list.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the list.</exception>
        /// <remarks>Inserting an item at the first or last position are O(1) operations if no capacity increase is needed.
        /// Otherwise, insertion is an O(n) operation.</remarks>
        public void Insert(int index, T item)
        {
            if (index == 0)
                AddFirst(item);
            else if (index == size)
                AddLast(item);
            else
            {
                // if we are here, shifting is necessary
                if ((uint)index > (uint)size)
                    throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));

                if (size == items.Length)
                {
                    IncreaseCapacityWithInsert(index, item);
                    return;
                }

                // calculating position
                int pos = startIndex + index;
                int length = items.Length;
                if (pos >= length)
                    pos -= length;

                // optimized for minimal data moving
                if (index >= (size >> 1))
                    ShiftUp(pos, size - index);
                else
                {
                    // decreasing startIndex and pos
                    if (startIndex > 0)
                        startIndex--;
                    else
                        startIndex = length - 1;

                    if (pos > 0)
                        pos--;
                    else
                        pos = length - 1;

                    ShiftDown(pos, index);
                }

                items[pos] = item;
                size++;
                version++;
            }
        }

        /// <summary>
        /// Removes the item from the list at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the list.</exception>
        /// <remarks>Removing an item at the first or last position are O(1) operations. At other positions removal is
        /// an O(n) operation.</remarks>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)size)
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));

            if (index == 0)
            {
                RemoveFirst();
                return;
            }

            if (index == size - 1)
            {
                RemoveLast();
                return;
            }

            // if we are here, shifting is necessary and there are at least 3 elements
            // optimized for minimal data moving
            if (index <= (--size >> 1))
            {
                ShiftUp(startIndex, index);
                items[startIndex++] = default(T);
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

                items[pos] = default(T);
            }

            version++;
        }

        /// <summary>
        /// Gets or sets the element at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                // casting to uint reduces the range check by one
                if ((uint)index >= (uint)size)
                    throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));

                // not calling ElementAt to be sure this code is inlined
                if (startIndex == 0)
                    return items[index];

                // faster than return items[(startIndex + count) % length];
                int pos = startIndex + index;
                int length = items.Length;
                if (pos >= length)
                    pos -= length;

                return items[pos];
            }
            set
            {
                // casting to uint reduces the range check by one
                if ((uint)index >= (uint)size)
                    throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));

                SetElementAt(index, value);
                version++;
            }
        }

        #endregion

        #region ICollection<T> Members

        /// <summary>
        /// Adds an <paramref name="item"/> to the end of the list.
        /// </summary>
        /// <param name="item">The item to add to the list.</param>
        /// <remarks>
        /// <para><see cref="CircularList{T}"/> accepts <see langword="null"/> as a valid value for reference and nullable types and allows duplicate elements.</para>
        /// <para>If <see cref="Count"/> already equals <see cref="Capacity"/>, the capacity of the list is increased by automatically reallocating the internal array, and the existing elements are copied to the new array before the new element is added.</para>
        /// <para>If <see cref="Count"/> is less than <see cref="Capacity"/>, this method is an O(1) operation. If the capacity needs to be increased to accommodate the
        /// new element, this method becomes an O(n) operation, where n is <see cref="Count"/>.
        /// When adding elements continuously, the amortized cost of this method is O(1) due to the low frequency of increasing capacity. For example, when 20 million
        /// items are added to a <see cref="CircularList{T}"/> that was created by the default constructor, capacity is increased only 23 times.</para>
        /// </remarks>
        public void Add(T item)
        {
            AddLast(item);
        }

        /// <summary>
        /// Removes all items from the list.
        /// </summary>
        /// <remarks>
        /// <para><see cref="Count"/> is set to 0, and references to other objects from elements of the collection are also released.</para>
        /// <para>This method is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// <para><see cref="Capacity"/> remains unchanged. To reset the capacity of the list to 0 as well, call the <see cref="Reset"/> method instead, which is an O(1) operation.
        /// Calling <see cref="TrimExcess"/> after Clear also resets the list, though Clear has more cost.</para>
        /// </remarks>
        public void Clear()
        {
            if (size == 0)
                return;

            int length = items.Length;
            int carry = startIndex + size - length;

            Array.Clear(items, startIndex, carry <= 0 ? size : size - carry);
            if (carry > 0)
                Array.Clear(items, 0, carry);
            size = 0;
            startIndex = 0;
            version++;
        }

        /// <summary>
        /// Determines whether the list contains the specific <paramref name="item"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> is found in the list; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="item">The object to locate in the list.</param>
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        /// <summary>
        /// Copies the entire list to a compatible one-dimensional array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from the list.
        /// The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
        /// <br/>-or-<br/>
        /// <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
        /// <br/>-or-<br/>
        /// The number of elements in the source list is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        /// <br/>-or-<br/>
        /// Type <typeparamref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));

            if (array.Length - arrayIndex < size)
                throw new ArgumentException(Res.Get(Res.DestArrayShort), nameof(array));

            // Delegating rest error checking to Array.Copy.
            if (size <= 0)
                return;

            int carry = startIndex + size - items.Length;
            CopyElements(items, startIndex, array, arrayIndex, carry <= 0 ? size : size - carry);
            if (carry > 0)
                CopyElements(items, 0, array, size - carry + arrayIndex, carry);
        }

        /// <summary>
        /// Gets the number of elements contained in the list.
        /// </summary>
        public int Count
        {
            get { return size; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurrence of the specific <paramref name="item"/> from the list.
        /// </summary>
        /// <param name="item">The object to remove from the list.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> was successfully removed from the list; otherwise, <see langword="false"/>. This method also returns false if <paramref name="item"/> is not found in the original list.
        /// </returns>
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

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            // Since result is handled as an interface,
            // returning a reference type enumerator to avoid boxing
            if (startIndex == 0)
                return new SimpleEnumeratorAsReference(this);
            return new EnumeratorAsReference(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        #endregion

        #region IList Members

        int IList.Add(object value)
        {
            if (!typeof(T).CanAcceptValue(value))
                throw new ArgumentException(Res.Get(Res.InvalidValueType), nameof(value));
            AddLast((T)value);
            return size - 1;
        }

        bool IList.Contains(object value)
        {
            if (!typeof(T).CanAcceptValue(value))
                throw new ArgumentException(Res.Get(Res.InvalidValueType), nameof(value));
            return Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            if (!typeof(T).CanAcceptValue(value))
                throw new ArgumentException(Res.Get(Res.InvalidValueType), nameof(value));
            return IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            if (!typeof(T).CanAcceptValue(value))
                throw new ArgumentException(Res.Get(Res.InvalidValueType), nameof(value));
            Insert(index, (T)value);
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            if (!typeof(T).CanAcceptValue(value))
                throw new ArgumentException(Res.Get(Res.InvalidValueType), nameof(value));
            Remove((T)value);
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set
            {
                if (!typeof(T).CanAcceptValue(value))
                    throw new ArgumentException(Res.Get(Res.InvalidValueType), nameof(value));
                this[index] = (T)value;
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            if (array != null && array.Rank != 1)
                throw new ArgumentException(Res.Get(Res.ArrayDimension), nameof(array));

            T[] typedArray = array as T[];
            if (typedArray != null)
            {
                CopyTo(typedArray, index);
                return;
            }

            object[] objectArray = array as object[];
            if (objectArray != null)
            {
                for (int i = 0; i < size; i++)
                {
                    objectArray[index++] = ElementAt(i);
                }
            }

            throw new ArgumentException(Res.Get(Res.ArrayTypeInvalid));
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

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
    }
}
