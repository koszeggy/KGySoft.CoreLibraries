#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommandSource.cs
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

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents source information about the command.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    public interface ICommandSource
    {
        #region Properties

        /// <summary>
        /// Gets the source of the invocation. For instance events, this is the object that owns the event. For static events, this is the declaring type of the event.
        /// </summary>
        object Source { get; }

        /// <summary>
        /// Gets the sender of the underlying event. For instance events, this is usually the same as <see cref="Source"/>, but it can be different in some cases,
        /// for example when the add/remove accessors of the event map the subscription to another object's event. For static events the sender is usually <see langword="null"/>.
        /// </summary>
#if NETFRAMEWORK || NETSTANDARD2_0
        object? Sender { get; }
#else
        object? Sender => Source;
#endif

        /// <summary>
        /// Gets the triggering event of the source object.
        /// </summary>
        string TriggeringEvent { get; }

        /// <summary>
        /// Gets the <see cref="System.EventArgs"/> instance containing the event data.
        /// </summary>
        EventArgs EventArgs { get; }

        #endregion
    }
}
