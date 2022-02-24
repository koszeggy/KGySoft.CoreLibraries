#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObservableObjectBase.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a base class for component model classes, which can notify their consumer about property changes.
    /// <br/>See the <strong>Remarks</strong> section for details and examples.
    /// </summary>
    /// <remarks>
    /// <para>Implementers can use the <see cref="Get{T}(T,string)">Get</see> and <see cref="Set">Set</see> methods in the property accessors to manage event raising automatically.</para>
    /// <para>Consumers can subscribe the <see cref="PropertyChanged"/> event to get notification about the property changes.</para>
    /// <para>Accessing properties can be fine tuned by overriding the <see cref="CanGetProperty">CanGetProperty</see> and <see cref="CanSetProperty">CanSetProperty</see> methods. By default they allow
    /// accessing the instance properties in the implementer class.
    /// <note type="inherit">Do not use <see cref="CanGetProperty">CanGetProperty</see> and <see cref="CanSetProperty">CanSetProperty</see> methods for property validation.
    /// To be able to validate property values consider to use the <see cref="ValidatingObjectBase"/> or <see cref="ModelBase"/> classes.</note>
    /// </para>
    /// <example>
    /// The following example shows a possible implementation of a derived class.
    /// <code lang="C#"><![CDATA[
    /// public class MyModel : ObservableObjectBase
    /// {
    ///     // A simple integer property (with zero default value). Until the property is set no value is stored internally.
    ///     public int IntProperty { get => Get<int>(); set => Set(value); }
    ///
    ///     // An int property with a specified default value. Until the property is set the default will be returned.
    ///     public int IntPropertyCustomDefault { get => Get(-1); set => Set(value); }
    ///
    ///     // If the default value is a complex one, which should not be evaluated each time you can provide a factory for it:
    ///     // When this property is read for the first time without setting it before, the provided delegate will be invoked
    ///     // and the returned default value is stored without triggering the PropertyChanged event.
    ///     public MyComplexType ComplexProperty { get => Get(() => new MyComplexType()); set => Set(value); }
    /// 
    ///     // You can use regular properties to prevent raising the events and not to store the value in the internal storage.
    ///     // The OnPropertyChanged method still can be called explicitly to raise the PropertyChanged event.
    ///     public int UntrackedProperty { get; set; }
    /// }
    /// ]]></code>
    /// </example>
    /// </remarks>
    /// <threadsafety instance="true" static="true"/>
    /// <seealso cref="INotifyPropertyChanged" />
    /// <seealso cref="PersistableObjectBase" />
    /// <seealso cref="UndoableObjectBase" />
    /// <seealso cref="EditableObjectBase" />
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ModelBase" />
    [Serializable]
    public abstract class ObservableObjectBase : INotifyPropertyChanged, IDisposable, ICloneable
    {
        #region MissingPropertyReference class

        [Serializable]
        private sealed class MissingPropertyReference : IObjectReference
        {
            #region Properties

            internal static MissingPropertyReference Value { get; } = new MissingPropertyReference();

            #endregion

            #region Methods

            [SecurityCritical] public object GetRealObject(StreamingContext context) => Value;
            public override string ToString() => Res.ComponentModelMissingPropertyReference;
            public override bool Equals(object? obj) => obj is MissingPropertyReference;
            public override int GetHashCode() => 0;

            #endregion
        }

        #endregion

        #region Fields

        #region Static Fields

        private static readonly IThreadSafeCacheAccessor<Type, StringKeyedDictionary<Type>> reflectedPropertiesCache =
            ThreadSafeCacheFactory.Create<Type, StringKeyedDictionary<Type>>(GetReflectedProperties, LockFreeCacheOptions.Profile128);

        private static Func<object, object?> customClone =
            o => o is string || o is Delegate ? o
                : o is ICloneable cloneable ? cloneable.Clone()
                : null;

        #endregion

        #region Instance Fields

        private ThreadSafeDictionary<string, object?>? properties;

        [NonSerialized]private StringKeyedDictionary<Type>? reflectedProperties;
        [NonSerialized]private int suspendCounter;
        [NonSerialized]private PropertyChangedEventHandler? propertyChanged;

        private volatile bool isModified;
        private volatile bool isDisposed;

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changed. The actual type of the event argument is <see cref="PropertyChangedExtendedEventArgs"/>.
        /// </summary>
        /// <remarks>
        /// <note>The <see cref="PropertyChanged"/> event uses the <see cref="PropertyChangedEventHandler"/> delegate in order to consumers, which rely on the conventional property
        /// changed notifications can use it in a compatible way. To get the old and new values in an event handler you can cast the argument to <see cref="PropertyChangedExtendedEventArgs"/>
        /// or call the <see cref="PropertyChangedEventArgsExtensions.TryGetOldPropertyValue">TryGetOldPropertyValue</see> and <see cref="PropertyChangedEventArgsExtensions.TryGetNewPropertyValue">TryGetNewPropertyValue</see> extension methods on it.</note>
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => value.AddSafe(ref propertyChanged);
            remove => value.RemoveSafe(ref propertyChanged);
        }

        #endregion

        #region Properties

        #region Static Properties

        /// <summary>
        /// Represents the value of a missing property value. Can be returned in <see cref="PropertyChangedExtendedEventArgs"/> by the <see cref="PropertyChanged"/> event
        /// if the stored value of the property has just been created and had no previous value, or when a property has been removed from the inner storage.
        /// </summary>
        /// <remarks><note>Reading the property when it has no value may return a default value or can cause to recreate a value.</note></remarks>
        public static object MissingProperty { get; } = MissingPropertyReference.Value;

        #endregion

        #region Instance Properties

        #region Public Properties

        /// <summary>
        /// Gets whether this instance has been modified.
        /// Modified state can be set by the <see cref="SetModified">SetModified</see> method.
        /// </summary>
        public bool IsModified => isModified;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets whether this instance has already been disposed.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <para>Properties accessed by the <see cref="Get{T}(T, string)"><![CDATA[Get<T>]]></see> and <see cref="Set">Set</see> methods
        /// throw an <see cref="ObjectDisposedException"/> when this property returns <see langword="true"/>.</para>
        /// <para>If the <see cref="Dispose(bool)"/> method is overridden and you need to dispose properties accessed by
        /// the <see cref="Get{T}(T, string)"><![CDATA[Get<T>]]></see> and <see cref="Set">Set</see> methods
        /// check this property first to prevent the <see cref="ObjectDisposedException"/>.</para>
        /// <note>The change of this property is not observable. When an <see cref="ObservableObjectBase"/> instance is disposed
        /// all subscribers of the <see cref="PropertyChanged"/> event are removed.</note>
        /// </remarks>
        protected bool IsDisposed => isDisposed;

        #endregion

        #region Private Protected Properties

        private protected ThreadSafeDictionary<string, object?> Properties
        {
            get
            {
                ThreadSafeDictionary<string, object?>? result = properties;
                if (result == null || isDisposed)
                    Throw.ObjectDisposedException();
                return result;
            }
        }

        private protected int Count => Properties.Count;

        #endregion

        #region Private Properties

        private StringKeyedDictionary<Type> ReflectedProperties => reflectedProperties ??= reflectedPropertiesCache[GetType()];

        #endregion

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableObjectBase"/> class.
        /// </summary>
        protected ObservableObjectBase()
        {
            properties = new ThreadSafeDictionary<string, object?>(ReflectedProperties.Count, StringSegmentComparer.Ordinal) { PreserveMergedKeys = true };
        }

        #endregion

        #region Methods

        #region Static Methods

        private static StringKeyedDictionary<Type> GetReflectedProperties(Type type)
        {
            static void PopulateProperties(StringKeyedDictionary<Type> dict, IEnumerable<PropertyInfo> props)
            {
                foreach (PropertyInfo prop in props)
                {
                    // for conflicting names only the first property is added
                    if (!dict.ContainsKey(prop.Name))
                        dict[prop.Name] = prop.PropertyType;
                }
            }

            // public properties of all levels
            var result = new StringKeyedDictionary<Type>();
            PopulateProperties(result, type.GetProperties(BindingFlags.Instance | BindingFlags.Public));

            // non-public properties by type (because private properties cannot be obtained for all levels in one step)
            for (Type? t = type; t != null && t != Reflector.ObjectType; t = t.BaseType)
                PopulateProperties(result, t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));

            return result;
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Sets the modified state of this <see cref="ObservableObjectBase"/> instance represented by the <see cref="IsModified"/> property.
        /// </summary>
        /// <param name="value"><see langword="true"/>&#160;to mark the object as modified; <see langword="false"/>&#160;to mark it unmodified.</param>
        public void SetModified(bool value)
        {
            if (isModified == value)
                return;

            isModified = value;
            OnPropertyChanged(new PropertyChangedExtendedEventArgs(!value, value, nameof(IsModified)));
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// <br/>The base implementation clones the internal property storage, the <see cref="IsModified" /> property and if <paramref name="clonePropertyChanged"/> is <see langword="true"/>, then also the subscribers of the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="clonePropertyChanged"><see langword="true"/>&#160;to clone also the subscribers of the <see cref="PropertyChanged"/> event; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public virtual ObservableObjectBase Clone(bool clonePropertyChanged = false)
        {
            Type type = GetType();
            if (type.GetDefaultConstructor() == null)
                Throw.InvalidOperationException(Res.ComponentModelObservableObjectHasNoDefaultCtor(type));
            ObservableObjectBase clone = (ObservableObjectBase)Reflector.CreateInstance(type);
            clone.properties = CloneProperties();
            clone.isModified = isModified;
            clone.propertyChanged = clonePropertyChanged ? propertyChanged : null;
            return clone;
        }

        /// <summary>
        /// Releases the resources held by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Internal Methods

        internal void ReplaceProperties(IDictionary<string, object?> newProperties, bool invokeChangedEvent)
        {
            // Firstly remove the properties, which are not among the new ones.
            // We accept that it can raise some unnecessary events but we cannot set the property if we cannot be sure about the default value.
            IEnumerable<string> toRemove = Properties.Keys.Except(newProperties.Select(p => p.Key));
            foreach (var propertyName in toRemove)
                ResetProperty(propertyName, invokeChangedEvent);

            foreach (var property in newProperties)
                Set(property.Value, invokeChangedEvent, property.Key);
        }

        internal bool TryReplaceProperty(string propertyName, object? originalValue, object? newValue, bool invokeChangedEvent)
        {
            if (propertyName == null!)
                Throw.ArgumentNullException(Argument.propertyName);

            // * -> missing: remove (does not require CanSet check since there is no actual value)
            if (MissingProperty.Equals(newValue))
                return ResetProperty(propertyName, invokeChangedEvent);

            if (!CanSetProperty(propertyName, newValue))
                Throw.InvalidOperationException(Res.ComponentModelCannotSetProperty(propertyName));

            // missing -> newValue: add
            if (MissingProperty.Equals(originalValue))
            {
                if (!Properties.TryAdd(propertyName, newValue))
                    return false;
            }
            // originalValue -> newValue
            else if (!Properties.TryUpdate(propertyName, newValue, originalValue))
                return false;

            if (invokeChangedEvent && !Equals(originalValue, newValue))
                OnPropertyChanged(new PropertyChangedExtendedEventArgs(originalValue, newValue, propertyName));
            return true;
        }

        internal ThreadSafeDictionary<string, object?> CloneProperties()
        {
            ThreadSafeDictionary<string, object?> result = new ThreadSafeDictionary<string, object?>(Properties.Count) { PreserveMergedKeys = true };
            foreach (KeyValuePair<string, object?> property in Properties)
                result[property.Key] = property.Value.DeepClone(customClone);

            return result;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the value of a property, or - if it was not set before -, then creates its initial value.
        /// The created initial value will be stored in the internal property storage without triggering the <see cref="PropertyChanged"/> event.
        /// For constant or simple expressions, or to return a default value for a non-existing property without storing it internally use the other <see cref="Get{T}(T,string)">Get</see> overload.
        /// <br/>For an example, see the <strong>Remarks</strong> section of the <see cref="ObservableObjectBase"/> class.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="createInitialValue">A delegate, which creates the initial value if the property does not exist. If <see langword="null"/>,
        /// then an exception is thrown for an uninitialized property.</param>
        /// <param name="propertyName">The name of the property to get. This parameter is optional.
        /// <br/>Default value: The name of the caller member.</param>
        /// <returns>The value of the property, or the created initial value returned by the <paramref name="createInitialValue"/> parameter.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> cannot be retrieved.
        /// <br/>-or-
        /// <br/>The stored value of the property is not compatible with <typeparamref name="T"/>.
        /// <br/>-or-
        /// <br/><paramref name="propertyName"/> value does not exist and <paramref name="createInitialValue"/> is <see langword="null"/>.
        /// <br/>-or-
        /// <br/>The created default value of the property cannot be set.
        /// <br/>-or-
        /// <br/><see cref="CanGetProperty">CanGetProperty</see> is not overridden and <paramref name="propertyName"/> is not an actual instance property in this instance.
        /// </exception>
        protected T Get<T>(Func<T> createInitialValue, [CallerMemberName] string propertyName = null!)
        {
            if (TryGetPropertyValue(propertyName, true, out object? value))
            {
                if (!typeof(T).CanAcceptValue(value))
                    Throw.InvalidOperationException(Res.ComponentModelReturnedTypeInvalid(typeof(T)));
                return (T)value!;
            }

            if (createInitialValue == null!)
                Throw.InvalidOperationException(Res.ComponentModelPropertyValueNotExist(propertyName));
            T result = createInitialValue.Invoke();
            Set(result, false, propertyName);
            return result;
        }

        /// <summary>
        /// Gets the value of a property or <paramref name="defaultValue"/> if no value is stored for it. No new value will be stored
        /// if the property does not exist. If the default initial value is too complex and should not be evaluated every time when the property is get,
        /// or to throw an exception for an uninitialized property use the other <see cref="Get{T}(Func{T},string)">Get</see> overload.
        /// <br/>For an example, see the <strong>Remarks</strong> section of the <see cref="ObservableObjectBase"/> class.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="defaultValue">The value to return if property does not exist. This parameter is optional.
        /// <br/>Default value: The default value of <typeparamref name="T"/> type.</param>
        /// <param name="propertyName">The name of the property to get. This parameter is optional.
        /// <br/>Default value: The name of the caller member.</param>
        /// <returns>The value of the property, or the specified <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> cannot be retrieved.
        /// <br/>-or-
        /// <br/><see cref="CanGetProperty">CanGetProperty</see> is not overridden and <paramref name="propertyName"/> is not an actual instance property in this instance.
        /// </exception>
        protected T Get<T>(T defaultValue = default!, [CallerMemberName]string propertyName = null!)
            => TryGetPropertyValue(propertyName, true, out object? value) && typeof(T).CanAcceptValue(value) ? (T)value! : defaultValue;

        /// <summary>
        /// Sets the value of a property.
        /// <br/>For an example, see the <strong>Remarks</strong> section of the <see cref="ObservableObjectBase"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="invokeChangedEvent">If <see langword="true"/>, and the <paramref name="value"/> is different from the previously stored value, then invokes the <see cref="PropertyChanged"/> event.</param>
        /// <param name="propertyName">Name of the property to set. This parameter is optional.
        /// <br/>Default value: The name of the caller member.</param>
        /// <returns><see langword="true"/>&#160;if property has been set (change occurred); otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> cannot be set.
        /// <br/>-or-
        /// <br/><see cref="CanSetProperty">CanSetProperty</see> is not overridden and <paramref name="propertyName"/> is not an actual instance property in this instance, or <paramref name="value"/> is not compatible with the property type.
        /// </exception>
        /// <remarks>
        /// <para>If a property is redefined in a derived class with a different type, or a type has multiple indexers with different types,
        /// the this method may throw an <see cref="InvalidOperationException"/>. Overriding the <see cref="CanSetProperty">CanSetProperty</see> method can solve this issue
        /// but it may lead to further errors if multiple properties use the same key in the inner storage.</para>
        /// </remarks>
        protected bool Set(object? value, bool invokeChangedEvent = true, [CallerMemberName] string propertyName = null!)
        {
            if (propertyName == null!)
                Throw.ArgumentNullException(Argument.propertyName);
            if (MissingProperty.Equals(value))
                return ResetProperty(propertyName, invokeChangedEvent);

            if (!CanSetProperty(propertyName, value))
                Throw.InvalidOperationException(Res.ComponentModelCannotSetProperty(propertyName));

            ThreadSafeDictionary<string, object?> values = Properties;
            bool changed = true;
            object? oldValue = MissingProperty;
            values.AddOrUpdate(propertyName, value, (_, origValue) =>
            {
                oldValue = origValue;
                changed = !Equals(value, origValue);
                return changed ? value : origValue;
            });

            if (!changed)
                return false;

            if (invokeChangedEvent)
                OnPropertyChanged(new PropertyChangedExtendedEventArgs(oldValue, value, propertyName));

            return true;
        }

        /// <summary>
        /// Resets the property of the specified name, meaning, it will be removed from the underlying storage so the getter methods will return the default value again.
        /// </summary>
        /// <param name="propertyName">The name of the property to reset.</param>
        /// <param name="invokeChangedEvent"><see langword="true"/>&#160;to allow raising the <see cref="PropertyChanged"/> event; otherwise, <see langword="false"/>.</param>
        /// <returns><see langword="true"/>&#160;if property has been reset (it existed previously); otherwise, <see langword="false"/>.</returns>
        protected bool ResetProperty(string propertyName, bool invokeChangedEvent = true)
        {
            if (propertyName == null!)
                Throw.ArgumentNullException(Argument.propertyName);

            ThreadSafeDictionary<string, object?> values = Properties;
            if (!values.TryRemove(propertyName, out object? oldValue))
                return false;

            if (invokeChangedEvent)
                OnPropertyChanged(new PropertyChangedExtendedEventArgs(oldValue, MissingProperty, propertyName));

            return true;
        }

        /// <summary>
        /// Gets whether the specified property can be retrieved.
        /// <br/>The base implementation allows to get the actual instance properties in this instance.
        /// </summary>
        /// <param name="propertyName">Name of the property to get.</param>
        /// <returns><see langword="true"/>, if the specified property can be retrieved; otherwise, <see langword="false"/>.</returns>
        protected virtual bool CanGetProperty(string propertyName) => ReflectedProperties.ContainsKey(propertyName);

        /// <summary>
        /// Gets whether the specified property can be set.
        /// <br/>The base implementation allows to set the actual instance properties in this instance if the specified <paramref name="value"/> is compatible with the property type.
        /// </summary>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <param name="value">The property value to set.</param>
        /// <returns><see langword="true"/>, if the specified property can be set; otherwise, <see langword="false"/>.</returns>
        protected virtual bool CanSetProperty(string propertyName, object? value)
            => ReflectedProperties.TryGetValue(propertyName, out Type? type) && type.CanAcceptValue(value);

        /// <summary>
        /// Suspends the raising of the <see cref="PropertyChanged"/> event until <see cref="ResumeChangedEvent">ResumeChangeEvents</see>
        /// method is called. Supports nested calls.
        /// </summary>
        protected void SuspendChangedEvent() => Interlocked.Increment(ref suspendCounter);

        /// <summary>
        /// Resumes the raising of the <see cref="PropertyChanged"/> event suspended by the <see cref="SuspendChangedEvent">SuspendChangeEvents</see> method.
        /// </summary>
        protected void ResumeChangedEvent() => Interlocked.Decrement(ref suspendCounter);

        /// <summary>
        /// Gets whether the change of the specified <paramref name="propertyName"/> affects the <see cref="IsModified"/> property.
        /// <br/>The <see cref="ObservableObjectBase"/> implementation excludes the <see cref="IsModified"/> property itself.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        /// <returns><see langword="true"/>&#160;if changing of the specified <paramref name="propertyName"/> affects the value of the <see cref="IsModified"/> property; otherwise, <see langword="false"/>.</returns>
        protected virtual bool AffectsModifiedState(string propertyName) => propertyName != nameof(IsModified);

        /// <summary>
        /// Releases the resources held by this instance.
        /// <br/>The base implementation removes the subscribers of the <see cref="PropertyChanged"/> event and clears the property storage.
        /// If the overridden method disposes properties accessed by the <see cref="Get{T}(T, string)"><![CDATA[Get<T>]]></see> and <see cref="Set">Set</see> methods,
        /// then check the <see cref="IsDisposed"/> property first and call the base method as the last step to prevent <see cref="ObjectDisposedException"/>.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to a call to <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;
            properties = null;
            reflectedProperties = null;
            propertyChanged = null;
        }

        #endregion

        #region Protected Internal Methods

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedExtendedEventArgs" /> instance containing the event data.</param>
        protected internal virtual void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
        {
            if (e == null!)
                Throw.ArgumentNullException(Argument.e);
            if (!isModified && AffectsModifiedState(e.PropertyName!))
                SetModified(true);
            if (suspendCounter <= 0)
                propertyChanged?.Invoke(this, e);
        }

        #endregion

        #region Private Protected Methods

        private protected bool TryGetPropertyValue(string propertyName, bool errorIfCannotGetProperty, out object? value)
        {
            if (propertyName == null!)
                Throw.ArgumentNullException(Argument.propertyName);
            if (!CanGetProperty(propertyName))
            {
                if (errorIfCannotGetProperty)
                    Throw.InvalidOperationException(Res.ComponentModelCannotGetProperty(propertyName));
                value = null;
                return false;
            }

            return Properties.TryGetValue(propertyName, out value);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        object ICloneable.Clone() => Clone();

        #endregion

        #endregion

        #endregion
    }
}
