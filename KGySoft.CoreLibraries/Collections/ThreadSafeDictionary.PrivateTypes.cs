#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThreadSafeDictionary.PrivateTypes.cs
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
            private FixedSizeStorage.InternalEnumerator wrappedEnumerator;

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
                        wrappedEnumerator = owner.fixedSizeStorage.GetInternalEnumerator();
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
                int length = array.Length;
                if (length - arrayIndex < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

                owner.EnsureMerged();
                FixedSizeStorage.InternalEnumerator enumerator = owner.fixedSizeStorage.GetInternalEnumerator();
                while (enumerator.MoveNext())
                {
                    // if elements were added concurrently
                    if (arrayIndex == length)
                        Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                    array[arrayIndex] = enumerator.Current.Key;
                    arrayIndex += 1;
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                owner.EnsureMerged();
                FixedSizeStorage.InternalEnumerator enumerator = owner.fixedSizeStorage.GetInternalEnumerator();
                while (enumerator.MoveNext())
                    yield return enumerator.Current.Key;
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
                int length = array.Length;
                if (length - index < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                if (array.Rank != 1)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

                if (array is object[] objectArray)
                {
                    owner.EnsureMerged();
                    var enumerator = owner.fixedSizeStorage.GetInternalEnumerator();
                    while (enumerator.MoveNext())
                    {
                        // if elements were added concurrently
                        if (index == length)
                            Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                        objectArray[index] = enumerator.Current.Key;
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

            #region Public Methods

            public bool Contains(TValue item) => owner.ContainsValue(item);

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null!)
                    Throw.ArgumentNullException(Argument.array);
                if (arrayIndex < 0 || arrayIndex > array.Length)
                    Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
                int length = array.Length;
                if (length - arrayIndex < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);

                owner.EnsureMerged();
                FixedSizeStorage.InternalEnumerator enumerator = owner.fixedSizeStorage.GetInternalEnumerator();
                while (enumerator.MoveNext())
                {
                    // if elements were added concurrently
                    if (arrayIndex == length)
                        Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                    array[arrayIndex] = enumerator.Current.Value;
                    arrayIndex += 1;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                owner.EnsureMerged();
                FixedSizeStorage.InternalEnumerator enumerator = owner.fixedSizeStorage.GetInternalEnumerator();
                while (enumerator.MoveNext())
                    yield return enumerator.Current.Value;
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
                int length = array.Length;
                if (length - index < Count)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                if (array.Rank != 1)
                    Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

                if (array is object?[] objectArray)
                {
                    owner.EnsureMerged();
                    var enumerator = owner.fixedSizeStorage.GetInternalEnumerator();
                    while (enumerator.MoveNext())
                    {
                        // if elements were added concurrently
                        if (index == length)
                            Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
                        objectArray[index] = enumerator.Current.Value;
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
    }
}