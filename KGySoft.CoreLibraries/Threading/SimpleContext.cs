#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SimpleContext.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// Represent a predefined simple context for non-async, possibly parallel operations where the maximum degree of parallelism can be specified,
    /// but cancellation and reporting progress is not supported. Can be used for methods with an <see cref="IAsyncContext"/> parameter where we want
    /// to force the degree of parallelism. To force a single threaded execution you can use the
    /// predefined <see cref="AsyncHelper.SingleThreadContext">AsyncHelper.SingleThreadContext</see> property.
    /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details about <see cref="IAsyncContext"/>.
    /// </summary>
    public sealed class SimpleContext : IAsyncContext
    {
        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the maximum degree of parallelism. If zero or less, then it is adjusted automatically.
        /// </summary>
        public int MaxDegreeOfParallelism { get; }

        #endregion

        #region Explicitly Implemented Interface Properties

        bool IAsyncContext.IsCancellationRequested => false;
        bool IAsyncContext.CanBeCanceled => false;
        IAsyncProgress? IAsyncContext.Progress => null;
        object? IAsyncContext.State => null;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="SimpleContext"/> class with the specified maximum degree of parallelism.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Specifies the maximum degree of parallelism. If zero or less, then it will be adjusted automatically.</param>
        public SimpleContext(int maxDegreeOfParallelism) => MaxDegreeOfParallelism = maxDegreeOfParallelism;

        #endregion

        #region Methods

        void IAsyncContext.ThrowIfCancellationRequested()
        {
        }

        #endregion
    }
}
