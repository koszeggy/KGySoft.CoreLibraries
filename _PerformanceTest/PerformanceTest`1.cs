#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PerformanceTest`1.cs
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

namespace _PerformanceTest
{
    internal class PerformanceTest<TResult> : PerformanceTest<Func<TResult>, TResult>
    {
        #region Methods

        protected override TResult Invoke(Func<TResult> del) => del.Invoke();

        #endregion
    }
}
