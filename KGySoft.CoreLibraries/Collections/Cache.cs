#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Cache.cs
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

using KGySoft.Annotations;
using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;
using KGySoft.Reflection;

#endregion

#region Suppressions

#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#pragma warning disable CS8604 // Possible null reference argument.
#endif

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a generic cache. If an item loader is specified, then cache expansion is transparent: the user needs only to read the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> to retrieve items.
    /// When a non-existing key is accessed, then the item is loaded automatically by the loader function that was passed to the
    /// <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
    /// If the cache is full (elements <see cref="Count"/> reaches the <see cref="Capacity"/>) and a new element has to be stored, then
    /// the oldest or least recent used element (depends on the value of <see cref="Behavior"/>) is removed from the cache.
    /// <br/>See the <strong>Remarks</strong> section for details and an example.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys stored in the cache.</typeparam>
    /// <typeparam name="TValue">Type of the values stored in the cache.</typeparam>
    /// <remarks>
    /// <note type="tip">To create a thread-safe <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance that fits the best for your needs use the members of the <see cref="ThreadSafeCacheFactory"/> class.</note>
    /// <para><see cref="Cache{TKey,TValue}"/> type provides a fast-access storage with limited capacity and transparent access. If you need to store
    /// items that are expensive to retrieve (for example from a database or remote service) and you don't want to run out of memory because of
    /// just storing newer and newer elements without getting rid of old ones, then this type might fit your expectations.
    /// Once a value is stored in the cache, its retrieval by using its key is very fast, close to O(1).</para>
    /// <para>A cache store must meet the following three criteria:
    /// <list type="number">
    /// <item><term>Associative access</term><description>Accessing elements works the same way as in case of the <see cref="Dictionary{TKey,TValue}"/> type.
    /// <see cref="Cache{TKey,TValue}"/> implements both the generic <see cref="IDictionary{TKey,TValue}"/> and the non-generic <see cref="IDictionary"/> interfaces so can be
    /// used similarly as <see cref="Dictionary{TKey,TValue}"/> or <see cref="Hashtable"/> types.</description></item>
    /// <item><term>Transparency</term><description>Users of the cache need only to read the cache by its <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> property.
    /// If needed, elements will be automatically loaded on the first access.</description></item>
    /// <item><term>Size management</term><description><see cref="Cache{TKey,TValue}"/> type has a <see cref="Capacity"/>, which is the allowed maximal elements count. If the cache is full, the
    /// oldest or least recent used element will be automatically removed from the cache (see <see cref="Behavior"/> property).</description></item>
    /// </list></para>
    /// <para>Since <see cref="Cache{TKey,TValue}"/> implements <see cref="IDictionary{TKey,TValue}"/> interface, <see cref="Add">Add</see>, <see cref="Remove">Remove</see>, <see cref="ContainsKey">ContainsKey</see> and
    /// <see cref="TryGetValue">TryGetValue</see> methods are available for it, and these methods work exactly the same way as in case the <see cref="Dictionary{TKey,TValue}"/> type. But using these methods
    /// usually are not necessary, unless we want to manually manage cache content or when cache is initialized without an item loader. Normally after cache is instantiated,
    /// it is needed to be accessed only by the getter accessor of its indexer.</para>
    /// <note type="caution">
    /// Serializing a cache instance by <see cref="IFormatter"/> implementations involves the serialization of the item loader delegate. To deserialize a cache the assembly of the loader must be accessible. If you need to
    /// serialize cache instances try to use static methods as data loaders and avoid using anonymous delegates or lambda expressions, otherwise it is not guaranteed that another
    /// implementations or versions of CLR will able to deserialize data and resolve the compiler-generated members.
    /// </note>
    /// <note type="warning">
    /// .NET Core does not support serializing delegates. If the <see cref="Cache{TKey,TValue}"/> instance was initialized by a loader delegate it is possible that serialization
    /// throws a <see cref="SerializationException"/> on some platforms.
    /// </note>
    /// </remarks>
    /// <threadsafety instance="false">Members of this type are not safe for multi-threaded operations, though a thread-safe accessor can be obtained for the <see cref="Cache{TKey,TValue}"/>
    /// by the <see cref="GetThreadSafeAccessor">GetThreadSafeAccessor</see> method. To get a thread-safe wrapper for all members use the
    /// <see cref="DictionaryExtensions.AsThreadSafe{TKey,TValue}">AsThreadSafe</see> extension method instead.
    /// <note>If a <see cref="Cache{TKey,TValue}"/> instance is wrapped into a <see cref="LockingDictionary{TKey, TValue}"/> instance, then the whole cache will be locked during the time when the item loader delegate is being called.
    /// If that is not desirable consider to use the <see cref="GetThreadSafeAccessor">GetThreadSafeAccessor</see> method instead with the default arguments and access the cache only via the returned accessor.</note>
    /// <note type="tip">To create a thread-safe <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance that fits the best for your needs use the members of the <see cref="ThreadSafeCacheFactory"/> class.</note>
    /// </threadsafety>
    /// <example>
    /// The following example shows the suggested usage of <see cref="Cache{TKey,TValue}"/>.
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/YGDY9c" target="_blank">online</a>.</note>
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
    /// // Cache elements count: 0
    /// // 
    /// // Item loading has been invoked for value 13
    /// // 13 is a prime: True
    /// // Item loading has been invoked for value 23
    /// // 23 is a prime: True
    /// // Item loading has been invoked for value 33
    /// // 33 is a prime: False
    /// // Item loading has been invoked for value 43
    /// // 43 is a prime: True
    /// // 
    /// // Cache elements count: 4
    /// // Cache elements:
    /// // Key: 13,        Value: True
    /// // Key: 23,        Value: True
    /// // Key: 33,        Value: False
    /// // Key: 43,        Value: True
    /// // 
    /// // 13 is a prime: True
    /// // 
    /// // Cache elements count: 4
    /// // Cache elements:
    /// // Key: 23,        Value: True
    /// // Key: 33,        Value: False
    /// // Key: 43,        Value: True
    /// // Key: 13,        Value: True
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
    /// <seealso cref="ThreadSafeCacheFactory"/>
    /// <seealso cref="ThreadSafeDictionary{TKey,TValue}"/>
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ").Name}; TValue = {typeof(" + nameof(TValue) + ").Name}; Hit = {" + nameof(Cache<_, _>.GetStatistics) + "()." + nameof(ICacheStatistics.HitRate) + " * 100}%")]
    public class Cache<TKey, TValue> : IDictionary<TKey, TValue>, ICache, ISerializable, IDeserializationCallback
#if !(NET35 || NET40)
        , IReadOnlyDictionary<TKey, TValue>
#endif
        where TKey : notnull
    {
        #region Nested Types

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

            private int index;

            private KeyValuePair<TKey, TValue> current;

            #endregion

            #region Properties

            #region Public Properties

            public KeyValuePair<TKey, TValue> Current => current;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    if (index == -1 || index == cache.usedCount)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    if (isGeneric)
                        return current;
                    return new DictionaryEntry(current.Key!, current.Value);
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index == -1 || index == cache.usedCount)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return new DictionaryEntry(current.Key!, current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index == -1 || index == cache.usedCount)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return current.Key;
                }
            }

            object? IDictionaryEnumerator.Value
            {
                get
                {
                    if (index == -1 || index == cache.usedCount)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
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
                this.isGeneric = isGeneric;
                index = -1;
            }

            #endregion

            #region Methods

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (version != cache.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                if (index == -1)
                    index = cache.first;
                else if (index == cache.usedCount)
                    return false;
                else
                    index = cache.entries![index].NextInOrder;

                if (index == -1)
                {
                    index = cache.usedCount;
                    current = default;
                    return false;
                }

                ref Entry item = ref cache.entries![index];
                current = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
                return true;
            }

            public void Reset()
            {
                if (version != cache.version)
                    Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);
                index = -1;
                current = default;
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

            private readonly Cache<TKey, TValue> owner;

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

            public override string ToString() => Res.CacheStatistics(typeof(TKey).Name, typeof(TValue).Name, owner.Count, owner.Capacity, Writes, Reads, Hits, Deletes, HitRate);

            #endregion
        }

        #endregion

        #region KeysCollection class

        [DebuggerTypeProxy(typeof(DictionaryKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ").Name}")]
        [Serializable]
        private sealed class KeysCollection : ICollection<TKey>, ICollection
        {
            #region Fields

            private readonly Cache<TKey, TValue> owner;
            [NonSerialized] private object? syncRoot;

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
                if (item == null!)
                    Throw.ArgumentNullException(Argument.item);
                return owner.ContainsKey(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
                if (array.Length - arrayIndex < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

                for (int current = owner.first; current != -1; current = owner.entries[current].NextInOrder)
                {
                    array[arrayIndex] = owner.entries![current].Key;
                    arrayIndex += 1;
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                if (owner.entries == null || owner.first == -1)
                    yield break;

                int version = owner.version;
                for (int current = owner.first; current != -1; current = owner.entries[current].NextInOrder)
                {
                    if (version != owner.version)
                        Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                    yield return owner.entries[current].Key;
                }
            }

            #endregion

            #region Explicitly Implemented Interface Methods

            void ICollection<TKey>.Add(TKey item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            void ICollection<TKey>.Clear() => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            bool ICollection<TKey>.Remove(TKey item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);

                if (array is TKey[] keys)
                {
                    CopyTo(keys, index);
                    return;
                }

                if (index < 0 || index > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.index);
                if (array.Length - index < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                if (array.Rank != 1)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

                if (array is object[] objectArray)
                {
                    for (int current = owner.first; current != -1; current = owner.entries[current].NextInOrder)
                    {
                        objectArray[index] = owner.entries![current].Key;
                        index += 1;
                    }

                    return;
                }

                Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
            }

            #endregion

            #endregion
        }

        #endregion

        #region ValuesCollection class

        [DebuggerTypeProxy(typeof(DictionaryValueCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}; TValue = {typeof(" + nameof(TValue) + ").Name}")]
        [Serializable]
        private sealed class ValuesCollection : ICollection<TValue>, ICollection
        {
            #region Fields

            private readonly Cache<TKey, TValue> owner;
            [NonSerialized] private object? syncRoot;

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
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
                if (array.Length - arrayIndex < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

                for (int current = owner.first; current != -1; current = owner.entries[current].NextInOrder)
                {
                    array[arrayIndex] = owner.entries![current].Value;
                    arrayIndex += 1;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                if (owner.entries == null || owner.first == -1)
                    yield break;

                int version = owner.version;
                for (int current = owner.first; current != -1; current = owner.entries[current].NextInOrder)
                {
                    if (version != owner.version)
                        Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                    yield return owner.entries[current].Value;
                }
            }

            #endregion

            #region Explicitly Implemented Interface Methods

            void ICollection<TValue>.Add(TValue item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            void ICollection<TValue>.Clear() => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
            bool ICollection<TValue>.Remove(TValue item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
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
                    for (int current = owner.first; current != -1; current = owner.entries[current].NextInOrder)
                    {
                        objectArray[index] = owner.entries![current].Value;
                        index += 1;
                    }

                    return;
                }

                Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
            }

            #endregion

            #endregion
        }

        #endregion

        #region ThreadSafeAccessorProtectLoader class

        private class ThreadSafeAccessorProtectLoader : IThreadSafeCacheAccessor<TKey, TValue>
        {
            #region Fields

            private readonly Cache<TKey, TValue> cache;

            #endregion

            #region Indexers

            public TValue this[TKey key]
            {
                get
                {
                    lock (cache.syncRootForThreadSafeAccessor!)
                        return cache[key];
                }
            }

            #endregion

            #region Constructors

            public ThreadSafeAccessorProtectLoader(Cache<TKey, TValue> cache) => this.cache = cache;

            #endregion
        }

        #endregion

        #region ThreadSafeAccessor class

        private class ThreadSafeAccessor : IThreadSafeCacheAccessor<TKey, TValue>
        {
            #region Fields

            private readonly Cache<TKey, TValue> cache;

            #endregion

            #region Indexers

            public TValue this[TKey key]
            {
                get
                {
                    lock (cache.syncRootForThreadSafeAccessor!)
                    {
                        if (cache.TryGetValue(key, out TValue? result))
                            return result;
                    }

                    TValue newItem = cache.itemLoader.Invoke(key);
                    lock (cache.syncRootForThreadSafeAccessor)
                    {
                        if (cache.TryGetValue(key, out TValue? result))
                        {
                            if (cache.DisposeDroppedValues && newItem is IDisposable disposable)
                                disposable.Dispose();
                            return result;
                        }

                        cache.Insert(key, newItem, false);
                    }

                    return newItem;
                }
            }

            #endregion

            #region Constructors

            public ThreadSafeAccessor(Cache<TKey, TValue> cache) => this.cache = cache;

            #endregion
        }

        #endregion

        #endregion

        #region Nested structs

        #region Entry struct

        [DebuggerDisplay("[{" + nameof(Key) + "}; {" + nameof(Value) + "}]")]
        private struct Entry
        {
            #region Fields

            internal uint Hash;

            [AllowNull]internal TKey Key;
            [AllowNull]internal TValue Value;

            /// <summary>
            /// Zero-based index of a chained item in the current bucket or -1 if last
            /// </summary>
            internal int NextInBucket;

            /// <summary>
            /// Zero-based index of next item in the evaluation order or -1 if last
            /// </summary>
            internal int NextInOrder;

            /// <summary>
            /// Zero-based index of previous item in the evaluation order or -1 if first
            /// </summary>
            internal int PrevInOrder;

            #endregion
        }

        #endregion

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
        private static readonly Func<TKey, TValue> nullLoader = _ => Throw.KeyNotFoundException<TValue>(Res.CacheNullLoaderInvoke);

        private static readonly Type typeKey = typeof(TKey);
        private static readonly Type typeValue = typeof(TValue);

        #endregion

        #region Instance Fields

        private Func<TKey, TValue> itemLoader;
        private IEqualityComparer<TKey>? comparer;

        private SerializationInfo? deserializationInfo;

        private int capacity;
        private CacheBehavior behavior = CacheBehavior.RemoveLeastRecentUsedElement;
        private bool disposeDroppedValues;

        private Entry[]? entries;
        private int[]? buckets; // 1-based indices for items. 0 means unused bucket.
        private int usedCount; // used elements in items including deleted ones
        private int deletedCount;
        private int deletedItemsBucket = -1; // First deleted entry among used elements. -1 if there are no deleted elements.
        private int first = -1; // First element both in traversal and in the evaluation order. -1 if empty.
        private int last = -1; // Last (newest) element. -1 if empty.
        private int version;

        private int cacheReads;
        private int cacheHit;
        private int cacheDeletes;
        private int cacheWrites;

        private object? syncRoot;
        private object? syncRootForThreadSafeAccessor;
        private KeysCollection? keysCollection;
        private ValuesCollection? valuesCollection;
        private bool ensureCapacity;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets the capacity of the cache. If new value is smaller than elements count (value of the <see cref="Count"/> property),
        /// then old or least used elements (depending on <see cref="Behavior"/>) will be removed from <see cref="Cache{TKey,TValue}"/>.
        /// <br/>Default value: <c>128</c>, if the <see cref="Cache{TKey,TValue}"/> was initialized without specifying a capacity; otherwise, as it was initialized.
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
                    Throw.ArgumentOutOfRangeException(Argument.value, Res.CacheMinSize);

                if (capacity == value)
                    return;

                capacity = value;
                if (Count - value > 0)
                    DropItems(Count - value);

                if (ensureCapacity)
                    DoEnsureCapacity();
            }
        }

        /// <summary>
        /// Gets or sets the cache behavior when cache is full and an element has to be removed.
        /// The cache is full, when <see cref="Count"/> reaches the <see cref="Capacity"/>.
        /// Default value: <see cref="CacheBehavior.RemoveLeastRecentUsedElement"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When cache is full (that is, when <see cref="Count"/> reaches <see cref="Capacity"/>) and a new element
        /// has to be stored, then an element has to be dropped out from the cache. The dropping-out strategy is
        /// specified by this property. The suggested behavior depends on cache usage. See possible behaviors at <see cref="CacheBehavior"/> enumeration.
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
                    Throw.EnumArgumentOutOfRangeWithValues(Argument.value, value);

                behavior = value;
            }
        }

        /// <summary>
        /// Gets or sets whether adding the first item to the cache or resetting <see cref="Capacity"/> on a non-empty cache should
        /// allocate memory for all cache entries.
        /// <br/>Default value: <see langword="false"/>, unless you use the <see cref="IDictionary{TKey,TValue}"/> initializer
        /// <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Collections.Generic.IDictionary{`0,`1},System.Collections.Generic.IEqualityComparer{`0})">constructor</see>,
        /// which initializes this property to <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="Capacity"/> is large (10,000 or bigger), and the cache is not likely to be full, the recommended value is <see langword="false"/>.</para>
        /// <para>When <see cref="EnsureCapacity"/> is <see langword="true"/>, the full capacity of the inner storage is allocated when the first
        /// item is added to the cache. Otherwise, inner storage is allocated dynamically, increasing its size again and again until the preset <see cref="Capacity"/> is reached.
        /// When increasing occurs the storage size is increased to a prime number close to the double of the previous storage size.
        /// <note>When <see cref="EnsureCapacity"/> is <see langword="false"/>, then after the last storage doubling the internally allocated storage can be much bigger than <see cref="Capacity"/>.
        /// But setting <see langword="true"/>&#160;to this property trims the possibly exceeded size to a prime value close to the actual capacity.</note>
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
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TKey> Keys => keysCollection ??= new KeysCollection(this);

        /// <summary>
        /// Gets the values stored in the cache in evaluation order.
        /// </summary>
        /// <remarks>
        /// <para>The order of the values in the <see cref="ICollection{T}"/> represents the evaluation order. When the <see cref="Cache{TKey,TValue}"/> is full, the element with the value key will be dropped.</para>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the values in the original <see cref="Cache{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="Cache{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TValue> Values => valuesCollection ??= new ValuesCollection(this);

        /// <summary>
        /// Gets number of elements currently stored in this <see cref="Cache{TKey,TValue}"/> instance.
        /// </summary>
        /// <seealso cref="Capacity"/>
        public int Count => usedCount - deletedCount;

        /// <summary>
        /// Gets or sets whether internally dropped values are disposed if they implement <see cref="IDisposable"/>.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>If the value of this property is <see langword="true"/>, then a disposable value will be disposed, if
        /// <list type="bullet">
        /// <item>The <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> equals <see cref="Capacity"/>), and
        /// a new item has to be stored so an element has to be dropped.</item>
        /// <item><see cref="Capacity"/> is decreased and therefore elements has to be dropped.</item>
        /// <item>The <see cref="Cache{TKey,TValue}"/> is accessed via an <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance and item for the same <typeparamref name="TKey"/>
        /// has been loaded concurrently so all but one loaded elements have to be discarded.</item>
        /// </list>
        /// </para>
        /// <note>In all cases when values are removed or replaced explicitly by the public members values are not disposed.</note>
        /// </remarks>
        public bool DisposeDroppedValues
        {
            get => disposeDroppedValues;
            set => disposeDroppedValues = value;
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;

        ICollection IDictionary.Keys => (ICollection)Keys;
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
        /// <returns>The element with the specified <paramref name="key"/>.</returns>
        /// <remarks>
        /// <para>Getting this property retrieves the needed element, while setting adds a new item (or overwrites an already existing item).
        /// If this <see cref="Cache{TKey,TValue}"/> instance was initialized by a non-<see langword="null"/>&#160;item loader, then it is enough to use only the get accessor because that will
        /// load elements into the cache by the delegate instance that was passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
        /// When the cache was initialized without an item loader, then getting a non-existing key will throw a <see cref="KeyNotFoundException"/>.</para>
        /// <para>If an item loader was passed to the <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>, then
        /// it is transparent whether the returned value of this property was in the cache before retrieving it.
        /// To test whether a key exists in the cache, use the <see cref="ContainsKey">ContainsKey</see> method. To retrieve a key only when it already exists in the cache,
        /// use the <see cref="TryGetValue">TryGetValue</see> method.</para>
        /// <para>When the <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> equals to <see cref="Capacity"/>) and
        /// a new item is added, an element (depending on <see cref="Behavior"/> property) will be dropped from the cache.</para>
        /// <para>If <see cref="EnsureCapacity"/> is <see langword="true"/>, getting or setting this property approaches an O(1) operation. Otherwise,
        /// when the capacity of the inner storage must be increased to accommodate a new element, this property becomes an O(n) operation, where n is <see cref="Count"/>.</para>
        /// <para><note type="tip">You can retrieve a thread-safe accessor by the <see cref="GetThreadSafeAccessor">GetThreadSafeAccessor</see> method.</note></para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved, the <see cref="Cache{TKey,TValue}"/> has been initialized without an item loader
        /// and <paramref name="key"/> does not exist in the cache.</exception>
        /// <seealso cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})"/>
        /// <seealso cref="Behavior"/>
        /// <seealso cref="GetThreadSafeAccessor"/>
        public TValue this[TKey key]
        {
            [CollectionAccess(CollectionAccessType.UpdatedContent)]
            get
            {
                int i = GetItemIndex(key);
                cacheReads += 1;
                if (i >= 0)
                {
                    cacheHit += 1;
                    if (behavior == CacheBehavior.RemoveLeastRecentUsedElement)
                        InternalTouch(i);
                    return entries![i].Value;
                }

                TValue newItem = itemLoader.Invoke(key);
                Insert(key!, newItem, false);
                return newItem;
            }
            set
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                Insert(key, value, false);
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object? IDictionary.this[object key]
        {
            get
            {
                // For valid keys this means a double cast but we don't want to return null from an InvalidCastException
                if (!CanAcceptKey(key))
                    return null;

                return TryGetValue((TKey)key, out TValue? value) ? (object)value! : null;
            }
            set
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                Throw.ThrowIfNullIsInvalid<TValue>(value);

                try
                {
                    TKey typedKey = (TKey)key;
                    try
                    {
                        this[typedKey] = (TValue)value!;
                    }
                    catch (InvalidCastException)
                    {
                        Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value!, typeValue));
                    }
                }
                catch (InvalidCastException)
                {
                    Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, typeKey));
                }
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

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
        public Cache() : this(null, defaultCapacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class with specified <paramref name="capacity"/> capacity and <paramref name="comparer"/> and no item loader.
        /// </summary>
        /// <param name="capacity"><see cref="Capacity"/> of the <see cref="Cache{TKey,TValue}"/> (possible maximum value of <see cref="Count"/>)</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
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
        public Cache(int capacity, IEqualityComparer<TKey>? comparer = null) : this(null, capacity, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class with the specified <paramref name="comparer"/>, default capacity of 128 and no item loader.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types.</param>
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
        public Cache(IEqualityComparer<TKey>? comparer) : this(null, defaultCapacity, comparer)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Cache{TKey,TValue}"/> instance with the given <paramref name="itemLoader"/> and <paramref name="comparer"/> using default capacity of 128.
        /// </summary>
        /// <param name="itemLoader">A delegate that contains the item loader routine. This delegate is accessed whenever a non-cached item is about to be loaded by reading the
        /// <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see>.
        /// If <see langword="null"/>, then similarly to a regular <see cref="Dictionary{TKey,TValue}"/>, a <see cref="KeyNotFoundException"/> will be thrown on accessing a non-existing key.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>Every key in a <see cref="Cache{TKey,TValue}"/> must be unique according to the specified comparer.</para>
        /// <para>When <see cref="Cache{TKey,TValue}"/> is full (that is, when <see cref="Count"/> reaches <see cref="Capacity"/>) and a new element is about to be stored, then an
        /// element will be dropped out from the cache. The strategy is controlled by <see cref="Behavior"/> property.</para>
        /// <para>If you want to add elements manually to the <see cref="Cache{TKey,TValue}"/>, then you can pass <see langword="null"/>&#160;to the <paramref name="itemLoader"/> parameter. In this case
        /// the <see cref="Cache{TKey,TValue}"/> can be used similarly to a <see cref="Dictionary{TKey,TValue}"/>: before getting an element, its existence must be checked by <see cref="ContainsKey">ContainsKey</see>
        /// or <see cref="TryGetValue">TryGetValue</see> methods, though <see cref="Capacity"/> is still maintained based on the strategy specified in the <see cref="Behavior"/> property.</para>
        /// </remarks>
        /// <overloads><see cref="Cache{TKey,TValue}"/> type has four different public constructors for initializing the item loader delegate, capacity and key comparer.</overloads>
        /// <seealso cref="Capacity"/>
        /// <seealso cref="EnsureCapacity"/>
        /// <seealso cref="Behavior"/>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public Cache(Func<TKey, TValue>? itemLoader, IEqualityComparer<TKey>? comparer) : this(itemLoader, defaultCapacity, comparer)
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
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
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
        /// <para>If you want to add elements manually to the <see cref="Cache{TKey,TValue}"/>, then you can pass <see langword="null"/>&#160;to the <paramref name="itemLoader"/> parameter. In this case
        /// the <see cref="Cache{TKey,TValue}"/> can be used similarly to a <see cref="Dictionary{TKey,TValue}"/>: before getting an element, its existence must be checked by <see cref="ContainsKey">ContainsKey</see>
        /// or <see cref="TryGetValue">TryGetValue</see> methods, though <see cref="Capacity"/> is still maintained based on the strategy specified in the <see cref="Behavior"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less or equal to 0.</exception>
        /// <overloads><see cref="Cache{TKey,TValue}"/> type has four different public constructors for initializing the item loader delegate, capacity and key comparer.</overloads>
        /// <seealso cref="Capacity"/>
        /// <seealso cref="EnsureCapacity"/>
        /// <seealso cref="Behavior"/>
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public Cache(Func<TKey, TValue>? itemLoader, int capacity = defaultCapacity, IEqualityComparer<TKey>? comparer = null)
        {
            this.itemLoader = itemLoader ?? nullLoader;
            Capacity = capacity;
            this.comparer = ComparerHelper<TKey>.GetSpecialDefaultEqualityComparerOrNull(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class from the specified <paramref name="dictionary"/> and <paramref name="comparer"/>, with no item loader.
        /// The <see cref="Capacity"/> will be initialized to the number of elements in <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary whose elements are added to the <see cref="Cache{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>Every key in a <see cref="Cache{TKey,TValue}"/> must be unique according to the specified comparer.</para>
        /// <para>This constructor does not specify an item loader and initializes <see cref="Capacity"/> to the number of elements in
        /// the specified <paramref name="dictionary"/>. Meaning, adding new elements manually will drop an element unless you increase <see cref="Capacity"/>,
        /// and reading the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> with a non-existing key will throw
        /// a <see cref="KeyNotFoundException"/>.</para>
        /// <para>This constructor sets <see cref="EnsureCapacity"/> to <see langword="true"/>, so no multiple allocations occur during the initialization.</para>
        /// </remarks>
        /// <seealso cref="Capacity"/>
        /// <seealso cref="EnsureCapacity"/>
        /// <seealso cref="Behavior"/>
        public Cache(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer = null)
        {
            if (dictionary == null!)
                Throw.ArgumentNullException(Argument.dictionary);
            itemLoader = nullLoader;
            this.comparer = ComparerHelper<TKey>.GetSpecialDefaultEqualityComparerOrNull(comparer);
            int count = dictionary.Count;
            if (count == 0)
                return;
            Capacity = count;
            EnsureCapacity = true;
            foreach (KeyValuePair<TKey, TValue> item in dictionary)
                Add(item.Key, item.Value);
        }

        #endregion

        #region Protected Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class from serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that stores the data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this deserialization.</param>
        /// <remarks><note type="inherit">If an inherited type serializes data, which may affect the hashes of the keys, then override
        /// the <see cref="OnDeserialization">OnDeserialization</see> method and use that to restore the data of the derived instance.</note></remarks>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. - the fields will be initialized in IDeserializationCallback.OnDeserialization
        protected Cache(SerializationInfo info, StreamingContext context)
#pragma warning restore CS8618
        {
            // deferring the actual deserialization until all objects are finalized and hashes do not change anymore
            deserializationInfo = info;
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        private static bool CanAcceptKey(object key)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return key is TKey;
        }

        #endregion

        #region Instance Methods

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
            int i = GetItemIndex(key);
            if (i < 0)
                Throw.KeyNotFoundException(Res.CacheKeyNotFound);

            InternalTouch(i);
            version += 1;
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
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            TValue result = itemLoader.Invoke(key);
            Insert(key, result, false);
            return result;
        }

        /// <summary>
        /// Determines whether the <see cref="Cache{TKey,TValue}"/> contains a specific value.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="Cache{TKey,TValue}"/>.
        /// The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns><see langword="true"/>&#160;if the <see cref="Cache{TKey,TValue}"/> contains an element with the specified <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="TValue"/> is an <see langword="enum"/>&#160;type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="TValue"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        /// </remarks>
        public bool ContainsValue(TValue value)
        {
            if (entries == null)
                return false;

            if (value == null)
            {
                for (int i = first; i != -1; i = entries[i].NextInOrder)
                {
                    if (entries[i].Value == null)
                        return true;
                }

                return false;
            }

            IEqualityComparer<TValue> valueComparer = ComparerHelper<TValue>.EqualityComparer;
            for (int i = first; i != -1; i = entries[i].NextInOrder)
            {
                if (valueComparer.Equals(value, entries[i].Value))
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
        /// Gets an <see cref="ICacheStatistics"/> instance that provides statistical information about this <see cref="Cache{TKey,TValue}"/>.
        /// </summary>
        /// <returns>An <see cref="ICacheStatistics"/> instance that provides statistical information about the <see cref="Cache{TKey,TValue}"/>.</returns>
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
        /// <param name="value">The value of the element to add. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <remarks>
        /// <para>You need to call this method only when this <see cref="Cache{TKey,TValue}"/> instance was initialized without using an item loader.
        /// Otherwise, you need only to read the get accessor of the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> property,
        /// which automatically invokes the item loader to add new items.</para>
        /// <para>If the <paramref name="key"/> of element already exists in the cache, this method throws an exception.
        /// In contrast, using the setter of the <see cref="P:KGySoft.Collections.Cache`2.Item(`0)">indexer</see> property replaces the old value with the new one.</para>
        /// <para>If you want to renew an element in the evaluation order, use the <see cref="Touch">Touch</see> method.</para>
        /// <para>If <see cref="EnsureCapacity"/> is <see langword="true"/>&#160;this method approaches an O(1) operation. Otherwise, when the capacity of the inner storage must be increased to accommodate the new element,
        /// this method becomes an O(n) operation, where n is <see cref="Count"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> already exists in the cache.</exception>
        /// <seealso cref="P:KGySoft.Collections.Cache`2.Item(`0)"/>
        public void Add(TKey key, TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            Insert(key, value, true);
        }

        /// <summary>
        /// Removes the value with the specified <paramref name="key"/> from the <see cref="Cache{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">Key of the item to remove.</param>
        /// <returns><see langword="true"/>&#160;if the element is successfully removed; otherwise, <see langword="false"/>. This method also returns <see langword="false"/>&#160;if key was not found in the <see cref="Cache{TKey,TValue}"/>.</returns>
        /// <remarks><para>If the <see cref="Cache{TKey,TValue}"/> does not contain an element with the specified key, the <see cref="Cache{TKey,TValue}"/> remains unchanged. No exception is thrown.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool Remove(TKey key)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            return InternalRemove(key, default, false);
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
        /// for the type <typeparamref name="TValue"/>; for example, 0 (zero) for integer types, <see langword="false"/>&#160;for Boolean types, and <see langword="null"/>&#160;for reference types.</para>
        /// <para>This method approaches an O(1) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <seealso cref="P:KGySoft.Collections.Cache`2.Item(`0)"/>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            int i = GetItemIndex(key);
            cacheReads += 1;
            if (i >= 0)
            {
                cacheHit += 1;
                if (behavior == CacheBehavior.RemoveLeastRecentUsedElement)
                    InternalTouch(i);

                value = entries![i].Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Determines whether the <see cref="Cache{TKey,TValue}"/> contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="Cache{TKey,TValue}"/>.</param>
        /// <returns><see langword="true"/>&#160;if the <see cref="Cache{TKey,TValue}"/> contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks><para>This method approaches an O(1) operation.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(TKey key) => GetItemIndex(key) >= 0;

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
            if (Count == 0)
                return;

            cacheDeletes += Count;
            buckets = null;
            entries = null;
            first = -1;
            last = -1;
            usedCount = 0;
            deletedCount = 0;
            deletedItemsBucket = -1;
            version += 1;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Cache{TKey,TValue}"/> elements in the evaluation order.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the <see cref="Cache{TKey,TValue}"/>.
        /// </returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this, true);

        /// <summary>
        /// Gets a thread safe accessor for this <see cref="Cache{TKey,TValue}"/> instance. As it provides only a
        /// single readable indexer, it makes sense only if an item loader was passed to the appropriate
        /// <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>
        /// and the cache will not be accessed by other members but via the returned accessor.
        /// </summary>
        /// <param name="protectItemLoader"><see langword="true"/>&#160;to ensure that also the item loader is locked if a new element has to be loaded and
        /// <see langword="false"/>&#160;to allow the item loader to be called concurrently. In latter case the <see cref="Cache{TKey,TValue}"/> is not locked during the time the item loader is being called
        /// but it can happen that values for same key are loaded multiple times and all but one will be discarded. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance providing a thread-safe readable indexer for this <see cref="Cache{TKey,TValue}"/> instance.</returns>
        /// <remarks>
        /// <note type="tip">
        /// <list type="bullet">
        /// <item>To create a thread-safe <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance that fits the best for your needs use the members of the <see cref="ThreadSafeCacheFactory"/> class.</item>
        /// <item>To access all <see cref="IDictionary{TKey, TValue}"/> members of the <see cref="Cache{TKey,TValue}"/> in a thread-safe manner use it via a <see cref="LockingDictionary{TKey,TValue}"/> instance.</item>
        /// </list></note>
        /// </remarks>
        public IThreadSafeCacheAccessor<TKey, TValue> GetThreadSafeAccessor(bool protectItemLoader = false)
        {
            if (syncRootForThreadSafeAccessor == null)
                Interlocked.CompareExchange(ref syncRootForThreadSafeAccessor, new object(), null);
            return protectItemLoader ? (IThreadSafeCacheAccessor<TKey, TValue>)new ThreadSafeAccessorProtectLoader(this) : new ThreadSafeAccessor(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// In a derived class populates a <see cref="SerializationInfo" /> with the additional data of the derived type needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization.</param>
        [SecurityCritical]
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }

        /// <summary>
        /// In a derived class restores the state the deserialized instance.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that stores the data.</param>
        [SecurityCritical]
        protected virtual void OnDeserialization(SerializationInfo info) { }

        #endregion

        #region Private Methods

        private void Initialize(int suggestedCapacity)
        {
            int bucketSize = HashHelper.GetPrime(suggestedCapacity);
            buckets = new int[bucketSize];

            // items.Length <= bucketSize!
            entries = new Entry[capacity < bucketSize ? capacity : bucketSize];
            first = -1;
            last = -1;
            deletedItemsBucket = -1;
        }

        private void DoEnsureCapacity()
        {
            if (entries == null || entries.Length == capacity)
                return;

            Resize(capacity);
        }


        [MethodImpl(MethodImpl.AggressiveInlining)]
        private int GetItemIndex(TKey key)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            int[]? bucketsLocal = buckets;
            if (bucketsLocal == null)
                return -1;

            uint hashCode;
            Entry[] items = entries!;
            IEqualityComparer<TKey>? comp = comparer;
            if (comp == null)
            {
                hashCode = (uint)key.GetHashCode();
                for (int i = bucketsLocal[hashCode % (uint)bucketsLocal.Length] - 1; i >= 0; i = items[i].NextInBucket)
                {
                    if (items[i].Hash == hashCode && ComparerHelper<TKey>.EqualityComparer.Equals(items[i].Key, key))
                        return i;
                }
            }
            else
            {
                hashCode = (uint)comp.GetHashCode(key);
                for (int i = bucketsLocal[hashCode % (uint)bucketsLocal.Length] - 1; i >= 0; i = items[i].NextInBucket)
                {
                    if (items[i].Hash == hashCode && comp.Equals(items[i].Key, key))
                        return i;
                }
            }

            return -1;
        }

        private void InternalTouch(int index)
        {
            // already last: nothing to do
            if (index == last)
                return;

            Entry[] items = entries!;
            ref Entry itemRef = ref items[index];

            // extracting from middle
            if (index != first)
                items[itemRef.PrevInOrder].NextInOrder = itemRef.NextInOrder;
            items[itemRef.NextInOrder].PrevInOrder = itemRef.PrevInOrder;

            // it was the first one
            Debug.Assert(first != -1, "first is -1 in InternalTouch");
            if (index == first)
                first = itemRef.NextInOrder;

            // setting prev/next/last
            itemRef.PrevInOrder = last;
            itemRef.NextInOrder = -1;
            items[last].NextInOrder = index;
            last = index;
        }

        private bool InternalRemove(TKey key, TValue? value, bool checkValue)
        {
            int[]? bucketsLocal = buckets;
            if (bucketsLocal == null)
                return false;

            IEqualityComparer<TKey>? comp = comparer;
            Entry[] items = entries!;

            uint hashCode;
            if (comp == null)
            {
                hashCode = (uint)key.GetHashCode();
                comp = ComparerHelper<TKey>.EqualityComparer;
            }
            else
                hashCode = (uint)comp.GetHashCode(key);
            uint bucket = hashCode % (uint)bucketsLocal.Length;
            int prevInBucket = -1;
            for (int i = bucketsLocal[bucket] - 1; i >= 0; prevInBucket = i, i = items[i].NextInBucket)
            {
                ref Entry itemRef = ref items[i];
                if (itemRef.Hash != hashCode || !comp.Equals(itemRef.Key, key))
                    continue;

                if (checkValue && !ComparerHelper<TValue>.EqualityComparer.Equals(itemRef.Value, value))
                    return false;

                // removing entry from the original bucket
                if (prevInBucket < 0)
                    bucketsLocal[bucket] = itemRef.NextInBucket + 1;
                else
                    items[prevInBucket].NextInBucket = itemRef.NextInBucket;

                // moving entry to a special bucket of removed entries
                itemRef.NextInBucket = deletedItemsBucket;
                deletedItemsBucket = i;
                deletedCount += 1;

                // adjusting first/last
                if (i == last)
                    last = itemRef.PrevInOrder;
                if (i == first)
                    first = itemRef.NextInOrder;

                // extracting from middle
                if (itemRef.PrevInOrder != -1)
                    items[itemRef.PrevInOrder].NextInOrder = itemRef.NextInOrder;
                if (itemRef.NextInOrder != -1)
                    items[itemRef.NextInOrder].PrevInOrder = itemRef.PrevInOrder;

                // cleanup
                if (Reflector<TKey>.IsManaged)
                    itemRef.Key = default;
                if (Reflector<TValue>.IsManaged)
                    itemRef.Value = default;

                cacheDeletes += 1;
                version += 1;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the first (oldest or the least used) item from the cache.
        /// </summary>
        private void DropFirst()
        {
            Debug.Assert(first != -1, "first is -1 in DropFirst");
            if (disposeDroppedValues && entries![first].Value is IDisposable disposable)
                disposable.Dispose();
            InternalRemove(entries![first].Key!, default, false);
        }

        private void DropItems(int amount)
        {
            Debug.Assert(Count >= amount, "Count is too few in DropItems");
            for (int i = 0; i < amount; i++)
                DropFirst();
        }

        /// <summary>
        /// Inserting a new element into the cache
        /// </summary>
        private void Insert(TKey key, TValue value, bool throwIfExists)
        {
            if (buckets == null)
                Initialize(ensureCapacity ? capacity : 1);

            int[] bucketsLocal = buckets!;
            Entry[] items = entries!;
            IEqualityComparer<TKey>? comp = comparer;
            uint hashCode;
            if (comp == null)
            {
                hashCode = (uint)key.GetHashCode();
                comp = ComparerHelper<TKey>.EqualityComparer;
            }
            else
                hashCode = (uint)comp.GetHashCode(key);

            ref int bucketRef = ref bucketsLocal[hashCode % (uint)bucketsLocal.Length];

            // searching for an existing key
            for (int i = bucketRef - 1; i >= 0; i = items[i].NextInBucket)
            {
                if (items[i].Hash != hashCode || !comp.Equals(items[i].Key, key))
                    continue;

                if (throwIfExists)
                    Throw.ArgumentException(Argument.key, Res.IDictionaryDuplicateKey);

                // overwriting existing element
                if (behavior == CacheBehavior.RemoveLeastRecentUsedElement)
                    InternalTouch(i);
                items[i].Value = value;
                cacheWrites += 1;
                version += 1;
                return;
            }

            // if used with full capacity, dropping an element
            if (Count == capacity)
            {
                DropFirst();
                items = entries!;
            }

            // re-using the removed entries if possible
            int index;
            if (deletedCount > 0)
            {
                index = deletedItemsBucket;
                deletedItemsBucket = items[index].NextInBucket;
                deletedCount -= 1;
            }
            // otherwise, adding a new entry
            else
            {
                // storage expansion is needed
                if (usedCount == items.Length)
                {
                    int oldSize = items.Length;
                    int newSize = capacity >> 1 > oldSize ? oldSize << 1 : capacity;
                    Resize(newSize);
                    bucketsLocal = buckets!;
                    items = entries!;
                    bucketRef = ref bucketsLocal[hashCode % (uint)bucketsLocal.Length];
                }

                index = usedCount;
                usedCount += 1;
            }

            ref Entry itemRef = ref items[index];
            itemRef.Hash = hashCode;
            itemRef.NextInBucket = bucketRef - 1; // Next is zero-based
            itemRef.Key = key;
            itemRef.Value = value;
            itemRef.PrevInOrder = last;
            itemRef.NextInOrder = -1;
            bucketRef = index + 1; // bucket indices are 1-based
            if (first == -1)
                first = index;
            if (last != -1)
                items[last].NextInOrder = index;
            last = index;

            cacheWrites += 1;
            version += 1;
        }

        private void Resize(int suggestedSize)
        {
            int newBucketSize = HashHelper.GetPrime(suggestedSize);
            var newBuckets = new int[newBucketSize];
            var newItems = new Entry[capacity < newBucketSize ? capacity : newBucketSize];
            Entry[] items = entries!;

            // possible shortcut when increasing capacity
            if (newItems.Length >= items.Length && deletedCount == 0)
                Array.Copy(items, 0, newItems, 0, usedCount);
            else
            {
                // keeping only the living elements while updating indices
                int newCount = 0;
                for (int i = first; i != -1; i = items[i].NextInOrder)
                {
                    ref Entry newItem = ref items[newCount];
                    newItem = items[i];
                    newItem.PrevInOrder = newCount - 1;
                    newItem.NextInOrder = ++newCount;
                }

                usedCount = newCount;
                first = 0;
                last = newCount - 1;
                newItems[last].NextInOrder = -1;
                deletedCount = 0;
                deletedItemsBucket = -1;
            }

            // re-applying buckets for the new size
            for (int i = 0; i < usedCount; i++)
            {
                uint bucket = newItems[i].Hash % (uint)newBucketSize;
                newItems[i].NextInBucket = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }

            buckets = newBuckets;
            entries = newItems;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void ICache.Touch(object key)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            try
            {
                Touch((TKey)key);
            }
            catch (InvalidCastException)
            {
                Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, typeKey));
            }
        }

        void ICache.RefreshValue(object key)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            try
            {
                RefreshValue((TKey)key);
            }
            catch (InvalidCastException)
            {
                Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, typeKey));
            }
        }

        object? ICache.GetValueUncached(object key)
        {
            // For valid keys this means a double cast but we don't want to return null from an InvalidCastException
            if (!CanAcceptKey(key))
                return null;

            return GetValueUncached((TKey)key);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            int i = GetItemIndex(item.Key);
            return i >= 0 && ComparerHelper<TValue>.EqualityComparer.Equals(item.Value, entries![i].Value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
            if (array.Length - arrayIndex < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

            for (int i = first; i != -1; i = entries[i].NextInOrder)
            {
                array[arrayIndex] = new KeyValuePair<TKey, TValue>(entries![i].Key, entries[i].Value);
                arrayIndex += 1;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null!)
                Throw.ArgumentNullException(Argument.key);
            return InternalRemove(item.Key, item.Value, true);
        }

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this, true);

        void IDictionary.Add(object key, object? value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            Throw.ThrowIfNullIsInvalid<TValue>(value);

            try
            {
                TKey typedKey = (TKey)key;
                try
                {
                    Add(typedKey, (TValue)value!);
                }
                catch (InvalidCastException)
                {
                    Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value!, typeValue));
                }
            }
            catch (InvalidCastException)
            {
                Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, typeKey));
            }
        }

        bool IDictionary.Contains(object key) => CanAcceptKey(key) && ContainsKey((TKey)key);

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, false);

        void IDictionary.Remove(object key)
        {
            if (CanAcceptKey(key))
                Remove((TKey)key);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (index < 0 || index > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (array.Length - index < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (array.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            switch (array)
            {
                case KeyValuePair<TKey, TValue>[] keyValuePairs:
                    ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(keyValuePairs, index);
                    return;

                case DictionaryEntry[] dictionaryEntries:
                    for (int i = first; i != -1; i = entries[i].NextInOrder)
                    {
                        dictionaryEntries[index] = new DictionaryEntry(entries![i].Key!, entries[i].Value);
                        index += 1;
                    }

                    return;

                case object[] objectArray:
                    for (int i = first; i != -1; i = entries[i].NextInOrder)
                    {
                        objectArray[index] = new KeyValuePair<TKey, TValue>(entries![i].Key, entries[i].Value);
                        index += 1;
                    }

                    return;

                default:
                    Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
                    return;
            }
        }

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);

            info.AddValue(nameof(capacity), capacity);
            info.AddValue(nameof(ensureCapacity), ensureCapacity);
            info.AddValue(nameof(comparer), ComparerHelper<TKey>.IsDefaultComparer(comparer) ? null : comparer);
            info.AddValue(nameof(itemLoader), itemLoader.Equals(nullLoader) ? null : itemLoader);
            info.AddValue(nameof(behavior), (byte)behavior);
            info.AddValue(nameof(disposeDroppedValues), disposeDroppedValues);

            int count = Count;
            TKey[] keys = new TKey[count];
            TValue[] values = new TValue[count];
            if (count > 0)
            {
                int i = 0;
                for (int current = first; current != -1; current = entries[current].NextInOrder, i++)
                {
                    keys[i] = entries![current].Key;
                    values[i] = entries[current].Value;
                }
            }

            info.AddValue(nameof(keys), keys);
            info.AddValue(nameof(values), values);

            info.AddValue(nameof(version), version);
            info.AddValue(nameof(cacheReads), cacheReads);
            info.AddValue(nameof(cacheWrites), cacheWrites);
            info.AddValue(nameof(cacheDeletes), cacheDeletes);
            info.AddValue(nameof(cacheHit), cacheHit);

            // custom data of a derived class
            GetObjectData(info, context);
        }

        [SecuritySafeCritical]
        void IDeserializationCallback.OnDeserialization(object? sender)
        {
            SerializationInfo? info = deserializationInfo;

            // may occur with remoting, which calls OnDeserialization twice.
            if (info == null)
                return;

            capacity = info.GetInt32(nameof(capacity));
            ensureCapacity = info.GetBoolean(nameof(ensureCapacity));
            comparer = ComparerHelper<TKey>.GetSpecialDefaultEqualityComparerOrNull((IEqualityComparer<TKey>?)info.GetValue(nameof(comparer), typeof(IEqualityComparer<TKey>)));
            behavior = (CacheBehavior)info.GetByte(nameof(behavior));
            itemLoader = (Func<TKey, TValue>?)info.GetValue(nameof(itemLoader), typeof(Func<TKey, TValue>)) ?? nullLoader;
            disposeDroppedValues = info.GetBoolean(nameof(disposeDroppedValues));

            // elements
            TKey[] keys = (TKey[])info.GetValue(nameof(keys), typeof(TKey[]))!;
            TValue[] values = (TValue[])info.GetValue(nameof(values), typeof(TValue[]))!;
            int count = keys.Length;
            if (count > 0)
            {
                Initialize(ensureCapacity ? capacity : count);
                for (int i = 0; i < count; i++)
                    Insert(keys[i]!, values[i], true);
            }

            version = info.GetInt32(nameof(version));
            cacheReads = info.GetInt32(nameof(cacheReads));
            cacheDeletes = info.GetInt32(nameof(cacheWrites));
            cacheDeletes = info.GetInt32(nameof(cacheDeletes));
            cacheHit = info.GetInt32(nameof(cacheHit));

            OnDeserialization(info);

            deserializationInfo = null;
        }

        #endregion

        #endregion

        #endregion
    }
}