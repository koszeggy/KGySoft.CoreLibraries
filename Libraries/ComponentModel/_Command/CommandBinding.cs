#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CommandBinding.cs
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
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    internal sealed class CommandBinding : ICommandBinding
    {
        #region Nested types

        #region SubscriptionInfo class

        private abstract class SubscriptionInfo
        {
            #region Fields

            internal CommandBinding Binding;
            internal string EventName;
            internal object Source;
            internal Delegate Delegate;

            #endregion
        }

        #endregion

        #region SubscriptionInfo<TEventArgs> class

        /// <summary>
        /// For providing a matching signature for any event handler.
        /// </summary>
        private sealed class SubscriptionInfo<TEventArgs> : SubscriptionInfo
            where TEventArgs : EventArgs
        {
            #region Methods

            internal void Execute(object sender, TEventArgs e) => Binding.InvokeCommand(new CommandSource<TEventArgs> { Source = Source, TriggeringEvent = EventName, EventArgs = e });

            #endregion
        }

        #endregion

        #region CommandGenericWrapper struct

        private struct CommandGenericWrapper<TEventArgs> : ICommand<TEventArgs> where TEventArgs : EventArgs
        {
            #region Fields

            private readonly ICommand command;

            #endregion

            #region Constructors

            internal CommandGenericWrapper(ICommand command) => this.command = command;

            #endregion

            #region Methods

            void ICommand<TEventArgs>.Execute(ICommandSource<TEventArgs> source, ICommandState state, object target) => command.Execute(source, state, target);
            void ICommand.Execute(ICommandSource source, ICommandState state, object target) => throw new InvalidOperationException();

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        private static readonly IThreadSafeCacheAccessor<Type, Dictionary<string, EventInfo>> eventsCache = new Cache<Type, Dictionary<string, EventInfo>>(t =>
            t.GetEvents(BindingFlags.Public | BindingFlags.Instance).ToDictionary(e => e.Name, e => e)).GetThreadSafeAccessor();

        #endregion

        #region Instance Fields

        private readonly ICommand command;
        private readonly bool disposeCommand;
        private readonly HashSet<object> targets = new HashSet<object>();
        private readonly CommandState state;
        private readonly Dictionary<object, Dictionary<EventInfo, SubscriptionInfo>> sources = new Dictionary<object, Dictionary<EventInfo, SubscriptionInfo>>();
        private readonly CircularList<ICommandStateUpdater> stateUpdaters = new CircularList<ICommandStateUpdater>();

        private bool disposed;

        #endregion

        #endregion

        #region Properties

        public ICommandState State => state;

        #endregion

        #region Constructors

        internal CommandBinding(ICommand command, IDictionary<string, object> initialState, bool disposeCommand)
        {
            this.command = command ?? throw new ArgumentNullException(nameof(command), Res.ArgumentNull);
            this.disposeCommand = disposeCommand;
            state = initialState is CommandState s ? s : new CommandState(initialState);
            state.PropertyChanged += State_PropertyChanged;
        }

        #endregion

        #region Methods

        #region Public Methods

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            state.PropertyChanged -= State_PropertyChanged;

            foreach (object source in sources.Keys.ToArray())
                RemoveSource(source);

            foreach (ICommandStateUpdater stateUpdater in stateUpdaters)
                stateUpdater.Dispose();
            stateUpdaters.Reset();

            targets.Clear();
            if (disposeCommand)
                (command as IDisposable)?.Dispose();
        }

        public ICommandBinding AddSource(object source, string eventName)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.ArgumentNull);
            if (eventName == null)
                throw new ArgumentNullException(nameof(eventName), Res.ArgumentNull);

            Type sourceType = source.GetType();
            if (!eventsCache[sourceType].TryGetValue(eventName, out EventInfo eventInfo))
                throw new ArgumentException(Res.CommandBindingMissingEvent(eventName, sourceType), nameof(eventName));

            MethodInfo invokeMethod = eventInfo.EventHandlerType.GetMethod(nameof(Action.Invoke));
            ParameterInfo[] parameters = invokeMethod?.GetParameters();
            if (invokeMethod?.ReturnType != typeof(void) || parameters.Length != 2 || parameters[0].ParameterType != typeof(object) || !typeof(EventArgs).IsAssignableFrom(parameters[1].ParameterType))
                throw new ArgumentException(Res.CommandBindingInvalidEvent(eventName), nameof(eventName));

            // already added
            if (sources.TryGetValue(source, out var subscriptions) && subscriptions.ContainsKey(eventInfo))
                return this;

            // creating generic info by reflection because the signature must match and EventArgs can vary
            var info = (SubscriptionInfo)Activator.CreateInstance(typeof(SubscriptionInfo<>).MakeGenericType(parameters[1].ParameterType));
            info.Source = source;
            info.EventName = eventName;
            info.Binding = this;

            // subscribing the event by info.Execute
            info.Delegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, info, nameof(SubscriptionInfo<EventArgs>.Execute));
            Reflector.RunMethod(source, eventInfo.GetAddMethod(), info.Delegate);

            if (subscriptions == null)
                sources[source] = new Dictionary<EventInfo, SubscriptionInfo> { { eventInfo, info } };
            else
                subscriptions[eventInfo] = info;

            UpdateSource(source);
            return this;
        }

        public bool RemoveSource(object source)
        {
            if (!sources.TryGetValue(source, out Dictionary<EventInfo, SubscriptionInfo> subscriptions))
                return false;

            foreach (KeyValuePair<EventInfo, SubscriptionInfo> subscriptionInfo in subscriptions)
                Reflector.RunMethod(source, subscriptionInfo.Key.GetRemoveMethod(), subscriptionInfo.Value.Delegate);

            return sources.Remove(source);
        }

        public ICommandBinding AddStateUpdater(ICommandStateUpdater updater)
        {
            stateUpdaters.Add(updater);
            return this;
        }

        public bool RemoveStateUpdater(ICommandStateUpdater updater)
        {
            return stateUpdaters.Remove(updater);
        }

        public ICommandBinding AddTarget(object target)
        {
            targets.Add(target ?? throw new ArgumentNullException(nameof(target), Res.ArgumentNull));
            return this;
        }

        public ICommandBinding AddTarget(Func<object> getTarget) => AddTarget((object)getTarget);

        public bool RemoveTarget(object target)
        {
            return targets.Remove(target);
        }

        #endregion

        #region Private Methods

        private void UpdateSource(object source)
        {
            foreach (string propertyName in ((IDictionary<string, object>)state).Keys)
                UpdateSource(source, propertyName);
        }

        private void UpdateSource(object source, string propertyName)
        {
            if (!state.TryGetValue(propertyName, out object stateValue))
                return;

            foreach (ICommandStateUpdater updater in stateUpdaters)
            {
                if (updater.TryUpdateState(source, propertyName, stateValue))
                    return;
            }
        }

        private void InvokeCommand<TEventArgs>(CommandSource<TEventArgs> source)
            where TEventArgs : EventArgs
        {
            if (disposed || !state.Enabled)
                return;

            ICommand<TEventArgs> cmd = command as ICommand<TEventArgs> ?? new CommandGenericWrapper<TEventArgs>(command);
            if (targets.IsNullOrEmpty())
                cmd.Execute(source, state, null);
            else
            {
                foreach (object targetEntry in targets)
                {
                    object target = targetEntry is Func<object> factory ? factory.Invoke() : targetEntry;
                    cmd.Execute(source, state, target);
                    if (disposed || !state.Enabled)
                        return;
                }
            }
        }

        #endregion

        #region Event handlers

        private void State_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            foreach (IComponent component in sources.Keys)
                UpdateSource(component, e.PropertyName);
        }

        #endregion

        #endregion
    }
}
