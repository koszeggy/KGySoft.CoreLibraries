namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents the context of the <see cref="ICommandBinding.Error">ICommandBinding.Error</see> event.
    /// </summary>
    public enum CommandBindingErrorContext
    {
        /// <summary>
        /// Indicates that the error occurred while invoking the <see cref="ICommand.Execute">ICommand.Execute</see> method.
        /// </summary>
        CommandExecute,

        /// <summary>
        /// Indicates that the error occurred while invoking the <see cref="ICommandBinding.Executing">ICommandBinding.Executing</see> event.
        /// </summary>
        ExecutingEvent,

        /// <summary>
        /// Indicates that the error occurred while invoking the <see cref="ICommandBinding.Executed">ICommandBinding.Executed</see> event.
        /// </summary>
        ExecutedEvent,

        /// <summary>
        /// Indicates that the error occurred while invoking the delegate specified in
        /// the <see cref="ICommandBinding.WithParameter">ICommandBinding.WithParameter</see> method.
        /// </summary>
        EvaluateParameter,

        /// <summary>
        /// Indicates that the error occurred while invoking the delegate specified in
        /// the <see cref="ICommandBinding.AddTarget(System.Func{object})">ICommandBinding.AddTarget</see> method.
        /// </summary>
        EvaluateTarget,

        /// <summary>
        /// Indicates that the error occurred while updating the command source by
        /// the <see cref="ICommandStateUpdater.TryUpdateState">ICommandStateUpdater.TryUpdateState</see> method.
        /// </summary>
        UpdateState
    }
}
