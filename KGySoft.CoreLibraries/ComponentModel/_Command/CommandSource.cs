#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CommandSource.cs
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
    internal class CommandSource<TEventArgs> : ICommandSource<TEventArgs> where TEventArgs : EventArgs
    {
        #region Properties

        #region Public Properties

        public object Source { get; internal set; } = default!;
        public string TriggeringEvent { get; internal set; } = default!;
        public TEventArgs EventArgs { get; internal set; } = default!;

        #endregion

        #region Explicitly Implemented Interface Properties

        EventArgs ICommandSource.EventArgs => EventArgs;

        #endregion

        #endregion
    }
}
