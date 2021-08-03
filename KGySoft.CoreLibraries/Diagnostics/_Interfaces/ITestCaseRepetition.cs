#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ITestCaseRepetition.cs
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

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Represents the performance test results of a single repetition of a test case.
    /// </summary>
    public interface ITestCaseRepetition
    {
        #region Properties

        /// <summary>
        /// Gets the actual execution time of the test case. It can be a somewhat longer time than
        /// <see cref="PerformanceTestBase.TestTime"/>, especially if the test case is an expensive operation.
        /// </summary>
        TimeSpan ExecutionTime { get; }

        /// <summary>
        /// Gets the performed iterations during this repetition.
        /// </summary>
        int Iterations { get; }

        #endregion
    }
}
