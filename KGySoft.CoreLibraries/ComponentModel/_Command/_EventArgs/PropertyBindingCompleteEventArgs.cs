#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PropertyBindingCompleteEventArgs.cs
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
    /// Provides data for the <see cref="Command.PropertyBindingComplete">Command.PropertyBindingComplete</see> event.
    /// </summary>
    /// <seealso cref="HandledEventArgs" />
    public sealed class PropertyBindingCompleteEventArgs : HandledEventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the source of the property binding.
        /// </summary>
        public object Source { get; }

        /// <summary>
        /// Gets the target of the property binding.
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Gets the source property name of the property binding.
        /// </summary>
        public string SourcePropertyName { get; }

        /// <summary>
        /// Gets the target property name of the property binding.
        /// </summary>
        public string TargetPropertyName { get; }

        /// <summary>
        /// Gets the value to be set. This is either the value of the source property,
        /// or a converted value if a formatting delegate has been specified.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the error if an <see cref="Exception"/> occurred while attempting to evaluate the property binding;
        /// or <see langword="null"/>, if the binding completed successfully.
        /// If this property is not null, then the <see cref="HandledEventArgs.Handled"/> property is initialized to <see langword="false"/>,
        /// which can be set to <see langword="true"/>&#160;to prevent throwing the exception.
        /// </summary>
        public Exception? Error { get; }

        #endregion

        #region Constructors

        internal PropertyBindingCompleteEventArgs(object source, object target, string sourcePropertyName, string targetPropertyName, object? value, Exception? error = null)
            : base(error == null)
        {
            Source = source;
            Target = target;
            SourcePropertyName = sourcePropertyName;
            TargetPropertyName = targetPropertyName;
            Value = value;
            Error = error;
        }

        #endregion
    }
}
