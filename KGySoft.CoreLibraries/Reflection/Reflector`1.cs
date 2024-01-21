#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Reflector`1.cs
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
using System.Runtime.CompilerServices;
#endif
#if !NETCOREAPP3_0_OR_GREATER
using System.Security;
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
            internal static readonly int Value =
#if NETFRAMEWORK || NETSTANDARD2_0
                EnvironmentHelper.IsPartiallyTrustedDomain ? typeof(T).SizeOf() : Initialize();
#else
                Initialize(); 
#endif

            #endregion

            #region Methods

            [SecurityCritical]
            private static unsafe int Initialize()
            {
                if (!typeof(T).IsValueType)
                    return IntPtr.Size;

                if (typeof(T).IsPrimitive)
                    return Buffer.ByteLength(new T[1]);

                // We can't use stackalloc because T is not constrained here so we need to create an array
                var items = new T[2];

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                // pinning the array and getting the distance between the items, in bytes
                fixed (T* pinnedItems = items)
                    return (int)((byte*)&pinnedItems[1] - (byte*)&pinnedItems[0]);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
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

#if NET6_0_OR_GREATER
        internal static int MaxArrayLength => Array.MaxLength;
#else
        // Based on the internal Array.MaxArrayLength and MaxByteArrayLength constants
        internal static int MaxArrayLength { get; } = typeof(T) == Reflector.ByteType ? 0x7FFFFFC7 : 0x7FEFFFFF;
#endif

        #endregion
    }
}
