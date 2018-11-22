#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SimpleCommand.cs
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
    /// Represents a command, which is unaware of its triggering sources and has no bound targets.
    /// <br/>To see examples how to use the difference command types see the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface.
    /// </summary>
    /// <seealso cref="ICommand" />
    public class SimpleCommand : ICommand, IDisposable
    {
        #region Fields

        private Action<ICommandState> callback;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleCommand"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public SimpleCommand(Action<ICommandState> callback) => this.callback = callback ?? throw new ArgumentNullException(nameof(callback));

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleCommand"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public SimpleCommand(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            this.callback = _ => callback.Invoke();
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

        void ICommand.Execute(ICommandSource source, ICommandState state, object target) => (callback ?? throw new ObjectDisposedException(null, Res.ObjectDisposed)).Invoke(state);

        #endregion

        #endregion
    }
}
