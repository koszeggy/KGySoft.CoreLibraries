#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Reflector`1.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
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
#if NETFRAMEWORK || NETSTANDARD2_0
using System.Linq;
using System.Reflection;
# endif
using System.Runtime.CompilerServices;

#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1
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
#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1

        private static class SizeOfCache
        {
            #region Fields

            internal static readonly int Value = typeof(T).IsPrimitive
                ? Buffer.ByteLength(new T[1])
                : typeof(T).SizeOf();

            #endregion
        }

#endif
        #endregion

        #region IsManagedCache
#if NETFRAMEWORK || NETSTANDARD2_0

        private static class IsManagedCache
        {
            #region Fields

            internal static readonly bool Value = HasReference(typeof(T));

            #endregion

            #region Methods

            private static bool HasReference(Type type)
            {
                if (!type.IsValueType)
                    return true;
                if (type.IsPrimitive || type.IsPointer || type.IsEnum)
                    return false;
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                return fields.Any(f => HasReference(f.FieldType));
            }

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
#if NETFRAMEWORK || NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD2_1
            SizeOfCache.Value;
#else
            Unsafe.SizeOf<T>();
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
