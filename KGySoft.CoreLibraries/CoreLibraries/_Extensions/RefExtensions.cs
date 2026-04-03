#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RefExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Runtime.CompilerServices;
using System.Security;

#if !NETCOREAPP3_0_OR_GREATER
using KGySoft.Reflection;
#endif

#endregion

#region Suppressions

#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class RefExtensions
    {
        #region Methods

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static ref TTarget As<TSource, TTarget>(this ref TSource source)
            where TSource : struct
            where TTarget : struct
        {
#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.As<TSource, TTarget>(ref source);
#else
            unsafe
            {
                fixed (TSource* p = &source)
                    return ref *(TTarget*)p;
            }
#endif
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static T As<T>(this object source)
            where T : class
        {
#if NETCOREAPP3_0_OR_GREATER
            return Unsafe.As<T>(source);
#else
            // This does not compile (causes CS0213), though I don't believe the reasoning is valid (source is already fixed). If it was true, no pinning would be required for an array either.
            //unsafe
            //{
            //    fixed (void* p = &source)
            //        return *(T*)p;
            //}

            // It compiles with latest compilers (not sure why, I don't think it shouldn't), though without pinning there is a chance for GC to move the source object during the operation.
            //unsafe { return *(T*)&source;}

            // The actually working version with pinning. Essentially the same as the following lines with a ReferenceHolder<T> struct with a T Value field - In SharpLab both produce the same JIT-ed code (at least in .NET 8+):
            //var holder = new ReferenceHolder<object> { Value = source };
            //return holder.As<ReferenceHolder<object>, ReferenceHolder<T>>().Value;
            ref object refSource = ref source;
            unsafe
            {
                fixed (object* p = &refSource)
                    return *(T*)p;
            }
#endif
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static ref T AsRef<T>(ref readonly T source)
            where T : struct
        {
#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.AsRef(in source);
#else
            unsafe
            {
                fixed (T* p = &source)
                    return ref *p;
            }
#endif
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static unsafe void* AsPointer<TSource>(this ref TSource source)
            where TSource : unmanaged
        {
#if NETCOREAPP3_0_OR_GREATER
            return Unsafe.AsPointer(ref source);
#else
            fixed (TSource* p = &source)
                return p;
#endif
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static ref T At<T>(this ref T source, int index)
            where T : unmanaged
        {
#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.Add(ref source, index);
#else
            unsafe
            {
                fixed (T* p = &source)
                    return ref p[index];
            }
#endif
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static ref TTarget At<TSource, TTarget>(this ref TSource source, int targetIndex)
            where TSource : struct
            where TTarget : struct
        {
#if NETCOREAPP3_0_OR_GREATER
            return ref Unsafe.Add(ref Unsafe.As<TSource, TTarget>(ref source), targetIndex);
#else
            unsafe
            {
                fixed (TSource* p = &source)
                    return ref ((TTarget*)p)[targetIndex];
            }
#endif
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static T ReadUnaligned<T>(this ref byte source)
            where T : struct
        {
#if NETCOREAPP3_0_OR_GREATER
            return Unsafe.ReadUnaligned<T>(ref source);
#else
            unsafe
            {
                fixed (byte* p = &source)
                {
                    // Happy path: source is properly aligned for T (in release build the false branches will be eliminated)
                    if (sizeof(T) == 1 || !Reflector<T>.IsPrimitive
                        || sizeof(T) == 2 && ((nint)p & 1) == 0
                        || sizeof(T) == 4 && ((nint)p & 3) == 0
                        || sizeof(T) == 8 && ((nint)p & MemoryHelper.PointerSizeMask) == 0)
                    {
                        return *(T*)p;
                    }

                    // T is an unaligned primitive type here. Primitive types have to be properly aligned to avoid possible DataMisalignedException on some architectures (e.g. ARM)
                    // See also ECMA-335 I.12.6.2 at https://github.com/stakx/ecma-335/blob/master/docs/i.12.6.2-alignment.md
                    byte* result = stackalloc byte[sizeof(T)]; // up to 8 bytes
                    for (int i = 0; i < sizeof(T); i++) // we could use MemoryHelper.CopyMemory(p, result, sizeof(T)); but for up to 8 bytes it would just be slower, and we already know that the data is misaligned here
                        result[i] = p[i];
                    return *(T*)result;
                }
            }
#endif
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static TTarget ReadUnalignedAt<TSource, TTarget>(this ref TSource source, int targetIndex)
            where TSource : struct
            where TTarget : struct
        {
#if NETCOREAPP3_0_OR_GREATER
            // Same as return source.At<TSource, TTarget>(targetIndex).As<TTarget, byte>().ReadUnaligned<TTarget>() inlined:
            return Unsafe.ReadUnaligned<TTarget>(ref Unsafe.As<TTarget, byte>(ref Unsafe.Add(ref Unsafe.As<TSource, TTarget>(ref source), targetIndex)));
#else
            // We could just use return source.At<TSource, TTarget>(targetIndex).As<TTarget, byte>().ReadUnaligned<TTarget>(), but this way we use only one pinning instead of three:
            unsafe
            {
                fixed (TSource* pSrc = &source)
                {
                    // Happy path: the target address is properly aligned for TTarget (in release build the false branches will be eliminated)
                    byte* addr = (byte*)(((TTarget*)pSrc) + targetIndex);
                    if (sizeof(TTarget) == 1 || !Reflector<TTarget>.IsPrimitive
                        || sizeof(TTarget) == 2 && ((nint)addr & 1) == 0
                        || sizeof(TTarget) == 4 && ((nint)addr & 3) == 0
                        || sizeof(TTarget) == 8 && ((nint)addr & MemoryHelper.PointerSizeMask) == 0)
                    {
                        return *(TTarget*)addr;
                    }

                    byte* result = stackalloc byte[sizeof(TTarget)]; // up to 8 bytes
                    for (int i = 0; i < sizeof(TTarget); i++) // we could use MemoryHelper.CopyMemory(addr, result, sizeof(TTarget)); but for up to 8 bytes it would just be slower, and we already know that the data is misaligned here
                        result[i] = addr[i];
                    return *(TTarget*)result;
                }
            }
#endif
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static void WriteUnalignedAt<TSource, TTarget>(this ref TSource source, int targetIndex, TTarget value)
            where TSource : struct
            where TTarget : struct
        {
#if NETCOREAPP3_0_OR_GREATER
            // Same as Unsafe.WriteUnaligned(source.At<TSource, TTarget>(targetIndex).As<TTarget, byte>(), value) inlined:
            Unsafe.WriteUnaligned(ref Unsafe.As<TTarget, byte>(ref Unsafe.Add(ref Unsafe.As<TSource, TTarget>(ref source), targetIndex)), value);
#else
            unsafe
            {
                fixed (TSource* pSrc = &source)
                {
                    // Happy path: the target address is properly aligned for TTarget (in release build the false branches will be eliminated)
                    byte* addr = (byte*)(((TTarget*)pSrc) + targetIndex);
                    if (sizeof(TTarget) == 1 || !Reflector<TTarget>.IsPrimitive
                        || sizeof(TTarget) == 2 && ((nint)addr & 1) == 0
                        || sizeof(TTarget) == 4 && ((nint)addr & 3) == 0
                        || sizeof(TTarget) == 8 && ((nint)addr & MemoryHelper.PointerSizeMask) == 0)
                    {
                        *(TTarget*)addr = value;
                    }

                    byte* pValue = (byte*)&value;
                    for (int i = 0; i < sizeof(TTarget); i++) // we could use MemoryHelper.CopyMemory(pValue, addr, sizeof(TTarget)); but for up to 8 bytes it would just be slower, and we already know that the data is misaligned here
                        addr[i] = pValue[i];
                }
            }
#endif
        }

        #endregion
    }
}
