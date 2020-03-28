#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SourceAwareTargetedCommand`2.cs
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
using System.Runtime.CompilerServices;

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
    public sealed class SourceAwareTargetedCommand<TEventArgs, TTarget> : ICommand<TEventArgs>, IDisposable
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
        {
            if (callback == null)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = callback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public SourceAwareTargetedCommand(Action<ICommandSource<TEventArgs>, TTarget> callback)
        {
            if (callback == null)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (src, _, t) => callback.Invoke(src, t);
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

#if !(NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void ICommand<TEventArgs>.Execute(ICommandSource<TEventArgs> source, ICommandState state, object target, object parameter)
        {
            Action<ICommandSource<TEventArgs>, ICommandState, TTarget> copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(source, state, (TTarget)target);
        }

#if !(NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        void ICommand.Execute(ICommandSource source, ICommandState state, object target, object parameter)
        {
            Action<ICommandSource<TEventArgs>, ICommandState, TTarget> copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(source.Cast<TEventArgs>(), state, (TTarget)target);
        }

        #endregion

        #endregion
    }
}
