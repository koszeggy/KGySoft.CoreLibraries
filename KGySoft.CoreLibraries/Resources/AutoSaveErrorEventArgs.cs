#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AutoSaveErrorEventArgs.cs
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
using System.ComponentModel;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Provides data for the <see cref="DynamicResourceManager.AutoSaveError"/> event.
    /// </summary>
    public sealed class AutoSaveErrorEventArgs : HandledEventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the <see cref="System.Exception"/> instance that occurred on auto saving.
        /// </summary>
        public Exception Exception { get; }

        #endregion

        #region Constructors

        internal AutoSaveErrorEventArgs(Exception exception) : base(false) => Exception = exception;

        #endregion
    }
}
