#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IMeasureItem.cs
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
    /// Represents a measurement item that is managed by the <see cref="Profiler"/> class.
    /// </summary>
    public interface IMeasureItem
    {
        #region Properties

        /// <summary>
        /// Gets the category name of the measurement item.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Gets the operation name of the measurement item.
        /// </summary>
        string Operation { get; }

        /// <summary>
        /// Gets the number of calls of the current operation.
        /// </summary>
        long NumberOfCalls { get; }

        /// <summary>
        /// Gets the duration of the first call of the current operation.
        /// </summary>
        TimeSpan FirstCall { get; }

        /// <summary>
        /// Gets the total duration of the current operation.
        /// </summary>
        TimeSpan TotalTime { get; }

        #endregion
    }
}
