#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Command.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using KGySoft.Collections;
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
        private const string stateHandler = nameof(stateHandler);
        private const string statePropertyNames = nameof(statePropertyNames);

        #endregion

        #region Properties

        private static readonly ICommand updatePropertyCommand = new SourceAwareTargetedCommand<EventArgs, object>(OnUpdatePropertyCommand);
        private static readonly ICommand propertyChangedCommand = new SourceAwareCommand<PropertyChangedEventArgs>(OnPropertyChangedCommand);

        #endregion

        #region Methods

        #region Public Methods

        #region CreateBinding

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
        /// <param name="disposeCommand"><see langword="true"/> to dispose the possibly disposable <paramref name="command"/> when the returned <see cref="ICommandBinding"/> is disposed; <see langword="false"/> to keep the <paramref name="command"/> alive when the returned <see cref="ICommandBinding"/> is disposed.
        /// Use <see langword="true"/> only if the command will not be re-used elsewhere. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the <paramref name="command"/> invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public static ICommandBinding CreateBinding(this ICommand command, IDictionary<string, object?>? initialState = null, bool disposeCommand = false)
            => new CommandBinding(command, initialState, disposeCommand);

        #endregion

        #region CreatePropertyBinding
        
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
            => CreatePropertyBinding((object)source, sourcePropertyName, targetPropertyName, null, targets, true, null);

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
            => CreatePropertyBinding((object)source, sourcePropertyName, targetPropertyName, format, targets, true, null);

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

        #endregion

        #region CreateSynchronizedPropertyBinding

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>, which allows to update the
        /// specified <paramref name="targetPropertyName"/> in the <paramref name="targets"/>, when the property of <paramref name="sourcePropertyName"/> changes in the <paramref name="source"/>.
        /// The target properties will be set using the <see cref="SynchronizationContext"/> of the thread on which this method was called.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="sourcePropertyName"/> parameter is observed.</param>
        /// <param name="sourcePropertyName">The name of the property, whose change is observed.</param>
        /// <param name="targetPropertyName">The name of the property in the target object(s).</param>
        /// <param name="awaitCompletion"><see langword="true"/> to block the thread of the triggering event until setting a target property is completed; otherwise, <see langword="false"/>.</param>
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
        /// <param name="awaitCompletion"><see langword="true"/> to block the thread of the triggering event until setting a target property is completed; otherwise, <see langword="false"/>.</param>
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
        /// <param name="awaitCompletion"><see langword="true"/> to block the thread of the triggering event until setting a target property is completed; otherwise, <see langword="false"/>.</param>
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
        /// <param name="awaitCompletion"><see langword="true"/> to block the thread of the triggering event until setting a target property is completed; otherwise, <see langword="false"/>.</param>
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

        #region CreateTwoWayPropertyBinding

        /// <summary>
        /// Creates a pair of special bindings for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>
        /// and <paramref name="target"/>, which allow to update the specified <paramref name="targetPropertyName"/> and <paramref name="sourcePropertyName"/> in both directions when any of them changes.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="sourcePropertyName"/> parameter is observed.</param>
        /// <param name="sourcePropertyName">The name of the <paramref name="source"/> property, whose change is observed.</param>
        /// <param name="target">The target object, whose property specified by the <paramref name="targetPropertyName"/> parameter is observed.</param>
        /// <param name="targetPropertyName">The name of the <paramref name="target"/> property, whose change is observed. If <see langword="null"/>,
        /// then it is considered as the same as <paramref name="sourcePropertyName"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="format">If not <see langword="null"/>, then can be used to format the value to be set in the <paramref name="target"/> object. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="parse">If not <see langword="null"/>, then can be used to parse the value to be set in the <paramref name="source"/> object. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>The created pair of <see cref="ICommandBinding"/> instances.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="sourcePropertyName"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="target"/> is neither an <see cref="INotifyPropertyChanged"/> implementation nor has a <c><paramref name="sourcePropertyName"/>Changed</c> event.</exception>
        public static ICommandBinding[] CreateTwoWayPropertyBinding(this INotifyPropertyChanged source, string sourcePropertyName, INotifyPropertyChanged target,
            string? targetPropertyName = null, Func<object?, object?>? format = null, Func<object?, object?>? parse = null)
        {
            var result = new ICommandBinding[]
            {
                CreatePropertyBinding((object)source, sourcePropertyName, targetPropertyName ?? sourcePropertyName, format, new object[] { target }, false, null),
                CreatePropertyBinding((object)target, targetPropertyName ?? sourcePropertyName, sourcePropertyName, parse, new object[] { source }, false, null)
            };

            // Syncing only from source to target and only when both bindings could be created successfully
            result[0].InvokeCommand(source, nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(sourcePropertyName));
            return result;
        }

        /// <summary>
        /// Creates a pair of special bindings for the <see cref="INotifyPropertyChanged.PropertyChanged"/> or <c><paramref name="sourcePropertyName"/>Changed</c> event of the specified <paramref name="source"/>
        /// and <paramref name="target"/>, which allow to update the specified <paramref name="targetPropertyName"/> and <paramref name="sourcePropertyName"/> in both directions when any of them changes.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="sourcePropertyName"/> parameter is observed.</param>
        /// <param name="sourcePropertyName">The name of the <paramref name="source"/> property, whose change is observed.</param>
        /// <param name="target">The target object, whose property specified by the <paramref name="targetPropertyName"/> parameter is observed.</param>
        /// <param name="targetPropertyName">The name of the <paramref name="target"/> property, whose change is observed. If <see langword="null"/>,
        /// then it is considered as the same as <paramref name="sourcePropertyName"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="format">If not <see langword="null"/>, then can be used to format the value to be set in the <paramref name="target"/> object. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="parse">If not <see langword="null"/>, then can be used to parse the value to be set in the <paramref name="source"/> object. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>The created pair of <see cref="ICommandBinding"/> instances.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="sourcePropertyName"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="target"/> is neither an <see cref="INotifyPropertyChanged"/> implementation nor has a <c><paramref name="sourcePropertyName"/>Changed</c> event.</exception>
        public static ICommandBinding[] CreateTwoWayPropertyBinding(object source, string sourcePropertyName, object target,
            string? targetPropertyName = null, Func<object?, object?>? format = null, Func<object?, object?>? parse = null)
        {
            var result = new ICommandBinding[]
            {
                CreatePropertyBinding(source, sourcePropertyName, targetPropertyName ?? sourcePropertyName, format, new[] { target }, false, null),
                CreatePropertyBinding(target, targetPropertyName ?? sourcePropertyName, sourcePropertyName, parse, new[] { source }, false, null)
            };

            // Syncing only from source to target and only when both bindings could be created successfully
            bool isNotifyPropertyChanged = source is INotifyPropertyChanged;
            string eventName = isNotifyPropertyChanged ? nameof(INotifyPropertyChanged.PropertyChanged) : sourcePropertyName + "Changed";
            result[0].InvokeCommand(source, eventName, isNotifyPropertyChanged ? new PropertyChangedEventArgs(sourcePropertyName) : EventArgs.Empty);
            return result;
        }

        #endregion

        #region CreateFilteredPropertyChangedHandler

        /// <summary>
        /// Creates a special command binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>
        /// that invokes the specified <paramref name="handler"/> only when the changed property is among the specified <paramref name="propertyNames"/>.
        /// </summary>
        /// <param name="source">The source object, whose <see cref="INotifyPropertyChanged.PropertyChanged"/> event is observed.</param>
        /// <param name="handler">The delegate to be invoked when the changed property is among the specified <paramref name="propertyNames"/>.</param>
        /// <param name="propertyNames">The property names, whose change invoke the specified <paramref name="handler"/>.</param>
        /// <returns>The created <see cref="ICommandBinding"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="propertyNames"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyNames"/> is empty.</exception>
        public static ICommandBinding CreatePropertyChangedHandlerBinding(this INotifyPropertyChanged source, Action handler, params string[] propertyNames)
            => DoCreatePropertyChangedHandlerBinding(source, handler, propertyNames);

        /// <summary>
        /// Creates a special command binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>
        /// that invokes the specified <paramref name="handler"/> only when the changed property is among the specified <paramref name="propertyNames"/>.
        /// </summary>
        /// <param name="source">The source object, whose <see cref="INotifyPropertyChanged.PropertyChanged"/> event is observed.</param>
        /// <param name="handler">The delegate to be invoked when the changed property is among the specified <paramref name="propertyNames"/>. Its parameter is the name of the changed property.</param>
        /// <param name="propertyNames">The property names, whose change invoke the specified <paramref name="handler"/>.</param>
        /// <returns>The created <see cref="ICommandBinding"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="propertyNames"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyNames"/> is empty.</exception>
        public static ICommandBinding CreatePropertyChangedHandlerBinding(this INotifyPropertyChanged source, Action<string> handler, params string[] propertyNames)
            => DoCreatePropertyChangedHandlerBinding(source, handler, propertyNames);

        #endregion

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
            ICommandBinding result = updatePropertyCommand.CreateBinding(state)
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

        private static ICommandBinding DoCreatePropertyChangedHandlerBinding(INotifyPropertyChanged source, Delegate handler, string[] propertyNames)
        {
            if (handler == null!)
                Throw.ArgumentNullException(Argument.handler);
            if (propertyNames == null!)
                Throw.ArgumentNullException(Argument.propertyNames);
            if (propertyNames.Length == 0)
                Throw.ArgumentException(Argument.propertyNames, Res.ArgumentEmpty);

            return propertyChangedCommand.CreateBinding(new StringKeyedDictionary<object?>(2) { { stateHandler, handler }, { statePropertyNames, propertyNames } })
                .AddSource(source, nameof(source.PropertyChanged));
        }

        private static void OnUpdatePropertyCommand(ICommandSource src, ICommandState state, object target)
        {
            #region Local Methods

            static void DoSetProperty(object target, string targetPropertyName, object? propertyValue)
            {
                switch (target)
                {
                    case IPersistableObject persistableTarget:
                        // if there is no such actual settable property, then setting by interface
                        if (!Reflector.TrySetProperty(target, targetPropertyName, propertyValue))
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

        private static void OnPropertyChangedCommand(ICommandSource<PropertyChangedEventArgs> source, ICommandState state)
        {
            if (!source.EventArgs.PropertyName.In(state.GetValueOrDefault<string[]>(statePropertyNames)))
                return;

            switch (state.GetValueOrDefault<Delegate>(stateHandler))
            {
                case Action handler:
                    handler.Invoke();
                    break;
                case Action<string?> handler:
                    handler.Invoke(source.EventArgs.PropertyName);
                    break;
                default:
                    // null or unexpected type
                    Throw.InvalidOperationException(Res.ComponentModelMissingState(stateHandler));
                    break;
            }
        }

        #endregion

        #endregion
    }
}
