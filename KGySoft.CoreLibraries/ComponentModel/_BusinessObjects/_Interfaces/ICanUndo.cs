#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICanUndo.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with undo capability.
    /// </summary>
    public interface ICanUndo
    {
        #region Properties

        /// <summary>
        /// Gets whether there are changes to undo.
        /// </summary>
        /// <value><see langword="true"/>, if there are changes to undo; otherwise, <see langword="false"/>.
        /// </value>
        bool CanUndo { get; }

        #endregion

        #region Methods

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

        #endregion
    }
}
