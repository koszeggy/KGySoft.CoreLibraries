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

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// Represents asynchronous configuration for <see cref="IAsyncResult"/>-returning methods.
    /// </summary>
    public class AsyncConfig : AsyncConfigBase
    {
        #region Properties

        /// <summary>
        /// Gets or sets a callback that can return whether cancellation has been requested.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        public Func<bool>? IsCancelRequestedCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that will be invoked when the operation is completed.
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

        #endregion
    }
}
