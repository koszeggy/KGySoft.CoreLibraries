#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RefExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2025 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Runtime.CompilerServices;
#if !NETCOREAPP3_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Security;

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

        #endregion
    }
}
