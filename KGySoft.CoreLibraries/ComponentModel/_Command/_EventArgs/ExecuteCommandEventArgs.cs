#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ExecuteCommandEventArgs.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides data for the <see cref="ICommandBinding.Executing">ICommandBinding.Executing</see>
    /// and <see cref="ICommandBinding.Executed">ICommandBinding.Executed</see> events.
    /// </summary>
    public class ExecuteCommandEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the triggering source of the <see cref="ICommand"/>.
        /// </summary>
        public ICommandSource Source { get; }

        /// <summary>
        /// Gets the state of the <see cref="ICommand"/>. Setting the <see cref="ICommandState.Enabled"/> property
        /// from the <see cref="ICommandBinding.Executing"/> event affects the actual execution.
        /// </summary>
        public ICommandState State { get; }

        #endregion

        #region Constructors

        internal ExecuteCommandEventArgs(ICommandSource source, ICommandState state)
        {
            Source = source;
            State = state;
        }

        #endregion
    }
}
