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
        /// Gets whether the specified property exists in this <see cref="IPersistableObject"/>
        /// </summary>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <returns><see langword="true"/> if the specified property exists in the underlying store; otherwise, <see langword="false"/>.</returns>
        bool PropertyExists(string propertyName);

        /// <summary>
        /// Gets the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <returns>The value of the property.</returns>
        /// <exception cref="InvalidOperationException">Cannot get property, property does not exist or type of <typeparamref name="T"/> is invalid.</exception>
        object GetProperty(string propertyName);

        /// <summary>
        /// Gets the specified property if it exists and has the correct value; otherwise, returns <paramref name="defaultValue"/>.
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
        /// Gets a copy of the stored properties.
        /// </summary>
        /// <exception cref="InvalidOperationException">A property cannot be get.</exception>
        IDictionary<string, object> GetProperties();

        /// <summary>
        /// Sets the properties provided in the specified dictionary.
        /// </summary>
        /// <param name="properties">The properties to set.</param>
        /// <param name="merge">If set to <see langword="true"/>, then merges the provided values with already existing ones.
        /// If set to <see langword="false"/>, then clears the originally stored properties.</param>
        /// <exception cref="InvalidOperationException">A property cannot be set.</exception>
        void SetProperties(IDictionary<string, object> properties, bool merge = false);

        #endregion
    }
}
