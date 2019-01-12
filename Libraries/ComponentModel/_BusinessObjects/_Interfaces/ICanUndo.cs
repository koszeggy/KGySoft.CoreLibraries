using System;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with undo capability.
    /// </summary>
    public interface ICanUndo
    {
        /// <summary>
        /// Gets whether there are changes to undo.
        /// </summary>
        /// <value><see langword="true"/>, if there are changes to undo; otherwise, <see langword="false"/>.
        /// </value>
        bool CanUndo { get; }

        /// <summary>
        /// Tries to perform one undo step.
        /// </summary>
        /// <returns><see langword="true"/>, if one step is successfully undone; otherwise, <see langword="false"/>.
        /// The result can be <see langword="false"/>&#160;if <see cref="CanUndo"/> was <see langword="false"/>&#160;or when the stored steps are inconsistent with the current property values.</returns>
        bool TryUndo();

        /// <summary>
        /// Undoes all possible undo steps.
        /// </summary>
        void UndoAll();

        /// <summary>
        /// Clears the undo history without performing any undo.
        /// </summary>
        void ClearUndoHistory();
    }
}
