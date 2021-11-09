#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockingDictionary.cs
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
using System.Linq;

using KGySoft.Diagnostics;

#endregion

#region Suppressions

#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#endif

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Provides a simple wrapper for an <see cref="IDictionary{TKey,TValue}"/> where all members are thread-safe.
    /// This only means that the inner state of the wrapped dictionary remains always consistent and not that all of the multi-threading concerns can be ignored.
    /// <br/>See the <strong>Remarks</strong> section for details and some examples.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// <note>Use this class only if you want to wrap a generic <see cref="IDictionary{TKey,TValue}"/> instance to make it thread-safe.
    /// If you want to use a thread-safe dictionary optimized for concurrent operations consider to use the <see cref="ThreadSafeDictionary{TKey,TValue}"/> class instead.</note>
    /// <para>Type safety means that all members of the underlying collection are accessed in a lock, which only provides that the collection remains consistent as long as it is accessed only by the members of this class.
    /// This does not solve every issue of multi-threading automatically. Consider the following example:
    /// <code lang="C#"><![CDATA[
    /// var asThreadSafe = new LockingDictionary<MyKey, MyValue>(myDictionary);
    ///
    /// // Though both calls use locks it still can happen that two threads try to add the same key twice this way
    /// // because the lock is released between the two calls:
    /// if (!asThreadSafe.ContainsKey(myKey))
    ///     asThreadSafe.Add(myKey, myValue);
    /// ]]></code></para>
    /// <para>For the situations above a lock can be requested also explicitly by the <see cref="LockingCollection{T}.Lock">Lock</see> method, which can be released by the <see cref="LockingCollection{T}.Unlock">Unlock</see> method.
    /// To release an explicitly requested lock the <see cref="LockingCollection{T}.Unlock">Unlock</see> method must be called the same times as the <see cref="LockingCollection{T}.Lock">Lock</see> method. The fixed version of the example above:
    /// <code lang="C#"><![CDATA[
    /// var asThreadSafe = new LockingDictionary<MyClass>(myDictionary);
    ///
    /// // This works well because the lock is not released between the two calls:
    /// asThreadSafe.Lock();
    /// try
    /// {
    ///     if (!asThreadSafe.ContainsKey(myKey))
    ///         asThreadSafe.Add(myKey, myValue);
    /// }
    /// finally
    /// {
    ///     asThreadSafe.Unlock();
    /// }
    /// ]]></code></para>
    /// <para>To avoid confusion, the non-generic <see cref="IDictionary"/> interface is not implemented by the <see cref="LockingDictionary{TKey,TValue}"/> class because it uses a different aspect of synchronization.</para>
    /// <para>The <see cref="LockingCollection{T}.GetEnumerator">GetEnumerator</see> method and <see cref="Keys"/> and <see cref="Values"/> properties create a snapshot of the underlying collections so obtaining
    /// these members have an O(n) cost on this class.</para>
    /// <para><note>Starting with .NET 4 a sort of concurrent collections appeared. While they provide good scalability for multiple concurrent readers by using separate locks for entries or for a set of entries,
    /// in many situations they perform worse than a simple locking collection, especially if the collection to lock uses a fast accessible storage (eg. an array) internally. It also may worth to mention that some members
    /// (such as the <c>Count</c> property) are surprisingly expensive operations on most concurrent collections as they traverse the inner storage and in the meantime they lock all entries while counting the elements.
    /// So it always depends on the concrete scenario whether a simple locking collection or a concurrent collection is more beneficial to use.</note>
    /// <note type="tip"><list type="bullet">
    /// <item>To use a thread-safe dictionary without wrapping any <see cref="IDictionary{TKey,TValue}"/> instance consider to use the <see cref="ThreadSafeDictionary{TKey,TValue}"/> class instead.</item>
    /// <item>For a <see cref="Cache{TKey,TValue}"/> use this class only if you want a thread-safe wrapper for all <see cref="IDictionary{TKey,TValue}"/> members and if it is not a problem if the cache remains locked
    /// during the invocation of the item loader delegate passed to the appropriate <see cref="M:KGySoft.Collections.Cache`2.#ctor(System.Func{`0,`1},System.Int32,System.Collections.Generic.IEqualityComparer{`0})">constructor</see>.
    /// Otherwise, it may worth to use an <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instead, which can be obtained by the <see cref="Cache{TKey,TValue}.GetThreadSafeAccessor">GetThreadSafeAccessor</see> method.</item>
    /// <item>To create a thread-safe <see cref="IThreadSafeCacheAccessor{TKey,TValue}"/> instance that fits the best for your needs use the members of the <see cref="ThreadSafeCacheFactory"/> class.</item>
    /// </list></note>
    /// </para>
    /// </remarks>
    /// <threadsafety instance="true"/>
    /// <seealso cref="IDictionary{TKey,TValue}" />
    /// <seealso cref="LockingCollection{T}" />
    /// <seealso cref="LockingList{T}" />
    /// <seealso cref="ThreadSafeCacheFactory"/>
    /// <seealso cref="ThreadSafeDictionary{TKey,TValue}"/>
    [Serializable]
    [DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; TKey = {typeof(" + nameof(TKey) + ").Name}; TValue = {typeof(" + nameof(TValue) + ").Name}")]
    public class LockingDictionary<TKey, TValue> : LockingCollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
        where TKey : notnull // actually depends on the wrapped implementation but is definitely true for the default constructor
    {
        #region Properties and Indexers

        #region Properties

        /// <summary>
        /// Gets an <see cref="ICollection{T}" /> containing the keys of the <see cref="LockingDictionary{TKey,TValue}" />.
        /// </summary>
        /// <remarks>
        /// <para>The returned collection represents a moment-in-time snapshot of the keys of the <see cref="LockingDictionary{TKey,TValue}"/>. It does not reflect any updates to the dictionary after <see cref="Keys"/> were obtained.
        /// The collection is safe to use concurrently with reads from and writes to the dictionary.</para>
        /// <para>This property has an O(n) cost where n is the number of elements in the dictionary.</para>
        /// <note>The enumerator of the returned collection supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TKey> Keys
        {
            get
            {
                Lock();
                try
                {
                    // returning an array because it is read-only as an ICollection<T>
                    return ((IDictionary<TKey, TValue>)InnerCollection).Keys.ToArray();
                }
                finally
                {
                    Unlock();
                }
            }
        }

        /// <summary>
        /// Gets an <see cref="ICollection{T}" /> containing the values of the <see cref="LockingDictionary{TKey,TValue}" />.
        /// </summary>
        /// <remarks>
        /// <para>The returned collection represents a moment-in-time snapshot of the values of the <see cref="LockingDictionary{TKey,TValue}"/>. It does not reflect any updates to the dictionary after <see cref="Keys"/> were obtained.
        /// The collection is safe to use concurrently with reads from and writes to the dictionary.</para>
        /// <para>This property has an O(n) cost where n is the number of elements in the dictionary.</para>
        /// <note>The enumerator of the returned collection supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public ICollection<TValue> Values
        {
            get
            {
                Lock();
                try
                {
                    // returning an array because it is read-only as an ICollection<T>
                    return ((IDictionary<TKey, TValue>)InnerCollection).Values.ToArray();
                }
                finally
                {
                    Unlock();
                }
            }
        }

        #endregion

        #region Indexers

        /// <summary>
        /// Gets or sets the element with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the element to get or set.</param>
        /// <returns>The element with the specified key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                Lock();
                try
                {
                    return ((IDictionary<TKey, TValue>)InnerCollection)[key];
                }
                finally
                {
                    Unlock();
                }
            }
            set
            {
                Lock();
                try
                {
                    ((IDictionary<TKey, TValue>)InnerCollection)[key] = value;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LockingDictionary{TKey, TValue}"/> class with a <see cref="Dictionary{TKey,TValue}"/> inside.
        /// </summary>
        public LockingDictionary() : this(new Dictionary<TKey, TValue>(1))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockingDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary to create a thread-safe wrapper for.</param>
        public LockingDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the <see cref="LockingDictionary{TKey,TValue}" /> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="LockingDictionary{TKey,TValue}" />.</param>
        /// <returns><see langword="true" />&#160;if the <see cref="LockingDictionary{TKey,TValue}" /> contains an element with the key; otherwise, <see langword="false" />.</returns>
        public bool ContainsKey(TKey key)
        {
            Lock();
            try
            {
                return ((IDictionary<TKey, TValue>)InnerCollection).ContainsKey(key);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="LockingDictionary{TKey,TValue}" />.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public void Add(TKey key, TValue value)
        {
            Lock();
            try
            {
                ((IDictionary<TKey, TValue>)InnerCollection).Add(key, value);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="LockingDictionary{TKey,TValue}" />.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><see langword="true" />&#160;if the element is successfully removed; otherwise, <see langword="false" />.
        /// This method also returns <see langword="false" />&#160;if <paramref name="key" /> was not found in the original <see cref="LockingDictionary{TKey,TValue}" />.
        /// </returns>
        public bool Remove(TKey key)
        {
            Lock();
            try
            {
                return ((IDictionary<TKey, TValue>)InnerCollection).Remove(key);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Gets the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" />&#160;if the <see cref="LockingDictionary{TKey,TValue}" /> contains an element with the specified key; otherwise, <see langword="false" />.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)]out TValue value)
        {
            Lock();
            try
            {
                return ((IDictionary<TKey, TValue>)InnerCollection).TryGetValue(key, out value);
            }
            finally
            {
                Unlock();
            }
        }

        #endregion
    }
}
