#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICanUndoRedo.cs
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
    /// Represents an object with undo and redo capability.
    /// </summary>
    /// <seealso cref="ICanUndo" />
    public interface ICanUndoRedo : ICanUndo
    {
        #region Properties

        /// <summary>
        /// Gets whether there are changes to redo.
        /// </summary>
        /// <value><see langword="true"/>, if there are changes to redo; otherwise, <see langword="false"/>.
        /// </value>
        bool CanRedo { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Tries to perform one redo step.
        /// </summary>
        /// <returns><see langword="true"/>, if one step is successfully redone; otherwise, <see langword="false"/>.
        /// The result can be <see langword="false"/>&#160;if <see cref="CanRedo"/> was <see langword="false"/>&#160;or when the stored steps are inconsistent with the current property values.</returns>
        bool TryRedo();

        /// <summary>
        /// Redoes all possible redo steps.
        /// </summary>
        void RedoAll();

        #endregion
    }
}
