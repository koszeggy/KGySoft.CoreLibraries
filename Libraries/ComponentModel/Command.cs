﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Command.cs
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
using System.Collections.Generic;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Contains extension methods for the <see cref="ICommand"/> type.
    /// </summary>
    public static class Command
    {
        #region Methods

        #region Public Methods

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/> as well as the optionally provided initial state of the binding.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="initialState">The initial state of the binding. The state entries, which are properties on the <paramref name="source"/> will be applied to the source object.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/> and to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public static ICommandBinding CreateBinding(this ICommand command, object source, string eventName, IDictionary<string, object> initialState = null, params object[] targets)
            => new CommandBinding(command, source ?? throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull)), eventName ?? throw new ArgumentNullException(nameof(eventName), Res.Get(Res.ArgumentNull)), initialState, targets);

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public static ICommandBinding CreateBinding(this ICommand command, object source, string eventName, params object[] targets)
            => new CommandBinding(command, source, eventName, null, targets);

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the <paramref name="command"/> invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public static ICommandBinding CreateBinding(this ICommand command, IDictionary<string, object> initialState = null)
            => new CommandBinding(command, null, null, initialState);

        #endregion

        #region Internal Methods

        internal static ICommandSource<T> Cast<T>(this ICommandSource orig)
            where T : EventArgs
            => new CommandSource<T>
            {
                EventArgs = (T)orig.EventArgs,
                Source = orig.Source,
                TriggeringEvent = orig.TriggeringEvent
            };

        #endregion

        #endregion
    }
}
