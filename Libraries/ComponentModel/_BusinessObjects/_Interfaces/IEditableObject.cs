using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object with commitable and revertable editing capability.
    /// </summary>
    /// <seealso cref="IEditableObject" />
    /// <seealso cref="EditableObjectBase" />
    public interface ICanEdit : IEditableObject
    {
        /// <summary>
        /// Gets the editing level. That is, the number of <see cref="IEditableObject.BeginEdit">BeginEdit</see> calls without a corresponding <see cref="IEditableObject.EndEdit">EndEdit</see> call.
        /// </summary>
        int EditLevel { get; }
    }
}
