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
        /// <returns><see langword="true"/>, if one step is successfully undone; otherwise, <see langword="false"/>.</returns>
        bool TryUndo();

        /// <summary>
        /// Gets or sets the undo capacity.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">When set, <paramref name="value"/> is less than 0.</exception>
        int UndoCapacity { get; set; }

        /// <summary>
        /// Undoes all possible undo steps.
        /// </summary>
        void UndoAll();

        /// <summary>
        /// Clears the undo history without performing any undo.
        /// </summary>
        void ClearHistory();
    }
}
