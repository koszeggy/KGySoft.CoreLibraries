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
#if !NET35
using System.Runtime.CompilerServices; 
#endif

#if NET40 || NET45
using KGySoft.Reflection;
#endif

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

        internal static IEqualityComparer<T> EqualityComparer
        {
#if !NET35
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get
            {
                return equalityComparer ??=
#if !NETSTANDARD2_0
                    typeof(T).IsEnum ? EnumComparer<T>.Comparer : (IEqualityComparer<T>)EqualityComparer<T>.Default;
#else
                    EqualityComparer<T>.Default;
#endif
            }
        }

        internal static IComparer<T> Comparer
        {
#if !NET35
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get
            {
                return comparer ??=
#if !NETSTANDARD2_0
                    typeof(T).IsEnum ? EnumComparer<T>.Comparer : (IComparer<T>)Comparer<T>.Default;
#else
                    Comparer<T>.Default;
#endif
            }
        }

        #endregion
    }
}