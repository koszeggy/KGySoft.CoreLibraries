#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReferenceEqualityComparer`1.cs
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#endregion

#region Suppressions

#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8769 // Nullability of reference types in type of parameter doesn't match implemented member (possibly because of nullability attributes).
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Forces objects to be compared by reference.
    /// </summary>
    internal class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
        #region Fields

        private static ReferenceEqualityComparer<T>? comparer;

        #endregion

        #region Properties

        internal static ReferenceEqualityComparer<T> Comparer => comparer ??= new ReferenceEqualityComparer<T>();

        #endregion

        #region Methods

        bool IEqualityComparer<T>.Equals(T? x, T? y) => ReferenceEquals(x, y);

        int IEqualityComparer<T>.GetHashCode([DisallowNull]T obj) => RuntimeHelpers.GetHashCode(obj);

        #endregion
    }
}
