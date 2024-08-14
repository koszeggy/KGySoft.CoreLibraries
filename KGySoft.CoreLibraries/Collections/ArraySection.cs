#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArraySection.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
using System.Buffers;
#endif
using System.Runtime.Serialization;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

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
    /// This type is very similar to <see cref="ArraySegment{T}"/>/<see cref="Memory{T}"><![CDATA[Memory<T>]]></see> types but can be used on every platform in the same way,
    /// allows span-like operations such as slicing, and it is faster than <see cref="Memory{T}"><![CDATA[Memory<T>]]></see> in most cases.
    /// Depending on the used platform it supports <see cref="ArrayPool{T}"/> allocation.
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
    /// <para>Though <see cref="ArraySection{T}"/> is practically immutable (has only <see langword="readonly"/> fields) it is not marked as <c>readonly</c>,
    /// which is needed for the <see cref="Release">Release</see> method to work properly. As <see cref="ArraySection{T}"/> is a
    /// non-<c>readonly</c>&#160;<see langword="struct"/> it is not recommended to use it as a <c>readonly</c> field; otherwise,
    /// accessing its members would make the pre-C# 8.0 compilers to create defensive copies, which leads to a slight performance degradation.</para>
    /// <note type="tip">You can always easily reinterpret an <see cref="ArraySection{T}"/> instance as a two or three-dimensional array by the <see cref="AsArray2D">AsArray2D</see>
    /// and <see cref="AsArray3D">AsArray3D</see> methods without any allocation on the heap.</note>
    /// </remarks>
    [Serializable]
    [DebuggerTypeProxy(typeof(ArraySection<>.ArraySectionDebugView))]
    [DebuggerDisplay("{typeof(" + nameof(T) + ")." + nameof(Type.Name) + ",nq}[{" + nameof(Length) + "}]")]
    public struct ArraySection<T> : IList<T>, IList, IEquatable<ArraySection<T>>
#if !(NET35 || NET40)
        , IReadOnlyList<T>
#endif
    {
        #region Nested Types

        private struct ArraySectionDebugView // strange error in VS2019: this must be a struct; otherwise, debug values will be completely misinterpreted
        {
            #region Fields

            [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "ArraySection is not readonly so it would generate defensive copies on platforms")]
            private ArraySection<T> array;

            #endregion

            #region Properties

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            readonly public T[]? Items => array.ToArray();

            #endregion

            #region Constructors

            internal ArraySectionDebugView(ArraySection<T> array) => this.array = array;

            #endregion
        }

        #endregion

        #region Constants

        private const int poolArrayMask = 1 << 31;
        private const int lengthMask = Int32.MaxValue;

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
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        public readonly int Length => length & lengthMask;
#else
        public readonly int Length => length;
#endif

        /// <summary>
        /// Gets whether this <see cref="ArraySection{T}"/> instance represents a <see langword="null"/> array.
        /// <br/>Please note that the <see cref="ToArray">ToArray</see> method returns <see langword="null"/> when this property returns <see langword="true"/>.
        /// </summary>
        public readonly bool IsNull => array == null;

        /// <summary>
        /// Gets whether this <see cref="ArraySection{T}"/> instance represents an empty array section or a <see langword="null"/> array.
        /// </summary>
        public readonly bool IsNullOrEmpty => length == 0; // empty sections are never pooled so we can use the field here

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns this <see cref="ArraySection{T}"/> as a <see cref="Memory{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public readonly Memory<T> AsMemory => new Memory<T>(array, offset, Length);

        /// <summary>
        /// Returns this <see cref="ArraySection{T}"/> as a <see cref="Span{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public readonly Span<T> AsSpan => new Span<T>(array, offset, Length);
#endif

        /// <summary>
        /// Returns the current <see cref="ArraySection{T}"/> instance as an <see cref="ArraySegment{T}"/>.
        /// </summary>
        public readonly ArraySegment<T> AsArraySegment => array == null ? default : new ArraySegment<T>(array, offset, Length);

        #endregion

        #region Explicitly Implemented Interface Properties

        readonly bool ICollection<T>.IsReadOnly => true;
        readonly int ICollection<T>.Count => Length;

        // It actually should use a private field but as we never lock on this we could never cause a deadlock even if someone uses it.
        readonly object ICollection.SyncRoot => array?.SyncRoot ?? Throw.InvalidOperationException<object>(Res.ArraySectionNull);
        readonly bool ICollection.IsSynchronized => false;

        readonly int ICollection.Count => Length;
        readonly bool IList.IsReadOnly => false;
        readonly bool IList.IsFixedSize => true;

#if !(NET35 || NET40)
        readonly int IReadOnlyCollection<T>.Count => Length;
#endif

        #endregion

        #endregion

        #region Indexers

        #region Public Indexers

        /// <summary>
        /// Gets or sets the element at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <remarks>
        /// <para>This member validates <paramref name="index"/> against <see cref="Length"/>. To allow getting/setting any element in the <see cref="UnderlyingArray"/> use
        /// the <see cref="GetElementUnchecked">GetElementUnchecked</see>/<see cref="SetElementUnchecked">SetElementUnchecked</see> methods instead.</para>
        /// <para>To return a reference to an element use the <see cref="GetElementReference">GetElementReference</see> method instead.</para>
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than zero or greater or equal to <see cref="Length"/>.</exception>
        public readonly T this[int index]
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)Length)
                    Throw.IndexOutOfRangeException();
                return GetItemInternal(index);
            }
            [MethodImpl(MethodImpl.AggressiveInlining)]
            set
            {
                if ((uint)index >= (uint)Length)
                    Throw.IndexOutOfRangeException();
                array![offset + index] = value;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object? IList.this[int index]
        {
            readonly get => this[index];
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

        /// <summary>
        /// Performs an implicit conversion from <see cref="ArraySegment{T}"/> to <see cref="ArraySection{T}"/>.
        /// </summary>
        /// <param name="arraySegment">The <see cref="ArraySegment{T}"/> to be converted to an <see cref="ArraySection{T}"/>.</param>
        /// <returns>
        /// An <see cref="ArraySection{T}"/> instance that represents the original <see cref="ArraySegment{T}"/>.
        /// </returns>
        public static implicit operator ArraySection<T>(ArraySegment<T> arraySegment) => new ArraySection<T>(arraySegment);

        /// <summary>
        /// Performs an implicit conversion from <see cref="ArraySection{T}"/> to <see cref="ArraySegment{T}"/>.
        /// </summary>
        /// <param name="arraySection">The <see cref="ArraySection{T}"/> to be converted to an <see cref="ArraySegment{T}"/>.</param>
        /// <returns>
        /// An <see cref="ArraySegment{T}"/> instance that represents this <see cref="ArraySection{T}"/>.
        /// </returns>
        public static implicit operator ArraySegment<T>(ArraySection<T> arraySection) => arraySection.AsArraySegment;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Performs an implicit conversion from <see cref="ArraySection{T}"/> to <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="arraySection">The <see cref="ArraySection{T}"/> to be converted to a <see cref="Span{T}"/>.</param>
        /// <returns>
        /// A <see cref="Span{T}"/> instance that represents the specified <see cref="ArraySection{T}"/>.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct using an internally allocated buffer.
        /// <br/>When using this overload, the returned <see cref="ArraySection{T}"/> instance must be released
        /// by the <see cref="Release">Release</see> method if it is not used anymore.
        /// </summary>
        /// <param name="length">The length of the <see cref="ArraySection{T}"/> to be created.</param>
        /// <param name="assureClean"><see langword="true"/> to make sure the allocated underlying array is zero-initialized;
        /// otherwise, <see langword="false"/>. May not have an effect on older targeted platforms. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
#if NETFRAMEWORK || NETSTANDARD2_0
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used in .NET Core 2.1/Standard 2.1 and above")]
#endif
        public ArraySection(int length, bool assureClean = true)
        {
            if (length < 0)
                Throw.ArgumentOutOfRangeException(Argument.length);
            offset = 0;
            this.length = length;

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            if (length >= poolingThreshold)
            {
                length |= poolArrayMask;
                array = ArrayPool<T>.Shared.Rent(length);
                if (assureClean)
                    Clear();
                return;
            }
#endif

            if (length == 0)
            {
                array = Reflector.EmptyArray<T>();
                return;
            }

#if NET5_0_OR_GREATER
            if (!assureClean)
            {
                array = GC.AllocateUninitializedArray<T>(length);
                return;
            }
#endif

            array = new T[length];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct from the specified <paramref name="array"/>.
        /// No heap allocation occurs when using this constructor overload.
        /// </summary>
        /// <param name="array">The array to initialize the new <see cref="ArraySection{T}"/> instance from.</param>
        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "False alarm, array CAN be null, it is just not ALLOWED (exception is thrown from the overload)")]
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct from the specified <paramref name="array"/>
        /// using the specified <paramref name="offset"/>.
        /// No heap allocation occurs when using this constructor overload.
        /// </summary>
        /// <param name="array">The array to initialize the new <see cref="ArraySection{T}"/> instance from.</param>
        /// <param name="offset">The index of the first element in the <paramref name="array"/> to include in the new <see cref="ArraySection{T}"/>.</param>
        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "False alarm, array CAN be null, it is just not ALLOWED (exception is thrown from the overload)")]
        public ArraySection(T[] array, int offset) : this(array, offset, (array?.Length ?? 0) - offset)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySection{T}" /> struct from the specified <see cref="ArraySegment{T}"/>.
        /// No heap allocation occurs when using this constructor overload.
        /// </summary>
        /// <param name="arraySegment">The <see cref="ArraySegment{T}"/> to initialize the new <see cref="ArraySection{T}"/> from.</param>
        public ArraySection(ArraySegment<T> arraySegment)
        {
            if (arraySegment.Array == null)
            {
                this = Null;
                return;
            }

            array = arraySegment.Array;
            offset = arraySegment.Offset;
            length = arraySegment.Count;
        }

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
            if (length == 0) // zero length sections never have pooled arrays so using the field is alright here
                return;
            Array.Clear(array!, offset, Length);
        }

        /// <summary>
        /// Gets a new <see cref="ArraySection{T}"/> instance, which represents a subsection of the current instance with the specified <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first item of the returned section.</param>
        /// <returns>The subsection of the current <see cref="ArraySection{T}"/> instance with the specified <paramref name="startIndex"/>.</returns>
        public readonly ArraySection<T> Slice(int startIndex) => Slice(startIndex, Length - startIndex);

        /// <summary>
        /// Gets a new <see cref="ArraySection{T}"/> instance, which represents a subsection of the current instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first item of the returned section.</param>
        /// <param name="length">The desired length of the returned section.</param>
        /// <returns>The subsection of the current <see cref="ArraySection{T}"/> instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.</returns>
        [SuppressMessage("ReSharper", "ParameterHidesMember", Justification = "Intended because it will be the new length of the returned instance")]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public readonly ArraySection<T> Slice(int startIndex, int length)
        {
            if (!IsNull)
                return new ArraySection<T>(array!, offset + startIndex, length);

            if (startIndex != 0)
                Throw.ArgumentOutOfRangeException(Argument.offset); // to be compatible with the non-null case above
            if (length != 0)
                Throw.ArgumentOutOfRangeException(Argument.length);
            return Null;
        }

        /// <summary>
        /// Returns a reference to the first element in this <see cref="ArraySection{T}"/>.
        /// This makes possible to use the <see cref="ArraySection{T}"/> in a <see langword="fixed"/> statement.
        /// </summary>
        /// <returns>A reference to the first element in this <see cref="ArraySection{T}"/>, or <see langword="null"/> if <see cref="IsNullOrEmpty"/> is <see langword="true"/>.</returns>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> is <see langword="true"/>.</exception>
        public readonly ref T GetPinnableReference()
        {
            if (IsNullOrEmpty)
                Throw.InvalidOperationException(Res.ArraySectionEmpty);
            return ref GetElementReferenceInternal(0);
        }

        /// <summary>
        /// Copies the elements of this <see cref="ArraySection{T}"/> to a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of this <see cref="ArraySection{T}"/>,
        /// or <see langword="null"/> if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        public readonly T[]? ToArray()
        {
            if (length == 0) // it's alright, pooled arrays never have zero length
                return IsNull ? null : Reflector.EmptyArray<T>();
            T[] result = new T[Length];
            array!.CopyElements(offset, result, 0,  result.Length);
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
        /// Reinterprets this <see cref="ArraySection{T}"/> by returning a <see cref="CastArray{TFrom,TTo}"/> struct,
        /// so its element type is cast from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
        /// This method can be used only when <typeparamref name="T"/> in this <see cref="ArraySection{T}"/> is a value type that contains no references.
        /// </summary>
        /// <typeparam name="TFrom">The actual element type of this <see cref="ArraySection{T}"/>. Must be the same as <typeparamref name="T"/>.</typeparam>
        /// <typeparam name="TTo">The reinterpreted element type after casting.</typeparam>
        /// <returns>A <see cref="CastArray{TFrom,TTo}"/> instance for this <see cref="ArraySection{T}"/>.</returns>
        /// <remarks>
        /// <para>If the size of <typeparamref name="TTo"/> cannot be divided by the size of <typeparamref name="TFrom"/>,
        /// then the cast result may not cover the whole original <see cref="ArraySection{T}"/> to prevent exceeding beyond the available buffer.</para>
        /// </remarks>
        public readonly CastArray<TFrom, TTo> Cast<TFrom, TTo>()
#if NETFRAMEWORK // To make the method compatible with older compilers
            where TFrom : struct, T
            where TTo : struct
#else
            where TFrom : unmanaged, T
            where TTo : unmanaged
#endif
        {
#if NETCOREAPP3_0_OR_GREATER
            return new CastArray<TFrom, TTo>(Unsafe.As<ArraySection<T>, ArraySection<TFrom>>(ref Unsafe.AsRef(in this)));
#else
            return new CastArray<TFrom, TTo>((ArraySection<TFrom>)(object)this);
#endif
        }

        /// <summary>
        /// Reinterprets this <see cref="ArraySection{T}"/> as a two-dimensional <see cref="CastArray2D{TFrom,TTo}"/> struct,
        /// while its element type is cast from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
        /// This method can be used only when <typeparamref name="T"/> in this <see cref="ArraySection{T}"/> is a value type that contains no references.
        /// </summary>
        /// <typeparam name="TFrom">The actual element type of this <see cref="ArraySection{T}"/>. Must be the same as <typeparamref name="T"/>.</typeparam>
        /// <typeparam name="TTo">The reinterpreted element type after casting.</typeparam>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance for this <see cref="ArraySection{T}"/>.</returns>
        public readonly CastArray2D<TFrom, TTo> Cast2D<TFrom, TTo>(int height, int width)
#if NETFRAMEWORK // To make the method compatible with older compilers
            where TFrom : struct, T
            where TTo : struct
#else
            where TFrom : unmanaged, T
            where TTo : unmanaged
#endif
        {
#if NETCOREAPP3_0_OR_GREATER
            return new CastArray2D<TFrom, TTo>(Unsafe.As<ArraySection<T>, ArraySection<TFrom>>(ref Unsafe.AsRef(in this)), height, width);
#else
            return new CastArray2D<TFrom, TTo>((ArraySection<TFrom>)(object)this, height, width);
#endif
        }

        /// <summary>
        /// Reinterprets this <see cref="ArraySection{T}"/> as a three-dimensional <see cref="CastArray2D{TFrom,TTo}"/> struct,
        /// while its element type is cast from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
        /// This method can be used only when <typeparamref name="T"/> in this <see cref="ArraySection{T}"/> is a value type that contains no references.
        /// </summary>
        /// <typeparam name="TFrom">The actual element type of this <see cref="ArraySection{T}"/>. Must be the same as <typeparamref name="T"/>.</typeparam>
        /// <typeparam name="TTo">The reinterpreted element type after casting.</typeparam>
        /// <param name="depth">The depth of the array to be returned.</param>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance for this <see cref="ArraySection{T}"/>.</returns>
        public readonly CastArray3D<TFrom, TTo> Cast3D<TFrom, TTo>(int depth, int height, int width)
#if NETFRAMEWORK // To make the method compatible with older compilers
            where TFrom : struct, T
            where TTo : struct
#else
            where TFrom : unmanaged, T
            where TTo : unmanaged
#endif
        {
#if NETCOREAPP3_0_OR_GREATER
            return new CastArray3D<TFrom, TTo>(Unsafe.As<ArraySection<T>, ArraySection<TFrom>>(ref Unsafe.AsRef(in this)), depth, height, width);
#else
            return new CastArray3D<TFrom, TTo>((ArraySection<TFrom>)(object)this, depth, height, width);
#endif
        }

        /// <summary>
        /// Gets the reference to the element at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the element to get the reference for.</param>
        /// <returns>The reference to the element at the specified index.</returns>
        /// <remarks>
        /// This method validates <paramref name="index"/> against <see cref="Length"/>.
        /// To allow returning a reference to any element from the <see cref="UnderlyingArray"/>
        /// (allowing even a negative <paramref name="index"/> if <see cref="Offset"/> is nonzero),
        /// then use the <see cref="GetElementReferenceUnchecked">GetElementReferenceUnchecked</see> method instead.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than zero or greater or equal to <see cref="Length"/>.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public readonly ref T GetElementReference(int index)
        {
            if ((uint)index >= (uint)Length)
                Throw.IndexOutOfRangeException();
            return ref GetElementReferenceInternal(index);
        }

        /// <summary>
        /// Gets the element at the specified <paramref name="index"/>, allowing it to point to any element in the <see cref="UnderlyingArray"/>.
        /// To validate <paramref name="index"/> against <see cref="Length"/> use the <see cref="this">indexer</see> instead.
        /// </summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> plus <see cref="Offset"/> is less than zero or greater or equal to the length of the <see cref="UnderlyingArray"/>.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public readonly T GetElementUnchecked(int index)
        {
            if (IsNull)
                Throw.IndexOutOfRangeException();
            return GetItemInternal(index);
        }

        /// <summary>
        /// Sets the element at the specified <paramref name="index"/>, allowing it to point to any element in the <see cref="UnderlyingArray"/>.
        /// To validate <paramref name="index"/> against <see cref="Length"/> use the <see cref="this">indexer</see> instead.
        /// </summary>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> plus <see cref="Offset"/> is less than zero or greater or equal to the length of the <see cref="UnderlyingArray"/>.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public readonly void SetElementUnchecked(int index, T value)
        {
            if (IsNull)
                Throw.IndexOutOfRangeException();
            SetItemInternal(index, value);
        }

        /// <summary>
        /// Gets the reference to the element at the specified <paramref name="index"/>, it to point to any element in the <see cref="UnderlyingArray"/>.
        /// To validate <paramref name="index"/> against <see cref="Length"/> use the <see cref="GetElementReference">GetElementReference</see> method instead.
        /// </summary>
        /// <param name="index">The index of the element to get the reference for.</param>
        /// <returns>The reference to the element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> plus <see cref="Offset"/> is less than zero or greater or equal to the length of the <see cref="UnderlyingArray"/>.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public readonly ref T GetElementReferenceUnchecked(int index)
        {
            if (IsNull)
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
            if (length == 0) // it's alright, pooled arrays never have zero length
                return -1;
            int result = Array.IndexOf(array!, item, offset, Length);
            return result < 0 ? result : result - offset;
        }

        /// <summary>
        /// Determines whether this <see cref="ArraySection{T}"/> contains the specific <paramref name="item"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> is found in this <see cref="ArraySection{T}"/>; otherwise, <see langword="false"/>.
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

            int len = Length;
            if (target.Length - targetIndex < len)
                Throw.ArgumentException(Argument.target, Res.ICollectionCopyToDestArrayShort);
            array?.CopyElements(offset, target, targetIndex, len);
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

            int len = Length;
            if (target.length - targetIndex < len)
                Throw.ArgumentException(Argument.target, Res.ICollectionCopyToDestArrayShort);
            array?.CopyElements(offset, target.array!, target.offset + targetIndex, len);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the items of this <see cref="ArraySection{T}"/>.
        /// </summary>
        /// <returns>An <see cref="ArraySectionEnumerator{T}"/> instance that can be used to iterate though the elements of this <see cref="ArraySection{T}"/>.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public readonly ArraySectionEnumerator<T> GetEnumerator() => new ArraySectionEnumerator<T>(array, offset, Length);

        /// <summary>
        /// Releases the underlying array. If this <see cref="ArraySection{T}"/> instance was instantiated by the <see cref="ArraySection{T}(int,bool)">self allocating constructor</see>,
        /// then this method must be called when the <see cref="ArraySection{T}"/> is not used anymore.
        /// On platforms that do not support the <see cref="ArrayPool{T}"/> class this method simply sets the self instance to <see cref="Null"/>.
        /// </summary>
        public void Release()
        {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            if (array != null && (length & poolArrayMask) != 0)
            {
                if (Reflector<T>.IsManaged)
                    Array.Clear(array, offset, Length);
                ArrayPool<T>.Shared.Return(array);
            }
#endif
            // this is required to prevent possible multiple returns to ArrayPool
            this = Null;
        }

        /// <summary>
        /// Indicates whether the current <see cref="ArraySection{T}"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// That is, when they both reference the same section of the same <see cref="UnderlyingArray"/> instance.
        /// </summary>
        /// <param name="other">An <see cref="ArraySection{T}"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public readonly bool Equals(ArraySection<T> other)
            => array == other.array && offset == other.offset && Length == other.Length; // Ignoring the pool array bit is intended.

        /// <summary>
        /// Determines whether the specified <see cref="object">object</see> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if the specified object is equal to this instance; otherwise, <see langword="false"/>.</returns>
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
        public readonly override int GetHashCode() => array == null ? 0 : (array, offset, Length).GetHashCode();

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal readonly T GetItemInternal(int index) => array![offset + index];

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal readonly void SetItemInternal(int index, T value) => array![offset + index] = value;

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal readonly ref T GetElementReferenceInternal(int index) => ref array![offset + index];

        #endregion

        #region Private Methods

        [OnDeserialized]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "False alarm, the [OnDeserialized] method must have this signature.")]
        private void OnDeserialized(StreamingContext ctx)
        {
            // This method is just to clear the poolArray flag from length after deserialization. Applying also on older frameworks because the serialized instance
            // may come from any platform. length &= lengthMask would be more obvious but that would require to make length non-readonly.
            this = Slice(0, Length);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        readonly void IList<T>.Insert(int index, T item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        readonly void IList<T>.RemoveAt(int index) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

        readonly void ICollection<T>.Add(T item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        readonly bool ICollection<T>.Remove(T item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        readonly int IList.IndexOf(object? value) => CanAccept(value) ? IndexOf((T)value!) : -1;
        readonly bool IList.Contains(object? value) => CanAccept(value) && Contains((T)value!);

        readonly void ICollection.CopyTo(Array targetArray, int index)
        {
            if (targetArray == null!)
                Throw.ArgumentNullException(Argument.array);

            if (targetArray is T[] typedArray)
            {
                CopyTo(typedArray, index);
                return;
            }

            int len = Length;
            if (index < 0 || index > len)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (targetArray.Length - index < len)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (targetArray.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            if (targetArray is object?[] objectArray)
            {
                for (int i = 0; i < len; i++)
                {
                    objectArray[index] = GetItemInternal(i);
                    index += 1;
                }

                return;
            }

            Throw.ArgumentException(Argument.array, Res.ICollectionArrayTypeInvalid);
        }

        readonly int IList.Add(object? item) => Throw.NotSupportedException<int>(Res.ICollectionReadOnlyModifyNotSupported);
        readonly void IList.Insert(int index, object? item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        readonly void IList.Remove(object? item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        readonly void IList.RemoveAt(int index) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

        #endregion

        #endregion

        #endregion
    }
}
