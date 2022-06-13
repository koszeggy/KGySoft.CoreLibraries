#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AsyncConfig.cs
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
using System.Threading;
using System.Threading.Tasks;
#endif

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// Represents asynchronous configuration for <see cref="IAsyncResult"/>-returning methods.
    /// </summary>
    public class AsyncConfig : ParallelConfig
    {
        #region Properties

        /// <summary>
        /// Gets or sets a callback that will be invoked when the operation is completed.
        /// This property is ignored for methods that expect a <see cref="ParallelConfig"/> parameter.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        public AsyncCallback? CompletedCallback { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncConfig"/> class.
        /// </summary>
        public AsyncConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncConfig"/> class.
        /// </summary>
        /// <param name="completedCallback">A callback that will be invoked when the operation is completed..</param>
        /// <param name="isCancelRequestedCallback">A callback that can return whether cancellation has been requested.</param>
        public AsyncConfig(AsyncCallback? completedCallback, Func<bool>? isCancelRequestedCallback = null)
        {
            CompletedCallback = completedCallback;
            IsCancelRequestedCallback = isCancelRequestedCallback;
        }

#if !NET35
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncConfig"/> class initializing the <see cref="ParallelConfig.IsCancelRequestedCallback"/>
        /// property from a <see cref="CancellationToken"/>.
        /// <br/>This constructor is available only for .NET Framework 4.0 and later.
        /// </summary>
        /// <param name="cancellationToken">Specifies the cancellation token for this operation.</param>
        public AsyncConfig(CancellationToken cancellationToken)
            => IsCancelRequestedCallback = () => cancellationToken.IsCancellationRequested;
#endif

        #endregion
    }
}
