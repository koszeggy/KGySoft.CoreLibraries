#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumeratorExtensions.cs
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

using System.Collections;
using System.Collections.Generic;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class EnumeratorExtensions
    {
        #region Methods

        internal static IList<T> RestToList<T>(this IEnumerator<T> enumerator)
        {
            var result = new List<T>();
            while (enumerator.MoveNext())
                result.Add(enumerator.Current);
            return result;
        }

        internal static IList<object> RestToList(this IEnumerator enumerator)
        {
            var result = new List<object>();
            while (enumerator.MoveNext())
                result.Add(enumerator.Current);
            return result;
        }

        #endregion
    }
}
