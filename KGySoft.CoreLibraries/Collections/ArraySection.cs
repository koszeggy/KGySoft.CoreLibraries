#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySection.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if !(NETFRAMEWORK || NETSTANDARD2_0)
using System.Buffers;
#endif

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a one dimensional array or a section of an array.
    /// This type is very similar to <see cref="ArraySegment{T}"/>/<see cref="Memory{T}"/> types but can be used on every platform in the same way
    /// and it is faster than <see cref="Memory{T}"/> in most cases. Depending on the used platform it supports <see cref="ArrayPool{T}"/> allocation.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <typeparam name="T">The type of the element in the collection.</typeparam>
    /// <remarks>
    /// <para>The <see cref="ArraySection{T}"/> type is similar to the combination of the <see cref="Memory{T}"/> type and the .NET Core version of the <see cref="ArraySegment{T}"/> type.</para>
    /// <para>In .NET Core 3.0/.NET Standard 2.1 and above an <see cref="ArraySection{T}"/> instance can be easily turned to a <see cref="Span{T}"/> instance (either by cast or by the <see cref="AsSpan"/> property),
    /// which is much faster than using the <see cref="Memory{T}.Span"/> property of a <see cref="Memory{T}"/> instance.</para>
    /// <para>If an <see cref="ArraySection{T}"/> is created by the <see cref="ArraySection{T}(int)">constructor with a specified size</see>, then depending on the size and the
    /// current platform the underlying array might be obtained by using the <see cref="ArrayPool{T}"/>.
    /// <note>An <see cref="ArraySection{T}"/> instance that was instantiated by the <see cref="ArraySection{T}(int)">self allocating constructor</see> must be released by calling the <see cref="Release">Release</see>
    /// method when it is not used anymore. The <see cref="ArraySection{T}"/> type does not implement <see cref="IDisposable"/> because releasing is required only in such case
    /// but not calling it when it is needed may lead to decreased application performance.</note></para>
    /// <para>As <see cref="ArraySection{T}"/> is a non-<c>readonly</c>&#160;<see langword="struct"/>&#160;it is not recommended to use it as a <c>readonly</c> field; otherwise,
    /// accessing its members would make the compiler to create a defensive copy, which leads to a slight performance degradation.</para>
    /// </remarks>
    [Serializable]
    public struct ArraySection<T> : IList<T>, IList
#if !(NET35 || NET40)
        , IReadOnlyList<T>
#endif
    {
        #region Enumerator Struct

        public struct Enumerator : IEnumerator<T>
        {
            #region Fields

            private readonly T[] array;
            private readonly int start;
            private readonly int end;

            private int index;

            #endregion

            #region Properties

            #region Public Properties

            public T Current => index >= start && index < end ? array[index] : default;

            #endregion

            #region Explicitly Implemented Interface Properties

            object IEnumerator.Current
            {
                get
                {
                    if (index < start || index >= end)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                    return Current;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal Enumerator(ArraySection<T> arraySection)
            {
                Debug.Assert(arraySection.array != null, "null section is not expected here");
                array = arraySection.array;
                start = arraySection.offset;
                end = arraySection.offset + arraySection.length;
                index = start - 1;
            }

            #endregion

            #region Methods

            #region Public Methods

            public bool MoveNext()
            {
                if (index >= end)
                    return false;

                index += 1;
                return index < end;
            }

            public void Reset() => index = start - 1;

            #endregion

            #region Explicitly Implemented Interface Methods

            void IDisposable.Dispose()
            {
            }

            #endregion

            #endregion
        }

        #endregion

        #region Fields

        #region Static Fields

#if !(NETFRAMEWORK || NETSTANDARD2_0)
        private static readonly int poolingThreshold = Math.Min(2, 1024 / Reflector.SizeOf<T>());
#endif

        #endregion

        #region Instance Fields

        private readonly T[] array;
        private readonly int offset;
        private readonly int length;
#if !(NETFRAMEWORK || NETSTANDARD2_0)
        private readonly bool poolArray;
#endif

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Static Properties

        public static ArraySection<T> Null => default;

        public static ArraySection<T> Empty => new ArraySection<T>(Reflector.EmptyArray<T>());

        #endregion

        #region Instance Properties

        #region Public Properties

        public int Count => length;

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        public Memory<T> AsMemory => new Memory<T>(array, offset, length);

        public Span<T> AsSpan => new Span<T>(array, offset, length);
#endif

        public ArraySegment<T> AsSegment => new ArraySegment<T>(array, offset, length);

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<T>.IsReadOnly => true;

        // It actually should use a private field but as we never lock on this we will never cause a deadlock with this.
        object ICollection.SyncRoot => array?.SyncRoot ?? Throw.InvalidOperationException<object>(Res.ArraySectionNull); // would be better to allocate a new field for this but 
        bool ICollection.IsSynchronized => false;

        bool IList.IsReadOnly => false;
        bool IList.IsFixedSize => true;

        #endregion

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers
        
        public T this[int index]
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)length)
                    Throw.ArgumentOutOfRangeException(Argument.index);
                return GetItemInternal(index);
            }
            [MethodImpl(MethodImpl.AggressiveInlining)]
            set
            {
                if ((uint)index >= (uint)length)
                    Throw.ArgumentOutOfRangeException(Argument.index);
                array[offset + index] = value;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object IList.this[int index]
        {
            get => this[index];
            set
            {
                Throw.ThrowIfNullIsInvalid<T>(value);
                try
                {
                    this[index] = (T)value;
                }
                catch (InvalidCastException)
                {
                    Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeof(T)));
                }
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct using an internally allocated buffer.
        /// <br/>When using this overload, the returned <see cref="ArraySection{T}"/> instance must be released
        /// by the <see cref="Release">Release</see> method if it is not used anymore.
        /// </summary>
        /// <param name="length">The length of the <see cref="ArraySection{T}"/> to be created.</param>
        public ArraySection(int length)
        {
            if (length < 0)
                Throw.ArgumentOutOfRangeException(Argument.length);
            offset = 0;
            this.length = length;

#if !(NETFRAMEWORK || NETSTANDARD2_0)
            poolArray = length >= poolingThreshold;
            if (poolArray)
            {
                array = ArrayPool<T>.Shared.Rent(length);
                return;
            }
#endif

            array = new T[length];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct from the specified <paramref name="array"/>.
        /// </summary>
        /// <param name="array">The array to initialize the new <see cref="ArraySection{T}"/> instance from.</param>
        public ArraySection(T[] array) : this(array, 0, array?.Length ?? 0)
        {
        }

        public ArraySection(T[] array, int offset, int length)
        {
            if (array == null)
                Throw.ArgumentNullException(Argument.array);
            if ((uint)offset > (uint)array.Length)
                Throw.ArgumentOutOfRangeException(Argument.offset);
            if ((uint)length > (uint)(array.Length - offset))
                Throw.ArgumentOutOfRangeException(Argument.offset);
            this.array = array;
            this.offset = offset;
            this.length = length;
#if !(NETFRAMEWORK || NETSTANDARD2_0)
            poolArray = false;
#endif
        }

        #endregion

        #region Methods

        #region Static Methods

        private static bool CanAccept(object value) => value is T || value == null && default(T) == null;

        #endregion

        #region Instance Methods

        #region Public Methods

        public ArraySection<T> Slice(int start) => new ArraySection<T>(array, offset + start, length - start);

        public ArraySection<T> Slice(int start, int length) => new ArraySection<T>(array, offset + start, length);

        public ref T GetPinnableReference() => ref GetElementRef(0);

        public T[] ToArray()
        {
            if (length == 0)
                return array; // it can be even null
            T[] result = new T[length];
            array.CopyElements(offset, result, 0, length);
            return result;
        }

        public ref T GetElementRef(int index)
        {
            if ((uint)index >= (uint)length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            return ref array[offset + index];
        }

        public int IndexOf(T value)
        {
            if (length == 0)
                return -1;
            int result = Array.IndexOf(array, value, offset, length);
            return result < 0 ? result : result - offset;
        }

        public bool Contains(T value) => IndexOf(value) >= 0;

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "False alarm, array CAN be null so it must be checked")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "False alarm, array CAN be null so the Throw is reachable")]
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (this.array == null)
                Throw.InvalidOperationException(Res.ArraySectionNull);
            if (array == null)
                Throw.ArgumentNullException(Argument.array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.arrayIndex);
            if (array.Length - arrayIndex < length)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            this.array.CopyElements(offset, array, arrayIndex, length);
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Releases the underlying array. If this <see cref="ArraySection{T}"/> instance was instantiated by the <see cref="ArraySection{T}(int)">self allocating constructor</see>,
        /// then this method must be called when the <see cref="ArraySection{T}"/> is not used anymore.
        /// On platforms that do not support the <see cref="ArrayPool{T}"/> class this method simply nullifies the underlying array.
        /// </summary>
        public void Release()
        {
#if !(NETFRAMEWORK || NETSTANDARD2_0)
            if (array != null && poolArray)
                ArrayPool<T>.Shared.Return(array);
#endif
            this = Null;
        }

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal T GetItemInternal(int index) => array[offset + index];

        #endregion

        #region Explicitly Implemented Interface Methods

        void IList<T>.Insert(int index, T item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void IList<T>.RemoveAt(int index) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

        void ICollection<T>.Add(T item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void ICollection<T>.Clear() => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        bool ICollection<T>.Remove(T item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IList.IndexOf(object value) => CanAccept(value) ? IndexOf((T)value) : -1;
        bool IList.Contains(object value) => CanAccept(value) && Contains((T)value);

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                Throw.ArgumentNullException(Argument.array);

            if (array is T[] typedArray)
            {
                CopyTo(typedArray, index);
                return;
            }

            if (index < 0 || index > array.Length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (array.Length - index < length)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (array.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            if (array is object[] objectArray)
            {
                for (int i = 0; i < length; i++)
                {
                    objectArray[index] = GetItemInternal(i);
                    index += 1;
                }
            }

            Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
        }

        int IList.Add(object item) => Throw.NotSupportedException<int>(Res.ICollectionReadOnlyModifyNotSupported);
        void IList.Insert(int index, object item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void IList.Remove(object item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void IList.RemoveAt(int index) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void IList.Clear() => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

        #endregion

        #endregion

        #endregion
    }
}
