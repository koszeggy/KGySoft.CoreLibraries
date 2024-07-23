#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AsyncContextWrapper.cs
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
    /// Provides a wrapper of an existing <see cref="IAsyncContext"/> instance to override its <see cref="IAsyncContext.MaxDegreeOfParallelism"/> or to conceal its <see cref="IAsyncContext.Progress"/>.
    /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details about <see cref="IAsyncContext"/>.
    /// </summary>
    public sealed class AsyncContextWrapper : IAsyncContext
    {
        #region Fields

        private readonly IAsyncContext wrappedContext;

        #endregion

        #region Properties

        #region Public Properties

        /// <inheritdoc />
        public int MaxDegreeOfParallelism { get; }

        /// <inheritdoc />
        public bool IsCancellationRequested => wrappedContext.IsCancellationRequested;

        /// <inheritdoc />
        public bool CanBeCanceled => wrappedContext.CanBeCanceled;

        /// <inheritdoc />
        public IAsyncProgress? Progress { get; }

        #endregion

        #region Explicitly Implemented Interface Properties

        object? IAsyncContext.State => wrappedContext.State;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncContextWrapper"/> class with the provided parameters.
        /// </summary>
        /// <param name="wrappedContext">The original <see cref="IAsyncContext"/> instance to wrap.</param>
        /// <param name="maxDegreeOfParallelism"></param>
        /// <param name="forwardProgress"></param>
        public AsyncContextWrapper(IAsyncContext wrappedContext, int maxDegreeOfParallelism, bool forwardProgress = true)
        {
            if (wrappedContext == null!)
                Throw.ArgumentNullException(nameof(wrappedContext));

            this.wrappedContext = wrappedContext is AsyncContextWrapper other ? other.wrappedContext : wrappedContext;
            MaxDegreeOfParallelism = maxDegreeOfParallelism;

            // Note: not taking other.Progress even if wrappedContext is also AsyncContextWrapper, so the progress can remain suppressed.
            Progress = forwardProgress ? wrappedContext.Progress : null;
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public void ThrowIfCancellationRequested() => wrappedContext.ThrowIfCancellationRequested();

        #endregion
    }
}
