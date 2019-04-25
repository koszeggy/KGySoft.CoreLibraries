#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommandSource.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
    /// Represents source information about the command.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event arguments of the source event.</typeparam>
    public interface ICommandSource<out TEventArgs> : ICommandSource
        where TEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets a <typeparamref name="TEventArgs"/> instance containing the event data.
        /// </summary>
        new TEventArgs EventArgs { get; }

        #endregion
    }
}
