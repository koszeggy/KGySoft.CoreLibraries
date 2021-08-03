#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SourceAwareCommand`2.cs
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
    /// Represents a parameterized command, which is aware of its triggering sources and has no bound targets.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event arguments of the triggering event.</typeparam>
    /// <typeparam name="TParam">The type of the command parameter.</typeparam>
    /// <seealso cref="ICommand" />
    /// <seealso cref="SourceAwareCommand{TEventArgs}"/>
    public sealed class SourceAwareCommand<TEventArgs, TParam> : ICommand<TEventArgs>, IDisposable
        where TEventArgs : EventArgs
    {
        #region Fields

        private Action<ICommandSource<TEventArgs>, ICommandState, TParam>? callback;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public SourceAwareCommand(Action<ICommandSource<TEventArgs>, ICommandState, TParam> callback)
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
        public SourceAwareCommand(Action<ICommandSource<TEventArgs>, TParam> callback)
        {
            if (callback == null!)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (src, _, param) => callback.Invoke(src, param);
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
            Action<ICommandSource<TEventArgs>, ICommandState, TParam>? copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(source, state, (TParam)parameter!);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        void ICommand.Execute(ICommandSource source, ICommandState state, object? target, object? parameter)
        {
            Action<ICommandSource<TEventArgs>, ICommandState, TParam>? copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(source.Cast<TEventArgs>(), state, (TParam)parameter!);
        }

        #endregion

        #endregion
    }
}
