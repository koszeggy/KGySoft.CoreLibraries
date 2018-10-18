#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CommandBindingsCollection.cs
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
using System.Collections.ObjectModel;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a collection of command bindings. If a component or control uses events, then this class can be used
    /// to create and hold the event bindings, regardless of any used technology. When this class is disposed, all of the
    /// internally subscribed events will be released at once.
    /// <br/>For examples see the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface.
    /// </summary>
    /// <seealso cref="ICommand" />
    /// <seealso cref="ICommandBinding" />
    public class CommandBindingsCollection : Collection<ICommandBinding>, IDisposable
    {
        #region Methods

        #region Public Methods

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/> as well as the optionally provided initial state of the binding.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="initialState">The initial state of the binding. The state entries, which are properties on the <paramref name="source"/> will be applied to the source object.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/> and to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public virtual ICommandBinding Add(ICommand command, object source, string eventName, IDictionary<string, object> initialState = null, params object[] targets)
        {
            var result = command.CreateBinding(source, eventName, initialState, targets);
            Add(result);
            return result;
        }

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/>.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public ICommandBinding Add(ICommand command, object source, string eventName, params object[] targets)
            => Add(command, source, eventName, null, targets);

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <param name="disposeCommand"><see langword="true"/> to dispose the possibly disposable <paramref name="command"/> when the returned <see cref="ICommandBinding"/> is disposed; <see langword="false"/> to keep the <paramref name="command"/> alive when the returned <see cref="ICommandBinding"/> is disposed.
        /// Use <see langword="true"/> only if the command will not be re-used elsewhere. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the <paramref name="command"/> invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add(ICommand command, IDictionary<string, object> initialState = null, bool disposeCommand = false)
        {
            var result = command.CreateBinding(initialState, disposeCommand);
            Add(result);
            return result;
        }

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SimpleCommand"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add(Action<ICommandState> callback, IDictionary<string, object> initialState = null)
            => Add(new SimpleCommand(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SimpleCommand"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add(Action callback, IDictionary<string, object> initialState = null)
            => Add(new SimpleCommand(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareCommand{TEventArgs}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs>(Action<ICommandSource<TEventArgs>, ICommandState> callback, IDictionary<string, object> initialState = null) where TEventArgs : EventArgs
            => Add(new SourceAwareCommand<TEventArgs>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareCommand{TEventArgs}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs>(Action<ICommandSource<TEventArgs>> callback, IDictionary<string, object> initialState = null) where TEventArgs : EventArgs
            => Add(new SourceAwareCommand<TEventArgs>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="TargetedCommand{TTarget}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TTarget>(Action<ICommandState, TTarget> callback, IDictionary<string, object> initialState = null)
            => Add(new TargetedCommand<TTarget>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="TargetedCommand{TTarget}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TTarget>(Action<TTarget> callback, IDictionary<string, object> initialState = null)
            => Add(new TargetedCommand<TTarget>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareTargetedCommand{TEventArgs,TTarget}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs, TTarget>(Action<ICommandSource<TEventArgs>, ICommandState, TTarget> callback, IDictionary<string, object> initialState = null) where TEventArgs : EventArgs
            => Add(new SourceAwareTargetedCommand<TEventArgs, TTarget>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareTargetedCommand{TEventArgs,TTarget}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs, TTarget>(Action<ICommandSource<TEventArgs>, TTarget> callback, IDictionary<string, object> initialState = null) where TEventArgs : EventArgs
            => Add(new SourceAwareTargetedCommand<TEventArgs, TTarget>(callback), initialState, true);

        /// <summary>
        /// Releases every binding in this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Inserts a binding into the <see cref="CommandBindingsCollection" /> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The binding to insert.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        protected override void InsertItem(int index, ICommandBinding item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            base.InsertItem(index, item);
        }

        /// <summary>
        /// Replaces the binding at the specified index. The overridden binding will be disposed.
        /// </summary>
        /// <param name="index">The zero-based index of the binding to replace.</param>
        /// <param name="item">The binding to insert at the specified index.</param>
        /// <exception cref="ArgumentNullException">item</exception>
        protected override void SetItem(int index, ICommandBinding item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (this[index] == item)
                return;
            this[index].Dispose();
            base.SetItem(index, item);
        }

        /// <summary>
        /// Removes the binding at the specified <paramref name="index"/> of the <see cref="CommandBindingsCollection" />.
        /// The removed binding will be disposed.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        protected override void RemoveItem(int index)
        {
            this[index].Dispose();
            base.RemoveItem(index);
        }

        /// <summary>
        /// Removes all elements from the <see cref="CommandBindingsCollection" />.
        /// The removed bindings will be disposed.
        /// </summary>
        protected override void ClearItems()
        {
            var length = Count;
            for (int i = length - 1; i >= 0; i--)
                RemoveItem(i);
        }

        /// <summary>
        /// Releases every binding in this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if this method was invoked by explicit disposing, <see langword="false"/> if finalizing the object.</param>
        protected virtual void Dispose(bool disposing) => ClearItems();

        #endregion

        #endregion
    }
}
