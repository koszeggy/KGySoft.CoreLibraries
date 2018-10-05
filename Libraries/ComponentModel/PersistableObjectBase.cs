#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PersistableObjectBase.cs
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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using KGySoft.Collections;
using KGySoft.Libraries;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a base class for objects that can store their own properties in a common internal storage and are able to notify their consumers about property changes.
    /// <br/>For details see the <strong>Remarks</strong> section.
    /// </summary>
    /// <remarks>
    /// <para>Implementers can use the <see cref="Get{T}(T,string)">Get</see> and <see cref="Set">Set</see> methods in the property accessors to handle administration of
    /// getting and setting in a unified way.</para>
    /// <para>Consumers can subscribe the <see cref="ObservableObjectBase.PropertyChanged"/> event to get notification about the property changes.</para>
    /// <para>Accessibility of properties can be fine tuned by overriding the <see cref="CanGetProperty">CanGetProperty</see> and <see cref="CanSetProperty">CanSetProperty</see> methods.</para>
    /// <para>If cast to <see cref="IPersistableObject"/> the actually stored values can be read and restored by the <see cref="IPersistableObject.GetProperties">GetProperties</see> and
    /// <see cref="IPersistableObject.SetProperties">SetProperties</see> methods.</para>
    /// <example>
    /// The following example shows a possible implementation of a derived class.
    /// <code lang="C#"><![CDATA[
    /// public class BusinessClassExample : PersistableObjectBasepublic class BusinessClassExample : PersistableObjectBase
    /// {{
    ///     // A simple integer property (with zero default value):    public int IntProperty { get => Get<int>() }
    ///     public int IntProperty { get => Get<int>(); set => Set(value); }
    ///
    ///     // An int property with default value. Until the property is set, the default will be returned.
    ///     public int IntPropertyCustomDefault { get => Get(-1); set => Set(value); }
    ///
    ///     // If the properties above are only read they do not store anything in the underlying storage.
    ///     // If the default value is a complex one, which should not be evaluated each time you can provide a factory for it:
    ///     // When this property is read for the first time without setting it before, the provided delegate will be invoked
    ///     // and the returned default value is stored without triggering the PropertyChanged event.
    ///     public MyComplexType ComplexProperty { get => Get(() => new MyComplexType()); set => Set(value); }
    /// }}
    /// ]]></code>
    /// </example>
    /// </remarks>
    /// <seealso cref="ObservableObjectBase" />
    /// <seealso cref="IPersistableObject" />
    public abstract class PersistableObjectBase : ObservableObjectBase, IPersistableObject
    {
        #region Fields

        #region Static Fields

        private static readonly Cache<Type, Dictionary<string, PropertyInfo>> properties = new Cache<Type, Dictionary<string, PropertyInfo>>(GetProperties);

        #endregion

        #region Instance Fields

        private Dictionary<string, object> propertyValues = new Dictionary<string, object>();

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        private static Dictionary<string, PropertyInfo> GetProperties(Type type) => new Dictionary<string, PropertyInfo>(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToDictionary(pi => pi.Name, pi => pi));

        #endregion

        #region Instance Methods

        #region Protected Methods

        /// <summary>
        /// Gets the value of a property, or if it was not set before, then creates its initial value.
        /// The created initial value will be stored in the internal property storage without triggering the <see cref="ObservableObjectBase.PropertyChanged"/> event.
        /// For constant or simple expressions, or to return a default value for a non-existing property without storing it internally use the other <see cref="Get{T}(T,string)">Get</see> overload.
        /// <br/>For an example, see the <strong>Remarks</strong> section of the <see cref="PersistableObjectBase"/> class.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="createInitialValue">A delegate, which creates the initial value if the property does not exist. If <see langword="null"/>,
        /// then an exception is thrown for an uninitialized property.</param>
        /// <param name="propertyName">The name of the property to get. This parameter is optional.
        /// <br/>Default value: The name of the caller member.</param>
        /// <returns>The value of the property, or the created initial value returned by the <paramref name="createInitialValue"/> parameter.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> cannot be get.
        /// <br/>-or-
        /// <br/>The stored value of the property is not compatible with <typeparamref name="T"/>.
        /// <br/>-or-
        /// <br/><paramref name="propertyName"/> value does not exist and <paramref name="createInitialValue"/> is <see langword="null"/>.
        /// <br/>-or-
        /// <br/>The created default value of the property cannot be set.
        /// <br/>-or-
        /// <br/><see cref="CanGetProperty">CanGetProperty</see> is not overridden and <paramref name="propertyName"/> is not an actual instance property in this instance.
        /// </exception>
        protected T Get<T>(Func<T> createInitialValue, [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.Get(Res.ArgumentNull));
            if (!CanGetProperty(propertyName))
                throw new InvalidOperationException(Res.Get(Res.CannotGetProperty, propertyName));

            if (propertyValues.TryGetValue(propertyName, out object value))
            {
                if (!(value is T result))
                    throw new InvalidOperationException(Res.Get(Res.ReturnedTypeInvalid, typeof(T)));
                return result;
            }

            if (createInitialValue == null)
                throw new InvalidOperationException(Res.Get(Res.PropertyValueNotExist, propertyName));
            else
            {
                T result = createInitialValue.Invoke();
                ((IPersistableObject)this).SetProperty(propertyName, result, false);
                return result;
            }
        }

        /// <summary>
        /// Gets the value of a property or <paramref name="defaultValue"/> if no value is stored for it. No new value will be stored
        /// if the property does not exist. If the default initial value is too complex and should not be evaluated every time when the property is get,
        /// or to throw an exception for an uninitialized property use the other <see cref="Get{T}(Func{T},string)">Get</see> overload.
        /// <br/>For an example, see the <strong>Remarks</strong> section of the <see cref="PersistableObjectBase"/> class.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="defaultValue">The value to return if property does not exist. This parameter is optional.
        /// <br/>Default value: The default value of <typeparamref name="T"/> type.</param>
        /// <param name="propertyName">The name of the property to get. This parameter is optional.
        /// <br/>Default value: The name of the caller member.</param>
        /// <returns>The value of the property, or the specified <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> cannot be get.
        /// <br/>-or-
        /// <br/><see cref="CanGetProperty">CanGetProperty</see> is not overridden and <paramref name="propertyName"/> is not an actual instance property in this instance.
        /// </exception>
        protected T Get<T>(T defaultValue = default, [CallerMemberName] string propertyName = null)
            => ((IPersistableObject)this).GetPropertyOrDefault(propertyName, defaultValue);

        /// <summary>
        /// Sets the value of a property.
        /// <br/>For an example, see the <strong>Remarks</strong> section of the <see cref="PersistableObjectBase"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="invokeChangedEvent">If <see langword="true"/>, and the <paramref name="value"/> is different to the previously stored value, then invokes the <see cref="ObservableObjectBase.PropertyChanged"/> event.</param>
        /// <param name="propertyName">Name of the property to set. This parameter is optional.
        /// <br/>Default value: The name of the caller member.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> cannot be set.
        /// <br/>-or-
        /// <br/><see cref="CanSetProperty">CanSetProperty</see> is not overridden and <paramref name="propertyName"/> is not an actual instance property in this instance, or <paramref name="value"/> is not compatible with the property type.
        /// </exception>
        protected void Set(object value, bool invokeChangedEvent = true, [CallerMemberName]string propertyName = null)
            => ((IPersistableObject)this).SetProperty(propertyName, value, invokeChangedEvent);

        /// <summary>
        /// Gets whether the specified property can be get. The base implementation allows to get the actual instance properties in this instance.
        /// </summary>
        /// <param name="propertyName">Name of the property to get.</param>
        /// <returns><see langword="true"/> if the specified property can be get; otherwise, <see langword="false"/>.</returns>
        protected virtual bool CanGetProperty(string propertyName)
        {
            Dictionary<string, PropertyInfo> props;
            lock (properties)
                props = properties[GetType()];
            return props.ContainsKey(propertyName);
        }

        /// <summary>
        /// Gets whether the specified property can be set. The base implementation allows to set the actual instance properties in this instance if the specified <paramref name="value"/> is compatible with the property type.
        /// </summary>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns><see langword="true"/> if the specified property can be set; otherwise, <see langword="false"/>.</returns>
        protected virtual bool CanSetProperty(string propertyName, object value)
        {
            Dictionary<string, PropertyInfo> props;
            lock (properties)
                props = properties[GetType()];
            return props.TryGetValue(propertyName, out PropertyInfo pi) && pi.PropertyType.CanAcceptValue(value);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        bool IPersistableObject.PropertyExists(string propertyName) => propertyValues.ContainsKey(propertyName);

        object IPersistableObject.GetProperty(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (!CanGetProperty(propertyName))
                throw new InvalidOperationException($"Cannot get property '{propertyName}'");
            if (propertyValues.TryGetValue(propertyName, out object value))
                return value;
            throw new InvalidOperationException($"Property '{propertyName}' does not exist");
        }

        T IPersistableObject.GetPropertyOrDefault<T>(string propertyName, T defaultValue)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (!CanGetProperty(propertyName))
                throw new InvalidOperationException($"Cannot get property '{propertyName}'");
            return propertyValues.TryGetValue(propertyName, out object value) && value is T result ? result : defaultValue;
        }

        bool IPersistableObject.SetProperty(string propertyName, object value, bool invokeChangedEvent)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (!CanSetProperty(propertyName, value))
                throw new InvalidOperationException($"Cannot set property '{propertyName}'");
            if (propertyValues.TryGetValue(propertyName, out object oldValue) && Equals(value, oldValue))
                return false;
            propertyValues[propertyName] = value;
            if (invokeChangedEvent)
                OnPropertyChanged(propertyName);
            return true;
        }

        IDictionary<string, object> IPersistableObject.GetProperties()
            => propertyValues.ToDictionary(p => p.Key, p => CanGetProperty(p.Key) ? p.Value : throw new InvalidOperationException($"Cannot get property '{p.Key}'"));

        void IPersistableObject.SetProperties(IDictionary<string, object> newProperties, bool merge)
        {
            if (!merge)
                propertyValues = new Dictionary<string, object>();
            foreach (var property in newProperties)
                ((IPersistableObject)this).SetProperty(property.Key, property.Value);
        }

        #endregion

        #endregion

        #endregion
    }
}
