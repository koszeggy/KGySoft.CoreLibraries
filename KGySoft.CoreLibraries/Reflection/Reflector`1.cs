#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Reflector`1.cs
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

using System;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "False alarm, fields depend on T")]
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

                if (IsPrimitive)
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

        #region ArrayInfoCache

        private static class ArrayElementSizeExponentCache
        {
            #region Fields

            internal static readonly int Value = IsPrimitive ? (int)Math.Log(SizeOf, 2) : 0;

            #endregion
        }

        #endregion

        #region IsPrimitiveCache
#if !NET9_0_OR_GREATER

        private static class IsPrimitiveCache
        {
            #region Fields

            internal static readonly bool Value = typeof(T).IsPrimitive;

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
#pragma warning disable IDE0301 // Use collection expression syntax
            Array.Empty<T>();
#pragma warning restore IDE0301 // Use collection expression syntax
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
        internal static int MaxArrayLength => typeof(T) == typeof(byte) ? 0x7FFFFFC7 : 0x7FEFFFFF;
#endif

        internal static int ArrayElementSizeExponent => ArrayElementSizeExponentCache.Value;

#if NET9_0_OR_GREATER
        internal static bool IsPrimitive => typeof(T).IsPrimitive; // intrinsic in .NET 9+
#else
        internal static bool IsPrimitive => IsPrimitiveCache.Value;
#endif

        #endregion
    }
}
