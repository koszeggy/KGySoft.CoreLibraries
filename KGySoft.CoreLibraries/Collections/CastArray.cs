#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CastArray.cs
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
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Buffers;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Security;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Threading;
#endif

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

#region Suppressions

#if !(NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#if NETFRAMEWORK
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type - false alarm, the static constructor constrains unmanaged type arguments
#endif

#endregion

namespace KGySoft.Collections
{
    /// <summary>
    /// Represents a one dimensional array (or a section of it) where the original element type of <typeparamref name="TFrom"/> is cast to another element type of <typeparamref name="TTo"/>.
    /// Can be helpful on platforms where <see cref="Span{T}"/> is not available or wherever you must use arrays that you want to reinterpret. For example, you want to retrieve
    /// only byte arrays from the <see cref="ArrayPool{T}"/> but you want to reinterpret them as other array types.
    /// </summary>
    /// <typeparam name="TFrom">The actual element type of the underlying array.</typeparam>
    /// <typeparam name="TTo">The reinterpreted element type of the underlying array.</typeparam>
    /// <remarks>
    /// TODO: Pooling example. Returning to the pool must be done by the caller. You can use a self-allocating ArraySection, which can be released in the end.
    /// TODO: Slice may throw ArgumentException.
    /// TODO: .NET Framework 4.x only: in a partially trusted AppDomain the indexer and some methods may throw NotSupportedException, grant SecurityPermission with the SecurityPermissionFlag.SkipVerification flag.
    /// </remarks>
    [Serializable]
    [DebuggerTypeProxy(typeof(CastArray<,>.CastArrayDebugView))]
    [DebuggerDisplay("{typeof(" + nameof(TTo) + ")." + nameof(Type.Name) + ",nq}[{" + nameof(Length) + "}]")]
    public readonly struct CastArray<TFrom, TTo> : IList<TTo>, IList, IEquatable<CastArray<TFrom, TTo>> // TODO
#if !(NET35 || NET40)
        , IReadOnlyList<TTo>
#endif
#if NETFRAMEWORK // To make the type compatible with older compilers
        where TFrom : struct
        where TTo : struct
#else
        where TFrom : unmanaged
        where TTo : unmanaged
#endif
    {
        #region Nested Types

        #region Nested Classes

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private sealed class CastArrayMemoryManager : MemoryManager<TTo>
        {
            #region Fields

            private readonly CastArray<TFrom, TTo> castArray;

            private GCHandle pinnedHandle;
            private int pinCount;

            #endregion

            #region Constructors

            internal CastArrayMemoryManager(CastArray<TFrom, TTo> castArray) => this.castArray = castArray;

            #endregion

            #region Methods

            #region Public Methods

            public override Span<TTo> GetSpan() => castArray.AsSpan;

            public override unsafe MemoryHandle Pin(int elementIndex = 0)
            {
                // This must be before mutating anything because the index validation can throw an exception.
                ref TTo refResult = ref castArray.GetElementReference(elementIndex);

                // It's alright to lock on this, this instance is not exposed publicly.
                lock (this)
                {
                    if (!pinnedHandle.IsAllocated)
                        pinnedHandle = GCHandle.Alloc(castArray.buffer.UnderlyingArray, GCHandleType.Pinned);
                }

                Interlocked.Increment(ref pinCount);

                // Not returning the GCHandle in the result because if there are concurrent pinners, they could unpin the memory too early.
                // Passing only this instance so Unpin will be called that handles everything correctly.
#if NETCOREAPP3_0_OR_GREATER
                return new MemoryHandle(Unsafe.AsPointer(ref refResult), default, this);
#else
                // Actually fixed is not needed to pin the reference here, but the cast does not without it...
                fixed (void* ptr = &refResult)
                    return new MemoryHandle(ptr, default, this);
#endif
            }

            public override void Unpin()
            {
                // Can occur if a MemoryHandle of Pin was copied and more than one copies were disposed.
                if (!pinnedHandle.IsAllocated)
                    return;

                if (Interlocked.Decrement(ref pinCount) == 0)
                    pinnedHandle.Free();
            }

            #endregion

            #region Protected Methods

            protected override void Dispose(bool disposing)
            {
                if (pinnedHandle.IsAllocated)
                    pinnedHandle.Free();
            }

            #endregion

            #endregion
        }
#endif

        #endregion

        #region Nested Structs

        private struct CastArrayDebugView // Maybe would work also if it was a class but see ArraySectionDebugView
        {
            #region Fields

            private readonly CastArray<TFrom, TTo> array;

            #endregion

            #region Properties

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used by the debugger")]
            readonly public TTo[]? Items => array.ToArray();

            #endregion

            #region Constructors

            internal CastArrayDebugView(CastArray<TFrom, TTo> array) => this.array = array;

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        /// <summary>
        /// Represents the <see langword="null"/>&#160;<see cref="CastArray{TFrom,TTo}"/>. This field is read-only.
        /// </summary>
        public static readonly CastArray<TFrom, TTo> Null = default;

        /// <summary>
        /// Represents the empty <see cref="CastArray{TFrom,TTo}"/>. This field is read-only.
        /// </summary>
        public static readonly CastArray<TFrom, TTo> Empty = new CastArray<TFrom, TTo>(ArraySection<TFrom>.Empty);

        #endregion

        #region Instance Fields

        private readonly ArraySection<TFrom> buffer;
        private readonly int length;

        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the underlying buffer of this <see cref="CastArray{TFrom,TTo}"/> as an <see cref="ArraySection{T}"/> instance.
        /// </summary>
        public ArraySection<TFrom> Buffer => buffer;

        /// <summary>
        /// Gets the number of <typeparamref name="TTo"/> elements in this <see cref="CastArray{TFrom,TTo}"/>.
        /// To get the number of <typeparamref name="TFrom"/> elements check the <see cref="Buffer"/> property.
        /// </summary>
        public int Length => length;

        /// <summary>
        /// Gets whether this <see cref="CastArray{TFrom,TTo}"/> instance represents a <see langword="null"/> array.
        /// <br/>Please note that the <see cref="ToArray">ToArray</see> method returns <see langword="null"/> when this property returns <see langword="true"/>.
        /// </summary>
        public bool IsNull => buffer.IsNull;

        /// <summary>
        /// Gets whether this <see cref="CastArray{TFrom,TTo}"/> instance represents an empty array section or a <see langword="null"/> array.
        /// </summary>
        public bool IsNullOrEmpty => length == 0; // not buffer.IsNullOrEmpty because cast length can be truncated

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns this <see cref="CastArray{TFrom,TTo}"/> as a <see cref="Memory{T}"/> instance.
        /// Please note that getting this property allocates a custom <see cref="MemoryManager{T}"/> instance internally.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public Memory<TTo> AsMemory => IsNull ? default : new CastArrayMemoryManager(this).Memory;

        /// <summary>
        /// Returns this <see cref="CastArray{TFrom,TTo}"/> as a <see cref="Span{T}"/> instance.
        /// </summary>
        /// <remarks><note>This member is available in .NET Core 2.1/.NET Standard 2.1 and above.</note></remarks>
        public Span<TTo> AsSpan => MemoryMarshal.Cast<TFrom, TTo>(buffer.AsSpan);
#endif

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<TTo>.IsReadOnly => true;
        int ICollection<TTo>.Count => length;

        // It actually should use a private field but as we never lock on this we could never cause a deadlock even if someone uses it.
        object ICollection.SyncRoot => Buffer.UnderlyingArray?.SyncRoot ?? Throw.InvalidOperationException<object>(Res.ArraySectionNull);
        bool ICollection.IsSynchronized => false;

        int ICollection.Count => length;
        bool IList.IsReadOnly => false;
        bool IList.IsFixedSize => true;

#if !(NET35 || NET40)
        int IReadOnlyCollection<TTo>.Count => length;
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
        /// <para>This member validates <paramref name="index"/> against <see cref="Length"/>. To allow getting/setting any reference from the actual underlying array use
        /// the <see cref="GetElementUnsafe">GetElementUnsafe</see>/<see cref="SetElementUnsafe">SetElementUnsafe</see> methods instead.</para>
        /// <para>If the compiler you use supports members that return a value by reference, you can also use the <see cref="GetElementReference">GetElementReference</see> method.</para>
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than zero or greater or equal to <see cref="Length"/>.</exception>
        /// <exception cref="NotSupportedException">.NET Framework only: you access this member in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        public TTo this[int index]
        {
            // NOTE: We could simply use just a ref returning indexer and implement IList<TTo>.this[int] explicitly,
            // but that may cause a "not supported by the language" error when this library is used by older compilers that try to access the indexer.
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                try
                {
                    return GetElementReference(index);
                }
                catch (VerificationException e) when (EnvironmentHelper.IsPartiallyTrustedDomain)
                {
                    return Throw.NotSupportedException<TTo>(Res.UnsafeSecuritySettingsConflict, e);
                }
#else
                return GetElementReference(index);
#endif
            }

            [MethodImpl(MethodImpl.AggressiveInlining)]
            set
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                try
                {
                    GetElementReference(index) = value;
                }
                catch (VerificationException e) when (EnvironmentHelper.IsPartiallyTrustedDomain)
                {
                    Throw.NotSupportedException(Res.UnsafeSecuritySettingsConflict, e);
                }
#else
                GetElementReference(index) = value;
#endif
            }
        }

        #endregion

        #region Explicitly Implemented Interface Indexers

        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                if (value == null)
                    Throw.ArgumentNullException(Argument.value);
                try
                {
                    this[index] = (TTo)value;
                }
                catch (InvalidCastException)
                {
                    Throw.ArgumentException(Argument.value, Res.ICollectionNonGenericValueTypeInvalid(value, typeof(TTo)));
                }
            }
        }

        #endregion

        #endregion

        #endregion

        #region Operators

        /// <summary>
        /// Performs an implicit conversion from array of <typeparamref name="TFrom"/> to <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="array">The array to be converted to a <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <returns>
        /// A <see cref="CastArray{TFrom,TTo}"/> instance that represents the original array cast to an array of <typeparamref name="TTo"/>.
        /// </returns>
        public static implicit operator CastArray<TFrom, TTo>(TFrom[]? array) => new CastArray<TFrom, TTo>(array.AsSection());

        /// <summary>
        /// Performs an implicit conversion from <see cref="ArraySegment{T}"/> to <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="arraySegment">The <see cref="ArraySegment{T}"/> to be converted to a <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <returns>
        /// A <see cref="CastArray{TFrom,TTo}"/> instance that represents the original <see cref="ArraySegment{T}"/> cast to an array of <typeparamref name="TTo"/>.
        /// </returns>
        public static implicit operator CastArray<TFrom, TTo>(ArraySegment<TFrom> arraySegment) => new CastArray<TFrom, TTo>(arraySegment.AsSection());

        /// <summary>
        /// Performs an implicit conversion from <see cref="ArraySection{T}"/> to <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="arraySection">The <see cref="ArraySection{T}"/> to be converted to a <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <returns>
        /// A <see cref="CastArray{TFrom,TTo}"/> instance that represents the original <see cref="ArraySection{T}"/> cast to an array of <typeparamref name="TTo"/>.
        /// </returns>
        public static implicit operator CastArray<TFrom, TTo>(ArraySection<TFrom> arraySection) => new CastArray<TFrom, TTo>(arraySection);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Performs an implicit conversion from <see cref="CastArray{TFrom,TTo}"/> to a <see cref="Span{T}"/> of element type <typeparamref name="TTo"/>.
        /// </summary>
        /// <param name="castArray">The <see cref="CastArray{TFrom,TTo}"/> to be converted to a <see cref="Span{T}"/>.</param>
        /// <returns>
        /// A <see cref="Span{T}"/> of element type <typeparamref name="TTo"/> that represents the specified <see cref="CastArray{TFrom,TTo}"/>.
        /// </returns>
        public static implicit operator Span<TTo>(CastArray<TFrom, TTo> castArray) => castArray.AsSpan;
#endif

        /// <summary>
        /// Determines whether two specified <see cref="CastArray{TFrom,TTo}"/> instances have the same value.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the equality check.</returns>
        public static bool operator ==(CastArray<TFrom, TTo> a, CastArray<TFrom, TTo> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified <see cref="CastArray{TFrom,TTo}"/> instances have different values.
        /// </summary>
        /// <param name="a">The left argument of the equality check.</param>
        /// <param name="b">The right argument of the equality check.</param>
        /// <returns>The result of the inequality check.</returns>
        public static bool operator !=(CastArray<TFrom, TTo> a, CastArray<TFrom, TTo> b) => !(a == b);

        #endregion

        #region Constructors

        #region Static Constructor

        static CastArray()
        {
            // It's not just for the .NET Framework:
            // The unmanaged constraint is just a C# thing for newer compilers but the CLR still allows using any struct (e.g. by reflection).
            if (Reflector<TFrom>.IsManaged)
                Throw.InvalidOperationException(Res.UnmanagedTypeArgumentExpected<TFrom>(typeof(CastArray<,>)));
            else if (Reflector<TTo>.IsManaged)
                Throw.InvalidOperationException(Res.UnmanagedTypeArgumentExpected<TTo>(typeof(CastArray<,>)));
        }

        #endregion

        #region Instance Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CastArray{TFrom,TTo}" /> struct from the specified <see cref="ArraySection{T}"/>.
        /// No heap allocation occurs when using this constructor.
        /// </summary>
        /// <param name="buffer">The <see cref="ArraySection{T}"/> to initialize the new <see cref="CastArray{TFrom,TTo}"/> from.</param>
        /// <exception cref="ArgumentException"><see cref="ArraySection{T}.Length"/> of <paramref name="buffer"/> is too big to express it as the length in <typeparamref name="TTo"/> elements.</exception>
        [SecuritySafeCritical]
        public unsafe CastArray(ArraySection<TFrom> buffer)
        {
            this.buffer = buffer;

            // Not "caching" the sizes into variables (or static fields) so in Release build the JIT compiler can eliminate the false branches.
            // Same size: the simplest case
            if (sizeof(TFrom) == sizeof(TTo))
            {
                length = buffer.Length;
                return;
            }

            // byte-sized source
            if (sizeof(TFrom) == 1)
            {
                length = buffer.Length / sizeof(TTo);
                return;
            }

            // Any size: it can happen that length in TOut is larger than Int32.MaxValue.
            // Though we could still support it by a long length and via the direct indexer only, Memory/Span conversions could not work so not allowing it.
            try
            {
                length = checked((int)((long)buffer.Length * sizeof(TFrom) / sizeof(TTo)));
            }
            catch (OverflowException e)
            {
                Throw.ArgumentException(Argument.buffer, Res.CastArrayBufferTooBigForCastLength(typeof(TTo)), e);
            }
        }

        #endregion

        #region Internal Constructors

        internal CastArray(ArraySection<TFrom> buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
        }

        #endregion

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Gets a new <see cref="CastArray{TFrom,TTo}"/> instance, which represents a subsection of the current instance with the specified <paramref name="startIndex"/>.
        /// Please note that the size of <typeparamref name="TTo"/> multiplied by <paramref name="startIndex"/> must be divisible by the size of <typeparamref name="TFrom"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first item of the returned section.</param>
        /// <returns>The subsection of the current <see cref="CastArray{TFrom,TTo}"/> instance with the specified <paramref name="startIndex"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is out of range.</exception>
        /// <exception cref="ArgumentException">The size of <typeparamref name="TTo"/> multiplied by <paramref name="startIndex"/> is not divisible by the size of <typeparamref name="TFrom"/>.</exception>
        /// <remarks>
        /// <note>If the size of <typeparamref name="TTo"/> multiplied by <paramref name="startIndex"/> is not divisible by the size of <typeparamref name="TFrom"/>,
        /// then this method throws an <see cref="ArgumentException"/>. If the targeted platform supports the <see cref="Span{T}"/> type and misaligned memory access,
        /// then you can try to use the <see cref="AsSpan"/> property and the <see cref="MemoryMarshal.Cast{TFrom,TTo}(Span{TFrom})">MemoryMarshal.Cast</see> method.</note>
        /// </remarks>
        public CastArray<TFrom, TTo> Slice(int startIndex) => Slice(startIndex, length - startIndex);

        /// <summary>
        /// Gets a new <see cref="CastArray{TFrom,TTo}"/> instance, which represents a subsection of the current instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.
        /// Please note that the size of <typeparamref name="TTo"/> multiplied by <paramref name="startIndex"/> must be divisible by the size of <typeparamref name="TFrom"/>.
        /// </summary>
        /// <param name="startIndex">The offset that points to the first item of the returned section.</param>
        /// <param name="length">The desired length of the returned section.</param>
        /// <returns>The subsection of the current <see cref="ArraySection{T}"/> instance with the specified <paramref name="startIndex"/> and <paramref name="length"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> or <paramref name="length"/> is out of range.</exception>
        /// <exception cref="ArgumentException">The size of <typeparamref name="TTo"/> multiplied by <paramref name="startIndex"/> is not divisible by the size of <typeparamref name="TFrom"/>.</exception>
        /// <remarks>
        /// <note>If the size of <typeparamref name="TTo"/> multiplied by <paramref name="startIndex"/> is not divisible by the size of <typeparamref name="TFrom"/>,
        /// then this method throws an <see cref="ArgumentException"/>. If the targeted platform supports the <see cref="Span{T}"/> type and misaligned memory access,
        /// then you can try to use the <see cref="AsSpan"/> property and the <see cref="MemoryMarshal.Cast{TFrom,TTo}(Span{TFrom})">MemoryMarshal.Cast</see> method.</note>
        /// </remarks>
        [SecuritySafeCritical]
        [SuppressMessage("ReSharper", "ParameterHidesMember", Justification = "Intended because it will be the new length of the returned instance")]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public unsafe CastArray<TFrom, TTo> Slice(int startIndex, int length)
        {
            // After this validation there is no need for overflow check like in the constructor because the new length can only be smaller than the original one.
            if ((uint)startIndex > (uint)this.length)
                Throw.ArgumentOutOfRangeException(Argument.startIndex);
            if ((uint)length > (uint)(this.length - startIndex))
                Throw.ArgumentOutOfRangeException(Argument.length);

            // Not "caching" the sizes into variables (or static fields) so in Release build the JIT compiler can eliminate the false branches.
            // Same size: the simplest case
            if (sizeof(TFrom) == sizeof(TTo))
                return new CastArray<TFrom, TTo>(buffer.Slice(startIndex, length), length);

            // 1 byte from size: any offset will work
            if (sizeof(TFrom) == 1)
                return new CastArray<TFrom, TTo>(buffer.Slice(startIndex * sizeof(TTo), length * sizeof(TTo)), length);

            // Here we need to validate the alignment
            long byteOffset = sizeof(TTo) * startIndex;
            if (byteOffset % sizeof(TFrom) != 0)
                Throw.ArgumentException(Argument.startIndex, Res.CastArraySliceWrongStartIndex(startIndex, typeof(TFrom), typeof(TTo)));
            return new CastArray<TFrom, TTo>(buffer.Slice((int)(byteOffset / sizeof(TFrom)), (int)((long)length * sizeof(TTo) / sizeof(TFrom))), length);
        }

        /// <summary>
        /// Gets this <see cref="CastArray{TFrom,TTo}"/> as an <see cref="CastArray2D{TFrom,TTo}"/> instance
        /// using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// The <see cref="CastArray2D{TFrom,TTo}"/> must have enough capacity for the specified dimensions.
        /// </summary>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance using this <see cref="CastArray{TFrom,TTo}"/> as its underlying buffer that has the specified dimensions.</returns>
        public CastArray2D<TFrom, TTo> As2D(int height, int width) => new CastArray2D<TFrom, TTo>(this, height, width);

        /// <summary>
        /// Gets this <see cref="CastArray{TFrom,TTo}"/> as an <see cref="CastArray2D{TFrom,TTo}"/> instance
        /// using the specified <paramref name="height"/> and <paramref name="width"/>.
        /// The <see cref="CastArray2D{TFrom,TTo}"/> must have enough capacity for the specified dimensions.
        /// </summary>
        /// <param name="depth">The depth of the array to be returned.</param>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance using this <see cref="CastArray{TFrom,TTo}"/> as its underlying buffer that has the specified dimensions.</returns>
        public CastArray3D<TFrom, TTo> As3D(int depth, int height, int width) => new CastArray3D<TFrom, TTo>(this, depth, height, width);

        /// <summary>
        /// Reinterprets the <typeparamref name="TTo"/> type of this <see cref="CastArray{TFrom,TTo}"/> to type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The new desired type instead of the original <typeparamref name="TTo"/>.</typeparam>
        /// <returns>A <see cref="CastArray{TFrom,TTo}"/> instance where original <typeparamref name="TTo"/> type is replaced to <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <para>If the size of <typeparamref name="T"/> cannot be divided by the size of <typeparamref name="TFrom"/>,
        /// then the cast result may not cover the whole original <see cref="Buffer"/> to prevent exceeding beyond the original bounds.</para>
        /// </remarks>
        public CastArray<TFrom, T> Cast<T>()
#if NETFRAMEWORK
            where T : struct
#else
            where T : unmanaged
#endif
        {
            return buffer.Cast<TFrom, T>();
        }

        /// <summary>
        /// Reinterprets this <see cref="CastArray{TFrom,TTo}"/> as a two-dimensional <see cref="CastArray2D{TFrom,TTo}"/> struct,
        /// casting the <typeparamref name="TTo"/> type of this <see cref="CastArray{TFrom,TTo}"/> to type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The new desired type instead of the original <typeparamref name="TTo"/>.</typeparam>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>A <see cref="CastArray2D{TFrom,TTo}"/> instance where original <typeparamref name="TTo"/> type is replaced to <typeparamref name="T"/>.</returns>
        public CastArray2D<TFrom, T> Cast2D<T>(int height, int width)
#if NETFRAMEWORK
            where T : struct
#else
            where T : unmanaged
#endif
        {
            return buffer.Cast2D<TFrom, T>(height, width);
        }

        /// <summary>
        /// Reinterprets this <see cref="CastArray{TFrom,TTo}"/> as a three-dimensional <see cref="CastArray3D{TFrom,TTo}"/> struct,
        /// casting the <typeparamref name="TTo"/> type of this <see cref="CastArray{TFrom,TTo}"/> to type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The new desired type instead of the original <typeparamref name="TTo"/>.</typeparam>
        /// <param name="depth">The depth of the array to be returned.</param>
        /// <param name="height">The height of the array to be returned.</param>
        /// <param name="width">The width of the array to be returned.</param>
        /// <returns>A <see cref="CastArray3D{TFrom,TTo}"/> instance where original <typeparamref name="TTo"/> type is replaced to <typeparamref name="T"/>.</returns>
        public CastArray3D<TFrom, T> Cast3D<T>(int depth, int height, int width)
#if NETFRAMEWORK
            where T : struct
#else
            where T : unmanaged
#endif
        {
            return buffer.Cast3D<TFrom, T>(depth, height, width);
        }

        /// <summary>
        /// Gets the reference to the element at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the element to get the reference for.</param>
        /// <returns>The reference to the element at the specified index.</returns>
        /// <remarks>
        /// <para>This method validates <paramref name="index"/> against <see cref="Length"/>.
        /// To allow getting any reference from the actual underlying array
        /// use the <see cref="GetElementReferenceUnsafe">GetElementReferenceUnsafe</see> method instead.</para>
        /// <note>This method returns a value by reference. If this library is used by an older compiler that does not support such members,
        /// use the <see cref="this">indexer</see> instead.</note>
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than zero or greater or equal to <see cref="Length"/>.</exception>
        /// <exception cref="VerificationException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public ref TTo GetElementReference(int index)
        {
            if ((uint)index >= (uint)length)
                Throw.IndexOutOfRangeException();

            // There is no point in putting the try...catch (VerificationException) here. When it's thrown, it's already for this method as it has a ref return.
            return ref GetElementReferenceInternal(index);
        }

        /// <summary>
        /// Gets the element at the specified <paramref name="index"/> without any range check.
        /// To validate <paramref name="index"/> against <see cref="Length"/> use the <see cref="this">indexer</see> instead.
        /// </summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> returns <see langword="true"/>.</exception>
        /// <exception cref="NotSupportedException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        /// <remarks>
        /// <note type="caution">You must ensure that <paramref name="index"/> falls within <see cref="Length"/>, or at least in the bounds
        /// of the actual underlying array. Attempting to access protected memory may crash the runtime.</note>
        /// <para>If the compiler you use supports members that return a value by reference, you can also use
        /// the <see cref="GetElementReferenceUnsafe">GetElementReferenceUnsafe</see> method.</para>
        /// </remarks>
        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TTo GetElementUnsafe(int index)
        {
#if NETFRAMEWORK || NETSTANDARD2_0
            try
            {
                return GetElementReferenceUnsafe(index);
            }
            catch (VerificationException e) when (EnvironmentHelper.IsPartiallyTrustedDomain)
            {
                return Throw.NotSupportedException<TTo>(Res.UnsafeSecuritySettingsConflict, e);
            }
#else
            return GetElementReferenceUnsafe(index);
#endif
        }

        /// <summary>
        /// Sets the element at the specified <paramref name="index"/> without any range check.
        /// To validate <paramref name="index"/> against <see cref="Length"/> use the <see cref="this">indexer</see> instead.
        /// </summary>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> returns <see langword="true"/>.</exception>
        /// <exception cref="NotSupportedException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        /// <remarks>
        /// <note type="caution">You must ensure that <paramref name="index"/> falls within <see cref="Length"/>, or at least in the bounds
        /// of the actual underlying array. Attempting to access protected memory may crash the runtime.</note>
        /// <para>If the compiler you use supports members that return a value by reference, you can also use
        /// the <see cref="GetElementReferenceUnsafe">GetElementReferenceUnsafe</see> method.</para>
        /// </remarks>
        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetElementUnsafe(int index, TTo value)
        {
#if NETFRAMEWORK || NETSTANDARD2_0
            try
            {
                GetElementReferenceUnsafe(index) = value;
            }
            catch (VerificationException e) when (EnvironmentHelper.IsPartiallyTrustedDomain)
            {
                Throw.NotSupportedException(Res.UnsafeSecuritySettingsConflict, e);
            }
#else
            GetElementReferenceUnsafe(index) = value;
#endif
        }

        /// <summary>
        /// Gets the reference to the element at the specified <paramref name="index"/> without any range check.
        /// To validate <paramref name="index"/> against <see cref="Length"/> use the <see cref="GetElementReference">GetElementReference</see> method instead.
        /// </summary>
        /// <param name="index">The index of the element to get the reference for.</param>
        /// <returns>The reference to the element at the specified index.</returns>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> returns <see langword="true"/>.</exception>
        /// <exception cref="VerificationException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        /// <remarks>
        /// <note type="caution">You must ensure that <paramref name="index"/> falls within <see cref="Length"/>, or at least in the bounds
        /// of the actual underlying array. Attempting to access protected memory may crash the runtime.</note>
        /// <note>This method returns a value by reference. If this library is used by an older compiler that does not support such members,
        /// use the <see cref="GetElementUnsafe">GetElementUnsafe</see>/<see cref="SetElementUnsafe">SetElementUnsafe</see> methods instead.</note>
        /// </remarks>
        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public ref TTo GetElementReferenceUnsafe(int index)
        {
            if (IsNullOrEmpty)
                Throw.InvalidOperationException(Res.CollectionEmpty);

#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.Add(ref Unsafe.As<TFrom, TTo>(ref buffer.GetElementReferenceInternal(0)), index);
#else
            // There is no point in putting the try...catch (VerificationException) here. When it's thrown, it's already for this method as it has a ref return.
            return ref GetElementReferenceInternal(index);
#endif
        }

        /// <summary>
        /// Returns a reference to the first element in this <see cref="CastArray{TFrom,TTo}"/>.
        /// This makes possible to use the <see cref="CastArray{TFrom,TTo}"/> in a <see langword="fixed"/> statement.
        /// </summary>
        /// <returns>A reference to the first element in this <see cref="ArraySection{T}"/>.</returns>
        /// <exception cref="InvalidOperationException"><see cref="IsNullOrEmpty"/> is <see langword="true"/>.</exception>
        /// <exception cref="VerificationException">.NET Framework only: you execute this method in a partially trusted <see cref="AppDomain"/> that does not allow executing unverifiable code.</exception>
        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public ref TTo GetPinnableReference()
        {
            if (IsNullOrEmpty)
                Throw.InvalidOperationException(Res.CollectionEmpty);

#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.As<TFrom, TTo>(ref buffer.GetElementReferenceInternal(0));
#else
            // There is no point in putting the try...catch (VerificationException) here. When it's thrown, it's already for this method as it has a ref return.
            // Besides, this method is to use this instance in a fixed statement, which cannot be used in such a partially trusted domain anyway.
            return ref GetElementReferenceInternal(0);
#endif
        }

        /// <summary>
        /// Copies the elements of this <see cref="CastArray{TFrom,TTo}"/> to a new array of element type <typeparamref name="TTo"/>.
        /// </summary>
        /// <returns>An array of element type <typeparamref name="TTo"/> containing copies of the elements of this <see cref="CastArray{TFrom,TTo}"/>,
        /// or <see langword="null"/> if <see cref="IsNull"/> is <see langword="true"/>.</returns>
        [SecuritySafeCritical]
        public TTo[]? ToArray()
        {
            if (IsNullOrEmpty)
                return IsNull ? null : Reflector.EmptyArray<TTo>();

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return AsSpan.ToArray();
#elif NETFRAMEWORK || NETSTANDARD2_0
            try
            {
                TTo[] result = new TTo[length];
                DoCopyToUnsafe(ref result[0]);
                return result;
            }
            catch (VerificationException e) when (EnvironmentHelper.IsPartiallyTrustedDomain)
            {
                return Throw.NotSupportedException<TTo[]?>(Res.UnsafeSecuritySettingsConflict, e);
            }
#else
            TTo[] result = new TTo[length];
            DoCopyToUnsafe(ref result[0]);
            return result;
#endif
        }

        /// <summary>
        /// Clears the items in this <see cref="CastArray{TFrom,TTo}"/> instance so all elements will have the default value of type <typeparamref name="TTo"/>.
        /// </summary>
        public void Clear() => buffer.Clear();

        /// <summary>
        /// Determines the index of a specific item in this <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        [SecuritySafeCritical]
        public int IndexOf(TTo item)
        {
            // TODO: AsSpan.IndexOf, when TTo is IEquatable will be available: https://github.com/dotnet/csharplang/discussions/6308#discussioncomment-3212915
            //if (TTo is IComparable TComparable)
            //    return AsSpan.IndexOf<TComparable>(item);

            // Needed explicitly if we use GetPinnableReference or GetElementReferenceInternal
            if (IsNullOrEmpty)
                return -1;

#if NET5_0_OR_GREATER
            // Using the EqualityComparer<T>.Default intrinsic directly, which gets devirtualized
            // See https://github.com/dotnet/runtime/issues/10050
            ref TTo current = ref Unsafe.As<TFrom, TTo>(ref buffer.GetElementReferenceInternal(0));
            for (int i = 0; i < length; i++)
            {
                if (EqualityComparer<TTo>.Default.Equals(Unsafe.Add(ref current, i), item))
                    return i;
            }

            return -1;
#elif NETCOREAPP3_0_OR_GREATER
            var comparer = ComparerHelper<TTo>.EqualityComparer;
            ref TTo current = ref Unsafe.As<TFrom, TTo>(ref buffer.GetElementReferenceInternal(0));
            for (int i = 0; i < length; i++)
            {
                if (comparer.Equals(Unsafe.Add(ref current, i), item))
                    return i;
            }

            return -1;
#elif NETFRAMEWORK || NETSTANDARD2_0
            try
            {
                return DoIndexOfUnsafe(item);
            }
            catch (VerificationException e) when (EnvironmentHelper.IsPartiallyTrustedDomain)
            {
                return Throw.NotSupportedException<int>(Res.UnsafeSecuritySettingsConflict, e);
            }
#else
            return DoIndexOfUnsafe(item);
#endif
        }

        /// <summary>
        /// Determines whether this <see cref="CastArray{TFrom,TTo}"/> contains the specific <paramref name="item"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> is found in this <see cref="CastArray{TFrom,TTo}"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="item">The object to locate in this <see cref="CastArray{TFrom,TTo}"/>.</param>
        public bool Contains(TTo item) => IndexOf(item) >= 0;

        /// <summary>
        /// Copies the items of this <see cref="CastArray{TFrom,TTo}"/> to a compatible one-dimensional array, starting at a particular index.
        /// </summary>
        /// <param name="target">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from this <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <param name="targetIndex">The zero-based index in <paramref name="target"/> at which copying begins. This parameter is optional.
        /// <br/>Default value: 0.</param>
        [SecuritySafeCritical]
        public void CopyTo(TTo[] target, int targetIndex = 0)
        {
            if (target == null!)
                Throw.ArgumentNullException(Argument.target);
            if (targetIndex < 0 || targetIndex > target.Length)
                Throw.ArgumentOutOfRangeException(Argument.targetIndex);

            if (target.Length - targetIndex < length)
                Throw.ArgumentException(Argument.target, Res.ICollectionCopyToDestArrayShort);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            AsSpan.CopyTo(target.AsSpan(targetIndex));
#elif NETFRAMEWORK || NETSTANDARD2_0
            if (IsNullOrEmpty)
                return;

            try
            {
                DoCopyToUnsafe(ref target[targetIndex]);
            }
            catch (VerificationException e) when (EnvironmentHelper.IsPartiallyTrustedDomain)
            {
                Throw.NotSupportedException(Res.UnsafeSecuritySettingsConflict, e);
            }
#else
            if (IsNullOrEmpty)
                return;

            DoCopyToUnsafe(ref target[targetIndex]);
#endif
        }

        /// <summary>
        /// Copies the items of this <see cref="CastArray{TFrom,TTo}"/> to a compatible <see cref="ArraySection{T}"/>, starting at a particular index.
        /// </summary>
        /// <param name="target">The <see cref="ArraySection{T}"/> that is the destination of the elements copied from this <see cref="CastArray{TFrom,TTo}"/>.</param>
        /// <param name="targetIndex">The zero-based index in <paramref name="target"/> at which copying begins. This parameter is optional.
        /// <br/>Default value: 0.</param>
        public void CopyTo(ArraySection<TTo> target, int targetIndex = 0) => CopyTo(target.UnderlyingArray!, targetIndex + target.Offset);

        /// <summary>
        /// Copies the items of this <see cref="CastArray{TFrom,TTo}"/> to a compatible instance, starting at a particular index.
        /// </summary>
        /// <param name="target">The <see cref="CastArray{TFrom,TTo}"/> that is the destination of the elements copied from this instance.</param>
        /// <param name="targetIndex">The zero-based index in <paramref name="target"/> at which copying begins. This parameter is optional.
        /// <br/>Default value: 0.</param>
        [SecuritySafeCritical]
        public void CopyTo(CastArray<TFrom, TTo> target, int targetIndex = 0)
        {
            if (targetIndex < 0 || targetIndex > target.length)
                Throw.ArgumentOutOfRangeException(Argument.targetIndex);

            if (target.Length - targetIndex < length)
                Throw.ArgumentException(Argument.target, Res.ICollectionCopyToDestArrayShort);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            AsSpan.CopyTo(target.AsSpan.Slice(targetIndex));
#elif NETFRAMEWORK || NETSTANDARD2_0
            if (IsNullOrEmpty)
                return;

            try
            {
                DoCopyToUnsafe(ref target.GetElementReferenceInternal(targetIndex));
            }
            catch (VerificationException e) when (EnvironmentHelper.IsPartiallyTrustedDomain)
            {
                Throw.NotSupportedException(Res.UnsafeSecuritySettingsConflict, e);
            }
#else
            if (IsNullOrEmpty)
                return;

            DoCopyToUnsafe(ref target.UnsafeGetRef(targetIndex));
#endif
        }

        /// <summary>
        /// Returns an enumerator that iterates through the items of this <see cref="CastArray{TFrom,TTo}"/>.
        /// </summary>
        /// <returns>A <see cref="CastArrayEnumerator{TFrom,TTo}"/> instance that can be used to iterate though the elements of this <see cref="CastArray{TFrom,TTo}"/>.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public CastArrayEnumerator<TFrom, TTo> GetEnumerator() => new CastArrayEnumerator<TFrom, TTo>(this);

        /// <summary>
        /// Indicates whether the current <see cref="CastArray{TFrom,TTo}"/> instance is equal to another one specified in the <paramref name="other"/> parameter.
        /// That is, when they have the same <see cref="Length"/>, and they both reference the same section of the same underlying array.
        /// </summary>
        /// <param name="other">An <see cref="ArraySection{T}"/> instance to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
        public bool Equals(CastArray<TFrom, TTo> other) => length == other.length && buffer == other.buffer;

        /// <summary>
        /// Determines whether the specified <see cref="object">object</see> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if the specified object is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => obj is CastArray<TFrom, TTo> other && Equals(other);

        /// <summary>
        /// Returns a hash code for this <see cref="CastArray{TFrom,TTo}"/> instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            if (buffer.IsNull)
                return 0;
            return (buffer, length).GetHashCode();
        }

        #endregion

        #region Internal Methods

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal ref TTo GetElementReferenceInternal(int index)
        {
#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.Add(ref Unsafe.As<TFrom, TTo>(ref buffer.GetElementReferenceInternal(0)), index);
#else
            unsafe
            {
                fixed (TFrom* pBuf = &buffer.GetElementReferenceInternal(0))
                    return ref ((TTo*)pBuf)[index];
            }
#endif
        }

        #endregion

        #region Private Methods

#if !(NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private unsafe void DoCopyToUnsafe(ref TTo target)
        {
            fixed (void* pSrc = &buffer.GetElementReferenceInternal(0))
            fixed (TTo* pDst = &target)
                MemoryHelper.CopyMemory(pSrc, pDst, (long)length * sizeof(TTo));
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private unsafe int DoIndexOfUnsafe(TTo item)
        {
            var comparer = ComparerHelper<TTo>.EqualityComparer;
            fixed (TFrom* ptr = &buffer.GetElementReferenceInternal(0))
            {
                TTo* pTo = (TTo*)ptr;
                for (int i = 0; i < length; i++)
                {
                    if (comparer.Equals(pTo[i], item))
                        return i;
                }
            }

            return -1;
        }

#endif

        #endregion

        #region Explicitly Implemented Interface Methods

        void IList<TTo>.Insert(int index, TTo item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        void IList<TTo>.RemoveAt(int index) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);

        void ICollection<TTo>.Add(TTo item) => Throw.NotSupportedException(Res.ICollectionReadOnlyModifyNotSupported);
        bool ICollection<TTo>.Remove(TTo item) => Throw.NotSupportedException<bool>(Res.ICollectionReadOnlyModifyNotSupported);

        IEnumerator<TTo> IEnumerable<TTo>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IList.IndexOf(object? value)
        {
            if (value == null)
                Throw.ArgumentNullException(Argument.value);
            return IndexOf((TTo)value);
        }

        bool IList.Contains(object? value)
        {
            if (value == null)
                Throw.ArgumentNullException(Argument.value);
            return Contains((TTo)value);
        }

        void ICollection.CopyTo(Array targetArray, int index)
        {
            if (targetArray == null!)
                Throw.ArgumentNullException(Argument.array);

            if (targetArray is TTo[] typedArray)
            {
                CopyTo(typedArray, index);
                return;
            }

            if (index < 0 || index > length)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (targetArray.Length - index < length)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToDestArrayShort);
            if (targetArray.Rank != 1)
                Throw.ArgumentException(Argument.array, Res.ICollectionCopyToSingleDimArrayOnly);

            if (targetArray is object?[] objectArray)
            {
                for (int i = 0; i < length; i++)
                {
                    objectArray[index] = GetElementReferenceInternal(i);
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

        #endregion

        #endregion
    }
}
