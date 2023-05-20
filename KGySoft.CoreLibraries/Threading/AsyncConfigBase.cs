#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AsyncConfigBase.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
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
    /// Represents the base class for configuration of possibly asynchronous and multi-threaded operations.
    /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
    /// </summary>
    public abstract class AsyncConfigBase
    {
        #region Properties

        /// <summary>
        /// Gets or sets an <see cref="IAsyncProgress"/> instance that can handle progress notifications.
        /// <br/>Default value: <see langword="null"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="IAsyncProgress"/> interface for an example implementation.
        /// </summary>
        public IAsyncProgress? Progress { get; set; }

        /// <summary>
        /// Gets or sets the maximum degree of parallelism. Zero or less means an automatic configuration based on CPU cores.
        /// Set one to execute the operation on a single core. The asynchronous operation will not be blocking even if 1 is set.
        /// <br/>Default value: 0.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Gets or sets whether an <see cref="OperationCanceledException"/> should be thrown when ending or awaiting a canceled async operation.
        /// If the value of this property is <see langword="false"/>, then canceled non-void operations will return some value, which is usually the default value of their return type.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool ThrowIfCanceled { get; set; } = true;

        /// <summary>
        /// Gets or sets a user-provided object that will be returned by the <see cref="IAsyncResult.AsyncState"/> property that
        /// can be used to distinguish this particular asynchronous operation from other ones.
        /// This object will be returned also in the <see cref="IAsyncContext.State">IAsyncContext.State</see> property created by
        /// the methods of the <see cref="AsyncHelper"/> class.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        public object? State { get; set; }

        #endregion
    }
}


