#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TargetedCommand.cs
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
    /// Represents a command, which is unaware of its triggering sources and has one or more bound targets.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <seealso cref="ICommand" />
    public sealed class TargetedCommand<TTarget> : ICommand, IDisposable
    {
        #region Fields

        private Action<ICommandState, TTarget, object[]> callback;

        #endregion

        #region Constructors

        public TargetedCommand(Action<ICommandState, TTarget, object[]> callback)
        {
            if (callback == null)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = callback;
        }

        public TargetedCommand(Action<TTarget, object[]> callback)
        {
            if (callback == null)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (_, t, pars) => callback.Invoke(t, pars);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public TargetedCommand(Action<ICommandState, TTarget> callback)
        {
            if (callback == null)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (state, t, _) => callback.Invoke(state, t);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public TargetedCommand(Action<TTarget> callback)
        {
            if (callback == null)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (_, target, __) => callback.Invoke(target);
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

        void ICommand.Execute(ICommandSource source, ICommandState state, object target, object[] parameters)
        {
            Action<ICommandState, TTarget, object[]> copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(state, (TTarget)target, parameters);
        }

        #endregion

        #endregion
    }
}
