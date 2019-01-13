#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReferenceEqualityComparer.cs
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
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Forces objects to be compared by reference.
    /// </summary>
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        #region Fields

        private static ReferenceEqualityComparer comparer;

        #endregion

        #region Properties

        internal static ReferenceEqualityComparer Comparer => comparer ?? (comparer = new ReferenceEqualityComparer());

        #endregion

        #region Methods

        bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

        int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);

        #endregion
    }
}
