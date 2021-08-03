#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommand`1.cs
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

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// This interface exists just because in <see cref="CommandBinding"/> we already create a strongly typed delegate for EventArgs
    /// so we can re-use this strongly typed nature in source aware commands.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
    internal interface ICommand<in TEventArgs> : ICommand where TEventArgs : EventArgs
    {
        #region Methods

        void Execute(ICommandSource<TEventArgs> source, ICommandState state, object? target, object? parameter);

        #endregion
    }
}
