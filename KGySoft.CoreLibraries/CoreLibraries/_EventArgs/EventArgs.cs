#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EventArgs.cs
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

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a simple event argument of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the event argument.</typeparam>
    public class EventArgs<T> : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the event data.
        /// </summary>
        public T EventData { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs{T}"/> class.
        /// </summary>
        /// <param name="arg">The argument.</param>
        public EventArgs(T arg) => EventData = arg;

        #endregion
    }
}
