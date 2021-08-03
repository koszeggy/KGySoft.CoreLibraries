#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockingCollection.cs
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
using System.Linq;
using System.Threading;

using KGySoft.Diagnostics;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Provides a simple wrapper for an <see cref="ICollection{T}"/> where all members are thread-safe.
    /// This only means that the inner state of the wrapped collection remains always consistent and not that all of the multi-threading concerns can be ignored.
    /// <br/>See the <strong>Remarks</strong> section for details and some examples.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <remarks>
    /// <para>Type safety means that all members of the underlying collection are accessed in a lock, which only provides that the collection remains consistent as long as it is accessed only by the members of this class.
    /// This does not solve every issue of multi-threading automatically. Consider the following example:
    /// <code lang="C#"><![CDATA[
    /// var asThreadSafe = new LockingCollection<MyClass>(myCollection);
    ///
    /// // Though both calls use locks it still can happen that two threads add the same item twice this way
    /// // because the lock is released between the two calls:
    /// if (!asThreadSafe.Contains(myItem))
    ///     asThreadSafe.Add(myItem);
    /// ]]></code></para>
    /// <para>For the situations above a lock can be requested also explicitly by the <see cref="Lock">Lock</see> method, which can be released by the <see cref="Unlock">Unlock</see> method.
    /// To release an explicitly requested lock the <see cref="Unlock">Unlock</see> method must be called the same times as the <see cref="Lock">Lock</see> method. The fixed version of the example above:
    /// <code lang="C#"><![CDATA[
    /// var asThreadSafe = new LockingCollection<MyClass>(myCollection);
    ///
    /// // This works well because the lock is not released between the two calls:
    /// asThreadSafe.Lock();
    /// try
    /// {
    ///     if (!asThreadSafe.Contains(myItem))
    ///         asThreadSafe.Add(myItem);
    /// }
    /// finally
    /// {
    ///     asThreadSafe.Unlock();
    /// }
    /// ]]></code></para>
    /// <para>To avoid confusion, the non-generic <see cref="ICollection"/> interface is not implemented by the <see cref="LockingCollection{T}"/> class because it uses a different aspect of synchronization.</para>
    /// <para>The <see cref="GetEnumerator">GetEnumerator</see> method creates a snapshot of the underlying collection so obtaining the enumerator has an O(n) cost on this class.</para>
    /// <para><note>Starting with .NET 4 a sort of concurrent collections appeared. While they provide good scalability for multiple concurrent readers by using separate locks for entries or for a set of entries,
    /// in many situations they perform worse than a simple locking collection, especially if the collection to lock uses a fast accessible storage (eg. an array) internally. It also may worth to mention that some members
    /// (such as the <c>Count</c> property) are surprisingly expensive operations on most concurrent collections as they traverse the inner storage and in the meantime they lock all entries while counting the elements.
    /// So it always depends on the concrete scenario whether a simple locking collection or a concurrent collection is more beneficial to use.</note></para>
    /// </remarks>
    /// <threadsafety instance="true"/>
    /// <seealso cref="ICollection{T}" />
    /// <seealso cref="LockingList{T}" />
    /// <seealso cref="LockingDictionary{TKey,TValue}" />
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; T = {typeof(" + nameof(T) + ").Name}")]
    [Serializable]
    public class LockingCollection<T> : ICollection<T>
    {
        #region Fields

        private readonly ICollection<T> collection;
        private readonly object syncRoot = new object();

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the number of elements contained in the <see cref="LockingCollection{T}" />.
        /// </summary>
        public int Count
        {
            get
            {
                lock (syncRoot)
                    return collection.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="LockingCollection{T}" /> is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                lock (syncRoot)
                    return collection.IsReadOnly;
            }
        }

        #endregion

        #region Internal Properties

        internal ICollection<T> Collection => collection;

        #endregion

        #region Private Protected Properties

        /// <summary>
        /// Gets the inner collection.
        /// </summary>
        private protected ICollection<T> InnerCollection => collection;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LockingCollection{T}"/> class with a <see cref="HashSet{T}"/> inside.
        /// </summary>
        public LockingCollection() : this(new HashSet<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockingCollection{T}"/> class.
        /// </summary>
        /// <param name="collection">The collection to create a thread-safe wrapper for.</param>
        public LockingCollection(ICollection<T> collection) => this.collection = collection;

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Locks the access of the underlying collection from other threads until <see cref="Unlock">Unlock</see> is called as many times as this method was called. Needed to be called if
        /// multiple calls to the wrapped collection have to be combined without releasing the lock between each calls.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="LockingCollection{T}"/> class for details and some examples.
        /// </summary>
        public void Lock() => Monitor.Enter(syncRoot);

        /// <summary>
        /// When called as many times as <see cref="Lock">Lock</see> was called previously, then unlocks the access of the underlying collection so other threads also can access it.
        /// </summary>
        public void Unlock() => Monitor.Exit(syncRoot);

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        /// <remarks>
        /// <para>The enumeration represents a moment-in-time snapshot of the contents of the <see cref="LockingCollection{T}"/>. It does not reflect any updates to the collection after <see cref="GetEnumerator">GetEnumerator</see> was called.
        /// The enumerator is safe to use concurrently with reads from and writes to the collection.</para>
        /// <para>This method has an O(n) cost where n is the number of elements in the collection.</para>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public IEnumerator<T> GetEnumerator()
        {
            // returning an array enumerator because it is a reference type and performs better than list enumerator when boxed.
            lock (syncRoot)
                return ((IList<T>)collection.ToArray()).GetEnumerator();
        }

        /// <summary>
        /// Adds an item to the <see cref="LockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="LockingCollection{T}"/>.</param>
        public void Add(T item)
        {
            lock (syncRoot)
                collection.Add(item);
        }

        /// <summary>
        /// Removes all items from the <see cref="LockingCollection{T}"/>.
        /// </summary>
        public void Clear()
        {
            lock (syncRoot)
                collection.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="LockingCollection{T}" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="LockingCollection{T}" />.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item" /> is found in the <see cref="LockingCollection{T}" />; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(T item)
        {
            lock (syncRoot)
                return collection.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="LockingCollection{T}"/> to an <see cref="Array"/>, starting at a particular <paramref name="arrayIndex"/>.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array" /> that is the destination of the elements copied from <see cref="LockingCollection{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (syncRoot)
                collection.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="LockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="LockingCollection{T}"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item" /> was successfully removed from the <see cref="LockingCollection{T}" />; otherwise, <see langword="false" />.
        /// This method also returns <see langword="false" />&#160;if <paramref name="item" /> is not found in the original <see cref="LockingCollection{T}"/>.
        /// </returns>
        public bool Remove(T item)
        {
            lock (syncRoot)
                return collection.Remove(item);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}
