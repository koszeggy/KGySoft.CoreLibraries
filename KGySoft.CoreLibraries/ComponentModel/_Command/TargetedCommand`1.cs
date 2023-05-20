#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TargetedCommand`1.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
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
    /// Represents a non-parameterized command, which is unaware of its triggering sources and has one or more bound targets.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <seealso cref="ICommand"/>
    /// <seealso cref="TargetedCommand{TTarget,TParam}"/>
    public sealed class TargetedCommand<TTarget> : ICommand, IDisposable
    {
        #region Fields

        private readonly string name;
        private Action<ICommandState, TTarget>? callback;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetedCommand{TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public TargetedCommand(Action<ICommandState, TTarget> callback)
        {
            if (callback == null!)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = callback;
            name = callback.GetName();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetedCommand{TTarget}"/> class.
        /// </summary>
        /// <param name="callback">A delegate to invoke when the command is triggered.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        public TargetedCommand(Action<TTarget> callback)
        {
            if (callback == null!)
                Throw.ArgumentNullException(Argument.callback);
            this.callback = (_, target) => callback.Invoke(target);
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
            Action<ICommandState, TTarget>? copy = callback;
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

            copy.Invoke(state, typedTarget);
        }

        #endregion

        #endregion
    }
}
