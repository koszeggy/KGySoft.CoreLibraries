#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ExceptionExtensions.cs
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

using System;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class ExceptionExtensions
    {
        #region Methods

        internal static bool IsCritical(this Exception e) => e is OutOfMemoryException || e is StackOverflowException;

        internal static bool IsCriticalOr(this Exception e, bool condition) => e.IsCritical() || condition;

        #endregion
    }
}
