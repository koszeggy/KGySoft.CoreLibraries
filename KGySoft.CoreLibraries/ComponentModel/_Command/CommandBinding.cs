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
        /// To provide a matching signature for any event handler.
        /// </summary>
        private sealed class SubscriptionInfo<TEventArgs> : SubscriptionInfo
            where TEventArgs : EventArgs
        {
            #region Methods

            // ReSharper disable once UnusedParameter.Local - sender must be specified because this method is invoked by event handler delegates
            internal void Execute(object sender, TEventArgs e) => Binding.InvokeCommand(new CommandSource<TEventArgs> { Source = Source, TriggeringEvent = EventName, EventArgs = e });

            #endregion
        }

        #endregion

        #region CommandGenericWrapper class

        private sealed class CommandGenericWrapper<TEventArgs> : ICommand<TEventArgs> where TEventArgs : EventArgs
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
            t.GetEvents().ToDictionary(e => e.Name, e => e)).GetThreadSafeAccessor();

        #endregion

        #region Instance Fields

        private readonly ICommand command;
        private readonly bool disposeCommand;
        private readonly HashSet<object> targets = new HashSet<object>();
        private readonly CommandState state;
        private readonly Dictionary<object, Dictionary<EventInfo, SubscriptionInfo>> sources = new Dictionary<object, Dictionary<EventInfo, SubscriptionInfo>>();
        private readonly CircularList<ICommandStateUpdater> stateUpdaters = new CircularList<ICommandStateUpdater>();

        private bool disposed;
        private EventHandler<ExecuteCommandEventArgs> executing;
        private EventHandler<ExecuteCommandEventArgs> executed;

        #endregion

        #endregion

        #region Events

        public event EventHandler<ExecuteCommandEventArgs> Executing
        {
            add => executing += value;
            remove => executing -= value;
        }

        public event EventHandler<ExecuteCommandEventArgs> Executed
        {
            add => executed += value;
            remove => executed -= value;
        }

        #endregion

        #region Properties

        public bool IsDisposed => disposed;
        public ICommandState State => state;
        public IDictionary<object, string[]> Sources => sources?.ToDictionary(i => i.Key, i => i.Value.Values.Select(si => si.EventName).ToArray());
        public IList<object> Targets => targets?.ToArray();
        public IList<ICommandStateUpdater> StateUpdaters => stateUpdaters.ToArray();

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
            executing = null;
            executed = null;

            foreach (object source in sources.Keys.ToArray())
                DoRemoveSource(source);

            foreach (ICommandStateUpdater stateUpdater in stateUpdaters)
                stateUpdater.Dispose();
            stateUpdaters.Reset();

            targets.Clear();
            if (disposeCommand)
                (command as IDisposable)?.Dispose();
        }

        public ICommandBinding AddSource(object source, string eventName)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.ArgumentNull);
            if (eventName == null)
                throw new ArgumentNullException(nameof(eventName), Res.ArgumentNull);

            Type sourceType = source as Type ?? source.GetType();
            bool isStatic = ReferenceEquals(source, sourceType);
            if (!eventsCache[sourceType].TryGetValue(eventName, out EventInfo eventInfo))
                throw new ArgumentException(Res.ComponentModelMissingEvent(eventName, sourceType), nameof(eventName));
            MethodInfo addMethod = eventInfo.GetAddMethod(true);
            if (addMethod.IsStatic ^ isStatic)
                throw new ArgumentException(Res.ComponentModelInvalidCommandSource, nameof(source));

            MethodInfo invokeMethod = eventInfo.EventHandlerType.GetMethod(nameof(Action.Invoke));
            ParameterInfo[] parameters = invokeMethod?.GetParameters();

            // ReSharper disable once PossibleNullReferenceException - if parameters is null the first condition will match
            if (invokeMethod?.ReturnType != Reflector.VoidType || parameters.Length != 2 || parameters[0].ParameterType != Reflector.ObjectType || !typeof(EventArgs).IsAssignableFrom(parameters[1].ParameterType))
                throw new ArgumentException(Res.ComponentModelInvalidEvent(eventName), nameof(eventName));

            // already added
            if (sources.TryGetValue(source, out Dictionary<EventInfo, SubscriptionInfo> subscriptions) && subscriptions.ContainsKey(eventInfo))
                return this;

            // creating generic info by reflection because the signature must match and EventArgs can vary
            var info = (SubscriptionInfo)Reflector.CreateInstance(typeof(SubscriptionInfo<>).GetGenericType(parameters[1].ParameterType));
            info.Source = source;
            info.EventName = eventName;
            info.Binding = this;

            // subscribing the event by info.Execute
            info.Delegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, info, nameof(SubscriptionInfo<EventArgs>.Execute));
            Reflector.InvokeMethod(isStatic ? null : source, addMethod, info.Delegate);

            if (subscriptions == null)
                sources[source] = new Dictionary<EventInfo, SubscriptionInfo> { { eventInfo, info } };
            else
                subscriptions[eventInfo] = info;

            UpdateSource(source);
            return this;
        }

        public bool RemoveSource(object source)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            return DoRemoveSource(source);
        }

        public ICommandBinding AddStateUpdater(ICommandStateUpdater updater, bool updateSources)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            stateUpdaters.Add(updater);
            if (updateSources && sources.Count > 0)
                GetInstanceSources().ForEach(UpdateSource);
            return this;
        }

        public bool RemoveStateUpdater(ICommandStateUpdater updater)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            return stateUpdaters.Remove(updater);
        }

        public ICommandBinding AddTarget(object target)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            targets.Add(target ?? throw new ArgumentNullException(nameof(target), Res.ArgumentNull));
            return this;
        }

        public ICommandBinding AddTarget(Func<object> getTarget)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            return AddTarget((object)getTarget);
        }

        public bool RemoveTarget(object target)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            return targets.Remove(target);
        }

        public void InvokeCommand(object source, string eventName, EventArgs eventArgs)
        {
            if (disposed)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            InvokeCommand(new CommandSource<EventArgs>
            {
                Source = source ?? throw new ArgumentNullException(nameof(source), Res.ArgumentNull),
                TriggeringEvent = eventName ?? throw new ArgumentNullException(nameof(eventName), Res.ArgumentNull),
                EventArgs = eventArgs ?? EventArgs.Empty
            });
        }

        #endregion

        #region Private Methods

        private void UpdateSource(object source)
        {
            if (stateUpdaters.Count == 0)
                return;
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
            if (disposed)
                return;

            ICommand<TEventArgs> cmd = command as ICommand<TEventArgs> ?? new CommandGenericWrapper<TEventArgs>(command);
            var e = new ExecuteCommandEventArgs(source, state);
            OnExecuting(e);
            if (disposed || !state.Enabled)
                return;
            try
            {
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
            finally
            {
                if (!disposed)
                    OnExecuted(e);
            }
        }

        #endregion

        #region Private Methods

        private bool DoRemoveSource(object source)
        {
            if (!sources.TryGetValue(source, out Dictionary<EventInfo, SubscriptionInfo> subscriptions))
                return false;

            foreach (KeyValuePair<EventInfo, SubscriptionInfo> subscriptionInfo in subscriptions)
                Reflector.InvokeMethod(source is Type ? null : source, subscriptionInfo.Key.GetRemoveMethod(), subscriptionInfo.Value.Delegate);

            return sources.Remove(source);
        }

        private IEnumerable<object> GetInstanceSources() => sources.Keys.Where(k => !(k is Type));

        private void OnExecuting(ExecuteCommandEventArgs e) => executing?.Invoke(this, e);
        private void OnExecuted(ExecuteCommandEventArgs e) => executed?.Invoke(this, e);

        #endregion

        #region Event handlers

        private void State_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (stateUpdaters.Count == 0)
                return;
            foreach (object source in GetInstanceSources())
                UpdateSource(source, e.PropertyName);
        }

        #endregion

        #endregion
    }
}
