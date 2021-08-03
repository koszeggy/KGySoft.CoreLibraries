#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CommandBindingsCollection.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using KGySoft.Collections.ObjectModel;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a collection of command bindings. If a component or control uses events, then this class can be used
    /// to create and hold the event bindings, regardless of any used technology. When this class is disposed, all of the
    /// internally subscribed events will be released at once. Removed and replaced bindings will also be disposed.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    /// <seealso cref="ICommand" />
    /// <seealso cref="ICommandBinding" />
    public class CommandBindingsCollection : FastLookupCollection<ICommandBinding>, IDisposable
    {
        #region Methods

        #region Public Methods

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="disposeCommand"><see langword="true"/>&#160;to dispose the possibly disposable <paramref name="command"/> when the returned <see cref="ICommandBinding"/> is disposed; <see langword="false"/>&#160;to keep the <paramref name="command"/> alive when the returned <see cref="ICommandBinding"/> is disposed.
        /// Use <see langword="true"/>&#160;only if the command will not be re-used elsewhere. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the <paramref name="command"/> invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public virtual ICommandBinding Add(ICommand command, IDictionary<string, object?>? initialState = null, bool disposeCommand = false)
        {
            ICommandBinding result = command.CreateBinding(initialState, disposeCommand);
            Add(result);
            return result;
        }

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/> as well as the optionally provided initial state of the binding.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command. Can be a <see cref="Type"/> for static events.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/> and to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public ICommandBinding Add(ICommand command, object source, string eventName, IDictionary<string, object?>? initialState = null, params object[]? targets)
        {
            ICommandBinding result = Add(command, initialState).AddSource(source, eventName);
            if (!targets.IsNullOrEmpty())
            {
                foreach (object target in targets!)
                    result.AddTarget(target);
            }

            return result;
        }

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/>.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command. Can be a <see cref="Type"/> for static events.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public ICommandBinding Add(ICommand command, object source, string eventName, params object[]? targets)
            => Add(command, source, eventName, null, targets);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event of the specified <paramref name="source"/>, which allows to update the
        /// specified <paramref name="targetPropertyName"/> in the <paramref name="targets"/>, when the property of <paramref name="sourcePropertyName"/> changes in the <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="sourcePropertyName"/> parameter is observed.</param>
        /// <param name="sourcePropertyName">The name of the property, whose change is observed.</param>
        /// <param name="targetPropertyName">The name of the property in the target object(s).</param>
        /// <param name="targets">The targets to be updated. If the concrete instances to update have to be returned when the change occurs use the <see cref="ICommandBinding.AddTarget(Func{object})">ICommandBinding.AddTarget</see>
        /// method on the result <see cref="ICommandBinding"/> instance.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="sourcePropertyName"/> or <paramref name="targetPropertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> is neither an <see cref="INotifyPropertyChanged"/> implementation nor has a <c><paramref name="sourcePropertyName"/>Changed</c> event.</exception>
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.
        /// Or, when <paramref name="source"/> does not implement <see cref="INotifyPropertyChanged"/>, then an event of name <paramref name="sourcePropertyName"/> postfixed by <c>Changed</c> should exist on the <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified property names.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="targetPropertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public ICommandBinding AddPropertyBinding(object source, string sourcePropertyName, string targetPropertyName, params object[]? targets)
            => DoAddPropertyBinding(source, sourcePropertyName, targetPropertyName, null, targets, null);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event of the specified <paramref name="source"/>, which allows to update the
        /// specified <paramref name="targetPropertyName"/> in the <paramref name="targets"/>, when the property of <paramref name="sourcePropertyName"/> changes in the <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="sourcePropertyName"/> parameter is observed.</param>
        /// <param name="sourcePropertyName">The name of the property, whose change is observed.</param>
        /// <param name="targetPropertyName">The name of the property in the target object(s).</param>
        /// <param name="format">If not <see langword="null"/>, then can be used to format the value to be set in the <paramref name="targets"/>.</param>
        /// <param name="targets">The targets to be updated. If the concrete instances to update have to be returned when the change occurs use the <see cref="ICommandBinding.AddTarget(Func{object})">ICommandBinding.AddTarget</see>
        /// method on the result <see cref="ICommandBinding"/> instance.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="sourcePropertyName"/> or <paramref name="targetPropertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> is neither an <see cref="INotifyPropertyChanged"/> implementation nor has a <c><paramref name="sourcePropertyName"/>Changed</c> event.</exception>
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.
        /// Or, when <paramref name="source"/> does not implement <see cref="INotifyPropertyChanged"/>, then an event of name <paramref name="sourcePropertyName"/> postfixed by <c>Changed</c> should exist on the <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified property names and <paramref name="format"/>parameters.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="targetPropertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public ICommandBinding AddPropertyBinding(object source, string sourcePropertyName, string targetPropertyName, Func<object?, object?>? format, params object[] targets)
            => DoAddPropertyBinding(source, sourcePropertyName, targetPropertyName, format, targets, null);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event of the specified <paramref name="source"/>, which allows to update the
        /// specified <paramref name="targetPropertyName"/> in the <paramref name="targets"/>, when the property of <paramref name="sourcePropertyName"/> changes in the <paramref name="source"/>.
        /// The target properties will be set using the <see cref="SynchronizationContext"/> of the thread on which this method was called.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="sourcePropertyName"/> parameter is observed.</param>
        /// <param name="sourcePropertyName">The name of the property, whose change is observed.</param>
        /// <param name="targetPropertyName">The name of the property in the target object(s).</param>
        /// <param name="awaitCompletion"><see langword="true"/>&#160;to block the thread of the triggering event until setting a target property is completed; otherwise, <see langword="false"/>.</param>
        /// <param name="targets">The targets to be updated. If the concrete instances to update have to be returned when the change occurs use the <see cref="ICommandBinding.AddTarget(Func{object})">ICommandBinding.AddTarget</see>
        /// method on the result <see cref="ICommandBinding"/> instance.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="sourcePropertyName"/> or <paramref name="targetPropertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> is neither an <see cref="INotifyPropertyChanged"/> implementation nor has a <c><paramref name="sourcePropertyName"/>Changed</c> event.</exception>
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.
        /// Or, when <paramref name="source"/> does not implement <see cref="INotifyPropertyChanged"/>, then an event of name <paramref name="sourcePropertyName"/> postfixed by <c>Changed</c> should exist on the <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified property names.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="targetPropertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public ICommandBinding AddSynchronizedPropertyBinding(object source, string sourcePropertyName, string targetPropertyName, bool awaitCompletion, params object[]? targets)
            => DoAddPropertyBinding(source, sourcePropertyName, targetPropertyName, null, targets, awaitCompletion);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event of the specified <paramref name="source"/>, which allows to update the
        /// specified <paramref name="targetPropertyName"/> in the <paramref name="targets"/>, when the property of <paramref name="sourcePropertyName"/> changes in the <paramref name="source"/>.
        /// The target properties will be set using the <see cref="SynchronizationContext"/> of the thread on which this method was called.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="sourcePropertyName"/> parameter is observed.</param>
        /// <param name="sourcePropertyName">The name of the property, whose change is observed.</param>
        /// <param name="targetPropertyName">The name of the property in the target object(s).</param>
        /// <param name="format">If not <see langword="null"/>, then can be used to format the value to be set in the <paramref name="targets"/>.</param>
        /// <param name="awaitCompletion"><see langword="true"/>&#160;to block the thread of the triggering event until setting a target property is completed; otherwise, <see langword="false"/>.</param>
        /// <param name="targets">The targets to be updated. If the concrete instances to update have to be returned when the change occurs use the <see cref="ICommandBinding.AddTarget(Func{object})">ICommandBinding.AddTarget</see>
        /// method on the result <see cref="ICommandBinding"/> instance.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="sourcePropertyName"/> or <paramref name="targetPropertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> is neither an <see cref="INotifyPropertyChanged"/> implementation nor has a <c><paramref name="sourcePropertyName"/>Changed</c> event.</exception>
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.
        /// Or, when <paramref name="source"/> does not implement <see cref="INotifyPropertyChanged"/>, then an event of name <paramref name="sourcePropertyName"/> postfixed by <c>Changed</c> should exist on the <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified property names and <paramref name="format"/>parameters.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="targetPropertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public ICommandBinding AddSynchronizedPropertyBinding(object source, string sourcePropertyName, string targetPropertyName, Func<object?, object?>? format, bool awaitCompletion, params object[]? targets)
            => DoAddPropertyBinding(source, sourcePropertyName, targetPropertyName, format, targets, awaitCompletion);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SimpleCommand"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// </returns>
        public ICommandBinding Add(Action<ICommandState> callback, IDictionary<string, object?>? initialState = null)
            => Add(new SimpleCommand(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SimpleCommand"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// </returns>
        public ICommandBinding Add(Action callback, IDictionary<string, object?>? initialState = null)
            => Add(new SimpleCommand(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SimpleCommand{TParam}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TParam">The type of the command parameter.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="getParam">The delegate that returns the command parameter value for the <paramref name="callback"/> delegate when the command is executed.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TParam>(Action<ICommandState, TParam> callback, Func<TParam> getParam, IDictionary<string, object?>? initialState = null)
        {
            if (getParam == null!)
                Throw.ArgumentNullException(Argument.getParam);
            return Add(new SimpleCommand<TParam>(callback), initialState, true).WithParameter(() => getParam.Invoke());
        }

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SimpleCommand{TParam}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TParam">The type of the command parameter.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="getParam">The delegate that returns the command parameter value for the <paramref name="callback"/> delegate when the command is executed.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TParam>(Action<TParam> callback, Func<TParam> getParam, IDictionary<string, object?>? initialState = null)
        {
            if (getParam == null!)
                Throw.ArgumentNullException(Argument.getParam);
            return Add(new SimpleCommand<TParam>(callback), initialState, true).WithParameter(() => getParam.Invoke());
        }

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareCommand{TEventArgs}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event argument of the source events.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs>(Action<ICommandSource<TEventArgs>, ICommandState> callback, IDictionary<string, object?>? initialState = null) where TEventArgs : EventArgs
            => Add(new SourceAwareCommand<TEventArgs>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareCommand{TEventArgs}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event argument of the source events.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs>(Action<ICommandSource<TEventArgs>> callback, IDictionary<string, object?>? initialState = null) where TEventArgs : EventArgs
            => Add(new SourceAwareCommand<TEventArgs>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareCommand{TEventArgs,TParam}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event argument of the source events.</typeparam>
        /// <typeparam name="TParam">The type of the command parameter.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="getParam">The delegate that returns the command parameter value for the <paramref name="callback"/> delegate when the command is executed.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs, TParam>(Action<ICommandSource<TEventArgs>, ICommandState, TParam> callback, Func<TParam> getParam, IDictionary<string, object?>? initialState = null) where TEventArgs : EventArgs
        {
            if (getParam == null!)
                Throw.ArgumentNullException(Argument.getParam);
            return Add(new SourceAwareCommand<TEventArgs, TParam>(callback), initialState, true).WithParameter(() => getParam.Invoke());
        }

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareCommand{TEventArgs,TParam}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event argument of the source events.</typeparam>
        /// <typeparam name="TParam">The type of the command parameter.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="getParam">The delegate that returns the command parameter value for the <paramref name="callback"/> delegate when the command is executed.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs, TParam>(Action<ICommandSource<TEventArgs>, TParam> callback, Func<TParam> getParam, IDictionary<string, object?>? initialState = null) where TEventArgs : EventArgs
        {
            if (getParam == null!)
                Throw.ArgumentNullException(Argument.getParam);
            return Add(new SourceAwareCommand<TEventArgs, TParam>(callback), initialState, true).WithParameter(() => getParam.Invoke());
        }

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="TargetedCommand{TTarget}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the targets of the command binding.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TTarget>(Action<ICommandState, TTarget> callback, IDictionary<string, object?>? initialState = null)
            => Add(new TargetedCommand<TTarget>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="TargetedCommand{TTarget}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the targets of the command binding.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TTarget>(Action<TTarget> callback, IDictionary<string, object?>? initialState = null)
            => Add(new TargetedCommand<TTarget>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="TargetedCommand{TTarget,TParam}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the targets of the command binding.</typeparam>
        /// <typeparam name="TParam">The type of the command parameter.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="getParam">The delegate that returns the command parameter value for the <paramref name="callback"/> delegate when the command is executed.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TTarget, TParam>(Action<ICommandState, TTarget, TParam> callback, Func<TParam> getParam, IDictionary<string, object?>? initialState = null)
        {
            if (getParam == null!)
                Throw.ArgumentNullException(Argument.getParam);
            return Add(new TargetedCommand<TTarget, TParam>(callback), initialState, true).WithParameter(() => getParam.Invoke());
        }

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="TargetedCommand{TTarget,TParam}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the targets of the command binding.</typeparam>
        /// <typeparam name="TParam">The type of the command parameter.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="getParam">The delegate that returns the command parameter value for the <paramref name="callback"/> delegate when the command is executed.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TTarget, TParam>(Action<TTarget, TParam> callback, Func<TParam> getParam, IDictionary<string, object?>? initialState = null)
        {
            if (getParam == null!)
                Throw.ArgumentNullException(Argument.getParam);
            return Add(new TargetedCommand<TTarget, TParam>(callback), initialState, true).WithParameter(() => getParam.Invoke());
        }

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareTargetedCommand{TEventArgs,TTarget}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the targets of the command binding.</typeparam>
        /// <typeparam name="TEventArgs">The type of the event argument of the source events.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs, TTarget>(Action<ICommandSource<TEventArgs>, ICommandState, TTarget> callback, IDictionary<string, object?>? initialState = null) where TEventArgs : EventArgs
            => Add(new SourceAwareTargetedCommand<TEventArgs, TTarget>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareTargetedCommand{TEventArgs,TTarget}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the targets of the command binding.</typeparam>
        /// <typeparam name="TEventArgs">The type of the event argument of the source events.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs, TTarget>(Action<ICommandSource<TEventArgs>, TTarget> callback, IDictionary<string, object?>? initialState = null) where TEventArgs : EventArgs
            => Add(new SourceAwareTargetedCommand<TEventArgs, TTarget>(callback), initialState, true);

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareTargetedCommand{TEventArgs,TTarget,TParam}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the targets of the command binding.</typeparam>
        /// <typeparam name="TEventArgs">The type of the event argument of the source events.</typeparam>
        /// <typeparam name="TParam">The type of the command parameter.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="getParam">The delegate that returns the command parameter value for the <paramref name="callback"/> delegate when the command is executed.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs, TTarget, TParam>(Action<ICommandSource<TEventArgs>, ICommandState, TTarget, TParam> callback, Func<TParam> getParam, IDictionary<string, object?>? initialState = null) where TEventArgs : EventArgs
        {
            if (getParam == null!)
                Throw.ArgumentNullException(Argument.getParam);
            return Add(new SourceAwareTargetedCommand<TEventArgs, TTarget, TParam>(callback), initialState, true).WithParameter(() => getParam.Invoke());
        }

        /// <summary>
        /// Creates a binding with an internally created disposable <see cref="SourceAwareTargetedCommand{TEventArgs,TTarget,TParam}"/> for the specified <paramref name="callback"/>
        /// without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// The created binding will be added to this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the targets of the command binding.</typeparam>
        /// <typeparam name="TEventArgs">The type of the event argument of the source events.</typeparam>
        /// <typeparam name="TParam">The type of the command parameter.</typeparam>
        /// <param name="callback">The delegate to create the command from.</param>
        /// <param name="getParam">The delegate that returns the command parameter value for the <paramref name="callback"/> delegate when the command is executed.</param>
        /// <param name="initialState">The initial state of the binding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the command invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public ICommandBinding Add<TEventArgs, TTarget, TParam>(Action<ICommandSource<TEventArgs>, TTarget, TParam> callback, Func<TParam> getParam, IDictionary<string, object?>? initialState = null) where TEventArgs : EventArgs
        {
            if (getParam == null!)
                Throw.ArgumentNullException(Argument.getParam);
            return Add(new SourceAwareTargetedCommand<TEventArgs, TTarget, TParam>(callback), initialState, true).WithParameter(() => getParam.Invoke());
        }

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
            if (item == null!)
                Throw.ArgumentNullException(Argument.item);
            if (item.IsDisposed)
                Throw.ArgumentException(Argument.item, Res.ComponentModelCannotAddDisposedBinding);
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
            if (item == null!)
                Throw.ArgumentNullException(Argument.item);
            if (item.IsDisposed)
                Throw.ArgumentException(Argument.item, Res.ComponentModelCannotAddDisposedBinding);
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
        protected override void RemoveItemAt(int index)
        {
            this[index].Dispose();
            base.RemoveItemAt(index);
        }

        /// <summary>
        /// Removes all elements from the <see cref="CommandBindingsCollection" />.
        /// The removed bindings will be disposed.
        /// </summary>
        protected override void ClearItems()
        {
            var length = Count;
            for (int i = length - 1; i >= 0; i--)
                RemoveItemAt(i);
        }

        /// <summary>
        /// Releases every binding in this <see cref="CommandBindingsCollection"/>.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to explicit disposing, <see langword="false"/>&#160;if finalizing the object.</param>
        protected virtual void Dispose(bool disposing) => ClearItems();

        #endregion

        #region Private Methods

        private ICommandBinding DoAddPropertyBinding(object source, string sourcePropertyName, string targetPropertyName, Func<object?, object?>? format, object[]? targets, bool? awaitCompletion)
        {
            ICommandBinding result = Command.CreatePropertyBinding(source, sourcePropertyName, targetPropertyName, format, targets, false, awaitCompletion);
            Add(result);

            // syncing the targets only after adding the binding to the collection so a possible derived type can subscribe the binding events in InsertItem
            if (!targets.IsNullOrEmpty())
            {
                bool isNotifyPropertyChanged = source is INotifyPropertyChanged;
                string eventName = isNotifyPropertyChanged ? nameof(INotifyPropertyChanged.PropertyChanged) : sourcePropertyName + "Changed";
                result.InvokeCommand(source, eventName, isNotifyPropertyChanged ? new PropertyChangedEventArgs(sourcePropertyName) : EventArgs.Empty);
            }

            return result;
        }


        #endregion

        #endregion
    }
}
