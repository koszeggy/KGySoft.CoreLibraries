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

using System.Collections.Generic;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class ComparerHelper<T>
    {
        #region Properties

        internal static IEqualityComparer<T> EqualityComparer { get; } =
#if NETSTANDARD2_0
            EqualityComparer<T>.Default;
#else
            typeof(T).IsEnum ? EnumComparer<T>.Comparer : (IEqualityComparer<T>)EqualityComparer<T>.Default;
#endif

        internal static IComparer<T> Comparer { get; } =
#if NETSTANDARD2_0
            Comparer<T>.Default;
#else
            typeof(T).IsEnum ? EnumComparer<T>.Comparer : (IComparer<T>)Comparer<T>.Default;
#endif

        #endregion
    }
}