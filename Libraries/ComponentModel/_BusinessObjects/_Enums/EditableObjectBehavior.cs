#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EditableObjectBehavior.cs
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

#region Usings

using System;
using System.ComponentModel;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents the behavior of an <see cref="ICanEdit"/> implementation when it is treated as an <see cref="IEditableObject"/>.
    /// </summary>
    public enum EditableObjectBehavior
    {
        /// <summary>
        /// <see cref="IEditableObject"/> methods are ignored, the object must be used as an <see cref="ICanEdit"/> implementation to utilize editing features.
        /// </summary>
        Disabled,

        /// <summary>
        /// <see cref="IEditableObject.EndEdit">IEditableObject.EndEdit</see> and <see cref="IEditableObject.CancelEdit">IEditableObject.CancelEdit</see> calls ignore possible multiple
        /// <see cref="IEditableObject.BeginEdit">IEditableObject.BeginEdit</see> calls and commit/revert all of the previous changes.
        /// <see cref="ICanEdit.EditLevel"/> will be 0 after an <see cref="IEditableObject.EndEdit">IEditableObject.EndEdit</see> or <see cref="IEditableObject.CancelEdit">IEditableObject.CancelEdit</see> call.
        /// </summary>
        DisableNesting,

        /// <summary>
        /// Number of <see cref="IEditableObject.EndEdit">IEditableObject.EndEdit</see> and <see cref="IEditableObject.CancelEdit">IEditableObject.CancelEdit</see> calls must equal to previous <see cref="IEditableObject.BeginEdit">IEditableObject.BeginEdit</see> calls;
        /// otherwise, an <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        AllowNesting

    }
}
