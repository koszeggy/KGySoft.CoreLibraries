using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Extension methods for the <see cref="PropertyChangedEventArgs"/> type.
    /// </summary>
    public static class PropertyChangedEventArgsExtensions
    {
        /// <summary>
        /// If the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance, then gets the property value before the change.
        /// </summary>
        /// <param name="args">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <param name="oldValue">If the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance, then the property value before the change; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance; otherwise, <see langword="null"/>.</returns>
        public static bool TryGetOldPropertyValue(this PropertyChangedEventArgs args, out object oldValue)
        {
            if (args is PropertyChangedExtendedEventArgs ext)
            {
                oldValue = ext.OldValue;
                return !ObservableObjectBase.MissingProperty.Equals(oldValue);
            }

            oldValue = null;
            return false;
        }

        /// <summary>
        /// If the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance, then gets the property value after the change.
        /// </summary>
        /// <param name="args">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <param name="newValue">If the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance, then the property value after the change; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance; otherwise, <see langword="null"/>.</returns>
        public static bool TryGetNewPropertyValue(this PropertyChangedEventArgs args, out object newValue)
        {
            if (args is PropertyChangedExtendedEventArgs ext)
            {
                newValue = ext.NewValue;
                return !ObservableObjectBase.MissingProperty.Equals(newValue);
            }

            newValue = null;
            return false;
        }
    }
}
