#region Used namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;

#endregion

namespace KGySoft.Collections
{

    /// <summary>
    /// Represents a dictionary of key/value pairs that are sorted by key based on the associated <see cref="IComparer{T}"/> implementation.
    /// The dictionary behaves as list as well, as it has a direct indexed access to the elements through <see cref="Keys"/> and <see cref="Values"/> properties or by the <see cref="ElementAt"/> method.
    /// <see cref="CircularSortedList{TKey,TValue}"/> is fully compatible with <see cref="SortedList{TKey,TValue}"/>, but is generally faster than that.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="CircularSortedList{TKey,TValue}"/> generic class is an array of key/value pairs with O(log n) retrieval,
    /// where n is the number of elements in the dictionary. In this, it is similar to the <see cref="SortedList{TKey,TValue}"/> and <see cref="SortedDictionary{TKey,TValue}"/> generic classes.
    /// These three classes have similar object models, and all have O(log n) retrieval. Where the three classes differ is in memory use and speed of insertion and removal:
    /// <list type="table">
    /// <listheader><term>Collection type</term><description>Behavior</description></listheader>
    /// <item><term><see cref="SortedDictionary{TKey,TValue}"/></term>
    /// <description>
    /// <list type="bullet">
    /// <item><em>Store model and memory:</em> Elements are stored in a binary search tree where every node is an instance of a class, which references the left and right
    /// child nodes and wraps a <see cref="KeyValuePair{TKey,TValue}"/> of the element to be stored. (A node is similar to <see cref="LinkedListNode{T}"/> instances in a <see cref="LinkedList{T}"/>).
    /// This sorted dictionary variant consumes the most memory.</item>
    /// <item><em>Insertion and removal:</em> These operations have generally O(log n) cost at any position, which is the best of any sorted dictionary variants, though
    /// due to the slower navigation among nodes, the advantage of cost outperforms the other sorted dictionaries only in case of many elements (> 100)</item>
    /// <item><em>Populating from sorted data:</em> Adding a new element is always an O(log n) operation. Though when populating from sorted data, the tree always needed
    /// to be re-balanced, so it has worse performance than populating from random data.</item>
    /// <item><em>Enumerating the collection:</em> Considering that navigating among nodes is slower than array access, this sorted dictionary variant has the worse enumeration performance.</item>
    /// </list>
    /// </description></item>
    /// <item><term><see cref="SortedList{TKey,TValue}"/></term>
    /// <description>
    /// <list type="bullet">
    /// <item><em>Store model and memory:</em> Keys and values are stored in a separated array, which is the most compact storage form among the sorted dictionaries.</item>
    /// <item><em>Insertion and removal:</em> Position of the element is searched with binary search in the array, which in an O(log n) operation.
    /// Insertion/removal at the last position has a constant additional cost, so inserting/removing at the and has O(log n) cost, otherwise O(n) cost.</item>
    /// <item><em>Populating from sorted data:</em> Since position of the elements are always checked, adding a new element to the end has always O(log n) cost, though it is faster than in case of a <see cref="SortedDictionary{TKey,TValue}"/>.
    /// Though, populating from a reverse ordered data has the worst possible performance, because every already existing elements have to be shifted in the underlying arrays.</item>
    /// <item><em>Enumerating the collection:</em> Really fast, it is actually an iteration of an array.</item>
    /// </list>
    /// </description></item>
    /// <item><term><see cref="CircularSortedList{TKey,TValue}"/></term>
    /// <description>
    /// <list type="bullet">
    /// <item><em>Store model and memory:</em> Keys and values are stored in a separated <see cref="CircularList{TKey}"/>, which is a wrapper class around an array. This is a minimal overhead
    /// compared to the <see cref="SortedList{TKey,TValue}"/> class.</item>
    /// <item><em>Insertion and removal:</em> When inserting a new element, first of all it is checked, whether it comes to the last or first position. Due to the underlying <see cref="CircularList{T}"/>,
    /// inserting at the first/last position are O(1) operations. Removing an element from the last/first position by the <see cref="Remove"/> method has an O(log n) cost, because the item is found by binary search.
    /// However, removing the first or last element by the <see cref="RemoveAt"/> method is an O(1) operation. When an element is inserted/removed
    /// at any other position, it has generally O(n) cost, though the <see cref="CircularSortedList{TKey,TValue}"/> is designed so, that in worst case no more than half of the elements will be moved.</item>
    /// <item><em>Populating from sorted data:</em> Inserting element to the end of to the first position is O(1) cost, so it is faster than any other sorted dictionary types, even if populating from reverse ordered data.</item>
    /// <item><em>Enumerating the collection:</em> Really fast, it is actually an iteration of an array.</item>
    /// </list>
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>Similarly to <see cref="SortedList{TKey,TValue}"/>, <see cref="CircularSortedList{TKey,TValue}"/> supports efficient indexed retrieval of keys and values through the collections returned by the <see cref="Keys"/> and <see cref="Values"/> properties.
    /// It is not necessary to regenerate the lists when the properties are accessed, because the lists are just wrappers for the internal arrays of keys and values.</para>
    /// <para><see cref="CircularSortedList{TKey,TValue}"/> is implemented as a pair of <see cref="CircularList{T}"/> of key/value pairs, sorted by the key. Each element can be retrieved as a <see cref="KeyValuePair{TKey,TValue}"/> object.</para>
    /// <para>Key objects must be immutable as long as they are used as keys in the <see cref="CircularSortedList{TKey,TValue}"/>. Every key in a <see cref="CircularSortedList{TKey,TValue}"/> must be unique.
    /// A key cannot be <see langword="null"/>, but a value can be, if the type of values in the list, <typeparamref name="TValue"/>, is a reference type, or is a <see cref="Nullable{T}"/> type.</para>
    /// <para><see cref="CircularSortedList{TKey,TValue}"/> implements <see cref="IList{T}"/> as well, so it can be indexed directly when cast to <see cref="IList{T}"/> or though the <see cref="AsList"/> property:
    /// <code lang="C#"><![CDATA[KeyValuePair<int, string> firstItem = myCircularSortedList.AsList[0];]]></code>
    /// Setting the <see cref="P:System.Collections.Generic.IList`1.Item(System.Int32)">IList&lt;T&gt;.Item</see> property or using the <see cref="IList{T}.Insert">IList&lt;T&gt;.Insert</see> method throws
    /// a <see cref="NotSupportedException"/> for <see cref="CircularSortedList{TKey,TValue}"/>, as the position of an element cannot be set directly, it always depends on the comparer implementation.</para>
    /// <para><see cref="CircularSortedList{TKey,TValue}"/> requires a comparer implementation to sort and to perform comparisons.
    /// If comparer is not defined when <see cref="CircularSortedList{TKey,TValue}"/> is instantiated by one of the constructors, the comparer will be chosen automatically.
    /// When <typeparamref name="TKey"/> is en <see langword="enum"/>&#160;type, the comparer will be the <see cref="EnumComparer{TEnum}.Comparer"><![CDATA[EnumComparer<TEnum>.Comparer]]></see>.
    /// Otherwise, the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> will be chosen. The default comparer checks whether the key type <typeparamref name="TKey"/> implements <see cref="IComparable{T}"/> and uses that implementation, if available.
    /// If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether the key type <typeparamref name="TKey"/> implements <see cref="IComparable"/>. If the key type <typeparamref name="TKey"/> does not implement
    /// either interface, you can specify an <see cref="IComparable{T}"/> implementation in a constructor overload that accepts a comparer parameter.</para>
    /// <para>The capacity of a <see cref="CircularSortedList{TKey,TValue}"/> is the number of elements the <see cref="CircularSortedList{TKey,TValue}"/> can hold. As elements are added to a <see cref="CircularSortedList{TKey,TValue}"/>,
    /// the capacity is automatically increased as required by reallocating the internal array. The capacity can be decreased by calling <see cref="TrimExcess"/> or by setting the <see cref="Capacity"/> property explicitly.
    /// Decreasing the capacity reallocates memory and copies all the elements in the <see cref="CircularSortedList{TKey,TValue}"/>.</para>
    /// </remarks>
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ")}; TValue = {typeof(" + nameof(TValue) + ")}")]
    public class CircularSortedList<TKey, TValue> : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, IDictionary, IList
#if NET45
        , IReadOnlyDictionary<TKey, TValue>, IReadOnlyList<KeyValuePair<TKey, TValue>>
#elif !(NET35 || NET40)
#error .NET version is not set or not supported!
#endif
    {
        #region Nested types

        #region Nested classes

        #region KeysList class

        [DebuggerTypeProxy(typeof(DictionaryKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ")}")]
        [Serializable]
        private sealed class KeysList : IList<TKey>, IList
        {
            #region Fields

            private readonly CircularSortedList<TKey, TValue> list;

            [NonSerialized]
            private object syncRoot;

            #endregion

            #region Constructors

            internal KeysList(CircularSortedList<TKey, TValue> owner) => list = owner;

            #endregion

            #region IList<TKey> Members

            public int IndexOf(TKey item) => list.IndexOfKey(item);
            public void Insert(int index, TKey item) => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            public void RemoveAt(int index) => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            public TKey this[int index]
            {
                get => list.keys[index];
                set => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            }

            #endregion

            #region ICollection<TKey> Members

            public void Add(TKey item) => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            public void Clear() => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            public bool Contains(TKey item) => list.IndexOfKey(item) >= 0;
            public void CopyTo(TKey[] array, int arrayIndex) => list.keys.CopyTo(array, arrayIndex);
            public bool Remove(TKey item) => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            public int Count => list.keys.Count;
            public bool IsReadOnly => true;

            #endregion

            #region IEnumerable<TKey> Members

            public IEnumerator<TKey> GetEnumerator() 
                // casting to get enumerator as interface
                => ((IList<TKey>)list.keys).GetEnumerator();

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
                // casting to get enumerator as interface
                => ((IList<TKey>)list.keys).GetEnumerator();

            #endregion

            #region IList Members

            int IList.Add(object value) => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            bool IList.Contains(object value) => value == null ? throw new ArgumentNullException(nameof(value)) : typeKey.CanAcceptValue(value) && Contains((TKey)value);
            int IList.IndexOf(object value) => value == null ? throw new ArgumentNullException(nameof(value)) : (typeKey.CanAcceptValue(value) ? IndexOf((TKey)value) : -1);
            void IList.Insert(int index, object value) => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            bool IList.IsFixedSize => true;
            void IList.Remove(object value) => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

            object IList.this[int index]
            {
                get => list.keys[index];
                set => throw new NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            }

            #endregion

            #region ICollection Members

            void ICollection.CopyTo(Array array, int index) => ((ICollection)list.keys).CopyTo(array, index);
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
        }

        #endregion

        #region EnumeratorAsReference class

        /// <summary>
        /// Enumerates the elements of a <see cref="CircularSortedList{TKey,TValue}"/>.
        /// This enumerator is exactly the same as <see cref="Enumerator"/>,
        /// but is implemented as a reference type. This is returned when
        /// enumerator is requested as an <see cref="IEnumerator{T}"/> interface
        /// to avoid performance hit of boxing.
        /// </summary>
        [Serializable]
        private sealed class EnumeratorAsReference : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            #region Fields

            private readonly CircularSortedList<TKey, TValue> list;
            private readonly int version;
            private readonly int length;
            private readonly int size;
            private readonly TKey[] keys;
            private readonly TValue[] values;
            private readonly bool isGeneric;

            private int index;
            private int steps;
            private KeyValuePair<TKey, TValue> current;

            #endregion

            #region Constructors

            internal EnumeratorAsReference(CircularSortedList<TKey, TValue> list, bool isGeneric)
            {
                this.list = list;
                size = list.Count;
                keys = list.keys.Items;
                values = list.values.Items;
                index = list.keys.StartIndex;
                version = list.keys.Version;
                length = list.keys.Items.Length;
                this.isGeneric = isGeneric;
            }

            #endregion

            #region IEnumerator<T> Members

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public KeyValuePair<TKey, TValue> Current => current;

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
            /// <see langword="true"/>&#160;if the enumerator was successfully advanced to the next element; <see langword="false"/>&#160;if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                if (version != list.keys.Version)
                    throw new InvalidOperationException(Res.IEnumeratorCollectionModified);

                if (steps < size)
                {
                    current = new KeyValuePair<TKey, TValue>(keys[index], values[index]);
                    if (++index == length)
                        index = 0;
                    steps++;
                    return true;
                }

                steps = size + 1;
                current = default(KeyValuePair<TKey, TValue>);
                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (steps == 0 || steps > list.Count)
                        throw new InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return isGeneric ? (object)current : new DictionaryEntry(current.Key, current.Value);
                }
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                if (version != list.keys.Version)
                    throw new InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = list.keys.StartIndex;
                steps = 0;
                current = default(KeyValuePair<TKey, TValue>);
            }

            #endregion

            #region IDictionaryEnumerator Members

            DictionaryEntry IDictionaryEnumerator.Entry 
                => steps == 0 || steps > list.Count 
                    ? throw new InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished) 
                    : new DictionaryEntry(current.Key, current.Value);

            object IDictionaryEnumerator.Key 
                => steps == 0 || steps > list.Count 
                    ? throw new InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished) 
                    : current.Key;

            object IDictionaryEnumerator.Value 
                => steps == 0 || steps > list.Count 
                    ? throw new InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished) 
                    : current.Value;

            #endregion
        }

        #endregion

        #region SimpleEnumeratorAsReference class

        /// <summary>
        /// Enumerates the elements of a <see cref="CircularSortedList{TKey,TValue}"/> when start index is 0.
        /// This enumerator is returned when enumerator is requested as an <see cref="IEnumerator{T}"/> interface
        /// to avoid performance hit of boxing.
        /// </summary>
        [Serializable]
        private sealed class SimpleEnumeratorAsReference : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            #region Fields

            private readonly CircularSortedList<TKey, TValue> list;
            private readonly int version;
            private readonly int size;
            private readonly TKey[] keys;
            private readonly TValue[] values;

            private int index;
            private KeyValuePair<TKey, TValue> current;

            #endregion

            #region Constructors

            internal SimpleEnumeratorAsReference(CircularSortedList<TKey, TValue> list)
            {
                this.list = list;
                version = list.keys.Version;
                size = list.Count;
                keys = list.keys.Items;
                values = list.values.Items;
            }

            #endregion

            #region IEnumerator<T> Members

            public KeyValuePair<TKey, TValue> Current => current;

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            public bool MoveNext()
            {
                if (version != list.keys.Version)
                    throw new InvalidOperationException(Res.IEnumeratorCollectionModified);

                if (index < size)
                {
                    current = new KeyValuePair<TKey, TValue>(keys[index], values[index]);
                    index++;
                    return true;
                }

                index = size + 1;
                current = default(KeyValuePair<TKey, TValue>);
                return false;
            }

            object IEnumerator.Current => index == 0 || index > list.Count 
                ? throw new InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished) 
                : current;

            public void Reset()
            {
                if (version != list.keys.Version)
                    throw new InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = 0;
                current = default(KeyValuePair<TKey, TValue>);
            }

            #endregion
        }

        #endregion

        #endregion

        #region Nested structs

        #region Enumerator struct

        /// <summary>
        /// Enumerates the elements of a <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            #region Fields

            private readonly CircularSortedList<TKey, TValue> list;
            private readonly int version;
            private readonly int length;
            private readonly int size;
            private readonly TKey[] keys;
            private readonly TValue[] values;

            private int index;
            private int steps;
            private KeyValuePair<TKey, TValue> current;

            #endregion

            #region Constructors

            internal Enumerator(CircularSortedList<TKey, TValue> list)
            {
                this.list = list;
                size = list.Count;
                keys = list.keys.Items;
                values = list.values.Items;
                index = list.keys.StartIndex;
                version = list.keys.Version;
                length = list.keys.Items.Length;
                steps = 0;
                current = default(KeyValuePair<TKey, TValue>);
            }

            #endregion

            #region IEnumerator<T> Members

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public KeyValuePair<TKey, TValue> Current => current;

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
            /// <see langword="true"/>&#160;if the enumerator was successfully advanced to the next element; <see langword="false"/>&#160;if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                if (version != list.keys.Version)
                    throw new InvalidOperationException(Res.IEnumeratorCollectionModified);

                if (steps < size)
                {
                    current = new KeyValuePair<TKey, TValue>(keys[index], values[index]);
                    if (++index == length)
                        index = 0;
                    steps++;
                    return true;
                }

                steps = size + 1;
                current = default(KeyValuePair<TKey, TValue>);
                return false;
            }

            object IEnumerator.Current 
                => steps == 0 || steps > list.Count 
                    ? throw new InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished) 
                    : current;

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public void Reset()
            {
                if (version != list.keys.Version)
                    throw new InvalidOperationException(Res.IEnumeratorCollectionModified);

                index = list.keys.StartIndex;
                steps = 0;
                current = default(KeyValuePair<TKey, TValue>);
            }

            #endregion
        }

        #endregion

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        // ReSharper disable StaticMemberInGenericType - they depend on type arguments
        private static readonly bool isEnumKey;
        private static readonly bool isEnumValue;
        private static readonly Type typeKey;
        private static readonly Type typeValue;
        // ReSharper restore StaticMemberInGenericType

        #endregion

        #region Instance Fields

        private readonly CircularList<TKey> keys;
        private readonly CircularList<TValue> values;
        private readonly IComparer<TKey> comparer;

        [NonSerialized]
        private IList<TKey> keysList;

        [NonSerialized]
        private IList<TValue> valuesList;

        [NonSerialized]
        private object syncRoot;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the actual size of the internal storage of held elements.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Capacity is set to a value that is less than <see cref="Count"/>. </exception>
        /// <remarks>
        /// <para>Capacity is the number of elements that the <see cref="CircularSortedList{TKey,TValue}"/> can store before resizing is required, whereas
        /// <see cref="Count"/> is the number of elements that are actually in the <see cref="CircularSortedList{TKey,TValue}"/>.</para>
        /// <para>Capacity is always greater than or equal to <see cref="Count"/>. If <see cref="Count"/> exceeds Capacity while adding elements,
        /// the capacity is increased by automatically reallocating the internal <see cref="CircularList{T}"/> before copying the old elements and adding the new elements.</para>
        /// <para>If the capacity is significantly larger than the count and you want to reduce the memory used by the <see cref="CircularSortedList{TKey,TValue}"/>,
        /// you can decrease capacity by calling the <see cref="TrimExcess"/> method or by setting the Capacity property explicitly.
        /// When the value of Capacity is set explicitly, the array in the internal <see cref="CircularList{T}"/> is also reallocated to accommodate the specified capacity,
        /// and all the elements are copied.</para>
        /// <para>Retrieving the value of this property is an O(1) operation; setting the property is an O(n) operation, where n is the new capacity.</para>
        /// </remarks>
        public int Capacity
        {
            get => keys.Capacity;
            set
            {
                keys.Capacity = value;
                values.Capacity = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="IComparer{T}"/> that is used in the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        public IComparer<TKey> Comparer => comparer;

        /// <summary>
        /// Gets an indexable list containing the keys in the <see cref="CircularSortedList{TKey,TValue}"/>, in sorted order.
        /// </summary>
        /// <remarks>
        /// <para>The order of the keys in the <see cref="IList{T}"/> is the same as the order in the <see cref="CircularSortedList{TKey,TValue}"/>.</para>
        /// <para>The returned <see cref="IList{T}"/> is not a static copy; instead, the <see cref="IList{T}"/> refers back to the keys in the original <see cref="CircularSortedList{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="CircularSortedList{TKey,TValue}"/> continue to be reflected in the <see cref="IList{T}"/>.</para>
        /// <para>The collection returned by the Keys property provides an efficient way to retrieve keys by index. It is not necessary to regenerate the list when the
        /// property is accessed, because the list is just a wrapper for the internal <see cref="CircularList{T}"/> of keys. The following code shows the use of the Keys property for indexed
        /// retrieval of keys from a sorted list of elements with string keys:</para>
        /// <code lang="C#">string v = mySortedList.Keys[3];</code>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// </remarks>
        public IList<TKey> Keys => GetKeys();

        /// <summary>
        /// Gets an indexable list containing the values in the <see cref="CircularSortedList{TKey,TValue}"/>, in the order of the sorted keys.
        /// </summary>
        /// <remarks>
        /// <para>The order of the values in the <see cref="IList{T}"/> is the same as the order in the <see cref="CircularSortedList{TKey,TValue}"/>.</para>
        /// <para>The returned <see cref="IList{T}"/> is not a static copy; instead, the <see cref="IList{T}"/> refers back to the values in the original <see cref="CircularSortedList{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="CircularSortedList{TKey,TValue}"/> continue to be reflected in the <see cref="IList{T}"/>.</para>
        /// <para>The collection returned by the Values property provides an efficient way to retrieve keys by index. It is not necessary to regenerate the list when the
        /// property is accessed, because the list is just a wrapper for the internal <see cref="CircularList{T}"/> of values. The following code shows the use of the Values property for indexed
        /// retrieval of values from a sorted list of elements with string values:</para>
        /// <code lang="C#">string v = mySortedList.Values[3];</code>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// </remarks>
        public IList<TValue> Values => GetValues();

        /// <summary>
        /// Gets the <see cref="CircularSortedList{TKey,TValue}"/> cast to an <see cref="IList{T}"/>.
        /// </summary>
        /// <remarks>
        /// <para><see cref="CircularSortedList{TKey,TValue}"/> implements both <see cref="IDictionary{TKey,TValue}"/>
        /// and <see cref="IList{T}"/> interfaces. This means, for example, that two kind of indexers are available
        /// for it: <see cref="P:System.Collections.Generic.IDictionary`2.Item(`0)">IDictionary&lt;TKey,TValue&gt;.Item[TKey]</see> and
        /// <see cref="P:System.Collections.Generic.IList`1.Item(System.Int32)">IList&lt;T&gt;.Item[int]</see>. Latter is
        /// implemented as explicit interface implementation to avoid ambiguity when <typeparamref name="TKey"/> is <see cref="int">int</see>,
        /// so the <see cref="CircularSortedList{TKey,TValue}"/> should be cast to <see cref="IList{T}"/> when the list indexer
        /// is used. Alternatively, the AsList property can be used to use the indexer (and other members) of <see cref="IList{T}"/> interface
        /// as it is demonstrated in the example below.
        /// </para>
        /// <para>This property is an O(1) operation.</para>
        /// <example>
        /// <code lang="C#"><![CDATA[
        /// var coll = new CircularSortedList<int, string> { { 1, "One" }, { 2, "Two" } };
        /// var value = coll[1]; // value contains "One" - same as ((IDictionary<int, string>)coll)[1];
        /// var item = coll.AsList[1]; // item contains KeyValuePair<int, string>(2, "Two") - same as ((IList<KeyValuepair<int, string>>)coll)[1];
        /// ]]></code>
        /// </example>
        /// </remarks>
        public IList<KeyValuePair<TKey, TValue>> AsList => this;

        #endregion

        #region Constructors

        #region Static Constructor

        static CircularSortedList()
        {
            typeKey = typeof(TKey);
            typeValue = typeof(TValue);
            isEnumKey = typeKey.IsEnum;
            isEnumValue = typeValue.IsEnum;
        }

        #endregion

        #region Instance Constructors

        /// <summary>
        /// Creates a new instance of <see cref="CircularSortedList{TKey,TValue}"/> with empty capacity and a default comparer.
        /// </summary>
        /// <remarks>
        /// <para>Every key in a <see cref="CircularSortedList{TKey,TValue}"/> must be unique according to the specified comparer.</para>
        /// <para>When <typeparamref name="TKey"/> is en <see langword="enum"/>&#160;type, the comparer will be the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>.
        /// Otherwise, the default comparer <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> will be chosen.
        /// The default comparer checks whether the key type <typeparamref name="TKey"/> implements <see cref="IComparable{T}"/> and uses that implementation, if available.
        /// If not, <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> checks whether the key type <typeparamref name="TKey"/> implements <see cref="IComparable"/>.
        /// If the key type <typeparamref name="TKey"/> does not implement
        /// either interface, you can specify an <see cref="IComparable{T}"/> implementation in a constructor overload that accepts a comparer parameter.</para>
        /// </remarks>
        public CircularSortedList()
            : this(0)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="CircularSortedList{TKey,TValue}"/>, that is empty, and has the specified initial <paramref name="capacity"/>,
        /// and uses the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="CircularSortedList{TKey,TValue}"/> can contain.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;<typeparamref name="TKey"/> types, or <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>Every key in a <see cref="CircularSortedList{TKey,TValue}"/> must be unique according to the specified comparer.</para>
        /// <para>The capacity of a <see cref="CircularSortedList{TKey,TValue}"/> is the number of elements that the <see cref="CircularSortedList{TKey,TValue}"/> can hold before resizing.
        /// As elements are added to a <see cref="CircularSortedList{TKey,TValue}"/>, the capacity is automatically increased as required by reallocating the array of the internal <see cref="CircularList{T}"/>.</para>
        /// <para>If the size of the collection can be estimated, specifying the initial capacity eliminates the need to perform a number of resizing operations while adding elements to the <see cref="CircularSortedList{TKey,TValue}"/>.</para>
        /// <para>The capacity can be decreased by calling <see cref="TrimExcess"/> or by setting the <see cref="Capacity"/> property explicitly.
        /// Decreasing the capacity reallocates memory and copies all the elements in the <see cref="CircularSortedList{TKey,TValue}"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        public CircularSortedList(int capacity, IComparer<TKey> comparer = null)
        {
            keys = new CircularList<TKey>(capacity);
            values = new CircularList<TValue>(capacity);

            if (comparer == null)
                this.comparer = isEnumKey ? (IComparer<TKey>)EnumComparer<TKey>.Comparer : Comparer<TKey>.Default;
            else
                this.comparer = comparer;
        }

        /// <summary>
        /// Creates a new instance of <see cref="CircularSortedList{TKey,TValue}"/> with empty capacity, that uses the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;<typeparamref name="TKey"/> types, or <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for other types.</param>
        /// <remarks>
        /// <para>Every key in a <see cref="CircularSortedList{TKey,TValue}"/> must be unique according to the specified comparer.</para>
        /// </remarks>
        public CircularSortedList(IComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="CircularSortedList{TKey,TValue}"/>, that initializes its elements from the provided <paramref name="dictionary"/>,
        /// and uses the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="IDictionary{TKey,TValue}"/> whose elements are copied to the new S<see cref="CircularSortedList{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;<typeparamref name="TKey"/> types, or <see cref="Comparer{T}.Default">Comparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="dictionary"/> contains one or more duplicate keys.</exception>
        /// <remarks>
        /// <para>Every key in a <see cref="CircularSortedList{TKey,TValue}"/> must be unique according to the specified <paramref name="comparer"/>; likewise, every key in the source <paramref name="dictionary"/> must
        /// also be unique according to the specified <paramref name="comparer"/>.</para>
        /// <para>The capacity of the new <see cref="CircularSortedList{TKey,TValue}"/> is set to the number of elements in <paramref name="dictionary"/>, so no resizing takes place while the list is being populated.</para>
        /// <para>If the data in <paramref name="dictionary"/> are sorted, this constructor is an O(n) operation, where n is the number of elements in <paramref name="dictionary"/>.
        /// Otherwise it is an O(n*n) operation.</para>
        /// </remarks>
        public CircularSortedList(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer = null)
            : this(dictionary?.Count ?? 0, comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary), Res.ArgumentNull);

            // this way of initialization is better than the one in SortedList, which would allow duplicate keys
            foreach (KeyValuePair<TKey, TValue> item in dictionary)
                Add(item.Key, item.Value);
        }

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be <see langword="null"/>&#160;for reference and <see cref="Nullable{T}"/> types.</param>
        /// <returns>The zero-based index in the <see cref="CircularSortedList{TKey,TValue}"/> at which the key-value pair has been added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="CircularSortedList{TKey,TValue}"/>.</exception>
        /// <remarks>
        /// <para>A key cannot be <see langword="null"/>, but a value can be, if the type of values in the sorted list, <typeparamref name="TValue"/>, is a reference or <see cref="Nullable{T}"/> type.</para>
        /// <para>You can also use the <see cref="P:KGySoft.Collections.CircularSortedList`2.Item(`0)"/> property to add new elements by setting the value of a
        /// key that does not exist in the <see cref="CircularSortedList{TKey,TValue}"/>; for example, <code>myCollection["myNonexistentKey"] = myValue</code>.
        /// However, if the specified key already exists in the <see cref="CircularSortedList{TKey,TValue}"/>, setting the <see cref="P:KGySoft.Collections.CircularSortedList`2.Item(`0)"/> property
        /// overwrites the old value. In contrast, the Add method does not modify existing elements.</para>
        /// <para>If <see cref="Count"/> already equals <see cref="Capacity"/>, the capacity of the <see cref="CircularSortedList{TKey,TValue}"/> is increased by
        /// automatically reallocating the array in internal <see cref="CircularList{T}"/>, and the existing elements are copied to the new array before the new element is added.</para>
        /// <para>This method is an O(n) operation for unsorted data, where n is <see cref="Count"/>. It is an O(1) operation if the new element is added at the end or the head of the list.
        /// If insertion causes a resize, the operation is O(n).</para>
        /// </remarks>
        public int Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.ArgumentNull);

            int pos = SearchKeyOptimizedLastOrFirst(key);
            if (pos >= 0)
                throw new ArgumentException(Res.IDictionaryDuplicateKey, nameof(key));

            pos = ~pos;
            Insert(pos, key, value);
            return pos;
        }

        /// <summary>
        /// Searches for the specified <paramref name="key"/> and returns the zero-based index within the entire <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="CircularSortedList{TKey,TValue}"/>.</param>
        /// <returns>The zero-based index of <paramref name="key"/> within the entire <see cref="CircularSortedList{TKey,TValue}"/>, if found; otherwise, -1.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <remarks>This method performs a binary search; therefore, this method is an O(log n) operation, where n is <see cref="Count"/>.</remarks>
        public int IndexOfKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.ArgumentNull);

            int index = keys.BinarySearch(key, comparer);
            if (index < 0)
                return -1;

            return index;
        }

        /// <summary>
        /// Determines whether the <see cref="CircularSortedList{TKey,TValue}"/> contains a specific value.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="CircularSortedList{TKey,TValue}"/>. The value can be <see langword="null"/>&#160;for reference and <see cref="Nullable{T}"/> types.</param>
        /// <returns><see langword="true"/>&#160;if the <see cref="CircularSortedList{TKey,TValue}"/> contains an element with the specified <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="TValue"/> is an <see langword="enum"/>&#160;type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="TValue"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        /// </remarks>
        public bool ContainsValue(TValue value) => values.IndexOf(value) >= 0;

        /// <summary>
        /// Searches for the specified value and returns the zero-based index of the first occurrence within the entire <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// The value can be <see langword="null"/>&#160;for reference and <see cref="Nullable{T}"/> types.</param>
        /// <returns>The zero-based index of the first occurrence of value within the entire <see cref="CircularSortedList{TKey,TValue}"/>, if found; otherwise, -1.</returns>
        /// <remarks>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="TValue"/> is an <see langword="enum"/>&#160;type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="TValue"/> types.
        /// <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> checks whether the value type <typeparamref name="TValue"/> implements <see cref="IEquatable{T}"/> and uses
        /// that implementation, if available. If not, <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> uses <see cref="object.Equals(object)">Object.Equals</see>.
        /// </para>
        /// <para>This method performs a linear search; therefore, the average execution time is proportional to <see cref="Count"/>. That is, this method is an O(n)
        /// operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        public int IndexOfValue(TValue value) => values.IndexOf(value);

        /// <summary>
        /// Sets the capacity to the actual number of elements in the <see cref="CircularSortedList{TKey,TValue}"/>, if that number is less than 90 percent of current capacity.
        /// </summary>
        /// <remarks>
        /// <para>This method can be used to minimize a collection's memory overhead if no new elements will be added to the collection. The cost of reallocating and
        /// copying a large <see cref="CircularSortedList{TKey,TValue}"/> can be considerable, however, so the TrimExcess method does nothing if the list is at more
        /// than 90 percent of capacity. This avoids incurring a large reallocation cost for a relatively small gain.</para>
        /// <para>This method is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// <para>To reset a <see cref="CircularSortedList{TKey,TValue}"/> to its initial state, call the <see cref="Reset"/> method. Calling the <see cref="Clear"/> and TrimExcess methods has the same effect; however,
        /// <see cref="Reset"/> method is an O(1) operation, while <see cref="Clear"/> is an O(n) operation. Trimming an empty <see cref="CircularSortedList{TKey,TValue}"/> sets the capacity of the list to 0.</para>
        /// <para>The capacity can also be set using the Capacity property.</para>
        /// </remarks>
        public void TrimExcess()
        {
            keys.TrimExcess();
            values.TrimExcess();
        }

        /// <summary>
        /// Removes all items from the <see cref="CircularSortedList{TKey,TValue}"/> and resets the <see cref="Capacity"/> to 0.
        /// </summary>
        /// <remarks>
        /// <para><see cref="Count"/> and <see cref="Capacity"/> are set to 0, and references to other objects from elements of the collection are also released.</para>
        /// <para>This method is an O(1) operation.</para>
        /// <para>Calling <see cref="Clear"/> and then <see cref="TrimExcess"/> methods also resets the <see cref="CircularSortedList{TKey,TValue}"/>, though
        /// <see cref="Clear"/> is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        public void Reset()
        {
            keys.Reset();
            values.Reset();
        }

        /// <summary>
        /// Gets a <see cref="KeyValuePair{TKey,TValue}"/> of the <typeparamref name="TKey"/> and <typeparamref name="TValue"/> elements
        /// at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularSortedList{TKey,TValue}"/></exception>
        public KeyValuePair<TKey, TValue> ElementAt(int index) => new KeyValuePair<TKey, TValue>(keys[index], values[index]);

        /// <summary>
        /// Returns an enumerator that iterates through the collection in the order of the sorted keys.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        #endregion

        #region Private Methods

        /// <summary>
        /// Searches a key element first checking the last and first items.
        /// </summary>
        /// <param name="key">The key element to search</param>
        /// <returns>The non-negative index if key is found; otherwise, the bitwise complement of the index where the element can be inserted.</returns>
        private int SearchKeyOptimizedLastOrFirst(TKey key)
        {
            int size = keys.Count;

            // no elements: ~0 == -1
            if (size == 0)
                return -1;

            // comparing to the last element
            int lastIndex = size - 1;
            int order = comparer.Compare(key, keys[lastIndex]);

            // key is larger than last element
            if (order > 0)
                return ~size;

            // found at last position
            if (order == 0)
                return lastIndex;

            // size is 1 and key is less than that: ~0 == -1
            if (size == 1)
                return -1;

            // comparing to the first element
            order = comparer.Compare(key, keys[0]);

            // key is smaller than first element
            if (order < 0)
                return -1;

            // found at first position
            if (order == 0)
                return 0;

            // searching in the middle
            switch (size)
            {
                case 2:
                    // size is 2: no elements in the middle
                    return ~1;

                case 3:
                    // size is 3: one element in the middle
                    order = comparer.Compare(key, keys[1]);

                    // key should come after the middle
                    if (order > 0)
                        return ~2;

                    // key should come before the middle
                    if (order < 0)
                        return ~1;

                    // found at the middle
                    return 1;

                default:
                    // performing binary search for the elements in the middle
                    return keys.BinarySearch(1, size - 2, key, comparer);
            }
        }

        private void Insert(int index, TKey key, TValue value)
        {
            keys.Insert(index, key);
            values.Insert(index, value);
        }

        private int IndexOf(KeyValuePair<TKey, TValue> item)
        {
            int index = IndexOfKey(item.Key);
            if (index < 0)
                return index;

            if (isEnumValue)
                return EnumComparer<TValue>.Comparer.Equals(item.Value, values[index]) ? index : -1;

            return EqualityComparer<TValue>.Default.Equals(item.Value, values[index]) ? index : -1;
        }

        private IList<TKey> GetKeys() => keysList ?? (keysList = new KeysList(this));

        private IList<TValue> GetValues() => valuesList ?? (valuesList = values.AsReadOnly());

        #endregion

        #endregion

        #region IDictionary<TKey,TValue> Members

        /// <summary>
        /// Determines whether the <see cref="CircularSortedList{TKey,TValue}"/> contains an element with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>, if the <see cref="CircularSortedList{TKey,TValue}"/> contains an element with the <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key to locate in the <see cref="CircularSortedList{TKey,TValue}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <remarks>This method is an O(log n) operation, where n is <see cref="Count"/>.</remarks>
        public bool ContainsKey(TKey key) => IndexOfKey(key) >= 0;

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => Add(key, value);

        /// <summary>
        /// Removes the element with the specified <paramref name="key"/> from the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if the element is successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <remarks>This method performs a binary search; however, the elements are moved up to fill in the open spot.
        /// So this method is an O(log n) operation, when the first or last element is removed; otherwise, O(n), where n is <see cref="Count"/>.
        /// If it is known that the first or last element should be removed, use <see cref="RemoveAt"/> instead, which is an O(1) operation in this case.</remarks>
        public bool Remove(TKey key)
        {
            int index = IndexOfKey(key);
            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Gets the <paramref name="value"/> associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if the <see cref="CircularSortedList{TKey,TValue}"/> contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>If the <paramref name="key"/> is not found, then the value parameter gets the appropriate default value for the value type <typeparamref name="TValue"/>;
        /// for example, zero (0) for integer types, <see langword="false"/>&#160;for Boolean types, and <see langword="null"/>&#160;for reference types.</para>
        /// <para>This method performs a binary search; therefore, this method is an O(log n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        public bool TryGetValue(TKey key, out TValue value)
        {
            int index = IndexOfKey(key);
            if (index >= 0)
            {
                value = values[index];
                return true;
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Gets or sets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>
        /// The element with the specified <paramref name="key"/>.
        /// </returns>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
        /// <remarks>
        /// <para>A key cannot be <see langword="null"/>, but a value can be, if the type of values in the list, <typeparamref name="TValue"/>, is a reference or <see cref="Nullable{T}"/> type.</para>
        /// <para>If the <paramref name="key"/> is not found when a value is being retrieved, <see cref="KeyNotFoundException"/> is thrown.
        /// If the key is not found when a value is being set, the key and value are added.</para>
        /// <para>You can also use this property to add new elements by setting the value of a key that does not exist in the <see cref="CircularSortedList{TKey,TValue}"/>; for example,
        /// <code>myCollection["myNonexistentKey"] = myValue</code>. However, if the specified key already exists in the <see cref="CircularSortedList{TKey,TValue}"/>, setting this property
        /// overwrites the old value. In contrast, the <see cref="Add"/> throws an <see cref="ArgumentException"/>, when <paramref name="key"/> already exists in the collection.</para>
        /// <para>Retrieving the value of this property is an O(log n) operation, where n is <see cref="Count"/>. Setting the property is an O(1) operation if the <paramref name="key"/>
        /// is at the first or last position. Otherwise, setting this property is an O(log n) operation, if the <paramref name="key"/> already exists in the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// If the <paramref name="key"/> is not in the list, and the new element is not at the first or last position, setting the property is an O(n) operation. If insertion causes a resize, the operation is O(n).</para>
        /// </remarks>
        public TValue this[TKey key]
        {
            get
            {
                int index = IndexOfKey(key);
                if (index >= 0)
                    return values[index];

                throw new KeyNotFoundException(Res.IDictionaryKeyNotFound);
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key), Res.ArgumentNull);

                int index = SearchKeyOptimizedLastOrFirst(key);
                if (index >= 0)
                    values[index] = value;
                else
                    Insert(~index, key, value);
            }
        }

        /// <summary>
        /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="ICollection{T}"/> containing the keys of the object that implements <see cref="IDictionary{TKey,TValue}"/>.
        /// </returns>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => GetKeys();

        /// <summary>
        /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="ICollection{T}"/> containing the values in the object that implements <see cref="IDictionary{TKey,TValue}"/>.
        /// </returns>
        ICollection<TValue> IDictionary<TKey, TValue>.Values => GetValues();

        #endregion

        #region IList<KeyValuePair<TKey,TValue>> Members

        int IList<KeyValuePair<TKey, TValue>>.IndexOf(KeyValuePair<TKey, TValue> item)
        {
            int index = IndexOfKey(item.Key);
            if (index < 0)
                return -1;

            if (isEnumValue)
                return EnumComparer<TValue>.Comparer.Equals(item.Value, values[index]) ? index : -1;

            return EqualityComparer<TValue>.Default.Equals(item.Value, values[index]) ? index : -1;
        }

        void IList<KeyValuePair<TKey, TValue>>.Insert(int index, KeyValuePair<TKey, TValue> item) => throw new NotSupportedException(Res.CircularSortedListInsertByIndexNotSupported);

        /// <summary>
        /// Removes the element at the specified index of the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="CircularSortedList{TKey,TValue}"/>.</exception>
        /// <remarks>When the first or the last element is removed, this method is an O(1) operation; otherwise, an O(n) operation, where n is <see cref="Count"/>.</remarks>
        public void RemoveAt(int index)
        {
            keys.RemoveAt(index);
            values.RemoveAt(index);
        }

        KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index]
        {
            get => ElementAt(index);
            set => throw new NotSupportedException(Res.CircularSortedListInsertByIndexNotSupported);
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        /// <summary>
        /// Removes all items from the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para><see cref="Count"/> is set to 0, and references to other objects from elements of the collection are also released.</para>
        /// <para>This method is an O(n) operation, where n is <see cref="Count"/>.</para>
        /// <para><see cref="Capacity"/> remains unchanged. To reset the capacity of the <see cref="CircularSortedList{TKey,TValue}"/> to 0 as well,
        /// call the <see cref="Reset"/> method instead, which is an O(1) operation.
        /// Calling <see cref="TrimExcess"/> after Clear also resets the list, though Clear has more cost.</para>
        /// </remarks>
        public void Clear()
        {
            keys.Clear();
            values.Clear();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => IndexOf(item) >= 0;

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.ArgumentNull);

            if ((arrayIndex < 0) || (arrayIndex > array.Length))
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), Res.ArgumentOutOfRange);

            int size = keys.Count;
            if ((array.Length - arrayIndex) < size)
                throw new ArgumentException(Res.ICollectionCopyToDestArrayShort, nameof(array));

            for (int i = 0; i < size; i++)
                array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            int index = IndexOfKey(item.Key);
            if (index < 0)
                return false;

            bool equals = isEnumValue
                ? EnumComparer<TValue>.Comparer.Equals(item.Value, values[index])
                : EqualityComparer<TValue>.Default.Equals(item.Value, values[index]);

            if (!equals)
                return false;

            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        /// The number of key/value pairs contained in the <see cref="CircularSortedList{TKey,TValue}"/>.
        /// </returns>
        public int Count => keys.Count;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => keys.StartIndex == 0
                ? (IEnumerator<KeyValuePair<TKey, TValue>>)new SimpleEnumeratorAsReference(this)
                : new EnumeratorAsReference(this, true);

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();

        #endregion

        #region IDictionary Members

        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.ArgumentNull);
            if (!typeKey.CanAcceptValue(key))
                throw new ArgumentException(Res.IDictionaryNongenericKeyTypeInvalid(value, typeof(TKey)), nameof(key));
            if (!typeValue.CanAcceptValue(value))
                throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(value, typeof(TValue)), nameof(value));
            Add((TKey)key, (TValue)value);
        }

        bool IDictionary.Contains(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.ArgumentNull);
            return typeKey.CanAcceptValue(key) && ContainsKey((TKey)key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() => new EnumeratorAsReference(this, false);
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        ICollection IDictionary.Keys => (ICollection)Keys;
        ICollection IDictionary.Values => (ICollection)Values;

        void IDictionary.Remove(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.ArgumentNull);
            if (typeKey.CanAcceptValue(key))
                Remove((TKey)key);
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key), Res.ArgumentNull);
                if (!typeKey.CanAcceptValue(key))
                    throw new ArgumentException(Res.IDictionaryNongenericKeyTypeInvalid(key, typeof(TKey)), nameof(key));
                return this[(TKey)key];
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key), Res.ArgumentNull);
                if (!typeKey.CanAcceptValue(key))
                    throw new ArgumentException(Res.IDictionaryNongenericKeyTypeInvalid(value, typeof(TKey)), nameof(key));
                if (!typeValue.CanAcceptValue(value))
                    throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(value, typeof(TValue)), nameof(value));
                this[(TKey)key] = (TValue)value;
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.ArgumentNull);
            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), Res.ArgumentOutOfRange);
            if (array.Length - index < Count)
                throw new ArgumentException(Res.ICollectionCopyToDestArrayShort, nameof(index));
            if (array.Rank != 1)
                throw new ArgumentException(Res.ICollectionCopyToSingleDimArrayOnly, nameof(array));

            int size = keys.Count;
            if (size == 0)
                return;

            if (array is KeyValuePair<TKey, TValue>[] keyValuePairs)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(keyValuePairs, index);
                return;
            }

            if (array is DictionaryEntry[] dictionaryEntries)
            {
                for (int i = 0; i < size; i++)
                    dictionaryEntries[index++] = new DictionaryEntry(keys[i], values[i]);
            }

            if (array is object[] objectArray)
            {
                for (int i = 0; i < size; i++)
                    objectArray[index++] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
            }

            throw new ArgumentException(Res.ICollectionArrayTypeInvalid);
        }

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

        #region IList Members

        int IList.Add(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), Res.ArgumentNull);

            if (value is KeyValuePair<TKey, TValue> keyValuePair)
                return Add(keyValuePair.Key, keyValuePair.Value);

            if (value is DictionaryEntry dictionaryEntry)
            {
                if (!typeKey.CanAcceptValue(dictionaryEntry.Key))
                    throw new ArgumentException(Res.IDictionaryNongenericKeyTypeInvalid(dictionaryEntry, typeof(TKey)), nameof(value));
                if (!typeValue.CanAcceptValue(dictionaryEntry.Value))
                    throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(dictionaryEntry, typeof(TValue)), nameof(value));
                return Add((TKey)dictionaryEntry.Key, (TValue)dictionaryEntry.Value);
            }

            throw new ArgumentException(Res.CircularSortedListInvalidKeyValueType(typeof(KeyValuePair<TKey, TValue>)), nameof(value));
        }

        bool IList.Contains(object value)
        {
            if (value is KeyValuePair<TKey, TValue> keyValuePair)
                return IndexOf(keyValuePair) >= 0;

            if (value is DictionaryEntry entry)
            {
                if (!typeKey.CanAcceptValue(entry.Key))
                    throw new ArgumentException(Res.IDictionaryNongenericKeyTypeInvalid(entry, typeof(TKey)), nameof(value));
                if (!typeValue.CanAcceptValue(entry.Value))
                    throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(entry, typeof(TValue)), nameof(value));
                return IndexOf(new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value)) >= 0;
            }

            return false;
        }

        int IList.IndexOf(object value)
        {
            if (value is KeyValuePair<TKey, TValue> keyValuePair)
                return IndexOf(keyValuePair);

            if (value is DictionaryEntry entry)
            {
                if (!typeKey.CanAcceptValue(entry.Key))
                    throw new ArgumentException(Res.IDictionaryNongenericKeyTypeInvalid(entry, typeof(TKey)), nameof(value));
                if (!typeValue.CanAcceptValue(entry.Value))
                    throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(entry, typeof(TValue)), nameof(value));
                return IndexOf(new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value));
            }

            return -1;
        }

        void IList.Insert(int index, object value) => throw new NotSupportedException(Res.CircularSortedListInsertByIndexNotSupported);
        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => false;

        void IList.Remove(object value)
        {
            if (value is KeyValuePair<TKey, TValue> keyValuePair)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)this).Remove(keyValuePair);
                return;
            }

            if (value is DictionaryEntry entry)
            {
                if (!typeKey.CanAcceptValue(entry.Key))
                    throw new ArgumentException(Res.IDictionaryNongenericKeyTypeInvalid(entry, typeof(TKey)), nameof(value));
                if (!typeValue.CanAcceptValue(entry.Value))
                    throw new ArgumentException(Res.ICollectionNongenericValueTypeInvalid(entry, typeof(TValue)), nameof(value));
                ((ICollection<KeyValuePair<TKey, TValue>>)this).Remove(new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value));
            }
        }

        object IList.this[int index]
        {
            get => ElementAt(index);
            set => throw new NotSupportedException(Res.CircularSortedListInsertByIndexNotSupported);
        }

        #endregion

#if !(NET35 || NET40)
        #region IReadOnlyDictionary<TKey,TValue> Members

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => GetKeys();
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => GetValues();

        #endregion

        #region IReadOnlyList<KeyValuePair<TKey,TValue>> Members

        KeyValuePair<TKey, TValue> IReadOnlyList<KeyValuePair<TKey, TValue>>.this[int index] => ElementAt(index);

        #endregion
#endif
    }
}
