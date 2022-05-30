#if !NET35
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TaskConfig.cs
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

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// Represents asynchronous configuration for <see cref="Task"/>-returning methods.
    /// </summary>
    public class TaskConfig : AsyncConfigBase
    {
        #region Properties

        /// <summary>
        /// Gets or sets the cancellation token for this operation.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskConfig"/> class.
        /// </summary>
        public TaskConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskConfig"/> class.
        /// </summary>
        /// <param name="cancellationToken">Specifies the cancellation token for this operation.</param>
        public TaskConfig(CancellationToken cancellationToken) => CancellationToken = cancellationToken;

        #endregion
    }
}
#endif