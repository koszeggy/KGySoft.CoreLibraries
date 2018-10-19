#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IPersistableObject.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.Collections.Generic;
using System.ComponentModel;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an object that can store its own properties and is able to notify its consumer about property changes.
    /// </summary>
    /// <seealso cref="INotifyPropertyChanged" />
    public interface IPersistableObject : INotifyPropertyChanged
    {
        #region Methods

        /// <summary>
        /// Tries to get the specified property from the inner storage.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="value">Returns the value of the property if it could be found in the inner storage. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the property exists in the inner storage; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Cannot get the property.</exception>
        bool TryGetPropertyValue(string propertyName, out object value);

        /// <summary>
        /// Gets the specified property if it exists in the inner storage and has the correct value; otherwise, returns <paramref name="defaultValue"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="defaultValue">The default value to return if property does not exist or has an incompatible type with <typeparamref name="T"/>.</param>
        /// <exception cref="InvalidOperationException">Cannot get the property.</exception>
        T GetPropertyOrDefault<T>(string propertyName, T defaultValue = default);

        /// <summary>
        /// Sets the property with specified <paramref name="value"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="triggerChangedEvent"><see langword="true"/> to allow raising the <see cref="INotifyPropertyChanged.PropertyChanged"/> event; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if property has been set (change occurred); otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Cannot set the property.</exception>
        bool SetProperty(string propertyName, object value, bool triggerChangedEvent = true);

        /// <summary>
        /// Resets the property with specified property, meaning, it will be removed from the underlying storage so the property getters will return the default value again and <see cref="TryGetPropertyValue">TryGetPropertyValue</see> will return <see langword="false"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to reset.</param>
        /// <param name="triggerChangedEvent"><see langword="true"/> to allow raising the <see cref="INotifyPropertyChanged.PropertyChanged"/> event; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if property has been reset (it existed previously); otherwise, <see langword="false"/>.</returns>
        bool ResetProperty(string propertyName, bool triggerChangedEvent = true);

        /// <summary>
        /// Gets a copy of the stored properties.
        /// </summary>
        /// <returns>A copy of the stored properties.</returns>
        /// <exception cref="InvalidOperationException">A property cannot be get.</exception>
        IDictionary<string, object> GetProperties();

        /// <summary>
        /// Sets the properties provided in the specified dictionary.
        /// </summary>
        /// <param name="properties">The properties to set.</param>
        /// <exception cref="InvalidOperationException">A property cannot be set.</exception>
        void SetProperties(IDictionary<string, object> properties);

        /// <summary>
        /// Tries to the replace a property value. The replacement will succeed if the currently stored value equals to <paramref name="originalValue"/>.
        /// Non-existing value can be represented by <see cref="ObservableObjectBase.MissingProperty"/> so the method supports also "try remove" and "try add" functionality.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="originalValue">The original value.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="triggerChangedEvent"><see langword="true"/> to allow raising the <see cref="INotifyPropertyChanged.PropertyChanged"/> event; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the originally stored value equals <paramref name="originalValue"/> and the replacement was successful; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Cannot set the property.</exception>
        bool TryReplaceProperty(string propertyName, object originalValue, object newValue, bool triggerChangedEvent = true);

        #endregion
    }
}
