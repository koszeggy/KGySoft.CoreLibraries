#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PersistableObjectBase.cs
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
using System.Diagnostics.CodeAnalysis;

using KGySoft.Collections;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a base class for component model classes, which provide a public access to their internal property storage located in the <see cref="ObservableObjectBase"/> base class
    /// by implementing also the <see cref="IPersistableObject"/> interface.
    /// <br/>For details see the <strong>Remarks</strong> section.
    /// </summary>
    /// <remarks>
    /// <para>The class should be cast to <see cref="IPersistableObject"/> to access the property storage and allow to manipulate the properties by name.
    /// All of the actually stored values can be read and restored by the <see cref="IPersistableObject.GetProperties">GetProperties</see> and
    /// <see cref="IPersistableObject.SetProperties">SetProperties</see> methods.</para>
    /// <para>The <see cref="IPersistableObject"/> also provides some concurrent-proof operations if the instance is accessed from multiple threads. See the <see cref="IPersistableObject.TryGetPropertyValue">TryGetPropertyValue</see>,
    /// <see cref="IPersistableObject.GetPropertyOrDefault{T}">GetPropertyOrDefault</see> and <see cref="IPersistableObject.TryReplaceProperty">TryReplaceProperty</see> methods.</para>
    /// <note type="implement">For an example see the <strong>Remarks</strong> section of the <see cref="ObservableObjectBase"/> class.
    /// The same applies also for the <see cref="PersistableObjectBase"/> class in terms of implementation.</note>
    /// </remarks>
    /// <threadsafety instance="true"/>
    /// <seealso cref="IPersistableObject" />
    /// <seealso cref="ObservableObjectBase" />
    /// <seealso cref="UndoableObjectBase" />
    /// <seealso cref="EditableObjectBase" />
    /// <seealso cref="ValidatingObjectBase" />
    /// <seealso cref="ModelBase" />
    [Serializable]
    public abstract class PersistableObjectBase : ObservableObjectBase, IPersistableObject
    {
        #region Methods

        bool IPersistableObject.TryGetPropertyValue(string propertyName, out object? value)
            => TryGetPropertyValue(propertyName, false, out value);

        bool IPersistableObject.CanGetProperty(string propertyName) => CanGetProperty(propertyName);
        bool IPersistableObject.CanSetProperty(string propertyName, object? value) => CanSetProperty(propertyName, value);

        T IPersistableObject.GetPropertyOrDefault<T>(string propertyName, T defaultValue)
            => Get(defaultValue, propertyName);

        bool IPersistableObject.SetProperty(string propertyName, object? value, bool invokeChangedEvent)
            => Set(value, invokeChangedEvent, propertyName);

        bool IPersistableObject.ResetProperty(string propertyName, bool invokeChangedEvent)
            => ResetProperty(propertyName, invokeChangedEvent);

        bool IPersistableObject.TryReplaceProperty(string propertyName, object? originalValue, object? newValue, bool invokeChangedEvent)
            => TryReplaceProperty(propertyName, originalValue, newValue, invokeChangedEvent);

        IDictionary<string, object?> IPersistableObject.GetProperties()
        {
            var result = new StringKeyedDictionary<object?>(Count);
            foreach (KeyValuePair<string, object?> item in Properties)
            {
                if (!CanGetProperty(item.Key))
                    Throw.InvalidOperationException(Res.ComponentModelCannotGetProperty(item.Key));
                result.Add(item.Key, item.Value);
            }

            return result;
        }

        void IPersistableObject.SetProperties(IDictionary<string, object?> newProperties, bool triggerChangedEvent)
        {
            if (newProperties == null!)
                Throw.ArgumentNullException(Argument.newProperties);

            // Properties can change even during the set.
            // This is desirable because OnChanging/changed events are raised during this process,
            // which may cause that consumers read/write the values.
            foreach (var property in newProperties)
                Set(property.Value, triggerChangedEvent, property.Key);
        }

        void IPersistableObject.ReplaceProperties(IDictionary<string, object?> properties, bool triggerChangedEvent)
            => ReplaceProperties(properties, triggerChangedEvent);

        #endregion
    }
}
