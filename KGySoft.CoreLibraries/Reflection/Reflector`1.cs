#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Reflector`1.cs
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
using System.Security;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
using System.Runtime.CompilerServices;
#endif

#if !NETCOREAPP3_0_OR_GREATER
using KGySoft.CoreLibraries;
#endif

#endregion

namespace KGySoft.Reflection
{
    internal static class Reflector<T>
    {
        #region Nested Classes

        #region EmptyArrayCache class
#if NET35 || NET40 || NET45

        private static class EmptyArrayCache
        {
            #region Fields

            internal static readonly T[] Value = new T[0];

            #endregion
        }

#endif
        #endregion

        #region SizeOfCache
#if !NETCOREAPP3_0_OR_GREATER

        [SecuritySafeCritical]
        private static class SizeOfCache
        {
            #region Fields

            // ReSharper disable once StaticMemberInGenericType - false alarm, value depends on T
            internal static readonly int Value = Initialize();

            #endregion
            
            #region Methods
            
            private static unsafe int Initialize()
            {
                if (!typeof(T).IsValueType)
                    return IntPtr.Size;

                if (typeof(T).IsPrimitive)
                    return Buffer.ByteLength(new T[1]);

                if (!Reflector.CanUseTypedReference)
                    return typeof(T).SizeOf();

                // We can't use stackalloc because T is not constrained here so we need to create an array
                var items = new T[2];

                // In .NET Core 3+ we could use Unsafe.ByteOffset for ref items[0]/[1] (if there wasn't Unsafe.SizeOf in the first place), which is not available here.
                // So we need to pin the array and use unmanaged pointers. Not using the slow GCHandle.Alloc, which throws an exception for non-blittable types anyway.
                TypedReference arrayReference = __makeref(items);
                while (true)
                {
                    byte* unpinnedAddress = Reflector.GetReferencedDataAddress(arrayReference);
                    ref byte asRef = ref *unpinnedAddress;
                    fixed (byte* pinnedAddress = &asRef)
                    {
                        // If GC has relocated the array in the meantime, then trying again
                        if (pinnedAddress != Reflector.GetReferencedDataAddress(arrayReference))
                            continue;

                        // Now we can safely obtain the address of the pinned items. We can't use T* here so using typed references again.
                        return (int)(Reflector.GetValueAddress(__makeref(items[1])) - Reflector.GetValueAddress(__makeref(items[0])));
                    }
                }
            }

            #endregion
        }

#endif
        #endregion

        #region IsManagedCache
#if NETFRAMEWORK || NETSTANDARD2_0

        private static class IsManagedCache
        {
            #region Fields

            internal static readonly bool Value = typeof(T).IsManaged();

            #endregion
        }

#endif
        #endregion

        #endregion

        #region Properties

        internal static T[] EmptyArray =>
#if NET35 || NET40 || NET45
            EmptyArrayCache.Value;
#else
            Array.Empty<T>();
#endif

        internal static int SizeOf =>
#if NETCOREAPP3_0_OR_GREATER
            Unsafe.SizeOf<T>();
#else
            SizeOfCache.Value;
#endif

        internal static bool IsManaged =>
#if NETFRAMEWORK || NETSTANDARD2_0
            IsManagedCache.Value;
#else
            RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#endif

        #endregion
    }
}
