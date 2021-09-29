#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CommandBinding.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

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

            // these fields are initialized after creating a concrete instance by reflection so they are not marked as nullable
            internal CommandBinding Binding = null!;
            internal string EventName = null!;
            internal object Source = null!;
            internal Delegate Delegate = null!;

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

            [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "They are necessary")]
            [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
            [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Event handler")]
            [MethodImpl(MethodImpl.AggressiveInlining)]
            internal void Execute(object sender, TEventArgs e)
            {
                object? param = null;
                try
                {
                    param = Binding.getParameterCallback?.Invoke();
                }
                catch (Exception ex) when (!ex.IsCritical())
                {
                    Binding.HandleError(CommandBindingErrorContext.EvaluateParameter, ex);
                }

                Binding.InvokeCommand(new CommandSource<TEventArgs>
                {
                    Source = Source,
                    TriggeringEvent = EventName,
                    EventArgs = e
                }, param);
            }

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        #region Static Fields

        private static readonly IThreadSafeCacheAccessor<Type, StringKeyedDictionary<EventInfo>> eventsCache =
            ThreadSafeCacheFactory.Create<Type, StringKeyedDictionary<EventInfo>>(GetEvents, LockFreeCacheOptions.Profile128);

        #endregion

        #region Instance Fields

        private readonly ICommand command;
        private readonly bool disposeCommand;
        private readonly HashSet<object> targets = new HashSet<object>();
        private readonly CommandState state;
        private readonly Dictionary<object, Dictionary<EventInfo, SubscriptionInfo>> sources = new Dictionary<object, Dictionary<EventInfo, SubscriptionInfo>>();
        private readonly CircularList<ICommandStateUpdater> stateUpdaters = new CircularList<ICommandStateUpdater>();

        private bool disposed;
        private Func<object?>? getParameterCallback;
        private EventHandler<ExecuteCommandEventArgs>? executingHandler;
        private EventHandler<ExecuteCommandEventArgs>? executedHandler;
        private EventHandler<CommandBindingErrorEventArgs>? errorHandler;

        #endregion

        #endregion

        #region Events

        public event EventHandler<ExecuteCommandEventArgs>? Executing
        {
            add => value.AddSafe(ref executingHandler);
            remove => value.RemoveSafe(ref executingHandler);
        }

        public event EventHandler<ExecuteCommandEventArgs>? Executed
        {
            add => value.AddSafe(ref executedHandler);
            remove => value.RemoveSafe(ref executedHandler);
        }

        public event EventHandler<CommandBindingErrorEventArgs>? Error
        {
            add => value.AddSafe(ref errorHandler);
            remove => value.RemoveSafe(ref errorHandler);
        }

        #endregion

        #region Properties

        public bool IsDisposed => disposed;
        public ICommandState State => state;
        public IDictionary<object, string[]> Sources => sources.ToDictionary(i => i.Key, i => i.Value.Values.Select(si => si.EventName).ToArray());
        public IList<object> Targets => targets.ToArray();
        public IList<ICommandStateUpdater> StateUpdaters => stateUpdaters.ToArray();

        #endregion

        #region Constructors

        internal CommandBinding(ICommand command, IDictionary<string, object?>? initialState, bool disposeCommand)
        {
            if (command == null!)
                Throw.ArgumentNullException(Argument.command);

            this.command = command;
            this.disposeCommand = disposeCommand;
            state = initialState is CommandState s ? s : new CommandState(initialState);
            state.PropertyChanged += State_PropertyChanged;
        }

        #endregion

        #region Methods

        #region Static Methods

        private static StringKeyedDictionary<EventInfo> GetEvents(Type type)
        {
            static void PopulateEvents(StringKeyedDictionary<EventInfo> dict, IEnumerable<EventInfo> events)
            {
                foreach (EventInfo eventInfo in events)
                {
                    // for conflicting names only the first event is added
                    if (!dict.ContainsKey(eventInfo.Name))
                        dict[eventInfo.Name] = eventInfo;
                }
            }

            // public events of all levels
            var result = new StringKeyedDictionary<EventInfo>();
            PopulateEvents(result, type.GetEvents());

            // non-public events by type (because private events cannot be obtained for all levels in one step)
            for (Type? t = type; t != null && t != Reflector.ObjectType; t = t.BaseType)
                PopulateEvents(result, t.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));

            return result;
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            state.PropertyChanged -= State_PropertyChanged;
            executingHandler = null;
            executedHandler = null;
            errorHandler = null;
            getParameterCallback = null;

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
                Throw.ObjectDisposedException();
            if (source == null!)
                Throw.ArgumentNullException(Argument.source);
            if (eventName == null!)
                Throw.ArgumentNullException(Argument.eventName);

            Type sourceType = source as Type ?? source.GetType();
            bool isStatic = ReferenceEquals(source, sourceType);
            if (!eventsCache[sourceType].TryGetValue(eventName, out EventInfo? eventInfo))
                Throw.ArgumentException(Argument.eventName, Res.ComponentModelMissingEvent(eventName, sourceType));
            MethodInfo? addMethod = eventInfo.GetAddMethod(true);
            MethodInfo? invokeMethod = eventInfo.EventHandlerType?.GetMethod(nameof(Action.Invoke));
            ParameterInfo[]? invokerParameters = invokeMethod?.GetParameters();

            if (addMethod == null || invokeMethod?.ReturnType != Reflector.VoidType || invokerParameters?.Length != 2
                || invokerParameters[0].ParameterType != Reflector.ObjectType || !typeof(EventArgs).IsAssignableFrom(invokerParameters[1].ParameterType))
                Throw.ArgumentException(Argument.eventName, Res.ComponentModelInvalidEvent(eventName));

            if (addMethod.IsStatic ^ isStatic)
                Throw.ArgumentException(Argument.commandSource, Res.ComponentModelInvalidCommandSource);

            // already added
            if (sources.TryGetValue(source, out Dictionary<EventInfo, SubscriptionInfo>? subscriptions) && subscriptions.ContainsKey(eventInfo))
                return this;

            // creating generic info by reflection because the signature must match and EventArgs can vary
            var info = (SubscriptionInfo)Reflector.CreateInstance(typeof(SubscriptionInfo<>).GetGenericType(invokerParameters[1].ParameterType));
            info.Source = source;
            info.EventName = eventName;
            info.Binding = this;

            // subscribing the event by info.Execute
            info.Delegate = Delegate.CreateDelegate(eventInfo.EventHandlerType!, info, nameof(SubscriptionInfo<EventArgs>.Execute));
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
                Throw.ObjectDisposedException();
            if (source == null!)
                Throw.ArgumentNullException(Argument.source);
            return DoRemoveSource(source);
        }

        public ICommandBinding AddStateUpdater(ICommandStateUpdater updater, bool updateSources)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (updater == null!)
                Throw.ArgumentNullException(Argument.updater);
            stateUpdaters.Add(updater);
            if (updateSources && sources.Count > 0)
                GetInstanceSources().ForEach(UpdateSource);
            return this;
        }

        public bool RemoveStateUpdater(ICommandStateUpdater updater)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (updater == null!)
                Throw.ArgumentNullException(Argument.updater);
            return stateUpdaters.Remove(updater);
        }

        public ICommandBinding AddTarget(object target)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (target == null!)
                Throw.ArgumentNullException(Argument.target);

            targets.Add(target);
            return this;
        }

        public ICommandBinding AddTarget(Func<object> getTarget)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            return AddTarget((object)getTarget);
        }

        public bool RemoveTarget(object target)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (target == null!)
                Throw.ArgumentNullException(Argument.target);
            return targets.Remove(target);
        }

        public ICommandBinding WithParameter(Func<object?>? callback)
        {
            getParameterCallback = callback;
            return this;
        }

        public void InvokeCommand(object source, string eventName, EventArgs eventArgs, object? parameter)
        {
            if (disposed)
                Throw.ObjectDisposedException();
            if (source == null!)
                Throw.ArgumentNullException(Argument.source);
            if (eventName == null!)
                Throw.ArgumentNullException(Argument.eventName);
            if (eventArgs == null!)
                Throw.ArgumentNullException(Argument.eventArgs);
            InvokeCommand(new CommandSource<EventArgs>
            {
                Source = source,
                TriggeringEvent = eventName,
                EventArgs = eventArgs,
            }, parameter);
        }

        #endregion

        #region Private Methods

        private void UpdateSource(object source)
        {
            if (stateUpdaters.Count == 0)
                return;
            foreach (string propertyName in ((IDictionary<string, object?>)state).Keys)
                UpdateSource(source, propertyName);
        }

        private void UpdateSource(object source, string propertyName)
        {
            if (!state.TryGetValue(propertyName, out object? stateValue))
                return;

            foreach (ICommandStateUpdater updater in stateUpdaters)
            {
                try
                {
                    if (updater.TryUpdateState(source, propertyName, stateValue))
                        return;
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    HandleError(CommandBindingErrorContext.UpdateState, e);
                }
            }
        }

        private void InvokeCommand<TEventArgs>(CommandSource<TEventArgs> source, object? parameter)
            where TEventArgs : EventArgs
        {
            if (disposed)
                return;

            ICommand<TEventArgs>? cmdTypedArgs = command as ICommand<TEventArgs>;
            ExecuteCommandEventArgs? e = null;
            try
            {
                executingHandler?.Invoke(this, e = new ExecuteCommandEventArgs(source, state));
            }
            catch (Exception ex) when (!ex.IsCritical())
            {
                HandleError(CommandBindingErrorContext.ExecutingEvent, ex);
            }

            if (disposed || !state.Enabled)
                return;
            try
            {
                if (targets.IsNullOrEmpty())
                {
                    try
                    {
                        if (cmdTypedArgs != null)
                            cmdTypedArgs.Execute(source, state, null, parameter);
                        else
                            command.Execute(source, state, null, parameter);
                    }
                    catch (Exception ex) when (!ex.IsCritical())
                    {
                        HandleError(CommandBindingErrorContext.CommandExecute, ex);
                    }
                }
                else
                {
                    foreach (object targetEntry in targets)
                    {
                        object target = targetEntry;
                        if (target is Func<object> factory)
                        {
                            try
                            {
                                target = factory.Invoke();
                            }
                            catch (Exception ex) when (!ex.IsCritical())
                            {
                                HandleError(CommandBindingErrorContext.EvaluateTarget, ex);
                            }
                        }

                        try
                        {
                            if (cmdTypedArgs != null)
                                cmdTypedArgs.Execute(source, state, target, parameter);
                            else
                                command.Execute(source, state, target, parameter);
                        }
                        catch (Exception ex) when (!ex.IsCritical())
                        {
                            HandleError(CommandBindingErrorContext.CommandExecute, ex);
                        }

                        if (disposed || !state.Enabled)
                            return;
                    }
                }
            }
            finally
            {
                if (!disposed)
                {
                    try
                    {
                        executedHandler?.Invoke(this, e ?? new ExecuteCommandEventArgs(source, state));
                    }
                    catch (Exception ex) when (!ex.IsCritical())
                    {
                        HandleError(CommandBindingErrorContext.ExecutedEvent, ex);
                    }
                }
            }
        }

        private bool DoRemoveSource(object source)
        {
            if (!sources.TryGetValue(source, out Dictionary<EventInfo, SubscriptionInfo>? subscriptions))
                return false;

            foreach (KeyValuePair<EventInfo, SubscriptionInfo> subscriptionInfo in subscriptions)
                Reflector.InvokeMethod(source is Type ? null : source, subscriptionInfo.Key.GetRemoveMethod(true)!, subscriptionInfo.Value.Delegate);

            return sources.Remove(source);
        }

        private IEnumerable<object> GetInstanceSources() => sources.Keys.Where(k => k is not Type);

        private void HandleError(CommandBindingErrorContext context, Exception error)
        {
            Debug.Assert(!error.IsCritical());
            EventHandler<CommandBindingErrorEventArgs>? handler = errorHandler;

            if (handler != null)
            {
                var args = new CommandBindingErrorEventArgs(context, error);
                handler.Invoke(this, args);
                if (args.Handled)
                    return;
            }

            ExceptionDispatchInfo.Capture(error).Throw();
        }

        #endregion

        #region Event handlers

        private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (stateUpdaters.Count == 0)
                return;
            foreach (object source in GetInstanceSources())
                UpdateSource(source, e.PropertyName!);
        }

        #endregion

        #endregion

        #endregion
    }
}
