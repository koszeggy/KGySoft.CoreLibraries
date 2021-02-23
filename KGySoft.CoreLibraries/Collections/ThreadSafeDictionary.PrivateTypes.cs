#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeDictionary.PrivateTypes.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
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

#endregion

namespace KGySoft.Collections
{
    partial class ThreadSafeDictionary<TKey, TValue>
    {
        #region Enumerator Class

        private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            #region Nested Types

            private enum State
            {
                NotStarted,
                Enumerating,
                Finished
            }

            #endregion

            #region Fields

            private readonly ThreadSafeDictionary<TKey, TValue> owner;
            private readonly bool isGeneric;

            private State state;
            private FixedSizeStorage.CustomEnumerator wrappedEnumerator;

            #endregion

            #region Properties

            #region Public Properties

            public KeyValuePair<TKey, TValue> Current
                // wrappedEnumerator.Current is a tuple field so no double copying occurs when accessing its Key and Value
                => new KeyValuePair<TKey, TValue>(wrappedEnumerator.Current.Key, wrappedEnumerator.Current.Value);

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    // the non-generic IEnumerable.Current should throw an exception when used improperly
                    if (state != State.Enumerating)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return isGeneric ? Current : new DictionaryEntry(wrappedEnumerator.Current.Key, wrappedEnumerator.Current.Value);
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (state != State.Enumerating)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return new DictionaryEntry(wrappedEnumerator.Current.Key, wrappedEnumerator.Current.Value);
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (state != State.Enumerating)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return wrappedEnumerator.Current.Key;
                }
            }

            object? IDictionaryEnumerator.Value
            {
                get
                {
                    if (state != State.Enumerating)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return wrappedEnumerator.Current.Value;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(ThreadSafeDictionary<TKey, TValue> owner, bool isGeneric)
            {
                this.owner = owner;
                this.isGeneric = isGeneric;
            }

            #endregion

            #region Methods

            public bool MoveNext()
            {
                switch (state)
                {
                    case State.NotStarted:
                        owner.EnsureMerged();
                        wrappedEnumerator = owner.fixedSizeStorage.GetEnumerator();
                        state = State.Enumerating;
                        goto case State.Enumerating;

                    case State.Enumerating:
                        if (wrappedEnumerator.MoveNext())
                            return true;

                        wrappedEnumerator = default;
                        state = State.Finished;
                        return false;

                    default:
                        return false;
                }
            }

            public void Reset()
            {
                wrappedEnumerator = default;
                state = State.NotStarted;
            }

            public void Dispose()
            {
            }

            #endregion
        }

        #endregion

        #region KeysCollection Class
        
        private sealed class KeysCollection : ICollection<TKey>, ICollection
        {
            #region Fields
            
            private readonly ThreadSafeDictionary<TKey, TValue> owner;

            #endregion

            #region Properties

            #region Public Properties

            public int Count => owner.Count;
            public bool IsReadOnly => true;

            #endregion

            #region Explicitly Implemented Interface Properties

            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => Throw.NotSupportedException<object>();

            #endregion

            #endregion

            #region Constructors

            internal KeysCollection(ThreadSafeDictionary<TKey, TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            void ICollection<TKey>.Add(TKey item)
            {
                throw new System.NotImplementedException();
            }

            void ICollection<TKey>.Clear()
            {
                throw new System.NotImplementedException();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                throw new System.NotImplementedException();
            }

            void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
            {
                throw new System.NotImplementedException();
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                throw new System.NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new System.NotImplementedException();
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new System.NotImplementedException();
            }

            void ICollection.CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #endregion

        #region ValuesCollection Class

        private sealed class ValuesCollection : ICollection<TValue>, ICollection
        {
            #region Fields

            private readonly ThreadSafeDictionary<TKey, TValue> owner;

            #endregion

            #region Properties

            #region Public Properties

            public int Count => owner.Count;
            public bool IsReadOnly => true;

            #endregion

            #region Explicitly Implemented Interface Properties

            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => Throw.NotSupportedException<object>();

            #endregion

            #endregion

            #region Constructors

            internal ValuesCollection(ThreadSafeDictionary<TKey, TValue> owner) => this.owner = owner;

            #endregion

            #region Methods

            void ICollection<TValue>.Add(TValue item)
            {
                throw new System.NotImplementedException();
            }

            void ICollection<TValue>.Clear()
            {
                throw new System.NotImplementedException();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                throw new System.NotImplementedException();
            }

            void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex)
            {
                throw new System.NotImplementedException();
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                throw new System.NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new System.NotImplementedException();
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new System.NotImplementedException();
            }

            void ICollection.CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #endregion

    }
}