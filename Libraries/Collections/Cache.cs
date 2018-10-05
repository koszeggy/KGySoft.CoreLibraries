#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Cache.cs
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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

using KGySoft.Annotations;
using KGySoft.Diagnostics;
using KGySoft.Libraries;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a generic cache. If an item loader is specified, then cache expansion is transparent; user needs only to read the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> to retrieve items.
    /// When a non-existing key is accessed, then item is loaded automatically by the loader function that was passed to the
    /// <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
    /// If the cache is full (elements <see cref="Count"/> reaches the <see cref="Capacity"/>) and a new element has to be stored, then
    /// the oldest or least recent used element (depends on the value of <see cref="Behavior"/>) is removed from the cache.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys stored in the cache.</typeparam>
    /// <typeparam name="TValue">Type of the values stored in the cache.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="Cache{TKey,TValue}"/> type provides a fast-access storage with limited capacity and transparent access. If you need to store
    /// items that are expensive to retrieve (for example from a database or remote service) and you don't want to run out of memory because of
    /// just storing newer and newer elements without getting rid of old ones, then this type might fit your expectations.
    /// Once a value is stored in the cache, its retrieval by using its key is very fast, close to O(1).
    /// </para>
    /// <para>
    /// A cache store must meet the following three criteria:
    /// <list type="number">
    /// <item><term>Associative access</term><description>Accessing elements works the same way as in case of the <see cref="Dictionary{TKey,TValue}"/> type.
    /// <see cref="Cache{TKey,TValue}"/> implements both the generic <see cref="IDictionary{TKey,TValue}"/> and the non-generic <see cref="IDictionary"/> interfaces so can be
    /// used similarly as <see cref="Dictionary{TKey,TValue}"/> or <see cref="Hashtable"/> types.</description></item>
    /// <item><term>Transparency</term><description>Users of the cache need only to read the cache by its <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> property.
    /// If needed, elements will be automatically loaded on the first access.</description></item>
    /// <item><term>Size management</term><description><see cref="Cache{TKey,TValue}"/> type has a <see cref="Capacity"/>, which is the allowed maximal elements count. If the cache is full, the
    /// oldest or least recent used element will be automatically removed from the cache (see <see cref="Behavior"/> property).</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Since <see cref="Cache{TKey,TValue}"/> implements <see cref="IDictionary{TKey,TValue}"/> interface, <see cref="Add">Add</see>, <see cref="Remove">Remove</see>, <see cref="ContainsKey">ContainsKey</see> and
    /// <see cref="TryGetValue">TryGetValue</see> methods are available for it, and these methods work exactly the same way as in case the <see cref="Dictionary{TKey,TValue}"/> type. But using these methods
    /// usually are not necessary, unless we want to manually manage cache content or when cache is initialized without an item loader. Normally after cache is instantiated,
    /// it is needed to be accessed only by the getter accessor of its indexer.
    /// </para>
    /// <note type="caution">
    /// Serializing a cache instance by <see cref="IFormatter"/> implementations involves the serialization of the item loader delegate. To deserialize a cache the assembly of the loader must be accessible. If you need to
    /// serialize cache instances try to use static methods as data loaders and avoid using anonymous delegates or lambda expressions, otherwise it is not guaranteed that another
    /// implementations or versions of CLR will able to deserialize data and resolve the compiler-generated members.
    /// </note>
    /// </remarks>
    /// <example>
    /// The following example shows the suggested usage of <see cref="Cache{TKey,TValue}"/>.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections.Generic;
    /// using KGySoft.Collections;
    /// 
    /// class Example
    /// {
    ///     private static Cache<int, bool> isPrimeCache;
    /// 
    ///     public static void Main()
    ///     {
    ///         // Cache capacity is initialized to store maximum 4 values
    ///         isPrimeCache = new Cache<int, bool>(ItemLoader, 4);
    /// 
    ///         // If cache is full the least recent used element will be deleted
    ///         isPrimeCache.Behavior = CacheBehavior.RemoveLeastRecentUsedElement;
    /// 
    ///         // cache is now empty
    ///         DumpCache();
    /// 
    ///         // reading the cache invokes the loader method
    ///         CheckPrime(13);
    /// 
    ///         // reading a few more values
    ///         CheckPrime(23);
    ///         CheckPrime(33);
    ///         CheckPrime(43);
    /// 
    ///         // dumping content
    ///         DumpCache();
    /// 
    ///         // accessing an already read item does not invoke loader again
    ///         // Now it changes cache order because of the chosen behavior
    ///         CheckPrime(13);
    ///         DumpCache();
    /// 
    ///         // reading a new element with full cache will delete an old one (now 23)
    ///         CheckPrime(111);
    ///         DumpCache();
    /// 
    ///         // but accessing a deleted element causes to load it again
    ///         CheckPrime(23);
    ///         DumpCache();
    /// 
    ///         // dumping some statistics
    ///         Console.WriteLine(isPrimeCache.GetStatistics().ToString());
    ///     }
    /// 
    ///     // This is the item loader method. It can access database or perform slow calculations.
    ///     // If cache is meant to be serialized it should be a static method rather than an anonymous delegate or lambda expression.
    ///     private static bool ItemLoader(int number)
    ///     {
    ///         Console.WriteLine("Item loading has been invoked for value {0}", number);
    /// 
    ///         // In this example item loader checks whether the given number is a prime by a not too efficient algorithm.
    ///         if (number <= 1)
    ///             return false;
    ///         if (number % 2 == 0)
    ///             return true;
    ///         int i = 3;
    ///         int sqrt = (int)Math.Floor(Math.Sqrt(number));
    ///         while (i <= sqrt)
    ///         {
    ///             if (number % i == 0)
    ///                 return false;
    ///             i += 2;
    ///         }
    /// 
    ///         return true;
    ///     }
    /// 
    ///     private static void CheckPrime(int number)
    ///     {
    ///         // cache is used transparently here: indexer is always just read
    ///         bool isPrime = isPrimeCache[number];
    ///         Console.WriteLine("{0} is a prime: {1}", number, isPrime);
    ///     }
    /// 
    ///     private static void DumpCache()
    ///     {
    ///         Console.WriteLine();
    ///         Console.WriteLine("Cache elements count: {0}", isPrimeCache.Count);
    ///         if (isPrimeCache.Count > 0)
    ///         {
    ///             // enumerating through the cache shows the elements in the evaluation order
    ///             Console.WriteLine("Cache elements:");
    ///             foreach (KeyValuePair<int, bool> item in isPrimeCache)
    ///             {
    ///                 Console.WriteLine("\tKey: {0},\tValue: {1}", item.Key, item.Value);
    ///             }
    ///         }
    /// 
    ///         Console.WriteLine();
    ///     }
    /// }
    /// 
    /// // This code example produces the following output:
    /// // 
    /// // Item loading has been invoked for value 111
    /// // 111 is a prime: False
    /// // 
    /// // Cache elements count: 4
    /// // Cache elements:
    /// // Key: 33,        Value: False
    /// // Key: 43,        Value: True
    /// // Key: 13,        Value: True
    /// // Key: 111,       Value: False
    /// // 
    /// // Item loading has been invoked for value 23
    /// // 23 is a prime: True
    /// // 
    /// // Cache elements count: 4
    /// // Cache elements:
    /// // Key: 43,        Value: True
    /// // Key: 13,        Value: True
    /// // Key: 111,       Value: False
    /// // Key: 23,        Value: True
    /// // 
    /// // Cache<Int32, Boolean> cache statistics:
    /// // Count: 4
    /// // Capacity: 4
    /// // Number of writes: 6
    /// // Number of reads: 7
    /// // Number of cache hits: 1
    /// // Number of deletes: 2
    /// // Hit rate: 14,29%]]></code></example>
    /// <seealso cref="CacheBehavior"/>
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ")}; TValue = {typeof(" + nameof(TValue) + ")}; Hit = {" + nameof(Cache<_,_>.GetStatistics) + "()." + nameof(ICacheStatistics.HitRate) + " * 100}%")]
    public sealed class Cache<TKey, TValue> : IDictionary<TKey, TValue>, ICache, ISerializable
#if !(NET35 || NET40)
        , IReadOnlyDictionary<TKey, TValue>
#endif
    {
        #region Nested classes

        #region Enumerator class

        /// <summary>
        /// Enumerates the elements of a <see cref="Cache{TKey,TValue}"/> instance in the evaluation order.
        /// </summary>
        /// <seealso cref="Cache{TKey,TValue}"/>
        [Serializable]
        private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            #region Fields

            private readonly Cache<TKey, TValue> cache;
            private readonly int version;
            private readonly bool isGeneric;

            private CacheItem current;
            private bool beforeFirst;

            #endregion

            #region Properties

            #region Public Properties

            public KeyValuePair<TKey, TValue> Current => current != null ? new KeyValuePair<TKey, TValue>(current.Key, current.Value) : default;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    if (beforeFirst || (!beforeFirst && current == null))
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));
                    if (isGeneric)
                        return Current;
                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (beforeFirst || (!beforeFirst && current == null))
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));
                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (beforeFirst || (!beforeFirst && current == null))
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));
                    return current.Key;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (beforeFirst || (!beforeFirst && current == null))
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));
                    return current.Value;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(Cache<TKey, TValue> cache, bool isGeneric)
            {
                this.cache = cache;
                version = cache.version;
                current = null;
                this.isGeneric = isGeneric;
                beforeFirst = true;
            }

            #endregion

            #region Methods

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (version != cache.version)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                if (beforeFirst)
                {
                    beforeFirst = false;
                    if (cache.first == null)
                        return false;

                    current = cache.first;
                    return true;
                }

                if (current != null)
                {
                    current = current.Next;
                    return current != null;
                }

                return false;
            }

            public void Reset()
            {
                if (version != cache.version)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));
                beforeFirst = true;
                current = null;
            }

            #endregion
        }

        #endregion

        #region CacheStatistics class

        /// <summary>
        /// Retrieves statistics of a <see cref="Cache{TKey,TValue}"/> instance.
        /// </summary>
        [Serializable]
        private sealed class CacheStatistics : ICacheStatistics
        {
            #region Fields

            readonly Cache<TKey, TValue> owner;

            #endregion

            #region Properties

            public int Reads => owner.cacheReads;

            public int Writes => owner.cacheWrites;

            public int Deletes => owner.cacheDeletes;

            public int Hits => owner.cacheHit;

            public float HitRate => Reads == 0 ? 0 : (float)Hits / Reads;

            #endregion

            #region Constructors

            internal CacheStatistics(Cache<TKey, TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            public override string ToString() => Res.Get(Res.CacheStatistics, typeof(TKey).Name, typeof(TValue).Name, owner.Count, owner.Capacity, Writes, Reads, Hits, Deletes, HitRate);

            #endregion
        }

        #endregion

        #region CacheItem class

        [Serializable]
        [DebuggerDisplay("[{" + nameof(Key) + "}; {" + nameof(Value) + "}]")]
        private sealed class CacheItem
        {
            #region Fields

            internal TKey Key;
            internal TValue Value;
            internal CacheItem Next;
            internal CacheItem Prev;

            #endregion
        }

        #endregion

        #region KeysCollection class

        [DebuggerTypeProxy(typeof(DictionaryKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ")}")]
        [Serializable]
        private sealed class KeysCollection : ICollection<TKey>, ICollection
        {
            #region Fields

            private readonly Cache<TKey, TValue> owner;
            [NonSerialized] private object syncRoot;

            #endregion

            #region Properties

            #region Public Properties

            public int Count => owner.Count;

            public bool IsReadOnly => true;

            #endregion

            #region Explicitly Implemented Interface Properties

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

            #region Constructors

            internal KeysCollection(Cache<TKey, TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            #region Public Methods

            public bool Contains(TKey item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item), Res.Get(Res.ArgumentNull));
                return owner.ContainsKey(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex), Res.Get(Res.ArgumentOutOfRange));
                if (array.Length - arrayIndex < Count)
                    throw new ArgumentException(Res.Get(Res.DestArrayShort), nameof(array));

                for (CacheItem current = owner.first; current != null; current = current.Next)
                {
                    array[arrayIndex++] = current.Key;
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                if (owner.cacheStore == null || owner.first == null)
                    yield break;

                int version = owner.version;
                for (CacheItem current = owner.first; current != null; current = current.Next)
                {
                    if (version != owner.version)
                        throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                    yield return current.Key;
                }
            }

            #endregion

            #region Explicitly Implemented Interface Methods

            void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException(Res.Get(Res.ModifyNotSupported));

            void ICollection<TKey>.Clear() => throw new NotSupportedException(Res.Get(Res.ModifyNotSupported));

            bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException(Res.Get(Res.ModifyNotSupported));

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));

                if (array is TKey[] keys)
                {
                    CopyTo(keys, index);
                    return;
                }

                if (index < 0 || index > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
                if (array.Length - index < Count)
                    throw new ArgumentException(Res.Get(Res.DestArrayShort), nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(Res.Get(Res.ArrayDimension), nameof(array));

                if (array is object[] objectArray)
                {
                    for (CacheItem current = owner.first; current != null; current = current.Next)
                        objectArray[index++] = current.Key;
                }

                throw new ArgumentException(Res.Get(Res.ArrayTypeInvalid));
            }

            #endregion

            #endregion
        }

        #endregion

        #region ValuesCollection class

        [DebuggerTypeProxy(typeof(DictionaryValueCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}; TValue = {typeof(" + nameof(TValue) + ")}")]
        [Serializable]
        private sealed class ValuesCollection : ICollection<TValue>, ICollection
        {
            #region Fields

            private readonly Cache<TKey, TValue> owner;
            [NonSerialized] private object syncRoot;

            #endregion

            #region Properties

            #region Public Properties

            public int Count => owner.Count;

            public bool IsReadOnly => true;

            #endregion

            #region Explicitly Implemented Interface Properties

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

            #region Constructors

            internal ValuesCollection(Cache<TKey, TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            #region Public Methods

            public bool Contains(TValue item) => owner.ContainsValue(item);

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex), Res.Get(Res.ArgumentOutOfRange));
                if (array.Length - arrayIndex < Count)
                    throw new ArgumentException(Res.Get(Res.DestArrayShort), nameof(array));

                for (CacheItem current = owner.first; current != null; current = current.Next)
                    array[arrayIndex++] = current.Value;
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                if (owner.cacheStore == null || owner.first == null)
                    yield break;

                int version = owner.version;
                for (CacheItem current = owner.first; current != null; current = current.Next)
                {
                    if (version != owner.version)
                        throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

                    yield return current.Value;
                }
            }

            #endregion

            #region Explicitly Implemented Interface Methods

            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException(Res.Get(Res.ModifyNotSupported));

            void ICollection<TValue>.Clear() => throw new NotSupportedException(Res.Get(Res.ModifyNotSupported));

            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException(Res.Get(Res.ModifyNotSupported));

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                    return;
                }

                if (index < 0 || index > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
                if (array.Length - index < Count)
                    throw new ArgumentException(Res.Get(Res.DestArrayShort), nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(Res.Get(Res.ArrayDimension), nameof(array));

                if (array is object[] objectArray)
                {
                    for (CacheItem current = owner.first; current != null; current = current.Next)
                        objectArray[index++] = current.Value;
                }

                throw new ArgumentException(Res.Get(Res.ArrayTypeInvalid));
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Constants

        private const int defaultCapacity = 128;

        #endregion

        #region Fields

        #region Static Fields

        /// <summary>
        /// A loader function that can be used at constructors if you want to manage element additions to the cache manually.
        /// If you want to get an element with a non-existing key using this loader, a <see cref="KeyNotFoundException"/> will be thrown.
        /// This field is read-only.
        /// <remarks>
        /// When this field is used as loader function at one of the constructors, the <see cref="Cache{TKey,TValue}"/> can be used
        /// similarly to a <see cref="Dictionary{TKey,TValue}"/>: existence of keys should be tested by <see cref="ContainsKey"/> or <see cref="TryGetValue"/>
        /// methods, and elements should be added by <see cref="Add"/> method or by setter of the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)"/> property.
        /// The only difference to a <see cref="Dictionary{TKey,TValue}"/> is that <see cref="Capacity"/> is still maintained so
        /// when the <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> equals to <see cref="Capacity"/>), and
        /// a new element is added, then an element will be dropped from the cache depending on the current <see cref="Behavior"/>.
        /// </remarks>
        /// </summary>
        /// <seealso cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func`2,System.Int32,System.Collections.Generic.IEqualityComparer`1)"/>
        /// <seealso cref="P:KGySoft.Collections.Cache`2.Item(`0)"/>
        /// <seealso cref="Behavior"/>
        private static readonly Func<TKey, TValue> nullLoader = key => throw new KeyNotFoundException(Res.Get(Res.CacheNullLoaderInvoke));

        // ReSharper disable StaticMemberInGenericType
        private static readonly bool useEnumKeyComparer;
        private static readonly bool useEnumValueComparer;
        private static readonly Type typeKey;
        private static readonly Type typeValue;
        // ReSharper restore StaticMemberInGenericType

        #endregion

        #region Instance Fields

        private readonly bool isDefaultComparer;
        private readonly Func<TKey, TValue> itemLoader;
        private readonly IEqualityComparer<TKey> comparer;

        private int cacheReads;
        private int cacheHit;
        private int cacheDeletes;
        private int capacity;
        private int cacheWrites;
        private bool ensureCapacity;
        private CacheBehavior behavior = CacheBehavior.RemoveLeastRecentUsedElement;
        private int version;
        private Dictionary<TKey, CacheItem> cacheStore;
        private object syncRoot;
        private KeysCollection keysCollection;
        private ValuesCollection valuesCollection;

        /// <summary>
        /// First element in the evaluation order. This element will be dropped first as least used item.
        /// </summary>
        private CacheItem first;

        /// <summary>
        /// Last (newest) element in the evaluation order.
        /// </summary>
        private CacheItem last;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets the capacity of the cache. If new value is smaller than elements count (value of the <see cref="Count"/> property),
        /// then old or least used elements (depending on <see cref="Behavior"/>) will be removed from <see cref="Cache{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para>If new value is smaller than elements count, then cost of setting this property is O(n), where n is the difference of
        /// <see cref="Count"/> before setting the property and the new capacity to set.</para>
        /// <para>If new value is larger than elements count, and <see cref="EnsureCapacity"/> returns <see langword="true"/>, then cost of setting this property is O(n),
        /// where n is the new capacity.</para>
        /// <para>Otherwise, the cost of setting this property is O(1).</para>
        /// </remarks>
        /// <seealso cref="Count"/>
        /// <seealso cref="Behavior"/>
        /// <seealso cref="EnsureCapacity"/>
        public int Capacity
        {
            get => capacity;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.CacheMinSize));

                if (capacity == value)
                    return;

                capacity = value;
                if (Count - value > 0)
                    RemoveLeastUsedItems(Count - value);

                if (ensureCapacity)
                    DoEnsureCapacity();
            }
        }

        /// <summary>
        /// Gets or sets the cache behavior when cache is full and an element has to be removed.
        /// The cache is full, when <see cref="Count"/> reaches the <see cref="Capacity"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When cache is full (that is, when <see cref="Count"/> reaches <see cref="Capacity"/>) and a new element
        /// has to be stored, then an element has to be dropped out from the cache. The dropping-out strategy is
        /// specified by a <see cref="Behavior"/> property. The suggested behavior depends on cache usage. See
        /// possible behaviors at <see cref="CacheBehavior"/> enumeration.
        /// </para>
        /// <para>
        /// Default value: <see cref="CacheBehavior.RemoveLeastRecentUsedElement"/>.
        /// </para>
        /// <note>
        /// Changing value of this property will not reorganize cache, just switches between the maintaining strategies.
        /// Cache order is maintained on accessing a value.
        /// </note>
        /// </remarks>
        /// <seealso cref="Count"/>
        /// <seealso cref="Capacity"/>
        /// <seealso cref="CacheBehavior"/>
        /// <seealso cref="EnsureCapacity"/>
        public CacheBehavior Behavior
        {
            get => behavior;
            set
            {
                if (!Enum<CacheBehavior>.IsDefined(value))
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));

                behavior = value;
            }
        }

        /// <summary>
        /// Gets or sets whether adding the first item to the cache or resetting <see cref="Capacity"/> on a non-empty cache should
        /// allocate memory for all cache entries.
        /// <br/>Default value is <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="Capacity"/> is large (10,000 or bigger), and the cache is not likely to be full, the recommended value is <see langword="false"/>.</para>
        /// <para>When <see cref="EnsureCapacity"/> is <see langword="true"/>, the full capacity of the inner storage is allocated when the first
        /// item is added to the cache. Otherwise, inner storage is allocated dynamically, doubling the currently used inner
        /// storage until the preset <see cref="Capacity"/> is reached.
        /// <note>When <see cref="EnsureCapacity"/> is <see langword="false"/> and <see cref="Capacity"/> is not a power of 2, then after the last storage doubling
        /// the internally allocated storage can be bigger than <see cref="Capacity"/>. But setting <see langword="true"/> to this property trims the possibly exceeded size of the internal storage.</note>
        /// <note>Even if <see cref="EnsureCapacity"/> is <see langword="true"/> (and thus the internal storage is preallocated), adding elements to the cache
        /// consumes some memory for each added element.</note>
        /// </para>
        /// <para>When cache is not empty and <see cref="EnsureCapacity"/> is just turned on, the cost of setting this property is O(n),
        /// where n is <see cref="Count"/>. In any other cases cost of setting this property is O(1).</para>
        /// </remarks>
        /// <seealso cref="Capacity"/>
        public bool EnsureCapacity
        {
            get => ensureCapacity;
            set
            {
                if (ensureCapacity == value)
                    return;

                ensureCapacity = value;
                if (ensureCapacity)
                    DoEnsureCapacity();
            }
        }

        /// <summary>
        /// Gets the keys stored in the cache in evaluation order.
        /// </summary>
        /// <remarks>
        /// <para>The order of the keys in the <see cref="ICollection{T}"/> represents the evaluation order. When the <see cref="Cache{TKey,TValue}"/> is full, the element with the first key will be dropped.</para>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the keys in the original <see cref="Cache{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="Cache{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <para>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">Reset</see> method.</para>
        /// </remarks>
        public ICollection<TKey> Keys => keysCollection ?? (keysCollection = new KeysCollection(this));

        /// <summary>
        /// Gets the values stored in the cache in evaluation order.
        /// </summary>
        /// <remarks>
        /// <para>The order of the values in the <see cref="ICollection{T}"/> represents the evaluation order. When the <see cref="Cache{TKey,TValue}"/> is full, the element with the value key will be dropped.</para>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the values in the original <see cref="Cache{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="Cache{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <para>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">Reset</see> method.</para>
        /// </remarks>
        public ICollection<TValue> Values => valuesCollection ?? (valuesCollection = new ValuesCollection(this));

        /// <summary>
        /// Gets number of elements currently stored in this <see cref="Cache{TKey,TValue}"/> instance.
        /// </summary>
        /// <seealso cref="Capacity"/>
        public int Count => cacheStore?.Count ?? 0;

        #endregion

        #region Explicitly Implemented Interface Properties

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <returns>
        /// This is always a <see langword="false"/> value for <see cref="Cache{TKey,TValue}"/>.
        /// </returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary"/> object has a fixed size.
        /// </summary>
        /// <returns>
        /// This is always a <see langword="false"/> value for <see cref="Cache{TKey,TValue}"/>.
        /// </returns>
        bool IDictionary.IsFixedSize => false;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary"/> object is read-only.
        /// </summary>
        /// <returns>
        /// This is always a <see langword="false"/> value for <see cref="Cache{TKey,TValue}"/>.
        /// </returns>
        bool IDictionary.IsReadOnly => false;

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"/> object containing the keys of the <see cref="T:System.Collections.IDictionary"/> object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.ICollection"/> object containing the keys of the <see cref="T:System.Collections.IDictionary"/> object.
        /// </returns>
        ICollection IDictionary.Keys => (ICollection)Keys;

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"/> object containing the values in the
        /// <see cref="T:System.Collections.IDictionary"/> object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.ICollection"/> object containing the values in the <see cref="T:System.Collections.IDictionary"/> object.
        /// </returns>
        ICollection IDictionary.Values => (ICollection)Values;

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

#if !(NET35 || NET40)

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
#endif

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        /// <summary>
        /// Gets or sets the value associated with the specified <paramref name="key"/>. When an element with a non-existing key
        /// is read, and an item loader was specified by the appropriate <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>,
        /// then the value is retrieved by the specified loader delegate of this <see cref="Cache{TKey,TValue}"/> instance.
        /// </summary>
        /// <param name="key">Key of the element to get or set.</param>
        /// <returns>
        /// The element with the specified <paramref name="key"/>.
        /// </returns>
        /// <remarks>
        /// <para>Getting this property retrieves the needed element, while setting adds a new item (or overwrites an already existing item).
        /// If this <see cref="Cache{TKey,TValue}"/> instance was initialized by a non-<see langword="null"/> item loader, then it is enough to use only the get accessor because that will
        /// load elements into the cache by the delegate instance that was passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
        /// When the cache was initialized without an item loader, then getting a non-existing key will throw a <see cref="KeyNotFoundException"/>.</para>
        /// <para>If an item loader was passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>, then
        /// it is transparent whether the returned value of this property was in the cache before retrieving it.
        /// To test whether a key exists in the cache, use <see cref="ContainsKey">ContainsKey</see> method. To retrieve a key only when it already exists in the cache,
        /// use <see cref="TryGetValue">TryGetValue</see> method.</para>
        /// <para>When the <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> equals to <see cref="Capacity"/>) and
        /// a new item is added, an element (depending on <see cref="Behavior"/> property) will be dropped from the cache.</para>
        /// <para>If <see cref="EnsureCapacity"/> is <see langword="true"/>, getting or setting this property approaches an O(1) operation. Otherwise,
        /// when the capacity of the inner storage must be increased to accommodate a new element, this property becomes an O(n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved, the <see cref="Cache{TKey,TValue}"/> has been initialized without an item loader
        /// and <paramref name="key"/> does not exist in the cache.</exception>
        /// <seealso cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})"/>
        /// <seealso cref="Behavior"/>
        public TValue this[TKey key]
        {
            [CollectionAccess(CollectionAccessType.UpdatedContent)]
            get { return GetValue(key); }
            set
            {
                if (cacheStore != null && cacheStore.TryGetValue(key, out var element))
                {
                    if (behavior == CacheBehavior.RemoveLeastRecentUsedElement)
                        InternalTouch(element);

                    // replacing original value
                    element.Value = value;
                    version++;
                }
                else
                    Insert(key, value);
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <returns>
        /// The element with the specified key.
        /// </returns>
        /// <param name="key">The key of the element to get or set.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="key"/> or <paramref name="value"/> has an invalid type.</exception>
        object IDictionary.this[object key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));
                if (!typeKey.CanAcceptValue(key))
                    throw new ArgumentException(Res.Get(Res.InvalidKeyType), nameof(key));
                return this[(TKey)key];
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));
                if (!typeKey.CanAcceptValue(key))
                    throw new ArgumentException(Res.Get(Res.InvalidKeyType), nameof(key));
                if (!typeValue.CanAcceptValue(value))
                    throw new ArgumentException(Res.Get(Res.InvalidValueType), nameof(value));
                this[(TKey)key] = (TValue)value;
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        #region Static Constructor

        static Cache()
        {
            typeKey = typeof(TKey);
            typeValue = typeof(TValue);
            useEnumKeyComparer = typeKey.IsEnum;
            useEnumValueComparer = typeValue.IsEnum;
#if NET40 || NET45
            Type intType = typeof(int);
            if (useEnumKeyComparer)
                useEnumKeyComparer = Enum.GetUnderlyingType(typeKey) != intType;
            if (useEnumValueComparer)
                useEnumValueComparer = Enum.GetUnderlyingType(typeValue) != intType;
#elif !NET35
#error .NET version is not set or not supported!
#endif


        }

        #endregion

        #region Instance Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class with default capacity of 128 and no item loader.
        /// </summary>
        /// <remarks>
        /// <para>When <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> reaches <see cref="Capacity"/>) and a new element is about to be stored, then an
        /// element will be dropped out from the cache. The strategy is controlled by <see cref="Behavior"/> property.</para>
        /// <para>This constructor does not specify an item loader so you have to add elements manually to this <see cref="Cache{TKey,TValue}"/> instance. In this case
        /// the <see cref="Cache{TKey,TValue}"/> can be used similarly to a <see cref="Dictionary{TKey,TValue}"/>: before getting an element, its existence must be checked by <see cref="ContainsKey">ContainsKey</see>
        /// or <see cref="TryGetValue">TryGetValue</see> methods, though <see cref="Capacity"/> is still maintained based on the strategy specified in the <see cref="Behavior"/> property.</para>
        /// </remarks>
        /// <seealso cref="Capacity"/>
        /// <seealso cref="EnsureCapacity"/>
        /// <seealso cref="Behavior"/>
        public Cache() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class with specified <paramref name="capacity"/> capacity and <paramref name="comparer"/> and no item loader.
        /// </summary>
        /// <param name="capacity"><see cref="Capacity"/> of the <see cref="Cache{TKey,TValue}"/> (possible maximum value of <see cref="Count"/>)</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/> key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>Every key in a <see cref="Cache{TKey,TValue}"/> must be unique according to the specified comparer.</para>
        /// <para>The <paramref name="capacity"/> of a <see cref="Cache{TKey,TValue}"/> is the maximum number of elements that the <see cref="Cache{TKey,TValue}"/> can hold. When <see cref="EnsureCapacity"/>
        /// is <see langword="true"/>, the internal store is allocated when the first element is added to the cache. When <see cref="EnsureCapacity"/> is <see langword="false"/>, then as elements are added to the
        /// <see cref="Cache{TKey,TValue}"/>, the inner storage is automatically increased as required until <see cref="Capacity"/> is reached or exceeded. When <see cref="EnsureCapacity"/> is
        /// turned on while there are elements in the <see cref="Cache{TKey,TValue}"/>, then internal storage will be reallocated to have exactly the same size that <see cref="Capacity"/> defines.
        /// The possible exceeding storage will be trimmed in this case.</para>
        /// <para>When <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> reaches <see cref="Capacity"/>) and a new element is about to be stored, then an
        /// element will be dropped out from the cache. The strategy is controlled by <see cref="Behavior"/> property.</para>
        /// <para>This constructor does not specify an item loader so you have to add elements manually to this <see cref="Cache{TKey,TValue}"/> instance. In this case
        /// the <see cref="Cache{TKey,TValue}"/> can be used similarly to a <see cref="Dictionary{TKey,TValue}"/>: before getting an element, its existence must be checked by <see cref="ContainsKey">ContainsKey</see>
        /// or <see cref="TryGetValue">TryGetValue</see> methods, though <see cref="Capacity"/> is still maintained based on the strategy specified in the <see cref="Behavior"/> property.</para>
        /// </remarks>
        /// <seealso cref="Capacity"/>
        /// <seealso cref="EnsureCapacity"/>
        /// <seealso cref="Behavior"/>
        public Cache(int capacity, IEqualityComparer<TKey> comparer = null) : this(null, capacity, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class with the specified <paramref name="comparer"/>, default capacity of 128 and no item loader.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/> key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types.</param>
        /// <remarks>
        /// <para>Every key in a <see cref="Cache{TKey,TValue}"/> must be unique according to the specified comparer.</para>
        /// <para>When <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> reaches <see cref="Capacity"/>) and a new element is about to be stored, then an
        /// element will be dropped out from the cache. The strategy is controlled by <see cref="Behavior"/> property.</para>
        /// <para>This constructor does not specify an item loader so you have to add elements manually to this <see cref="Cache{TKey,TValue}"/> instance. In this case
        /// the <see cref="Cache{TKey,TValue}"/> can be used similarly to a <see cref="Dictionary{TKey,TValue}"/>: before getting an element, its existence must be checked by <see cref="ContainsKey">ContainsKey</see>
        /// or <see cref="TryGetValue">TryGetValue</see> methods, though <see cref="Capacity"/> is still maintained based on the strategy specified in the <see cref="Behavior"/> property.</para>
        /// </remarks>
        /// <seealso cref="Capacity"/>
        /// <seealso cref="EnsureCapacity"/>
        /// <seealso cref="Behavior"/>
        public Cache(IEqualityComparer<TKey> comparer) : this(null, defaultCapacity, comparer)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Cache{TKey,TValue}"/> instance with the given <paramref name="itemLoader"/>, <paramref name="capacity"/> and <paramref name="comparer"/>.
        /// </summary>
        /// <param name="itemLoader">A delegate that contains the item loader routine. This delegate is accessed whenever a non-cached item is about to be loaded by reading the
        /// <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see>.
        /// If <see langword="null"/>, then similarly to a regular <see cref="Dictionary{TKey,TValue}"/>, a <see cref="KeyNotFoundException"/> will be thrown on accessing a non-existing key.</param>
        /// <param name="capacity"><see cref="Capacity"/> of the <see cref="Cache{TKey,TValue}"/> (possible maximum value of <see cref="Count"/>). This parameter is optional.
        /// <br/>Default value: <c>128</c>.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/> key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>Every key in a <see cref="Cache{TKey,TValue}"/> must be unique according to the specified comparer.</para>
        /// <para>The <paramref name="capacity"/> of a <see cref="Cache{TKey,TValue}"/> is the maximum number of elements that the <see cref="Cache{TKey,TValue}"/> can hold. When <see cref="EnsureCapacity"/>
        /// is <see langword="true"/>, the internal store is allocated when the first element is added to the cache. When <see cref="EnsureCapacity"/> is <see langword="false"/>, then as elements are added to the
        /// <see cref="Cache{TKey,TValue}"/>, the inner storage is automatically increased as required until <see cref="Capacity"/> is reached or exceeded. When <see cref="EnsureCapacity"/> is
        /// turned on while there are elements in the <see cref="Cache{TKey,TValue}"/>, then internal storage will be reallocated to have exactly the same size that <see cref="Capacity"/> defines.
        /// The possible exceeding storage will be trimmed in this case.</para>
        /// <para>When <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> reaches <see cref="Capacity"/>) and a new element is about to be stored, then an
        /// element will be dropped out from the cache. The strategy is controlled by <see cref="Behavior"/> property.</para>
        /// <para>If you want to add elements manually to the <see cref="Cache{TKey,TValue}"/>, then you can pass <see langword="null"/> to the <paramref name="itemLoader"/> parameter. In this case
        /// the <see cref="Cache{TKey,TValue}"/> can be used similarly to a <see cref="Dictionary{TKey,TValue}"/>: before getting an element, its existence must be checked by <see cref="ContainsKey">ContainsKey</see>
        /// or <see cref="TryGetValue">TryGetValue</see> methods, though <see cref="Capacity"/> is still maintained based on the strategy specified in the <see cref="Behavior"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="itemLoader"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less or equal to 0.</exception>
        /// <overloads><see cref="Cache{TKey,TValue}"/> type has four different public constructors for initializing the item loader delegate, capacity and key comparer.</overloads>
        /// <seealso cref="Capacity"/>
        /// <seealso cref="EnsureCapacity"/>
        /// <seealso cref="Behavior"/>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public Cache(Func<TKey, TValue> itemLoader, int capacity = defaultCapacity, IEqualityComparer<TKey> comparer = null)
        {
            this.itemLoader = itemLoader ?? nullLoader;
            Capacity = capacity;
            this.comparer = comparer ?? (useEnumKeyComparer ? (IEqualityComparer<TKey>)EnumComparer<TKey>.Comparer : EqualityComparer<TKey>.Default);
            isDefaultComparer = useEnumKeyComparer ? this.comparer.Equals(EnumComparer<TKey>.Comparer) : this.comparer.Equals(EqualityComparer<TKey>.Default);
        }

        #endregion

        #region Private Constructors

        /// <summary>
        /// Special constructor for deserialization
        /// </summary>
        private Cache(SerializationInfo info, StreamingContext context)
        {
            // capacity
            capacity = info.GetInt32(nameof(capacity));
            ensureCapacity = info.GetBoolean(nameof(ensureCapacity));

            // comparer
            comparer = (IEqualityComparer<TKey>)info.GetValue(nameof(comparer), typeof(IEqualityComparer<TKey>));
            isDefaultComparer = comparer == null;
            if (comparer == null)
                comparer = useEnumKeyComparer ? (IEqualityComparer<TKey>)EnumComparer<TKey>.Comparer : EqualityComparer<TKey>.Default;

            // loader
            itemLoader = (Func<TKey, TValue>)info.GetValue(nameof(itemLoader), typeof(Func<TKey, TValue>)) ?? nullLoader;

            // elements
            TKey[] keys = (TKey[])info.GetValue(nameof(keys), typeof(TKey[]));
            TValue[] values = (TValue[])info.GetValue(nameof(values), typeof(TValue[]));
            cacheStore = new Dictionary<TKey, CacheItem>(ensureCapacity ? capacity : keys.Length, comparer);
            for (int i = 0; i < keys.Length; i++)
            {
                Insert(keys[i], values[i]);
            }

            // other data
            version = info.GetInt32(nameof(version));
            cacheReads = info.GetInt32(nameof(cacheReads));
            cacheDeletes = info.GetInt32(nameof(cacheWrites));
            cacheDeletes = info.GetInt32(nameof(cacheDeletes));
            cacheHit = info.GetInt32(nameof(cacheHit));
        }

        #endregion

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Renews the value with the specified <paramref name="key"/> in the evaluation order.
        /// </summary>
        /// <param name="key">The key of the item to renew.</param>
        /// <remarks>
        /// <para><see cref="Cache{TKey,TValue}"/> maintains an evaluation order for the stored elements. When the <see cref="Cache{TKey,TValue}"/> is full
        /// (that is when <see cref="Count"/> equals to <see cref="Capacity"/>), then adding a new element will drop the element, which is the first one in the evaluation order.
        /// By calling this method, the element with the specified <paramref name="key"/> will be sent to the back in the evaluation order.</para>
        /// <para>When <see cref="Behavior"/> is <see cref="CacheBehavior.RemoveLeastRecentUsedElement"/> (which is the default behavior), then whenever an existing element
        /// is accessed in the <see cref="Cache{TKey,TValue}"/>, then it will be touched internally.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="key"/> does not exist in the <see cref="Cache{TKey,TValue}"/>.</exception>
        /// <seealso cref="Behavior"/>
        public void Touch(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));

            if (cacheStore != null && cacheStore.TryGetValue(key, out CacheItem element))
            {
                InternalTouch(element);
                version++;
            }
            else
                throw new KeyNotFoundException(Res.Get(Res.CacheKeyNotFound, key));
        }

        /// <summary>
        /// Refreshes the value of the <paramref name="key"/> in the <see cref="Cache{TKey,TValue}"/> even if it already exists in the cache
        /// by using the item loader that was passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
        /// </summary>
        /// <param name="key">The key of the item to refresh.</param>
        /// <remarks>
        /// <para>The loaded value will be stored in the <see cref="Cache{TKey,TValue}"/>. If a value already existed in the cache for the given <paramref name="key"/>, then the value will be replaced.</para>
        /// <para><note type="caution">Do not use this method when the <see cref="Cache{TKey,TValue}"/> was initialized without an item loader.</note></para>
        /// <para>To get the refreshed value as well, use <see cref="GetValueUncached"/> method instead.</para>
        /// <para>The cost of this method depends on the cost of the item loader function that was passed to the constructor. Refreshing the already loaded value approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <see cref="Cache{TKey,TValue}"/> has been initialized without an item loader.</exception>
        public void RefreshValue(TKey key) => GetValueUncached(key);

        /// <summary>
        /// Loads the value of the <paramref name="key"/> even if it already exists in the <see cref="Cache{TKey,TValue}"/>
        /// by using the item loader that was passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
        /// </summary>
        /// <param name="key">The key of the item to reload.</param>
        /// <returns>A <typeparamref name="TValue"/> instance that was retrieved by the item loader that was used to initialize this <see cref="Cache{TKey,TValue}"/> instance.</returns>
        /// <remarks>
        /// <para>To get a value from the <see cref="Cache{TKey,TValue}"/>, and using the item loader only when <paramref name="key"/> does not exist in the cache,
        /// read the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> property.</para>
        /// <para>The loaded value will be stored in the <see cref="Cache{TKey,TValue}"/>. If a value already existed in the cache for the given <paramref name="key"/>, then the value will be replaced.</para>
        /// <para><note type="caution">Do not use this method when the <see cref="Cache{TKey,TValue}"/> was initialized without an item loader.</note></para>
        /// <para>The cost of this method depends on the cost of the item loader function that was passed to the constructor. Handling the already loaded value approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <see cref="Cache{TKey,TValue}"/> has been initialized without an item loader.</exception>
        /// <seealso cref="P:KGySoft.Collections.Cache`2.Item(`0)"/>
        public TValue GetValueUncached(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));

            TValue result = itemLoader(key);
            if (cacheStore != null && cacheStore.TryGetValue(key, out CacheItem element))
            {
                if (behavior == CacheBehavior.RemoveLeastRecentUsedElement)
                    InternalTouch(element);

                element.Value = result;
                cacheWrites++;
                version++;
                return result;
            }

            Insert(key, result);
            return result;
        }

        /// <summary>
        /// Determines whether the <see cref="Cache{TKey,TValue}"/> contains a specific value.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="Cache{TKey,TValue}"/>.
        /// The value can be <see langword="null"/> for reference types.</param>
        /// <returns><see langword="true"/> if the <see cref="Cache{TKey,TValue}"/> contains an element with the specified <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="TValue"/> is an <see langword="enum"/> type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="TValue"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        /// </remarks>
        public bool ContainsValue(TValue value)
        {
            if (cacheStore == null)
                return false;

            IEqualityComparer<TValue> valueComparer = useEnumValueComparer ? (IEqualityComparer<TValue>)EnumComparer<TValue>.Comparer : EqualityComparer<TValue>.Default;
            for (CacheItem item = first; item != null; item = item.Next)
            {
                if (valueComparer.Equals(value, item.Value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Clears the <see cref="Cache{TKey,TValue}"/> and resets statistics.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="Count"/> property is set to 0, and references to other objects from elements of the collection are also released.
        /// The <see cref="Capacity"/> remains unchanged. The statistics will be reset.</para>
        /// <para>This method is an O(1) operation.</para>
        /// </remarks>
        /// <seealso cref="Clear"/>
        public void Reset()
        {
            Clear();
            cacheReads = 0;
            cacheWrites = 0;
            cacheDeletes = 0;
            cacheHit = 0;
        }

        /// <summary>
        /// Gets an <see cref="ICacheStatistics"/> instance of the <see cref="Cache{TKey,TValue}"/> that can provide statistical information about the cache.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICacheStatistics"/> instance is a wrapper around the <see cref="Cache{TKey,TValue}"/> and reflects any changes
        /// happened to the cache immediately. Therefore it is not necessary to call this method again whenever new statistics are required.</para>
        /// <para>This method is an O(1) operation.</para>
        /// </remarks>
        public ICacheStatistics GetStatistics() => new CacheStatistics(this);

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="Cache{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be <see langword="null"/> for reference types.</param>
        /// <remarks>
        /// <para>You need to call this method only when this <see cref="Cache{TKey,TValue}"/> instance was initialized without using an item loader.
        /// Otherwise, you need only to read the get accessor of the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> property,
        /// which automatically invokes the item loader to add new items.</para>
        /// <para>If the <paramref name="key"/> of element already exists in the cache, this method throws an exception.
        /// In contrast, using the setter of the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> property replaces the old value with the new one.</para>
        /// <para>If you want to renew an element in the evaluation order, use the <see cref="Touch">Touch</see> method.</para>
        /// <para>If <see cref="EnsureCapacity"/> is <see langword="true"/> this method approaches an O(1) operation. Otherwise, when the capacity of the inner storage must be increased to accommodate the new element,
        /// this method becomes an O(n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> already exists in the cache.</exception>
        /// <seealso cref="P:KGySoft.Collections.Cache`2.Item(`0)"/>
        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));
            if (cacheStore != null && cacheStore.ContainsKey(key))
                throw new ArgumentException(Res.Get(Res.DuplicateKey), nameof(key));

            Insert(key, value);
        }

        /// <summary>
        /// Removes the value with the specified <paramref name="key"/> from the <see cref="Cache{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">Key of the item to remove.</param>
        /// <returns><see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if key was not found in the <see cref="Cache{TKey,TValue}"/>.</returns>
        /// <remarks><para>If the <see cref="Cache{TKey,TValue}"/> does not contain an element with the specified key, the <see cref="Cache{TKey,TValue}"/> remains unchanged. No exception is thrown.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));

            if (cacheStore == null)
                return false;

            if (!cacheStore.TryGetValue(key, out CacheItem element))
                return false;

            InternalRemove(element);
            return true;
        }

        /// <summary>
        /// Tries to gets the value associated with the specified <paramref name="key"/> without using the item loader passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>, if cache contains an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <remarks>
        /// <para>Use this method if the <see cref="Cache{TKey,TValue}"/> was initialized without an item loader, or when you want to determine if a
        /// <paramref name="key"/> exists in the <see cref="Cache{TKey,TValue}"/> and if so, you want to get the value as well.
        /// Reading the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> property would transparently load a non-existing element by
        /// calling the item loader delegate that was passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.</para>
        /// <para>Works exactly the same way as in case of <see cref="Dictionary{TKey,TValue}"/> class. If <paramref name="key"/> is not found, does not use the
        /// item loader passed to the constructor.</para>
        /// <para>If the <paramref name="key"/> is not found, then the <paramref name="value"/> parameter gets the appropriate default value
        /// for the type <typeparamref name="TValue"/>; for example, 0 (zero) for integer types, <see langword="false"/> for Boolean types, and <see langword="null"/> for reference types.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <seealso cref="P:KGySoft.Collections.Cache`2.Item(`0)"/>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (cacheStore == null)
            {
                value = default;
                return false;
            }

            cacheReads++;
            if (cacheStore.TryGetValue(key, out CacheItem element))
            {
                cacheHit++;
                if (behavior == CacheBehavior.RemoveLeastRecentUsedElement)
                    InternalTouch(element);

                value = element.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Determines whether the <see cref="Cache{TKey,TValue}"/> contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="Cache{TKey,TValue}"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="Cache{TKey,TValue}"/> contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks><para>This method approaches an O(1) operation.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));

            return (cacheStore != null && cacheStore.ContainsKey(key));
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="Cache{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="Count"/> property is set to 0, and references to other objects from elements of the collection are also released.
        /// The <see cref="Capacity"/> remains unchanged.</para>
        /// <para>This method is an O(1) operation.</para>
        /// </remarks>
        /// <seealso cref="Reset"/>
        public void Clear()
        {
            cacheDeletes += Count;
            first = null;
            last = null;
            cacheStore = null;
            version++;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Cache{TKey,TValue}"/> elements in the evaluation order.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        /// <remarks>
        /// <para>The returned enumerator supports the <see cref="IEnumerator.Reset">Reset</see> method.</para>
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this, true);

        #endregion

        #region Private Methods

        private void DoEnsureCapacity()
        {
            if (cacheStore == null)
                return;

            var old = cacheStore;
            cacheStore = new Dictionary<TKey, CacheItem>(capacity, comparer);
            foreach (KeyValuePair<TKey, CacheItem> pair in old)
                cacheStore.Add(pair.Key, pair.Value);
        }

        /// <summary>
        /// Gets a value from the cache. If item  does not exist in cache, loads it by the item loader that was passed to the constructor.
        /// </summary>
        /// <param name="key">The key of the item to retrieve.</param>
        private TValue GetValue(TKey key)
        {
            cacheReads++;

            if (cacheStore != null && cacheStore.TryGetValue(key, out CacheItem element))
            {
                cacheHit++;
                if (behavior == CacheBehavior.RemoveLeastRecentUsedElement)
                    InternalTouch(element);
                return element.Value;
            }

            TValue newItem = itemLoader.Invoke(key);
            Insert(key, newItem);
            return newItem;
        }

        private void InternalTouch(CacheItem element)
        {
            if (last == element)
                return;

            // extracting from middle
            if (element != first)
                element.Prev.Next = element.Next;
            element.Next.Prev = element.Prev; // element.Next is never null because because element is not last

            // adjusting first
            Debug.Assert(first != null, "first is null at InternalTouch");
            if (first == element)
                first = first.Next;

            // setting prev/next/last
            element.Prev = last;
            element.Next = null;
            last.Next = element;
            last = element;
        }

        private void InternalRemove(CacheItem element)
        {
            cacheStore.Remove(element.Key);

            // adjusting first/last
            if (last == element)
                last = element.Prev;
            if (first == element)
                first = element.Next;

            // extracting from middle
            if (element.Prev != null)
                element.Prev.Next = element.Next;
            if (element.Next != null)
                element.Next.Prev = element.Prev;

            cacheDeletes++;
            version++;
        }

        /// <summary>
        /// Removes the least used item from the cache.
        /// </summary>
        private void RemoveLeastUsedItem()
        {
            Debug.Assert(first != null, "first is null at RemoveLeastUsedItem");
            cacheStore.Remove(first.Key);
            first = first.Next;
            if (first != null)
                first.Prev = null;
            cacheDeletes++;
            version++;
        }

        private void RemoveLeastUsedItems(int amount)
        {
            Debug.Assert(Count >= amount, "Count is too few in RemoveLeastUsedItems");
            for (int i = 0; i < amount; i++)
            {
                Debug.Assert(first != null, "first is null at RemoveLeastUsedItems");

                // ReSharper disable once PossibleNullReferenceException
                cacheStore.Remove(first.Key);
                first = first.Next;
                if (first != null)
                    first.Prev = null;
            }

            cacheDeletes += amount;
            version++;
        }

        /// <summary>
        /// Inserting a new element into the cache
        /// </summary>
        private void Insert(TKey key, TValue value)
        {
            if (cacheStore == null)
            {
                cacheStore = new Dictionary<TKey, CacheItem>(ensureCapacity ? capacity : 1, comparer);
            }

            if (cacheStore.Count >= capacity)
                RemoveLeastUsedItem();

            var element = new CacheItem
            {
                Key = key,
                Value = value,
                Prev = last
            };

            cacheStore[key] = element;
            if (first == null)
                first = element;
            if (last != null)
                last.Next = element;
            last = element;

            cacheWrites++;
            version++;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        /// <summary>
        /// Renews an item in the evaluation order.
        /// </summary>
        /// <param name="key">The key of the item to renew.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> must exist in the cache.</exception>
        void ICache.Touch(object key)
        {
            if (!typeKey.CanAcceptValue(key))
                throw new ArgumentException(Res.Get(Res.InvalidKeyType), nameof(key));
            Touch((TKey)key);
        }

        /// <summary>
        /// Refreshes the value in the cache even if it was already loaded.
        /// </summary>
        /// <param name="key">The key of the item to refresh.</param>
        void ICache.RefreshValue(object key)
        {
            if (!typeKey.CanAcceptValue(key))
                throw new ArgumentException(Res.Get(Res.InvalidKeyType), nameof(key));
            RefreshValue((TKey)key);
        }

        /// <summary>
        /// Reloads the value into the cache even if it was already loaded using the item loader that was passed to the constructor.
        /// </summary>
        /// <param name="key">The key of the item to reload.</param>
        /// <returns>Loaded value</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> must not be <see langword="null"/>.</exception>
        object ICache.GetValueUncached(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));
            if (!typeKey.CanAcceptValue(key))
                throw new ArgumentException(Res.Get(Res.InvalidKeyType), nameof(key));
            return GetValueUncached((TKey)key);
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            if (cacheStore == null)
                return false;
            if (cacheStore.TryGetValue(item.Key, out CacheItem element))
            {
                return useEnumValueComparer
                    ? EnumComparer<TValue>.Comparer.Equals(item.Value, element.Value)
                    : EqualityComparer<TValue>.Default.Equals(item.Value, element.Value);
            }

            return false;
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>,
        /// starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from
        /// <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0, or larger than length
        /// of <paramref name="array"/>.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="arrayIndex"/> is equal to or greater than the length
        /// of <paramref name="array"/>.
        /// <br/>-or-
        /// <br/>The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available
        /// space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), Res.Get(Res.ArgumentOutOfRange));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(Res.Get(Res.DestArrayShort), nameof(array));

            for (CacheItem current = first; current != null; current = current.Next)
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(current.Key, current.Value);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>;
        /// otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original
        /// <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (cacheStore == null)
                return false;

            if (!cacheStore.TryGetValue(item.Key, out CacheItem element) || !EqualityComparer<TValue>.Default.Equals(item.Value, element.Value))
                return false;
            InternalRemove(element);
            return true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, true);

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.IDictionary"/> object.
        /// </summary>
        /// <param name="key">The <see cref="T:System.Object"/> to use as the key of the element to add.</param>
        /// <param name="value">The <see cref="T:System.Object"/> to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="key"/> or <paramref name="value"/> has an invalid type
        /// <br/>-or-
        /// <br/>An element with the same key already exists in the <see cref="T:System.Collections.IDictionary"/> object.</exception>
        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));
            if (!typeKey.CanAcceptValue(key))
                throw new ArgumentException(Res.Get(Res.InvalidKeyType), nameof(key));
            if (!typeValue.CanAcceptValue(value))
                throw new ArgumentException(Res.Get(Res.InvalidValueType), nameof(value));

            Add((TKey)key, (TValue)value);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IDictionary"/> object contains an element with the specified key.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.IDictionary"/> contains an element with the key; otherwise, false.
        /// </returns>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.IDictionary"/> object.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        bool IDictionary.Contains(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));
            return typeKey.CanAcceptValue(key) && ContainsKey((TKey)key);
        }

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IDictionaryEnumerator"/> object for the
        /// <see cref="T:System.Collections.IDictionary"/> object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IDictionaryEnumerator"/> object for the <see cref="T:System.Collections.IDictionary"/> object.
        /// </returns>
        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, false);

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.IDictionary"/> object.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        void IDictionary.Remove(object key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.Get(Res.ArgumentNull));
            if (typeKey.CanAcceptValue(key))
                Remove((TKey)key);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>,
        /// starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"/>.
        /// The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is less than zero,
        /// or larger that <paramref name="array"/> length.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
        /// <br/>-or-
        /// <br/>The number of elements in the source <see cref="T:System.Collections.ICollection"/> is greater
        /// than the available space from <paramref name="index"/> to the end of the destination <paramref name="array"/>.
        /// <br/>-or-
        /// <br/>Element type of <paramref name="array"/> is neither <see cref="KeyValuePair{TKey,TValue}"/>,
        /// <see cref="DictionaryEntry"/> nor <see cref="object"/>.</exception>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));
            if (index < 0 || index > array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), Res.Get(Res.ArgumentOutOfRange));
            if (array.Length - index < Count)
                throw new ArgumentException(Res.Get(Res.DestArrayShort), nameof(index));
            if (array.Rank != 1)
                throw new ArgumentException(Res.Get(Res.ArrayDimension), nameof(array));
            if (first == null)
                return;

            switch (array)
            {
                case KeyValuePair<TKey, TValue>[] keyValuePairs:
                    ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(keyValuePairs, index);
                    return;

                case DictionaryEntry[] dictionaryEntries:
                    for (CacheItem current = first; current != null; current = current.Next)
                        dictionaryEntries[index++] = new DictionaryEntry(current.Key, current.Value);
                    return;

                case object[] objectArray:
                    for (CacheItem current = first; current != null; current = current.Next)
                        objectArray[index++] = new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                    return;

                default:
                    throw new ArgumentException(Res.Get(Res.ArrayTypeInvalid));
            }
        }

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // capacity
            info.AddValue(nameof(capacity), capacity);
            info.AddValue(nameof(ensureCapacity), ensureCapacity);

            // comparer
            info.AddValue(nameof(comparer), isDefaultComparer ? null : comparer);

            // loader
            info.AddValue(nameof(itemLoader), itemLoader.Equals(nullLoader) ? null : itemLoader);

            // elements
            int count = Count;
            TKey[] keys = new TKey[count];
            TValue[] values = new TValue[count];
            if (count > 0)
            {
                int i = 0;
                for (CacheItem item = first; item != null; item = item.Next, i++)
                {
                    keys[i] = item.Key;
                    values[i] = item.Value;
                }
            }

            info.AddValue(nameof(keys), keys);
            info.AddValue(nameof(values), values);

            // other data
            info.AddValue(nameof(version), version);
            info.AddValue(nameof(cacheReads), cacheReads);
            info.AddValue(nameof(cacheWrites), cacheWrites);
            info.AddValue(nameof(cacheDeletes), cacheDeletes);
            info.AddValue(nameof(cacheHit), cacheHit);
        }

        #endregion

        #endregion
    }
}
