using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a <see cref="PropertyChangedEventArgs"/> with property value. The actual type of the event argument of the <see cref="ObservableObjectBase.PropertyChanged">ObservableObjectBase.PropertyChanged</see> event is
    /// <see cref="PropertyChangedExtendedEventArgs"/>.
    /// </summary>
    /// <seealso cref="PropertyChangedEventArgs" />
    /// <remarks><note>The <see cref="ObservableObjectBase.PropertyChanged">ObservableObjectBase.PropertyChanged</see> event uses the <see cref="PropertyChangedEventHandler"/> delegate in order to consumers, which rely on the conventional property
    /// changed notifications can use it in a compatible way. To get the old value in an event handler you can cast the argument to <see cref="PropertyChangedExtendedEventArgs"/>
    /// or call the <see cref="PropertyChangedEventArgsExtensions.TryGetPropertyValue">TryGetPropertyValue</see> extension method on it.</note></remarks>
    public class PropertyChangedExtendedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>
        /// Gets the property value before the change.
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// Gets the property value after the change.
        /// Can be <see langword="null"/> if the property has been removed and has no value.
        /// </summary>
        public object NewValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedExtendedEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The property value before the change.</param>
        /// <param name="newValue">The property value after the change.</param>
        /// <param name="propertyName">Name of the property.</param>
        public PropertyChangedExtendedEventArgs(object oldValue, object newValue, string propertyName) : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
