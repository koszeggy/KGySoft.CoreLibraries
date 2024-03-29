﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParallelConfig.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#pragma warning disable CS1580 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// Represents a configuration for parallel operations.
    /// </summary>
    public class ParallelConfig : AsyncConfigBase
    {
        #region Properties

        /// <summary>
        /// Gets or sets a callback that can return whether cancellation has been requested. To use a <see cref="CancellationToken"/>
        /// on .NET Framework 4.0 or later, use the appropriate <see cref="ParallelConfig(CancellationToken)">constructor</see>
        /// or the <see cref="TaskConfig"/> type with <see cref="Task"/>-returning methods.
        /// <br/>Default value: <see langword="null"/>, if the default constructor was called.
        /// </summary>
        public Func<bool>? IsCancelRequestedCallback { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelConfig"/> class.
        /// </summary>
        public ParallelConfig()
        {
        }

#if !NET35
        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelConfig"/> class initializing the <see cref="IsCancelRequestedCallback"/>
        /// property from a <see cref="CancellationToken"/>.
        /// <br/>This constructor is available only for .NET Framework 4.0 and later.
        /// </summary>
        /// <param name="cancellationToken">Specifies the cancellation token for this operation.</param>
        public ParallelConfig(CancellationToken cancellationToken)
            => IsCancelRequestedCallback = () => cancellationToken.IsCancellationRequested;
#endif

        #endregion
    }
}
