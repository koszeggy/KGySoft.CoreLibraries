using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a <see cref="PropertyChangingEventArgs"/> with property value. The actual type of the event argument of the <see cref="ObservableObjectBase.PropertyChanging">ObservableObjectBase.PropertyChanging</see> event is
    /// <see cref="PropertyChangingExtendedEventArgs"/>.
    /// </summary>
    /// <seealso cref="PropertyChangingEventArgs" />
    /// <remarks><note>The <see cref="ObservableObjectBase.PropertyChanging">ObservableObjectBase.PropertyChanging</see> event uses the <see cref="PropertyChangingEventHandler"/> delegate in order to consumers, which rely on the conventional property
    /// changing notifications can use it in a compatible way. To get the old value in an event handler you can cast the argument to <see cref="PropertyChangingExtendedEventArgs"/>
    /// or call the <see cref="EventArgsExtensions.TryGetPropertyValue">TryGetPropertyValue</see> extension method on it.</note></remarks>
    public class PropertyChangingExtendedEventArgs : PropertyChangingEventArgs
    {
        /// <summary>
        /// Gets the property value before the change.
        /// </summary>
        public object PropertyValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangingExtendedEventArgs"/> class.
        /// </summary>
        /// <param name="propertyValue">The property value before the change.</param>
        /// <param name="propertyName">Name of the property.</param>
        public PropertyChangingExtendedEventArgs(object propertyValue, string propertyName) : base(propertyName) => PropertyValue = propertyValue;
    }
}
