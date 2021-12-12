#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySection.cs
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
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
using System.Buffers;
#endif

using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

#endregion

#region Suppressions

#if !(NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif
#if !NET5_0_OR_GREATER
// ReSharper disable UnusedMember.Local - ArraySectionDebugView.Items
#endif


#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a one dimensional array or a section of an array.
    /// This type is very similar to <see cref="ArraySegment{T}"/>/<see cref="Memory{T}"><![CDATA[Memory<T>]]></see> types but can be used on every platform in the same way
    /// and it is faster than <see cref="Memory{T}"><![CDATA[Memory<T>]]></see> in most cases. Depending on the used platform it supports <see cref="ArrayPool{T}"/> allocation.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <remarks>
    /// <para>The <see cref="ArraySection{T}"/> type is similar to the combination of the <see cref="Memory{T}"/> type and the .NET Core version of the <see cref="ArraySegment{T}"/> type.</para>
    /// <para>In .NET Core 2.1/.NET Standard 2.1 and above an <see cref="ArraySection{T}"/> instance can be easily turned to a <see cref="Span{T}"/> instance (either by cast or by the <see cref="AsSpan"/> property),
    /// which is much faster than using the <see cref="Memory{T}.Span">Span</see> property of a <see cref="Memory{T}"/> instance.</para>
    /// <para>If an <see cref="ArraySection{T}"/> is created by the <see cref="ArraySection{T}(int,bool)">constructor with a specified size</see>, then depending on the size and the
    /// current platform the underlying array might be obtained by using the <see cref="ArrayPool{T}"/>. 
    /// <note>An <see cref="ArraySection{T}"/> instance that was instantiated by the <see cref="ArraySection{T}(int,bool)">self allocating constructor</see> must be released by calling the <see cref="Release">Release</see>
    /// method when it is not used anymore. The <see cref="ArraySection{T}"/> type does not implement <see cref="IDisposable"/> because releasing is not required when <see cref="ArraySection{T}"/> is created
    /// from an existing array but not calling it when it would be needed may lead to decreased application performance.</note></para>
    /// <para>Though <see cref="ArraySection{T}"/> is practically immutable (has only <see langword="readonly"/>&#160;fields) it is not marked as <c>readonly</c>,
    /// which is needed for the <see cref="Release">Release</see> method to work properly. As <see cref="ArraySection{T}"/> is a
    /// non-<c>readonly</c>&#160;<see langword="struct"/>&#160;it is not recommended to use it as a <c>readonly</c> field; otherwise,
    /// accessing its members would make the pre-C# 8.0 compilers to create defensive copies, which leads to a slight performance degradation.</para>
    /// </remarks>
    [Serializable]
    [DebuggerTypeProxy(typeof(ArraySection<>.ArraySectionDebugView))]
    [DebuggerDisplay("{typeof(" + nameof(T) + ")." + nameof(Type.Name) + ",nq}[{" + nameof(Length) + "}]")]
    public struct ArraySection<T> : IList<T>, IList, IEquatable<ArraySection<T>>, ISerializable
#if !(NET35 || NET40)
        , IReadOnlyList<T>
#endif
    {
        #region Nested Types

        private struct ArraySectionDebugView // strange error in VS2019: this must be a struct; otherwise, debug values will be completely misinterpreted
        {
            #region Fields

            [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "ArraySection is not readonly so it would generate defensive copies")]
            private ArraySection<T> array;

            #endregion

            #region Properties

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[]? Items => array.ToArray();

            #endregion

            #region Constructors

            internal ArraySectionDebugView(ArraySection<T> array) => this.array = array;

            #endregion
        }

        #endregion

        #region Fields

        #region Static Fields

        #region Public Fields

        /// <summary>
        /// Represents the <see langword="null"/>&#160;<see cref="ArraySection{T}"/>. This field is read-only.
        /// </summary>
        public static readonly ArraySection<T> Null = default;

        /// <summary>
        /// Represents the empty <see cref="ArraySection{T}"/>. This field is read-only.
        /// </summary>
        public static readonly ArraySection<T> Empty = new ArraySection<T>(Reflector<T>.EmptyArray);

        #endregion

        #region Private Fields

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        private static readonly int poolingThreshold = Math.Max(2, 1024 / Reflector<T>.SizeOf);
#endif

        #endregion

        #endregion

        #region Instance Fields

        private readonly T[]? array;
        private readonly int offset;
        private readonly int length;
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        private readonly bool poolArray;
#endif

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the underlying array of this <see cref="ArraySection{T}"/>.
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Intended, same as for ArraySegment")]
        public readonly T[]? UnderlyingArray => array;

        /// <summary>
        /// Gets the offset, which denotes the start position of this <see cref="ArraySection{T}"/> within the <see cref="UnderlyingArray"/>.
        /// </summary>
        public readonly int Offset => offset;

        /// <summary>
        /// Gets the number of elements in this <see cref="ArraySection{T}"/>.
        /// </summary>
        public readonly int Length => length;

        /// <summary>
        /// Gets whether this <see cref="ArraySection{T}"/> instance represents a <see langword="null"/>&#160;array.
        /// <br/>Please note that the <see cref="ToArray">ToArray</see> method returns <see langword="null"/>&#160;when this property returns <see langword="true"/>.
        /// </summary>
        public readonly bool IsNull => array == null;

        /// <summary>
        /// Gets whether this <see cref="ArraySection{T}"/> instance represents an empty array section or a <see langword="null"/>&#160;array.
        /// </summary>
        public readonly bool IsNullOrEmpty => length == 0;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns the current <see cref="ArraySection{T}"/> instance as a <see cref="Memory{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public readonly Memory<T> AsMemory => new Memory<T>(array, offset, length);

        /// <summary>
        /// Returns the current <see cref="ArraySection{T}"/> instance as a <see cref="Span{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public readonly Span<T> AsSpan => new Span<T>(array, offset, length);
#endif

        /// <summary>
        /// Returns the current <see cref="ArraySection{T}"/> instance as an <see cref="ArraySegment{T}"/>.
        /// </summary>
        public readonly ArraySegment<T> AsArraySegment => array == null ? default : new ArraySegment<T>(array, offset, length);

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<T>.IsReadOnly => true;
        int ICollection<T>.Count => length;

        // It actually should use a private field but as we never lock on this we could never cause a deadlock even if someone uses it.
        object ICollection.SyncRoot => array?.SyncRoot ?? Throw.InvalidOperationException<object>(Res.ArraySectionNull);
        bool ICollection.IsSynchronized => false;

        int ICollection.Count => length;
        bool IList.IsReadOnly => false;
        bool IList.IsFixedSize => true;

#if !(NET35 || NET40)
        int IReadOnlyCollection<T>.Count => length;
#endif

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        /// <summary>
        /// Gets or sets the element at the specified <paramref name="index"/>.
        /// <br/>To return a reference to an element use the <see cref="GetElementReference">GetElementReference</see> method instead.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public readonly T this[int index]
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)length)
                    Throw.IndexOutOfRangeException();
                return GetItemInternal(index);
            }
            [MethodImpl(MethodImpl.AggressiveInlining)]
            set
            {
                if ((uint)index >= (uint)length)
                    Throw.IndexOutOfRangeException();
                array![offset + index] = value;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                Throw.ThrowIfNullIsInvalid<T>(value);
                try
                {
                    this[index] = (T)value!;
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

        #region Operators

        /// <summary>
        /// Performs an implicit conversion from array of <typeparamref name="T"/> to <see cref="ArraySection{T}"/>.
        /// </summary>
        /// <param name="array">The array to be converted to an <see cref="ArraySection{T}"/>.</param>
        /// <returns>
        /// An <see cref="ArraySection{T}"/> instance that represents the original array.
        /// </returns>
        public static implicit operator ArraySection<T>(T[]? array) => array == null ? Null : new ArraySection<T>(array);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Performs an implicit conversion from <see cref="ArraySection{T}"/> to <see cref="Span{T}"><![CDATA[Span<T>]]></see>.
        /// </summary>
        /// <param name="arraySection">The <see cref="ArraySection{T}"/> to be converted to a <see cref="Span{T}"><![CDATA[Span<T>]]></see>.</param>
        /// <returns>
        /// A <see cref="Span{T}"><![CDATA[Span<T>]]></see> instance that represents the specified <see cref="ArraySection{T}"/>.
        /// </returns>
        public static implicit operator Span<T>(ArraySection<T> arraySection) => arraySection.AsSpan;
#endif

        /// <summary>
        /// Determines whether two specified <see cref="ArraySection{T}"/> instances have the same value.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(ArraySection<T> a, ArraySection<T> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified <see cref="ArraySection{T}"/> instances have different values.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(ArraySection<T> a, ArraySection<T> b) => !(a == b);

        #endregion

        #region Constructors
        
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct using an internally allocated buffer.
        /// <br/>When using this overload, the returned <see cref="ArraySection{T}"/> instance must be released
        /// by the <see cref="Release">Release</see> method if it is not used anymore.
        /// </summary>
        /// <param name="length">The length of the <see cref="ArraySection{T}"/> to be created.</param>
        /// <param name="assureClean"><see langword="true"/>&#160;to make sure the allocated array is zero-initialized;
        /// otherwise, <see langword="false"/>. Affects larger arrays only, if current platform supports using <see cref="ArrayPool{T}"/>. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
#if NETFRAMEWORK || NETSTANDARD2_0
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used in .NET Core 2.1/Standard 2.1 and above")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "ReSharper issue")]
#endif
        public ArraySection(int length, bool assureClean = true)
        {
            if (length < 0)
                Throw.ArgumentOutOfRangeException(Argument.length);
            offset = 0;
            this.length = length;

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            poolArray = length >= poolingThreshold;
            if (poolArray)
            {
                array = ArrayPool<T>.Shared.Rent(length);
                if (assureClean)
                    Clear();
                return;
            }
#endif

            array = length == 0 ? Reflector.EmptyArray<T>() : new T[length];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct from the specified <paramref name="array"/>.
        /// No heap allocation occurs when using this constructor overload.
        /// </summary>
        /// <param name="array">The array to initialize the new <see cref="ArraySection{T}"/> instance from.</param>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier", Justification = "False alarm, array CAN be null, it is just not ALLOWED (exception is thrown from the overload)")]
        public ArraySection(T[] array) : this(array, 0, array?.Length ?? 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct from the specified <paramref name="array"/>
        /// using the specified <paramref name="offset"/> and <paramref name="length"/>.
        /// No heap allocation occurs when using this constructor overload.
        /// </summary>
        /// <param name="array">The array to initialize the new <see cref="ArraySection{T}"/> instance from.</param>
        /// <param name="offset">The index of the first element in the <paramref name="array"/> to include in the new <see cref="ArraySection{T}"/>.</param>
        /// <param name="length">The number of items to include in the new <see cref="ArraySection{T}"/>.</param>
        public ArraySection(T[] array, int offset, int length)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);
            if ((uint)offset > (uint)array.Length)
                Throw.ArgumentOutOfRangeException(Argument.offset);
            if ((uint)length > (uint)(array.Length - offset))
                Throw.ArgumentOutOfRangeException(Argument.length);
            this.array = array;
            this.offset = offset;
            this.length = length;
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            poolArray = false;
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct from the specified <paramref name="array"/>
        /// using the specified <paramref name="offset"/>.
        /// No heap allocation occurs when using this constructor overload.
        /// </summary>
        /// <param name="array">The array to initialize the new <see cref="ArraySection{T}"/> instance from.</param>
        /// <param name="offset">The index of the first element in the <paramref name="array"/> to include in the new <see cref="ArraySection{T}"/>.</param>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier", Justification = "False alarm, array CAN be null, it is just not ALLOWED (exception is thrown from the overload)")]
        public ArraySection(T[] array, int offset) : this(array, offset, (array?.Length ?? 0) - offset)
        {
        }

        #endregion

        #region Private Constructors

        private ArraySection(SerializationInfo info, StreamingContext context) : this()
        {
            // deserialized instances never use array pool
            array = info.GetValueOrDefault<T[]>(nameof(array));
            length = array?.Length ?? 0;
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        private static bool CanAccept(object? value) => value is T || value == null && default(T) == null;

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Clears the items in this <see cref="ArraySection{T}"/> instance so all elements will have the default value of type <typeparamref name="T"/>.
        /// </summary>
        public readonly void Clear()
        {
            if (length == 0)
                return;
            Array.Clear(array!, offset, length);
        }

        /// <summary>
        /// Gets a new <see cref="ArraySection{T}"/> instance, which represents a subsection of the current instance with the specified <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first item of the returned section.</param>
        /// <returns>The subsection of the current <see cref="ArraySection{T}"/> instance with the specified <paramref name="startIndex"/>.</returns>
        public readonly ArraySection<T> Slice(int startIndex) => new ArraySection<T>(array!, offset + startIndex, length - startIndex);

        /// <summary>
        /// Gets a new <see cref="ArraySection{T}"/> instance, which represents a subsection of the current instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first item of the returned section.</param>
        /// <param name="length">The desired length of the returned section.</param>
        /// <returns>The subsection of the current <see cref="ArraySection{T}"/> instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.</returns>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "ParameterHidesMember", Justification = "Intended because it will be the new length of the returned instance")]
        public readonly ArraySection<T> Slice(int startIndex, int length) => new ArraySection<T>(array!, offset + startIndex, length);

        /// <summary>
        /// Returns a reference to the first element in this <see cref="ArraySection{T}"/>.
        /// This makes possible to use the <see cref="ArraySection{T}"/> in a <see langword="fixed"/>&#160;statement.
        /// </summary>
        /// <returns>A reference to the first element in this <see cref="ArraySection{T}"/>, or <see langword="null"/>&#160;if <see cref="IsNullOrEmpty"/> is <see langword="true"/>.</returns>
        public readonly ref T GetPinnableReference()
        {
            if (IsNullOrEmpty)
            {
#if NET5_0_OR_GREATER
                return ref Unsafe.NullRef<T>();
#elif NETCOREAPP3_0
                unsafe
                {
                    return ref Unsafe.AsRef<T>(null);
                }
#else
                Throw.InvalidOperationException(Res.ArraySectionEmpty);
#endif
            }

            return ref GetElementReferenceInternal(0);
        }

        /// <summary>
        /// Copies the elements of this <see cref="ArraySection{T}"/> to a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of this <see cref="ArraySection{T}"/>,
        /// or <see langword="null"/>&#160;if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        public readonly T[]? ToArray()
        {
            if (length == 0)
                return array; // it can be even null
            T[] result = new T[length];
            array!.CopyElements(offset, result, 0, length);
            return result;
        }

        /// <summary>
        /// Gets this <see cref="ArraySection{T}"/> as an <see cref="Array2D{T}"/> instance
        /// using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// The <see cref="ArraySection{T}"/> must have enough capacity for the specified dimensions.
        /// </summary>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>An <see cref="Array2D{T}"/> instance using this <see cref="ArraySection{T}"/> as its underlying buffer that has the specified dimensions.</returns>
        public readonly Array2D<T> AsArray2D(int height, int width) => new Array2D<T>(this, height, width);

        /// <summary>
        /// Gets this <see cref="ArraySection{T}"/> as an <see cref="Array3D{T}"/> instance
        /// using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// The <see cref="ArraySection{T}"/> must have enough capacity for the specified dimensions.
        /// </summary>
        /// <param name="depth">The depth of the array to be returned.</param>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>An <see cref="Array3D{T}"/> instance using this <see cref="ArraySection{T}"/> as its underlying buffer that has the specified dimensions.</returns>
        public readonly Array3D<T> AsArray3D(int depth, int height, int width) => new Array3D<T>(this, depth, height, width);

        /// <summary>
        /// Gets the reference to the element at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the element to get the reference for.</param>
        /// <returns>The reference to the element at the specified index.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public readonly ref T GetElementReference(int index)
        {
            if ((uint)index >= (uint)length)
                Throw.IndexOutOfRangeException();
            return ref GetElementReferenceInternal(index);
        }

        /// <summary>
        /// Determines the index of a specific item in this <see cref="ArraySection{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="ArraySection{T}"/>.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        public readonly int IndexOf(T item)
        {
            if (length == 0)
                return -1;
            int result = Array.IndexOf(array!, item, offset, length);
            return result < 0 ? result : result - offset;
        }

        /// <summary>
        /// Determines whether this <see cref="ArraySection{T}"/> contains the specific <paramref name="item"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if <paramref name="item"/> is found in this <see cref="ArraySection{T}"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="item">The object to locate in this <see cref="ArraySection{T}"/>.</param>
        public readonly bool Contains(T item) => IndexOf(item) >= 0;

        /// <summary>
        /// Copies the items of this <see cref="ArraySection{T}"/> to a compatible one-dimensional array, starting at a particular index.
        /// </summary>
        /// <param name="target">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from this <see cref="ArraySection{T}"/>.</param>
        /// <param name="targetIndex">The zero-based index in <paramref name="target"/> at which copying begins. This parameter is optional.
        /// <br/>Default value: 0.</param>
        public readonly void CopyTo(T[] target, int targetIndex = 0)
        {
            if (target == null!)
                Throw.ArgumentNullException(Argument.target);
            if (targetIndex < 0 || targetIndex > target.Length)
                Throw.ArgumentOutOfRangeException(Argument.targetIndex);
            if (target.Length - targetIndex < length)
                Throw.ArgumentException(Argument.target, Res.ICollectionCopyToDestArrayShort);
            array?.CopyElements(offset, target, targetIndex, length);
        }

        /// <summary>
        /// Copies the items of this <see cref="ArraySection{T}"/> to a compatible instance, starting at a particular index.
        /// </summary>
        /// <param name="target">The <see cref="ArraySection{T}"/> that is the destination of the elements copied from this instance.</param>
        /// <param name="targetIndex">The zero-based index in <paramref name="target"/> at which copying begins.</param>
        public readonly void CopyTo(ArraySection<T> target, int targetIndex = 0)
        {
            if (target.IsNull)
                Throw.ArgumentNullException(Argument.target);
            if (targetIndex < 0 || targetIndex > target.Length)
                Throw.ArgumentOutOfRangeException(Argument.targetIndex);
            if (target.length - targetIndex < length)
                Throw.ArgumentException(Argument.target, Res.ICollectionCopyToDestArrayShort);
            array?.CopyElements(offset, target.array!, target.offset + targetIndex, length);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the items of this <see cref="ArraySection{T}"/>.
        /// </summary>
        /// <returns>An <see cref="ArraySectionEnumerator{T}"/> instance that can be used to iterate though the elements of this <see cref="ArraySection{T}"/>.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public readonly ArraySectionEnumerator<T> GetEnumerator() => new ArraySectionEnumerator<T>(array, offset, length);

        /// <summary>
        /// Releases the underlying array. If this <see cref="ArraySection{T}"/> instance was instantiated by the <see cref="ArraySection{T}(int,bool)">self allocating constructor</see>,
        /// then this method must be called when the <see cref="ArraySection{T}"/> is not used anymore.
        /// On platforms that do not support the <see cref="ArrayPool{T}"/> class this method simply sets the self instance to <see cref="Null"/>.
        /// </summary>
        public void Release()
        {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            if (array != null && poolArray)
            {
                if (Reflector<T>.IsManaged)
                    Array.Clear(array, offset, length);
                ArrayPool<T>.Shared.Return(array);
            }
#endif
            // this is required to prevent possible multiple returns to ArrayPool
            this = Null;
        }

        /// <summary>
        /// Indicates whether the current <see cref="ArraySection{T}"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// </summary>
        /// <param name="other">An <see cref="ArraySection{T}"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public readonly bool Equals(ArraySection<T> other)
            => array == other.array && offset == other.offset && length == other.length;

        /// <summary>
        /// Determines whether the specified <see cref="object">object</see> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified object is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public readonly override bool Equals(object? obj)
            => obj == null ? IsNull
                : obj is ArraySection<T> other ? Equals(other)
                : obj is T[] arr && Equals(new ArraySection<T>(arr)); // must check array because == operator supports it as well

        /// <summary>
        /// Returns a hash code for this <see cref="ArraySection{T}"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public readonly override int GetHashCode() => array == null ? 0 : (array, offset, length).GetHashCode();

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal readonly T GetItemInternal(int index) => array![offset + index];

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal readonly void SetItemInternal(int index, T value) => array![offset + index] = value;

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal readonly ref T GetElementReferenceInternal(int index) => ref array![offset + index];

        #endregion

        #region Explicitly Implemented Interface Methods

        void IList<T>.Insert(int index, T item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void IList<T>.RemoveAt(int index) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

        void ICollection<T>.Add(T item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        bool ICollection<T>.Remove(T item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IList.IndexOf(object? value) => CanAccept(value) ? IndexOf((T)value!) : -1;
        bool IList.Contains(object? value) => CanAccept(value) && Contains((T)value!);

        void ICollection.CopyTo(Array targetArray, int index)
        {
            if (targetArray == null!)
                Throw.ArgumentNullException(Argument.array);

            if (targetArray is T[] typedArray)
            {
                CopyTo(typedArray, index);
                return;
            }

            if (index < 0 || index > targetArray.Length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (targetArray.Length - index < length)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (targetArray.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            if (targetArray is object?[] objectArray)
            {
                for (int i = 0; i < length; i++)
                {
                    objectArray[index] = GetItemInternal(i);
                    index += 1;
                }

                return;
            }

            Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
        }

        int IList.Add(object? item) => Throw.NotSupportedException<int>(Res.ICollectionReadOnlyModifyNotSupported);
        void IList.Insert(int index, object? item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void IList.Remove(object? item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void IList.RemoveAt(int index) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // as the underlying array and the offset is not exposed by public members serializing the represented array only
            info.AddValue(nameof(array), length == 0 || length == array!.Length ? array : ToArray());
        }

        #endregion

        #endregion

        #endregion
    }
}
