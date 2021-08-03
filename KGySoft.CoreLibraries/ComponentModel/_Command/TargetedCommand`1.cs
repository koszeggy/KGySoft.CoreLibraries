#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TargetedCommand`1.cs
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
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a non-parameterized command, which is unaware of its triggering sources and has one or more bound targets.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <seealso cref="ICommand"/>
    /// <seealso cref="TargetedCommand{TTarget,TParam}"/>
    public sealed class TargetedCommand<TTarget> : ICommand, IDisposable
    {
        #region Fields

        private Action<ICommandState, TTarget>? callback;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public TargetedCommand(Action<ICommandState, TTarget> callback)
        {
            if (callback == null!)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = callback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public TargetedCommand(Action<TTarget> callback)
        {
            if (callback == null!)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (_, target) => callback.Invoke(target);
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Releases the delegate passed to the constructor. Should be called if the callback is an instance method, which holds references to other objects.
        /// </summary>
        public void Dispose() => callback = null;

        #endregion

        #region Explicitly Implemented Interface Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        void ICommand.Execute(ICommandSource source, ICommandState state, object? target, object? parameter)
        {
            Action<ICommandState, TTarget>? copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(state, (TTarget)target!);
        }

        #endregion

        #endregion
    }
}
