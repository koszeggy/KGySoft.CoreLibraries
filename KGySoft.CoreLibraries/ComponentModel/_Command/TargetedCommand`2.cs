﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TargetedCommand`2.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents parameterized a command, which is unaware of its triggering sources and has one or more bound targets.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TParam">The type of the command parameter.</typeparam>
    /// <seealso cref="ICommand" />
    /// <seealso cref="TargetedCommand{TTarget}"/>
    public sealed class TargetedCommand<TTarget, TParam> : ICommand, IDisposable
    {
        #region Fields

        private readonly string name;
        private Action<ICommandState, TTarget, TParam>? callback;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetedCommand{TTarget, TParam}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public TargetedCommand(Action<ICommandState, TTarget, TParam> callback)
        {
            if (callback == null!)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = callback;
            name = callback.GetName();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetedCommand{TTarget, TParam}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public TargetedCommand(Action<TTarget, TParam> callback)
        {
            if (callback == null!)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (_, t, param) => callback.Invoke(t, param);
            name = callback.GetName();
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Releases the delegate passed to the constructor. Should be called if the callback is an instance method, which holds references to other objects.
        /// </summary>
        public void Dispose() => callback = null;

        /// <summary>Returns a string that represents the current command.</summary>
        /// <returns>A string that represents the current command.</returns>
        public override string ToString() => name;

        #endregion

        #region Explicitly Implemented Interface Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        void ICommand.Execute(ICommandSource source, ICommandState state, object? target, object? parameter)
        {
            Action<ICommandState, TTarget, TParam>? copy = callback;
            if (copy == null)
                Throw.ObjectDisposedException(name);

            TTarget typedTarget;
            try
            {
                typedTarget = (TTarget)target!;
            }
            catch
            {
                Throw.ArgumentException<TTarget>(Res.ComponentModelCannotCastCommandTarget(target, typeof(TTarget)));
                throw;
            }

            TParam param;
            try
            {
                param = (TParam)parameter!;
            }
            catch
            {
                Throw.ArgumentException<TParam>(Res.ComponentModelCannotCastCommandParam(parameter, typeof(TParam)));
                throw;
            }

            copy.Invoke(state, typedTarget, param);
        }

        #endregion

        #endregion
    }
}
