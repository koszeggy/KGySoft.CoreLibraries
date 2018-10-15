using System;
using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents the behavior of an <see cref="ICanEdit"/> implementation when it is treated as an <see cref="IEditableObject"/>.
    /// </summary>
    public enum EditableObjectBehavior
    {
        /// <summary>
        /// <see cref="IEditableObject.EndEdit">EndEdit</see> and <see cref="IEditableObject.CancelEdit">CancelEdit</see> calls ignore possible multiple
        /// <see cref="IEditableObject.BeginEdit">BeginEdit</see> calls and commit/revert all of the previous changes. Tolerates also no <see cref="IEditableObject.BeginEdit">BeginEdit</see>
        /// call at all before committing/canceling. <see cref="ICanEdit.EditLevel"/> will be 0 after a <see cref="IEditableObject.EndEdit">EndEdit</see> or <see cref="IEditableObject.CancelEdit">CancelEdit</see> call.
        /// </summary>
        NestingDisabled,

        /// <summary>
        /// Number of <see cref="IEditableObject.EndEdit">EndEdit</see> and <see cref="IEditableObject.CancelEdit">CancelEdit</see> calls must equal to previous <see cref="IEditableObject.BeginEdit">BeginEdit</see> calls;
        /// otherwise a <see cref="InvalidOperationException"/> will be called.
        /// </summary>
        NestingAllowed,

        /// <summary>
        /// <see cref="IEditableObject"/> methods are ignored, the object must be used as an <see cref="ICanEdit"/> implementation to utilize editing features.
        /// </summary>
        Disabled
    }
}
