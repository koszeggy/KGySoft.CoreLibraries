#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IAsyncContext.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
#if !NET35
using System.Threading.Tasks;
#endif

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// Represents the context of a possibly asynchronous operation.
    /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class to see how to create sync and async methods (supporting
    /// both <see cref="Task"/> and <see cref="IAsyncResult"/> return types) using the same shared implementation with an <see cref="IAsyncContext"/> parameter.
    /// </summary>
    public interface IAsyncContext
    {
        #region Properties

        /// <summary>
        /// Gets the maximum degree of parallelism. If zero or less, then it is adjusted automatically.
        /// </summary>
        int MaxDegreeOfParallelism { get; }

        /// <summary>
        /// Gets whether the cancellation of the current operation has been requested.
        /// </summary>
        bool IsCancellationRequested { get; }

        /// <summary>
        /// Gets whether this operation can be canceled.
        /// </summary>
        bool CanBeCanceled { get; }

        /// <summary>
        /// Gets an <see cref="IAsyncProgress"/> instance that can be used to report progress, or <see langword="null"/>&#160;if
        /// no progress reporter belongs to the current operation.
        /// </summary>
        IAsyncProgress? Progress { get; }

        /// <summary>
        /// Gets the user provided object that was configured in the <see cref="AsyncConfigBase.State"/> property
        /// this <see cref="IAsyncContext"/> instance was created from.
        /// </summary>
        object? State { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Throws an <see cref="OperationCanceledException"/> if <see cref="IsCancellationRequested"/> returns <see langword="true"/>.
        /// </summary>
        void ThrowIfCancellationRequested();

        #endregion
    }
}