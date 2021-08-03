#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SourceAwareCommand`1.cs
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
    /// Represents a non-parameterized command, which is aware of its triggering sources and has no bound targets.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event arguments of the triggering event.</typeparam>
    /// <seealso cref="ICommand"/>
    /// <seealso cref="SourceAwareCommand{TEventArgs,TParam}"/>
    public sealed class SourceAwareCommand<TEventArgs> : ICommand<TEventArgs>, IDisposable
        where TEventArgs : EventArgs
    {
        #region Fields

        private Action<ICommandSource<TEventArgs>, ICommandState>? callback;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public SourceAwareCommand(Action<ICommandSource<TEventArgs>, ICommandState> callback)
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
        public SourceAwareCommand(Action<ICommandSource<TEventArgs>> callback)
        {
            if (callback == null!)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (src, _) => callback.Invoke(src);
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
        void ICommand<TEventArgs>.Execute(ICommandSource<TEventArgs> source, ICommandState state, object? target, object? parameter)
        {
            Action<ICommandSource<TEventArgs>, ICommandState>? copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(source, state);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        void ICommand.Execute(ICommandSource source, ICommandState state, object? target, object? parameter)
        {
            Action<ICommandSource<TEventArgs>, ICommandState>? copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(source.Cast<TEventArgs>(), state);
        }

        #endregion

        #endregion
    }
}
