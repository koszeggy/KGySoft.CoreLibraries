using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with nested committable and revertible editing capability.
    /// </summary>
    /// <seealso cref="EditableObjectBase" />
    public interface ICanEdit
    {
        /// <summary>
        /// Begins a new level of committable/revertible editing session on the object.
        /// </summary>
        void BeginNewEdit();

        /// <summary>
        /// Commits all changes since the last <see cref="BeginNewEdit"/> call.
        /// </summary>
        void CommitLastEdit();

        /// <summary>
        /// Discards all changes since the last <see cref="BeginNewEdit"/> call.
        /// </summary>
        void RevertLastEdit();

        /// <summary>
        /// Commits all changes of all editing levels.
        /// </summary>
        /// <returns><see langword="true"/>&#160;if <see cref="EditLevel"/> was greater than 0 before the call; otherwise, <see langword="false"/>.</returns>
        bool TryCommitAllEdits();

        /// <summary>
        /// Reverts all changes of all editing levels.
        /// </summary>
        /// <returns><see langword="true"/>&#160;if <see cref="EditLevel"/> was greater than 0 before the call; otherwise, <see langword="false"/>.</returns>
        bool TryRevertAllEdits();

        /// <summary>
        /// Gets the editing level. That is, the number of <see cref="BeginNewEdit">BeginNewEdit</see> calls without a corresponding <see cref="CommitLastEdit">CommitLastEdit</see> or <see cref="RevertLastEdit">RevertLastEdit</see> call.
        /// </summary>
        int EditLevel { get; }
    }
}
