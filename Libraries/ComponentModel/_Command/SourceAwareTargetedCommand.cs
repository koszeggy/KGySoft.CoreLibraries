#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SourceAwareTargetedCommand.cs
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
    /// Represents a command, which is aware of its triggering sources and has one or more bound targets.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event arguments of the triggering event.</typeparam>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <seealso cref="ICommand" />
    public class SourceAwareTargetedCommand<TEventArgs, TTarget> : ICommand<TEventArgs>, IDisposable
        where TEventArgs : EventArgs
    {
        #region Fields

        private Action<ICommandSource<TEventArgs>, ICommandState, TTarget> callback;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public SourceAwareTargetedCommand(Action<ICommandSource<TEventArgs>, ICommandState, TTarget> callback)
            => this.callback = callback ?? throw new ArgumentNullException(nameof(callback));

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public SourceAwareTargetedCommand(Action<ICommandSource<TEventArgs>, TTarget> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            this.callback = (e, _, t) => callback.Invoke(e, t);
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

        void ICommand<TEventArgs>.Execute(ICommandSource<TEventArgs> source, ICommandState state, object target)
            => (callback ?? throw new ObjectDisposedException(null, Res.ObjectDisposed)).Invoke(source, state, (TTarget)target);

        void ICommand.Execute(ICommandSource source, ICommandState state, object target)
            => (callback ?? throw new ObjectDisposedException(null, Res.ObjectDisposed)).Invoke(source.Cast<TEventArgs>(), state, (TTarget)target);

        #endregion

        #endregion
    }
}
