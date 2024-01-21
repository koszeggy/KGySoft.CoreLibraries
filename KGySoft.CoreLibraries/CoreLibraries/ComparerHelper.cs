#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ComparerHelper.cs
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
using System.Collections.Generic;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class ComparerHelper<T>
    {
        #region Properties

        internal static IEqualityComparer<T> EqualityComparer { get; } =
#if NET35
            typeof(T).IsEnum ? EnumComparer<T>.Comparer : EqualityComparer<T>.Default;
#elif NETFRAMEWORK
            !EnvironmentHelper.IsPartiallyTrustedDomain && typeof(T).IsEnum ? EnumComparer<T>.Comparer : EqualityComparer<T>.Default;
#else
            EqualityComparer<T>.Default;
#endif

        internal static IComparer<T> Comparer { get; } =
#if NET35
            typeof(T).IsEnum ? EnumComparer<T>.Comparer : Comparer<T>.Default;
#elif NETFRAMEWORK
            !EnvironmentHelper.IsPartiallyTrustedDomain && typeof(T).IsEnum ? EnumComparer<T>.Comparer : Comparer<T>.Default;
#else
            Comparer<T>.Default;
#endif

        #endregion

        #region Methods

        #region Internal Methods

#if NET5_0_OR_GREATER
        internal static IEqualityComparer<T>? GetEqualityComparer(IEqualityComparer<T>? comparer) =>
            typeof(T).IsValueType ? GetNonDefaultEqualityComparerOrNull(comparer) : comparer ?? EqualityComparer;
#else
        internal static IEqualityComparer<T> GetEqualityComparer(IEqualityComparer<T>? comparer) => comparer ?? EqualityComparer;
#endif

        internal static bool IsDefaultComparer(IEqualityComparer<T>? comparer)
            => comparer == null
                || ReferenceEquals(comparer, EqualityComparer)
                // Left part can be optimized away by JIT but only if we use typeof(string) and not Reflector.StringType
                || typeof(T) == typeof(string) && ReferenceEquals(comparer, StringComparer.Ordinal);

        #endregion

        #region Private Methods

        private static IEqualityComparer<T>? GetNonDefaultEqualityComparerOrNull(IEqualityComparer<T>? comparer) => IsDefaultComparer(comparer) ? null : comparer;
        
        #endregion

        #endregion
    }
}