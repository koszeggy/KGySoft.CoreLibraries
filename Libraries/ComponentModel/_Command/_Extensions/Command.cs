using System;
using System.Collections.Generic;
using System.ComponentModel;
using KGySoft.Libraries;
using KGySoft.Reflection;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Contains extension methods for the <see cref="ICommand"/> type.
    /// </summary>
    public static class Command
    {
        private const string statePropertyName = nameof(statePropertyName);
        private const string stateFormatValue = nameof(stateFormatValue);
        private static ICommand BindPropertyCommand { get; } = new SourceAwareTargetedCommand<PropertyChangedEventArgs, object>(OnBindPropertyCommand);

        private static void OnBindPropertyCommand(ICommandSource<PropertyChangedEventArgs> src, ICommandState state, object target)
        {
            string propertyName = state.GetValueOrDefault<string>(statePropertyName) ?? throw new InvalidOperationException(Res.Get(Res.PropertyBindingNoPropertyName));
            if (propertyName != src.EventArgs.PropertyName)
                return;

            if (!src.EventArgs.TryGetNewPropertyValue(out object propertyValue))
            {
                object source = src.Source;
                propertyValue = source is IPersistableObject persistableSource && persistableSource.TryGetPropertyValue(propertyName, out propertyValue)
                    || source is ICommandState stateSource && stateSource.TryGetValue(propertyName, out propertyValue)
                        ? propertyValue : Reflector.GetInstancePropertyByName(source, propertyName);
            }

            Func<object, object> formatValue = state.GetValueOrDefault<Func<object, object>>(stateFormatValue);
            if (formatValue != null)
                propertyValue = formatValue?.Invoke(propertyValue);

            if (target is IPersistableObject persistableTarget)
                persistableTarget.SetProperty(propertyName, propertyValue);
            else if (target is ICommandState stateTarget)
                stateTarget[propertyName] = propertyValue;
            else
                Reflector.SetInstancePropertyByName(target, propertyName, propertyValue);
        }

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
        {
            ICommandBinding result = command.CreateBinding(initialState)
                .AddSource(source ?? throw new ArgumentNullException(nameof(source), Res.ArgumentNull), eventName ?? throw new ArgumentNullException(nameof(eventName), Res.ArgumentNull));
            if (!targets.IsNullOrEmpty())
            {
                foreach (object target in targets)
                    result.AddTarget(target);
            }

            return result;
        }

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> using the specified <paramref name="source"/>, <paramref name="eventName"/> and <paramref name="targets"/>.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="source">The source, which can trigger the command.</param>
        /// <param name="eventName">The name of the event on the <paramref name="source"/> that can trigger the command.</param>
        /// <param name="targets">Zero or more targets for the binding.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        public static ICommandBinding CreateBinding(this ICommand command, object source, string eventName, params object[] targets)
            => command.CreateBinding(source, eventName, null, targets);

        /// <summary>
        /// Creates a binding for a <paramref name="command"/> without any sources and targets. At least one source must be added by the <see cref="ICommandBinding.AddSource">ICommandBinding.AddSource</see> method to make the command invokable.
        /// If an initial state was provided, then its entries will be applied on the added sources. Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">ICommandBinding.AddTarget</see> method.
        /// </summary>
        /// <param name="command">The command to bind.</param>
        /// <param name="initialState">The initial state of the binding. The state entries will be applied to the sources when they are added to the binding.</param>
        /// <param name="disposeCommand"><see langword="true"/> to dispose the possibly disposable <paramref name="command"/> when the returned <see cref="ICommandBinding"/> is disposed; <see langword="false"/> to keep the <paramref name="command"/> alive when the returned <see cref="ICommandBinding"/> is disposed.
        /// Use <see langword="true"/> only if the command will not be re-used elsewhere. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, whose <see cref="ICommandBinding.State"/> is initialized by the provided <paramref name="initialState"/>.
        /// To make the <paramref name="command"/> invokable by this binding, at least one source must be added by the <see cref="ICommandBinding.AddSource">AddSource</see> method on the result.
        /// Targets can be added by the <see cref="ICommandBinding.AddTarget(object)">AddTarget</see> method on the result.
        /// </returns>
        public static ICommandBinding CreateBinding(this ICommand command, IDictionary<string, object> initialState = null, bool disposeCommand = false)
            => new CommandBinding(command, initialState, disposeCommand);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>, which allows to update the
        /// specified property in the <paramref name="targets"/>, when the property of the same name changes in the <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="propertyName"/> parameter is observed.</param>
        /// <param name="propertyName">The name of the property, whose change is observed.</param>
        /// <param name="targets">The targets to be updated. If the concrete instances to update have to be returned when the change occurs use the <see cref="ICommandBinding.AddTarget(Func{object})">ICommandBinding.AddTarget</see>
        /// method on the result <see cref="ICommandBinding"/> instance.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified <paramref name="propertyName"/> and <paramref name="format"/>parameters.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="propertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public static ICommandBinding CreatePropertyBinding(this INotifyPropertyChanged source, string propertyName, params object[] targets)
            => CreatePropertyBinding(source, propertyName, null, targets);

        /// <summary>
        /// Creates a special binding for the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/>, which allows to update the
        /// specified property in the <paramref name="targets"/>, when the property of the same name changes in the <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source object, whose property specified by the <paramref name="propertyName"/> parameter is observed.</param>
        /// <param name="propertyName">The name of the property, whose change is observed.</param>
        /// <param name="format">If not <see langword="null"/>, then can be used to format the value to be set in the <paramref name="targets"/>.</param>
        /// <param name="targets">The targets to be updated. If the concrete instances to update have to be returned when the change occurs use the <see cref="ICommandBinding.AddTarget(Func{object})">ICommandBinding.AddTarget</see>
        /// method on the result <see cref="ICommandBinding"/> instance.</param>
        /// <returns>An <see cref="ICommandBinding"/> instance, to which the specified <paramref name="source"/> and <paramref name="targets"/> are bound.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/><paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>This method uses a prepared command internally, which is bound to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the specified <paramref name="source"/> object.</para>
        /// <para>The <see cref="ICommandState"/>, which is created for the underlying command contains the specified <paramref name="propertyName"/> and <paramref name="format"/>parameters.
        /// Do not remove these state entries; otherwise, the command will throw an <see cref="InvalidOperationException"/> when executed.</para>
        /// <para>The property with <paramref name="propertyName"/> will be set in the specified <paramref name="targets"/> immediately when this method is called.
        /// The targets, which are added later by the <see cref="O:KGySoft.ComponentModel.ICommandBinding.AddTarget">ICommandBinding.AddTarget</see> methods, are set only when the
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event occurs on the <paramref name="source"/> object.</para>
        /// </remarks>
        public static ICommandBinding CreatePropertyBinding(this INotifyPropertyChanged source, string propertyName, Func<object, object> format, params object[] targets)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.Get(Res.ArgumentNull));
            var state = new CommandState { { statePropertyName, propertyName }, { stateFormatValue, format } };
            ICommandBinding result = BindPropertyCommand.CreateBinding(state, true)
                .AddStateUpdater(NullStateUpdater.Updater)
                .AddSource(source, nameof(source.PropertyChanged));
            if (!targets.IsNullOrEmpty())
            {
                var commandSource = new CommandSource<PropertyChangedEventArgs>
                {
                    EventArgs = new PropertyChangedEventArgs(propertyName),
                    Source = source,
                    TriggeringEvent = nameof(INotifyPropertyChanged.PropertyChanged)
                };

                foreach (object target in targets)
                {
                    result.AddTarget(target);
                    OnBindPropertyCommand(commandSource, state, target);
                }
            }

            return result;
        }

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
