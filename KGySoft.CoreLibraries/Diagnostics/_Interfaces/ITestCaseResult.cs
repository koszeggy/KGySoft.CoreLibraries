#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ITestCaseResult.cs
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

using System;
using System.Collections.Generic;

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Represents a performance test case result in an <see cref="IPerformanceTestResultCollection"/>.
    /// </summary>
    public interface ITestCaseResult
    {
        #region Properties

        /// <summary>
        /// Gets the name of the test case.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the result of the test case, or <see langword="null"/>, if not applicable.
        /// </summary>
        object? Result { get; }

        /// <summary>
        /// Gets the <see cref="Exception"/> of the test case if it failed; otherwise, gets <see langword="null"/>.
        /// </summary>
        Exception? Error { get; }

#if NET35 || NET40
        /// <summary>
        /// Gets the results of repetitions of this test case.
        /// Items count is determined by the <see cref="PerformanceTestBase.Repeat"/> property of the original test,
        /// unless <see cref="Error"/> returns a non-<see langword="null"/> value.
        /// Order of the items is the original execution order.
        /// </summary>
        IList<ITestCaseRepetition>? Repetitions { get; }
#else
        /// <summary>
        /// Gets the results of repetitions of this test case.
        /// Items count is determined by the <see cref="PerformanceTestBase.Repeat"/> property of the original test,
        /// unless <see cref="Error"/> returns a non-<see langword="null"/> value.
        /// Order of the items is the original execution order.
        /// </summary>
        IReadOnlyList<ITestCaseRepetition>? Repetitions { get; }
#endif

        #endregion
    }
}
