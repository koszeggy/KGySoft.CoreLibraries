#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeDictionary.cs
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
#if !NET35
using System.Collections.Concurrent; 
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Diagnostics;
using KGySoft.Serialization.Binary;

#endregion

#region Suppressions

#if NET35 || NET40
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif
#if NET40 || NET45 || NET472 || NETSTANDARD
#pragma warning disable CS0436 // Type conflicts with imported type - Using custom SpinWait even if available in some targets
#endif
#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#endif

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Implements a thread-safe dictionary, which can be a good alternative for <see cref="ConcurrentDictionary{TKey,TValue}"/> where it is not available (.NET Framework 3.5),
    /// or where <see cref="ConcurrentDictionary{TKey,TValue}"/> has a poorer performance.
    /// <br/>See the <strong>Remarks</strong> section for details and for a comparison between <see cref="ThreadSafeDictionary{TKey,TValue}"/> and <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys stored in the dictionary.</typeparam>
    /// <typeparam name="TValue">Type of the values stored in the dictionary.</typeparam>
    /// <remarks>
    /// <note type="tip">
    /// <list type="bullet">
    /// <item>If you would only use the <see cref="GetOrAdd(TKey, Func{TKey, TValue})"/> method, then consider to create a thread safe cache by the <see cref="ThreadSafeCacheFactory"/> instead,
    /// which is much faster than both <see cref="ThreadSafeDictionary{TKey,TValue}"/> and <see cref="ConcurrentDictionary{TKey,TValue}"/>, and supports capacity management as well.</item>
    /// <item>If you want to wrap any <see cref="IDictionary{TKey,TValue}"/> into a thread-safe wrapper without copying the actual items, then you can also use <see cref="LockingDictionary{TKey,TValue}"/>.</item>
    /// </list></note>
    /// <para>The purpose of <see cref="ThreadSafeDictionary{TKey,TValue}"/> is similar to <see cref="ConcurrentDictionary{TKey,TValue}"/> but its approach is somewhat different.
    /// While <see cref="ConcurrentDictionary{TKey,TValue}"/> uses a group of locks to perform modifications (their amount can be configured or depends on the number of CPU cores);
    /// on the other hand, <see cref="ThreadSafeDictionary{TKey,TValue}"/> uses two separate internal storage: items with new keys are added to a temporary storage using a single lock,
    /// which might regularly be merged into a faster lock-free storage, depending on the value of the <see cref="MergeInterval"/> property. Once the items are merged, their access
    /// (both read and write) becomes lock free. Even deleting and re-adding a value for the same key becomes faster after the key is merged into the lock-free storage.
    /// <note>Therefore, <see cref="ThreadSafeDictionary{TKey,TValue}"/> is not always a good alternative of <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// If new keys are continuously added, then always a shared lock is used, in which case <see cref="ConcurrentDictionary{TKey,TValue}"/> might perform better, unless you need to use
    /// some members, which are very slow in <see cref="ConcurrentDictionary{TKey,TValue}"/> (see the table below). If the newly added elements are regularly removed,
    /// make sure the <see cref="PreserveMergedKeys"/> property is <see langword="false"/>; otherwise, the already merged keys are not removed from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>
    /// even if they are deleted or when you call the <see cref="Clear">Clear</see> method. To remove even the merged keys you must call the <see cref="Reset">Reset</see> method,
    /// or to remove the deleted keys only you can explicitly call the <see cref="TrimExcess">TrimExcess</see> method.</note></para>
    /// <h1 class="heading">Comparison with <see cref="ConcurrentDictionary{TKey,TValue}"/></h1>
    /// <para><strong>When to use</strong>&#160;<see cref="ThreadSafeDictionary{TKey,TValue}"/>:
    /// <list type="bullet">
    /// <item>If it is known that a fixed set of keys will be used. <see cref="ThreadSafeDictionary{TKey,TValue}"/> is fast if the already added keys are updated,
    /// or even deleted and re-added with any value. In this case consider to set the <see cref="PreserveMergedKeys"/> to <see langword="true"/> so it is not checked whether
    /// a cleanup should be performed due to many deleted items.</item>
    /// <item>If you access mainly existing keys by the <see cref="O:KGySoft.Collections.ThreadSafeDictionary`2.AddOrUpdate">AddOrUpdate</see> methods,
    /// which are separate try get/add/update operations at <see cref="ConcurrentDictionary{TKey,TValue}"/> but are optimized at <see cref="ThreadSafeDictionary{TKey,TValue}"/> to avoid
    /// multiple lookups.</item>
    /// <item>If it is needed to access <see cref="Count"/>, enumerate the items or <see cref="Keys"/>/<see cref="Values"/>, or you need to call <see cref="ToArray">ToArray</see>,
    /// which are particularly slow in case of <see cref="ConcurrentDictionary{TKey,TValue}"/>.</item>
    /// <item>If it is expected that there will be many hash collisions.</item>
    /// <item>If the dictionary is needed to be serialized.</item>
    /// </list></para>
    /// <para><strong>Performance comparison</strong> with <see cref="ConcurrentDictionary{TKey,TValue}"/> (default concurrency level depends on <see cref="Environment.ProcessorCount"/>):
    /// <list type="table">
    /// <listheader><term>Member(s) or operation</term><term><see cref="ThreadSafeDictionary{TKey,TValue}"/></term><term><see cref="ConcurrentDictionary{TKey,TValue}"/>, concurrency level = 2</term><term><see cref="ConcurrentDictionary{TKey,TValue}"/>, concurrency level = 8</term></listheader>
    /// <item><term><see cref="Count">Count</see></term><term>Fastest</term><term>5.01x slower</term><term>530.44x slower</term></item>
    /// <item><term><see cref="TryAdd">TryAdd</see> (10 million new keys, sequential)</term><term>1.07x slower</term><term>Fastest</term><term>1.25x slower</term></item>
    /// <item><term><see cref="TryAdd">TryAdd</see> (10 million new keys, parallel)</term><term>1.02x slower</term><term>Fastest</term><term>1.53x slower</term></item>
    /// <item><term><see cref="TryGetValue">TryGetValue</see> (no collisions, <typeparamref name="TKey"/> is <see cref="int">int</see>)</term><term>1.55x slower</term><term>Fastest</term><term>Fastest</term></item>
    /// <item><term><see cref="TryGetValue">TryGetValue</see> (no collisions, <typeparamref name="TKey"/> is <see cref="string">string</see>)</term><term>Fastest</term><term>1.26x slower</term><term>1.26x slower</term></item>
    /// <item><term><see cref="TryGetValue">TryGetValue</see> (many key collisions)</term><term>Fastest</term><term>1.92x slower</term><term>2.01x slower</term></item>
    /// <item><term><see cref="this">Indexer</see> set (sequential, <typeparamref name="TKey"/> is <see cref="int">int</see>)</term><term>Fastest</term><term>1.07x slower</term><term>1.07x slower</term></item>
    /// <item><term><see cref="this">Indexer</see> set (parallel, <typeparamref name="TKey"/> is <see cref="int">int</see>)</term><term>Fastest</term><term>2.03x slower</term><term>1.01x slower</term></item>
    /// <item><term><see cref="TryUpdate">TryUpdate</see> (sequential)</term><term>Fastest</term><term>1.21x slower</term><term>1.17x slower</term></item>
    /// <item><term><see cref="TryUpdate">TryUpdate</see> (parallel)</term><term>Fastest</term><term>4.02x slower</term><term>1.27x slower</term></item>
    /// <item><term><see cref="TryRemove(TKey)">TryRemove</see> + <see cref="TryAdd">TryAdd</see> (same key, sequential)</term><term>Fastest</term><term>1.12x slower</term><term>1.16x slower</term></item>
    /// <item><term><see cref="TryRemove(TKey)">TryRemove</see> + <see cref="TryAdd">TryAdd</see> (same key, parallel)</term><term>Fastest</term><term>2.53x slower</term><term>2.43x slower</term></item>
    /// <item><term>Enumerating items</term><term>Fastest</term><term>2.87x slower</term><term>262.45x slower</term></item>
    /// <item><term>Enumerating <see cref="Keys"/></term><term>Fastest</term><term>117.92x slower</term><term>368.11x slower</term></item>
    /// <item><term><see cref="ToArray">ToArray</see></term><term>1.69x slower</term><term>Fastest</term><term>2.12x slower</term></item>
    /// <item><term><see cref="AddOrUpdate(TKey,TValue,Func{TKey,TValue,TValue})">AddOrUpdate</see> (random existing keys, sequential)</term><term>Fastest</term><term>3.35x slower</term><term>3.16x slower</term></item>
    /// <item><term><see cref="AddOrUpdate(TKey,TValue,Func{TKey,TValue,TValue})">AddOrUpdate</see> (random existing keys, parallel)</term><term>Fastest</term><term>9.74x slower</term><term>2.27x slower</term></item>
    /// <item><term><see cref="GetOrAdd(TKey,TValue)">GetOrAdd</see> (random existing keys)</term><term>1.11x slower</term><term>Fastest</term><term>Fastest</term></item>
    /// </list>
    /// <note type="tip">If <typeparamref name="TKey"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
    /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the constructor for even better performance.
    /// Or, you can use <see cref="StringSegmentComparer.OrdinalRandomized">StringSegmentComparer.OrdinalRandomized</see> to use a comparer with randomized hash also on
    /// platforms where default string hashing is not randomized (eg. .NET Framework 3.5).</note></para>
    /// <para><strong>Incompatibilities</strong> with <see cref="ConcurrentDictionary{TKey,TValue}"/>:
    /// <list type="bullet">
    /// <item>Constructor signatures are different</item>
    /// <item>The <see cref="Keys"/> and <see cref="Values"/> property of <see cref="ThreadSafeDictionary{TKey,TValue}"/> return wrappers for the current keys and values
    /// (enumerating the same instance again and again may yield different items), whereas <see cref="ConcurrentDictionary{TKey,TValue}"/> return a snapshot for these properties.</item>
    /// <item>The <see cref="ICollection.SyncRoot"/> property of <see cref="Keys"/> and <see cref="Values"/> throw a <see cref="NotSupportedException"/> just like for their
    /// owner <see cref="ThreadSafeDictionary{TKey,TValue}"/> instance. In contrast, in case of <see cref="ConcurrentDictionary{TKey,TValue}"/> only the dictionary itself throws an exception
    /// when accessing the <see cref="ICollection.SyncRoot"/>, whereas its keys and values don't.</item>
    /// </list></para>
    /// </remarks>
    /// <seealso cref="ThreadSafeCacheFactory"/>
    /// <seealso cref="LockingDictionary{TKey,TValue}"/>
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ").Name}; TValue = {typeof(" + nameof(TValue) + ").Name}")]
    [Serializable]
    public partial class ThreadSafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, ISerializable, IDeserializationCallback
        where TKey : notnull
    {
        #region Constants

        private const int defaultCapacity = 4;

        #endregion

        #region Fields

        #region Static Fields

        private static readonly IEqualityComparer<TKey> defaultComparer = ComparerHelper<TKey>.EqualityComparer;
        private static readonly IEqualityComparer<TValue> valueComparer = ComparerHelper<TValue>.EqualityComparer;

        #endregion

        #region Instance Fields

        private readonly object syncRoot = new object();

        private IEqualityComparer<TKey>? comparer;
        private int initialLockingCapacity;
        private bool bitwiseAndHash;
        private volatile bool preserveMergedKeys;
        private volatile bool isMerging;
        private long mergeInterval = TimeHelper.GetInterval(100);
        private long nextMerge;

        /// <summary>
        /// Values here are accessed without locking.
        /// Once a new key is added, it is never removed anymore even for deleted entries.
        /// </summary>
        private volatile FixedSizeStorage fixedSizeStorage;

        /// <summary>
        /// A temporary storage for new values. It is regularly merged with fixedSizeStorage into a new fixed size instance.
        /// </summary>
        private volatile TempStorage? expandableStorage;

        private KeysCollection? keysCollection;
        private ValuesCollection? valuesCollection;
        private SerializationInfo? deserializationInfo;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the number of elements contained in this <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if (expandableStorage == null)
                    return fixedSizeStorage.Count;

                lock (syncRoot)
                {
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return fixedSizeStorage.Count;

                    int result = fixedSizeStorage.Count + lockingValues.Count;
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Gets whether this <see cref="ThreadSafeDictionary{TKey,TValue}"/> is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                // Note: here we access even expandableStorage without locking. Though expandableStorage.Count returns an
                // expression where both operands are mutable we can accept it because we don't expect multiple changes at
                // once (changes are in locks) and expandableStorage is volatile so Count will see valid values.
                return fixedSizeStorage.Count == 0 && (expandableStorage?.Count ?? 0) == 0;
            }
        }

        /// <summary>
        /// Gets or sets whether keys of entries that have already been merged into the faster lock-free storage are preserved even when their value is removed.
        /// <br/>Default value: <see langword="false"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <para>If the possible number of keys in this <see cref="ThreadSafeDictionary{TKey,TValue}"/> is known to be a limited value, then this property
        /// can be set to <see langword="true"/>, so once the values have been merged into the faster lock-free storage, their entry is not removed anymore even if the
        /// corresponding value is deleted. This ensures that removing and re-adding a value with the same key again and again remains a lock-free operation.
        /// <note>Do not set this property to <see langword="true"/>, if the number of the possibly added keys is not limited.</note></para>
        /// <para>This property can be set to <see langword="true"/>&#160;even if keys are never removed so it is not checked before a merge operation whether
        /// the amount of deleted items exceeds a specific limit.</para>
        /// <para>If this property is <see langword="true"/>, then the already merged keys are not removed even when calling the <see cref="Clear">Clear</see> method.
        /// The memory of the deleted entries can be freed by explicitly calling the <see cref="TrimExcess">TrimExcess</see> method,
        /// whereas to remove all allocated entries you can call the <see cref="Reset">Reset</see> method.</para>
        /// <para>Even if this property is <see langword="false"/>, the removed keys are not dropped immediately. Unused keys are removed during a merge operation and only
        /// when their number exceeds a specific limit. You can call the <see cref="TrimExcess">TrimExcess</see> method to force removing unused keys on demand.</para>
        /// </remarks>
        public bool PreserveMergedKeys
        {
            get => preserveMergedKeys;
            set => preserveMergedKeys = value;
        }

        /// <summary>
        /// Gets or sets the minimum lifetime for the temporarily created internal locking storage when adding new keys to the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// <br/>Default value: 100 milliseconds.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <para>When adding items with new keys, they will be put in a temporary locking storage first.
        /// Whenever the locking storage is accessed, it will be checked whether the specified time interval has been expired since its creation. If so, then
        /// it will be merged with the previous content of the fast non-locking storage into a new one.
        /// If new keys are typically added together, rarely or periodically, then it is recommended to set some small positive value (up to a few seconds).</para>
        /// <para>Even if the value of this property is <see cref="TimeSpan.Zero">TimeSpan.Zero</see>, adding new items are not necessarily merged immediately
        /// to the fast-accessing storage. Depending on the targeted platform a minimum 15 ms delay is possible. Setting <see cref="TimeSpan.Zero">TimeSpan.Zero</see>
        /// is not recommended though, unless new items are almost never added at the same time.</para>
        /// <para>When the value of this property is negative (eg. <see cref="Timeout.InfiniteTimeSpan">Timeout.InfiniteTimeSpan</see>), then the locking storage is not merged
        /// automatically with the lock-free storage. You still can call the <see cref="EnsureMerged">EnsureMerged</see> method to perform a merge explicitly.</para>
        /// <para>This property is ignored if a value is accessed in the fast-accessing storage including removing and adding values of keys that have already been merged to the lock-free storage.</para>
        /// <note>Some operations (such as enumerating the <see cref="ThreadSafeDictionary{TKey,TValue}"/> or its <see cref="Keys"/> and <see cref="Values"/>,
        /// calling the <see cref="ToArray">ToArray</see> or the <see cref="ICollection.CopyTo">ICollection.CopyTo</see> implementations) as well as serializing the dictionary may trigger a merging
        /// regardless the value of this property.</note>
        /// </remarks>
        public TimeSpan MergeInterval
        {
            get => TimeHelper.GetTimeSpan(Volatile.Read(ref mergeInterval));
            set
            {
                lock (syncRoot)
                {
                    long interval = TimeHelper.GetInterval(value);

                    // adjusting to prevent immediate merging for nonzero value
                    if (interval == 0L && value != TimeSpan.Zero)
                        interval = value < TimeSpan.Zero ? -1L : 1L;

                    if (interval == mergeInterval)
                        return;

                    mergeInterval = interval;

                    if (expandableStorage == null || interval < 0L)
                        return;

                    nextMerge = TimeHelper.GetTimeStamp() + interval;
                }
            }
        }

        /// <summary>
        /// Gets a collection reflecting the keys stored in this <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the keys in the original <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TKey> Keys
        {
            get
            {
                if (keysCollection == null)
                    Interlocked.CompareExchange(ref keysCollection, new KeysCollection(this), null);
                return keysCollection;
            }
        }

        /// <summary>
        /// Gets a collection reflecting the values stored in this <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ICollection{T}"/> is not a static copy; instead, the <see cref="ICollection{T}"/> refers back to the values in the original <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// Therefore, changes to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> continue to be reflected in the <see cref="ICollection{T}"/>.</para>
        /// <para>Retrieving the value of this property is an O(1) operation.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TValue> Values
        {
            get
            {
                if (valuesCollection == null)
                    Interlocked.CompareExchange(ref valuesCollection, new ValuesCollection(this), null);
                return valuesCollection;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        ICollection IDictionary.Keys => new KeysCollection(this);
        ICollection IDictionary.Values => new ValuesCollection(this);
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => Throw.NotSupportedException<object>();

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        /// <summary>
        /// Gets or sets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value of the specified <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get => TryGetValue(key, out TValue? value) ? value : Throw.KeyNotFoundException<TValue>(Res.IDictionaryKeyNotFound);
            set
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.OverwriteIfExists);
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object? IDictionary.this[object key]
        {
            get => key switch
            {
                TKey k => TryGetValue(k, out TValue? value) ? value : null,
                null => Throw.ArgumentNullException<object>(Argument.key),
                _ => null
            };
            set
            {
                if (key == null!)
                    Throw.ArgumentNullException(Argument.key);
                Throw.ThrowIfNullIsInvalid<TValue>(value);
                if (key is TKey k)
                {
                    if (value is TValue v)
                        this[k] = v;
                    else
                        Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeof(TValue)));
                }
                else
                    Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, typeof(TKey)));
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors
        
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class that is empty
        /// and uses the default comparer and <see cref="HashingStrategy.Auto"/> hashing strategy.
        /// </summary>
        public ThreadSafeDictionary() : this(defaultCapacity, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class that is empty
        /// and uses the default comparer and the specified hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="capacity">Specifies the initial minimum capacity of the internal temporal storage for values with new keys.
        /// If 0, then a default capacity is used.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeDictionary{TKey, TValue}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        public ThreadSafeDictionary(int capacity, HashingStrategy strategy = HashingStrategy.Auto)
            : this(capacity, null, strategy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class that is empty
        /// and uses the specified <paramref name="comparer"/> and hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeDictionary{TKey, TValue}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        /// <remarks>
        /// <note type="tip">If <typeparamref name="TKey"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
        /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the <paramref name="comparer"/> parameter for better performance.</note>
        /// </remarks>
        public ThreadSafeDictionary(IEqualityComparer<TKey>? comparer, HashingStrategy strategy = HashingStrategy.Auto)
            : this(defaultCapacity, comparer, strategy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class that is empty
        /// and uses the specified <paramref name="comparer"/> and hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="capacity">Specifies the initial minimum capacity of the internal temporal storage for values with new keys.
        /// If 0, then a default capacity is used.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeDictionary{TKey, TValue}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        /// <remarks>
        /// <note type="tip">If <typeparamref name="TKey"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
        /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the <paramref name="comparer"/> parameter for better performance.</note>
        /// </remarks>
        public ThreadSafeDictionary(int capacity, IEqualityComparer<TKey>? comparer, HashingStrategy strategy = HashingStrategy.Auto)
        {
            if (!strategy.IsDefined())
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);
            if (capacity <= 0)
            {
                if (capacity < 0)
                    Throw.ArgumentOutOfRangeException(Argument.capacity, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                capacity = defaultCapacity;
            }

            fixedSizeStorage = FixedSizeStorage.Empty;
            initialLockingCapacity = capacity;
            bitwiseAndHash = strategy.PreferBitwiseAndHash(comparer);
            this.comparer = ComparerHelper<TKey>.GetSpecialDefaultEqualityComparerOrNull(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class from the specified <paramref name="collection"/>
        /// and uses the specified <paramref name="comparer"/> and hashing <paramref name="strategy"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements are coped to the new <see cref="ThreadSafeDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys. When <see langword="null"/>, <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see>
        /// will be used for <see langword="enum"/>&#160;key types, and <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other types. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="strategy">The hashing strategy to be used in the created <see cref="ThreadSafeDictionary{TKey, TValue}"/>. This parameter is optional.
        /// <br/>Default value: <see cref="HashingStrategy.Auto"/>.</param>
        /// <remarks>
        /// <note type="tip">If <typeparamref name="TKey"/> is <see cref="string">string</see> and it is safe to use a non-randomized string comparer,
        /// then you can pass <see cref="StringSegmentComparer.Ordinal">StringSegmentComparer.Ordinal</see> to the <paramref name="comparer"/> parameter for better performance.</note>
        /// </remarks>
        public ThreadSafeDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer = null, HashingStrategy strategy = HashingStrategy.Auto)
            : this(defaultCapacity, comparer, strategy)
        {
            if (collection == null!)
                Throw.ArgumentNullException(Argument.dictionary);

            // trying to initialize directly in the fixed storage
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile - no problem, this is the constructor so there are no other threads at this point
            if (collection is ICollection<KeyValuePair<TKey, TValue>> c && FixedSizeStorage.TryInitialize(c, bitwiseAndHash, comparer, out fixedSizeStorage))
                return;
#pragma warning restore CS0420

            // initializing in the expando storage (no locking is needed here as we are in the constructor)
            fixedSizeStorage = FixedSizeStorage.Empty;
            expandableStorage = new TempStorage(collection, comparer, bitwiseAndHash);
            nextMerge = TimeHelper.GetTimeStamp() + mergeInterval;
        }

        #endregion

        #region Protected Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDictionary{TKey, TValue}"/> class from serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that stores the data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this deserialization.</param>
        /// <remarks><note type="inherit">If an inherited type serializes data, which may affect the hashes of the keys, then override
        /// the <see cref="OnDeserialization">OnDeserialization</see> method and use that to restore the data of the derived instance.</note></remarks>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. - the fields will be initialized in IDeserializationCallback.OnDeserialization
        protected ThreadSafeDictionary(SerializationInfo info, StreamingContext context)
#pragma warning restore CS8618
        {
            // deferring the actual deserialization until all objects are finalized and hashes do not change anymore
            deserializationInfo = info;
        }

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <remarks>
        /// <para>If the <paramref name="key"/> of element already exists in the cache, this method throws an exception.
        /// In contrast, using the setter of the <see cref="this">indexer</see> property replaces the old value with the new one.</para>
        /// <para>If multiple values are added to this <see cref="ThreadSafeDictionary{TKey,TValue}"/> concurrently, then you should use
        /// the <see cref="TryAdd">TryAdd</see> method instead, which simply returns <see langword="false"/>&#160;if the <paramref name="key"/>
        /// to add already exists in the dictionary.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> already exists in the cache.</exception>
        /// <seealso cref="this"/>
        /// <seealso cref="TryAdd"/>
        public void Add(TKey key, TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.ThrowIfExists);
        }

        /// <summary>
        /// Tries to add the specified <paramref name="key"/> and <paramref name="value"/> to the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be <see langword="null"/>&#160;for reference types.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="key"/> and <paramref name="value"/> was added to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> successfully;
        /// <see langword="false"/>&#160;if the key already exists.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <seealso cref="O:KGySoft.Collections.ThreadSafeDictionary`2.GetOrAdd">GetOrAdd</seealso>
        /// <seealso cref="O:KGySoft.Collections.ThreadSafeDictionary`2.AddOrUpdate">AddOrUpdate</seealso>
        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            return TryInsertInternal(key, value, GetHashCode(key), DictionaryInsertion.DoNotOverwrite);
        }

        /// <summary>
        /// Tries to get the value associated with the specified <paramref name="key"/> from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns><see langword="true"/>&#160;if the key was found in the <see cref="ThreadSafeDictionary{TKey,TValue}"/>; otherwise, <see langword="false"/>.</returns>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the <paramref name="key"/> is found;
        /// otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <seealso cref="this"/>
        /// <seealso cref="O:KGySoft.Collections.ThreadSafeDictionary`2.GetOrAdd">GetOrAdd</seealso>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            return TryGetValueInternal(key, hashCode, out value);
        }

        /// <summary>
        /// Determines whether the <see cref="ThreadSafeDictionary{TKey,TValue}"/> contains an element with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="ThreadSafeDictionary{TKey,TValue}"/> contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(TKey key) => TryGetValue(key, out var _);

        /// <summary>
        /// Determines whether the <see cref="ThreadSafeDictionary{TKey,TValue}"/> contains an element with the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="ThreadSafeDictionary{TKey,TValue}"/> contains an element with the specified <paramref name="value"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method determines equality using the <see cref="EnumComparer{TEnum}.Comparer">EnumComparer&lt;TEnum&gt;.Comparer</see> when <typeparamref name="TValue"/> is an <see langword="enum"/>&#160;type,
        /// or the default equality comparer <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see> for other <typeparamref name="TValue"/> types.</para>
        /// <para>This method performs a linear search; therefore, this method is an O(n) operation.</para>
        /// </remarks>
        public bool ContainsValue(TValue value)
        {
            FixedSizeStorage.InternalEnumerator lockFreeEnumerator = fixedSizeStorage.GetInternalEnumerator();
            while (lockFreeEnumerator.MoveNext())
            {
                if (valueComparer.Equals(value, lockFreeEnumerator.Current.Value))
                    return true;
            }

            if (expandableStorage == null)
                return false;

            lock (syncRoot)
            {
                TempStorage? lockingValues = expandableStorage;

                // lost race
                if (lockingValues == null)
                    return false;

                bool result = false;
                TempStorage.InternalEnumerator lockingEnumerator = lockingValues.GetInternalEnumerator();
                while (!result && lockingEnumerator.MoveNext())
                    result = valueComparer.Equals(value, lockingEnumerator.Current.Value);
                MergeIfExpired();
                return result;
            }
        }

        /// <summary>
        /// Tries to remove the value with the specified <paramref name="key"/> from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">Key of the item to remove.</param>
        /// <returns><see langword="true"/>&#160;if the element is successfully removed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool TryRemove(TKey key)
        {
            // Note: we could re-use TryRemove(TKey, out TValue) but it is faster to do it this way
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            bool removed = false;
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryRemoveInternal(key, hashCode);

                // making sure that correct result is returned even if multiple tries are necessary due to merging
                if (success == true)
                    removed = true;

                // successfully removed from fixed-size storage (now, or in a previous attempt)
                if (removed)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;

                    continue;
                }

                // item was already deleted (if we removed it in a previous attempt, then true is returned above)
                if (success == false)
                    return false;

                if (expandableStorage == null)
                    return false;

                lock (syncRoot)
                {
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return false;

                    bool result = lockingValues.TryRemoveInternal(key, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Tries to remove and return the <paramref name="value"/> with the specified <paramref name="key"/> from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">Key of the item to remove.</param>
        /// <param name="value">When this method returns, contains the value removed from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>,
        /// or the default value of the <typeparamref name="TValue"/> type if <paramref name="key"/> does not exist.</param>
        /// <returns><see langword="true"/>&#160;if the element is successfully removed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool TryRemove(TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryRemoveInternal(key, hashCode, out value);

                // making sure that removed entry is returned even if multiple tries are necessary due to merging
                if (success == true)
                {
                    if (IsUpToDate(lockFreeValues))
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition. - false alarm, TryRemoveInternal returned true
                        return true;
#pragma warning restore CS8762

                    continue;
                }

                // item was already deleted
                if (success == false)
                    return false;

                if (expandableStorage == null)
                    return false;

                lock (syncRoot)
                {
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return false;

                    bool result = lockingValues.TryRemoveInternal(key, hashCode, out value);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Tries to remove the item from the <see cref="ThreadSafeDictionary{TKey,TValue}"/> that has the specified <paramref name="key"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The key of the item to remove.</param>
        /// <param name="value">The value of the item to remove.</param>
        /// <returns><see langword="true"/>&#160;if the element is successfully removed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public bool TryRemove(TKey key, TValue value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            bool removed = false;
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryRemoveInternal(key, value, hashCode);

                // making sure that correct result is returned even if multiple tries are necessary due to merging
                if (success == true)
                    removed = true;

                // successfully removed from fixed-size storage (now, or in a previous attempt)
                if (removed)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;

                    continue;
                }

                // item was already deleted (if we removed it in a previous attempt, then true is returned above)
                if (success == false)
                    return false;

                if (expandableStorage == null)
                    return false;

                lock (syncRoot)
                {
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return false;

                    bool result = lockingValues.TryRemoveInternal(key, value, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="PreserveMergedKeys"/> is <see langword="true"/>, or when the amount of removed items does not exceed a limit, then
        /// This method is an O(n) operation where n is the number of elements present in the inner lock-free storage. Otherwise,
        /// this method calls the <see cref="Reset">Reset</see> method, which frees up all the allocated entries.</para>
        /// <note>Note that if <see cref="PreserveMergedKeys"/> is <see langword="true"/>, then though this method removes all values from
        /// the <see cref="ThreadSafeDictionary{TKey,TValue}"/>, it never removes the keys that are already merged into the faster lock-free storage.
        /// This ensures that adding a new value with an already used key will always be a fast, lock-free operation.
        /// To remove all keys and values use the <see cref="Reset">Reset</see> method instead.</note>
        /// </remarks>
        /// <seealso cref="Reset"/>
        public void Clear()
        {
            if (!preserveMergedKeys && fixedSizeStorage.Capacity > 16)
            {
                Reset();
                return;
            }

            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                lockFreeValues.Clear();
                if (!IsUpToDate(lockFreeValues))
                    continue;

                // It is not a problem if a merge has been started before this line because it will nullify expandableStorage in the end anyway.
                expandableStorage = null;
                return;
            }
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Clear">Clear</see> method for details.
        /// </summary>
        /// <seealso cref="Clear"/>
        public void Reset()
        {
            // We can't use a loop with IsUpToDate here because we replace fixedSizeStorage reference
            // but the lock waits also for possible concurrent merges to finish
            lock (syncRoot)
            {
                fixedSizeStorage = FixedSizeStorage.Empty;
                expandableStorage = null; 
            }
        }

        /// <summary>
        /// Updates the value associated with <paramref name="key"/> to <paramref name="newValue"/>
        /// if the existing value with <paramref name="key"/> is equal to <paramref name="originalValue"/>.
        /// </summary>
        /// <param name="key">The key of the item to replace.</param>
        /// <param name="newValue">The replacement value of <paramref name="key"/> if its value equals to <paramref name="originalValue"/>.</param>
        /// <param name="originalValue">The expected original value of the stored item with the associated <paramref name="key"/>.</param>
        /// <returns><see langword="true"/>&#160;if the value with <paramref name="key"/> was equal to <paramref name="originalValue"/>
        /// and was replaced with <paramref name="newValue"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public bool TryUpdate(TKey key, TValue newValue, TValue originalValue)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            return TryReplaceInternal(key, hashCode, newValue, originalValue);
        }

        /// <summary>
        /// Adds or updates a key/value pair in the <see cref="ThreadSafeDictionary{TKey,TValue}"/> based on whether the specified <paramref name="key"/> already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated.</param>
        /// <param name="addValue">The value to be added for an absent key.</param>
        /// <param name="updateValue">The value to be set for an existing key.</param>
        /// <returns>The new value for the <paramref name="key"/>. This will be either <paramref name="addValue"/> (if the key was absent)
        /// or <paramref name="updateValue"/> (if the key was present).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public TValue AddOrUpdate(TKey key, TValue addValue, TValue updateValue)
        {
            // Note: we could just return AddOrUpdate(key, _ => addValue, _ => updateValue) but creating a new delegate would be a great performance loss
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                if (lockFreeValues.TryAddOrUpdate(key, addValue, updateValue, hashCode, out TValue? result))
                {
                    if (IsUpToDate(lockFreeValues))
                        return result;
                    continue;
                }

                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    result = lockingValues.AddOrUpdate(key, addValue, updateValue, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> if the <paramref name="key"/> does not already exist,
        /// or updates a key/value pair in the <see cref="ThreadSafeDictionary{TKey,TValue}"/> by using the specified <paramref name="updateValueFactory"/> if the <paramref name="key"/> already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated.</param>
        /// <param name="addValue">The value to be added for an absent key.</param>
        /// <param name="updateValueFactory">A delegate used to generate a new value for an existing key based on the key's existing value.</param>
        /// <returns>The new value for the <paramref name="key"/>. This will be either <paramref name="addValue"/> (if the key was absent)
        /// or the result of <paramref name="updateValueFactory"/> (if the key was present).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="updateValueFactory"/> is <see langword="null"/>.</exception>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            // Note: we could just return AddOrUpdate(key, _ => addValue, updateValueFactory) but creating a new delegate would be a great performance loss
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (updateValueFactory == null!)
                Throw.ArgumentNullException(nameof(updateValueFactory));

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                if (lockFreeValues.TryAddOrUpdate(key, addValue, updateValueFactory, hashCode, out TValue? result))
                {
                    if (IsUpToDate(lockFreeValues))
                        return result;
                    continue;
                }

                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    result = lockingValues.AddOrUpdate(key, addValue, updateValueFactory, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Uses the specified delegates to add a key/value pair to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> if the <paramref name="key"/> does not already exist,
        /// or to update a key/value pair in the <see cref="ThreadSafeDictionary{TKey,TValue}"/> if the <paramref name="key"/> already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated.</param>
        /// <param name="addValueFactory">A delegate used to generate a value for an absent key.</param>
        /// <param name="updateValueFactory">A delegate used to generate a new value for an existing key based on the key's existing value.</param>
        /// <returns>The new value for the <paramref name="key"/>. This will be either the result of <paramref name="addValueFactory"/> (if the key was absent)
        /// or the result of <paramref name="updateValueFactory"/> (if the key was present).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/>, <paramref name="addValueFactory"/> or <paramref name="updateValueFactory"/> is <see langword="null"/>.</exception>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (addValueFactory == null!)
                Throw.ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory == null!)
                Throw.ArgumentNullException(nameof(updateValueFactory));

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                if (lockFreeValues.TryAddOrUpdate(key, addValueFactory, updateValueFactory, hashCode, out TValue? result))
                {
                    if (IsUpToDate(lockFreeValues))
                        return result;
                    continue;
                }

                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    result = lockingValues.AddOrUpdate(key, addValueFactory, updateValueFactory, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Uses the specified delegates to add a key/value pair to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> if the <paramref name="key"/> does not already exist,
        /// or to update a key/value pair in the <see cref="ThreadSafeDictionary{TKey,TValue}"/> if the <paramref name="key"/> already exists.
        /// </summary>
        /// <typeparam name="TArg">The type of an argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/>.</typeparam>
        /// <param name="key">The key to be added or whose value should be updated.</param>
        /// <param name="addValueFactory">A delegate used to generate a value for an absent key.</param>
        /// <param name="updateValueFactory">A delegate used to generate a new value for an existing key based on the key's existing value.</param>
        /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/>.</param>
        /// <returns>The new value for the <paramref name="key"/>. This will be either the result of <paramref name="addValueFactory"/> (if the key was absent)
        /// or the result of <paramref name="updateValueFactory"/> (if the key was present).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/>, <paramref name="addValueFactory"/> or <paramref name="updateValueFactory"/> is <see langword="null"/>.</exception>
        public TValue AddOrUpdate<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
        {
            // Note: we could just return AddOrUpdate(key, k => addValueFactory.Invoke(k, factoryArgument), (k, v) => updateValueFactory.Invoke(k, v, factoryArgument))
            // but creating new delegates and invoking them from each other would be a great performance loss
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (addValueFactory == null!)
                Throw.ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory == null!)
                Throw.ArgumentNullException(nameof(updateValueFactory));

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                if (lockFreeValues.TryAddOrUpdate(key, addValueFactory, updateValueFactory, factoryArgument, hashCode, out TValue? result))
                {
                    if (IsUpToDate(lockFreeValues))
                        return result;
                    continue;
                }

                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    result = lockingValues.AddOrUpdate(key, addValueFactory, updateValueFactory, factoryArgument, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> if the key does not already exist, and returns either the added or the existing value.
        /// </summary>
        /// <param name="key">The key of the element to add or whose value should be returned.</param>
        /// <param name="addValue">The value to be added, if the key does not already exist.</param>
        /// <returns>The value for the key. This will be either the existing value for the <paramref name="key"/> if the key is already in the dictionary,
        /// or the specified <paramref name="addValue"/> if the key was not in the dictionary.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public TValue GetOrAdd(TKey key, TValue addValue)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                if (lockFreeValues.TryGetOrAdd(key, addValue, hashCode, out TValue? result))
                {
                    if (IsUpToDate(lockFreeValues))
                        return result;
                    continue;
                }

                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    result = lockingValues.GetOrAdd(key, addValue, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> by using the specified <paramref name="addValueFactory"/>
        /// if the key does not already exist, and returns either the added or the existing value.
        /// </summary>
        /// <param name="key">The key of the element to add or whose value should be returned.</param>
        /// <param name="addValueFactory">The delegate to be used to generate the value, if the key does not already exist.</param>
        /// <returns>The value for the key. This will be either the existing value for the <paramref name="key"/> if the key is already in the dictionary,
        /// or the result of the specified <paramref name="addValueFactory"/> if the key was not in the dictionary.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="addValueFactory"/> is <see langword="null"/>.</exception>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> addValueFactory)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (addValueFactory == null!)
                Throw.ArgumentNullException(nameof(addValueFactory));

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                if (lockFreeValues.TryGetOrAdd(key, addValueFactory, hashCode, out TValue? result))
                {
                    if (IsUpToDate(lockFreeValues))
                        return result;
                    continue;
                }

                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    result = lockingValues.GetOrAdd(key, addValueFactory, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ThreadSafeDictionary{TKey,TValue}"/> by using the specified <paramref name="addValueFactory"/>
        /// if the key does not already exist, and returns either the added or the existing value.
        /// </summary>
        /// <typeparam name="TArg">The type of an argument to pass into <paramref name="addValueFactory"/>.</typeparam>
        /// <param name="key">The key of the element to add or whose value should be returned.</param>
        /// <param name="addValueFactory">The delegate to be used to generate the value, if the key does not already exist.</param>
        /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory"/>.</param>
        /// <returns>The value for the key. This will be either the existing value for the <paramref name="key"/> if the key is already in the dictionary,
        /// or the result of the specified <paramref name="addValueFactory"/> if the key was not in the dictionary.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="addValueFactory"/> is <see langword="null"/>.</exception>
        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, TArg factoryArgument)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (addValueFactory == null!)
                Throw.ArgumentNullException(nameof(addValueFactory));

            uint hashCode = GetHashCode(key);
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                if (lockFreeValues.TryGetOrAdd(key, addValueFactory, factoryArgument, hashCode, out TValue? result))
                {
                    if (IsUpToDate(lockFreeValues))
                        return result;
                    continue;
                }

                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    result = lockingValues.GetOrAdd(key, addValueFactory, factoryArgument, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        /// <summary>
        /// Ensures that all elements in this <see cref="ThreadSafeDictionary{TKey,TValue}"/> are merged into the faster lock-free storage.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="MergeInterval"/> property for details.
        /// </summary>
        public void EnsureMerged()
        {
            if (expandableStorage == null)
                return;

            lock (syncRoot)
            {
                TempStorage? lockingValues = expandableStorage;

                // lost race
                if (lockingValues == null)
                    return;

                DoMerge(!preserveMergedKeys && fixedSizeStorage.IsCleanupLimitReached);
            }
        }

        /// <summary>
        /// Forces to perform a merge while removing all possibly allocated but already deleted entries from the lock-free storage,
        /// even if the <see cref="PreserveMergedKeys"/> property is <see langword="true"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="PreserveMergedKeys"/> property for details.
        /// </summary>
        public void TrimExcess()
        {
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                TempStorage? lockingValues = expandableStorage;

                if (lockingValues == null && lockFreeValues.DeletedCount == 0)
                {
                    // this is just needed for awaiting a possible concurrent merge session
                    if (IsUpToDate(lockFreeValues))
                        return;
                    continue;
                }

                lock (syncRoot)
                {
                    lockFreeValues = fixedSizeStorage;
                    lockingValues = expandableStorage;

                    if (lockingValues?.Count > 0)
                    {
                        DoMerge(true);
                        return;
                    }

                    // lost race
                    if (lockFreeValues.DeletedCount == 0)
                        return;

                    // special merge: just removing deleted entries from fixedSizeStorage
                    // As we set isMerging it is guaranteed that concurrent updates will wait in IsUpToDate after up to one update attempt
                    isMerging = true;
                    try
                    {
                        fixedSizeStorage = new FixedSizeStorage(lockFreeValues);
                    }
                    finally
                    {
                        isMerging = false;
                    }

                    expandableStorage = null;
                    return;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the keys and values of this <see cref="ThreadSafeDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> that can be used to iterate through the <see cref="Cache{TKey,TValue}"/>.
        /// </returns>
        /// <remarks>
        /// <para>The enumerator returned from the dictionary is safe to use concurrently with reads and writes to the dictionary; however, it does not represent a moment-in-time snapshot of the dictionary.
        /// The contents exposed through the enumerator may contain modifications made to the dictionary after <see cref="GetEnumerator">GetEnumerator</see> was called.</para>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this, true);

        /// <summary>
        /// Copies the key and value pairs stored in the <see cref="ThreadSafeDictionary{TKey,TValue}"/> to a new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of key and value pairs copied from the <see cref="ThreadSafeDictionary{TKey,TValue}"/>.</returns>
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            EnsureMerged();
            return fixedSizeStorage.ToArray();
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

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private bool TryGetValueInternal(TKey key, uint hashCode, [MaybeNullWhen(false)]out TValue value)
        {
            bool? success = fixedSizeStorage.TryGetValueInternal(key, hashCode, out value);
            if (success.HasValue)
                return success.Value;

            if (expandableStorage == null)
                return false;

            lock (syncRoot)
            {
                TempStorage? lockingValues = expandableStorage;

                // lost race
                if (lockingValues == null)
                    return false;

                bool result = lockingValues.TryGetValueInternal(key, hashCode, out value);
                MergeIfExpired();
                return result;
            }
        }

        private bool TryInsertInternal(TKey key, TValue value, uint hashCode, DictionaryInsertion behavior)
        {
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryInsertInternal(key, value, hashCode, behavior);
                if (success == true)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;
                    continue;
                }

                // duplicate key
                if (success == false)
                    return false;

                lock (syncRoot)
                {
                    TempStorage lockingValues = GetCreateLockingStorage();
                    bool result = lockingValues.TryInsertInternal(key, value, hashCode, behavior);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        private bool TryReplaceInternal(TKey key, uint hashCode, TValue newValue, TValue originalValue)
        {
            while (true)
            {
                FixedSizeStorage lockFreeValues = fixedSizeStorage;
                bool? success = lockFreeValues.TryReplaceInternal(key, newValue, originalValue, hashCode);
                if (success == true)
                {
                    if (IsUpToDate(lockFreeValues))
                        return true;
                    continue;
                }

                // already deleted or originalValue does not match
                if (success == false)
                    return false;

                if (expandableStorage == null)
                    return false;

                lock (syncRoot)
                {
                    TempStorage? lockingValues = expandableStorage;

                    // lost race
                    if (lockingValues == null)
                        return false;

                    bool result = lockingValues.TryReplaceInternal(key, newValue, originalValue, hashCode);
                    MergeIfExpired();
                    return result;
                }
            }
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private uint GetHashCode(TKey key)
        {
            IEqualityComparer<TKey>? comp = comparer;
            return (uint)(comp == null ? key.GetHashCode() : comp.GetHashCode(key));
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private TempStorage GetCreateLockingStorage()
        {
            TempStorage? result = expandableStorage;
            if (result != null)
                return result;
            result = expandableStorage = new TempStorage(initialLockingCapacity, comparer, bitwiseAndHash);
            long interval = mergeInterval;
            if (interval >= 0L)
                nextMerge = TimeHelper.GetTimeStamp() + interval;
            return result;
        }

        private void MergeIfExpired()
        {
            // must be called in lock
            if (mergeInterval >= 0L && TimeHelper.GetTimeStamp() > nextMerge)
                DoMerge(!preserveMergedKeys && fixedSizeStorage.IsCleanupLimitReached);
        }

        private void DoMerge(bool removeDeletedKeys)
        {
            // Must be in a lock to work properly!
            Debug.Assert(!isMerging || expandableStorage != null, "Make sure caller is in a lock");

            TempStorage lockingValues = expandableStorage!;
            if (lockingValues.Count != 0 || removeDeletedKeys)
            {
                // Indicating that from this point fixedSizeStorage cannot be considered safe even though its reference is not replaced yet.
                // Note: we could spare the flag if we just nullified fixedSizeStorage before merging but this way can prevent that keys in the
                // fixed-size storage reappear in the locking one.
                isMerging = true;
                try
                {
                    fixedSizeStorage = new FixedSizeStorage(fixedSizeStorage, lockingValues, removeDeletedKeys);
                }
                finally
                {
                    // now we can reset the flag because an outdated storage reference can be detected safely
                    isMerging = false;
                }
            }

            expandableStorage = null;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private bool IsUpToDate(FixedSizeStorage lockFreeValues)
        {
            // fixed-size storage has been replaced
            if (lockFreeValues != fixedSizeStorage)
                return false;

            if (!isMerging)
                return true;

            // a merge has been started, values from lockFreeValues storage might be started to be copied:
            // preventing current thread from consuming CPU until merge is finished
            WaitWhileMerging();
            return false;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private void WaitWhileMerging()
        {
            var wait = new SpinWait();
            while (isMerging)
                wait.SpinOnce();
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        bool IDictionary<TKey, TValue>.Remove(TKey key) => TryRemove(key);

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => ((IDictionary<TKey, TValue>)this).Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => TryGetValue(item.Key, out TValue? value) && ComparerHelper<TValue>.EqualityComparer.Equals(value, item.Value);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
            int length = array.Length;
            if (length - arrayIndex < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

            EnsureMerged();
            FixedSizeStorage.InternalEnumerator enumerator = fixedSizeStorage.GetInternalEnumerator();
            while (enumerator.MoveNext())
            {
                // if elements were added concurrently
                if (arrayIndex == length)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                array[arrayIndex] = new KeyValuePair<TKey, TValue>(enumerator.Current.Key, enumerator.Current.Value);
                arrayIndex += 1;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
            => TryRemove(item.Key, item.Value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, false);

        void IDictionary.Add(object key, object? value)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            Throw.ThrowIfNullIsInvalid<TValue>(value);
            if (key is TKey k)
            {
                if (value is TValue v)
                    Add(k, v);
                else
                    Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeof(TValue)));
            }
            else
                Throw.ArgumentException(Argument.key, Res.IDictionaryNonGenericKeyTypeInvalid(key, typeof(TKey)));
        }

        bool IDictionary.Contains(object key)
            => key switch
            {
                TKey k => ContainsKey(k),
                null => Throw.ArgumentNullException<bool>(Argument.key),
                _ => false
            };

        void IDictionary.Remove(object key)
        {
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (key is TKey k)
                TryRemove(k);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if (index < 0 || index > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            int length = array.Length;
            if (length - index < Count)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (array.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            switch (array)
            {
                case KeyValuePair<TKey, TValue>[] keyValuePairs:
                    ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(keyValuePairs, index);
                    return;

                case DictionaryEntry[] dictionaryEntries:
                    EnsureMerged();
                    FixedSizeStorage.InternalEnumerator enumerator = fixedSizeStorage.GetInternalEnumerator();
                    while (enumerator.MoveNext())
                    {
                        // if elements were added concurrently
                        if (index == length)
                            Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                        dictionaryEntries[index] = new DictionaryEntry(enumerator.Current.Key, enumerator.Current.Value);
                        index += 1;
                    }

                    return;

                case object[] objectArray:
                    EnsureMerged();
                    enumerator = fixedSizeStorage.GetInternalEnumerator();
                    while (enumerator.MoveNext())
                    {
                        // if elements were added concurrently
                        if (index == length)
                            Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                        objectArray[index] = new KeyValuePair<TKey,TValue>(enumerator.Current.Key, enumerator.Current.Value);
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

            info.AddValue(nameof(initialLockingCapacity), initialLockingCapacity);
            info.AddValue(nameof(bitwiseAndHash), bitwiseAndHash);
            info.AddValue(nameof(comparer), comparer);
            info.AddValue(nameof(mergeInterval), mergeInterval);
            info.AddValue("items", ToArray());

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

            initialLockingCapacity = info.GetInt32(nameof(initialLockingCapacity));
            bitwiseAndHash = info.GetBoolean(nameof(bitwiseAndHash));
            comparer = (IEqualityComparer<TKey>?)info.GetValue(nameof(comparer), typeof(IEqualityComparer<TKey>));
            mergeInterval = info.GetInt64(nameof(mergeInterval));
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile - no problem, deserialization is a single threaded-access
            FixedSizeStorage.TryInitialize(info.GetValueOrDefault<KeyValuePair<TKey, TValue>[]>("items"), bitwiseAndHash, comparer, out fixedSizeStorage);
#pragma warning restore CS0420
            
            OnDeserialization(info);

            deserializationInfo = null;
        }

        #endregion

        #endregion
    }
}
