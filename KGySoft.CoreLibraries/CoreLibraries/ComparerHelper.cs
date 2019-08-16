#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ComparerHelper.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

#if NET40 || NET45
using System;
# endif
using System.Collections.Generic;

#if NET40 || NET45
using KGySoft.Reflection;
# endif

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class ComparerHelper<T>
    {
        #region Fields

        private static IEqualityComparer<T> equalityComparer;
        private static IComparer<T> comparer;

        #endregion

        #region Properties

        internal static IEqualityComparer<T> EqualityComparer => equalityComparer ?? (equalityComparer =
#if NET35 // Choosing always EnumComparer for enums
            typeof(T).IsEnum ? EnumComparer<T>.Comparer :
#elif NET40 || NET45 // In Framework 4.0 and above int-based enums are optimized so choosing EnumComparer for non-int enums only
            typeof(T).IsEnum && Enum.GetUnderlyingType(typeof(T)) != Reflector.IntType ? EnumComparer<T>.Comparer :
#elif !NETCOREAPP2_0 // In .NET Core the default comparer is optimized for all enum types. EnumComparer.Equals is usually still better but GetHashCode is slower.
#error .NET version is not set or not supported! - check EnumComparer performance in new framework
#endif
            (IEqualityComparer<T>)EqualityComparer<T>.Default);

        internal static IComparer<T> Comparer => comparer ?? (comparer =
#if NET35 || NET40 || NET45 || NETCOREAPP2_0 // Choosing EnumComparer for enums in all supported frameworks.
            typeof(T).IsEnum ? EnumComparer<T>.Comparer :
#else
#error .NET version is not set or not supported! - check EnumComparer performance in new framework
#endif
            (IComparer<T>)Comparer<T>.Default);

        #endregion
    }
}