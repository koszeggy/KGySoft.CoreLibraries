#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SimpleCommand`1.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
    public sealed class SimpleCommand<TParam> : ICommand, IDisposable
    {
        #region Fields

        private Action<ICommandState, TParam> callback;

        #endregion

        #region Constructors

        public SimpleCommand(Action<ICommandState, TParam> callback)
        {
            if (callback == null)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = callback;
        }

        public SimpleCommand(Action<TParam> callback)
        {
            if (callback == null)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (_, param) => callback.Invoke(param);
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
        void ICommand.Execute(ICommandSource source, ICommandState state, object target, object parameter)
        {
            Action<ICommandState, TParam> copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException();
            copy.Invoke(state, (TParam)parameter);
        }

        #endregion

        #endregion
    }
}
