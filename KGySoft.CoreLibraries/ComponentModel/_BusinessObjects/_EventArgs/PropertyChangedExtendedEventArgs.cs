#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PropertyChangedExtendedEventArgs.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.ComponentModel;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a <see cref="PropertyChangedEventArgs"/> with property value. The actual type of the event argument of the <see cref="ObservableObjectBase.PropertyChanged">ObservableObjectBase.PropertyChanged</see> event is
    /// <see cref="PropertyChangedExtendedEventArgs"/>.
    /// </summary>
    /// <seealso cref="PropertyChangedEventArgs" />
    /// <remarks><note>The <see cref="ObservableObjectBase.PropertyChanged">ObservableObjectBase.PropertyChanged</see> event uses the <see cref="PropertyChangedEventHandler"/> delegate in order to consumers, which rely on the conventional property
    /// changed notifications can use it in a compatible way. To get the old value in an event handler you can cast the argument to <see cref="PropertyChangedExtendedEventArgs"/>
    /// or call the <see cref="PropertyChangedEventArgsExtensions.TryGetOldPropertyValue">TryGetOldPropertyValue</see> extension method on it.</note></remarks>
    public class PropertyChangedExtendedEventArgs : PropertyChangedEventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the property value before the change or <see cref="ObservableObjectBase.MissingProperty"/> if no previous value was stored for the property before the change.
        /// </summary>
        public object? OldValue { get; }

        /// <summary>
        /// Gets the property value after the change or <see cref="ObservableObjectBase.MissingProperty"/> if the property has just been reset and there is no stored value for it.
        /// </summary>
        public object? NewValue { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedExtendedEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The property value before the change.</param>
        /// <param name="newValue">The property value after the change.</param>
        /// <param name="propertyName">Name of the property.</param>
        public PropertyChangedExtendedEventArgs(object? oldValue, object? newValue, string propertyName) : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        #endregion
    }
}
