#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Command.cs
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

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Contains extension methods for the <see cref="ICommand"/> and <see cref="INotifyPropertyChanged"/> types as well as a couple of property binding creation methods for any object.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.
    /// </summary>
    public static class Command
    {
        #region Constants

        private const string stateSourcePropertyName = nameof(stateSourcePropertyName);
        private const string stateTargetPropertyName = nameof(stateTargetPropertyName);
        private const string stateFormatValue = nameof(stateFormatValue);
        private const string stateSyncContext = nameof(stateSyncContext);
        private const string stateAwaitCompletion = nameof(stateAwaitCompletion);

        #endregion

        #region Properties

        private static ICommand UpdatePropertyCommand { get; } = new SourceAwareTargetedCommand<EventArgs, object>(OnUpdatePropertyCommand);

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/> as well as the optionally provided initial state of the binding.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command. Can be a <see cref="Type"/> for static events.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="initialState">The initial state of the binding.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/> and to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public static ICommandBinding CreateBinding(this ICommand command, object source, string eventName, IDictionary<string, object?>? initialState = null, params object[]? targets)
        {
            if (source == null!)
                Throw.ArgumentNullException(Argument.source);
            if (eventName == null!)
                Throw.ArgumentNullException(Argument.eventName);
            ICommandBinding result = command.CreateBinding(initialState).AddSource(source, eventName);
            if (!targets.IsNullOrEmpty())
            {
                foreach (object target in targets!)
                    result.AddTarget(target);
            }

            return result;
        }

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command. Can be a <see cref="Type"/> for static events.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public static ICommandBinding CreateBinding(this ICommand command, object source, string eventName, params object[]? targets)
            => command.CreateBinding(source, eventName, null, targets);

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="initialState">The initial state of the binding.</param>
        /// <param name="disposeCommand"><see langword="true"/>&#160;to dispose the possibly disposable <paramref name="command"/> when the returned <see cref="ICommandBinding"/> is disposed; <see langword="false"/>&#160;to keep the <paramref name="command"/> alive when the returned <see cref="ICommandBinding"/> is disposed.
        /// Use <see langword="true"/>&#160;only if the command will not be re-used elsewhere. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the <paramref name="command"/> invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public static ICommandBinding CreateBinding(this ICommand command, IDictionary<string, object?>? initialState = null, bool disposeCommand = false)
            => new CommandBinding(command, initialState, disposeCommand);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>, which allows to update the
        /// specified <paramref name="targetPropertyName"/> in the <paramref name="targets"/>, when the property of <paramref name="sourcePropertyName"/> changes in the <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="sourcePropertyName"/> parameter is observed.</param>
        /// <param name="sourcePropertyName">The name of the property, whose change is observed.</param>
        /// <param name="targetPropertyName">The name of the property in the target object(s).</param>
        /// <param name="targets">The targets to be updated. If the concrete instances to update have to be returned when the change occurs use the <see cref="ICommandBinding.AddTarget(Func{object})">ICommandBinding.AddTarget</see>
        /// method on the result <see cref="ICommandBinding"/> instance.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="sourcePropertyName"/> or <paramref name="targetPropertyName"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified property names.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="targetPropertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public static ICommandBinding CreatePropertyBinding(this INotifyPropertyChanged source, string sourcePropertyName, string targetPropertyName, params object[]? targets)
            => CreatePropertyBinding((object)source, sourcePropertyName, targetPropertyName, null, targets);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>, which allows to update the
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
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified property names and <paramref name="format"/>parameters.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="targetPropertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public static ICommandBinding CreatePropertyBinding(this INotifyPropertyChanged source, string sourcePropertyName, string targetPropertyName, Func<object?, object?>? format, params object[]? targets)
            => CreatePropertyBinding((object)source, sourcePropertyName, targetPropertyName, format, targets);

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
        public static ICommandBinding CreatePropertyBinding(object source, string sourcePropertyName, string targetPropertyName, params object[]? targets)
            => CreatePropertyBinding(source, sourcePropertyName, targetPropertyName, null, targets, true, null);

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
        public static ICommandBinding CreatePropertyBinding(object source, string sourcePropertyName, string targetPropertyName, Func<object?, object?>? format, params object[]? targets)
            => CreatePropertyBinding(source, sourcePropertyName, targetPropertyName, format, targets, true, null);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>, which allows to update the
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
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified property names.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="targetPropertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public static ICommandBinding CreateSynchronizedPropertyBinding(this INotifyPropertyChanged source, string sourcePropertyName, string targetPropertyName, bool awaitCompletion, params object[]? targets)
            => CreatePropertyBinding((object)source, sourcePropertyName, targetPropertyName, null, targets, true, awaitCompletion);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>, which allows to update the
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
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified property names and <paramref name="format"/>parameters.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="targetPropertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public static ICommandBinding CreateSynchronizedPropertyBinding(this INotifyPropertyChanged source, string sourcePropertyName, string targetPropertyName, Func<object?, object?>? format, bool awaitCompletion, params object[]? targets)
            => CreatePropertyBinding((object)source, sourcePropertyName, targetPropertyName, format, targets, true, awaitCompletion);

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
        public static ICommandBinding CreateSynchronizedPropertyBinding(object source, string sourcePropertyName, string targetPropertyName, bool awaitCompletion, params object[]? targets)
            => CreatePropertyBinding(source, sourcePropertyName, targetPropertyName, null, targets, true, awaitCompletion);

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
        public static ICommandBinding CreateSynchronizedPropertyBinding(object source, string sourcePropertyName, string targetPropertyName, Func<object?, object?>? format, bool awaitCompletion, params object[]? targets)
            => CreatePropertyBinding(source, sourcePropertyName, targetPropertyName, format, targets, true, awaitCompletion);

        #endregion

        #region Internal Methods

        internal static ICommandBinding CreatePropertyBinding(object source, string sourcePropertyName, string targetPropertyName, Func<object?, object?>? format, object[]? targets, bool syncTargets, bool? awaitCompletion)
        {
            if (source == null!)
                Throw.ArgumentNullException(Argument.source);
            if (sourcePropertyName == null!)
                Throw.ArgumentNullException(Argument.sourcePropertyName);
            if (targetPropertyName == null!)
                Throw.ArgumentNullException(Argument.targetPropertyName);

            var state = new CommandState
            {
                { stateSourcePropertyName, sourcePropertyName },
                { stateTargetPropertyName, targetPropertyName },
                { stateFormatValue, format }
            };

            if (awaitCompletion.HasValue)
            {
                SynchronizationContext? capturedContext = SynchronizationContext.Current;
                if (capturedContext != null)
                {
                    state[stateSyncContext] = capturedContext;
                    state[stateAwaitCompletion] = awaitCompletion.Value;
                }
            }

            bool isNotifyPropertyChanged = source is INotifyPropertyChanged;
            string eventName = isNotifyPropertyChanged ? nameof(INotifyPropertyChanged.PropertyChanged) : sourcePropertyName + "Changed";
            ICommandBinding result = UpdatePropertyCommand.CreateBinding(state)
                .AddStateUpdater(NullStateUpdater.Updater)
                .AddSource(source, eventName);
            if (!targets.IsNullOrEmpty())
            {
                foreach (object target in targets!)
                    result.AddTarget(target);

                if (syncTargets)
                    result.InvokeCommand(source, eventName, isNotifyPropertyChanged ? new PropertyChangedEventArgs(sourcePropertyName) : EventArgs.Empty);
            }

            return result;
        }

        internal static ICommandSource<T> Cast<T>(this ICommandSource orig) where T : EventArgs
            => orig as ICommandSource<T> ?? new CommandSource<T>
            {
                EventArgs = (T)orig.EventArgs,
                Source = orig.Source,
                TriggeringEvent = orig.TriggeringEvent
            };

        #endregion

        #region Private Methods

        private static void OnUpdatePropertyCommand(ICommandSource src, ICommandState state, object target)
        {
            #region Local Methods

            static void DoSetProperty(object target, string targetPropertyName, object? propertyValue)
            {
                switch (target)
                {
                    case IPersistableObject persistableTarget:
                        // setting by interface only if there is such a property in the storage so simple properties are still set by reflection
                        if (persistableTarget.TryGetPropertyValue(targetPropertyName, out var _))
                            persistableTarget.SetProperty(targetPropertyName, propertyValue);
                        else
                            Reflector.SetProperty(target, targetPropertyName, propertyValue);
                        break;
                    case ICommandState stateTarget:
                        stateTarget[targetPropertyName] = propertyValue;
                        break;
                    default:
                        Reflector.SetProperty(target, targetPropertyName, propertyValue);
                        break;
                }
            }

            #endregion

            string? sourcePropertyName = state.GetValueOrDefault<string?>(stateSourcePropertyName);
            if (sourcePropertyName == null)
                Throw.InvalidOperationException(Res.ComponentModelMissingState(stateSourcePropertyName));

            string? targetPropertyName = state.GetValueOrDefault<string?>(stateTargetPropertyName);
            if (targetPropertyName == null)
                Throw.InvalidOperationException(Res.ComponentModelMissingState(stateTargetPropertyName));

            object? propertyValue = null;
            bool propertyValueObtained = false;
            if (src.EventArgs is PropertyChangedEventArgs changedArgs)
            {
                if (changedArgs.PropertyName != sourcePropertyName)
                    return;
                propertyValueObtained = changedArgs.TryGetNewPropertyValue(out propertyValue);
            }

            if (!propertyValueObtained)
            {
                object source = src.Source;
                propertyValue = source is IPersistableObject persistableSource && persistableSource.TryGetPropertyValue(sourcePropertyName, out propertyValue)
                    || source is ICommandState stateSource && stateSource.TryGetValue(sourcePropertyName, out propertyValue)
                        ? propertyValue
                        : Reflector.GetProperty(source, sourcePropertyName);
            }

            var formatValue = state.GetValueOrDefault<Func<object?, object?>?>(stateFormatValue);
            if (formatValue != null)
                propertyValue = formatValue.Invoke(propertyValue);

            var capturedContext = state.GetValueOrDefault<SynchronizationContext?>(stateSyncContext);
            if (capturedContext != null)
            {
                if (state.GetValueOrDefault<bool>(stateAwaitCompletion))
                    capturedContext.Send(_ => DoSetProperty(target, targetPropertyName, propertyValue), null);
                else
                    capturedContext.Post(_ => DoSetProperty(target, targetPropertyName, propertyValue), null);
            }
            else
                DoSetProperty(target, targetPropertyName, propertyValue);
        }

        #endregion

        #endregion
    }
}
