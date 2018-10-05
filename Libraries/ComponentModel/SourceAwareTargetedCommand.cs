using System;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a command, which is aware of its triggering source and targets
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <seealso cref="ICommand" />
    public class SourceAwareTargetedCommand<TEventArgs, TTarget> : ICommand, IDisposable
        where TEventArgs : EventArgs
    {
        private Action<object, string, TEventArgs, TTarget> action;

        public SourceAwareTargetedCommand(Action<object, string, TEventArgs, TTarget> action)
            => this.action = action ?? throw new ArgumentNullException(nameof(action));

        public void Execute(object source, string eventName, TEventArgs e, TTarget target)
            => action.Invoke(source, eventName, e, target);

        void ICommand.Execute(object source, string eventName, EventArgs e, object target)
            => Execute(source, eventName, (TEventArgs)e, (TTarget)target);

        /// <summary>
        /// Releases the delegate passed to the constructor. Should be called if the command was initialized by an instance method.
        /// </summary>
        public void Dispose() => action = null;
    }
}
