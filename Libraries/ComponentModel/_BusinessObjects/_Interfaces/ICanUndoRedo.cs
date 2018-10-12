namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with undo and redo capability.
    /// </summary>
    /// <seealso cref="ICanUndo" />
    public interface ICanUndoRedo : ICanUndo
    {
        /// <summary>
        /// Gets whether there are changes to redo.
        /// </summary>
        /// <value><see langword="true"/>, if there are changes to redo; otherwise, <see langword="false"/>.
        /// </value>
        bool CanRedo { get; }

        /// <summary>
        /// Tries to perform one redo step.
        /// </summary>
        /// <returns><see langword="true"/>, if one step is successfully redone; otherwise, <see langword="false"/>.
        /// The result can be <see langword="false"/> if <see cref="CanRedo"/> was <see langword="false"/> or when the stored steps are inconsistent with the current property values.</returns>
        bool TryRedo();

        /// <summary>
        /// Redoes all possible redo steps.
        /// </summary>
        void RedoAll();
    }
}