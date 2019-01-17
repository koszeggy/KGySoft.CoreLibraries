#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICanEdit.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with nested committable and revertible editing capability.
    /// </summary>
    /// <seealso cref="EditableObjectBase" />
    public interface ICanEdit
    {
        #region Properties

        /// <summary>
        /// Gets the editing level. That is, the number of <see cref="BeginNewEdit">BeginNewEdit</see> calls without a corresponding <see cref="CommitLastEdit">CommitLastEdit</see> or <see cref="RevertLastEdit">RevertLastEdit</see> calls.
        /// </summary>
        int EditLevel { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Begins a new level of committable/revertible editing session on the object.
        /// </summary>
        void BeginNewEdit();

        /// <summary>
        /// Commits all changes since the last <see cref="BeginNewEdit">BeginNewEdit</see> call.
        /// </summary>
        void CommitLastEdit();

        /// <summary>
        /// Discards all changes since the last <see cref="BeginNewEdit">BeginNewEdit</see> call.
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

        #endregion
    }
}
