#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CommandBindingErrorEventArgs.cs
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

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides data for the <see cref="ICommandBinding.Error">ICommandBinding.Error</see> event.
    /// </summary>
    /// <seealso cref="HandledEventArgs" />
    public sealed class CommandBindingErrorEventArgs : HandledEventArgs
    {
        #region Properties

        ///// <summary>
        ///// Gets the source of the property binding.
        ///// </summary>
        //public object Source { get; }

        ///// <summary>
        ///// Gets the target of the property binding.
        ///// </summary>
        //public object Target { get; }

        ///// <summary>
        ///// Gets the value to be set. This is either the value of the source property,
        ///// or a converted value if a formatting delegate has been specified.
        ///// </summary>
        //public object? Value { get; }

        /// <summary>
        /// Gets the context of the error.
        /// </summary>
        public CommandBindingErrorContext Context { get; }

        /// <summary>
        /// Gets the <see cref="Exception"/> occurred while attempting to execute the binding.
        /// You can set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>&#160;to
        /// suppress the error. Critical exceptions (<see cref="OutOfMemoryException"/>, <see cref="StackOverflowException"/>)
        /// cannot be handled by the <see cref="ICommandBinding.Error">ICommandBinding.Error</see> event.
        /// </summary>
        public Exception Error { get; }

        #endregion

        #region Constructors

        internal CommandBindingErrorEventArgs(CommandBindingErrorContext context, Exception error)
        {
            Context = context;
            Error = error;
        }

        #endregion
    }
}
