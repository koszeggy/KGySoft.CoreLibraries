#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LockingList.cs
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
using KGySoft.Diagnostics;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Provides a simple wrapper for an <see cref="IList{T}"/> where all members are thread-safe.
    /// This only means that the inner state of the wrapped list remains always consistent and not that all of the multi-threading concerns can be ignored.
    /// <br/>See the <strong>Remarks</strong> section for details and some examples.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    /// <remarks>
    /// <para>Type safety means that all members of the underlying collection are accessed in a lock, which only provides that the collection remains consistent as long as it is accessed only by the members of this class.
    /// This does not solve every issue of multi-threading automatically. Consider the following example:
    /// <code lang="C#"><![CDATA[
    /// var asThreadSafe = new LockingList<MyClass>(myList);
    ///
    /// // Though both calls use locks it still can happen that two threads tries to remove the only element
    /// // because the lock is released between the two calls:
    /// if (asThreadSafe.Count > 0)
    ///     asThreadSafe.RemoveAt(0);
    /// ]]></code></para>
    /// <para>For the situations above a lock can be requested also explicitly by the <see cref="LockingCollection{T}.Lock">Lock</see> method, which can be released by the <see cref="LockingCollection{T}.Unlock">Unlock</see> method.
    /// To release an explicitly requested lock the <see cref="LockingCollection{T}.Unlock">Unlock</see> method must be called the same times as the <see cref="LockingCollection{T}.Lock">Lock</see> method. The fixed version of the example above:
    /// <code lang="C#"><![CDATA[
    /// var asThreadSafe = new LockingList<MyClass>(myList);
    ///
    /// // This works well because the lock is not released between the two calls:
    /// asThreadSafe.Lock();
    /// try
    /// {
    ///     if (asThreadSafe.Count > 0)
    ///         asThreadSafe.RemoveAt(0);
    /// }
    /// finally
    /// {
    ///     asThreadSafe.Unlock();
    /// }
    /// ]]></code></para>
    /// <para>To avoid confusion, the non-generic <see cref="IList"/> interface is not implemented by the <see cref="LockingList{T}"/> class because it uses a different aspect of synchronization.</para>
    /// <para>The <see cref="LockingCollection{T}.GetEnumerator">GetEnumerator</see> method creates a snapshot of the underlying list so obtaining the enumerator has an O(n) cost on this class.</para>
    /// <para><note>Starting with .NET 4 a sort of concurrent collections appeared. While they provide good scalability for multiple concurrent readers by using separate locks for entries or for a set of entries,
    /// in many situations they perform worse than a simple locking collection, especially if the collection to lock uses a fast accessible storage (eg. an array) internally. It also may worth to mention that some members
    /// (such as the <c>Count</c> property) are surprisingly expensive operations on most concurrent collections as they traverse the inner storage and in the meantime they lock all entries while counting the elements.
    /// So it always depends on the concrete scenario whether a simple locking collection or a concurrent collection is more beneficial to use.</note></para>
    /// </remarks>
    /// <threadsafety instance="true"/>
    /// <seealso cref="IList{T}" />
    /// <seealso cref="LockingCollection{T}" />
    /// <seealso cref="LockingDictionary{TKey,TValue}" />
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}; T = {typeof(" + nameof(T) + ").Name}")]
    [Serializable]
    public class LockingList<T> : LockingCollection<T>, IList<T>
    {
        #region Indexers

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                Lock();
                try
                {
                    return ((IList<T>)InnerCollection)[index];
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
                    ((IList<T>)InnerCollection)[index] = value;
                }
                finally
                {
                    Unlock();
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LockingList{T}"/> with a <see cref="List{T}"/> inside.
        /// </summary>
        public LockingList() : this(new List<T>(1))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockingList{T}"/> class.
        /// </summary>
        /// <param name="list">The list to create a thread-safe wrapper for.</param>
        public LockingList(IList<T> list) : base(list)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines the index of a specific item in the <see cref="LockingList{T}" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="LockingList{T}" />.</param>
        /// <returns>
        /// The index of <paramref name="item" /> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            Lock();
            try
            {
                return ((IList<T>)InnerCollection).IndexOf(item);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Inserts an item to the <see cref="LockingList{T}" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="LockingList{T}" />.</param>
        public void Insert(int index, T item)
        {
            Lock();
            try
            {
                ((IList<T>)InnerCollection).Insert(index, item);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Removes the <see cref="LockingList{T}" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            Lock();
            try
            {
                ((IList<T>)InnerCollection).RemoveAt(index);
            }
            finally
            {
                Unlock();
            }
        }

        #endregion
    }
}
